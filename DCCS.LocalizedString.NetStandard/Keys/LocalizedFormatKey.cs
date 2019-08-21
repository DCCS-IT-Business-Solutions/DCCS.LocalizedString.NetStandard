using System;
using System.Collections.Generic;
using System.Text;

namespace DCCS.LocalizedString.NetStandard
{
    public class LocalizedFormatKey : LocalizerKey, ILocalizedFormatKey
    {
        public override string Default { get; }
        public override string[] ParameterNames { get; }

        public LocalizedFormatKey(string englishDefaultFormat, params string[] parameterNames)
        {
            Default = englishDefaultFormat;
            ParameterNames = parameterNames;
        }

    }
}
