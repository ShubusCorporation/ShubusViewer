using System;
using System.Windows.Forms;
using System.Collections.Generic;
using ExtendedData;

namespace ShubusViewer
{
    public partial class Form1 : Form
    {
        private class ViewManager
        {
            private Form1 mainForm;

            [Flags]
            public enum EViewMode
            {
                EModeNone = 0
              , EModeTextPresent = 1
              , EModeNameDefault = 2
              , EModeTextEmpty = 4
              , EModeTextChanged = 8
              , EModeTextLocked = 16
              , EModeBinary = 32
              , EModeBrowser = 64
              , EModeAll = 1 + 2 + 4 + 8 + 16 + 32 + 64
            };
            private enum EEnableHelper
            {
                EEnableAny, EEnableOnly, EEnableAnyOrOnly
            };
            private EViewMode currentViewMode;
            private Dictionary<ToolStripMenuItem, EViewMode[]> itemEnableModes;
            private Dictionary<ToolStripMenuItem, EEnableHelper> itemActivator;

            public ViewManager(Form1 parent)
            {
                this.mainForm = parent;

                this.itemActivator = new Dictionary<ToolStripMenuItem, EEnableHelper>();
                this.itemActivator[parent.saveToolStripMenuItem] = EEnableHelper.EEnableOnly;
                this.itemActivator[parent.newToolStripMenuItem] = EEnableHelper.EEnableAny;
                this.itemActivator[parent.copyToolStripMenuItem] = EEnableHelper.EEnableAny;
                this.itemActivator[parent.clearToolStripMenuItem] = EEnableHelper.EEnableAnyOrOnly;
                this.itemActivator[parent.lockToolStripMenuItem] = EEnableHelper.EEnableAny;
                this.itemActivator[parent.setEncodingToolStripMenuItem] = EEnableHelper.EEnableAny;
                this.itemActivator[parent.modeToolStripMenuItem] = EEnableHelper.EEnableAnyOrOnly;

                this.itemEnableModes = new Dictionary<ToolStripMenuItem, EViewMode[]>();

                this.itemEnableModes[parent.saveToolStripMenuItem] // Save
                    = new EViewMode[] { EViewMode.EModeTextChanged };

                this.itemEnableModes[parent.newToolStripMenuItem] // Open
                    = new EViewMode[] { EViewMode.EModeAll };

                this.itemEnableModes[parent.copyToolStripMenuItem] // Copy
                    = new EViewMode[]{ EViewMode.EModeAll ^
                         (EViewMode.EModeNameDefault
                        | EViewMode.EModeTextLocked
                        | EViewMode.EModeBrowser)
                    };

                this.itemEnableModes[parent.clearToolStripMenuItem] // New
                    = new EViewMode[]{
                        EViewMode.EModeAll ^ (EViewMode.EModeNameDefault | EViewMode.EModeTextLocked),
                        EViewMode.EModeAll ^ (EViewMode.EModeTextEmpty | EViewMode.EModeTextLocked),
                        EViewMode.EModeBinary | EViewMode.EModeTextLocked
                    };

                this.itemEnableModes[parent.lockToolStripMenuItem] // Lock
                    = new EViewMode[]{EViewMode.EModeAll ^
                         ( EViewMode.EModeTextEmpty
                         | EViewMode.EModeBrowser
                         | EViewMode.EModeBinary)
                    };

                this.itemEnableModes[parent.modeToolStripMenuItem] // Mode
                    = new EViewMode[]{EViewMode.EModeAll ^
                        ( EViewMode.EModeTextEmpty
                        | EViewMode.EModeTextLocked
                        | EViewMode.EModeTextChanged)                    
                        , EViewMode.EModeBrowser, EViewMode.EModeBinary };

                this.itemEnableModes[parent.setEncodingToolStripMenuItem] // Encoding
                    = new EViewMode[] { EViewMode.EModeAll };
            }

            public EViewMode getMode(EViewMode mask)
            {
                return this.currentViewMode & mask;
            }

            private void closeTextMode()
            {
                this.currentViewMode |= EViewMode.EModeTextEmpty;

                if ((this.currentViewMode & EViewMode.EModeTextPresent) == EViewMode.EModeTextPresent)
                    this.currentViewMode ^= EViewMode.EModeTextPresent;

                if ((this.currentViewMode & EViewMode.EModeTextChanged) == EViewMode.EModeTextChanged)
                    this.currentViewMode ^= EViewMode.EModeTextChanged;
            }

