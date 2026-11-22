using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MvcBB.API.Controllers;
using MvcBB.API.InMemory;
using MvcBB.Shared.Models.Message;
using MvcBB.Shared.Models.Report;

namespace MvcBB.Tests;

public class ReportsControllerTests
{
    private const string SenderId = "10";
    private const string RecipientId = "11";
    private const string OtherMemberId = "12";

    private static (ReportsController Controller, InMemoryReportRepository Reports, int MessageId) Create(string callerUserId)
    {
        var store = new InMemoryStore();
        var messages = new InMemoryMessageRepository(store);
        var message = messages.Add(new Message
        {
            Subject = "Hello",
            Content = "Body",
            SenderUserId = SenderId,
            RecipientUserId = RecipientId,
            CreatedAt = DateTime.UtcNow
        });

        var reports = new InMemoryReportRepository(store);
        var controller = new ReportsController(
            reports,
            new InMemoryForumThreadRepository(store),
            new InMemoryPostRepository(store),
            messages,
            new InMemoryUserRepository(store))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, callerUserId) }, "Test"))
                }
            }
        };

        return (controller, reports, message.Id);
    }

    [Fact]
    public void MessageReport_FromRecipient_IsAccepted()
    {
        var (controller, reports, messageId) = Create(RecipientId);

        var result = controller.CreateReport(new CreateReportRequest { Reason = "Abuse", Type = ReportType.Message, ContentId = messageId });

        Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(1, reports.Count());
    }

    [Theory]
    [InlineData(SenderId)]
    [InlineData(OtherMemberId)]
    public void MessageReport_FromAnyoneButTheRecipient_IsNotFound(string callerUserId)
    {
        var (controller, reports, messageId) = Create(callerUserId);

        var result = controller.CreateReport(new CreateReportRequest { Reason = "Abuse", Type = ReportType.Message, ContentId = messageId });

        Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(0, reports.Count());
    }

    [Fact]
    public void MessageReport_ForMissingMessage_LooksTheSameAsSomeoneElses()
    {
        var (controller, reports, _) = Create(RecipientId);

        var result = controller.CreateReport(new CreateReportRequest { Reason = "Abuse", Type = ReportType.Message, ContentId = 999 });

        Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(0, reports.Count());
    }
}
