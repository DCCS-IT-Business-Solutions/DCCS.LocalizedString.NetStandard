using System;
using System.Collections.Generic;
using System.Globalization;
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
        public LocalizedExceptionContract(ITranslationService translationService, LocalizedException userException, CultureInfo cultureInfo = null) : base(LocalizedException.CreateLocalizedMessage(translationService, userException), cultureInfo)
        {
            IsError = !(userException is LocalizedWarningException);
        }
    }
}
