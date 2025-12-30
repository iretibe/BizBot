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
                new Claim("plan", tenant.Plan ?? "starter"),
                new Claim(
                    JwtRegisteredClaimNames.Iat,
                    DateTimeOffset.UtcNow
                        .ToUnixTimeSeconds()
                        .ToString()
                )
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _config["Widget:SigningKey"]!));

            var token = new JwtSecurityToken(
                issuer: "bizbot",
                audience: "bizbot-widget",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }
    }
}
