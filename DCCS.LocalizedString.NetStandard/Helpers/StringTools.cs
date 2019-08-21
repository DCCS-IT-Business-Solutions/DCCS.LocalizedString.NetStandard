using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace DCCS.LocalizedString.NetStandard.Helpers
{
    static class StringTools
    {
        private const int MaxArguments = 50;

        public static string SafeFormat(string format, params object[] arguments)
        {
            return DoSafeFormat(null, CultureInfo.CurrentCulture, format, arguments);
        }

        public static string SafeFormat(IFormatProvider formatProvider, string format, params object[] arguments)
        {
            return DoSafeFormat(null, formatProvider, format, arguments);
        }

        public static string SafeFormat(out Exception exception, IFormatProvider formatProvider, string format, params object[] arguments)
        {
            List<Exception> errors = new List<Exception>();
            var result = DoSafeFormat(errors, formatProvider, format, arguments);
            if (errors.Count == 0)
                exception = null;
            else if (errors.Count > 1)
                exception = new AggregateException(errors);
            else
                exception = errors[0];
            return result;
        }

        static string DoSafeFormat(List<Exception> errors, IFormatProvider formatProvider, string format, object[] arguments)
        {
            if (arguments == null)
                arguments = new object[0];
            if (arguments.Length > MaxArguments)
                throw new ArgumentOutOfRangeException(nameof(arguments), $"Not more than {MaxArguments} parameters supported");
            ParameterFormatter[] formatters = new ParameterFormatter[MaxArguments];
            for (int i = 0; i < arguments.Length; i++)
            {
                formatters[i] = new ParameterFormatter(formatProvider, arguments[i]);
            }
            for (int i = arguments.Length; i < formatters.Length; i++)
            {
                formatters[i] = new ParameterFormatter(formatProvider, null);
            }

            // ReSharper disable once CoVariantArrayConversion
            var result = string.Format(formatProvider, format, formatters);

            bool assert = false;
            if (errors == null)
            {
                errors = new List<Exception>();
                assert = true;
            }

            StringBuilder unusedParameters = null;
            for (int i = 0; i < formatters.Length; i++)
            {
                var formatter = formatters[i];
                if (i < arguments.Length)
                {
                    if (formatter.Error != null)
                        errors.Add(new Exception($"Argument {i} has an formatting error", formatter.Error));
                    if (!formatter.Used)
                    {
                        if (unusedParameters == null)
                            unusedParameters = new StringBuilder();
                        if (unusedParameters.Length > 0)
                            unusedParameters.Append(", ");
                        unusedParameters.Append(formatter.ToString());
                        errors.Add(new Exception($"Argument {i} not used"));
                    }
                }
                else
                {
                    if (formatter.Used)
                        errors.Add(new Exception($"Argument {i} used but not provided"));
                }
            }

            if (unusedParameters != null)
            {
                unusedParameters.Insert(0, " (Unused Parameters: ");
                unusedParameters.Append(")");
                unusedParameters.Insert(0, result);
                result = unusedParameters.ToString();
            }

            if (assert && errors.Count > 0)
            {
#if DEBUG
                StringBuilder message = new StringBuilder();
                foreach (var error in errors)
                {
                    if (message.Length > 0)
                        message.Append("\r\n");
                    message.Append(error.Message);
                }

                Debug.Assert(false, message.ToString());
#endif
            }

            return result;
        }


        class ParameterFormatter : IFormattable
        {
            private object _value;
            private IFormatProvider _formatProvider;
            public bool Used { get; private set; }
            public Exception Error { get; private set; }

            public ParameterFormatter(IFormatProvider formatProvider, object value)
            {
                _formatProvider = formatProvider;
                _value = value;
            }

            public string ToString(string format, IFormatProvider formatProvider)
            {
                Used = true;
                try
                {
                    if (_value is IFormattable formattable)
                        return formattable.ToString(format, formatProvider);
                    return string.Format(_formatProvider, "{0}", _value);
                }
                catch (Exception e)
                {
                    Error = e;
                    return ToString();
                }
            }

            public override string ToString()
            {
                if (_value == null)
                    return "";
                try
                {
                    return _value.ToString();
                }
                catch (Exception)
                {
                    return _value.GetType().FullName ?? "";
                }
            }
        }

        enum CharType
        {
            Lower,
            Upper,
            Digit,
            Other,
        }

        public static string ToDisplayName(string name)
        {
            StringBuilder builder = new StringBuilder();
            bool addBlankBeforeNextUpperOrDigit = false;
            int upperCount = 0;
            CharType lastType = CharType.Other;
            for (int i = 0; i < name.Length; i++)
            {
                var c = name[i];
                CharType type = CharType.Other;
                if (char.IsLower(c))
                    type = CharType.Lower;
                else if (char.IsUpper(c))
                    type = CharType.Upper;
                else if (char.IsDigit(c))
                    type = CharType.Digit;
                var cNext = i < name.Length - 1 ? name[i + 1] : (char)0;
                if (char.IsUpper(c))
                    upperCount++;
                else
                    upperCount = 0;
                if (upperCount > 1 && cNext != 0 && char.IsLower(cNext))
                {
                    builder.Append(' ');
                    addBlankBeforeNextUpperOrDigit = true;
                }
                else if (type == CharType.Upper || type == CharType.Digit)
                {
                    if (i != 0)
                    {
                        if (addBlankBeforeNextUpperOrDigit || (type != lastType && lastType != CharType.Other))
                            builder.Append(' ');
                    }
                    addBlankBeforeNextUpperOrDigit = false;
                }
                else
                {
                    addBlankBeforeNextUpperOrDigit = true;
                }

                if (c == '_')
                {
                    builder.Append(' ');
                    addBlankBeforeNextUpperOrDigit = false;
                }
                else
                {
                    builder.Append(c);
                }

                lastType = type;
            }
            return builder.ToString();
        }
    }
}
