// CR-054
using System.Windows.Forms;
using System.Drawing;
using ExtendedData;
using System.Text.RegularExpressions;
using System;
using System.Linq;
using System.Collections.Generic;

//#if !RUNNING_ON_4
//using CustomTuple;
//#endif

using System.Text;
using ShubusViewer.Interfaces;

namespace ShubusViewer.Components
{
    public partial class ExtRichTextBox : RichTextBox
    {
        // http://stackoverflow.com/questions/3282384/richtextbox-syntax-highlighting-in-real-time-disabling-the-repaint
       // [DllImport("user32.dll")]
       // private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

        // http://stackoverflow.com/questions/7518876/disable-scrolling-when-selecting-text-in-richtextbox-c
        //[DllImport("user32.dll", EntryPoint = "LockWindowUpdate", SetLastError = true, CharSet = CharSet.Auto)]
        //private static extern IntPtr LockWindow(IntPtr Handle);

        enum EScroll
        {
            Page, UpLine, DownLine
        }

        private const int WM_SETREDRAW = 0x0b;
        private const int WM_PAINT = 15;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_HSCROLL = 0x114;
        private const int WM_VSCROLL = 0x115;
        private const int WM_SIZE = 0x0005;
        private const int WM_KEYDOWN = 0x100;
        private const int WM_KEYUP = 0x101;
        private const int SB_LINEUP = 0;
        private const int SB_LINEDOWN = 1;

        private ITextProcessor mTextProcessor = null;
        Queue<Tuple<int, int>> mLightWordsQueue = new Queue<Tuple<int, int>>();
        public delegate bool processAppNotification(TypeNotification notification);
        public processAppNotification appEventCallBack;
        private string selectedText = string.Empty;

        public ExtRichTextBox()
        {
            this.DoubleClick += new System.EventHandler(ExtRichTextBox_DoubleClick);
            this.MouseUp += ExtRichTextBox_MouseUp;
            this.MouseDown += new MouseEventHandler(ExtRichTextBox_MouseDownAlt);
            this.KeyDown +=new KeyEventHandler(ExtRichTextBox_KeyDown);
        }

        public void SetTextProcessor(ITextProcessor aTextProcessor)
        {
            this.mTextProcessor = aTextProcessor;
        }

