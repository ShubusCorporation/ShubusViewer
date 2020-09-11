using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using ExtendedData;
using System.Windows.Forms;

namespace Utils
{
    public static class Utils
    {
        private static Regex rxEmail = new Regex("^[a-zA-Z0-9\\.-]+@[a-zA-Z0-9_-]+\\.[a-zA-Z]{2,6}$");

        public static bool checkEmail(string s)
        {
            return rxEmail.IsMatch(s);
        }

        public static string getExtension(string path)
        {
            return Regex.Match(path, "(?=\\.([^\\./\\\\]+)$)").Groups[1].Value.ToLower();
        }

        public static string getShortName(string path)
        {
            return Regex.Match(path, "(?=\\\\([^\\\\]+)$)").Groups[1].Value;
        }

        public static string getPath(string path)
        {
            return Regex.Match(path, "^([\\w]:.*)[\\\\]([^\\\\]+)$").Groups[1].Value;
        }

        public static void GetClipboardURL()
        {
            if (!Clipboard.ContainsText())
            {
                return;
            }
            string s = Clipboard.GetText();
            if (string.IsNullOrEmpty(s) || !s.Contains('%')) return;

            s = getURL(s);

            if (!string.IsNullOrEmpty(s))
            {
                for (int j = 0; j < 10; j++)
                {
                    string s1 = System.Web.HttpUtility.UrlDecode(s);

                    if (string.Equals(s1, s, StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }
                    else s = s1;
                }
                Clipboard.SetText(s);
            }
        }

        static Regex rx1 = new Regex(string.Format("(?=(u|url)=(http(%253A%252F%252F|%3A%2F%2F|:\\/\\/)([{0}]+)$))"
           , rgxScr("*/\\:&?~=+()%_.a-z0-9-"))
           , RegexOptions.Compiled | RegexOptions.IgnoreCase);

        static Regex rx2 = new Regex(string.Format("(?=(u|url)=(~[{0}]+~)$)", rgxScr("+*=_/a-z0-9-"))
        , RegexOptions.Compiled | RegexOptions.IgnoreCase);

        static Regex rx3 = new Regex(string.Format("(?=(u|url)=([{0}]+)$)", rgxScr("*/\\:&?~=+()%_.a-z0-9-"))
        , RegexOptions.Compiled | RegexOptions.IgnoreCase);

        static Regex rx4 = new Regex("aHR0cDo[0-9a-zA-Z]+", RegexOptions.Compiled);

        private static string getURL(string path)
        {
            string s = rx1.Match(path).Groups[2].Value;

            if (s.Length == 0)
                s = rx2.Match(path).Groups[2].Value;
            if (s.Length == 0)
                s = rx3.Match(path).Groups[2].Value;

            if (s.Length > 0)
            {
                if (rx4.IsMatch(s))
                {
                    s = Utils.getStringFromBase64(string.Format("{0}{1}"                        
                        , rx4.Match(s).Groups[0].Value
                        , "=="));
                }
            }
            if (string.IsNullOrEmpty(s) || s.Length < 4) return path;
            return s;
        }

        public static string getStringFromBase64(string s)
        {
            try
            {
                return System.Text.ASCIIEncoding.UTF8.GetString(System.Convert.FromBase64String(s));
            }
            catch 
            {
                throw;
            }
        }

        public static string GetShortEncodingName(System.Text.Encoding aEnc)
        {
            string name1 = "№ " + aEnc.CodePage.ToString();
            string name2 = aEnc.HeaderName.ToUpper();
            string name;

            if (name2.Length <= Constants.STR_ENCODING.Length) name = name2;
            else name = (name1.Length < name2.Length) ? name1 : name2;

            return name;
        }

        public static string rgxScr(string arg)
        {
            return string.IsNullOrEmpty(arg) ? string.Empty : Regex.Escape(arg);
        }

        public static bool checkBase64(string arg)
        {
            return Regex.IsMatch(arg, string.Format("^([{0}]+)$", rgxScr("+*=_/a-zA-Z0-9-")));
        }

        public static bool checkEncodedURL(string arg)
        {
            return Regex.IsMatch(arg, string.Format("([{0}]+)", rgxScr("=+()%_.a-zA-Z0-9-")));
        }

        public static bool checkURL(string arg)
        {
            return Regex.IsMatch(arg, string.Format("^([{0}]+)$", rgxScr("/\\:&?~=+()%_.a-zA-Z0-9-")));
        }

        // http://social.msdn.microsoft.com/Forums/en/csharpgeneral/thread/15514c1a-b6a1-44f5-a06c-9b029c4164d7
        public static int IndexOf(byte[] searchIn, byte[] searchFor, int start)
        {
            try
            {
                if ((searchIn != null) && (searchFor != null))
                {
                    if (searchFor.Length > searchIn.Length) return 0;

                    for (int i = start; i < searchIn.Length; i++)
                    {
                        int startIndex = i;
                        bool match = true;

                        for (int j = 0; j < searchFor.Length; j++)
                        {
                            if (searchIn[startIndex] != searchFor[j])
                            {
                                match = false;
                                break;
                            }
                            else if (startIndex < searchIn.Length)
                            {
                                startIndex++;
                            }
                        }
                        if (match)
                            return startIndex - searchFor.Length;
                    }
                }
            }
            catch { return -1; }
            return -1;
        }

        private static string _cfgPath = "";

        public static string CfgPath
        {
            get
            {
                if (_cfgPath.Length < 1)
                {
                    // http://stackoverflow.com/questions/884260/how-can-i-watch-the-user-config-file-and-reload-the-settings-when-it-changes
                    // The following code will return the path to the user.config file. You need to add a reference to System.Configuration.dll
                    // Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);

                    string sc = "\\Shubus_Corporation\\ShubusViewer";
                    _cfgPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + sc;

                    try
                    {
                        if (Directory.Exists(_cfgPath) == false)
                        {
                            Directory.CreateDirectory(_cfgPath);
                        }
                    }
                    catch (Exception) { }
                }
                return _cfgPath;
            }
        }

        public static void ShowHelp()
        {
            string chmName = "ShubusViewer.chm";
            string exePath = System.Windows.Forms.Application.ExecutablePath;
            string chmPath = exePath.Substring(0, exePath.LastIndexOf('\\') + 1) + chmName;

            try
            {
                System.Diagnostics.Process.Start(chmPath);
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message,
                    ExtendedData.Constants.STR_APP_TITLE,
                    MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }
    }
}