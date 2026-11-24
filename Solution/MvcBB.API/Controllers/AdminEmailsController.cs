using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MvcBB.API.Services;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.AdminEmail;
using MvcBB.Shared.Models.Settings;
using MvcBB.Shared.Models.User;
using MvcBB.Shared.Services;

namespace MvcBB.API.Controllers
{
    /// <summary>
    /// Emails from admins to members (maintenance notices, news, alerts).
    /// Sending only queues the email: AdminEmailDispatcher sends it in the
    /// background in throttled batches, and the send log shows progress.
    /// Every endpoint needs a signed-in Administrator - the App's service
    /// token has no user to send as.
    /// </summary>
    [ApiController]
    [Route("api/admin-emails")]
    [Authorize(Roles = "Administrator")]
    public class AdminEmailsController : ControllerBase
    {
        private const int MaxLogEntries = 200;

        private readonly IAdminEmailRepository _adminEmailRepository;
        private readonly IUserRepository _userRepository;
        private readonly IDisplaySettingsService _displaySettingsService;
        private readonly IEmailSender _emailSender;
        private readonly AdminEmailQueue _queue;
        private readonly IConfiguration _configuration;
        private readonly AdminEmailOptions _options;
        private readonly IEmailSendLogRepository _sendLog;
        private readonly EmailSendCounter _sendCounter;
        private readonly TimeProvider _timeProvider;

        public AdminEmailsController(
            IAdminEmailRepository adminEmailRepository,
            IUserRepository userRepository,
            IDisplaySettingsService displaySettingsService,
            IEmailSender emailSender,
            AdminEmailQueue queue,
            IConfiguration configuration,
            IOptionsSnapshot<AdminEmailOptions> options,
            IEmailSendLogRepository sendLog,
            EmailSendCounter sendCounter,
            TimeProvider timeProvider)
        {
            _adminEmailRepository = adminEmailRepository;
            _userRepository = userRepository;
            _displaySettingsService = displaySettingsService;
            _emailSender = emailSender;
            _queue = queue;
            _configuration = configuration;
            _options = options.Value;
            _sendLog = sendLog;
            _sendCounter = sendCounter;
            _timeProvider = timeProvider;
        }

        /// <summary>Emails sent in the last hour and day against the host's sending limits, and how many admin emails could go now.</summary>
        [HttpGet("usage")]
        public ActionResult<EmailSendUsage> GetUsage()
        {
            return Ok(CurrentUsage(_displaySettingsService.GetDisplaySettings()));
        }

        /// <summary>
        /// Emails sent over a period, for the Sending activity chart: range
        /// "24h" (hourly bars plus a rolling 60-minute line), "7d" or "30d"
        /// (daily bars), in the forum's time zone, plus current usage.
        /// </summary>
        [HttpGet("activity")]
        public ActionResult<EmailActivity> GetActivity([FromQuery] string? range = null)
        {
            var settings = _displaySettingsService.GetDisplaySettings();
            return Ok(EmailActivityBuilder.Build(
                range,
                _sendLog,
                CurrentUsage(settings),
                ForumTimeZone.Resolve(settings.TimeZoneId),
                _timeProvider.GetUtcNow().UtcDateTime));
        }

        private EmailSendUsage CurrentUsage(DisplaySettings settings) =>
            AdminEmailRateLimit.Usage(SendLimits.Resolve(_options, settings), _sendCounter, _timeProvider.GetUtcNow().UtcDateTime);

        /// <summary>What sending would do - recipient count, opt-outs left out, From/Reply-To - without sending.</summary>
        [HttpPost("preview")]
        public ActionResult<AdminEmailPreview> Preview(SendAdminEmailRequest request)
        {
            if (!TryPrepare(request, out var admin, out var sender, out var error))
            {
                return error!;
            }

            var (total, optedOut) = _adminEmailRepository.CountRecipients(TargetRole(request), TargetUserId(request));
            var settings = _displaySettingsService.GetDisplaySettings();
            var siteName = settings.SiteName;
            var limits = SendLimits.Resolve(_options, settings);
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var estimate = AdminEmailRateLimit.Estimate(
                limits, _sendCounter.SentSince(now - AdminEmailRateLimit.Day), now, request.IsImportant ? total : total - optedOut);

            return new AdminEmailPreview
            {
                RecipientCount = request.IsImportant ? total : total - optedOut,
                OptedOutCount = request.IsImportant ? 0 : optedOut,
                From = AdminEmailComposer.FormatAddress(sender!.FromAddress, sender.FromName),
                ReplyTo = sender.ReplyToAddress,
                AdminAddressMovedToReplyTo = sender.AdminAddressMovedToReplyTo,
                SingleUserName = request.Audience == AdminEmailAudience.SingleUser ? _userRepository.GetById(request.UserId!.Value)?.Username : null,
                Footer = AdminEmailComposer.BuildFooter(siteName, AdminEmailLinks.ProfileUrl(_configuration), request.IsImportant),
                MaxPerHour = limits.PerHour,
                MaxPerDay = limits.PerDay,
                EstimatedHours = estimate.Hours,
                CanSendNow = estimate.CanSendNow,
                Usage = AdminEmailRateLimit.Usage(limits, _sendCounter, now)
            };
        }

