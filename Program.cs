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
builder.Services.AddControllers();
builder.Services.AddMemoryCache();

// Add HttpClient factory
builder.Services.AddHttpClient();

// Configure API key settings
builder.Services.Configure<ApiKeySettings>(builder.Configuration.GetSection("ApiKeySettings"));
builder.Services.Configure<ApiKeyConfiguration>(builder.Configuration.GetSection("ApiKeyConfiguration"));

// Add AWS Secrets Manager
builder.Services.AddAWSService<IAmazonSecretsManager>();
builder.Services.AddScoped<IAwsSecretsManagerService>(sp =>
{
    var env = sp.GetRequiredService<IHostEnvironment>();
    if (env.IsDevelopment())
    {
        return new MockAwsSecretsManagerService(
            sp.GetRequiredService<ILogger<MockAwsSecretsManagerService>>(),
            sp.GetRequiredService<IOptions<ApiKeyConfiguration>>()
        );
    }
    return new AwsSecretsManagerService(
        sp.GetRequiredService<IAmazonSecretsManager>(),
        sp.GetRequiredService<ILogger<AwsSecretsManagerService>>(),
        sp.GetRequiredService<IOptions<ApiKeyConfiguration>>()
    );
});

// Register repositories
builder.Services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
builder.Services.AddScoped<IMerchantRepository, MerchantRepository>();

// Register services
builder.Services.AddScoped<IMerchantService, MerchantService>();
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
builder.Services.AddScoped<IRequestSigningService, RequestSigningService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<ISurchargeFeeService, SurchargeFeeService>();
builder.Services.AddScoped<IRefundService, RefundService>();
builder.Services.AddScoped<ISaleService, SaleService>();
builder.Services.AddScoped<IApiKeyGenerator, ApiKeyGenerator>();
builder.Services.AddHostedService<ApiKeyExpirationService>();

// Configure authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "ApiKey";
    options.DefaultChallengeScheme = "ApiKey";
})
.AddScheme<AuthenticationSchemeOptions, ApiKeyAuthHandler>("ApiKey", null);

// Add authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiKeyAccess", policy =>
        policy.RequireAssertion(context =>
        {
            var httpContext = context.Resource as HttpContext;
            if (httpContext == null)
            {
                Log.Warning("Authorization failed: HttpContext is null");
                return false;
            }

            var user = context.User;
            if (user == null)
            {
                Log.Warning("Authorization failed: User is null");
                return false;
            }

            var merchantId = user.FindFirst("MerchantId")?.Value;
            if (string.IsNullOrEmpty(merchantId))
            {
                Log.Warning("Authorization failed: No MerchantId claim found");
                return false;
            }

            // Check if the user has the Admin role
            var isAdmin = user.IsInRole("Admin");
            if (!isAdmin)
            {
                Log.Warning("Authorization failed: User does not have Admin role");
                return false;
            }

            // Check if the endpoint is allowed
            var allowedEndpoints = user.FindFirst("AllowedEndpoints")?.Value;
            if (!string.IsNullOrEmpty(allowedEndpoints))
            {
                var requestPath = httpContext.Request.Path.Value?.ToLower();
                var allowedPaths = allowedEndpoints.Split(',').Select(p => p.Trim().ToLower());
                
                if (!allowedPaths.Any(p => requestPath?.StartsWith(p) == true))
                {
                    Log.Warning("Authorization failed: Endpoint {Path} not in allowed endpoints: {AllowedEndpoints}", 
                        requestPath, allowedEndpoints);
                    return false;
                }
            }

            // Log the request path and claims for debugging
            Log.Information("Request path: {Path}", httpContext.Request.Path);
            Log.Information("Authorization successful for merchant {MerchantId}", merchantId);
            Log.Information("User claims: {Claims}", string.Join(", ", user.Claims.Select(c => $"{c.Type}: {c.Value}")));
            
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

var app = builder.Build();

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

Log.Information("Adding authentication middleware");
app.UseAuthentication();
Log.Information("Adding authorization middleware");
app.UseAuthorization();

app.MapControllers();

app.Run();