        void ExtRichTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && this.mLightWordsQueue.Count > 0)
            {
                this.removeWordLigthtening();
            }
            switch (e.KeyCode)
            {
                case Keys.Right:
                    e.Handled = this.mTextProcessor.TextMoveRight(this);
                    break;

                case Keys.Left:
                    e.Handled = this.mTextProcessor.TextMoveLeft(this);
                    break;
            }
        }
    
        private Regex rgxNewLine = new Regex("[\\r\\n]", RegexOptions.Compiled);

        private void ExtRichTextBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (Control.ModifierKeys == Keys.Alt || this.altModeData.started)
            {
                this.MouseUp -= ExtRichTextBox_MouseUp;
                return;
            }
            if (this.rgxNewLine.IsMatch(this.SelectedText))
            {
                if (this.mLightWordsQueue.Count > 0)
                {
                    this.removeWordLigthtening();
                }
                return;
            }            
            if (this.SelectionLength == 0 || this.SelectionLength >= this.Text.Length)
            {
               // this.MouseDown -= new MouseEventHandler(ExtRichTextBox_MouseDown);
                this.KeyPress -= new KeyPressEventHandler(ExtRichTextBox_KeyPress);
                return;
            }
            selectedText = this.SelectedText;

            int sL = this.SelectionLength;
            int sIn = this.GetCharIndexFromPosition(new Point(0, this.Font.Height / 2));
            int sCurr = this.SelectionStart;
            
            this.removeWordLigthtening();
            this.makeLighttening(EScroll.Page);

            this.Select(sIn, 0);
            this.Select(sCurr, sL);

            this.MouseDown += new MouseEventHandler(ExtRichTextBox_MouseDown);
            this.KeyPress += new KeyPressEventHandler(ExtRichTextBox_KeyPress);
        }

        void ExtRichTextBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (Control.ModifierKeys == Keys.Alt)
                {
                    return;
                }
                this.MouseDown -= ExtRichTextBox_MouseDown;
                this.removeWordLigthtening();
            }
        }

        void removeWordLigthtening()
        {
            if (this.mLightWordsQueue.Count > 0)
            {
                appEventCallBack(TypeNotification.EDisableChangedNotification);
               // LockWindow(this.Handle);

                int sIn = this.GetCharIndexFromPosition(new Point(0, this.Font.Height / 2));
                int sIn1 = this.SelectionStart;
                int sL = this.SelectionLength;

                while (this.mLightWordsQueue.Count > 0)
                {
                    var lightWordTuple = this.mLightWordsQueue.Dequeue();

                    this.Select(lightWordTuple.Item1, lightWordTuple.Item2);
                    this.SelectionBackColor = this.BackColor;
                    this.SelectionColor = this.ForeColor;
                }
                this.Select(sIn, 0);
                this.Select(sIn1, sL);

              //  LockWindow(IntPtr.Zero);
                appEventCallBack(TypeNotification.EEnableChangedNotification);
            }
        }

       private int TopLeftChar = 0;

        void makeLighttening(EScroll mode)
        {
            int st = 0, fn = 0;

            switch (mode)
            {
                case EScroll.Page:
                    st = this.GetCharIndexFromPosition(new Point(0, Font.Height / 2));
                    fn = this.GetCharIndexFromPosition(new Point(ClientRectangle.Width, ClientRectangle.Height));
                    break;

                case EScroll.DownLine:
                    st = this.GetCharIndexFromPosition(new Point(0, ClientRectangle.Height / 2));
                    fn = this.GetCharIndexFromPosition(new Point(ClientRectangle.Width, ClientRectangle.Height));
                    break;

                case EScroll.UpLine:
                    st = this.GetCharIndexFromPosition(new Point(0, Font.Height / 2));
                    fn = this.GetCharIndexFromPosition(new Point(ClientRectangle.Width, ClientRectangle.Height / 2));
                    break;
            }
            if (st >= fn) return;

            string check = selectedText;
            if (check.Length == 0 || false == Regex.IsMatch(check, "[\\w\\.\\-]+")) return;
            string wndText = this.Text.Substring(st, fn - st + 1);

            appEventCallBack(TypeNotification.EDisableChangedNotification);
            //SendMessage(this.Handle, WM_SETREDRAW, (IntPtr)0, IntPtr.Zero);
           // LockWindow(this.Handle);

            var matches = Regex.Matches(wndText, Utils.Utils.rgxScr(check)
                , RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                if (this.mLightWordsQueue.FirstOrDefault(x => x.Item1 == match.Index + st) != null)
                {
                    continue;
                }
                char prevChar = (match.Index + st > 0) ? this.Text[match.Index + st - 1] : ' ';
                char nextChar = (match.Index + st + match.Length > this.Text.Length - 2) ? ' ' : this.Text[match.Index + st + match.Length];
                bool isSubStr = char.IsLetterOrDigit(prevChar) || char.IsLetterOrDigit(nextChar);

                Color color;
                this.Select(match.Index + st, match.Length);
                this.mLightWordsQueue.Enqueue(Tuple.Create(match.Index + st, match.Length));

                if (string.Compare(selectedText, match.Value) == 0)
                {
                    if (isSubStr)
                    {
                        color = Color.Aquamarine;
                    }
                    else color = Color.LightGreen;
                }
                else
                {
                    if (isSubStr)
                    {
                        color = Color.LightYellow;
                    }
                    else color = Color.Yellow;
                }
                this.SelectionBackColor = color;
                this.SelectionColor = Color.Black;
            }
           // LockWindow(IntPtr.Zero);
           // SendMessage(this.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
            this.Invalidate();
            appEventCallBack(TypeNotification.EEnableChangedNotification);
        }

        void ExtRichTextBox_DoubleClick(object sender, System.EventArgs e)
        {            
            int sIn = this.GetCharIndexFromPosition(new Point(0, this.Font.Height / 2));
            int sCurr = this.SelectionStart;
            int sL = this.SelectionLength;
            selectedText = this.SelectedText.Trim();

            if (selectedText.Length == this.Text.Length || selectedText.Length == 0)
                return;

            this.removeWordLigthtening();
            this.makeLighttening(EScroll.Page);

            this.Select(sIn, 0);
            this.Select(sCurr, 0);
            this.MouseDown += new MouseEventHandler(ExtRichTextBox_MouseDown);
            this.KeyPress += new KeyPressEventHandler(ExtRichTextBox_KeyPress);
        }

        void ExtRichTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
           // appEventCallBack(TypeNotification.EDisableChangedNotification);
            this.KeyPress -= ExtRichTextBox_KeyPress;
            this.removeWordLigthtening();
           // appEventCallBack(TypeNotification.EEnableChangedNotification);
        }

        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            if (m.Msg == WM_KEYDOWN && Control.ModifierKeys == Keys.Control)
            {
                Keys keyData = (Keys)((int)m.WParam);

                if (this.altModeData.started)
                {
                    if (keyData == Keys.C)
                    {
                        int cnt = 0;
                        StringBuilder sb = new StringBuilder();

                        foreach (var line in this.altModeData.selBlocks)
                        {
                            if (++cnt > 1) sb.Append("\r\n");
                            sb.Append(this.Text.Substring(line.Key, line.Value));
                        }
                        Clipboard.SetText(sb.ToString());
                    }
                }
                else // No Alt-selection mode
                {
                    base.WndProc(ref m);
                }
            }
            else if (m.Msg == WM_MOUSEWHEEL && Control.ModifierKeys == Keys.Control)
            {
                appEventCallBack(TypeNotification.EDisableChangedNotification);
				var mwParam = (int)(long)m.WParam;

				if (mwParam > 0)
                {
                    if (this.Font.Size < 72)
                    {
                        this.Font = new Font(this.Font.Name
                            , this.Font.Size + 1, this.Font.Style);

                        if (this.Focused == false)
                            this.Focus();
                    }
                }
                else if (mwParam < 0)
                {
                    if (this.Font.Size > 8)
                    {
                        this.Font = new Font(this.Font.Name
                            , this.Font.Size - 1, this.Font.Style);

                        if (this.Focused == false)
                            this.Focus();
                    }
                }
                m.Result = (System.IntPtr)1;
                appEventCallBack(TypeNotification.EEnableChangedNotification);
            }
            else // Some other wnd messages
            {
                base.WndProc(ref m);
                // http://stackoverflow.com/questions/14163007/catch-textbox-scroll-event
                if (m.Msg == WM_SIZE 
                    || m.Msg == WM_VSCROLL
                    || m.Msg == WM_MOUSEWHEEL
                   )
                {
                    if (this.mLightWordsQueue.Count > 0)
                    {
                        EScroll mode = EScroll.Page;
                        int chTL = this.GetCharIndexFromPosition(new Point(0, Font.Height / 2));
                        int low = this.TopLeftChar > chTL ? SB_LINEUP : SB_LINEDOWN;
                        this.TopLeftChar = chTL;

                          switch (low)
                          {
                              case SB_LINEDOWN:
                                  mode = EScroll.DownLine;
                                  break;

                              case SB_LINEUP:
                                  mode = EScroll.UpLine;
                                  break;
                          }                        
                        makeLighttening(mode);
                    }
                }
            }
        }

        public void MoveCarretToNewLine()
        {
            this.mTextProcessor.MoveCarretToNewLine(this);
        }
    }
}