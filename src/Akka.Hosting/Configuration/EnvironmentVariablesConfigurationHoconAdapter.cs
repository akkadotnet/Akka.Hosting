// -----------------------------------------------------------------------
//   <copyright file="EnvironmentVariablesConfigurationHoconAdapter.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Configuration.EnvironmentVariables;

namespace Akka.Hosting.Configuration;

internal class EnvironmentVariablesConfigurationHoconAdapter: EnvironmentVariablesConfigurationProvider
{
    public static EnvironmentVariablesConfigurationHoconAdapter Create(EnvironmentVariablesConfigurationProvider parent)
    {
        var propertyInfo = typeof(EnvironmentVariablesConfigurationProvider).GetProperty("Data", BindingFlags.Instance | BindingFlags.NonPublic);
        if (propertyInfo is null)
            throw new Exception($"{nameof(EnvironmentVariablesConfigurationProvider)} source code has changed: Could not find protected property 'Data'");
        
        var prefix = (Dictionary<string, string>)propertyInfo.GetValue(parent);
        return new EnvironmentVariablesConfigurationHoconAdapter(prefix);
    }
    
    private EnvironmentVariablesConfigurationHoconAdapter(Dictionary<string, string> data)
    {
        foreach (var kvp in data)
        {
            // Sanitize configuration brought in from environment variables, 
            // "__" are already converted to ":" by the environment configuration provider.
            var newKey = kvp.Key.ToLowerInvariant().Replace("_", "-");
            Data[newKey] = kvp.Value;
        }
    }

    public override void Load()
    {
        throw new NotImplementedException();
    }
}