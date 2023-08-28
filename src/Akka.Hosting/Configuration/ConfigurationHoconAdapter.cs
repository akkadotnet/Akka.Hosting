// -----------------------------------------------------------------------
//  <copyright file="IConfigurationAdapter.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Akka.Configuration;
using Akka.Configuration.Hocon;
using Akka.Util.Internal;
using Microsoft.Extensions.Configuration;

namespace Akka.Hosting.Configuration
{
    public static class ConfigurationHoconAdapter
    {
        public static Config ToHocon(this IConfiguration config)
        {
            var rootObject = new HoconObject();
            switch (config)
            {
                case IConfigurationRoot root:
                {
                    var hoconRoot = new HoconConfigurationRoot(root.Providers);
                    foreach (var child in hoconRoot.GetHoconChildren())
                    {
                        var value = child.ExpandKey(rootObject);
                        value.AppendValue(child.ToHoconElement());
                    }
                    break;
                }
                
                case IConfigurationSection section:
                {
                    var hoconSection = HoconConfigurationSection.Create(section);
                    var value = hoconSection.ExpandKey(rootObject);
                    value.AppendValue(hoconSection.ToHoconElement());
                    break;
                }
                
                default:
                    // We don't know what this is, just do the best we can to convert it to HOCON
                    foreach (var child in config.GetChildren())
                    {
                        var hoconChild = HoconConfigurationSection.Create(child);
                        var value = hoconChild.ExpandKey(rootObject);
                        value.AppendValue(hoconChild.ToHoconElement());
                    }
                    break;
            }
            
            var rootValue = new HoconValue();
            rootValue.AppendValue(rootObject);
            return new Config(new HoconRoot(rootValue));
        }

        private static HoconValue ExpandKey(this HoconConfigurationSection config, HoconObject parent)
        {
            var keys = config.Key.SplitDottedPathHonouringQuotes().ToList();

            // No need to expand the chain
            if (keys.Count == 1)
            {
                return parent.GetOrCreateKey(keys[0]);
            }
            
            var currentObj = parent;
            while (keys.Count > 1)
            {
                var key = keys.Pop();
                var currentValue = currentObj.GetOrCreateKey(key);
                if (currentValue.IsObject())
                {
                    currentObj = currentValue.GetObject();
                }
                else
                {
                    currentObj = new HoconObject();
                    currentValue.AppendValue(currentObj);
                }
            }

            return currentObj.GetOrCreateKey(keys[0]);
        }
        
        private static IHoconElement ToHoconElement(this HoconConfigurationSection config)
        {
            if (config.IsArray())
            {
                var array = new HoconArray();
                foreach (var child in config.GetChildren().OrderBy(c => int.Parse(c.Key)))
                {
                    var value = new HoconValue();
                    var hoconChild = HoconConfigurationSection.Create(child);
                    var element = hoconChild.ToHoconElement();
                    value.AppendValue(element);
                    array.Add(value);
                }
                return array;
            }
            
            if (config.IsObject())
            {
                var rootObject = new HoconObject();
                foreach (var child in config.GetChildren())
                {
                    var hoconChild = HoconConfigurationSection.Create(child);
                    var value = hoconChild.ExpandKey(rootObject);
                    value.AppendValue(hoconChild.ToHoconElement());
                }
                return rootObject;
            }
            
            // Need to back-convert "True" and "False" to "on" and "off"
            return new HoconLiteral
            {
                Value = config.Value switch
                {
                    "True" => "on",
                    "False" => "off",
                    _ => config.Value
                }
            };
        }

        private static string Pop(this IList<string> list)
        {
            var first = list.First();
            list.RemoveAt(0);
            return first;
        }

        private static bool IsObject(this HoconConfigurationSection config)
            => config.GetHoconChildren().Any() && config.Value == null;

        private static bool IsArray(this HoconConfigurationSection config)
        {
            var children = config.GetHoconChildren().ToArray();
            return children.Length > 0 && children.All(c => int.TryParse(c.Key, out _));
        }
    }
}