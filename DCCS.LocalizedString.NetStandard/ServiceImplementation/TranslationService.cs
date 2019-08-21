using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;

namespace DCCS.LocalizedString.NetStandard
{
    /// <summary>
    /// Translation service implementation
    /// </summary>
    public class TranslationService : ITranslationService
    {
        internal static readonly CultureInfo DevelopmentKeyCulture = new CultureInfo("yo");

        private readonly ITranslationProviderService[] _translationProviders;
        private readonly ConcurrentDictionary<ILocalizerKey, ILocalizedString> _textCache = new ConcurrentDictionary<ILocalizerKey, ILocalizedString>(new LocalizedKeyComparer());

        class LocalizedKeyComparer : IEqualityComparer<ILocalizerKey>
        {
            public bool Equals(ILocalizerKey x, ILocalizerKey y)
            {
                return x.Key == y.Key && x.AssemblyName == y.AssemblyName;
            }

            public int GetHashCode(ILocalizerKey obj)
            {
                return ValueTuple.Create(obj.Key, obj.AssemblyName).GetHashCode();
            }
        }

        /// <summary>
        /// Construct with the provided translation providers
        /// </summary>
        /// <param name="translationProviders">Translation providers</param>
        public TranslationService(IEnumerable<ITranslationProviderService> translationProviders)
        {
            _translationProviders = translationProviders.ToArray();
        }

        /// <summary>
        /// Create localized string instance for the key
        /// </summary>
        /// <param name="key">Localizer Key</param>
        /// <returns>Localized string instance</returns>
        public ILocalizedString Create(ILocalizerKey key)
        {
            return _textCache.GetOrAdd(key, k => new TranslationProviderLocalizedString(_translationProviders, k));
        }

        /// <summary>
        /// Create localized string instance for the format key and runtime parameters
        /// </summary>
        /// <param name="formatKey">Localizer Format Key</param>
        /// <param name="parameters">Runtimeparameters</param>
        /// <returns>Localized string instance</returns>
        public ILocalizedString Create(ILocalizedFormatKey formatKey, params object[] parameters)
        {
            return new LocalizedFormat(this, formatKey, parameters);
        }

        /// <summary>
        /// Create localized string instance from an enum
        /// </summary>
        /// <param name="localizedEnum">Enum value</param>
        /// <returns>Localized string instance</returns>
        public ILocalizedString Create(Enum localizedEnum)
        {           
            return Create(new LocalizedEnumKey(localizedEnum));
        }
            
      
    }
}
