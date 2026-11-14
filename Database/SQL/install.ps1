<#
.SYNOPSIS
    Deploys the MvcBB SQL Server schema (Database/SQL) in dependency order:
    Tables -> Functions -> Views -> Stored Procedures -> Seed Data.

.DESCRIPTION
    Runs every .sql script under this folder through sqlcmd, in the order
    required by their DROP/CREATE dependencies (a table must exist before a
    view or function can reference it, a function must exist before a view
    or procedure calls it, etc).

    WARNING - this is NOT safe to re-run against a database that already
    holds real data. Every script under Tables/ does "DROP TABLE IF EXISTS"
    then "CREATE TABLE" UNCONDITIONALLY, every single run - that deletes
    every row in that table, every time this script runs, with no prompt
    and no way back. Functions/, Views/ and Stored Procedures/ are genuinely
    safe to re-run any time (they're just code, not data), and Seed Data/
    scripts only insert when their target table is empty - but Tables/ is
    not, despite that having been claimed here previously. To add new
    schema (new tables/procs/functions/views) to a database that already
    has data in it, do NOT run this whole script - run only the specific
    new files you need (sqlcmd against each one directly), or everything
    under Functions/Views/Stored Procedures/Seed Data plus just the brand
    new Tables/*.sql files that don't exist in that database yet.

.PARAMETER ServerInstance
    SQL Server instance to connect to, e.g. "localhost" or ".\SQLEXPRESS".

.PARAMETER Database
    Target database name. Created automatically if -CreateDatabase is passed
    and it does not already exist.

.PARAMETER Username
    SQL Server login. If omitted, Windows/integrated authentication is used.

.PARAMETER Password
    Password for -Username. Prompted for interactively (without echoing) if
    -Username is supplied without -Password.

.PARAMETER CreateDatabase
    Create the target database first if it doesn't already exist.

.EXAMPLE
    ./install.ps1 -ServerInstance localhost -Database MvcBB -CreateDatabase

.EXAMPLE
    ./install.ps1 -ServerInstance localhost -Database MvcBB -Username sa -Password "MyPassword123"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ServerInstance,

    [Parameter(Mandatory = $true)]
    [string]$Database,

    [string]$Username,

    [string]$Password,

    [switch]$CreateDatabase
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw "sqlcmd was not found on PATH. Install the SQL Server command-line tools (sqlcmd) and try again."
}

# -U must be passed explicitly on the command line: sqlcmd defaults to
# Windows/Integrated Authentication whenever -U is absent, regardless of
# SQLCMDUSER/SQLCMDPASSWORD - that mismatch is what previously produced
# "Login failed...this login is from an untrusted domain and cannot be used
# with Integrated Authentication" even when a username/password were given.
# The password itself still goes through the SQLCMDPASSWORD env var (not -P)
# so it never appears as a plain command-line argument.
$authArgs = @("-E")
if ($Username) {
    if (-not $Password) {
        $securePassword = Read-Host -Prompt "Password for $Username" -AsSecureString
        $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
        try {
            $Password = [Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
        } finally {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
        }
    }
    $env:SQLCMDPASSWORD = $Password
    $authArgs = @("-U", $Username)
}

function Invoke-SqlFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$TargetDatabase
    )

    Write-Host "  -> $(Resolve-Path -Relative $Path)"
    & sqlcmd -S $ServerInstance -d $TargetDatabase @authArgs -b -I -i $Path
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd failed on '$Path' (exit code $LASTEXITCODE)"
    }
}

try {
    if ($CreateDatabase) {
        Write-Host "Ensuring database '$Database' exists on '$ServerInstance'..."
        & sqlcmd -S $ServerInstance -d master @authArgs -b -Q "IF DB_ID(N'$Database') IS NULL CREATE DATABASE [$Database];"
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to create database '$Database' (exit code $LASTEXITCODE)"
        }
    }

    Write-Host "Deploying MvcBB schema to [$ServerInstance].[$Database]"

    Write-Host "`n1) Tables"
    Get-ChildItem -Path (Join-Path $root "Tables") -Filter *.sql | Sort-Object Name | ForEach-Object {
        Invoke-SqlFile -Path $_.FullName -TargetDatabase $Database
    }

    Write-Host "`n2) Functions"
    Get-ChildItem -Path (Join-Path $root "Functions") -Filter *.sql | Sort-Object Name | ForEach-Object {
        Invoke-SqlFile -Path $_.FullName -TargetDatabase $Database
    }

    Write-Host "`n3) Views"
    Get-ChildItem -Path (Join-Path $root "Views") -Filter *.sql | Sort-Object Name | ForEach-Object {
        Invoke-SqlFile -Path $_.FullName -TargetDatabase $Database
    }

    Write-Host "`n4) Stored Procedures"
    Get-ChildItem -Path (Join-Path $root "Stored Procedures") -Filter *.sql | Sort-Object Name | ForEach-Object {
        Invoke-SqlFile -Path $_.FullName -TargetDatabase $Database
    }

    Write-Host "`n5) Seed Data"
    Get-ChildItem -Path (Join-Path $root "Seed Data") -Filter *.sql | Sort-Object Name | ForEach-Object {
        Invoke-SqlFile -Path $_.FullName -TargetDatabase $Database
    }

    Write-Host "`nDone."
} finally {
    Remove-Item Env:\SQLCMDPASSWORD -ErrorAction SilentlyContinue
}
