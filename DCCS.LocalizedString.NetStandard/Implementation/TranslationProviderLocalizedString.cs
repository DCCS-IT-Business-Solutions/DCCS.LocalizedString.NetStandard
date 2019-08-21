using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DCCS.LocalizedString.NetStandard
{
    /// <summary>
    /// Implementation of a localized string based on the <see cref="ITranslationProviderService"/>
    /// </summary>
    class TranslationProviderLocalizedString : ILocalizedString
    {
        static readonly CultureInfo FallbackCulture = CultureInfo.GetCultureInfo("en");
        private readonly ITranslationProviderService[] _translationProviders;
        private readonly ILocalizerKey _key;

        /// <summary>
        /// Initialize the instance
        /// </summary>
        /// <param name="providers">Translation providers</param>
        /// <param name="key">Localize key</param>
        public TranslationProviderLocalizedString(ITranslationProviderService[] providers, ILocalizerKey key)
        {
            this._translationProviders = providers;
            this._key = key;
        }

        /// <summary>
        /// Return the text for the specified culture
        /// </summary>
        /// <param name="cultureInfo">Culture</param>
        /// <returns>Text in the specified language</returns>
        public string GetText(CultureInfo cultureInfo)
        {
            if (ReferenceEquals(cultureInfo, TranslationService.DevelopmentKeyCulture))
                return this._key.Key;
            bool fallbackTested = false;
            for (CultureInfo lookupCultureInfo = cultureInfo; ; lookupCultureInfo = lookupCultureInfo.Parent)
            {
                if (!fallbackTested && lookupCultureInfo.LCID == CultureInfo.InvariantCulture.LCID)
                {
                    lookupCultureInfo = FallbackCulture;
                }

                if (lookupCultureInfo.Name == FallbackCulture.Name)
                    fallbackTested = true;

                foreach (var provider in _translationProviders)
                {
                    string text = provider.FindText(_key.AssemblyName, _key.Key, lookupCultureInfo);
                    if (text != null)
                        return text;
                }
                if (lookupCultureInfo.LCID == CultureInfo.InvariantCulture.LCID)
                    break;
            }
            return _key.Default;
        }
    }
}
