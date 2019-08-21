using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace DCCS.LocalizedString.NetStandard
{
    /// <summary>
    /// This is the main interface for the usage of the translation service. It will be used to create instances of <see cref="ILocalizedString"/> instances.
    /// </summary>
    public interface ITranslationService
    {
        /// <summary>
        /// Creates a simple localized string
        /// </summary>
        /// <param name="key">Key for the localized string.</param>
        /// <returns></returns>
        ILocalizedString Create(ILocalizerKey key);

        /// <summary>
        /// Creates a localized string contains runtime parameters
        /// </summary>
        /// <param name="formatKey">Key fot the localized string</param>
        /// <param name="parameters">Runtime parameters</param>
        /// <returns></returns>
        ILocalizedString Create(ILocalizedFormatKey formatKey, params object[] parameters);

        /// <summary>
        /// Create a localized string from an enum value
        /// </summary>
        /// <param name="localizedEnum">Enum value.</param>
        /// <returns></returns>
        ILocalizedString Create(Enum localizedEnum);
    }

    /// <summary>
    /// This is main interface for the translation provider service. It will be used to get the translated strings
    /// </summary>
    public interface ITranslationProviderService
    {
        /// <summary>
        /// Returns the localized string.
        /// </summary>
        /// <param name="assemblyName">Name of the assembly</param>
        /// <param name="key">Key, unique inside of the assembly</param>
        /// <param name="language">Requested language</param>
        /// <returns>Localized string or null, if the key is not available</returns>
        string FindText(string assemblyName, string key, CultureInfo language);
    }

    /// <summary>
    /// Interface for all localized string implementation. Provides access to the different languages
    /// </summary>
    public interface ILocalizedString
    {
        /// <summary>
        /// Returns the text in the requested language
        /// </summary>
        /// <param name="cultureInfo">Specifies the language</param>
        /// <returns></returns>
        string GetText(CultureInfo cultureInfo);
    }
    /// <summary>
    /// Key for a simple string
    /// </summary>
    public interface ILocalizerKey
    {
        /// <summary>
        /// Key for the localized string
        /// </summary>
        string Key { get; }
        /// <summary>
        /// Context for the key. Specifies the own assembly of the key.
        /// </summary>
        string AssemblyName { get; }
        /// <summary>
        /// Default english text
        /// </summary>
        string Default { get; }
    }

    /// <summary>
    /// Key for strings containing runtime parameters
    /// </summary>
    public interface ILocalizedFormatKey : ILocalizerKey
    {
        /// <summary>
        /// Names of the parameters.
        /// </summary>
        string[] ParameterNames { get; }
    }


}
