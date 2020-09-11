using System;
using System.Text;

using DataProcessor;
using WebDataProcessor;
using ExtendedData;
using System.Text.RegularExpressions;

namespace DataSourceFacade
{
    class AppModelFacade
    {
        private AppModel appModel;
        private AppWebModel appWebModel;
        private AppExtendedData sharedData;

        private readonly string strGame = "((~(\\w+)\\.uni)|(\\.unity3d)|(\\.swf)|(\\.dcr)|(\\.wrl)|(\\.wrz))$".ToLower();
        private readonly string strWeb = "((\\.htm(\\w{0,1}))|(\\.xml)|(\\.fb2)|(\\.mht)|(\\.hta))$".ToLower();

        private enum TypeDump
        {
            EDumpHex = 0, EDumpBin
        };
        TypeDump txtDump;

        public Encoding encoding
        {
            get { return this.appModel.encoding; }
            set { this.appModel.encoding = value; }
        }

        public AppModelFacade(AppExtendedData shData)
        {
            this.sharedData = shData;
            this.appModel = new AppModel(this.sharedData.basicInfo);
            this.appWebModel = new AppWebModel(this.sharedData);
        }

        public void webInit(ZoomBrowser.MyBrowser browser)
        {
            this.appWebModel.initModel(browser);
        }

        // Base interface;
        public bool changed
        {
           get 
           { 
               return (this.appModel.changed || this.appWebModel.changed);
           }
           set
           {
               if (value == false)
                   this.appModel.changed = this.appWebModel.changed = false;
           }
        }

        public TypeFileFormat getFileType() // CR032
        {
            string path = this.sharedData.basicInfo.curFile.ToLower();

            if (Regex.IsMatch(path, this.appWebModel.extPicture))
            {
                return TypeFileFormat.EWebPicture;
            }
            else if (Regex.IsMatch(path, this.strGame))
            {
                return TypeFileFormat.EWebGame;
            }
            else if (Regex.IsMatch(path, this.strWeb))
            {
                return TypeFileFormat.EExplorerText;
            }
            return TypeFileFormat.ESimpleText;
        }

        private void setContentType() // CR032
        {
            this.sharedData.fileType = this.getFileType();
        }

        public void openFile()
        {
            this.setContentType();

            if (this.sharedData.fileType == TypeFileFormat.ESimpleText)
            {
                this.appModel.openFile();
            }
            else
            {
                if (this.sharedData.fileType == TypeFileFormat.EExplorerText)
                {
                    this.appModel.openFile(); // Detects encoding and BOM length.
                }
                else
                {
                    this.appModel.clearData();
                }
                this.appWebModel.openFile();
            }
        }

        // Text file interface;
        public void txtOpenFile()
        {
            this.sharedData.fileType = TypeFileFormat.ESimpleText;
            this.appModel.openFile();
        }

        public void txtOpenWithoutDetect()
        {
            this.sharedData.fileType = TypeFileFormat.ESimpleText;
            this.appModel.openFileWithoutDetect();
        }

        public void txtEncodeFile()
        {
            this.appModel.encodeFile();
        }

        public void txtUpdateDump()
        {
            if (this.txtDump == TypeDump.EDumpHex)
                this.txtMakeHexDump();
            else
                this.txtMakeBinDump();
        }

        public bool txtSearchInDump(string s, int start)
        {
            return this.appModel.searchInDump(s, start);
        }

        public void txtMakeHexDump()
        {
            this.txtDump = TypeDump.EDumpHex;
            this.sharedData.fileType = TypeFileFormat.ESimpleText;
            this.sharedData.basicInfo.langRTL = false;
            this.appModel.makeHexDump();
        }

        public void txtMakeBinDump()
        {
            this.txtDump = TypeDump.EDumpBin;
            this.sharedData.fileType = TypeFileFormat.ESimpleText;
            this.sharedData.basicInfo.langRTL = false;
            this.appModel.makeBinDump();
        }

        public void txtNewFile()
        {
            this.sharedData.fileType = TypeFileFormat.ESimpleText;
            this.appModel.newFile();
        }

        public void txtSaveFile(string path)
        {
            this.appModel.saveFile(path);
        }

        public void txtUpdate()
        {
            this.appModel.updateData();
        }

        public void txtPreview()
        {
            this.appModel.previewBeforeSave();
        }

        // Web data interface
        public void setBrowser(ZoomBrowser.MyBrowser webBrowser1)
        {
            this.appWebModel.initModel(webBrowser1);
        }

        public void webOpenInBrowser()
        {
            this.appWebModel.openFile();
        }

        public void webClosePage()
        {
            this.appWebModel.closePage();
        }

        public void webOpenURL(string url)
        { 
            this.appWebModel.openURL(url);
        }

        public void webSearchInGoole(string url)
        {
            this.appWebModel.searchInGoogle(url);
        }

        public void webModeExplorer()       
        {
            this.setContentType();
            // Otherwise ESimpleText can remain.
            if (this.sharedData.fileType == TypeFileFormat.ESimpleText)
            {
                this.sharedData.fileType = TypeFileFormat.EExplorerText;
            }
            this.appWebModel.openFile();
        }
    }
}