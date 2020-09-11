using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using DataProcessor;
using ExtendedData;
using BaseAbstractModel;
using System.Drawing;
using System.Text.RegularExpressions;

namespace WebDataProcessor
{
    // http://www.codeproject.com/Articles/17561/Programmatically-adding-attachments-to-emails-in-C
    public class AppWebModel : AppAbstractModel
    {
        private bool ch;
        private ZoomBrowser.MyBrowser webBrowser;
        private AppExtendedData mySharedData;
        private readonly string extUnity3D = "((~\\w+\\.uni)|(\\.unity3d))$".ToLower();
        private readonly string extDCR = "(.dcr)$";
        private const string replaceFile = "MultimediaFile";
        private const string swVersion = "**";

        // It's public for using by AppModelFacade.
        public readonly string extPicture =  "((\\.jpg)|(\\.jpeg)|(\\.bmp)|(\\.gif)|(\\.png)|(\\.ico)|(\\.emf)|(\\.wmf)|(\\.webp))$".ToLower();
        private readonly string extCortona3D = "((\\.wrl)|(\\.wrz))$".ToLower();
        private readonly string[] headers = { "www.", "http:", "https:", "about:", "ftp:" };
        private const string localHost = "localhost";
        private const string localIP = "127.0.0.1";
        private const string browserHome = "about:blank";
        private const string searchRequestGoogle = "http://www.google.com/#newwindow=1&q=";

        byte[] scriptBytes;
        private const string script = "PGhlYWQ+DQo8c2NyaXB0IGxhbmd1YWdlID0gIkphdmFTY3JpcHQiPg0KDQp2YXIgdGltZXI7DQoNCmZ1bmN0aW9uIGVudHJ5KGFyZykNCnsNCiAgICB2YXIgb2JqZWN0ID0gZG9jdW1lbnQuY3JlYXRlRWxlbWVudCgnZGl2Jyk7DQogICAgdmFyIGRvY0JvZHkgPSBkb2N1bWVudC5nZXRFbGVtZW50c0J5VGFnTmFtZSgnYm9keScpWzBdOw0KDQogICAgb2JqZWN0LmlkID0gInBpYyI7DQogICAgb2JqZWN0LmlubmVySFRNTCA9ICc8aW1nIHNyYz0ib2xkUGF0aCI+JzsNCiAgICBvYmplY3Quc3R5bGUuY3NzVGV4dCA9ICJQT1NJVElPTjogYWJzb2x1dGUiOw0KICAgIGRvY0JvZHkuYXBwZW5kQ2hpbGQob2JqZWN0KTsNCiAgICB6b29tKCk7DQp9DQoNCmZ1bmN0aW9uIHpvb20oKQ0Kew0KICAgIHZhciBvYmplY3QgPSBkb2N1bWVudC5nZXRFbGVtZW50QnlJZCgicGljIik7DQogICAgdmFyIHBpY1dpZHRoID0gb2JqZWN0LmNsaWVudFdpZHRoOw0KICAgIHZhciB3d2lkdGggPSBkb2N1bWVudC5ib2R5LmNsaWVudFdpZHRoOw0KICAgIHZhciB3aGVpZ2h0ID0gZG9jdW1lbnQuYm9keS5jbGllbnRIZWlnaHQ7DQogICAgdmFyIHBpY0hlaWdodCA9IG9iamVjdC5jbGllbnRIZWlnaHQ7DQoNCiAgICBvYmplY3Quc3R5bGUucGl4ZWxMZWZ0ID0gKHd3aWR0aCA+IHBpY1dpZHRoKSA/ICh3d2lkdGggLSBwaWNXaWR0aCkgLyAyIDogMDsNCiAgICBvYmplY3Quc3R5bGUucGl4ZWxUb3AgID0gKHdoZWlnaHQgPiBwaWNIZWlnaHQpID8gICh3aGVpZ2h0IC0gcGljSGVpZ2h0KSAvIDIgOiAwOw0KICAgIHRpbWVyID0gc2V0VGltZW91dCgnem9vbSgpJywgMSk7DQp9DQoNCjwvc2NyaXB0Pg0KPC9oZWFkPg0KDQo8c3R5bGU+DQoNCiAgICBib2R5DQogICAgew0KICAgICAgIGJhY2tncm91bmQtY29sb3I6ICM2OTY5Njk7DQogICAgfQ0KDQo8L3N0eWxlPg0KDQo8Ym9keSBPbkxvYWQgPSAnZW50cnkoKTsnPjwvYm9keT4NCg0K";

