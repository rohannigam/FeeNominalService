using FeeNominalService;
using Destructurama;
using Serilog;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using FeeNominalService.Swagger;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using Amazon.SecretsManager;
using Amazon;
using FeeNominalService.Authentication;
using FeeNominalService.Services;
using FeeNominalService.Models;
using FeeNominalService.Models.Configuration;
using FeeNominalService.Settings;
using Microsoft.EntityFrameworkCore;
using FeeNominalService.Data;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using FeeNominalService.Repositories;
using FeeNominalService.Middleware;
using FeeNominalService.Services.AWS;
using FeeNominalService.Services.Adapters.InterPayments;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Logging;
using FeeNominalService.Utils;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Destructure.UsingAttributes()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// Add API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Configure Swagger/OpenAPI
builder.Services.AddSwaggerGen();
builder.Services.ConfigureOptions<SwaggerConfiguration>();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.MaxDepth = 64;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddMemoryCache();

// Add HttpClient factory
builder.Services.AddHttpClient();

// Configure API key settings
builder.Services.Configure<ApiKeySettings>(builder.Configuration.GetSection("ApiKeySettings"));
builder.Services.Configure<ApiKeyConfiguration>(builder.Configuration.GetSection("ApiKeyConfiguration"));

// Configure Surcharge Provider validation settings
builder.Services.Configure<SurchargeProviderValidationSettings>(builder.Configuration.GetSection("SurchargeProviderValidation"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<SurchargeProviderValidationSettings>>().Value);

// Add AWS Secrets Manager
builder.Services.AddAWSService<IAmazonSecretsManager>();

// Register services
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IAwsSecretsManagerService, FeeNominalService.Services.AWS.LocalApiKeySecretService>();
}
else
{
    builder.Services.AddScoped<IAwsSecretsManagerService, AwsSecretsManagerService>();
}

// Register repositories
builder.Services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
builder.Services.AddScoped<IMerchantRepository, MerchantRepository>();
builder.Services.AddScoped<IMerchantAuditTrailRepository, MerchantAuditTrailRepository>();
builder.Services.AddScoped<ISurchargeProviderRepository, SurchargeProviderRepository>();
builder.Services.AddScoped<ISurchargeProviderConfigRepository, SurchargeProviderConfigRepository>();
builder.Services.AddScoped<ISurchargeProviderConfigHistoryRepository, SurchargeProviderConfigHistoryRepository>();
builder.Services.AddScoped<IApiKeyUsageRepository, ApiKeyUsageRepository>();
builder.Services.AddScoped<ISurchargeTransactionRepository, SurchargeTransactionRepository>();
builder.Services.AddScoped<ISupportedProviderRepository, SupportedProviderRepository>();

// Register services
builder.Services.AddScoped<IMerchantService, MerchantService>();
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
builder.Services.AddScoped<IRequestSigningService, RequestSigningService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<ISurchargeProviderService, SurchargeProviderService>();
builder.Services.AddScoped<ISurchargeProviderConfigService, SurchargeProviderConfigService>();
builder.Services.AddScoped<ISurchargeTransactionService, SurchargeTransactionService>();
builder.Services.AddScoped<IApiKeyGenerator, ApiKeyGenerator>();
builder.Services.AddScoped<IProviderValidationService, ProviderValidationService>();
builder.Services.AddScoped<ICredentialValidationService, CredentialValidationService>();
builder.Services.AddHostedService<ApiKeyExpirationService>();

// Configure authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "ApiKey";
    options.DefaultChallengeScheme = "ApiKey";
    options.DefaultForbidScheme = "ApiKey";
})
.AddScheme<AuthenticationSchemeOptions, ApiKeyAuthHandler>("ApiKey", null);

// Add authorization
builder.Services.AddAuthorization(options =>
{
    // Policy for initial API key generation - only requires timestamp and nonce
    options.AddPolicy("InitialKeyGeneration", policy =>
        policy.RequireAssertion(context =>
        {
            var user = context.User;
            if (user == null) return false;

            // For initial key generation, we only need the IsInitialKeyGeneration claim
            return user.HasClaim(c => c.Type == "IsInitialKeyGeneration" && c.Value == "true");
        }));

    // Policy for API key access - requires MerchantId and allowed endpoints
    options.AddPolicy("ApiKeyAccess", policy =>
        policy.RequireAssertion(context =>
        {
            var httpContext = context.Resource as HttpContext;
            if (httpContext == null)
            {
                Log.Warning("Authorization failed: HttpContext is null");
                return false;
            }

            // Skip authorization for ping endpoint
            if (httpContext.Request.Path.StartsWithSegments("/api/v1/ping"))
            {
                Log.Debug("Skipping authorization for ping endpoint");
                return true;
            }

            var user = context.User;
            if (user == null)
            {
                Log.Warning("Authorization failed: User is null");
                return false;
            }

            Log.Debug("Authorization check for path: {Path}", httpContext.Request.Path);
            Log.Debug("User claims: {Claims}", string.Join(", ", user.Claims.Select(c => $"{c.Type}: {c.Value}")));

            var merchantId = user.FindFirst("MerchantId")?.Value;
            if (string.IsNullOrEmpty(merchantId))
            {
                Log.Warning("Authorization failed: No MerchantId claim found");
                return false;
            }

            // Check if the endpoint is allowed
            var allowedEndpoints = user.FindFirst("AllowedEndpoints")?.Value;
            if (!string.IsNullOrEmpty(allowedEndpoints))
            {
                var requestPath = httpContext.Request.Path.Value?.ToLower();
                if (string.IsNullOrEmpty(requestPath))
                {
                    Log.Warning("Authorization failed: Request path is null or empty");
                    return false;
                }

                var allowedPaths = allowedEndpoints.Split(',').Select(p => p.Trim().ToLower());
                
                Log.Debug("Checking endpoint access - Request path: {Path}, Allowed paths: {AllowedPaths}", 
                    requestPath, string.Join(", ", allowedPaths));

                if (!EndpointMatcher.IsEndpointAllowed(requestPath, allowedPaths))
                {
                    Log.Warning("Authorization failed: Endpoint {Path} not in allowed endpoints: {AllowedEndpoints}", 
                        requestPath, allowedEndpoints);
                    return false;
                }
            }

            Log.Debug("Authorization successful for merchant {MerchantId} and path {Path}", 
                merchantId, httpContext.Request.Path);
            Log.Debug("User claims: {Claims}", string.Join(", ", user.Claims.Select(c => $"{c.Type}: {c.Value}")));
            return true;
        }));
});

// Add PostgreSQL DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), 
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null)));

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Register provider adapters and factory for DI
builder.Services.AddSingleton<InterPaymentsAdapter>();
builder.Services.AddSingleton<ISurchargeProviderAdapterFactory, SurchargeProviderAdapterFactory>();

var app = builder.Build();

// Add middleware to handle ping endpoint before authentication
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/v1/ping"))
    {
        context.Response.StatusCode = 200;
        await context.Response.WriteAsync("pong");
        return;
    }
    await next();
});

// Add authentication middleware
app.UseAuthentication();

// Add authorization middleware
app.UseAuthorization();

// Add global exception handler
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An unhandled exception occurred");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { error = "An internal server error occurred" });
    }
});

// Seed the database in development mode
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            await DbSeeder.SeedDatabaseAsync(context);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json",
                $"FeeNominalService API {description.ApiVersion}");
        }
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Add request/response logging middleware
app.UseMiddleware<RequestResponseLoggingMiddleware>();

app.MapControllers();

app.Run();
