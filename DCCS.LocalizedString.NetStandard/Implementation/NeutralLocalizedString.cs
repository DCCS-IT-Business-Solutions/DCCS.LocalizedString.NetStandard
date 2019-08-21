using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace DCCS.LocalizedString.NetStandard
{
    /// <summary>
    /// Implementation of <see cref="ILocalizedString"/> for untranslateable strings
    /// </summary>
    public class NeutralLocalizedString : ILocalizedString
    {
        private readonly object _object;

        /// <summary>
        /// Intialize the instance
        /// </summary>
        /// <param name="text">Untranslateable text object</param>
        public NeutralLocalizedString(object text)
        {
            _object = text;
        }

        /// <summary>
        /// EmptylLocalized string
        /// </summary>
        public static readonly ILocalizedString Empty = new NeutralLocalizedString("");

        /// <summary>
        /// New line localized string instance
        /// </summary>
        public static readonly ILocalizedString NewLine = new NeutralLocalizedString(Environment.NewLine);

        /// <summary>
        /// Returns the text object formatted with the specfied culture.
        /// </summary>
        /// <param name="cultureInfo">Culture</param>
        /// <returns>Formatted object</returns>
        public string GetText(CultureInfo cultureInfo)
        {
            if (_object == null)
                return string.Empty;
            var old = Thread.CurrentThread.CurrentUICulture;
            try
            {
                Thread.CurrentThread.CurrentUICulture = cultureInfo;
                string text = _object.ToString();
                return text;
            }
            finally
            {
                Thread.CurrentThread.CurrentUICulture = old;
            }
        }
    }
}