        byte[] unityBytes;
        // src="http://webplayer.unity3d.com/download_webplayer-3.x/3.0/uo/UnityObject.js"
        private const string unityScript = "PD94bWwgdmVyc2lvbj0iMS4wIj8+DQo8IURPQ1RZUEUgSFRNTCBQVUJMSUMgIi0vL1czQy8vRFREIEhUTUwgNC4wMSBUcmFuc2l0aW9uYWwvL0VOIj4NCjxodG1sIHhtbG5zPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5L3hodG1sIj4NCjxoZWFkPg0KDQo8c3R5bGU+DQpib2R5IHttYXJnaW46IDA7IHBhZGRpbmc6MDsgYm9yZGVyOjA7IG92ZXJmbG93OiBoaWRkZW47IGJhY2tncm91bmQtY29sb3I6IGJsYWNrfQ0KZGl2Lm1pc3NpbmcgaW1nIHtib3JkZXItd2lkdGg6IDBweDt9DQoNCmRpdi5taXNzaW5nIGEgewloZWlnaHQ6IDUwJTsNCgkJcG9zaXRpb246IHJlbGF0aXZlOw0KCQl0b3A6IC0zMXB4OyB9DQo8L3N0eWxlPg0KDQo8c2NyaXB0Pg0KZnVuY3Rpb24gc3RhcnQoKQ0Kew0KICAgIHZhciBvYmplY3QgPSBkb2N1bWVudC5nZXRFbGVtZW50QnlJZCgnVW5pdHlNaXNzaW5nJyk7DQogICAgdmFyIHd3aWR0aCA9IGRvY3VtZW50LmJvZHkuY2xpZW50V2lkdGg7DQogICAgdmFyIHdoZWlnaHQgPSBkb2N1bWVudC5ib2R5LmNsaWVudEhlaWdodDsNCiAgICB2YXIgcGljV2lkdGggPSAxOTM7DQogICAgdmFyIHBpY0hlaWdodCA9IDYzOw0KICAgIG9iamVjdC5zdHlsZS5waXhlbExlZnQgPSAod3dpZHRoID4gcGljV2lkdGgpID8gKHd3aWR0aCAtIHBpY1dpZHRoKSAvIDIgOiAwOw0KICAgIG9iamVjdC5zdHlsZS5waXhlbFRvcCAgPSAod2hlaWdodCA+IHBpY0hlaWdodCkgPyAgKHdoZWlnaHQgLSBwaWNIZWlnaHQpIC8gMiA6IDA7DQp9DQo8L3NjcmlwdD4NCg0KPC9oZWFkPg0KDQo8Ym9keSBPbkxvYWQ9InN0YXJ0KCk7IiBPblJlc2l6ZT0ic3RhcnQoKTsiPg0KDQo8b2JqZWN0IGlkPSJVbml0eU9iamVjdCIgY2xhc3NpZD0iY2xzaWQ6NDQ0Nzg1RjEtREU4OS00Mjk1LTg2M0EtRDQ2QzNBNzgxMzk0Ig0KICAgIHN0eWxlPSJ3aWR0aDogMTAwJTsgaGVpZ2h0OiAxMDAlOyBwb3NpdGlvbjphYnNvbHV0ZTsgbGVmdDowcHg7IHRvcDowcHgiDQogICAgY29kZWJhc2U9Imh0dHA6Ly93ZWJwbGF5ZXIudW5pdHkzZC5jb20vZG93bmxvYWRfd2VicGxheWVyL1VuaXR5V2ViUGxheWVyLmNhYiN2ZXJzaW9uPTIsMCwwLDAiPg0KICAgIDxwYXJhbSBuYW1lPSJzcmMiIHZhbHVlPSJNdWx0aW1lZGlhRmlsZSIgLz4NCiAgICA8ZW1iZWQgaWQ9IlVuaXR5RW1iZWQiIHNyYz0iTXlEYXRhRmlsZS51bml0eTNkIiB3aWR0aD0xMDAlIGhlaWdodD0xMDAlDQogICAgdHlwZT0iYXBwbGljYXRpb24vdm5kLnVuaXR5IiBwbHVnaW5zcGFnZT0iaHR0cDovL3d3dy51bml0eTNkLmNvbS91bml0eS13ZWItcGxheWVyLTIueCIgLz4NCg0KPGRpdiBpZD0iVW5pdHlNaXNzaW5nIiBjbGFzcz0ibWlzc2luZyIgU1RZTEU9InBvc2l0aW9uOmFic29sdXRlIj4NCiAgICA8YSBocmVmPSJodHRwOi8vdW5pdHkzZC5jb20vd2VicGxheWVyLyIgdGl0bGU9IlVuaXR5IFdlYiBQbGF5ZXIuIEluc3RhbGwgbm93ISI+DQogICAgICAgIDxpbWcgYWx0PSJVbml0eSBXZWIgUGxheWVyLiBJbnN0YWxsIG5vdyEiIHNyYz0iaHR0cDovL3dlYnBsYXllci51bml0eTNkLmNvbS9pbnN0YWxsYXRpb24vZ2V0dW5pdHkucG5nIiB3aWR0aD0iMTkzIiBoZWlnaHQ9IjYzIiAvPg0KICAgIDwvYT4NCjwvZGl2Pg0KDQo8L2JvZHk+DQo8L2h0bWw+";

