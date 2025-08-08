using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace FeeNominalService.Swagger
{
    /// <summary>
    /// Configures Swagger generation options
    /// </summary>
    public class SwaggerConfiguration : IConfigureOptions<SwaggerGenOptions>
    {
        private readonly IApiVersionDescriptionProvider _provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="SwaggerConfiguration"/> class.
        /// </summary>
        /// <param name="provider">The API version descriptor provider used to generate documents.</param>
        public SwaggerConfiguration(IApiVersionDescriptionProvider provider) => _provider = provider;

        /// <inheritdoc />
        public void Configure(SwaggerGenOptions options)
        {
            // Add a swagger document for each discovered API version
            foreach (var description in _provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(
                    description.GroupName,
                    new OpenApiInfo
                    {
                        Title = $"FeeNominalSurcharge API {description.ApiVersion}",
                        Version = description.ApiVersion.ToString(),
                        Description = @"API for managing surcharge fees and API keys.
                        
                        ## API Key Management
                        - Initial API key generation requires merchant details and onboarding metadata
                        - Additional API keys can be generated with proper authentication
                        - API keys can be rotated, updated, and revoked
                        - All API keys support rate limiting and endpoint restrictions
                        - Usage tracking and monitoring is available
                        
                        ## Authentication
                        All API requests (except initial API key generation) require:
                        - API Key authentication
                        - Request signing with HMAC-SHA256
                        - Timestamp and nonce for replay protection
                        
                        ## Rate Limiting
                        - Each API key has configurable rate limits
                        - Usage is tracked per endpoint
                        - IP address tracking is enabled
                        
                        ## Security Features
                        - API key rotation support
                        - Automatic expiration
                        - Revocation with audit trail
                        - Secret storage in AWS Secrets Manager
                        - Usage tracking and monitoring",
                        Contact = new OpenApiContact
                        {
                            Name = "API Support",
                            Email = "support@feenominal.com"
                        }
                    });
            }

            // Add security definitions for each required header
            options.AddSecurityDefinition("X-Merchant-ID", new OpenApiSecurityScheme
            {
                Description = "Your merchant ID (required for all authenticated requests)",
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = "X-Merchant-ID"
            });
            options.AddSecurityDefinition("X-API-Key", new OpenApiSecurityScheme
            {
                Description = "Your API key (required for all authenticated requests)",
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = "X-API-Key"
            });
            options.AddSecurityDefinition("X-Timestamp", new OpenApiSecurityScheme
            {
                Description = "Current UTC time in ISO 8601 format (required for request signing)",
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = "X-Timestamp"
            });
            options.AddSecurityDefinition("X-Nonce", new OpenApiSecurityScheme
            {
                Description = "Unique random string for each request (required for request signing)",
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = "X-Nonce"
            });
            options.AddSecurityDefinition("X-Signature", new OpenApiSecurityScheme
            {
                Description = "HMAC-SHA256 signature of (timestamp|nonce|merchantId|apiKey), base64 encoded (required)",
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = "X-Signature"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "X-Merchant-ID" } }, new string[] { }
                },
                {
                    new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "X-API-Key" } }, new string[] { }
                },
                {
                    new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "X-Timestamp" } }, new string[] { }
                },
                {
                    new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "X-Nonce" } }, new string[] { }
                },
                {
                    new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "X-Signature" } }, new string[] { }
                }
            });

            // Add XML comments if the file exists
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Add operation filters for better documentation
            options.OperationFilter<SwaggerDefaultValues>();
            options.OperationFilter<SwaggerApiKeyOperationFilter>();
        }
    }
} 