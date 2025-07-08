using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FeeNominalService.Swagger
{
    /// <summary>
    /// Operation filter to add API key authentication requirements to Swagger documentation
    /// </summary>
    public class SwaggerApiKeyOperationFilter : IOperationFilter
    {
        /// <summary>
        /// Applies the filter to the specified operation using the given context.
        /// </summary>
        /// <param name="operation">The operation to apply the filter to.</param>
        /// <param name="context">The current operation filter context.</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Skip authentication for initial API key generation
            if (context.MethodInfo.Name.Contains("InitialGenerate"))
            {
                return;
            }

            // Add security requirement for all other operations
            operation.Security = new List<OpenApiSecurityRequirement>
            {
                new OpenApiSecurityRequirement
                {
                    { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "X-Merchant-ID" } }, new string[] { } },
                    { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "X-API-Key" } }, new string[] { } },
                    { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "X-Timestamp" } }, new string[] { } },
                    { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "X-Nonce" } }, new string[] { } },
                    { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "X-Signature" } }, new string[] { } }
                }
            };

            // Document required headers for secured endpoints
            operation.Parameters ??= new List<OpenApiParameter>();
            var requiredHeaders = new[] { "X-Merchant-ID", "X-API-Key", "X-Timestamp", "X-Nonce", "X-Signature" };
            foreach (var header in requiredHeaders)
            {
                if (!operation.Parameters.Any(p => p.Name == header))
                {
                    operation.Parameters.Add(new OpenApiParameter
                    {
                        Name = header,
                        In = ParameterLocation.Header,
                        Required = true,
                        Schema = new OpenApiSchema { Type = "string" },
                        Description = $"{header} (required)"
                    });
                }
            }

            // Add request body schema for operations that need it
            if (context.MethodInfo.Name.Contains("Update") || 
                context.MethodInfo.Name.Contains("Generate") ||
                context.MethodInfo.Name.Contains("Rotate"))
            {
                operation.RequestBody = new OpenApiRequestBody
                {
                    Required = true,
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Schema = context.SchemaGenerator.GenerateSchema(
                                context.MethodInfo.GetParameters()
                                    .FirstOrDefault(p => p.ParameterType.Name.Contains("Request"))?.ParameterType
                                    ?? typeof(object),
                                context.SchemaRepository)
                        }
                    }
                };
            }
        }
    }
} 