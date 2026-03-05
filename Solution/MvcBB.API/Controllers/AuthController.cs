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
        private readonly Dictionary<string, ApiCredentials> _validClients;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
            
            // In a real application, these would come from a database or configuration
            _validClients = new Dictionary<string, ApiCredentials>
            {
                {
                    "mvc_client",
                    new ApiCredentials
                    {
                        ClientId = "mvc_client",
                        ClientSecret = "your_secure_secret_here" // In production, use a secure secret
                    }
                }
            };
        }

        [HttpPost("token")]
        public ActionResult<LoginResponse> GetToken(LoginRequest request)
        {
            // Validate client credentials
            if (!_validClients.TryGetValue(request.Username, out var client) ||
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