using System;
using System.Windows.Forms;
using System.Drawing;
using StateMachine;
using ExtendedData;

namespace ShubusViewer
{
    public partial class Form1
    {
        #region Internal Web Browser processing
        // It's invoked from ViewManager when Browser mode is active.
        private void goFromTextToBrowserMode()
        {
            this.textBox1.TextChanged -= this.textBox1_TextChanged;
            this.textBox1.Clear();
            this.textBox1.TextChanged += this.textBox1_TextChanged;
            this.webBrowser1.Visible = true;
        }

        private void WebBrowserClosePage1()
        {
            if (this.webBrowser1.Visible)
            {
                this.webBrowser1.DocumentCompleted -= this.webBrowser1_DocumentCompleted;
                this.webBrowser1.Navigated -= this.webBrowser1_Navigated;
                this.myController.closePage();
            }
        }

        private void webBrowser1_DocumentCompleted1(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            // http://msug.vn.ua/Posts/Details/3769
            if (string.Equals(e.Url.AbsolutePath
                , (sender as WebBrowser).Url.AbsolutePath
                , StringComparison.OrdinalIgnoreCase) == false)
            {
                return;
            }
            this.doOnDocumentReady(sender);
        }

        private void webBrowser1_Navigated1(object sender, WebBrowserNavigatedEventArgs e)
        {
            if (string.Equals(e.Url.AbsolutePath
                , (sender as WebBrowser).Url.AbsolutePath
                , StringComparison.OrdinalIgnoreCase) == false)
            {
                return;
            }
            this.doOnDocumentReady(sender);
        }

        private void doOnDocumentReady(object sender, bool calcZum = true)
        {
            this.webBrowser1.WebBrowserShortcutsEnabled = 
                (this.mySharedData.fileType != TypeFileFormat.EWebGame
                 && this.mySharedData.fileType != TypeFileFormat.EWebPicture);

            if (calcZum)
            {
                this.CalcZum();
            }
            // http://go4answers.webhost4life.com/Example/webbrowser-control-documentcomplete-67566.aspx
            if (this.myViewManager.getMode(ViewManager.EViewMode.EModeBrowser)
                == ViewManager.EViewMode.EModeNone)
            {
                this.myViewManager.setMode(ViewManager.EViewMode.EModeBrowser, true);
            }
            this.myController.processOperation(TypeAction.EUpdateCaption);
        }

        private void CalcZum()
        {
            if (this.mySharedData.fileType != TypeFileFormat.EWebPicture)
            {
                this.webBrowser1.Zoom = 100;
                return;
            }
            Size picSize = new Size();
            Size wbrSize = this.webBrowser1.Size;
            bool fileNotBroken = true;

            try
            {
                picSize = Image.FromFile(this.mySharedData.basicInfo.curFile).Size;
            }
            catch { fileNotBroken = false; }
            double newZoom = 100;

            if (fileNotBroken)
            {
                if (picSize.Width > wbrSize.Width || picSize.Height > wbrSize.Height)
                {
                    double vert = 100;
                    double hor = 100;

                    if (picSize.Width > wbrSize.Width)
                    {
                        vert = ((double)wbrSize.Width / (double)picSize.Width) * 100;
                    }
                    if (picSize.Height > wbrSize.Height)
                    {
                        hor = ((double)wbrSize.Height / (double)picSize.Height) * 100;
                    }
                    newZoom = (vert > hor) ? hor : vert;
                }
            }
            this.webBrowser1.Zoom = (int)newZoom;
        }


        private void HideLeftPanel()
        {
            this.textBox1.Location = new Point(0, this.textBox1.Location.Y);
            this.webBrowser1.Location = new Point(0, this.webBrowser1.Location.Y);

            this.textBox1.Size = new Size(this.textBox1.Size.Width + LEFT_PANEL_WIDTH, this.textBox1.Size.Height);
            this.webBrowser1.Size = new Size(this.webBrowser1.Size.Width + LEFT_PANEL_WIDTH, this.webBrowser1.Size.Height);
            //this.Padding = new Padding(0, 0, 0, 0);
            //this.menuStrip1.Padding = (this.textBox1.RightToLeft == RightToLeft.No) ?
            //new Padding(250, 0, 0, 0) : new Padding(0, 0, 250, 0);
        }

