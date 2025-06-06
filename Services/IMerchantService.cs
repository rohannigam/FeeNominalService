using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FeeNominalService.Models.Merchant;
using FeeNominalService.Models.ApiKey.Requests;
using FeeNominalService.Models.Merchant.Responses;
using FeeNominalService.Models.ApiKey;

namespace FeeNominalService.Services
{
    public interface IMerchantService
    {
        Task<MerchantResponse> CreateMerchantAsync(GenerateInitialApiKeyRequest request, string createdBy);
        Task<Merchant?> GetMerchantAsync(Guid id);
        Task<Merchant> UpdateMerchantStatusAsync(Guid id, int statusId);
        Task<Merchant?> GetByExternalMerchantIdAsync(string externalMerchantId);
        Task<Merchant?> GetByExternalMerchantGuidAsync(Guid externalMerchantGuid);
        Task<IEnumerable<MerchantAuditTrail>> GetMerchantAuditTrailAsync(Guid merchantId);
        Task<Merchant> UpdateMerchantAsync(Guid merchantId, string name, string updatedBy);
        Task<IEnumerable<Merchant>> GetAllMerchantsAsync();
        Task<bool> IsMerchantActiveAsync(Guid id);
        /// <summary>
        /// Generates a new API key for a merchant
        /// </summary>
        Task<ApiKeyInfo> GenerateApiKeyAsync(Guid merchantId, GenerateApiKeyRequest request, OnboardingMetadata? onboardingMetadata);
        /// <summary>
        /// Creates an audit trail entry for a merchant action
        /// </summary>
        Task CreateAuditTrailAsync(
            Guid merchantId,
            string action,
            string entityType,
            string? oldValue,
            string? newValue,
            string performedBy
        );
    }
} 