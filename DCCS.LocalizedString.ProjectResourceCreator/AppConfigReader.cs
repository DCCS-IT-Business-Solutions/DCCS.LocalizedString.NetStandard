using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace DCCS.LocalizedString.ProjectResourceCreator
{
    static class AppConfigReader
    {
        public static string GetString(string key, string initialValue)
        {
            return ReadValue(key);
        }

        public static bool GetBool(string key, bool initialValue)
        {
            var value = ReadValue(key);
            return (value != null) ? Convert.ToBoolean(value) : initialValue;
        }

        private static string ReadValue(string key)
        {
            try
            {
                return ConfigurationManager.AppSettings[key];
            }
            catch
            {
                return null;
            }
        }

    }
}
