using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace PlatformStatusTracker.Core.Configuration
{
    public static class ConfigurationHelper
    {
        public static IReadOnlyDictionary<string, string> BuildConfigurationLog(IConfiguration configuration)
        {
            var dict = new Dictionary<string, string>();
            foreach (var child in configuration.GetChildren())
            {
                if (child.Value == null)
                {
                    TraverseConfiguration(child, dict);
                }
                else
                {
                    // Skip root that is a string, as it is usually an environment variable.
                }
            }

            return dict;

            void TraverseConfiguration(IConfigurationSection section, Dictionary<string, string> dict)
            {
                if (section.Value != null)
                {
                    dict[section.Path] = MaskSecrets(section);
                }

                var children = section.GetChildren();
                if (children != null)
                {
                    foreach (var child in children)
                    {
                        TraverseConfiguration(child, dict);
                    }
                }
            }

            string MaskSecrets(IConfigurationSection section)
            {
                if (section.Key.EndsWith("Key", StringComparison.OrdinalIgnoreCase) ||
                    section.Key.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
                    section.Key.Contains("Secret", StringComparison.OrdinalIgnoreCase) ||
                    section.Key.Contains("Token", StringComparison.OrdinalIgnoreCase))
                {
                    return $"<Secret Hash={HashString(section.Value)}>";
                }
                else
                {
                    return Regex.Replace(section.Value, "(password|authtoken|accountkey)=([^;]+)", (m) => $"{m.Groups[1].Value}=<Secret Hash={HashString(m.Groups[2].Value)}>", RegexOptions.IgnoreCase);
                }
            }

            string HashString(string value)
            {
                using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Assembly.GetEntryAssembly()!.FullName!)))
                {
                    return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(value))).Substring(0, 6).ToLowerInvariant();
                }
            }
        }
    }
}