        byte[] dcrBytes;
        // http://drupal.org/node/781008
        // http://www.experts-exchange.com/Web_Development/Software/Macromedia_Director_Video_Softwa/Q_21839106.html
        private const string dcrScript = "PG9iamVjdCBjbGFzc2lkPSJjbHNpZDoxNjZCMUJDQS0zRjlDLTExQ0YtODA3NS00NDQ1NTM1NDAwMDAiIApjb2RlYmFzZT0iaHR0cDovL2Rvd25sb2FkLm1hY3JvbWVkaWEuY29tL3B1Yi9zaG9ja3dhdmUvY2Ficy9kaXJlY3Rvci9zdy5jYWIiCmhlaWdodD0xMDAlIHdpZHRoPTEwMCU+Cgo8cGFyYW0gbmFtZT0ic3dSZW1vdGUiIHZhbHVlPSJzd1NhdmVFbmFibGVkPSd0cnVlJyBzd1ZvbHVtZT0ndHJ1ZScgc3dSZXN0YXJ0PSd0cnVlJyBzd1BhdXNlUGxheT0ndHJ1ZScKIHN3RmFzdEZvcndhcmQ9J3RydWUnIHN3Q29udGV4dE1lbnU9J3RydWUnIj4KCjxwYXJhbSBuYW1lPSJQbGF5ZXJWZXJzaW9uIiB2YWx1ZT0iKioiPgo8cGFyYW0gbmFtZT0ic3dTdHJldGNoU3R5bGUiIHZhbHVlPSJtZWV0Ij4KPHBhcmFtIG5hbWU9ImJnQ29sb3IiIHZhbHVlPSIjMDAwMDAwIj4KPHBhcmFtIG5hbWU9InNyYyIgdmFsdWU9Ik11bHRpbWVkaWFGaWxlIj4KCjxlbWJlZCBzcmM9Ik11bHRpbWVkaWFGaWxlIgpiZ2NvbG9yPSIjMDAwMDAwIiBzd3JlbW90ZT0ic3dTYXZlRW5hYmxlZD0ndHJ1ZScgc3dWb2x1bWU9J3RydWUnIHN3UmVzdGFydD0ndHJ1ZScgc3dQYXVzZVBsYXk9J3RydWUnIHN3RmFzdEZvcndhcmQ9J3RydWUnCnN3Q29udGV4dE1lbnU9J3RydWUnIiBzd3N0cmV0Y2hzdHlsZT0ibWVldCIgdHlwZT0iYXBwbGljYXRpb24veC1kaXJlY3RvciIgCnBsdWdpbnNwYWdlPSJodHRwOi8vd3d3Lm1hY3JvbWVkaWEuY29tL3Nob2Nrd2F2ZS9kb3dubG9hZC8iIGhlaWdodD0xMDAlIHdpZHRoPTEwMCU+Cgo8L29iamVjdD4=";