            private void updateButtons()
            {
                bool isFullWindow = this.mainForm.FormBorderStyle == FormBorderStyle.None;
                bool isWebGameMode = this.mainForm.mySharedData.fileType == TypeFileFormat.EWebGame;
                bool isFileBrowsing = (this.mainForm.FormBorderStyle == FormBorderStyle.None) ||
                    (this.currentViewMode & EViewMode.EModeNameDefault) == EViewMode.EModeNameDefault;

                this.mainForm.button3.Visible 
                    = this.mainForm.LaunchButton.Visible
                    = this.mainForm.textBox2.Visible
                    = !isFullWindow;

                this.mainForm.button1.Visible = this.mainForm.button2.Visible = !isFullWindow && !isWebGameMode;
                this.mainForm.LArrButton.Visible = this.mainForm.RArrButton.Visible = !isFileBrowsing;
                this.mainForm.setScaleButtonsPosition();
            }

            public void setMode(EViewMode mode, bool val)
            {
                if (((this.currentViewMode & mode) != EViewMode.EModeNone) == val)
                {
                    this.updateButtons();
                    return; // Mode is same, nothing to do.
                }
                if (!val)
                {
                    switch (mode)
                    {
                        case EViewMode.EModeTextEmpty:
                            mode = EViewMode.EModeTextPresent; val = true;
                            break;

                        case EViewMode.EModeTextPresent:
                            mode = EViewMode.EModeTextEmpty; val = true;
                            break;
                    }
                }
                if (val)
                {
                    if (mode != EViewMode.EModeBrowser)
                    {
                        if ((this.currentViewMode & EViewMode.EModeBrowser) == EViewMode.EModeBrowser)
                        {
                            this.currentViewMode ^= EViewMode.EModeBrowser;
                            this.mainForm.WebBrowserClosePage1();
                        }
                    }
                    this.currentViewMode |= mode;

                    switch (mode)
                    {
                        case EViewMode.EModeBrowser:
                            this.closeTextMode();
                            this.mainForm.appNotificationHandler(TypeNotification.ESwitchFromTextToBrowser);
                            break;

                        case EViewMode.EModeTextLocked:
                            this.mainForm.appNotificationHandler(TypeNotification.ETextLock);
                            break;

                        case EViewMode.EModeTextChanged:
                            if ((this.currentViewMode & EViewMode.EModeTextEmpty) == EViewMode.EModeTextEmpty)
                            {
                                this.currentViewMode ^= EViewMode.EModeTextEmpty;
                            }
                            break;

                        case EViewMode.EModeTextEmpty:
                            if ((this.currentViewMode & EViewMode.EModeNameDefault) == EViewMode.EModeNameDefault)
                            {
                                if ((this.currentViewMode & EViewMode.EModeTextChanged) == EViewMode.EModeTextChanged)
                                    this.currentViewMode ^= EViewMode.EModeTextChanged;
                                this.mainForm.textBox2.Clear(); // CR 066
                            }
                            if ((this.currentViewMode & EViewMode.EModeTextPresent) == EViewMode.EModeTextPresent)
                                this.currentViewMode ^= EViewMode.EModeTextPresent;
                            break;

                        case EViewMode.EModeTextPresent:
                            if ((this.currentViewMode & EViewMode.EModeTextEmpty) == EViewMode.EModeTextEmpty)
                                this.currentViewMode ^= EViewMode.EModeTextEmpty;
                            break;

                        default:
                            break;
                    }
                }
                else
                {
                    if ((this.currentViewMode & mode) == mode)
                    {
                        this.currentViewMode ^= mode;
                        if (mode == EViewMode.EModeTextLocked)
                            this.mainForm.appNotificationHandler(TypeNotification.ETextUnlock);
                    }
                }
                foreach (KeyValuePair<ToolStripMenuItem, EViewMode[]> pair in this.itemEnableModes)
                {
                    pair.Key.Visible = false;

                    for (int i = 0; i < pair.Value.Length; i++)
                    {
                        bool enable;
                        EViewMode res = this.currentViewMode & pair.Value[i];

                        enable = (this.itemActivator[pair.Key] == EEnableHelper.EEnableOnly) ?
                          (res == pair.Value[i]) : (res == this.currentViewMode);

                        if (this.itemActivator[pair.Key] == EEnableHelper.EEnableAnyOrOnly)
                            enable = ((res == pair.Value[i]) || (res == this.currentViewMode));

                        if (enable)
                        {
                            pair.Key.Visible = true;
                            break;
                        }
                    }
                }
                this.updateButtons();
            }
        }
    }
}