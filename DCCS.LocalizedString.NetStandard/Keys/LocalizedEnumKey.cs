using DCCS.LocalizedString.NetStandard.Helpers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DCCS.LocalizedString.NetStandard
{
    [Serializable]
    public class LocalizedEnumKey : LocalizerKey
    {
        public LocalizedEnumKey(Enum enumValue) : this(enumValue.GetType(), enumValue.ToString())
        {
            if (!enumValue.GetType().IsDefined(typeof(TranslatedAttribute), true))
                throw new Exception($"Enum '{enumValue.GetType().FullName}' is not marked with [{typeof(TranslatedAttribute).Name}]");
        }
        internal LocalizedEnumKey(Type enumType, string enumName) : base(enumType, enumType.FullName + "." + enumName)
        {
            if (enumName == null)
                throw new ArgumentNullException(nameof(enumName));
            var translatedAttribute = enumType.GetField(enumName, BindingFlags.Static | BindingFlags.Public)?.GetCustomAttribute<TranslatedAttribute>();
            if (translatedAttribute != null && translatedAttribute.Default != null)
                Default = translatedAttribute.Default;
            else
                Default = StringTools.ToDisplayName(enumName);
        }
        public override string Default { get; }
        public override string[] ParameterNames => new string[0];
    }
}
