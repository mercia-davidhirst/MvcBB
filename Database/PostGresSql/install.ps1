<#
.SYNOPSIS
    Deploys the MvcBB PostgreSQL schema (Database/PostGresSql) in dependency
    order: Tables -> helper Functions -> Views -> remaining Functions ->
    Stored Procedures -> Seed Data.

.DESCRIPTION
    Runs every .sql script under this folder through psql, in the order
    required by their DROP/CREATE dependencies:
      1. Tables (00_extensions.sql enables citext, then 01-06 create the
         forum tables in FK order, 07-08 create the standalone BBCode tag /
         smilie tables).
      2. fn_role_name and fn_report_content_exists - helper functions that
         vw_post_details and sp_report_insert depend on, so they must exist
         before step 3.
      3. vw_post_details - the view, which needs fn_role_name to already
         exist.
      4. The remaining Functions, including the fn_post_* ones which query
         the view created in step 3.
      5. Stored Procedures.
      6. Seed Data - only inserts when its target table is empty, so
         re-running won't resurrect rows an admin has since edited or
         deleted.
    Each script already contains its own "DROP ... IF EXISTS" header, so this
    is safe to re-run.

.PARAMETER HostName
    PostgreSQL server host, e.g. "localhost".

.PARAMETER Port
    PostgreSQL server port. Defaults to 5432.

.PARAMETER Database
    Target database name. Created automatically if -CreateDatabase is passed
    and it does not already exist.

.PARAMETER Username
    PostgreSQL role to connect as. Defaults to "postgres".

.PARAMETER Password
    Optional password, as a SecureString. If omitted, psql falls back to a
    configured .pgpass file or prompts interactively.

.PARAMETER CreateDatabase
    Create the target database first if it doesn't already exist.

.EXAMPLE
    ./install.ps1 -HostName localhost -Database mvcbb -Username postgres -CreateDatabase
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$HostName,

    [int]$Port = 5432,

    [Parameter(Mandatory = $true)]
    [string]$Database,

    [string]$Username = "postgres",

    [SecureString]$Password,

    [switch]$CreateDatabase
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    throw "psql was not found on PATH. Install the PostgreSQL command-line client and try again."
}

# Use the PGPASSWORD env var rather than a command-line argument so the
# password never appears as a plain process argument.
if ($Password) {
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Password)
    try {
        $env:PGPASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
    } finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

function Invoke-SqlFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$TargetDatabase
    )

    Write-Host "  -> $(Resolve-Path -Relative $Path)"
    & psql -h $HostName -p $Port -U $Username -d $TargetDatabase -v ON_ERROR_STOP=1 -f $Path
    if ($LASTEXITCODE -ne 0) {
        throw "psql failed on '$Path' (exit code $LASTEXITCODE)"
    }
}

try {
    if ($CreateDatabase) {
        Write-Host "Ensuring database '$Database' exists on '$HostName`:$Port'..."
        $exists = & psql -h $HostName -p $Port -U $Username -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname = '$Database'"
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to check for database '$Database' (exit code $LASTEXITCODE)"
        }
        if ($exists -notmatch "1") {
            & psql -h $HostName -p $Port -U $Username -d postgres -v ON_ERROR_STOP=1 -c "CREATE DATABASE `"$Database`";"
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to create database '$Database' (exit code $LASTEXITCODE)"
            }
        }
    }

    Write-Host "Deploying MvcBB schema to $HostName`:$Port/$Database"

    Write-Host "`n1) Tables (extension + tables, FK order)"
    Get-ChildItem -Path (Join-Path $root "Tables") -Filter *.sql | Sort-Object Name | ForEach-Object {
        Invoke-SqlFile -Path $_.FullName -TargetDatabase $Database
    }

    Write-Host "`n2) Helper functions required by the view (fn_role_name, fn_report_content_exists)"
    $helperFunctions = @("fn_role_name.sql", "fn_report_content_exists.sql")
    foreach ($name in $helperFunctions) {
        Invoke-SqlFile -Path (Join-Path $root "Functions" $name) -TargetDatabase $Database
    }

    Write-Host "`n3) Views"
    Get-ChildItem -Path (Join-Path $root "Views") -Filter *.sql | Sort-Object Name | ForEach-Object {
        Invoke-SqlFile -Path $_.FullName -TargetDatabase $Database
    }

    Write-Host "`n4) Remaining functions"
    Get-ChildItem -Path (Join-Path $root "Functions") -Filter *.sql |
        Where-Object { $helperFunctions -notcontains $_.Name } |
        Sort-Object Name |
        ForEach-Object { Invoke-SqlFile -Path $_.FullName -TargetDatabase $Database }

    Write-Host "`n5) Stored Procedures"
    Get-ChildItem -Path (Join-Path $root "Stored Procedures") -Filter *.sql | Sort-Object Name | ForEach-Object {
        Invoke-SqlFile -Path $_.FullName -TargetDatabase $Database
    }

    Write-Host "`n6) Seed Data"
    Get-ChildItem -Path (Join-Path $root "Seed Data") -Filter *.sql | Sort-Object Name | ForEach-Object {
        Invoke-SqlFile -Path $_.FullName -TargetDatabase $Database
    }

    Write-Host "`nDone."
} finally {
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}
