using FeeNominalService.Models.SurchargeProvider;
using FeeNominalService.Services.Adapters.InterPayments;
using System;
using System.Collections.Generic;

namespace FeeNominalService.Services;

public class SurchargeProviderAdapterFactory : ISurchargeProviderAdapterFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, ISurchargeProviderAdapter> _adapters;

    public SurchargeProviderAdapterFactory(IServiceProvider serviceProvider, InterPaymentsAdapter interPaymentsAdapter)
    {
        _serviceProvider = serviceProvider;
        // Map provider code (or type) to adapter. Add more as needed.
        _adapters = new(StringComparer.OrdinalIgnoreCase)
        {
            { "INTERPAYMENTS", interPaymentsAdapter },
            { "INTERPAYMENTS_TEST_001", interPaymentsAdapter },
            { "INTERPAYMENTS_PROD_001", interPaymentsAdapter }
            // Add more mappings for other providers here
        };
    }

    public ISurchargeProviderAdapter GetAdapter(SurchargeProviderConfig config)
    {
        if (config.Provider == null)
            throw new ArgumentNullException(nameof(config.Provider), "Provider cannot be null in provider config");
        var type = config.Provider.ProviderType;
        if (_adapters.TryGetValue(type, out var adapter))
            return adapter;
        throw new NotSupportedException($"No adapter registered for provider type: {type}");
    }
} 