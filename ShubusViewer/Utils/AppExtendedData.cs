using SharedData;

namespace ExtendedData
{
    public enum TypeFileFormat
    {
        ESimpleText = 0,
        EExplorerText,
        EWebGame,
        EWebPicture,
        EOther
    };

    public enum TypeNotification
    {
        ESaveAsDialog = 0
      , EOpenDialog
      , EOnFileSaved
      , EOnFileOpened
      , ERemoveFromRecentList
      , ETextChanged
      , ETextRestored
      , EHorizontalScroll
      , ENewDataReady
      , ETextModeRTL
      , ETextModeLTR
      , ETextLock
      , ETextUnlock
      , ETextProvide
      , ECheckForUserEncoding
      , ESwitchFromTextToBrowser
      , EExitApp
      , EExitCancel
      , ELastNotification
      , EDisableChangedNotification
      , EEnableChangedNotification
    };

    public class AppExtendedData
    {
        public ModelInitData basicInfo;
        public TypeFileFormat fileType;
        public string swPlayerVersion = "11"; // Adobe ShockWave player version.

        public AppExtendedData()
        {
            this.basicInfo = new ModelInitData();
        }
    }

    public struct SRecentTag
    {
        public int sel; // Current selection in the file.
        public int encoding; // User preferred encoding for recent file.
    }

    public class Constants
    {
        public const string STR_APP_TITLE = "Shubus Viewer";
        public const string STR_LOCK = "Lock";
        public const string STR_UNLOCK = "Unlock";
        public const string STR_ENCODING = "Encoding";
        public const string STR_UNKNOWN = "Some error is occured.";
        public const string STR_TOOLTIP_B1 = "Zoom out (Ctrl-\"-\")";
        public const string STR_TOOLTIP_B2 = "Zoom in (Ctrl-\"+\")";
        public const string STR_TOOLTIP1 = "\r\nClick Right Mouse Button to invoke the scale menu.";
        public const string STR_TOOLTIP2 = "\r\nPS. Mouse wheel also works in this mode.";
        public const string STR_TOOLTIP3 = "Launch new copy of Shubus Viewer";
        public const string STR_TOOLTIP_LARR = "Open previous file with same extension";
        public const string STR_TOOLTIP_RARR = "Open next file with same extension";
        public const string STR_REOPEN = "Reload current file";
        public const string STR_RECENTFILES = "The recent files are:";
        public const string STR_NORECENTFILES = "There are no recent files in the list.";
        public const string STR_RECENTFILELOST = "Do you want to remove this entry from the Recent list?";
        public const string STR_ZOOM = "Zoom : ";
        public const string STR_EMAIL_PREFIX = "mailto:";
        public const string STR_NOTDEFINED_MSG = "Info : This encoding has not been applied.";
        public const string STR_ENCODINGTIP_MSG = "Click here to change an encoding.\r\nThe current encoding is ";
        public const string STR_REPLACE_DLG = " occurences found.";
        public const string STR_DEFAULT_FN = "New";
        public const string STR_NOTFOUND_MSG = "The following specified text was not found:\r\n\r\n";
        public const string STR_FILENOTEXISTS_MSG = " - this file is not exists.";
        public const string STR_REGHOSTSREADERROR = "\tHosts file location detection filed.\r\n The operation result may be not correct. Do you want to continue?";
        public const string STR_UPDATEINFOWRONG_MSG = "Error: the version information is unavailable.";
        public const string STR_HAVELATEST_MSG = "You already have the relevant version.";
        public const string STR_OUTOFRANGE_MSG = "The entered address value is out of range ";
        public const string STR_LASTDATA_MSG = "Last data occurrence is found. Start new search from the beginning?";
        public const string STR_LOSTDATA_DLG = "All changes in the current document will be lost.\r\nDo you want to continue anyway?";
        public const string STR_SAVEDATA_DLG = "Do you want to save your changes before quit?";
        public const string STR_FILECHANGED_MSG = "The current file was changed outside the editor. Reload it now?";
        public const string STR_UPDATE_APP = "New version is available. Do you want to download it now?";
        public const string STR_PREVIEWTITLE_DLG = "Preview the text before saving :";
        public const string STR_ENCODINGWRONG_MSG = "\tSelected encoding is wrong.\nYou should choose another encoding to prevent data losing.";
        public const string STR_TEXTTOOBIG_MSG = "This text is too large. The Edit feature is disabled.\n The maximal text length to edit is 10 000 000 letters.";
        public const string STR_ENCODETITLE_DLG = "Choose encoding to view current text :";
        public const string STR_NOCOLUMN_MSG = "Line {0} has not column number {1}.";
        public const string STR_NOPLUGINS_MSG = "\tThe plugins are not implemented yet.\n Your donations to Shubus Corporation will improve this software.\n\n WM: Z265015012302";
        public const string STR_SECRET_MSG = "You have found a secret!";
        public const string STR_ARG_TEXTMODE = "-t";
    }
}
