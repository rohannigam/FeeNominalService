using FeeNominalService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FeeNominalService.Services;

public interface ISurchargeFeeService
{
    Task<string> CalculateSurchargeAsync(SurchargeRequest request);
    Task<List<string>> CalculateBatchSurchargesAsync(List<SurchargeRequest> requests);
} 