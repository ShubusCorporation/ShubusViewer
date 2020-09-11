using System;
using System.Net;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using ExtendedData;

namespace ShubusViewer
{
    class AppUpdater
    {
        private const string urlDownload = "https://my.cloudme.com//superdim-007//ShubusViewer//ShubusViewer.zip"; //"http://shubuscorporation.webs.com/downloads";
        private const string infoURL = "http://shubuscorporation.webs.com/update.txt";

        private string getHtmlPageText()
        {
            string txt = String.Empty;
            WebRequest req = WebRequest.Create(infoURL);
            WebResponse resp = req.GetResponse();

            using (Stream stream = resp.GetResponseStream())
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    txt = sr.ReadToEnd();
                }
            }
            return txt;
        }

        public static string getCurrentVersionStr()
        {
            Version curVersion;

            try { curVersion = AssemblyName.GetAssemblyName(Assembly.Load("ShubusViewer").Location).Version; }
            catch (Exception) { return string.Empty; }

            return curVersion.ToString();
        }

        public void CheckForNewVersion()
        {
            string marker = "version=\"";
            string strInfo = this.getHtmlPageText();

            int start = strInfo.IndexOf(marker) + marker.Length;

            if (start < marker.Length)
            {
                MessageBox.Show(Constants.STR_UPDATEINFOWRONG_MSG, Constants.STR_APP_TITLE,
                   MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            int end = strInfo.IndexOf("\"", start);

            if (end < 0)
            {
                MessageBox.Show(Constants.STR_UPDATEINFOWRONG_MSG, Constants.STR_APP_TITLE,
                   MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Version curVersion = new Version(strInfo.Substring(start, end - start));
            Version myVersion = AssemblyName.GetAssemblyName(Assembly.Load("ShubusViewer").Location).Version;

            if (curVersion > myVersion)
            {
                if (MessageBox.Show(Constants.STR_UPDATE_APP, Constants.STR_APP_TITLE,
                   MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start(urlDownload);
                }
            }
            else
            {
                MessageBox.Show(Constants.STR_HAVELATEST_MSG, Constants.STR_APP_TITLE,
                   MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
