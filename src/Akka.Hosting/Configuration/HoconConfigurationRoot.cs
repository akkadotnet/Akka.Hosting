// -----------------------------------------------------------------------
//   <copyright file="HoconConfigurationRoot.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Primitives;

namespace Akka.Hosting.Configuration;

internal class HoconConfigurationRoot: IConfigurationRoot
{
    private readonly IList<IConfigurationProvider> _providers;

    public HoconConfigurationRoot(IEnumerable<IConfigurationProvider> providers)
    {
        _providers = providers.Select(provider => 
            provider is not EnvironmentVariablesConfigurationProvider envProvider ? 
                provider : EnvironmentVariablesConfigurationHoconAdapter.Create(envProvider)).ToList();
    }

    public IConfigurationSection GetSection(string key) => new ConfigurationSection(this, key);

    public HoconConfigurationSection GetHoconSection(string key) => new (this, key);

    public IEnumerable<IConfigurationSection> GetChildren()
    {
        return _providers
            .Aggregate(Enumerable.Empty<string>(),
                (seed, source) => source.GetChildKeys(seed, null))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(GetSection).ToList();
    }

    public IEnumerable<HoconConfigurationSection> GetHoconChildren()
    {
        return _providers
            .Aggregate(Enumerable.Empty<string>(),
                (seed, source) => source.GetChildKeys(seed, null))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(GetHoconSection).ToList();
    }
    
    public IChangeToken GetReloadToken() => throw new NotImplementedException();

    public string? this[string key]
    {
        get => GetConfiguration(_providers, key);
        set => throw new NotImplementedException();
    }
    
    private static string? GetConfiguration(IList<IConfigurationProvider> providers, string key)
    {
        for (var i = providers.Count - 1; i >= 0; i--)
        {
            var provider = providers[i];
            if (provider.TryGet(key, out var value))
            {
                return value;
            }
        }

        return null;
    }

    public void Reload() => throw new NotImplementedException();

    public IEnumerable<IConfigurationProvider> Providers => _providers;
}