        byte[] wrlzBytes;
        private const string wrlzScript = "PD94bWwgdmVyc2lvbj0iMS4wIj8+CjwhRE9DVFlQRSBIVE1MIFBVQkxJQyAiLS8vVzNDLy9EVEQgSFRNTCA0LjAxIFRyYW5zaXRpb25hbC8vRU4iPgo8aHRtbCB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMTk5OS94aHRtbCI+CjxoZWFkPgoKPHN0eWxlPgpib2R5IHttYXJnaW46IDA7IHBhZGRpbmc6MDsgYm9yZGVyOjA7IG92ZXJmbG93OiBoaWRkZW47IGJhY2tncm91bmQtY29sb3I6IGJsYWNrfQpkaXYubWlzc2luZyBpbWcge2JvcmRlci13aWR0aDogMHB4O30KCmRpdi5taXNzaW5nIGEgewloZWlnaHQ6IDUwJTsKCQlwb3NpdGlvbjogcmVsYXRpdmU7CgkJdG9wOiAtMzFweDsgfQo8L3N0eWxlPgoKPHNjcmlwdD4KZnVuY3Rpb24gc3RhcnQoKQp7CiAgICB2YXIgb2JqZWN0ID0gZG9jdW1lbnQuZ2V0RWxlbWVudEJ5SWQoJ0NvcnRvbmEzRE1pc3NpbmcnKTsKICAgIHZhciB3d2lkdGggPSBkb2N1bWVudC5ib2R5LmNsaWVudFdpZHRoOwogICAgdmFyIHdoZWlnaHQgPSBkb2N1bWVudC5ib2R5LmNsaWVudEhlaWdodDsKICAgIHZhciBwaWNXaWR0aCA9IDI5MjsKICAgIHZhciBwaWNIZWlnaHQgPSA1NDsKICAgIG9iamVjdC5zdHlsZS5waXhlbExlZnQgPSAod3dpZHRoID4gcGljV2lkdGgpID8gKHd3aWR0aCAtIHBpY1dpZHRoKSAvIDIgOiAwOwogICAgb2JqZWN0LnN0eWxlLnBpeGVsVG9wICA9ICh3aGVpZ2h0ID4gcGljSGVpZ2h0KSA/ICAod2hlaWdodCAtIHBpY0hlaWdodCkgLyAyIDogMDsKfQo8L3NjcmlwdD4KCjwvaGVhZD4KCjxib2R5IE9uTG9hZD0ic3RhcnQoKTsiIE9uUmVzaXplPSJzdGFydCgpOyI+Cgo8T0JKRUNUIGlkPSJDT1JUT05BM0RvYmplY3QiIGNsYXNzaWQ9IkNMU0lEOjg2QTg4OTY3LTdBMjAtMTFkMi04RURBLTAwNjAwODE4RURCMSIKICAgIHN0eWxlPSJ3aWR0aDogMTAwJTsgaGVpZ2h0OiAxMDAlOyBwb3NpdGlvbjphYnNvbHV0ZTsgbGVmdDowcHg7IHRvcDowcHgiCiAgICAKICAgIGNvZGViYXNlPSJodHRwOi8vd3d3LmNvcnRvbmEzZC5jb20vYmluL2NvcnRvbmEzZC5jYWIjVmVyc2lvbj02LDAsMCwxNzkiPgogICAgCiAgICA8UEFSQU0gTkFNRT0iU1JDIiBWQUxVRT0iTXVsdGltZWRpYUZpbGUiPgogICAgPFBBUkFNIE5BTUU9IlZSTUxfQkFDS0dST1VORF9DT0xPUiIgVkFMVUU9IiM2OTY5NjkiPgogICAgPFBBUkFNIE5BTUU9IlZSTUxfREFTSEJPQVJEIiBWQUxVRT0iZmFsc2UiPgogICAgPFBBUkFNIE5BTUU9IlZSTUxfU1BMQVNIU0NSRUVOIiBWQUxVRT0iZmFsc2UiPgogICAgPFBBUkFNIE5BTUU9IkNPTlRFWFRNRU5VIiBWQUxVRT0iZmFsc2UiPgo8L09CSkVDVD4KCjxkaXYgaWQ9IkNvcnRvbmEzRE1pc3NpbmciIGNsYXNzPSJtaXNzaW5nIiBTVFlMRT0icG9zaXRpb246YWJzb2x1dGUiPgogICAgPGEgaHJlZj0iaHR0cDovL3d3dy5jb3J0b25hM2QuY29tL2luc3RhbGwuYXNweCIgdGl0bGU9Ikluc3RhbGwgQ29ydG9uYTNEIFZpZXdlciI+CiAgICAgICAgPGltZyBhbHQ9Ikluc3RhbGwgQ29ydG9uYTNEIFZpZXdlciIgc3JjPSJodHRwOi8vd3d3LmNvcnRvbmEzZC5jb20vY29ydG9uYTNkL2ZpbGVzLzdkLzdkM2NiMDA5LThlMDQtNGQ0YS05YTM2LTk0YzAwZTBhZTAwNS5qcGciIHdpZHRoPSIyOTIiIGhlaWdodD0iNTQiIC8+IDwvYT4KPC9kaXY+Cgo8L2JvZHk+CjwvaHRtbD4=";

        private enum TypeContent
        {
            EPicture = 0
          , EDocument
          , EUnity3D
          , EAdobeDirector
          , ECortona3D
        };

        public AppWebModel(AppExtendedData aData)
        {
            this.mySharedData = aData;
        }

        public void initModel(ZoomBrowser.MyBrowser browserServer)
        {
            this.webBrowser = browserServer;
        }

