// -----------------------------------------------------------------------
//   <copyright file="HoconConfigurationSection.cs" company="Petabridge, LLC">
//     Copyright (C) 2015-2023 .NET Petabridge, LLC
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Akka.Hosting.Configuration;

internal class HoconConfigurationSection: IConfigurationSection
{
    public static HoconConfigurationSection Create(IConfigurationSection section)
    {
        if (section is HoconConfigurationSection hoconSection)
            return hoconSection;

        if (section is not ConfigurationSection configSection)
            throw new Exception($"{nameof(section)} argument could not be cast to {nameof(ConfigurationSection)}");

        var fieldInfo = typeof(ConfigurationSection).GetField("_root", BindingFlags.Instance | BindingFlags.NonPublic);
        if (fieldInfo is null)
            throw new Exception($"{nameof(ConfigurationSection)} source code has changed: Could not find private field '_root'");

        var root = (IConfigurationRoot)fieldInfo.GetValue(configSection);
        var hoconRoot = new HoconConfigurationRoot(root.Providers);
        return new HoconConfigurationSection(hoconRoot, configSection.Path);
    }

    private string? _key;

    public HoconConfigurationSection(HoconConfigurationRoot root, string path)
    {
        Root = root;
        Path = path;
    }

    public IConfigurationSection GetSection(string key) => Root.GetSection(ConfigurationPath.Combine(Path, key));

    public IEnumerable<IConfigurationSection> GetChildren()
    {
        return Root.Providers
            .Aggregate(Enumerable.Empty<string>(),
                (seed, source) => source.GetChildKeys(seed, Path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(key => Root.GetSection(ConfigurationPath.Combine(Path, key)));
    }

    public IEnumerable<HoconConfigurationSection> GetHoconChildren()
    {
        return Root.Providers
            .Aggregate(Enumerable.Empty<string>(),
                (seed, source) => source.GetChildKeys(seed, Path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(key => Root.GetHoconSection(ConfigurationPath.Combine(Path, key)));
    }

    public IChangeToken GetReloadToken() => Root.GetReloadToken();

    public string? this[string key]
    {
        get => Root[ConfigurationPath.Combine(Path, key)];
        set => throw new NotImplementedException();
    }

    // Key is calculated lazily as last portion of Path
    public string Key => _key ??= ConfigurationPath.GetSectionKey(Path);

    public HoconConfigurationRoot Root { get; }

    public string Path { get; }

    public string? Value
    {
        get => Root[Path];
        set => throw new NotImplementedException();
    }
}