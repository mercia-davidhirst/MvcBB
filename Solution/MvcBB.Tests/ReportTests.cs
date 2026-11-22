using MvcBB.API.InMemory;
using MvcBB.Shared.Models.Report;

namespace MvcBB.Tests;

public class ReportTests
{
    [Fact]
    public void SiteIssue_IsAppendedSoStoredValuesDontShift()
    {
        // Stored as numbers in SQL Server/PostgreSQL - existing values must not move.
        Assert.Equal(0, (int)ReportType.Thread);
        Assert.Equal(1, (int)ReportType.Post);
        Assert.Equal(2, (int)ReportType.Message);
        Assert.Equal(3, (int)ReportType.User);
        Assert.Equal(4, (int)ReportType.SiteIssue);
    }

    [Theory]
    [InlineData(ReportType.SiteIssue, "Site issue")]
    [InlineData(ReportType.Post, "Post")]
    [InlineData(ReportType.User, "User")]
    public void TypeName_IsHumanReadable(ReportType type, string expected)
    {
        Assert.Equal(expected, ReportTypeNames.GetDisplayName(type));
        Assert.Equal(expected, new ReportResponse { Type = type }.TypeName);
    }

    [Fact]
    public void HasContent_IsFalseOnlyForSiteIssues()
    {
        Assert.False(new ReportResponse { Type = ReportType.SiteIssue }.HasContent);
        Assert.True(new ReportResponse { Type = ReportType.Thread }.HasContent);
    }

    [Theory]
    [InlineData(ReportStatus.Pending, true)]
    [InlineData(ReportStatus.Investigating, true)]
    [InlineData(ReportStatus.Resolved, false)]
    [InlineData(ReportStatus.Dismissed, false)]
    public void IsOpen(ReportStatus status, bool expected)
    {
        Assert.Equal(expected, ReportTypeNames.IsOpen(status));
    }

    [Theory]
    [InlineData("/Threads/Details/5?page=2", "/Threads/Details/5?page=2")]
    [InlineData("  /Boards  ", "/Boards")]
    [InlineData("/", "/")]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("https://example.com/x", null)]
    [InlineData("//example.com/x", null)]
    [InlineData("/\\example.com/x", null)]
    [InlineData("javascript:alert(1)", null)]
    [InlineData("Threads/Details/5", null)]
    [InlineData("/a\nb", null)]
    public void PageUrl_KeepsOnlySiteRelativePaths(string? input, string? expected)
    {
        Assert.Equal(expected, ReportPageUrl.Normalize(input));
    }

    [Fact]
    public void PageUrl_RejectsOverlongValues()
    {
        Assert.Null(ReportPageUrl.Normalize("/" + new string('a', ReportPageUrl.MaxLength)));
    }

    [Fact]
    public void InMemoryRepository_CountsOpenAndSiteIssueReports()
    {
        var repository = new InMemoryReportRepository(new InMemoryStore());
        repository.Add(new Report { Reason = "a", ReporterUserId = "1", Type = ReportType.SiteIssue, PageUrl = "/Boards", Status = ReportStatus.Pending });
        repository.Add(new Report { Reason = "b", ReporterUserId = "1", Type = ReportType.Post, ContentId = 3, Status = ReportStatus.Investigating });
        repository.Add(new Report { Reason = "c", ReporterUserId = "1", Type = ReportType.SiteIssue, Status = ReportStatus.Resolved });
        repository.Add(new Report { Reason = "d", ReporterUserId = "1", Type = ReportType.Thread, ContentId = 1, Status = ReportStatus.Dismissed });

        Assert.Equal(2, repository.CountOpen());
        Assert.Equal(2, repository.CountByType(ReportType.SiteIssue));
        Assert.Equal("/Boards", repository.GetAll().Single(r => r.Reason == "a").PageUrl);
    }
}
