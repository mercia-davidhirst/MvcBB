using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MvcBB.API.Models.Authorisation;

namespace MvcBB.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly MvcBB.API.Services.JwtTokenService _jwtTokenService;

        public AuthController(IConfiguration configuration, MvcBB.API.Services.JwtTokenService jwtTokenService)
        {
            _configuration = configuration;
            _jwtTokenService = jwtTokenService;
        }

        [AllowAnonymous]
        [HttpPost("token")]
        public ActionResult<LoginResponse> GetToken(LoginRequest request)
        {
            // The single known service client (MvcBB.App's own anonymous/service
            // calls) - credentials live in appsettings.json (ApiClient:ClientId/
            // ClientSecret), not hardcoded here, so they can be rotated/overridden
            // per environment without a code change.
            var client = new ApiCredentials
            {
                ClientId = _configuration["ApiClient:ClientId"] ?? string.Empty,
                ClientSecret = _configuration["ApiClient:ClientSecret"] ?? string.Empty
            };

            if (string.IsNullOrEmpty(client.ClientId) ||
                string.IsNullOrEmpty(client.ClientSecret) ||
                !FixedTimeEquals(request.Username, client.ClientId) ||
                !FixedTimeEquals(request.Password, client.ClientSecret))
            {
                return Unauthorized();
            }

            // Signed with the service key, for the service audience - see JwtTokenService.
            var (token, expiresAt) = _jwtTokenService.CreateServiceToken(client.ClientId);

            return new LoginResponse
            {
                Token = token,
                Username = client.ClientId,
                ExpiresAt = expiresAt
            };
        }

        // Compares without leaking, through timing, how much of the value matched.
        private static bool FixedTimeEquals(string? a, string b) =>
            a != null && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
    }
} 