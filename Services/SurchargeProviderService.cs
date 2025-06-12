using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FeeNominalService.Models.SurchargeProvider;
using FeeNominalService.Repositories;
using Microsoft.Extensions.Logging;
using Json.Schema;

namespace FeeNominalService.Services
{
    public class SurchargeProviderService : ISurchargeProviderService
    {
        private readonly ISurchargeProviderRepository _repository;
        private readonly ILogger<SurchargeProviderService> _logger;

        public SurchargeProviderService(
            ISurchargeProviderRepository repository,
            ILogger<SurchargeProviderService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SurchargeProvider?> GetByIdAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Getting provider by ID {ProviderId}", id);
                return await _repository.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting provider by ID {ProviderId}", id);
                throw;
            }
        }

        public async Task<SurchargeProvider?> GetByCodeAsync(string code)
        {
            try
            {
                _logger.LogInformation("Getting provider by code {ProviderCode}", code);
                return await _repository.GetByCodeAsync(code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting provider by code {ProviderCode}", code);
                throw;
            }
        }

        public async Task<IEnumerable<SurchargeProvider>> GetAllAsync()
        {
            try
            {
                _logger.LogInformation("Getting all providers");
                return await _repository.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all providers");
                throw;
            }
        }

        public async Task<IEnumerable<SurchargeProvider>> GetActiveAsync()
        {
            try
            {
                _logger.LogInformation("Getting active providers");
                return await _repository.GetActiveAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active providers");
                throw;
            }
        }

        public async Task<SurchargeProvider> CreateAsync(SurchargeProvider provider)
        {
            try
            {
                _logger.LogInformation("Creating provider {ProviderName}", provider.Name);

                // Validate provider code uniqueness
                if (await _repository.ExistsByCodeAsync(provider.Code))
                {
                    throw new InvalidOperationException($"Provider with code {provider.Code} already exists");
                }

                // Set timestamps
                provider.CreatedAt = DateTime.UtcNow;
                provider.UpdatedAt = DateTime.UtcNow;

                // Get the status ID from the code
                var status = await _repository.GetStatusByCodeAsync("ACTIVE");
                if (status == null)
                {
                    throw new InvalidOperationException("ACTIVE status not found in the database");
                }
                provider.StatusId = status.StatusId;
                provider.Status = status;

                return await _repository.AddAsync(provider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating provider {ProviderName}", provider.Name);
                throw;
            }
        }

        public async Task<SurchargeProvider> UpdateAsync(SurchargeProvider provider)
        {
            try
            {
                _logger.LogInformation("Updating provider {ProviderId}", provider.Id);

                // Check if provider exists
                var existingProvider = await _repository.GetByIdAsync(provider.Id);
                if (existingProvider == null)
                {
                    throw new KeyNotFoundException($"Provider with ID {provider.Id} not found");
                }

                // Validate provider code uniqueness if changed
                if (provider.Code != existingProvider.Code && await _repository.ExistsByCodeAsync(provider.Code))
                {
                    throw new InvalidOperationException($"Provider with code {provider.Code} already exists");
                }

                // Update timestamp
                provider.UpdatedAt = DateTime.UtcNow;

                return await _repository.UpdateAsync(provider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating provider {ProviderId}", provider.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Deleting provider {ProviderId}", id);

                // Check if provider exists
                if (!await _repository.ExistsAsync(id))
                {
                    return false;
                }

                return await _repository.DeleteAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting provider {ProviderId}", id);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            try
            {
                return await _repository.ExistsAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking provider existence {ProviderId}", id);
                throw;
            }
        }

        public async Task<bool> ExistsByCodeAsync(string code)
        {
            try
            {
                return await _repository.ExistsByCodeAsync(code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking provider existence by code {ProviderCode}", code);
                throw;
            }
        }

        public async Task<bool> ValidateCredentialsSchemaAsync(Guid providerId, JsonDocument credentials)
        {
            try
            {
                _logger.LogInformation("Validating credentials schema for provider {ProviderId}", providerId);

                var provider = await _repository.GetByIdAsync(providerId);
                if (provider == null)
                {
                    throw new KeyNotFoundException($"Provider with ID {providerId} not found");
                }

                if (string.IsNullOrEmpty(provider.CredentialsSchema.RootElement.GetRawText()))
                {
                    return true; // No schema defined, consider valid
                }

                // Parse the schema
                var schema = JsonSchema.FromText(provider.CredentialsSchema.RootElement.GetRawText());
                if (schema == null)
                {
                    _logger.LogError("Invalid schema format for provider {ProviderId}", providerId);
                    return false;
                }

                // Validate credentials against schema
                var validationResult = schema.Evaluate(credentials.RootElement);
                
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning(
                        "Credentials validation failed for provider {ProviderId}. Details: {Details}",
                        providerId,
                        string.Join(", ", validationResult.Details.Select(d => d.ToString()))
                    );
                    return false;
                }

                _logger.LogInformation("Credentials validation successful for provider {ProviderId}", providerId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating credentials schema for provider {ProviderId}", providerId);
                throw;
            }
        }

        // Helper method to generate a schema for a provider
        public static string GenerateCredentialsSchema(string providerType)
        {
            try
            {
                string schemaJson = providerType.ToLower() switch
                {
                    "basic" => @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""username"": { ""type"": ""string"", ""minLength"": 1 },
                            ""password"": { ""type"": ""string"", ""minLength"": 1 }
                        },
                        ""required"": [""username"", ""password""]
                    }",

                    "oauth2" => @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""client_id"": { ""type"": ""string"", ""minLength"": 1 },
                            ""client_secret"": { ""type"": ""string"", ""minLength"": 1 },
                            ""token_url"": { ""type"": ""string"", ""format"": ""uri"" }
                        },
                        ""required"": [""client_id"", ""client_secret"", ""token_url""]
                    }",

                    "api_key" => @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""api_key"": { ""type"": ""string"", ""minLength"": 1 },
                            ""api_key_header"": { ""type"": ""string"", ""minLength"": 1 }
                        },
                        ""required"": [""api_key"", ""api_key_header""]
                    }",

                    "custom" => @"{
                        ""type"": ""object"",
                        ""additionalProperties"": true
                    }",

                    _ => throw new ArgumentException($"Unsupported provider type: {providerType}")
                };

                return schemaJson;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error generating schema for provider type {providerType}", ex);
            }
        }

        public async Task<SurchargeProviderStatus?> GetStatusByCodeAsync(string code)
        {
            try
            {
                _logger.LogInformation("Getting status by code {StatusCode}", code);
                return await _repository.GetStatusByCodeAsync(code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting status by code {StatusCode}", code);
                throw;
            }
        }
    }
} 