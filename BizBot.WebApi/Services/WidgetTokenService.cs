using BizBot.WebApi.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BizBot.WebApi.Services
{
    public class WidgetTokenService
    {
        private readonly IConfiguration _config;

        public WidgetTokenService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateWidgetToken(TenantConfig tenant)
        {
            var claims = new[]
            {
                new Claim("tid", tenant.Id),
                new Claim("plan", tenant.Plan!)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Widget:SigningKey"]!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "bizbot",
                audience: "bizbot-widget",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(12),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
