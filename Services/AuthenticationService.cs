using SimpleJWTApi.Models;

namespace SimpleJWTApi.Services;

/// <summary>
/// Service responsible for handling client authentication
/// This service validates client credentials and orchestrates token generation
/// </summary>
public class AuthenticationService
{
    private readonly JwtService _jwtService;
    private readonly ClientCredentials _clientCredentials;

    public AuthenticationService(JwtService jwtService, ClientCredentials clientCredentials)
    {
        _jwtService = jwtService;
        _clientCredentials = clientCredentials;
    }

    /// <summary>
    /// Authenticates a client using client credentials flow
    /// This method implements the OAuth 2.0 Client Credentials Grant flow
    /// </summary>
    /// <param name="request">The client credentials request containing client_id and client_secret</param>
    /// <returns>Token response if authentication successful, null if failed</returns>
    public TokenResponse? AuthenticateClient(ClientCredentialsRequest request)
    {
        // HOW WE GET CLIENT ID AND CLIENT SECRET:
        // 1. Client ID and Client Secret are obtained from the request body
        // 2. These are compared against configured values in appsettings.json
        // 3. In a real application, these would typically be stored in a database
        //    with the client_secret being hashed (like a password)
        
        // Validate the client credentials against our configured values
        // In production, this would typically involve:
        // - Looking up the client in a database
        // - Verifying the client_secret hash
        // - Checking if the client is active/enabled
        // - Validating any requested scopes
        if (!IsValidClient(request.ClientId, request.ClientSecret))
        {
            return null; // Authentication failed
        }

        // Generate JWT token for the authenticated client
        // The token will contain claims that identify the client and any granted permissions
        var token = _jwtService.GenerateToken(request.ClientId, request.Scope);

        // Return the token response
        return new TokenResponse
        {
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresIn = 3600, // 1 hour in seconds
            Scope = request.Scope
        };
    }

    /// <summary>
    /// Validates client credentials against configured values
    /// In a production environment, this would involve database lookup and secure comparison
    /// </summary>
    /// <param name="clientId">The client identifier to validate</param>
    /// <param name="clientSecret">The client secret to validate</param>
    /// <returns>True if credentials are valid, false otherwise</returns>
    private bool IsValidClient(string clientId, string clientSecret)
    {
        // HOW WE VALIDATE CLIENT CREDENTIALS:
        // 1. Compare the provided client_id with our configured client_id
        // 2. Compare the provided client_secret with our configured client_secret
        // 3. In production, the client_secret should be hashed and compared securely
        
        // Simple validation against configured values
        // In a real application, you would:
        // - Query a database for the client record
        // - Use secure hash comparison for the secret
        // - Check client status (active/inactive)
        // - Validate client permissions/scopes
        return clientId == _clientCredentials.ClientId && 
               clientSecret == _clientCredentials.ClientSecret;
    }
}