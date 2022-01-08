using ExtendedData;
using FSProvider;
using ShubusViewer.Engines;
using ShubusViewer.Interfaces;
using StateMachine;
using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;

namespace ShubusViewer // BackColor = Gainsboro
{
    public partial class Form1 : Form
    {
        private const int LEFT_PANEL_WIDTH = 17;
        private int txtIndex;
        private AppController myController;
        private DlgDecoder dlgDecoder;
        private DlgView    dlgView;
        private DlgMode dlgMode;
        private DlgGoTo dlgGoTo;
        private DlgAddress dlgAddress;
        private AppRecent appRecent;
        private FormClosingEventArgs closingArgs; // For canceling close op.
        private bool stateClosing = false;
        private bool stateUpdating = false;
        private bool fullpathView = true;
        private bool inTheWatcher = false;
        private AppExtendedData mySharedData;

        private FileSystemWatcher watcher;
        private FileSystemWatcher confWatcher;

        private delegate void watchDelegate();
        private watchDelegate changeWatcher;
        private watchDelegate watchRestorer;
        private watchDelegate deleteWatcher;

        private string formatLines = "Ln : {0:D1} ";
        private const int MAX_LENGTH_INT = 2048;
        private FSProvider.DirectoryExplorer dExplorer = null;

        private ViewManager myViewManager;
        private bool favListNotEmpty = true;
        private DlgManager mngrDlg;
        private FormWindowState currWinState;
 
        private void InitConfWatcher()
        {
            this.confWatcher = new FileSystemWatcher(Utils.Utils.CfgPath, "user.config");
            this.confWatcher.Changed += new FileSystemEventHandler(conf_Changed);
            this.confWatcher.EnableRaisingEvents = true;
        }

