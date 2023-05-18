using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

var authority = config.GetRequiredSection("Auth0:Domain").Value;
var audience = config.GetRequiredSection("Auth0:Audience").Value;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.Authority = authority;
    options.Audience = audience;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
        {
            var issuerSigningKeys = GetIssuerSigningKeys().GetAwaiter().GetResult();
            return issuerSigningKeys;
        }
    };
});

// Retrieve the public keys for token verification
async Task<IEnumerable<SecurityKey>> GetIssuerSigningKeys()
{
    // Fetch the JSON Web Key Set (JWKS) from Auth0
    var jwksUri = "https://dev-4mabt7u0jnmm1g7r.us.auth0.com/.well-known/jwks.json";
    using (var httpClient = new HttpClient())
    {
        var jwksResponse = await httpClient.GetAsync(jwksUri);
        if (jwksResponse.IsSuccessStatusCode)
        {
            var jwksJson = await jwksResponse.Content.ReadAsStringAsync();

            // Parse the JWKS to obtain the issuer signing keys
            var jwks = new JsonWebKeySet(jwksJson);
            return jwks.GetSigningKeys();
        }
    }

    return null; // Failed to retrieve JWKS or no signing keys found
}

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MyProject", Version = "v1.0.0" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Description = "Using the Authorization header with the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    };

    c.AddSecurityDefinition("Bearer", securityScheme);

    var securityRequirement = new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            } },
            new string[] { }
        }
    };

    c.AddSecurityRequirement(securityRequirement);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
