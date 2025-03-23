using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using mpesaIntegration.Models.Authentication;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;


namespace mpesaIntegration.Services
{
    /// <summary>
    /// Service for handling JWT token generation and validation
    /// </summary>
    /// 

    public interface IJwtService
    {
        /// <summary>
        /// Generates a JWT access token for the authenticated user
        /// </summary>
        /// <param name="user">User to generate token for</param>
        /// <returns>Token and expiration time</returns>
        /// 

        (string token, DateTime expiration) genereateJwtToken(User user);

        /// <summary>
        /// Generates a refresh token for maintaining persistent sessions
        /// </summary>
        /// <returns>Refresh token string</returns>
        /// 

        string GenerateRefreshToken();
        /// <summary>
        /// Gets principal from expired access token
        /// </summary>
        /// <param name="token">Expired JWT token</param>
        /// <returns>ClaimsPrincipal if token is valid but expired</returns>
        /// 
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);

    }

    /// <summary>
    /// Implementation of JWT token service
    /// </summary>
    /// 

    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Generates a JWT access token for the authenticated user
        /// </summary>
        /// 

        public (string token, DateTime expiration) genereateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"]);

            // Token expiration time (typically short-lived, e.g., 15-60 minutes)

            var expiration = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpirationInMinutes"]));

            var Claims = new List<Claim>
{
    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
    new Claim(ClaimTypes.Name, user.FullName),
    new Claim(ClaimTypes.Email, user.Email),
    new Claim(ClaimTypes.Role, user.Role.ToString()),
    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
};


            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(Claims),
                Expires = expiration,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), // Corrected class name
                    SecurityAlgorithms.HmacSha256Signature
                ),
                Issuer = _configuration["Jwt:Issuer"],
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return (tokenHandler.WriteToken(token), expiration);
        }

        /// <summary>
        /// Generates a cryptographically secure refresh token
        /// </summary>
        /// 

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return  Convert.ToBase64String(randomNumber);

        }
         /// <summary>
        /// Validates and extracts claims from an expired token
        /// </summary>
        /// 
        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"])),
                ValidateLifetime = false,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],

            };
               var tokenHandler = new JwtSecurityTokenHandler();
                   var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
            
            if (securityToken is not JwtSecurityToken jwtSecurityToken || 
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256Signature, 
                    StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }
            return principal;
        }


    }
}