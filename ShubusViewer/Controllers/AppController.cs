using System;
using System.Windows.Forms;
using System.Text;
using DataSourceFacade;
using ExtendedData;
using System.Collections.Generic;

namespace StateMachine
{
    // Action for state machine public interface.
    public enum TypeGetObject
    {
        EGetEncoding = 0
    };


    public enum TypeAction
    {   EOpen = 0
      , ESave
      , ENew
      , ECopy
      , ELock
      , EEncode
      , EPreview
      , EModeHex
      , EModeDumpBin
      , EDumpUpdate
      , ETextForce
      , EOpenAsRecent
      , EModeText
      , EModeExplorer
      , ETextChanged
      , EUpdateCaption
      , EUpdateText
      , EExit
      , ELastAction
    };

    public class AppController
    {
        private enum AppState
        {   EFileReadOnly = 0
          , ETextChanged
          , ETextHasName
          , ETextIsEmpty
          , EFileNameExists
          , EAppLastState
        };

        Boolean[] appState;
        AppModelFacade appFacade;
        string currentFile = ""; // For state ETextHasName
        string nextFile = "";    // For state EFileNameExists
        public AppExtendedData sharedData;

        public delegate bool processAppNotification(TypeNotification notification);
        processAppNotification appEventCallBack;

        // After Encoding operation text changed flag should not be taken off.
        public bool isFileChanged
        {
            get { return this.appState[(int)AppState.ETextChanged]; }
        }

        public void setFileName(string file)
        {
            this.nextFile = file;
            //this.sharedData.basicInfo.curFile = this.nextFile;

            if ("" == file || file == Constants.STR_DEFAULT_FN)
            {
                appState[(int)AppState.EFileNameExists] = false;
            }
            else
            {
                appState[(int)AppState.EFileNameExists] = true;                
            }
        }

        public void updateTextState(bool empty)
        {
            this.appState[(int)AppState.ETextIsEmpty] = empty;
        }

        public void setAppNotificationHandler(processAppNotification callBack)
        {
            appEventCallBack = callBack;
        }

        public Encoding curEncoding
        {
            get { return this.appFacade.encoding; }
            set { this.appFacade.encoding = value; }
        }

