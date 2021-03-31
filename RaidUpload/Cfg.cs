using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections;

// a simple helper class to read/store/increment values in the .config file

namespace RaidUtil
{
    public class Cfg
    {
        private static byte[] entropy = System.Text.Encoding.Unicode.GetBytes("FuFuSaltySaltyFuDogSaltyFu");

        public static List<string> getSections()
        {
            List<string> ret = new List<string>();
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            KeyValueConfigurationCollection settings = config.AppSettings.Settings;

            foreach (KeyValueConfigurationElement e in settings)
            {
                if (e.Key.Contains("_"))
                {
                    ret.Add(e.Key.Split('_')[0]);
                }
            }
            return ret;
        }

        public static Dictionary<string, string> getList(string partKey)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            KeyValueConfigurationCollection settings = config.AppSettings.Settings;

            Dictionary<string, string> d = new Dictionary<string, string>();

            foreach (KeyValueConfigurationElement e in settings)
            {
                if (e.Key.StartsWith(partKey))
                {
                    d.Add(e.Key.Substring(partKey.Length + 1), e.Value);
                }
            }
            return d;
        }

        public static void setList(string partKey, Dictionary<string,string> values)
        {
            foreach (KeyValuePair<string,string> kvp in values) {
                set(partKey + "_" + kvp.Key, kvp.Value);
            }
        }

        public static string get(string key)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            KeyValueConfigurationCollection settings = config.AppSettings.Settings;
            string v;
            try
            {
                v = settings[key].Value;
                return v;
            }
            catch (Exception e)
            {
                return String.Empty;
            }
        }

        public static string getEncrypted(string key)
        {
            var eVal = get(key);
            if (String.IsNullOrEmpty(eVal)) {
                return String.Empty;
            }
            try
            {
                byte[] decrypted = System.Security.Cryptography.ProtectedData.Unprotect(
                    Convert.FromBase64String(eVal),
                    entropy,
                    System.Security.Cryptography.DataProtectionScope.CurrentUser
                );
                return System.Text.Encoding.UTF8.GetString(decrypted);
            }
            catch (System.Security.Cryptography.CryptographicException e)
            {
                return String.Empty;
            }
        }

        public static void delete(string key)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            KeyValueConfigurationCollection settings = config.AppSettings.Settings;
            if (settings[key] != null)
            {
                settings.Remove(key);
            }
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(config.AppSettings.SectionInformation.Name);
        }


        public static void set(string key, object value)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            KeyValueConfigurationCollection settings = config.AppSettings.Settings;
            if (config.AppSettings.Settings[key] == null)
            {
                settings.Add(key, value.ToString());
            }
            else
            {
                settings[key].Value = value.ToString();
            }

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(config.AppSettings.SectionInformation.Name);
        }

        public static void setEncrypted(string key, object value)
        {
            byte[] bVal = System.Text.Encoding.UTF8.GetBytes(value.ToString());

            byte[] encrypted = System.Security.Cryptography.ProtectedData.Protect(
                bVal,
                entropy,
                System.Security.Cryptography.DataProtectionScope.CurrentUser
            );
            set(key, Convert.ToBase64String(encrypted));
        }

        public static int increment(string key, int startFrom = 0)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            KeyValueConfigurationCollection settings = config.AppSettings.Settings;

            string v = settings[key].Value;
            int test;

            try
            {
                test = int.Parse(v);
            }
            catch (Exception e)
            {
                if (String.IsNullOrEmpty(v))
                {
                    test = startFrom - 1;
                }
                else
                {
                    return -1;
                }
            }

            test++;
            settings[key].Value = test.ToString();

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(config.AppSettings.SectionInformation.Name);

            return test;
        }
    }
}