        private void conf_Changed(object sender, FileSystemEventArgs e)
        {
            this.Invoke(
                new watchDelegate(() =>
                {
                    bool stop = false;
                    int max = 20;
                    XmlDocument xml = new XmlDocument();

                    while (!stop && --max > 0)
                    {                       
                        try
                        {
                            // http://stackoverflow.com/questions/1812598/c-sharp-xml-load-locking-file-on-disk-causing-errors
                            string conf = Utils.Utils.CfgPath + "\\user.config";
                            FileInfo fInfo = new FileInfo(conf);
                            long fSize = fInfo.Length;
                            
                            byte[] myBuff = new byte[fSize];
                            // http://stackoverflow.com/questions/1158100/read-file-that-is-used-by-another-process
                            using (FileStream fs = File.Open(conf, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                fs.Read(myBuff, 0, myBuff.Length);
                            }
                            string xm = Encoding.UTF8.GetString(myBuff, 3, myBuff.Length - 3);
                            xml.LoadXml(xm);
                            stop = true;
                        }
                        catch { }
                    }
                    if (!stop) return;

                    string myFontName = "";
                    FontStyle myFontStyle = FontStyle.Regular;
                    string myBgColor = "";
                    string myFgColor = "";

                    foreach (XmlNode x in xml.SelectNodes("configuration/userSettings/ShubusViewer.Properties.Settings/setting"))
                    {
                        switch (x.Attributes["name"].Value)
                        {
                            case "myFontName":
                                myFontName = x.InnerText;
                                break;

                            case "myFontStyle":
                                myFontStyle = (FontStyle)Enum.Parse(typeof(FontStyle), x.InnerText, true);
                                break;

                            case "myBgColor":
                                myBgColor = x.InnerText;
                                break;

                            case "myFgColor":
                                myFgColor = x.InnerText;
                                break;
                        }
                    }
                    this.textBox1.TextChanged -= this.textBox1_TextChanged;
                    this.SetFont(myFontName, myFontStyle, myBgColor, myFgColor, this.textBox1.Font.Size);
                    this.textBox1.TextChanged += this.textBox1_TextChanged;
                }));
        }


        public Form1(AppController aController, ITextProcessor aTextProcessor)
        {
            InitializeComponent();
            this.mngrDlg = new DlgManager(this);
            this.textBox1.SetTextProcessor(aTextProcessor);
            this.txtIndex = -1;

            this.stateClosing = false;
            this.stateUpdating = false;
            this.myController = aController;
            this.mySharedData = aController.sharedData;
            this.myController.setBrowser(this.webBrowser1);

            this.changeWatcher = new watchDelegate(this.reopenOnChangeOutside);
            this.deleteWatcher = new watchDelegate(() =>
            {
                this.myController.processOperation(TypeAction.ETextChanged);
                this.myController.processOperation(TypeAction.EUpdateCaption);
            });
            this.watchRestorer = new watchDelegate(() => { this.watcher.EnableRaisingEvents = true; });

            this.myViewManager = new ViewManager(this);
            this.myController.setAppNotificationHandler(appNotificationHandler);
            this.textBox1.ContextMenuStrip = this.contextMenuStrip1;
            this.checkBox1.ContextMenuStrip = this.contextMenuStrip2;
            this.redoLastActionToolStripMenuItem.Tag = new bool();
            this.readFileDlgSettings();
            // http://opinions5.blogspot.ru/2008/02/mouse-wheel-event-in-c.html
            this.MouseWheel += new MouseEventHandler(Form1_MouseWheel);

            // http://msdn.microsoft.com/en-us/library/fkw1hc6t%28v=vs.85%29.aspx
            this.textBox1.AllowDrop = true;
            this.textBox1.EnableAutoDragDrop = false;
            this.textBox1.DragEnter += new DragEventHandler(this.TextBox1_DragEnter);
            this.textBox1.DragDrop += new DragEventHandler(this.TextBox1_DragDrop);
            this.textBox1.appEventCallBack += this.appNotificationHandler;

            this.toolTip1.SetToolTip(this.LaunchButton, Constants.STR_TOOLTIP3);
            this.toolTip1.SetToolTip(this.LArrButton, Constants.STR_TOOLTIP_LARR);
            this.toolTip1.SetToolTip(this.RArrButton, Constants.STR_TOOLTIP_RARR);
            this.currWinState = WindowState;
        }

        private void textBox1_MouseDown(object sender, MouseEventArgs e)
        {
            this.textBox1.EnableAutoDragDrop = true; // For self-source
        }

        private void TextBox1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Text))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
                e.Effect = DragDropEffects.None;
        }

        private void TextBox1_DragDrop(object sender, DragEventArgs e)
        {
            if (this.textBox1.EnableAutoDragDrop) return;

            int i = this.textBox1.SelectionStart;
            string sDrop = e.Data.GetData(DataFormats.Text).ToString();

            this.textBox1.Text = this.textBox1.Text.Insert(i, sDrop);
            this.textBox1.SelectionStart = i;
            this.textBox1.SelectionLength = sDrop.Length;
            this.textBox1.ScrollToCaret();
            this.textBox1.EnableAutoDragDrop = false;
        }

        void Form1_MouseWheel(object sender, MouseEventArgs e)
        {
            if (this.mySharedData.fileType == TypeFileFormat.EWebPicture)
            {
                if (e.Delta > 0)
                {
                    this.button2_Click(sender, null);
                }
                else if (e.Delta < 0)
                {
                    this.button1_Click(sender, null);
                }
            }
        }

        private string myCaption
        {
            get { return this.Text; }
            set
            {
                if (this.fullpathView)
                {
                    if (value != this.Text) this.Text = value;
                }
                else
                {
                    int ind = value.LastIndexOf('\\');
                    this.Text = (value[0] == '*') ? "*" : "";

                    if (ind > -1)
                    {                        
                        this.Text += value.Substring(ind + 1, value.Length - ind - 1);
                    }
                    else
                    {
                        ind = value.IndexOf('-');

                        if (ind > -1 && ind < value.Length - 2)
                        {
                            this.Text += value.Substring(ind + 2, value.Length - ind - 2);
                        }
                        else this.Text += Constants.STR_APP_TITLE;
                    }
                }
                bool res;
                // Don't switch to 'TextEmpty' text mode when Browser mode is active.
                if (this.myViewManager.getMode(ViewManager.EViewMode.EModeBrowser)
                    == ViewManager.EViewMode.EModeNone)
                {
                    res = (this.textBox1.Text == "");
                    this.myViewManager.setMode(ViewManager.EViewMode.EModeTextEmpty, res);
                }
                // When New is pressed, file name is default. So, turn off browser mode.
                res = (this.mySharedData.basicInfo.curFile == Constants.STR_DEFAULT_FN);
                this.myViewManager.setMode(ViewManager.EViewMode.EModeNameDefault, res);
            }
        }

        private void restoreRecommendedFont()
        {
            this.textBox1.FontChanged -= this.textBox1_FontChanged;
            this.textBox1.TextChanged -= this.textBox1_TextChanged;

            try
            {
                if (this.mySharedData.basicInfo.binary)
                {
                    this.textBox1.Font = new Font("Lucida Console", textBox1.Font.Size, textBox1.Font.Style);
                }
                else
                {
                    string sfont = (this.mySharedData.basicInfo.recommendedFont == "") ?
                        this.mySharedData.basicInfo.preferredFont : this.mySharedData.basicInfo.recommendedFont;
                    this.textBox1.Font = new Font(sfont, textBox1.Font.Size, textBox1.Font.Style);
                }
            }
            catch (Exception) { }
            this.textBox1.FontChanged += this.textBox1_FontChanged;
            this.textBox1.TextChanged += this.textBox1_TextChanged;
        }

        // this.stateUpdating == true - When new data from the model
        // this.stateUpdating == false  - When user change the text
        // this.stateUpdating should be 'true' in this function.
        public void textModeUpdate()
        {  
            bool langRTL = this.mySharedData.basicInfo.langRTL;
            TypeNotification note = (langRTL) ? TypeNotification.ETextModeRTL : TypeNotification.ETextModeLTR;
            TypeNotification check = (this.RightToLeft == RightToLeft.Yes) ? TypeNotification.ETextModeRTL : TypeNotification.ETextModeLTR;

            this.stateUpdating = (textBox1.Text != this.mySharedData.basicInfo.container);

            if (this.stateUpdating)
            {
                // http://codebetter.com/patricksmacchia/2008/07/07/some-richtextbox-tricks/
                // http://stackoverflow.com/questions/4398037/is-there-way-to-speed-up-displaying-a-lot-of-text-in-a-winforms-textbox
                this.setEncodingItemTextChange();
                this.textBox1.FontChanged -= this.textBox1_FontChanged;
                this.textBox1.TextChanged -= this.textBox1_TextChanged;
                this.textBox1.Text = string.Empty; // To set appropriate Selection Indent.

                if (this.mySharedData.basicInfo.container != "")
                {
                    if (note != check)
                    {
                        this.appNotificationHandler(note);
                        this.textBox1.TextChanged -= this.textBox1_TextChanged;
                    }
                }
                this.textBox1.SelectionIndent = 5;
                this.textBox1.FontChanged += this.textBox1_FontChanged;
                this.textBox1.TextChanged += this.textBox1_TextChanged;

                // To fire up the TextChanged event.
                if (this.mySharedData.basicInfo.container != "")
                    this.textBox1.Text = this.mySharedData.basicInfo.container;
                else
                    this.textBox1_TextChanged((object)this.textBox1, null);
            }
            else
            {
                this.stateUpdating = true;
                this.textBox1_TextChanged((object)this.textBox1, null);
            }
            this.stateUpdating = false;
            this.restoreRecommendedFont();

            if (this.textBox1.Focused == false)
            {
                this.textBox1.Focus();
            }
            this.getFormatLines();
            this.myController.processOperation(TypeAction.EUpdateCaption); // hack...
            this.textBox1.ClearUndo();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            // this.stateUpdating will be false when user change the text
            // and will be true when Open operation change the text with new data.
            // The problem is that this.stateUpdating is true after Encoding operation too,
            // but Encoding should not affect EModeTextChanged flag in the ViewManager.
            bool bTextEmpty = ("" == this.textBox1.Text);
            bool bNameDefault = (this.mySharedData.basicInfo.curFile == Constants.STR_DEFAULT_FN);
            bool bFileBinary = this.mySharedData.basicInfo.binary;

            this.myViewManager.setMode(ViewManager.EViewMode.EModeTextEmpty, bTextEmpty);
            this.myViewManager.setMode(ViewManager.EViewMode.EModeNameDefault, bNameDefault);
            this.myController.updateTextState(bTextEmpty);

            this.myViewManager.setMode(ViewManager.EViewMode.EModeBinary, bFileBinary);

            if (bFileBinary)
                this.myViewManager.setMode(ViewManager.EViewMode.EModeTextLocked, true);

            if (this.stateUpdating == false)
            {
                this.myController.processOperation(TypeAction.ETextChanged); // user chenges the text.
            } 
            this.myController.processOperation(TypeAction.EUpdateCaption);
            this.stateUpdating = false;
        }

        private void textBox1_FontChanged(object sender, EventArgs e)
        {
            if (this.mySharedData.basicInfo.binary == false)
                this.mySharedData.basicInfo.preferredFont = this.textBox1.Font.Name;
        }

        public bool appNotificationHandler(TypeNotification notify)
        {
            switch (notify)
            {
                case TypeNotification.EDisableChangedNotification:
                    this.textBox1.TextChanged -= this.textBox1_TextChanged;
                    this.textBox1.FontChanged -= this.textBox1_FontChanged;
                    break;

                case TypeNotification.EEnableChangedNotification:
                    this.textBox1.TextChanged += this.textBox1_TextChanged;
                    this.textBox1.FontChanged += this.textBox1_FontChanged;
                    break;

                case TypeNotification.ETextChanged:
                    {
                        string str = "*" + this.getCurPosStr() + " - " + mySharedData.basicInfo.curFile + " - " + Constants.STR_APP_TITLE; //+ " - " + this.getCurPosStr();
                        this.myCaption = str;
                        this.myViewManager.setMode(ViewManager.EViewMode.EModeTextChanged, true);
                    }
                    break;

                case TypeNotification.ETextRestored:
                    {
                        string str = "";

                        if (this.myViewManager.getMode(ViewManager.EViewMode.EModeBrowser)
                            == ViewManager.EViewMode.EModeNone)
                        {
                            str += this.getCurPosStr() + " - ";
                            this.myViewManager.setMode(ViewManager.EViewMode.EModeTextChanged, false);
                        }
                        else if (this.mySharedData.fileType != TypeFileFormat.EWebGame)
                        {
                            str += Constants.STR_ZOOM + this.webBrowser1.Zoom.ToString() + "%" + " - ";
                        }
                        str += mySharedData.basicInfo.curFile + " - " + Constants.STR_APP_TITLE;
                        this.myCaption = str;
                    }
                    break;

                case TypeNotification.ENewDataReady:
                    if (this.mySharedData.basicInfo.curFile == Constants.STR_DEFAULT_FN)
                        this.clearWatcher();

                    if (mySharedData.fileType == TypeFileFormat.ESimpleText)
                    {
                        bool b = (this.textBox1.Text.Length == 0);
                        this.myViewManager.setMode(ViewManager.EViewMode.EModeTextPresent, !b);
                        this.textModeUpdate();
                    }
                    break;

                case TypeNotification.ERemoveFromRecentList:

                    if (this.appRecent == null)
                        this.appRecent = new AppRecent(this, this.contextMenuStrip3, this.RecentFile_click, Utils.Utils.CfgPath);

                    if (this.appRecent.isFileRecent(this.mySharedData.basicInfo.curFile))
                    {
                        if (MessageBox.Show(Constants.STR_RECENTFILELOST, Constants.STR_APP_TITLE,
                              MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            this.appRecent.DeleteRecentFile(this.mySharedData.basicInfo.curFile);
                            return false;
                        }
                    }
                    break;

                case TypeNotification.ECheckForUserEncoding:

                    if (this.appRecent == null)
                        this.appRecent = new AppRecent(this, this.contextMenuStrip3, this.RecentFile_click, Utils.Utils.CfgPath);

                    if (this.appRecent.isFileRecent(this.mySharedData.basicInfo.curFile))
                    {
                        int num = this.appRecent.getRecentInfo(this.mySharedData.basicInfo.curFile).enc;

                        if (num == -1)
                        {
                            return true; // controller hack.
                        }
                        else
                        {
                            try { this.myController.curEncoding = Encoding.GetEncoding(num); }
                            catch (Exception) { return true; }
                            return false;
                        }
                    }
                    // else return true; // Try to detect.
                    break;

                case TypeNotification.ETextModeRTL:
                    {
                        this.checkBox1.Checked = false;
                        this.Activated -= this.Form1_Activated;
                        this.RightToLeft = RightToLeft.Yes; // will fire Form1_Activated
                        this.RightToLeftLayout = true;
                        this.Activated += this.Form1_Activated;
                    }
                    break;

                case TypeNotification.ETextModeLTR:
                    {
                        this.textBox1.WordWrap = !this.checkBox1.Checked;
                        this.Activated -= this.Form1_Activated; 
                        this.RightToLeftLayout = false;
                        this.RightToLeft = RightToLeft.No;
                        this.Activated += this.Form1_Activated; 
                    }
                    break;

                case TypeNotification.ETextLock:
                    if (this.textBox1.ReadOnly == false)
                    {
                        this.lockToolStripMenuItem.Text = Constants.STR_UNLOCK;
                        this.textBox1.ReadOnly = true;
                    }
                    break;

                case TypeNotification.ETextUnlock:
                    if (this.textBox1.ReadOnly)
                    {
                        this.lockToolStripMenuItem.Text = Constants.STR_LOCK;
                        this.textBox1.ReadOnly = false;
                    }
                    break;

                case TypeNotification.ETextProvide:
                    this.mySharedData.basicInfo.container = this.textBox1.Text;
                    break;

                case TypeNotification.ESaveAsDialog:
                    if (this.mySharedData.basicInfo.curFile != Constants.STR_DEFAULT_FN)
                    {
                        this.saveFileDialog.FileName = Utils.Utils.getShortName(this.mySharedData.basicInfo.curFile);
                    }
                    if (this.saveFileDialog.ShowDialog()
                        == System.Windows.Forms.DialogResult.OK)
                    {                       
                        myController.setFileName(this.saveFileDialog.FileName);
                    }
                    this.textBox1.SelectionLength = 0;
                    break;

                case TypeNotification.EOnFileSaved:
                    this.saveFileDialog.InitialDirectory = Utils.Utils.getPath(this.mySharedData.basicInfo.curFile);

                    if (this.watcher == null)
                    {
                        this.createFileWatcher();
                    }
                    break;

                case TypeNotification.EOpenDialog:
                    if (this.mySharedData.basicInfo.curFile != Constants.STR_DEFAULT_FN)
                    {
                        this.openFileDialog.FileName = Utils.Utils.getShortName(this.mySharedData.basicInfo.curFile);
                    }
                    if (this.openFileDialog.ShowDialog()
                        == System.Windows.Forms.DialogResult.OK)
                    {                        
                        this.myController.setFileName(this.openFileDialog.FileName);                        
                    }
                    this.textBox1.SelectionLength = 0;
                    break;

                case TypeNotification.EOnFileOpened:
                    if (this.inTheWatcher) this.inTheWatcher = false;
                    else this.createFileWatcher();

                    this.openFileDialog.InitialDirectory = Utils.Utils.getPath(this.mySharedData.basicInfo.curFile);
                    this.myViewManager.setMode(ViewManager.EViewMode.EModeTextLocked, false);
                    this.setEncodingItemTextChange();
                    this.txtIndex = -1;

                    if (this.mySharedData.basicInfo.curFile != Constants.STR_DEFAULT_FN)
                    {
                        this.textBox2.Text = this.mySharedData.basicInfo.curFile;
                    }
                    break;

                case TypeNotification.EHorizontalScroll:
                    this.textBox1.WordWrap = !(this.checkBox1.Checked);
                    if (this.textBox1.Focused == false) this.textBox1.Focus();
                    break;

                case TypeNotification.ESwitchFromTextToBrowser:
                    this.goFromTextToBrowserMode();
                    break;

                case TypeNotification.EExitApp:
                    if (this.stateClosing == false)
                    {
                        this.Close();
                    }
                    else
                    {
                        // http://msdn.microsoft.com/ru-ru/library/yh598w02.aspx
                        if (this.webBrowser1 != null && this.webBrowser1.Disposing == false)
                        {
                            ((IDisposable)this.webBrowser1).Dispose();
                        }
                        this.clearWatcher();                        
                        this.updateIfRecent();
                    }
                    break;

                case TypeNotification.EExitCancel:
                    if (watcher != null) this.watcher.EnableRaisingEvents = true;
                    this.closingArgs.Cancel = true;
                    break;

                default:
                    break;
            }
            return true;
        }

        private void clearWatcher()
        {
            if (this.watcher != null)
            {
                this.watcher.EnableRaisingEvents = false;
                this.watcher.Dispose();
                this.watcher = null;
            }
        }

        private void createFileWatcher()
        {
            try
            {
                string path = Utils.Utils.getPath(this.mySharedData.basicInfo.curFile);

                if (path == Constants.STR_DEFAULT_FN || Directory.Exists(path) == false)
                {
                    return;
                }
                this.clearWatcher();                
                string mask = Utils.Utils.getShortName(this.mySharedData.basicInfo.curFile);
                this.watcher = new FileSystemWatcher(path, mask);
                this.watcher.Changed += new FileSystemEventHandler(watcher_Changed);
                this.watcher.Deleted += new FileSystemEventHandler(watcher_Deleted);
                this.watcher.EnableRaisingEvents = true;
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message, Constants.STR_APP_TITLE,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // These functions are invoked in the separate thread.
        void watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            this.Invoke(this.deleteWatcher);
        }

        void watcher_Changed(object sender, FileSystemEventArgs e)
        {
            // http://social.msdn.microsoft.com/Forums/en/csharpgeneral/thread/f68a4510-2264-41f2-b611-9f1633bca21d
            try { (sender as FileSystemWatcher).EnableRaisingEvents = false; }
            catch (Exception) { }
            this.Invoke(this.changeWatcher);
            this.Invoke(this.watchRestorer);
        }

        // http://msdn.microsoft.com/en-us/library/zyzhdc6b.aspx
        void reopenOnChangeOutside()
        {
            this.Activate();

            if (MessageBox.Show(Constants.STR_FILECHANGED_MSG, Constants.STR_APP_TITLE
              , MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                this.inTheWatcher = true;
                this.myController.setFileName(this.mySharedData.basicInfo.curFile);
                this.myController.processOperation(TypeAction.EOpen);
            }
        }

        private void setScaleButtonsPosition()
        {
            int delta = 20;
            int delta1 = 5;
            int width = this.setEncodingToolStripMenuItem.Width
                + this.aboutToolStripMenuItem.Width + this.button1.Width; // CR 047

            this.LaunchButton.Location = new Point(this.Size.Width - width - delta, button1.Location.Y);
            this.RArrButton.Location = new Point(this.LaunchButton.Location.X - this.button2.Width - delta1, button2.Location.Y);
            this.LArrButton.Location = new Point(this.RArrButton.Location.X - this.button2.Width - delta1, button2.Location.Y);

            int xb = this.LArrButton.Visible ? this.LArrButton.Location.X : this.LaunchButton.Location.X;
            this.button1.Location = new Point(xb - this.button2.Width - delta1, button2.Location.Y);
            this.button2.Location = new Point(this.button1.Location.X - this.button2.Width - delta1, button2.Location.Y);

            var xleft = this.menuStrip1.Location.X + delta1;

            for (int i = 0; i < this.menuStrip1.Items.Count; i++)
            {
                if (this.menuStrip1.Items[i].Visible 
                    && this.menuStrip1.Items[i].Alignment == ToolStripItemAlignment.Left)
                {
                    xleft += this.menuStrip1.Items[i].Width;
                }
            }
            var xright = this.button2.Visible ? button2.Location.X - delta1 : xb - delta1;
            var boxWidth = xright - xleft;

            this.textBox2.Location = new Point(xleft, this.textBox2.Location.Y);
            this.textBox2.Width = boxWidth;
        }

        private void setEncodingToolTips() // CR 048
        {
            StringBuilder bld = new StringBuilder(Constants.STR_ENCODINGTIP_MSG);
            bld.Append(" :\r\n");
            bld.Append(this.myController.curEncoding.CodePage.ToString());
            bld.Append(" : ");
            bld.Append(this.myController.curEncoding.BodyName);
            bld.Append(" : ");
            bld.Append(this.myController.curEncoding.EncodingName);
            this.setEncodingToolStripMenuItem.ToolTipText = bld.ToString();
        }

        private void setScaleButtonsToolTips()
        {
            string str = "";

            if (this.mySharedData.fileType == TypeFileFormat.EExplorerText ||
                this.mySharedData.fileType == TypeFileFormat.EWebPicture)
            {
                str = Constants.STR_TOOLTIP1;

                if (this.mySharedData.fileType == TypeFileFormat.EWebPicture)
                    str += Constants.STR_TOOLTIP2;
            }
            this.toolTip1.SetToolTip(this.button1, Constants.STR_TOOLTIP_B1 + str);
            this.toolTip1.SetToolTip(this.button2, Constants.STR_TOOLTIP_B2 + str);            
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (this.watcher != null)
                {
                    this.watcher.EnableRaisingEvents = false;
                }
                myController.processOperation(TypeAction.EUpdateText);
                myController.processOperation(TypeAction.ESave);
                this.textBox1.SelectionLength = 0;

                if (this.watcher != null)
                {
                    this.watcher.EnableRaisingEvents = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace, ex.Message);
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.updateIfRecent();
            this.myController.processOperation(TypeAction.EOpen);
            this.textBox1.SelectionLength = 0;
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.updateIfRecent();
            this.mySharedData.basicInfo.recommendedFont = ""; // CR-041
            this.restoreRecommendedFont();
            this.myController.processOperation(TypeAction.ENew);
            this.myViewManager.setMode(ViewManager.EViewMode.EModeTextLocked, false);
            this.myController.processOperation(TypeAction.EUpdateCaption);
            this.setScaleButtonsToolTips();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.mySharedData.basicInfo.curFile != Constants.STR_DEFAULT_FN)
            {
                this.saveFileDialog.FileName = Utils.Utils.getShortName(this.mySharedData.basicInfo.curFile);
                this.updateIfRecent();
            }
            Encoding enc = this.myController.curEncoding;
            string s = this.textBox1.Text;

            this.clearWatcher();
            this.myController.processOperation(TypeAction.ECopy);
            this.myController.curEncoding = enc; // Restore previous encoding.
            //this.textBox1.Text = s;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            appExit();
        }

        private void lockToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool locked = this.textBox1.ReadOnly;
            this.myViewManager.setMode(ViewManager.EViewMode.EModeTextLocked, !locked);
        }

        private void readFileDlgSettings()
        {
            string myDocs = "";            
            Properties.Settings ps = Properties.Settings.Default;

            try { myDocs = Environment.GetFolderPath(Environment.SpecialFolder.Personal); }
            catch { }

            string mySave = myDocs;
            string myOpen = myDocs;

            try { myOpen = ps.myOpenFolderPref; }
            catch { }
            try { mySave = ps.mySaveFolderPref; }
            catch { }
            this.openFileDialog.InitialDirectory = ps.myOpenFolderPref;
            this.saveFileDialog.InitialDirectory = ps.mySaveFolderPref;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Properties.Settings ps = Properties.Settings.Default;

            // RichTextBox rises this event when font changed.
            this.textBox1.TextChanged -= this.textBox1_TextChanged;

            if (ps.myFontSize == 0)
            {
                try
                {
                    DlgFirstTime dlgFirstTime = new DlgFirstTime();

                    this.FormClosing -= this.Form1_FormClosing;
                    this.Activated -= this.Form1_Activated;
                    this.Deactivate -= this.Form1_Deactivate;

                    if (dlgFirstTime.ShowDialog() != DialogResult.Yes)
                    {
                        this.Close();
                        return;
                    }
                }
                catch (Exception)
                {
                    this.Close();
                    return;
                }
                this.FormClosing += this.Form1_FormClosing;
                this.Activated += this.Form1_Activated;
                this.Deactivate += this.Form1_Deactivate;
            }
            else if (ps.myFontSize > 0)
            {
                this.Location = ps.myLocation;
                this.Size = ps.mySize;

                try
                {
                    DlgDecoder.staticDlgManualItem = ps.textDlgDecoder;
                    DlgDecoder.staticDlgComboBoxItem = ps.itemDlgDecoder;
                    DlgMode.autoClose = ps.dlgModeAutoClose;                    
                    this.favListNotEmpty = ps.favListNotEmpty;
                }
                catch (Exception) { }

                if (ps.iamMaximized)
                {
                    this.WindowState = FormWindowState.Maximized;
                }
                this.mySharedData.swPlayerVersion = ps.swPlayerVersion;
                this.SetFont(ps.myFontName, ps.myFontStyle, ps.myBgColor, ps.myFgColor, ps.myFontSize);
            }

            this.checkBox1.Checked = !this.checkBox1.Checked;
            this.checkBox1.Checked = ps.myHorizontal;
            DlgView.AutoApplyColor = ps.autoApplyColor;
            DlgView.AutoApplyFont = ps.autoApplyFont;
            this.mySharedData.searchQueryGoogle = ps.searchQueryGoogle;

            this.setEncodingItemTextChange();
            this.mySharedData.basicInfo.preferredFont = this.textBox1.Font.Name;
            this.textBox1.SelectionIndent = 5;
            this.textBox1.TextChanged += this.textBox1_TextChanged;
            this.textBox1.ClearUndo();

            // http://stackoverflow.com/questions/2860026/richtextbox-autowordselection-broken
            this.BeginInvoke(new EventHandler(delegate
            {
                this.textBox1.AutoWordSelection = false;
            }));
            this.InitConfWatcher();
        }

        private void SetFont(string myFontName, FontStyle myFontStyle, string myBgColor, string myFgColor, float myFontSize)
        {
            try
            {                
                this.textBox1.Font = new Font(myFontName, myFontSize, myFontStyle);

                if (myBgColor != "" && myFgColor != "")
                {
                    KnownColor bgColor = (KnownColor)System.Enum.Parse(typeof(KnownColor), myBgColor);
                    this.textBox1.BackColor = System.Drawing.Color.FromKnownColor(bgColor);

                    KnownColor fgColor = (KnownColor)System.Enum.Parse(typeof(KnownColor), myFgColor);
                    this.textBox1.ForeColor = System.Drawing.Color.FromKnownColor(fgColor);
                }
            }
            catch (Exception) { }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.stateClosing = true;
            this.confWatcher.EnableRaisingEvents = false;
            this.confWatcher.Dispose();
            Properties.Settings ps = Properties.Settings.Default;

            try { ps.Reload(); } catch (Exception) { }

            if (this.WindowState == FormWindowState.Normal)
            {
                ps.myLocation = this.Location;
                ps.mySize = this.Size;
            }
            if (this.dlgDecoder != null)
            {
                if (this.dlgDecoder.dlgComboBoxItem > -1)
                {
                    ps.textDlgDecoder = this.dlgDecoder.dlgManualItem;
                    ps.itemDlgDecoder = this.dlgDecoder.dlgComboBoxItem;
                }
            }
            else
            {
                ps.textDlgDecoder = DlgDecoder.staticDlgManualItem;
                ps.itemDlgDecoder = DlgDecoder.staticDlgComboBoxItem;
            }
            ps.myFontSize = this.textBox1.Font.Size;
            ps.myFontName = this.mySharedData.basicInfo.preferredFont;
            ps.myFontStyle = this.textBox1.Font.Style;
            ps.myBgColor = this.textBox1.BackColor.Name;
            ps.myFgColor = this.textBox1.ForeColor.Name;
            ps.iamMaximized = (this.WindowState == FormWindowState.Maximized);

            if (this.searchDTO.dirty)
            {
                ps.textDlgSearch = this.searchDTO.txtFind;
                ps.cb1DlgSearch = this.searchDTO.findSameCase;
                ps.cb2DlgSearch = this.searchDTO.replaceWholeWord;
                ps.cb3DlgSearch = this.searchDTO.replaceSameCase;
            }
            ps.myHorizontal = this.checkBox1.Checked;
            ps.autoApplyColor = DlgView.AutoApplyColor;
            ps.autoApplyFont = DlgView.AutoApplyFont;
            ps.dlgModeAutoClose = DlgMode.autoClose;
            
            if (this.appRecent != null)
                ps.favListNotEmpty = this.appRecent.isListNotEmpty();

            try
            {
                if (Convert.ToInt16(this.mySharedData.swPlayerVersion) >= Convert.ToInt16(ps.swPlayerVersion))
                {
                    ps.swPlayerVersion = this.mySharedData.swPlayerVersion;
                }
            }
            catch (Exception) { }

            if (this.saveFileDialog.InitialDirectory != "")
            {
                ps.mySaveFolderPref = this.saveFileDialog.InitialDirectory;
            }
            if (this.openFileDialog.InitialDirectory != "")
            {
                ps.myOpenFolderPref = this.openFileDialog.InitialDirectory;
            }
            if (watcher != null) this.watcher.EnableRaisingEvents = false;
            try { ps.Save(); }
            catch (Exception)
            {
                try
                {
                    File.Delete(Utils.Utils.CfgPath + "\\user.config");
                    ps.Save();
                }
                catch { }
            }
            this.closingArgs = e;
            myController.processOperation(TypeAction.EUpdateText);
            myController.processOperation(TypeAction.EExit);
        }


        private void SaveFont()
        {
            this.confWatcher.EnableRaisingEvents = false;
            Properties.Settings ps = Properties.Settings.Default;

            ps.myBgColor = this.textBox1.BackColor.Name;
            ps.myFgColor = this.textBox1.ForeColor.Name;
            ps.myFontName = this.mySharedData.basicInfo.preferredFont;
            ps.myFontStyle = this.textBox1.Font.Style;
            ps.Save();
            this.confWatcher.EnableRaisingEvents = true;
        }


        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (this.textBox1.ReadOnly || this.textBox1.Text.Length < 2)
            {
                return;
            }
            if (this.textBox1.Text.Length > this.textBox1.MaxLength - 1)
            {
                MessageBox.Show(Constants.STR_TEXTTOOBIG_MSG, Constants.STR_APP_TITLE
                  , MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            switch (e.KeyChar)
            {
                case '\r':
                    {
                        this.textBox1.MoveCarretToNewLine();
                    }
                    break;
                default: break;
            }
        }

        int oldCarret = -1;

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            myController.processOperation(TypeAction.EUpdateCaption);

            switch (e.KeyCode)
            {
                case Keys.Escape:
                    appExit();
                    return;

                case Keys.F1:
                    Utils.Utils.ShowHelp();
                    break;

                case Keys.F3:
                    this.SearchCallback();
                    break;

                case Keys.C: // Copy
                    if (e.Control)
                    {
                        this.copyToolStripMenuItem1_Click(sender, null); // CR035
                        e.Handled = true;
                    }
                    break;

                case Keys.V: // Paste
                    if (e.Control)
                    {
                        this.pasteToolStripMenuItem_Click(sender, null);
                        e.Handled = true;
                    }
                    break;

                case Keys.S:
                    if (e.Control)
                        this.saveToolStripMenuItem_Click(sender, null);
                    break;

                case Keys.O:
                    if (e.Control)
                        this.newToolStripMenuItem_Click(sender, null);
                    break;

                case Keys.M:
                    if (e.Control && this.modeToolStripMenuItem.Visible)
                    {
                        this.dlgMode.StartPosition = FormStartPosition.CenterParent;
                        this.showModeDialog();
                        e.Handled = true;
                    }
                    break;

                case Keys.N:
                    if (e.Control)
                        this.clearToolStripMenuItem_Click(sender, null);
                    break;

                case Keys.F:
                    if (e.Control)
                        this.showFindDialog(0);
                    break;

                case Keys.H:
                    if (e.Control)
                        this.showFindDialog(1);
                    break;

                case Keys.G:
                    if (e.Control)
                    {
                        this.showGotoDialog();
                    }
                    break;

                case Keys.T:
                    if (e.Control)
                        this.showViewDialog();
                    break;

                default:
                    break;
            }
            if (e.Control)
            {
                if ((int)e.KeyCode == 187) // Ctrl + '='
                {
                    this.button2_Click((object)this.button2, e);
                }
                else if ((int)e.KeyCode == 189) // Ctrl + '-'
                {
                    this.button1_Click((object)this.button1, e);
                }
            }                
            if (this.myViewManager.getMode(ViewManager.EViewMode.EModeBinary)
                == ViewManager.EViewMode.EModeBinary)
            {
                int dLength = this.mySharedData.basicInfo.hexDump.Length;
                int sIndex = this.mySharedData.basicInfo.hexDump.startIndex;
                int sStart = this.textBox1.SelectionStart;
 
                switch (e.KeyCode)
                {
                    case Keys.PageDown:
                        if (this.textBox1.SelectionStart > this.textBox1.Text.Length - 3) //"\r\n"
                        {
                            if (sIndex + dLength < this.mySharedData.basicInfo.hexDump.fileSize)
                            {
                                this.mySharedData.basicInfo.hexDump.startIndex += dLength;
                                this.myController.processOperation(TypeAction.EDumpUpdate);
                                this.textBox1.SelectionStart = 0;
                                e.Handled = true;
                            }
                        }
                        else this.oldCarret = this.textBox1.SelectionStart;
                        break;

                    case Keys.PageUp:
                        if (this.textBox1.SelectionStart == 0)
                        {
                            if (sIndex - dLength >= 0)
                            {
                                this.mySharedData.basicInfo.hexDump.startIndex -= dLength;
                                this.myController.processOperation(TypeAction.EDumpUpdate);
                                this.textBox1.SelectionStart = this.textBox1.Text.Length;
                                e.Handled = true;
                            }
                            else if (sIndex > 0)
                            {
                                this.mySharedData.basicInfo.hexDump.startIndex = 0;
                                this.myController.processOperation(TypeAction.EDumpUpdate);
                                this.textBox1.SelectionStart = this.textBox1.Text.Length;
                                e.Handled = true;
                            }
                        }
                        else if (this.textBox1.SelectionStart != this.oldCarret)
                            this.oldCarret = this.textBox1.SelectionStart;
                        break;

                    case Keys.Home:
                        if (this.textBox1.SelectionStart == 0)
                        {
                            this.mySharedData.basicInfo.hexDump.startIndex = 0;
                            this.myController.processOperation(TypeAction.EDumpUpdate);
                        }
                        this.textBox1.SelectionStart = 0;
                        break;

                    case Keys.End:
                        if (this.textBox1.SelectionStart > this.textBox1.Text.Length - 3) // "\r\n"
                        {
                            if (sIndex < this.mySharedData.basicInfo.hexDump.fileSize - dLength)
                            {
                                this.mySharedData.basicInfo.hexDump.startIndex = -1;
                                this.myController.processOperation(TypeAction.EDumpUpdate);
                                this.textBox1.SelectionStart = this.textBox1.Text.Length;
                            }
                            else break;
                        }
                        this.textBox1.SelectionStart = this.textBox1.Text.Length;
                        break;

                    default: break;
                }
                if (sStart != this.textBox1.SelectionStart)
                    this.textBox1.ScrollToCaret();
            }
        }


        private void findToolStripMenuItem_Click(object sender, EventArgs e)
        {
            showFindDialog(0);
        }

        private void goTOThePositionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            showGotoDialog();
        }

        private void showGotoDialog()
        {
            if (this.myViewManager.getMode(ViewManager.EViewMode.EModeBinary) == ViewManager.EViewMode.EModeBinary)
            {
                if (this.dlgAddress == null)
                {
                    this.mngrDlg.Add(this.dlgAddress = new DlgAddress());
                }
                if (this.dlgAddress.ShowDialog() == DialogResult.OK)
                {
                    int max = this.mySharedData.basicInfo.hexDump.fileSize - 1;

                    if (this.dlgAddress.address > max || this.dlgAddress.address < 0)
                    {
                        string msg = string.Format(Constants.STR_OUTOFRANGE_MSG + "[0 - {0:X8}]", max);

                        MessageBox.Show(msg, Constants.STR_APP_TITLE
                        , MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        this.mySharedData.basicInfo.hexDump.startIndex = this.dlgAddress.address;
                        this.myController.processOperation(TypeAction.EDumpUpdate);
                    }
                }
            }
            else
            {
                if (this.dlgGoTo == null)
                {
                    this.mngrDlg.Add(this.dlgGoTo = new DlgGoTo(this.textBox1));
                }
                this.dlgGoTo.ShowDialog();
            }
            if (this.textBox1.Focused == false) this.textBox1.Focus();
            this.textBox1.SelectionLength = 0;
        }

        private void showViewDialog()
        {
            this.textBox1.TextChanged -= this.textBox1_TextChanged;

            if (this.dlgView == null)
            {
                this.dlgView = new DlgView(this.textBox1);
                this.mngrDlg.Add(dlgView);
            }
            this.dlgView.ShowDialog();
            if (this.textBox1.Focused == false) this.textBox1.Focus();
            this.textBox1.SelectionLength = 0;
            this.textBox1.TextChanged += this.textBox1_TextChanged;
            this.SaveFont();
        }
  
        private void appExit()
        {
            this.Close();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dlgAbout = new DlgAbout(this.mySharedData);
            this.aboutToolStripMenuItem.Enabled = false;
            dlgAbout.FormClosed += new FormClosedEventHandler((s, a) => this.aboutToolStripMenuItem.Enabled = true);

            dlgAbout.Show(this);

            if (this.textBox1.Focused == false && this.webBrowser1.Visible == false)
            {
                this.textBox1.Focus();
                this.textBox1.SelectionLength = 0;
            }
        }

        int s, sL;

        public void newTextChangeHandler(object sender, EventArgs e)
        {
            if (this.webBrowser1.Visible == false)
            {
                try
                {
                    this.textBox1.SelectionStart = s;
                    this.textBox1.SelectionLength = sL;
                }
                catch { }
            }
        }

        private void setEncodingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            s = this.textBox1.SelectionStart;
            sL = (this.textBox1.SelectionLength == this.textBox1.Text.Length) ? 0 : this.textBox1.SelectionLength;

            if (this.myViewManager.getMode( ViewManager.EViewMode.EModeTextChanged |
               ViewManager.EViewMode.EModeTextEmpty) != ViewManager.EViewMode.EModeNone)
            {
                this.myController.processOperation(TypeAction.EUpdateText);
            }
            if (this.dlgDecoder == null)
            {
                this.dlgDecoder = new DlgDecoder(this.myController);
                this.mngrDlg.Add(dlgDecoder);
            }
            this.textBox1.TextChanged += newTextChangeHandler;
            this.dlgDecoder.ShowDialog();
            this.textBox1.TextChanged -= newTextChangeHandler;

            this.myController.processOperation(TypeAction.EUpdateCaption);
            this.setEncodingItemTextChange();
        }

        private void modeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.showModeDialog();
            this.setScaleButtonsToolTips();
        }

        private void showModeDialog()
        {
            this.mySharedData.basicInfo.container = this.textBox1.Text; // Update it for browser mode.
            this.myController.setFileName(this.mySharedData.basicInfo.curFile);

            if (this.dlgMode == null)
            {
                this.mngrDlg.Add(this.dlgMode = new DlgMode(this.myController));
            }
            int newX = Control.MousePosition.X - (this.dlgMode.Width >> 1);

            if (newX < this.Location.X + this.Padding.Left)
            {
                newX = this.Location.X + this.Padding.Left;
            }
            this.dlgMode.StartPosition = FormStartPosition.Manual;
            this.dlgMode.Location = new Point(newX, this.Location.Y + this.textBox1.Location.Y + this.menuStrip1.Size.Height);
            this.dlgMode.ShowDialog();

            if (this.mySharedData.fileType == TypeFileFormat.ESimpleText)
            {
                this.textBox1.Focus();
                this.textBox1.SelectionLength = 0;
            }
            this.myController.setFileName(""); // CR 044
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (this.webBrowser1.Visible)
            {
                if (this.mySharedData.fileType != TypeFileFormat.EWebGame)
                {
                    try { this.webBrowser1.Zoom -= 2; }
                    catch (Exception) { }

                    if (this.FormBorderStyle != FormBorderStyle.None)
                        this.appNotificationHandler(TypeNotification.ETextRestored);

                    this.webBrowser1.Focus();
                }
            }
            else if (textBox1.Font.Size > 8)
            {
                this.textBox1.TextChanged -= this.textBox1_TextChanged;
                this.textBox1.FontChanged -= this.textBox1_FontChanged;

                this.textBox1.Font = new Font(textBox1.Font.Name
                    , textBox1.Font.Size - 1, textBox1.Font.Style);

                this.textBox1.FontChanged += this.textBox1_FontChanged;
                this.textBox1.TextChanged += this.textBox1_TextChanged;

                if (this.textBox1.Focused == false)
                    this.textBox1.Focus();
            }
        }

        // http://stackoverflow.com/questions/738232/zoom-in-on-a-web-page-using-webbrowser-net-control
        private void button2_Click(object sender, EventArgs e)
        {
            if (this.webBrowser1.Visible)
            {
                if (this.mySharedData.fileType != TypeFileFormat.EWebGame)
                {
                    try { this.webBrowser1.Zoom += 2; }
                    catch (Exception) { }

                    if (this.FormBorderStyle != FormBorderStyle.None)
                    {
                        this.appNotificationHandler(TypeNotification.ETextRestored);
                    }
                    this.webBrowser1.Focus();
                }
            }
            else if (textBox1.Font.Size < 72)
            {
                this.textBox1.TextChanged -= this.textBox1_TextChanged;
                this.textBox1.FontChanged -= this.textBox1_FontChanged;

                this.textBox1.Font = new Font(textBox1.Font.Name
                    , textBox1.Font.Size + 1, textBox1.Font.Style);

                this.textBox1.FontChanged += this.textBox1_FontChanged;
                this.textBox1.TextChanged += this.textBox1_TextChanged;

                if (this.textBox1.Focused == false)
                    this.textBox1.Focus();
            }
        }

        private void scaleMenuAdjusting(object sender, bool bPlus)
        {
            if (sender is ContextMenuStrip)
            {
                foreach (object obj in (sender as ContextMenuStrip).Items)
                {
                    if (obj is ToolStripMenuItem)
                    {
                        ToolStripMenuItem item = (ToolStripMenuItem)obj;

                        if (item.Tag != null)
                        {
                            if (bPlus)
                                item.Visible = (this.webBrowser1.Zoom < Convert.ToInt16(item.Tag));
                            else
                                item.Visible = (this.webBrowser1.Zoom > Convert.ToInt16(item.Tag));
                        }
                    }
                }
            }
        }

        private void contextMenuScale_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.webBrowser1.Visible == false)
            {
                e.Cancel = true;
                return;
            }
            this.scaleMenuAdjusting(sender, true);
        }

        private void contextScaleMinus_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.webBrowser1.Visible == false)
            {
                e.Cancel = true;
                return;
            }
            this.scaleMenuAdjusting(sender, false);
        }

        private void scaleMenuSetScale_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem)
            {
                try { this.webBrowser1.Zoom = Convert.ToInt16(((ToolStripMenuItem)sender).Tag); }
                catch (Exception) { }
                this.appNotificationHandler(TypeNotification.ETextRestored);
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            this.appNotificationHandler(TypeNotification.EHorizontalScroll);
            this.myController.processOperation(TypeAction.EUpdateCaption);
        }

        private void Form1_Deactivate(object sender, EventArgs e)
        {
            this.textBox1.EnableAutoDragDrop = false;

            if (this.WindowState == FormWindowState.Minimized)
            {
                this.fullpathView = false;
                this.myController.processOperation(TypeAction.EUpdateCaption);
            }
            else if (this.TopMost && !this.Disposing)
            {
                this.Opacity = 0.8;
            }
        }

        private void Form1_Activated(object sender, EventArgs e)
        {
           this.fullpathView = true;
           this.myController.processOperation(TypeAction.EUpdateCaption);

           if (this.Opacity < 1) this.Opacity = 1;
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.textBox1.Copy();
            this.textBox1.Cut();
        }

        private void copyToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (this.textBox1.SelectedText != null && this.textBox1.SelectedText != "") // CR 042
            {
                // CR035  // this.textBox1.Copy();
                Clipboard.SetText(this.textBox1.SelectedText);
            }
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try { Utils.Utils.GetClipboardURL(); }
            catch { }
            // http://msdn.microsoft.com/en-us/library/system.windows.forms.dataformats.format.aspx
            DataFormats.Format myFormat = DataFormats.GetFormat(DataFormats.Text);
            this.textBox1.Paste(myFormat);
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.textBox1.SelectAll();
        }

        private string getCurPosStr()
        {
            string ret;

            if (this.myViewManager.getMode(ViewManager.EViewMode.EModeBinary)
                != ViewManager.EViewMode.EModeNone)
            {
                int start = this.mySharedData.basicInfo.hexDump.startIndex;
                int end = this.mySharedData.basicInfo.hexDump.endIndex;

                ret = "[" + string.Format("{0:X8}", start) + " - "
                    + string.Format("{0:X8}", end) + "]";
            }
            else
            {
                // http://www.codeproject.com/Tips/292107/TextBox-cursor-position
                int s = this.textBox1.SelectionStart;
                int curLine = this.textBox1.GetLineFromCharIndex(s);
                int curCol = s - this.textBox1.GetFirstCharIndexFromLine(curLine);

                string s1 = string.Format(this.formatLines, curLine + 1);
                string s2 = string.Format("Col : {0:D3} ", curCol + 1);

                ret = s1 + s2 + " Size : " + this.textBox1.Text.Length.ToString();
            }           
            return ret;
        }

        private void getFormatLines()
        {
            int lnum = this.textBox1.GetLineFromCharIndex(this.textBox1.Text.Length - 1);
            int digitCount = (lnum > 0) ? (int)Math.Log10(lnum) + 1 : 1;
            this.formatLines = "Ln : {0:D" + digitCount.ToString() + "} ";
        }

        private void selectCurrentLineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int lineStartingCharIndex = this.textBox1.GetFirstCharIndexOfCurrentLine();
            int curLine = this.textBox1.GetLineFromCharIndex(lineStartingCharIndex);
            int j = lineStartingCharIndex;

            for (; this.textBox1.GetLineFromCharIndex(j) == curLine
                 && j < this.textBox1.Text.Length; j++) { }

            int lineLength = j - lineStartingCharIndex;
            this.textBox1.Select(lineStartingCharIndex, lineLength);
        }

        private void undoLastActionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (textBox1.CanUndo)
            {
                this.textBox1.Undo();
            }
        }

        private void redoLastActionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (textBox1.CanRedo)
            {
                this.textBox1.Redo();
            }
        }

        private void openWithTheBrowserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.textBox1.SelectionLength < 1) return;

            string url = this.textBox1.Text.Substring(this.textBox1.SelectionStart
                , this.textBox1.SelectionLength);

            this.myController.openURL(url);
        }

        private void searchInGoogleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.textBox1.SelectionLength < 1) return;

            string sQuery = this.textBox1.Text.Substring(this.textBox1.SelectionStart
                , this.textBox1.SelectionLength);

            this.myController.searchInGoogle(sQuery);
        }
        // URL decode
        private void decodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string urlDecoded = this.textBox1.SelectedText;
            string url;

            do
            {
                url = urlDecoded;
                // System.Web.dll (Menu Project -> Add Reference).
                urlDecoded = System.Web.HttpUtility.UrlDecode(url);
            }
            while (url != urlDecoded);

            this.textBox1.Cut();
            var selectionIndex = textBox1.SelectionStart;
            textBox1.Text = textBox1.Text.Insert(selectionIndex, url);
            textBox1.SelectionStart = selectionIndex;
            textBox1.SelectionLength = url.Length;
        }
        // URL encode
        private void encodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string url = this.textBox1.SelectedText;
            url = System.Web.HttpUtility.UrlEncode(url);

            this.textBox1.Cut();
            var selectionIndex = textBox1.SelectionStart;
            textBox1.Text = textBox1.Text.Insert(selectionIndex, url);
            textBox1.SelectionStart = selectionIndex;
            textBox1.SelectionLength = url.Length;
        }
        // Base64 decode
        private void decodeToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            try
            {
                string url = Utils.Utils.getStringFromBase64(this.textBox1.SelectedText);

                this.textBox1.Cut();
                var selectionIndex = textBox1.SelectionStart;
                textBox1.Text = textBox1.Text.Insert(selectionIndex, url);
                textBox1.SelectionStart = selectionIndex;
                textBox1.SelectionLength = url.Length;
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message, Constants.STR_APP_TITLE
                    , MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        // Base64 encode
        private void encodeToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            try
            {
                string url = this.textBox1.SelectedText;
                byte[] bytes = System.Text.ASCIIEncoding.UTF8.GetBytes(url);
                url = System.Convert.ToBase64String(bytes);

                this.textBox1.Cut();
                var selectionIndex = textBox1.SelectionStart;
                textBox1.Text = textBox1.Text.Insert(selectionIndex, url);
                textBox1.SelectionStart = selectionIndex;
                textBox1.SelectionLength = url.Length;
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message, Constants.STR_APP_TITLE
                    , MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        // Send e-mail
        private void sendEmailToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string email = this.textBox1.SelectedText;
            string prefix = Constants.STR_EMAIL_PREFIX;

            if (email.ToLower().IndexOf(prefix) == -1)
            {
                email = prefix + email;
            }
            try { System.Diagnostics.Process.Start(email); }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message, Constants.STR_APP_TITLE
                    , MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.undoLastActionToolStripMenuItem.Enabled = textBox1.CanUndo;
            this.redoLastActionToolStripMenuItem.Enabled = textBox1.CanRedo;
            this.undoRedoToolStripMenuItem.Visible = ((textBox1.CanUndo || textBox1.CanRedo)
                && this.textBox1.ReadOnly == false && this.textBox1.SelectionLength == 0);

            this.copyToolStripMenuItem1.Visible = this.cutToolStripMenuItem.Visible
                = (this.textBox1.SelectionLength > 0);

            this.pasteToolStripMenuItem.Visible = (
                Clipboard.ContainsText() && this.textBox1.ReadOnly == false);

            // it's necessary measure.
            if (this.textBox1.Text.Length == 0)
                this.copyToolStripMenuItem1.Visible = true;

            this.selectAlToolStripMenuItem.Visible = (
                this.textBox1.Text != "" && this.textBox1.SelectionLength == 0);           
            
            this.uRLToolStripMenuItem.Visible = (this.textBox1.SelectionLength > 0);
            this.base64ToolStripMenuItem.Visible = (this.textBox1.SelectionLength > 0);
             
            if (this.textBox1.SelectedText.Length <= MAX_LENGTH_INT) // CR 045
            {
                this.checkBase64();
                this.sendEmailToolStripMenuItem.Visible = Utils.Utils.checkEmail(this.textBox1.SelectedText);
            }
            else
            {
                this.decodeToolStripMenuItem1.Visible = true;
                this.decodeToolStripMenuItem.Visible = true;
                this.openWithTheBrowserToolStripMenuItem.Visible = true;
                this.sendEmailToolStripMenuItem.Visible = false;
            }
            this.makePasteTip();
        }


        private void checkBase64()
        {
            // Check for Base64 & URL appropriate chars.
            bool b64 = true;  // Encoded with Base64
            bool bEUrl = true; // Encoded URL
            bool bUrl = true;  // URL for browser

            // Web encodings menu build.
            int limit = this.textBox1.SelectedText.Length;
            char lastCh = (limit > 0) ? this.textBox1.SelectedText[limit - 1] : '\0';

            if (lastCh == '\r' || lastCh == '\n') limit--;

            string test = Regex.Replace(this.textBox1.SelectedText, "[\r\n]", "");

            b64 = Utils.Utils.checkBase64(test);
            bEUrl = Utils.Utils.checkEncodedURL(test);
            bUrl = Utils.Utils.checkURL(test);

            this.decodeToolStripMenuItem1.Visible = b64;  // Decode Base64
            this.decodeToolStripMenuItem.Visible = bEUrl; // Decode Encoded URL
            this.openWithTheBrowserToolStripMenuItem.Visible = bUrl;
        }

        private void makePasteTip()
        {
            if (Clipboard.ContainsText() && this.textBox1.ReadOnly == false)
            {
                try
                {
                    int lnCount = 1;
                    int cutIndex = 0;
                    bool cuted = false;
                    Graphics g = CreateGraphics();

                    string str = Clipboard.GetText();
                    if (cuted = (str.Length > MAX_LENGTH_INT)) str = str.Substring(0, MAX_LENGTH_INT);

                    // http://reflector.webtropy.com/default.aspx/DotNET/DotNET/8@0/untmp/whidbey/REDBITS/ndp/fx/src/WinForms/Managed/System/WinForms/ToolTip@cs/4/ToolTip@cs
                    // http://stackoverflow.com/questions/1264406/how-do-i-get-the-taskbars-position-and-size
                    int maxCount = 1 + ((Screen.PrimaryScreen.WorkingArea.Height - Cursor.Position.Y - this.contextMenuStrip1.Size.Height) / Control.DefaultFont.Height);
                    bool reserved;

                    if (reserved = (maxCount > 1)) maxCount--;

                    for (int i = 0; i < str.Length; i++)
                    {
                        char nextChar = (i < str.Length - 1) ? str[i + 1] : '\0';

                        // CR 035 : Affected!
                        if ((str[i] == '\r' && nextChar != '\n') || str[i] == '\n')
                        {
                            if (++lnCount > maxCount)
                            {
                                cutIndex = i;
                                break;
                            }
                        }
                    }
                    if (cuted = (cutIndex > 0)) str = str.Substring(0, cutIndex);
                    if (cuted && reserved) str += "\r\n <.....>";
                    this.pasteToolStripMenuItem.ToolTipText = str;
                }
                catch (Exception) { }
            }
        }

        private void rTLModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.RightToLeft == RightToLeft.No)
            {
                this.textBox1.TextChanged -= this.textBox1_TextChanged;
                string str = this.textBox1.Text;
                this.textBox1.Clear();
                this.appNotificationHandler(TypeNotification.ETextModeRTL);
                this.textBox1.SelectionIndent = 5;
                this.textBox1.Text = str;            
                this.textBox1.TextChanged += this.textBox1_TextChanged;
            }
        }

        private void lTRModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.RightToLeft == RightToLeft.Yes)
            {
                this.textBox1.TextChanged -= this.textBox1_TextChanged;
                string str = this.textBox1.Text;
                this.textBox1.Clear();
                this.appNotificationHandler(TypeNotification.ETextModeLTR);
                this.textBox1.SelectionIndent = 5;
                this.textBox1.Text = str;
                this.textBox1.TextChanged += this.textBox1_TextChanged;
            }
        }

        private void contextMenuStrip2_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.myViewManager.getMode(ViewManager.EViewMode.EModeBinary) == ViewManager.EViewMode.EModeBinary)
            {
                this.lTRModeToolStripMenuItem.Visible = this.rTLModeToolStripMenuItem.Visible = false;
            }
            else if (this.RightToLeft == RightToLeft.No)
            {
                this.rTLModeToolStripMenuItem.Visible = true;
                this.lTRModeToolStripMenuItem.Visible = false;
            }
            else
            {
                this.rTLModeToolStripMenuItem.Visible = false;
                this.lTRModeToolStripMenuItem.Visible = true;
            }
        }

        private void setEncodingItemTextChange()
        {
            string name =  Utils.Utils.GetShortEncodingName(this.myController.curEncoding);

            this.setEncodingToolStripMenuItem.Text = name;
            this.setEncodingToolTips();
            this.setScaleButtonsPosition();
            this.setScaleButtonsToolTips();
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            myController.processOperation(TypeAction.EUpdateCaption);
        }

        private void textBox1_MouseClick(object sender, MouseEventArgs e)
        {
            myController.processOperation(TypeAction.EUpdateCaption);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (this.checkBox1.Visible = this.textBox1.Location.X == 0)
            {
                ShowLeftPanel();
            }
            else HideLeftPanel();
            /*if (this.Padding.Left > 0)
            {
                HideLeftPanel();
            }
            else ShowLeftPanel();*/
         }

        private void toolStripTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (sender is ToolStripTextBox)
                {
                    try { this.webBrowser1.Zoom = Convert.ToInt16((sender as ToolStripTextBox).Text); }
                    catch (Exception ee)
                    {
                        MessageBox.Show(ee.Message, Constants.STR_APP_TITLE
                      , MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    (sender as ToolStripTextBox).Clear();
                    this.contextMenuScale.Close();
                    this.contextScaleMinus.Close();
                    this.appNotificationHandler(TypeNotification.ETextRestored);
                }
            }
        }

        private void fontColorsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.showViewDialog();
        }

        private void addCurrentFileToRecentListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string def = Utils.Utils.getShortName(this.mySharedData.basicInfo.curFile);
            DlgRecentDescr descr = new DlgRecentDescr(def) { TopMost = this.TopMost };

            descr.ShowDialog();

            if (descr.DialogResult == DialogResult.OK)
            {
                def = descr.Desr;
                this.appRecent.AddRecentFile(this.mySharedData.basicInfo.curFile, def
                    , this.textBox1.SelectionStart, this.getEncodingNumForRecentList());
            }
        }

        private void forgetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.appRecent.DeleteRecentFile(this.mySharedData.basicInfo.curFile);
        }

        private void reopenCurrentFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.myController.setFileName(this.mySharedData.basicInfo.curFile);
            this.myController.processOperation(TypeAction.EOpen);
        }

        private int getEncodingNumForRecentList()
        {
            try
            {   // CR032
                if (this.myController.getCurrentFileType() == TypeFileFormat.ESimpleText)
                {
                    return this.myController.curEncoding.CodePage;
                }
            }
            catch (Exception) { }
            return -1;
        }

        private void contextMenuStrip3_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.appRecent == null)
                this.appRecent = new AppRecent(this, this.contextMenuStrip3, this.RecentFile_click, Utils.Utils.CfgPath);

            ((ContextMenuStrip)sender).RightToLeft = RightToLeft.No;

            if (this.myViewManager.getMode(ViewManager.EViewMode.EModeNameDefault)
                != ViewManager.EViewMode.EModeNameDefault)
            {
                bool b = this.appRecent.isFileRecent(this.mySharedData.basicInfo.curFile);
                bool b1 = this.hostsName == this.mySharedData.basicInfo.curFile;

                this.forgetToolStripMenuItem.Visible = b;
                this.addCurrentFileToRecentListToolStripMenuItem.Visible = !b && !b1;
                this.reopenCurrnetFileToolStripMenuItem.Text = Constants.STR_REOPEN;
            }
            else
            {
                this.forgetToolStripMenuItem.Visible = false;
                this.addCurrentFileToRecentListToolStripMenuItem.Visible = false;

                this.reopenCurrnetFileToolStripMenuItem.Text =
                    this.appRecent.isListNotEmpty() ? Constants.STR_RECENTFILES
                    : Constants.STR_NORECENTFILES;
            }
            ViewManager.EViewMode mode = this.myViewManager.getMode(ViewManager.EViewMode.EModeTextEmpty);

            this.findToolStripMenuItem.Visible = this.goTOThePositionToolStripMenuItem.Visible
                = (mode == ViewManager.EViewMode.EModeNone);

            // Check if advanced feature workable. 
            this.specialBonusViewHostsFileToolStripMenuItem.Visible = this.toolStripSeparator1.Visible = this.checkAdmin();            
        }

        private bool checkAdmin()
        {
            try
            {
                System.Security.Principal.WindowsIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(identity);

                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch (Exception) { return false; }
        }

        private void RecentFile_click(object sender, EventArgs e)
        {
            string fn = (sender as ToolStripMenuItem).ToolTipText;
            this.openDemand(fn);
        }

        private void updateIfRecent()
        {
            if (this.appRecent == null 
                || this.mySharedData?.basicInfo == null
                || this.mySharedData?.basicInfo.curFile == string.Empty)
            {
                return;
            }
            try
            {
                if (this.appRecent.isFileRecent(this.mySharedData.basicInfo.curFile))
                {
                    if (this.watcher != null)
                        this.watcher.EnableRaisingEvents = false;

                    this.appRecent.updateRecentFile(this.mySharedData.basicInfo.curFile,
                        this.textBox1.SelectionStart + this.textBox1.SelectionLength,
                        this.getEncodingNumForRecentList());

                    if (this.watcher != null)
                        this.watcher.EnableRaisingEvents = true;
                }
            }
            catch (Exception) { }
        }

        private void button3_MouseEnter(object sender, EventArgs e)
        {
            this.button3.BackColor = Color.FromArgb(148, 156, 176);
        }

        private void button3_MouseLeave(object sender, EventArgs e)
        {
            this.button3.BackColor = Color.FromName("MenuBar");
        }

        private string hostsName = "";

        private void specialFeatureViewHostsFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            const string hostsPathRegKey = "SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters";
            const string subKey = "DataBasePath";
            string hostsPath = Environment.SystemDirectory + "\\drivers\\etc";
            Microsoft.Win32.RegistryKey regkey;

            this.updateIfRecent();

            try
            {
                regkey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(hostsPathRegKey);
                hostsPath = (string)(regkey.GetValue(subKey, hostsPath));
            }
            catch (Exception)
            {
                if (MessageBox.Show(Constants.STR_REGHOSTSREADERROR, Constants.STR_APP_TITLE
                    , MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                    return;
            }
            try
            {
                hostsPath = System.Environment.ExpandEnvironmentVariables(hostsPath) + "\\hosts";

                this.hostsName = hostsPath;
                this.myController.setFileName(hostsPath);
                this.myController.processOperation(TypeAction.EOpen);
            }
            catch (Exception) { MessageBox.Show(Constants.STR_UNKNOWN, Constants.STR_APP_TITLE
                , MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private void topMostToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool bTopMost = !((ToolStripMenuItem)sender).Checked;
            ((ToolStripMenuItem)sender).Checked = this.TopMost = bTopMost;
            this.mngrDlg.UpdateTopStatus();
        }

        private void LaunchButton_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Application.ExecutablePath);
        }

        private void openDemand(string fn)
        {
            if (Control.ModifierKeys == Keys.Shift)
            {
                System.Diagnostics.Process.Start(Application.ExecutablePath, "\"" + fn + "\"");
                return;
            }
            try
            {
                this.updateIfRecent();
                this.myController.setFileName(fn);
                this.myController.processOperation(TypeAction.EOpen); // Detect file type, then encoding if file is text.

                if (this.appRecent.isFileRecent(fn) && this.mySharedData.fileType == TypeFileFormat.ESimpleText)
                {
                    AppRecent.SRecentInfo rInfo = this.appRecent.getRecentInfo(fn);
                    int sel = rInfo.sel;
                    int enc = rInfo.enc;

                    if (sel > -1 && sel <= this.textBox1.Text.Length)
                    {
                        this.textBox1.SelectionStart = sel;
                        this.textBox1.ScrollToCaret();
                        this.selectCurrentLineToolStripMenuItem_Click(null, null);

                        int newSL = sel - this.textBox1.SelectionStart;
                        this.textBox1.SelectionLength = (newSL > 0) ? newSL : 1;
                    }
                }
            }
            catch { }
        }

        private void LArrButton_Click(object sender, EventArgs e)
        {
            if (this.dExplorer == null)
            {
                this.dExplorer = DirectoryExplorer.CreateObject(this.mySharedData.basicInfo);
            }
            if (dExplorer != null)
                this.openDemand(dExplorer.getPrev());
        }

        private void RArrButton_Click(object sender, EventArgs e)
        {
            if (this.dExplorer == null)
            {
                this.dExplorer = DirectoryExplorer.CreateObject(this.mySharedData.basicInfo);            
            }
            if (dExplorer != null)
                this.openDemand(dExplorer.getNext());
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            bool winStateChanged = this.currWinState != WindowState;
            this.currWinState = WindowState;

            if (this.mySharedData.fileType == TypeFileFormat.EWebPicture)
            {
                doOnDocumentReady(this.webBrowser1, !winStateChanged); // Not recalc zum on window max/min - CR064
            }
            else
            {
               this.myController.processOperation(TypeAction.EUpdateCaption);
            }
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (!string.IsNullOrEmpty(this.textBox2.Text))
                {
                    this.myController.setFileName(this.textBox2.Text.Trim(new char[] { '\r', '\n', ' ' }));
                    this.myController.processOperation(TypeAction.EOpen);
                }
            }
        }

        private void textBox2_Click(object sender, EventArgs e)
        {
            if (Constants.STR_DEFAULT_FN != this.mySharedData.basicInfo.curFile
                && this.textBox2.Text != this.mySharedData.basicInfo.curFile
                )
            {
                this.textBox2.Text = this.mySharedData.basicInfo.curFile;
            }
            this.textBox2.SelectionStart = 0;
            this.textBox2.SelectionLength = this.textBox2.Text.Length;            
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            webBrowser1_DocumentCompleted1(sender, e);
        }

        private void webBrowser1_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            webBrowser1_Navigated1(sender, e);
        }

        private void webBrowser1_VisibleChanged(object sender, EventArgs e)
        {
            webBrowser1_VisibleChanged1(sender, e);
        }

        private void webBrowser1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            webBrowser1_PreviewKeyDown1(sender, e);
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            this.textBox2.TextChanged -= textBox2_TextChanged;
            var oldSelection = this.textBox2.SelectionStart;
            this.textBox2.Text = this.textBox2.Text.Trim(new char[] { '\r', '\n', ' ' });
            this.textBox2.SelectionStart = oldSelection > this.textBox2.Text.Length 
                ? this.textBox2.Text.Length 
                : oldSelection;
            this.textBox2.TextChanged += textBox2_TextChanged;
        }
    }
}