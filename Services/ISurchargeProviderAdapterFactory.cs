namespace FeeNominalService.Services;

using FeeNominalService.Models.SurchargeProvider;

public interface ISurchargeProviderAdapterFactory
{
    ISurchargeProviderAdapter GetAdapter(SurchargeProviderConfig config);
} 