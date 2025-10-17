using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SimpleJWTApi.Data;
using SimpleJWTApi.Models;
using SimpleJWTApi.Services;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";

if (!builder.Environment.IsDevelopment())
{
    // In Azure, use the default port (80/443)
    builder.WebHost.UseUrls();
}


// Add services to the container.
builder.Services.AddOpenApi();

// Add Swagger services with JWT Authentication support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Simple JWT API", 
        Version = "v1",
        Description = "A simple API with JWT authentication and CRUD operations for Products",
        Contact = new OpenApiContact
        {
            Name = "API Support",
            Email = "support@example.com"
        }
    });

    // Define the JWT Bearer security scheme
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\""
    });

    // Add the security requirement for endpoints that need authentication
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure JWT Settings from appsettings.json
// HOW WE GET JWT CONFIGURATION:
// The JWT settings are loaded from appsettings.json configuration
// This includes the secret key, issuer, audience, and expiration time
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
builder.Services.AddSingleton(jwtSettings);

// Configure Client Credentials from appsettings.json
// HOW WE GET CLIENT CREDENTIALS CONFIGURATION:
// Client ID and Client Secret are loaded from the configuration
// In production, these should be stored securely (Azure Key Vault, environment variables, etc.)
var clientCredentials = builder.Configuration.GetSection("ClientCredentials").Get<ClientCredentials>()!;
builder.Services.AddSingleton(clientCredentials);

// Register our custom services
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<AuthenticationService>();

// Add Entity Framework with In-Memory database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("SimpleJWTApiDb"));

// Configure JWT Authentication
// HOW JWT AUTHENTICATION WORKS:
// 1. When a request comes in with an Authorization header, the middleware extracts the token
// 2. The token is validated using the same secret key used to sign it
// 3. If valid, claims are extracted and a ClaimsPrincipal is created
// 4. The ClaimsPrincipal becomes available through HttpContext.User
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Add authorization services
builder.Services.AddAuthorization();

var app = builder.Build();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.

    app.MapOpenApi();
    
    // Enable Swagger UI
    app.UseSwagger();
   app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Simple JWT API v1");
    options.RoutePrefix = string.Empty; // Serve Swagger UI at root
    options.DocumentTitle = "Simple JWT API Documentation";
    options.DefaultModelsExpandDepth(-1);
    options.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Model);
});




// Enable authentication and authorization middleware
// ORDER IS IMPORTANT: Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

// =============================================================================
// AUTHENTICATION ENDPOINTS
// =============================================================================

/// <summary>
/// OAuth 2.0 Client Credentials Grant endpoint
/// This endpoint allows clients to authenticate and receive a JWT token
/// </summary>
app.MapPost("/auth/token", (ClientCredentialsRequest request, AuthenticationService authService) =>
{
    // HOW THE AUTHENTICATION PROCESS WORKS:
    // 1. Client sends POST request with client_id and client_secret
    // 2. AuthenticationService validates these credentials
    // 3. If valid, a JWT token is generated with appropriate claims
    // 4. Token is returned to the client for use in subsequent requests
    
    var tokenResponse = authService.AuthenticateClient(request);
    
    if (tokenResponse == null)
    {
        return Results.Unauthorized();
    }
    
    return Results.Ok(tokenResponse);
})
.WithName("GetAccessToken")
.WithSummary("Get JWT access token using client credentials")
.WithDescription("Authenticate using client_id and client_secret to receive a JWT access token");

/// <summary>
/// Endpoint to get information about the current authenticated user/client
/// This demonstrates how to access claims from the JWT token
/// </summary>
app.MapGet("/auth/me", (ClaimsPrincipal user) =>
{
    // HOW WE GET CLAIMS PRINCIPAL:
    // 1. The JWT middleware automatically validates the token from the Authorization header
    // 2. If valid, it creates a ClaimsPrincipal object with all the claims from the token
    // 3. This ClaimsPrincipal is available as HttpContext.User or can be injected directly
    // 4. We can access individual claims using user.FindFirst() or user.Claims
    
    if (!user.Identity?.IsAuthenticated ?? false)
    {
        return Results.Unauthorized();
    }

    // Extract claims from the authenticated user
    // These claims were embedded in the JWT token when it was created
    var clientId = user.FindFirst("client_id")?.Value;
    var subject = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var jwtId = user.FindFirst("jti")?.Value;
    var scope = user.FindFirst("scope")?.Value;
    var tokenType = user.FindFirst("token_type")?.Value;
    
    // Get all claims for debugging purposes
    var allClaims = user.Claims.Select(c => new { c.Type, c.Value }).ToList();

    return Results.Ok(new
    {
        IsAuthenticated = user.Identity?.IsAuthenticated ?? false,
        ClientId = clientId,
        Subject = subject,
        JwtId = jwtId,
        Scope = scope,
        TokenType = tokenType,
        AllClaims = allClaims,
        Message = "Successfully authenticated! This endpoint shows how to access claims from the JWT token."
    });
})
.RequireAuthorization()  // This endpoint requires authentication
.WithName("GetCurrentUser")
.WithSummary("Get current authenticated client information")
.WithDescription("Returns information about the currently authenticated client extracted from JWT claims");

