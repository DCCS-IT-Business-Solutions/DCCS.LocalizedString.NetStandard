using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;

namespace DCCS.LocalizedString.NetStandard
{
    /// <summary>
    /// Transfers a localized string over the network with the current UI culture and invariant culture
    /// </summary>
    [DataContract]
    [Serializable]
    public class LocalizedStringContract : ILocalizedString
    {
        /// <summary>
        /// The type of the text
        /// </summary>
        [DataMember(Name = "type")]
        public string Type { get; set; } = LocalizedStringType.Information.ToString();
        /// <summary>
        /// Text in the language specified in <see cref="Language"/> 
        /// </summary>
        [DataMember(Name = "text")]
        public string Text { get; set; }

        /// <summary>
        /// Language of the <see cref="Text"/> property
        /// </summary>
        [DataMember(Name = "language")]
        public string Language { get; set; }

        /// <summary>
        /// Invariant representation of the text
        /// </summary>
        [DataMember(Name = "invariant")]
        public string Invariant { get; set; }

        public LocalizedStringContract()
        {
            
        }

        /// <summary>
        /// Creates a new instance of the contract
        /// </summary>
        /// <param name="localizedString">The localized string</param>
        /// <param name="culture">The used culture for the transfer. If null the CultureInfo.CurrentUICulture will be used.</param>
        public LocalizedStringContract(ILocalizedString localizedString, CultureInfo culture = null)
        {
            if (culture == null)
            {
                culture = CultureInfo.CurrentUICulture;
            }
            Language = culture.Name;
            Text = localizedString.GetText(culture);
            if (culture.LCID == CultureInfo.InvariantCulture.LCID)
            {
                Invariant = Text;
            }
            else
            {
                Invariant = localizedString.GetText(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Get the text in the specified culture.
        /// </summary>
        /// <param name="cultureInfo">Requested culture</param>
        /// <returns>Text in the specified culture. If the text is not available in the specified culture, the invariant representation will be returned.</returns>
        public string GetText(CultureInfo cultureInfo)
        {
            if (Language == cultureInfo.Name)
            {
                return Text;
            }
            return Invariant;
        }
    }

    public enum LocalizedStringType
    {
        Information,
        Warning,
        Error
    }
}
