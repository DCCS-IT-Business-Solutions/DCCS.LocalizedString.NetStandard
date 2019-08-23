using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace DCCS.LocalizedString.NetStandard
{
    [DataContract]
    [Serializable]
    public class LocalizedExceptionContract : LocalizedStringContract
    {
        [DataMember]
        public bool IsError { get; set; }
        public LocalizedExceptionContract(LocalizedException localizedException, CultureInfo cultureInfo = null, bool singleMessage = false) : base(singleMessage ? (ILocalizedString) localizedException : new LocalizedArray(LocalizedException.SearchLocalizedExceptions(localizedException)), cultureInfo)
        {
            if (singleMessage)
                IsError = !(localizedException is LocalizedWarningException);
            else
                IsError = LocalizedException.SearchLocalizedExceptions(localizedException).All(e => e is LocalizedWarningException);
        }

        public static LocalizedExceptionContract[] CreateArray(IEnumerable<LocalizedException> exceptions, CultureInfo cultureInfo = null)
        {
            var contracts = new List<LocalizedExceptionContract>();
            foreach (var exception in exceptions)
            {
                contracts.Add(new LocalizedExceptionContract(exception, cultureInfo, true));
            }
            return contracts.ToArray();
        }
    }
}
