using Microsoft.IdentityModel.Tokens;
using SimpleJWTApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SimpleJWTApi.Services;

/// <summary>
/// Service responsible for JWT token generation and validation
/// This service handles the core JWT operations for authentication
/// </summary>
public class JwtService
{
    private readonly JwtSettings _jwtSettings;

    public JwtService(JwtSettings jwtSettings)
    {
        _jwtSettings = jwtSettings;
    }

    /// <summary>
    /// Generates a JWT token for a given client
    /// This method creates the actual JWT token that clients will use for authentication
    /// </summary>
    /// <param name="clientId">The client identifier that will be included in the token claims</param>
    /// <param name="scope">Optional scope that defines the permissions granted to this token</param>
    /// <returns>A JWT token string</returns>
    public string GenerateToken(string clientId, string? scope = null)
    {
        // Create the secret key used for signing the JWT token
        // This key must be kept secure and should match the key used for validation
        var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        
        // Create signing credentials using HMAC SHA256 algorithm
        var signingCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

        // Define the claims that will be included in the JWT token
        // Claims are statements about the subject (in this case, the client)
        var claims = new List<Claim>
        {
            // Subject claim - identifies who the token is about (the client)
            new Claim(JwtRegisteredClaimNames.Sub, clientId),
            
            // JWT ID - a unique identifier for this specific token
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            
            // Issued at - timestamp when the token was created
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            
            // Custom claim for client identification
            new Claim("client_id", clientId),
            
            // Custom claim for the token type (useful for distinguishing different types of tokens)
            new Claim("token_type", "access_token")
        };

        // Add scope claim if provided
        // Scope defines what resources this token can access
        if (!string.IsNullOrEmpty(scope))
        {
            claims.Add(new Claim("scope", scope));
        }

        // Create the JWT token descriptor with all the necessary information
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            // Set the subject (principal) with all the claims
            Subject = new ClaimsIdentity(claims),
            
            // Set when the token expires
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes),
            
            // Set the issuer (who created the token)
            Issuer = _jwtSettings.Issuer,
            
            // Set the audience (who the token is intended for)
            Audience = _jwtSettings.Audience,
            
            // Set the signing credentials
            SigningCredentials = signingCredentials
        };

        // Create the token handler and generate the actual JWT token
        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        
        // Convert the token to a string format
        return tokenHandler.WriteToken(securityToken);
    }

    /// <summary>
    /// Validates a JWT token and extracts the claims
    /// This method is used by the authentication middleware to validate incoming tokens
    /// </summary>
    /// <param name="token">The JWT token to validate</param>
    /// <returns>ClaimsPrincipal if valid, null if invalid</returns>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));

            // Set up validation parameters
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = secretKey,
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero // Remove default 5 minute clock skew
            };

            // Validate the token and extract claims
            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch
        {
            // Token validation failed
            return null;
        }
    }
}