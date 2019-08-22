using System;
using System.Runtime.Serialization;

namespace DCCS.LocalizedString.NetStandard
{
    /// <summary>
    /// Use this exception for errors which are caused by user and should be shown to the user as warnings
    /// </summary>
    [Serializable]
    public class LocalizedWarningException : LocalizedException
    {
        public LocalizedWarningException(ILocalizedString message) : base(message)
        {

        }

        public LocalizedWarningException(Exception innerException, ILocalizedString message) : base(innerException, message)
        {
            
        }

        protected LocalizedWarningException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }
    }
}