        private TypeContent getContentType(string path)
        {
            path = path.ToLower();

            if (Regex.IsMatch(path, extUnity3D))
            { 
                return TypeContent.EUnity3D;
            }
            else if (Regex.IsMatch(path, this.extDCR))
            {
                return TypeContent.EAdobeDirector;
            }
            else if (Regex.IsMatch(path, this.extPicture))
            {
                return TypeContent.EPicture;
            }
            else if (Regex.IsMatch(path, this.extCortona3D))
            {
                return TypeContent.ECortona3D;
            }               
            return TypeContent.EDocument;
        }

        public override void openFile()
        {
            string path = this.mySharedData.basicInfo.curFile;

            if (File.Exists(path) == false)
                throw new FileNotFoundException(path + Constants.STR_FILENOTEXISTS_MSG);

            TypeContent type = this.getContentType(path);

            if (type != TypeContent.EDocument)
                path = path.Replace("\\", "\\\\");

            if (type == TypeContent.EPicture)
            {                
                if (this.scriptBytes == null)
                    this.scriptBytes = System.Convert.FromBase64String(script);

                string str = System.Text.ASCIIEncoding.UTF8.GetString(this.scriptBytes);
                str = str.Replace("oldPath", path);
                webBrowser.DocumentText = str;
            }
            else if (type == TypeContent.EUnity3D)
            {
                if (this.unityBytes == null)
                    this.unityBytes = System.Convert.FromBase64String(unityScript);

                string str = System.Text.ASCIIEncoding.UTF8.GetString(this.unityBytes);
                str = str.Replace(replaceFile, path);
                webBrowser.DocumentText = str;
            }
            else if (type == TypeContent.EAdobeDirector)
            {
                if (this.dcrBytes == null)
                    this.dcrBytes = System.Convert.FromBase64String(dcrScript);

                string str = System.Text.ASCIIEncoding.UTF8.GetString(this.dcrBytes);
                str = str.Replace(replaceFile, path);
                str = str.Replace(swVersion, this.mySharedData.swPlayerVersion);
                webBrowser.DocumentText = str;
            }
            else if (type == TypeContent.ECortona3D)
            {
                if (this.wrlzBytes == null)
                    this.wrlzBytes = System.Convert.FromBase64String(wrlzScript);

                string str = System.Text.ASCIIEncoding.UTF8.GetString(this.wrlzBytes);
                str = str.Replace(replaceFile, path);
                webBrowser.DocumentText = str;                
            }
            else if (type == TypeContent.EDocument)
            {
                this.webBrowser.Navigate("file:////" + path);
            }
            this.ch = true;
        }

        private bool stateClosing = false;

        public void closePage()
        {
            if (this.stateClosing) return;

            this.webBrowser.DocumentCompleted += this.webBrowser_DocumentCompleted;
            this.webBrowser.Navigated += this.webBrowser_Navigated;
            this.stateClosing = true;
            this.webBrowser.Navigate(browserHome);
        }

        private void webBrowser_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            this.webBrowser.Navigated -= this.webBrowser_Navigated;
        }

        private void webBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            this.webBrowser.DocumentCompleted -= this.webBrowser_DocumentCompleted;
            this.webBrowser.Visible = false;
            this.stateClosing = false;
        }

        public void openURL(string url)
        {           
            bool done = false;
            int startInd = 0;

            if (url.Length < 1) return;

            while (startInd < url.Length && !char.IsLetterOrDigit(url[startInd++])) { }
            url = url.Substring(--startInd);

            if (url.IndexOf(localHost, StringComparison.CurrentCultureIgnoreCase) != -1)
            {
                url = url.Replace(localHost, localIP);
            }
            if (char.IsDigit(url[0])) // IP: DDD.DDD.DDD
            {
                url = "http://" + url;
            }
            else
            {
                foreach (string s in this.headers)
                {
                    try
                    {
                        if (url.Substring(0, s.Length) == s)
                        {
                            done = true;
                            break;
                        }
                    }
                    catch (Exception) { }
                }
                if (done == false)
                    url = "www." + url;
            }
            System.Diagnostics.Process.Start(url); // Handle exception in the controller.
        }

        public void searchInGoogle(string aQuery)
        {
            aQuery = System.Web.HttpUtility.UrlEncode(aQuery);
            System.Diagnostics.Process.Start(searchRequestGoogle + aQuery);
        }

        public override bool changed
        {
            get
            { 
                bool temp = this.ch;
                this.ch = false;
                return temp;
            }
            set { this.ch = value; }
        }
    }
}