        private void ShowLeftPanel()
        {
            this.textBox1.Location = new Point(LEFT_PANEL_WIDTH, this.textBox1.Location.Y);
            this.webBrowser1.Location = new Point(LEFT_PANEL_WIDTH, this.webBrowser1.Location.Y);

            this.textBox1.Size = new Size(this.textBox1.Size.Width - LEFT_PANEL_WIDTH, this.textBox1.Size.Height);
            this.webBrowser1.Size = new Size(this.webBrowser1.Size.Width - LEFT_PANEL_WIDTH, this.webBrowser1.Size.Height);
            //this.menuStrip1.Padding = new Padding(0, 2, 0, 2);
            //this.Padding = new Padding(250, 0, 0, 0);
        }

        private void webBrowser1_VisibleChanged1(object sender, EventArgs e)
        {
            this.checkBox1.Visible = !this.webBrowser1.Visible;

            if (this.webBrowser1.Visible)
            {
                HideLeftPanel();
                this.webBrowser1.Focus();
                this.textBox1.KeyPress -= this.textBox1_KeyPress;
                this.KeyDown -= this.Form1_KeyDown;
            }
            else
            {
                if (this.textBox1.Focused == false)
                {
                    this.textBox1.Focus();
                }
                if (this.textBox1.SelectionLength == this.textBox1.Text.Length)
                    this.textBox1.SelectionLength = 0;

                ShowLeftPanel();

                this.webBrowser1.DocumentCompleted += this.webBrowser1_DocumentCompleted; // hello from appWebModel
                this.webBrowser1.Navigated += this.webBrowser1_Navigated;
                this.textBox1.KeyPress += this.textBox1_KeyPress;
                this.KeyDown += this.Form1_KeyDown;
            }
        }

        private void webBrowser1_PreviewKeyDown1(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.M && this.FormBorderStyle == FormBorderStyle.Sizable)
            {
                this.dlgMode.StartPosition = FormStartPosition.CenterParent;
                this.showModeDialog();
                return;
            }
            if (this.mySharedData.fileType == TypeFileFormat.EWebGame
                || this.mySharedData.fileType == TypeFileFormat.EWebPicture)
            {
                if (e.KeyCode == Keys.F11)
                {
                    if (this.FormBorderStyle == FormBorderStyle.Sizable)
                    {
                        this.WindowState = FormWindowState.Normal;
                        this.menuStrip1.Visible = this.textBox2.Visible = false;

                        this.webBrowser1.Location = new Point(0, 0);
                        this.webBrowser1.Size = new Size(this.webBrowser1.Size.Width
                            , this.webBrowser1.Size.Height + this.menuStrip1.Size.Height + 1);

                        this.FormBorderStyle = FormBorderStyle.None;
                        this.appNotificationHandler(TypeNotification.ETextRestored);
                        this.WindowState = FormWindowState.Maximized;
                    }
                    else
                    {
                        this.WindowState = FormWindowState.Normal;
                        this.menuStrip1.Visible = this.textBox2.Visible = true;

                        this.webBrowser1.Location = new Point(this.webBrowser1.Location.X
                            , this.menuStrip1.Size.Height + 1);
                        this.webBrowser1.Size = new Size(this.webBrowser1.Size.Width
                            , this.webBrowser1.Size.Height - this.menuStrip1.Size.Height);

                        this.FormBorderStyle = FormBorderStyle.Sizable;
                        this.appNotificationHandler(TypeNotification.ETextRestored);
                    }
                }
            }
            if (this.mySharedData.fileType != TypeFileFormat.EWebGame)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    this.appExit();
                }
                if (e.KeyCode == Keys.F1)
                {
                    Utils.Utils.ShowHelp();
                }
                else if (e.Control)
                {
                    EventArgs ea = new EventArgs();

                    if ((int)e.KeyCode == 187) // Ctrl + '='
                    {
                        this.button2_Click((object)this.button2, ea);
                    }
                    else if ((int)e.KeyCode == 189) // Ctrl + '-'
                    {
                        this.button1_Click((object)this.button1, ea);
                    }
                }
            }
        }
        #endregion // Internal Web Browser processing
    }
}