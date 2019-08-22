using DCCS.ExceptionHelpers.NetStandard;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace DCCS.LocalizedString.NetStandard
{
    /// <summary>
    /// Use this exception for errors which are caused by user and should be shown to the user
    /// </summary>
    [Serializable]
    public class LocalizedException : ApplicationException, ILocalizedString
    {
        static readonly LocalizedStringKey InternalErrorMessage = new LocalizedStringKey("Internal Error");
        private readonly ILocalizedString _message;

        public static ILocalizedString CreateLocalizedMessage(ITranslationService translationService, Exception exception)
        {
            var message = FindLocalizedMessage(exception);
            if (message != null)
                return message;
            return translationService.Create(InternalErrorMessage);
        }

        public static ILocalizedString FindLocalizedMessage(Exception exception)
        {
            var userExceptions = SearchLocalizedExceptions(exception);
            var localizedArray = new LocalizedArray(userExceptions);
            localizedArray.Separator = new NeutralLocalizedString(Environment.NewLine);
            if (localizedArray.Count > 0)
            {
                return localizedArray;
            }

            return null;
        }

        public static IEnumerable<LocalizedException> SearchLocalizedExceptions(Exception exception)
        {
            return exception.GetAllExceptionsInHirachy().OfType<LocalizedException>();
        }


        /// <summary>
        /// Construct the exception
        /// </summary>
        /// <param name="message">Error message</param>
        public LocalizedException(ILocalizedString message) : base(message.GetText())
        {
            _message = message;
        }

        /// <summary>
        /// Construct the exception
        /// </summary>
        /// <param name="innerException">Inner exception. Can be null.</param>
        /// <param name="message">Error message</param>
        public LocalizedException(Exception innerException, ILocalizedString message) : base(message.GetText(), innerException)
        {
            _message = message;
        }
      

        /// <summary>
        /// Deserialize constructor
        /// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Serialization context</param>
        protected LocalizedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            _message = (ILocalizedString) info.GetValue(nameof(_message), typeof(ILocalizedString));

        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(_message), new LocalizedStringContract(_message));
            base.GetObjectData(info, context);
        }

        public string GetText(CultureInfo cultureInfo)
        {
            return _message.GetText(cultureInfo);
        }

        public override string Message => _message.GetText();
    }
}
