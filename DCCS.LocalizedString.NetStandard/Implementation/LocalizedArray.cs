using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DCCS.LocalizedString.NetStandard
{
    /// <summary>
    /// Array of localized string instances joined to one localized string
    /// </summary>
    public class LocalizedArray : List<ILocalizedString>, ILocalizedString
    {
        /// <summary>
        /// Join separator
        /// </summary>
        public ILocalizedString Separator { get; set; }

        /// <summary>
        /// Initialize the localized string
        /// </summary>
        /// <param name="localizedStrings">Localized strings instances</param>
        public LocalizedArray(params ILocalizedString[] localizedStrings) : base(localizedStrings ?? new ILocalizedString[0])
        {
        }

        /// <summary>
        /// Initialize the localized string
        /// </summary>
        /// <param name="localizedStrings">Localized strings instances</param>
        public LocalizedArray(IEnumerable<ILocalizedString> localizedStrings) : base(localizedStrings ?? new ILocalizedString[0])
        {
        }

        /// <summary>
        /// Returns the text for the specified culture
        /// </summary>
        /// <param name="cultureInfo">Culture</param>
        /// <returns>Text for the specified culture</returns>
        public string GetText(CultureInfo cultureInfo)
        {
            string separator;
            if (Separator != null)
                separator = Separator.GetText(cultureInfo);
            else
                separator = cultureInfo.TextInfo.ListSeparator;
            return string.Join(separator, this.Select(e => e.GetText(cultureInfo)));
        }
    }
}