        /// <summary>Queues the email for sending and returns its send log entry.</summary>
        [HttpPost]
        public ActionResult<AdminEmail> Send(SendAdminEmailRequest request)
        {
            if (!TryPrepare(request, out var admin, out var sender, out var error))
            {
                return error!;
            }

            var (total, optedOut) = _adminEmailRepository.CountRecipients(TargetRole(request), TargetUserId(request));
            var recipientCount = request.IsImportant ? total : total - optedOut;
            if (recipientCount == 0)
            {
                return BadRequest(new
                {
                    message = optedOut > 0
                        ? "Everyone in that audience has turned off emails from the forum team. Tick \"Important service alert\" only if this really must reach them."
                        : "There's nobody in that audience to email."
                });
            }

            var email = _adminEmailRepository.Add(new AdminEmail
            {
                SentByUserId = admin!.Id,
                SentByUsername = admin.Username,
                CreatedAt = DateTime.UtcNow,
                Subject = request.Subject.Trim(),
                Body = request.Body,
                Audience = request.Audience,
                Role = TargetRole(request),
                UserId = TargetUserId(request),
                SenderMode = request.SenderMode,
                FromAddress = sender!.FromAddress,
                FromName = sender.FromName,
                ReplyToAddress = sender.ReplyToAddress,
                IsImportant = request.IsImportant,
                Status = AdminEmailStatus.Queued,
                RecipientCount = recipientCount
            });

            _queue.Enqueue(email.Id);

            return CreatedAtAction(nameof(GetById), new { id = email.Id }, email);
        }

        /// <summary>Sends the email straight away to the signed-in admin only, exactly as a member would get it.</summary>
        [HttpPost("test")]
        public async Task<ActionResult> SendTest(SendAdminEmailRequest request)
        {
            if (!TryPrepare(request, out var admin, out var sender, out var error))
            {
                return error!;
            }

            var siteName = _displaySettingsService.GetDisplaySettings().SiteName;
            var preview = new AdminEmail
            {
                Subject = "[Test] " + request.Subject.Trim(),
                Body = request.Body,
                SentByUsername = admin!.Username,
                FromAddress = sender!.FromAddress,
                FromName = sender.FromName,
                ReplyToAddress = sender.ReplyToAddress,
                IsImportant = request.IsImportant
            };
            var recipient = new AdminEmailRecipient { UserId = admin.Id, Username = admin.Username, Email = admin.Email };

            try
            {
                await _emailSender.SendAsync(AdminEmailComposer.BuildMessage(preview, recipient, siteName, AdminEmailLinks.ProfileUrl(_configuration)));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"The test email couldn't be sent: {ex.Message}" });
            }

