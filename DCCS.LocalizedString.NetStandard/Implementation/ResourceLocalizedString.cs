using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace DCCS.LocalizedString.NetStandard
{
    /// <summary>
    /// Localized String used for delayed resolving a string
    /// </summary>
    public class ResourceLocalizedString : ILocalizedString
    {
        Func<CultureInfo, string> _requestStringCallback;

        /// <summary>
        /// Callback for delayed resolving a string. The <see cref="Thread.CurrentThread.CurrentUICulture"/> will be set with the requested culture while the function will be called
        /// </summary>
        /// <param name="requestStringCallback"></param>
        public ResourceLocalizedString(Func<CultureInfo, string> requestStringCallback)
        {
            if (requestStringCallback == null)
                throw new ArgumentNullException();
            _requestStringCallback = requestStringCallback;
        }

        public string GetText(CultureInfo cultureInfo)
        {
            CultureInfo old = Thread.CurrentThread.CurrentUICulture;
            try
            {
                Thread.CurrentThread.CurrentUICulture = cultureInfo;
                string result = _requestStringCallback(cultureInfo);
                return result;
            }
            finally
            {
                Thread.CurrentThread.CurrentUICulture = old;
            }
        }
    }
}
