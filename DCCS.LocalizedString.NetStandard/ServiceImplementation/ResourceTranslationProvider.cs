using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading;

namespace DCCS.LocalizedString.NetStandard
{
    /// <summary>
    /// Loads the localized text from the resource
    /// </summary>
    public class ResourceTranslationProvider : ITranslationProviderService
    {
        public const string CommentPrefix = "!";
        static readonly ConcurrentDictionary<string, ResourceManager> MapAssemblyToResourceManagers = new ConcurrentDictionary<string, ResourceManager>();
        static readonly ConcurrentDictionary<Tuple<ResourceManager, string>, ResourceSet> MapResourceManagerAndCultureToResourceManagers = new ConcurrentDictionary<Tuple<ResourceManager, string>, ResourceSet>();

        /// <summary>
        /// Returns the text in the specified language
        /// </summary>
        /// <param name="assemblyName">Name of the resource assembly</param>
        /// <param name="key">Key for resource lookup</param>
        /// <param name="language">Requested language</param>
        /// <returns>Text in the specified language or null if the key was not found</returns>
        public string FindText(string assemblyName, string key, CultureInfo language)
        {
            var resourceManager = MapAssemblyToResourceManagers.GetOrAdd(assemblyName, k =>
            {
                Assembly assembly = Assembly.Load(assemblyName);
                const string resourcesPostfix = ".resources";
                string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(r => r.EndsWith(".Properties.Resources" + resourcesPostfix));
                if (resourceName == null)
                    return null;

                return new ResourceManager(resourceName.Substring(0, resourceName.Length - resourcesPostfix.Length), assembly);
            });
            if (resourceManager == null)
                return null;

            var resourceSet = MapResourceManagerAndCultureToResourceManagers.GetOrAdd(Tuple.Create(resourceManager, language.Name), k =>  resourceManager.GetResourceSet(language, true, false) );
            if (resourceSet == null)
                return null;
            string resourceKey = BuildResourceKey(key);
            var result = resourceSet.GetString(resourceKey);
            if (result != null && result.StartsWith(CommentPrefix))
                return null;
            return result;
        }

        /// <summary>
        /// Build the resource lookup key from the localization key
        /// </summary>
        /// <param name="key">Localization key</param>
        /// <returns>Resource key</returns>
        public static string BuildResourceKey(string key)
        {
            return key.Replace(".", "_").Replace("+", "_");
        }
    }
}
