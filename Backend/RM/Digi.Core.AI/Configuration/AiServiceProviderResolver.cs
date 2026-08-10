using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Digi.Core.AI.Providers;

namespace Digi.Core.AI.Configuration
{
    public interface IAIServiceProviderResolver
    {
        IAIServiceProvider Resolve(string providerKey);
    }

    public sealed class AiServiceProviderResolver : IAIServiceProviderResolver
    {
        private readonly IServiceProvider _serviceProvider;

        public AiServiceProviderResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IAIServiceProvider Resolve(string providerKey)
        {
            if (string.IsNullOrWhiteSpace(providerKey))
            {
                return _serviceProvider.GetRequiredService<MultinetAiProvider>();
            }

            return providerKey.ToLowerInvariant() switch
            {
                MultinetAiConstants.Name => _serviceProvider.GetRequiredService<MultinetAiProvider>(),
                "openai" => _serviceProvider.GetRequiredService<OpenAiProvider>(),
                "anthropic" => _serviceProvider.GetRequiredService<AnthropicProvider>(),
                "gemini" => _serviceProvider.GetRequiredService<GoogleGeminiProvider>(),
                "custom" => _serviceProvider.GetRequiredService<CustomAiProvider>(),
                _ => throw new NotSupportedException($"AI Provider '{providerKey}' is not supported.")
            };
        }
    }
}
