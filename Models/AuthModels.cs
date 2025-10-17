using System.Text.Json.Serialization;

namespace SimpleJWTApi.Models;

/// <summary>
/// Request model for client credentials authentication
/// This represents the credentials that a client application provides to authenticate
/// </summary>
public class ClientCredentialsRequest
{
    /// <summary>
    /// The grant type for OAuth 2.0 Client Credentials flow
    /// MUST be "client_credentials" according to RFC 6749 Section 4.4.2
    /// </summary>
    public string GrantType { get; set; } = "client_credentials";
    
    /// <summary>
    /// The client identifier - a unique identifier for the application
    /// This is typically a publicly known identifier that distinguishes different client applications
    /// </summary>
    public string ClientId { get; set; } = string.Empty;
    
    /// <summary>
    /// The client secret - a confidential key known only to the client application
    /// This acts as the "password" for the client application and should be kept secure
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional scope parameter to limit the access token's permissions
    /// In OAuth 2.0, scopes define what resources the client can access
    /// </summary>
    public string? Scope { get; set; }
}

/// <summary>
/// Response model containing the JWT access token
/// This is what gets returned to the client after successful authentication
/// </summary>
public class TokenResponse
{
    /// <summary>
    /// The JWT access token that can be used for subsequent API calls
    /// </summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Token type - typically "Bearer" for JWT tokens
    /// </summary>
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";
    
    /// <summary>
    /// Token expiration time in seconds
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
    
    /// <summary>
    /// The scope that was granted (if any)
    /// </summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}

/// <summary>
/// Configuration model for JWT settings from appsettings.json
/// </summary>
public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationInMinutes { get; set; }
}

/// <summary>
/// Configuration model for client credentials from appsettings.json
/// </summary>
public class ClientCredentials
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}