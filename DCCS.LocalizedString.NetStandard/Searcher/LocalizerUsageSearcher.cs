using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DCCS.LocalizedString.NetStandard
{

    /// <summary>
    /// Search used localization keys in a assembly
    /// </summary>
    public static class LocalizerUsageSearcher
    {
        [DebuggerHidden]
        private static Type[] GetExportedTypes(Assembly assembly)
        {
            Type[] exportedTypes;
            try
            {
                exportedTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                exportedTypes = e.Types.Where(t => t != null).ToArray();
            }
            return exportedTypes;
        }

        /// <summary>
        /// Returns a list of the required localization keys
        /// </summary>
        /// <param name="assembly">Assembly</param>
        /// <returns></returns>
        public static IEnumerable<LocalizerEntry> GetLocalizerEntries(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            foreach (var type in GetExportedTypes(assembly)) // If you break here with an exception, just continue the execution (it will be catched)
            {             
                bool isTranslatedType = false;
                try
                {
                    isTranslatedType = type.IsDefined(typeof(TranslatedAttribute));
                }
                catch
                {

                }

                if (isTranslatedType)
                {
                    if (type.IsEnum)
                    {
                        foreach (var entry in Enum.GetNames(type))
                        {
                            yield return new LocalizerEntry(new LocalizedEnumKey(type, entry));
                        }
                    }
                }
                if (type.IsGenericType)
                    continue;

                foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    LocalizerEntry entry = null;
                    try
                    {
                        if (typeof(ILocalizerKey).IsAssignableFrom(field.FieldType)) // If you break here with an exception, just continue the execution (it will be catched)
                        {
                            var localizerKey = (ILocalizerKey)field.GetValue(null);
                            if (localizerKey != null)
                            {
                                entry = new LocalizerEntry(localizerKey);
                            }
                        }
                        else if (field.FieldType.GetInterfaces().Any(t => t.FullName == typeof(ILocalizerKey).FullName)) // If you break here with an exception, just continue the execution (it will be catched)
                        {
                            object localizerKey = field.GetValue(null);
                            if (localizerKey != null)
                            {
                                entry = new LocalizerEntry(localizerKey);
                            }
                        }
                    }
                    catch (FileNotFoundException)
                    {

                    }
                    
                    if (entry != null)
                        yield return entry;
                }
            }
        }
    }

    /// <summary>
    /// Information of the localization key
    /// </summary>
    public class LocalizerEntry : ILocalizedFormatKey
    {
        /// <summary>
        /// Invariant default values
        /// </summary>
        public string Default { get; }
        /// <summary>
        /// Required runtime parameters
        /// </summary>
        public string[] ParameterNames { get; }
        /// <summary>
        /// Name of the assembly where the key is created
        /// </summary>
        public string AssemblyName { get; }
        /// <summary>
        /// Localizer key
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Creates an instance
        /// </summary>
        /// <param name="localizerKey">The key</param>
        public LocalizerEntry(ILocalizerKey localizerKey)
        {
            if (localizerKey == null)
                throw new ArgumentNullException(nameof(localizerKey));
            Default = localizerKey.Default;
            AssemblyName = localizerKey.AssemblyName;
            Key = localizerKey.Key;
            if (localizerKey is ILocalizedFormatKey formatKey)
            {
                ParameterNames = formatKey.ParameterNames;
            }

            if (ParameterNames == null)
                ParameterNames = new string[0];
        }

        /// <summary>
        /// Creates an instance
        /// </summary>
        /// <param name="localizerKey">The key as reflected object</param>
        public LocalizerEntry(object localizerKey)
        {
            if (localizerKey == null)
                throw new ArgumentNullException(nameof(localizerKey));
            if (localizerKey.GetType().GetInterfaces().All(t => t.FullName != typeof(ILocalizerKey).FullName))
                throw new Exception($"Provided object {localizerKey} does not implement interface {typeof(ILocalizerKey).FullName}");
            Default = GetProperty<string>(nameof(ILocalizerKey.Default), localizerKey);
            AssemblyName = GetProperty<string>(nameof(ILocalizerKey.AssemblyName), localizerKey);
            Key = GetProperty<string>(nameof(ILocalizerKey.Key), localizerKey);
            if (localizerKey.GetType().GetInterfaces().Any(t => t.FullName == typeof(ILocalizedFormatKey).FullName))
            {
                ParameterNames = GetProperty<string[]>(nameof(ILocalizedFormatKey.ParameterNames), localizerKey);
            }

            if (ParameterNames == null)
                ParameterNames = new string[0];
        }

        T GetProperty<T>(string propertyName, object localizerKey)
        {
            Type type = localizerKey.GetType();
            PropertyInfo propertyInfo = type.GetProperty(propertyName);
            if (propertyInfo == null)
                throw new Exception($"Accessor { type.FullName } has no public property {propertyName}");
            if (propertyInfo.PropertyType != typeof(T))
                throw new Exception($"Accessor {type.FullName} has no public property {propertyName} with type { typeof(T).FullName }.");

            T result = (T) propertyInfo.GetValue(localizerKey);
            if (result == null)
                throw new Exception($"Accessor {type.FullName } return null for property {propertyName}");
            return result;
        }

    }
}
