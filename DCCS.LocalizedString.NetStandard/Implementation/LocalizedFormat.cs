using DCCS.LocalizedString.NetStandard.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DCCS.LocalizedString.NetStandard
{
    /// <summary>
    /// Localized formatted string with runtime parameters similar to FormattableString
    /// </summary>
    class LocalizedFormat : ILocalizedString
    {
        private readonly ILocalizedFormatKey _key;
        private readonly object[] _parameters;
        private readonly ITranslationService _translationService;

        /// <summary>
        /// Initialize the instance
        /// </summary>
        /// <param name="translationService">Translation service</param>
        /// <param name="key">Key</param>
        /// <param name="parameters">Runtime parameters</param>
        public LocalizedFormat(ITranslationService translationService, ILocalizedFormatKey key, params object[] parameters)
        {
            _translationService = translationService;
            _key = key ?? throw new ArgumentNullException(nameof(key));
            _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        }

        /// <summary>
        /// Returns the formatted string in the specified culture
        /// </summary>
        /// <param name="cultureInfo">Culture</param>
        /// <returns>Formatted string</returns>
        public string GetText(CultureInfo cultureInfo)
        {
            object[] parameters = _parameters.Select(p => (object)p.ToLocalizedString(_translationService).GetText(cultureInfo)).ToArray();
            if (ReferenceEquals(cultureInfo, TranslationService.DevelopmentKeyCulture))
            {
                StringBuilder resultBuilder = new StringBuilder();
                resultBuilder.Append(_key.Key);
                if (parameters.Length > 0)
                {
                    resultBuilder.Append("(");
                    bool first = true;
                    foreach (var parameter in parameters)
                    {
                        if (first)
                            first = false;
                        else
                            resultBuilder.Append(", ");
                        resultBuilder.Append(parameter);
                    }
                    resultBuilder.Append(")");
                }
                return resultBuilder.ToString();
            }
            string format = _translationService.Create((ILocalizerKey)_key).GetText(cultureInfo);
            return StringTools.SafeFormat(format, parameters);
        }
    }
}
