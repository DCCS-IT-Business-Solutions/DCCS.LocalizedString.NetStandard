using System;
using System.Collections.Generic;
using System.Text;

namespace DCCS.LocalizedString.NetStandard
{
    [AttributeUsage(AttributeTargets.Enum | AttributeTargets.Field)]
    public class TranslatedAttribute : Attribute
    {
        public string Default { get; }
        public TranslatedAttribute(string defaultValue = null)
        {
            Default = defaultValue;
        }
    }
}
