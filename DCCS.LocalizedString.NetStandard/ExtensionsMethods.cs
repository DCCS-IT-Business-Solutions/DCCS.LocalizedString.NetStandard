using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DCCS.LocalizedString.NetStandard
{
    /// <summary>
    /// Extensions for the <see cref="ILocalizedString"/> interface
    /// </summary>
    public static class LocalizedStringExtensions
    {
        /// <summary>
        /// Get the text in the current UI culture
        /// </summary>
        /// <param name="localizedString">localized string instance. Can be null to return String.Empty</param>
        /// <returns>Returns the text in the current UI culture.</returns>
        public static string GetText(this ILocalizedString localizedString)
        {
            if (localizedString == null)
                return "";            
            return localizedString.GetText(CultureInfo.CurrentUICulture);
        }

        /// <summary>
        /// Returns the localized string representation of an object
        /// </summary>
        /// <param name="objectToConvert">The object which to be converted</param>
        /// <param name="translationService">The translation service</param>
        /// <returns></returns>
        public static ILocalizedString ToLocalizedString(this object objectToConvert, ITranslationService translationService)
        {
            if (translationService == null)
                throw new ArgumentNullException(nameof(translationService));
            if (objectToConvert == null)
                return NeutralLocalizedString.Empty;
            if (objectToConvert is ILocalizedString localizedString)
                return localizedString;
            var type = objectToConvert.GetType();
            if (type.IsEnum && type.IsDefined(typeof(TranslatedAttribute), false))
                return translationService.Create((Enum)objectToConvert);
            return new NeutralLocalizedString(objectToConvert.ToString());
        }
    }
}
