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
                        Description = "API for managing surcharge fees and API keys",
                        Contact = new OpenApiContact
                        {
                            Name = "API Support",
                            Email = "support@feenominal.com"
                        }
                    });
            }

            // Add security definitions
            options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Description = "API Key Authentication",
                Name = "X-Merchant-ID",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "ApiKey"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "ApiKey"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Add XML comments if the file exists
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        }
    }
} 