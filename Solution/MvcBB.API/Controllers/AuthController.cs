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

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

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
                request.Username != client.ClientId ||
                client.ClientSecret != request.Password)
            {
                return Unauthorized();
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:SecretKey"]);
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, client.ClientId),
                    new Claim(ClaimTypes.Role, "ApiClient")
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return new LoginResponse
            {
                Token = tokenHandler.WriteToken(token),
                Username = client.ClientId,
                ExpiresAt = tokenDescriptor.Expires.Value
            };
        }
    }
} 