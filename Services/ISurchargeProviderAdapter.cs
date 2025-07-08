namespace FeeNominalService.Services;

using FeeNominalService.Models.Surcharge.Requests;
using FeeNominalService.Models.Surcharge.Responses;
using System.Text.Json;

public interface ISurchargeProviderAdapter
{
    (bool IsValid, string? ErrorMessage) ValidateRequest(SurchargeAuthRequest request);
    Task<SurchargeAuthResponse> CalculateSurchargeAsync(SurchargeAuthRequest request, JsonDocument credentials);
} 