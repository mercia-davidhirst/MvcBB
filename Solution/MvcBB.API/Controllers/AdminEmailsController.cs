using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MvcBB.API.Services;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.AdminEmail;
using MvcBB.Shared.Models.User;

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

        public AdminEmailsController(
            IAdminEmailRepository adminEmailRepository,
            IUserRepository userRepository,
            IDisplaySettingsService displaySettingsService,
            IEmailSender emailSender,
            AdminEmailQueue queue,
            IConfiguration configuration,
            IOptionsSnapshot<AdminEmailOptions> options)
        {
            _adminEmailRepository = adminEmailRepository;
            _userRepository = userRepository;
            _displaySettingsService = displaySettingsService;
            _emailSender = emailSender;
            _queue = queue;
            _configuration = configuration;
            _options = options.Value;
        }

        /// <summary>What sending would do - recipient count, opt-outs left out, From/Reply-To - without sending.</summary>
        [HttpPost("preview")]
        public ActionResult<AdminEmailPreview> Preview(SendAdminEmailRequest request)
        {
            if (!TryPrepare(request, out var admin, out var sender, out var error))
            {
                return error!;
            }

            var (total, optedOut) = _adminEmailRepository.CountRecipients(TargetRole(request), TargetUserId(request));
            var siteName = _displaySettingsService.GetDisplaySettings().SiteName;

            return new AdminEmailPreview
            {
                RecipientCount = request.IsImportant ? total : total - optedOut,
                OptedOutCount = request.IsImportant ? 0 : optedOut,
                From = AdminEmailComposer.FormatAddress(sender!.FromAddress, sender.FromName),
                ReplyTo = sender.ReplyToAddress,
                AdminAddressMovedToReplyTo = sender.AdminAddressMovedToReplyTo,
                SingleUserName = request.Audience == AdminEmailAudience.SingleUser ? _userRepository.GetById(request.UserId!.Value)?.Username : null,
                Footer = AdminEmailComposer.BuildFooter(siteName, AdminEmailLinks.ProfileUrl(_configuration), request.IsImportant)
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