        public void searchInGoogle(string query)
        {
            try { this.appFacade.webSearchInGoole(query); }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message, Constants.STR_APP_TITLE,
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        public void setBrowser(ZoomBrowser.MyBrowser webBrowser1)
        {
            this.appFacade.webInit(webBrowser1);
        }

        public void openURL(string url)
        {
            try { this.appFacade.webOpenURL(url); }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message, Constants.STR_APP_TITLE
                  , MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public bool searchInDump(string s, int start)
        {
            return this.appFacade.txtSearchInDump(s, start);
        }

        public TypeFileFormat getCurrentFileType()
        {
            // CR032
            return this.appFacade.getFileType();
        }

        private void updateCaption()
        {
            if (appState[(int)AppState.ETextChanged])
            {
                appEventCallBack(TypeNotification.ETextChanged);
            }
            else
            {
                appEventCallBack(TypeNotification.ETextRestored);
            }
        }

        private void setFiles(string cur, string next)
        {
            this.currentFile = cur;
            this.nextFile = next;

            if ("" == next || Constants.STR_DEFAULT_FN == next)
            {
                appState[(int)AppState.EFileNameExists] = false;
            }
            else
            {
                appState[(int)AppState.EFileNameExists] = true;
            }
            if ("" == cur || Constants.STR_DEFAULT_FN == cur)
            {
                appState[(int)AppState.ETextHasName] = false;
            }
            else
            {
                appState[(int)AppState.ETextHasName] = true;
            }
        }

        public void closePage()
        {
            this.appFacade.webClosePage();
        }

        private void updateData()
        {
            this.appEventCallBack(TypeNotification.ETextProvide);
            this.appFacade.txtUpdate();
        }

        private Boolean forgetCurrentText()
        {
            if (appState[(int)AppState.ETextChanged]
                && appState[(int)AppState.ETextHasName])
            {
                if (MessageBox.Show(Constants.STR_LOSTDATA_DLG, Constants.STR_APP_TITLE
                  , MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                {
                    return false;
                }
            }
            return true;
        }

        private void newFile()
        {
            if (forgetCurrentText())
            {
                appState[(int)AppState.ETextChanged] = false;
                this.appFacade.txtNewFile();
                this.setFiles(Constants.STR_DEFAULT_FN, "");
                appEventCallBack(TypeNotification.ETextRestored);
            }
        }

        private void copyFile()
        {
            if (forgetCurrentText())
            {
                appState[(int)AppState.ETextChanged] = true;
                this.appFacade.txtNewFile();
                this.setFiles(Constants.STR_DEFAULT_FN, "");
                appEventCallBack(TypeNotification.ETextProvide);
                appEventCallBack(TypeNotification.ENewDataReady);
                appEventCallBack(TypeNotification.ETextChanged);
            }
        }

        private void prepareNewNamedFile()
        {
            this.sharedData.fileType = TypeFileFormat.ESimpleText;
            this.setFiles(this.nextFile, "");
            appState[(int)AppState.EFileNameExists] = false;
            appState[(int)AppState.ETextHasName] = true;
        }

        private void openFile()
        {
            if (forgetCurrentText())
            {
                if (false == appState[(int)AppState.EFileNameExists])
                {
                    appEventCallBack(TypeNotification.EOpenDialog);
                }
                if (appState[(int)AppState.EFileNameExists]) // There's no 'else' !
                {
                    string tmpCurrent = this.sharedData.basicInfo.curFile;
                    this.sharedData.basicInfo.curFile = this.nextFile;

                    try
                    {
                        if (this.userPreferredEncoding())
                        {
                            this.appFacade.txtOpenWithoutDetect();
                        }
                        else this.appFacade.openFile();
                    }
                    catch (Exception e)
                    {
                        this.onOpenError(e, tmpCurrent);
                        return;
                    }
                    if (this.appFacade.changed)
                    {
                        appState[(int)AppState.ETextChanged] = false;
                    }
                    this.setFiles(this.nextFile, "");
                    appEventCallBack(TypeNotification.ETextRestored);
                    appEventCallBack(TypeNotification.EOnFileOpened);
                }
            }
        }

        // When starts from command line with -t argument.
        private void openAsTextForce()
        {
            //this.appState[(int)AppState.EFileNameExists] = false;
            this.sharedData.basicInfo.curFile = this.nextFile;

            try
            {
                if (userPreferredEncoding())
                {
                    this.appFacade.txtOpenWithoutDetect();
                }
                else this.appFacade.txtOpenFile();
            }
            catch (Exception e)            
            {
                this.onOpenError(e, Constants.STR_DEFAULT_FN);
                return;
            }
            if (this.appFacade.changed)
            {
                appState[(int)AppState.ETextChanged] = false;
            }
            this.setFiles(this.nextFile, "");
            appEventCallBack(TypeNotification.ETextRestored);
            appEventCallBack(TypeNotification.EOnFileOpened);
         }

        // Invoked from the Recent files menu.
        private void openAsTextWithoutDetect()
        {
            if (forgetCurrentText() == false)
            {
                this.setFiles(this.currentFile, "");
                return;
            }
            string tmpCurrent = this.sharedData.basicInfo.curFile;
            this.sharedData.basicInfo.curFile = this.nextFile;

            try { this.appFacade.txtOpenWithoutDetect(); }
            catch (Exception e)
            {
                this.onOpenError(e, tmpCurrent);
                return;
            }
            if (this.appFacade.changed)
            {
                appState[(int)AppState.ETextChanged] = false;
            }
            this.setFiles(this.nextFile, "");
            //appEventCallBack(TypeNotification.ETextRestored); // CR-059
            appEventCallBack(TypeNotification.EOnFileOpened);
        }


        private void openAsTextFromModeDialog()
        {
            string tmpCurrent = this.sharedData.basicInfo.curFile;

            try { this.appFacade.txtOpenWithoutDetect(); }
            catch (Exception e)
            {
                this.onOpenError(e, tmpCurrent);
                return;
            }
            if (this.appFacade.changed)
            {
                appState[(int)AppState.ETextChanged] = false;
            }
            this.setFiles(tmpCurrent, ""); // Otherwise 'Open' will sometimes fall after Mode dlg.
            appEventCallBack(TypeNotification.ETextRestored);
            appEventCallBack(TypeNotification.EOnFileOpened);
        }

        private void onOpenError(Exception e, string tmpCurrent)
        {
            // To prevent caption Text change to next file.
            bool bRecreate;
            { 
                string tmpNext = this.sharedData.basicInfo.curFile;
                this.sharedData.basicInfo.curFile = tmpCurrent;

                MessageBox.Show(e.Message, Constants.STR_APP_TITLE,
                   MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                this.sharedData.basicInfo.curFile = tmpNext;
                bRecreate = this.appEventCallBack(TypeNotification.ERemoveFromRecentList);
            }
            if (e is System.IO.FileNotFoundException)
            {
                bool dirExists = System.IO.Directory.Exists(Utils.Utils.getPath(this.sharedData.basicInfo.curFile));

                if (dirExists && bRecreate && appState[(int)AppState.ETextIsEmpty] && appState[(int)AppState.ETextHasName] == false)
                {
                    this.prepareNewNamedFile();
                }
                else
                {
                    this.setFiles(tmpCurrent, "");
                    this.sharedData.basicInfo.curFile = tmpCurrent;
                }
            }
            else
            {
                if (appState[(int)AppState.ETextHasName] == false)
                {
                    this.newFile();
                }
                else
                {
                    this.setFiles(tmpCurrent, "");
                    this.sharedData.basicInfo.curFile = tmpCurrent;
                }
            }
            this.updateCaption();
            this.appEventCallBack(TypeNotification.EOnFileOpened);
            return;
        }

        private bool userPreferredEncoding()
        {
            // To detect or not to detect?
            return !this.appEventCallBack(TypeNotification.ECheckForUserEncoding);
        }

        private void encodeFile()
        {
            try
            {
                this.appFacade.txtEncodeFile();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, e.Source
                  , MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void previewText()
        {
            try
            {
                this.appFacade.txtPreview();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, e.Source
                  , MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void saveFile()
        {
            if (appState[(int)AppState.ETextChanged])
            {
                if (false == appState[(int)AppState.ETextHasName]
                 && false == appState[(int)AppState.EFileNameExists])
                {
                    appEventCallBack(TypeNotification.ESaveAsDialog);
                }
                if (appState[(int)AppState.ETextHasName])
                {
                    try
                    {
                        this.appFacade.txtSaveFile(this.currentFile);
                    }
                    catch(Exception e)
                    {
                        MessageBox.Show(e.Message, Constants.STR_APP_TITLE,
                            MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        return;
                    }
                }
                else if (appState[(int)AppState.EFileNameExists])
                {
                    // Creating new file for current unnamed text.
                    try
                    {
                        this.setFiles(this.nextFile, "");
                        this.appFacade.txtSaveFile(this.currentFile);
                    }
                    catch (Exception ee)
                    {
                        MessageBox.Show(ee.Message, Constants.STR_APP_TITLE,
                            MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                        this.setFiles("", "");
                        return;
                    }
                }
                if (appState[(int)AppState.EFileNameExists]
                 || appState[(int)AppState.ETextHasName])
                {
                    appState[(int)AppState.ETextChanged] = false;
                    appEventCallBack(TypeNotification.ETextRestored);
                    appEventCallBack(TypeNotification.EOnFileSaved);
                }                
            }
        }

        private void exitApp()
        {
            if (appState[(int)AppState.ETextChanged])
            {
                DialogResult res = MessageBox.Show(Constants.STR_SAVEDATA_DLG, Constants.STR_APP_TITLE
                  , MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (res == DialogResult.Cancel)
                {
                    appEventCallBack(TypeNotification.EExitCancel);
                    return;
                }
                if (res == DialogResult.Yes)
                {
                    saveFile();
                }
            }
            appEventCallBack(TypeNotification.EExitApp);
        }

        private void textChanged()
        {
            if (appState[(int)AppState.ETextHasName] == false && appState[(int)AppState.ETextIsEmpty])
            {
                this.appState[(int)AppState.ETextChanged] = false;
            }
            else if (false == appState[(int)AppState.ETextChanged])
            {
                appState[(int)AppState.ETextChanged] = true;
                appState[(int)AppState.ETextIsEmpty] = false;
            }
        }

        private delegate void processTypeAction();
        private Dictionary<TypeAction, processTypeAction> doAction;

        private delegate object processTypeGetObject();
        private Dictionary<TypeGetObject, processTypeGetObject> doGetObject;

        public object processOperation(TypeGetObject action)
        {
            return doGetObject[action]();
        }

        public void processOperation(TypeAction action)
        {
            doAction[action]();

            if (this.appFacade.changed)
            {
                this.appFacade.changed = false; // !!!
                doAction[TypeAction.ELastAction]();
            }
        }

        private Encoding DetectEncoding()
        {
            if (!this.appState[(int)AppState.ETextIsEmpty])
            {
                this.appFacade.txtOpenFile();
            }
            return appFacade.encoding;
        }

        public AppController()
        {
            this.sharedData = new AppExtendedData();
            this.appFacade = new AppModelFacade(this.sharedData);
            this.appEventCallBack = arg => { return true; };
            this.appState = new Boolean[(int)AppState.EAppLastState];
            this.appState[(int)AppState.ETextIsEmpty] = true;

            doGetObject = new Dictionary<TypeGetObject,processTypeGetObject>()
            {
                { TypeGetObject.EGetEncoding, this.DetectEncoding }
            };

            doAction = new Dictionary<TypeAction, processTypeAction>()
            {
               { TypeAction.EOpen, this.openFile },
               { TypeAction.ESave, this.saveFile },
               { TypeAction.ENew, this.newFile },
               { TypeAction.ECopy, this.copyFile },
               { TypeAction.EEncode, this.encodeFile },
               { TypeAction.EPreview, this.previewText },
               { TypeAction.EModeHex, this.appFacade.txtMakeHexDump },
               { TypeAction.EModeDumpBin, this.appFacade.txtMakeBinDump },
               { TypeAction.EDumpUpdate, this.appFacade.txtUpdateDump },
               { TypeAction.ETextForce, this.openAsTextForce },
               { TypeAction.EOpenAsRecent, this.openAsTextWithoutDetect },
               { TypeAction.EModeText, this.openAsTextFromModeDialog },
               { TypeAction.ETextChanged, this.textChanged },
               { TypeAction.EUpdateCaption, this.updateCaption },
               { TypeAction.EUpdateText, this.updateData },
               { TypeAction.EModeExplorer, this.appFacade.webModeExplorer },
               { TypeAction.EExit, this.exitApp },
               { TypeAction.ELastAction, 
                   () => this.appEventCallBack(TypeNotification.ENewDataReady) }
            };
        }
    }
}