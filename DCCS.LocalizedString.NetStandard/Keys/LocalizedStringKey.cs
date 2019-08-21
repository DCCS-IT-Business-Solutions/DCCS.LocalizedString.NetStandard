using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DCCS.LocalizedString.NetStandard
{
    public class LocalizedStringKey : LocalizerKey
    {
        public LocalizedStringKey(string englishDefaultText)
        {
            Default = englishDefaultText;
        }
        public override string Default { get; }
        public override string[] ParameterNames => new string[0];
    }
}