// =============================================================================
// CRUD ENDPOINTS FOR PRODUCTS
// =============================================================================

/// <summary>
/// Get all products
/// </summary>
app.MapGet("/api/products", async (AppDbContext db) =>
{
    var products = await db.Products.ToListAsync();
    return Results.Ok(products);
})
.RequireAuthorization()  // Requires JWT authentication
.WithName("GetProducts")
.WithSummary("Get all products")
.WithDescription("Retrieve a list of all products (requires JWT authentication)");

/// <summary>
/// Get a specific product by ID
/// </summary>
app.MapGet("/api/products/{id:int}", async (int id, AppDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    
    if (product == null)
        return Results.NotFound($"Product with ID {id} not found");
    
    return Results.Ok(product);
})
.RequireAuthorization()
.WithName("GetProduct")
.WithSummary("Get product by ID")
.WithDescription("Retrieve a specific product by its ID (requires JWT authentication)");

/// <summary>
/// Create a new product
/// </summary>
app.MapPost("/api/products", async (ProductCreateDto createDto, AppDbContext db, ClaimsPrincipal user) =>
{
    // Demonstrate how to access claims in CRUD operations
    var clientId = user.FindFirst("client_id")?.Value;
    
    var product = new Product
    {
        Name = createDto.Name,
        Description = createDto.Description,
        Price = createDto.Price,
        CategoryId = createDto.CategoryId,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    db.Products.Add(product);
    await db.SaveChangesAsync();

    return Results.Created($"/api/products/{product.Id}", new 
    { 
        Product = product,
        CreatedBy = clientId,  // Show which client created this product
        Message = "Product created successfully"
    });
})
.RequireAuthorization()
.WithName("CreateProduct")
.WithSummary("Create a new product")
.WithDescription("Create a new product (requires JWT authentication)");

/// <summary>
/// Update an existing product
/// </summary>
app.MapPut("/api/products/{id:int}", async (int id, ProductUpdateDto updateDto, AppDbContext db, ClaimsPrincipal user) =>
{
    var product = await db.Products.FindAsync(id);
    
    if (product == null)
        return Results.NotFound($"Product with ID {id} not found");

    // Update only provided fields
    if (updateDto.Name != null)
        product.Name = updateDto.Name;
    if (updateDto.Description != null)
        product.Description = updateDto.Description;
    if (updateDto.Price.HasValue)
        product.Price = updateDto.Price.Value;
    if (updateDto.CategoryId.HasValue)
        product.CategoryId = updateDto.CategoryId.Value;
    
    product.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();

    var clientId = user.FindFirst("client_id")?.Value;
    
    return Results.Ok(new 
    { 
        Product = product,
        UpdatedBy = clientId,  // Show which client updated this product
        Message = "Product updated successfully"
    });
})
.RequireAuthorization()
.WithName("UpdateProduct")
.WithSummary("Update an existing product")
.WithDescription("Update an existing product (requires JWT authentication)");

/// <summary>
/// Delete a product
/// </summary>
app.MapDelete("/api/products/{id:int}", async (int id, AppDbContext db, ClaimsPrincipal user) =>
{
    var product = await db.Products.FindAsync(id);
    
    if (product == null)
        return Results.NotFound($"Product with ID {id} not found");

    db.Products.Remove(product);
    await db.SaveChangesAsync();

    var clientId = user.FindFirst("client_id")?.Value;
    
    return Results.Ok(new 
    { 
        Message = "Product deleted successfully",
        DeletedBy = clientId,  // Show which client deleted this product
        ProductId = id
    });
})
.RequireAuthorization()
.WithName("DeleteProduct")
.WithSummary("Delete a product")
.WithDescription("Delete a product by ID (requires JWT authentication)");

// =============================================================================
// PUBLIC ENDPOINTS (No authentication required)
// =============================================================================

/// <summary>
/// Public endpoint that doesn't require authentication
/// </summary>
app.MapGet("/api/public/health", () =>
{
    return Results.Ok(new 
    { 
        Status = "Healthy",
        Timestamp = DateTime.UtcNow,
        Message = "This is a public endpoint that doesn't require authentication"
    });
})
.WithName("HealthCheck")
.WithSummary("Health check endpoint")
.WithDescription("Public health check endpoint (no authentication required)");

app.Run();