            return Ok(new { message = $"Test email sent to {admin.Email}." });
        }

        /// <summary>The send log, newest first.</summary>
        [HttpGet]
        public ActionResult<IEnumerable<AdminEmail>> GetRecent([FromQuery] int take = 50)
        {
            return Ok(_adminEmailRepository.GetRecent(Math.Clamp(take, 1, MaxLogEntries)));
        }

        [HttpGet("{id:int}")]
        public ActionResult<AdminEmail> GetById(int id)
        {
            var email = _adminEmailRepository.GetById(id);
            return email == null ? NotFound(new { message = "Email not found" }) : email;
        }

        /// <summary>
        /// The members the email couldn't be sent to and still hasn't reached,
        /// with the latest reason for each, oldest first - the same members
        /// FailedCount counts. Anyone a resume reached since is left out.
        /// </summary>
        [HttpGet("{id:int}/failures")]
        public ActionResult<IEnumerable<AdminEmailFailure>> GetFailures(int id)
        {
            if (_adminEmailRepository.GetById(id) == null)
            {
                return NotFound(new { message = "Email not found" });
            }

            var delivered = _adminEmailRepository.GetDeliveredUserIds(id).ToHashSet();
            var failures = _adminEmailRepository.GetFailures(id)
                .Where(f => !delivered.Contains(f.UserId))
                .GroupBy(f => f.UserId)
                .Select(g => g.Last())
                .OrderBy(f => f.FailedAt).ThenBy(f => f.Id)
                .ToList();
            return Ok(failures);
        }

        /// <summary>For an Interrupted email: how many members a resume would send it to (the audience now, less those it already reached).</summary>
        [HttpGet("{id:int}/resume")]
        public ActionResult<int> GetResumeCount(int id)
        {
            var email = _adminEmailRepository.GetById(id);
            if (email == null)
            {
                return NotFound(new { message = "Email not found" });
            }

            return Ok(RemainingRecipientCount(email));
        }

        /// <summary>
        /// Queues an Interrupted email again. It goes only to members in its
        /// audience it hasn't reached yet, worked out when sending starts -
        /// members who joined since are included, and those who opted out since
        /// are left out (unless it's an important alert).
        /// </summary>
        [HttpPost("{id:int}/resume")]
        public ActionResult<AdminEmail> Resume(int id)
        {
            var email = _adminEmailRepository.GetById(id);
            if (email == null)
            {
                return NotFound(new { message = "Email not found" });
            }

            if (email.Status != AdminEmailStatus.Interrupted)
            {
                return BadRequest(new { message = "Only an email whose sending was interrupted can be resumed." });
            }

            if (RemainingRecipientCount(email) == 0)
            {
                return BadRequest(new { message = "Everyone this email is for has already had it." });
            }

            email.Status = AdminEmailStatus.Queued;
            email.CompletedAt = null;
            email.PausedUntil = null;
            _adminEmailRepository.UpdateProgress(email);
            _queue.Enqueue(email.Id);

            return Ok(email);
        }

        private int RemainingRecipientCount(AdminEmail email)
        {
            var delivered = _adminEmailRepository.GetDeliveredUserIds(email.Id).ToHashSet();
            return _adminEmailRepository.GetRecipients(email.Role, email.UserId, includeOptedOut: email.IsImportant)
                .Count(r => !delivered.Contains(r.UserId));
        }

        private static UserRole? TargetRole(SendAdminEmailRequest request) =>
            request.Audience == AdminEmailAudience.Role ? request.Role : null;

        private static int? TargetUserId(SendAdminEmailRequest request) =>
            request.Audience == AdminEmailAudience.SingleUser ? request.UserId : null;

        // Shared checks for preview/send/test: a real admin with an email
        // address, a valid audience, and the From/Reply-To worked out.
        private bool TryPrepare(SendAdminEmailRequest request, out User? admin, out AdminEmailComposer.Sender? sender, out ActionResult? error)
        {
            admin = null;
            sender = null;
            error = null;

            if (!ModelState.IsValid)
            {
                error = BadRequest(ModelState);
                return false;
            }

            var adminId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedId) ? parsedId : 0;
            admin = adminId == 0 ? null : _userRepository.GetById(adminId);
            if (admin == null)
            {
                error = Forbid();
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Body))
            {
                error = BadRequest(new { message = "Enter a subject and a message." });
                return false;
            }

            if (!Enum.IsDefined(request.Audience) || !Enum.IsDefined(request.SenderMode))
            {
                error = BadRequest(new { message = "Choose who to send to and who it's from." });
                return false;
            }

            if (request.Audience == AdminEmailAudience.Role &&
                (request.Role == null || request.Role == UserRole.Deleted || !Enum.IsDefined(request.Role.Value)))
            {
                error = BadRequest(new { message = "Choose a role to send to." });
                return false;
            }

            if (request.Audience == AdminEmailAudience.SingleUser)
            {
                var target = request.UserId.HasValue ? _userRepository.GetById(request.UserId.Value) : null;
                if (target == null || target.Role == UserRole.Deleted)
                {
                    error = BadRequest(new { message = "That member wasn't found." });
                    return false;
                }

                if (string.IsNullOrWhiteSpace(target.Email))
                {
                    error = BadRequest(new { message = $"{target.Username} has no email address." });
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(admin.Email))
            {
                error = BadRequest(new { message = "Your account has no email address, so emails can't be sent as you or tested. Add one on your profile first." });
                return false;
            }

            var siteName = _displaySettingsService.GetDisplaySettings().SiteName;
            var noReplyAddress = _configuration["Smtp:FromEmail"];
            if (string.IsNullOrWhiteSpace(noReplyAddress))
            {
                error = BadRequest(new { message = "Smtp:FromEmail isn't set in the API's appsettings.json, so there's no site address to send from." });
                return false;
            }

            sender = AdminEmailComposer.ResolveSender(request.SenderMode, admin.Username, admin.Email, siteName, noReplyAddress, _options.AllowAdminFromAddress);
            return true;
        }
    }
}
