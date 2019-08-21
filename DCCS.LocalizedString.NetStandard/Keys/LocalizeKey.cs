using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace DCCS.LocalizedString.NetStandard
{
    [Serializable]
    public abstract class LocalizerKey : ILocalizerKey
    {        
        private Type _type;
        private string _key;
        public string AssemblyName => _type.Assembly.GetName(false).FullName;

        public string Key
        {
            get
            {
                InitializeAllKeysOfThisClass();
                return _key;
            }
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            InitializeAllKeysOfThisClass();
        }
        void InitializeAllKeysOfThisClass()
        {
            if (_key == null)
            {
                foreach (var fieldInfo in _type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (typeof(LocalizerKey).IsAssignableFrom(fieldInfo.FieldType))
                    {
                        var localizerKey = (LocalizerKey) fieldInfo.GetValue(null);
                        if (localizerKey != null)
                            localizerKey._key = _type.FullName + "." + fieldInfo.Name;
                    }
                }
            }
        }

        public abstract string Default { get; }
        public abstract string[] ParameterNames { get; }

        protected LocalizerKey(Type type, string key)
        {
            _type = type;
            _key = key;
        }
        protected LocalizerKey()
        {
            // Search the method wich creates this instance
            var currentType = GetType();
            var stackTrace = new StackTrace(1, false);
            StackFrame callingFrame = null;
            bool searchFirstMethodWhichIsNotOwnConstructor = false;
            for (int i = 0; i < stackTrace.FrameCount; i++)
            {
                var frame = stackTrace.GetFrame(i);
                var method = frame.GetMethod();
                if (searchFirstMethodWhichIsNotOwnConstructor)
                {
                    if (method.DeclaringType != currentType)
                    {
                        callingFrame = frame;
                        break;
                    }
                    if (method.MemberType != MemberTypes.Constructor)
                    {
                        callingFrame = frame;
                        break;
                    }
                    if (method.IsStatic)
                    {
                        callingFrame = frame;
                        break;
                    }
                }
                if (method.DeclaringType != currentType)
                {
                    if (method.DeclaringType != null && !method.DeclaringType.IsSerializable)
                        throw new Exception($"Type '{ method.DeclaringType }' must be serializable");
                    continue;
                }
                // current type constructor found
                searchFirstMethodWhichIsNotOwnConstructor = true;
            }
            if (callingFrame == null)
                throw new Exception($"'{currentType.FullName}' must be used as static readonly object.");

            var callingMethod = callingFrame.GetMethod();
            if (callingMethod.MemberType != MemberTypes.Constructor || !callingMethod.IsStatic)
                throw new Exception($"'{currentType.FullName}' created in '{callingMethod.DeclaringType?.FullName}.{callingMethod.Name}' must be used as static readonly object.");
            _type = callingMethod.DeclaringType;
            if (_type == null)
                throw new Exception("No Type found which creates the Key. Calling from dynamically created method?");
            if (_type.IsGenericType)
                throw new Exception($"'{currentType.FullName}' must not be created in the generic class '{_type.FullName}'");

        }
    }
}
