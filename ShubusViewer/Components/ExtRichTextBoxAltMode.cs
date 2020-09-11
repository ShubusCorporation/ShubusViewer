using System.Windows.Forms;
using System.Drawing;
using ExtendedData;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;

namespace ShubusViewer.Components
{
    public partial class ExtRichTextBox : RichTextBox
    {
        #region Region Alt mode selection
        class CAltModeData
        {
            public bool started;
            public int front1;
            public int selstart;
            public int selend;
            public Dictionary<int, int> selBlocks = new Dictionary<int, int>();
            public int[] linsert;
            public int width;
        }
        CAltModeData altModeData = new CAltModeData();

        void ExtRichTextBox_MouseDownAlt(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (Control.ModifierKeys == Keys.Alt)
                {
                    if (altModeData.started == false)
                    {
                        altModeData.started = true;
                        altModeData.front1 = this.SelectionStart;
                        this.KeyPress -= ExtRichTextBox_KeyPress;
                        this.MouseDown -= this.ExtRichTextBox_MouseDown;
                        this.MouseUp -= this.ExtRichTextBox_MouseUp;
                        this.removeWordLigthtening();
                        this.DoubleClick -= ExtRichTextBox_DoubleClick;                        

                        this.SelectionChanged += new EventHandler(ExtRichTextBox_AltSelectionChanged);
                        this.KeyPress += new KeyPressEventHandler(ExtRichTextBox_AltKeyPress);
                        this.KeyDown += new KeyEventHandler(ExtRichTextBox_AltKeyDown);
                    }
                }
                else
                {
                    if (altModeData.started)
                    {
                        this.endAltMode();
                    }
                }
            }
        }

        private void endAltMode()
        {
            altModeData.started = false;
            this.KeyPress -= ExtRichTextBox_AltKeyPress;
            this.KeyDown -= ExtRichTextBox_AltKeyDown;
            this.SelectionChanged -= new EventHandler(ExtRichTextBox_AltSelectionChanged);
            this.altRemoveAllSels();
            altModeData.selBlocks = new Dictionary<int, int>();
            altModeData.linsert = null;
            this.SelectionLength = 0;
            this.MouseUp += this.ExtRichTextBox_MouseUp;
            this.DoubleClick += new System.EventHandler(ExtRichTextBox_DoubleClick);
            this.MouseDown += this.ExtRichTextBox_MouseDown;
            this.KeyPress += ExtRichTextBox_KeyPress;
            appEventCallBack(TypeNotification.EEnableChangedNotification);
        }

        void ExtRichTextBox_AltSelectionChanged(object sender, EventArgs e)
        {
            if (this.SelectionLength == 0)
            {
                endAltMode();
                return;
            }
            appEventCallBack(TypeNotification.EDisableChangedNotification);
            this.SelectionChanged -= this.ExtRichTextBox_AltSelectionChanged;

            int m = this.SelectionLength - 1;
            int s = this.SelectionStart;
            this.SelectionLength = 0;
            this.SelectionStart += m;

            if (this.SelectionStart > altModeData.front1)
            {
                altModeData.selstart = altModeData.front1;
                altModeData.selend = this.SelectionStart;
            }
            else if (this.SelectionStart < altModeData.front1)
            {
                altModeData.selstart = this.SelectionStart;
                altModeData.selend = altModeData.front1;
            }
            int ln1 = this.GetLineFromCharIndex(altModeData.selstart);
            int ln2 = this.GetLineFromCharIndex(altModeData.selend);
            {
                int colStart = altModeData.selstart - this.GetFirstCharIndexFromLine(ln1);
                int colEnd = altModeData.selend - this.GetFirstCharIndexFromLine(ln2);

                if (colStart > colEnd)
                {
                    int t = colStart;
                    colStart = colEnd;
                    colEnd = t;
                    altModeData.selstart -= (colEnd - colStart);
                }
                int slength = colEnd - colStart;
                Dictionary<int, int> newDict = new Dictionary<int, int>();

                for (int i = ln1; i < ln2 + 1; i++)
                {
                    int selSt = colStart + this.GetFirstCharIndexFromLine(i);

                    if (this.GetLineFromCharIndex(selSt) != i)
                    {
                        continue;
                    }
                    if (this.GetLineFromCharIndex(selSt + slength - 1) != i)
                    {
                        continue;
                    }
                    else
                    {
                        if (newDict.ContainsKey(selSt) == false)
                        {
                            newDict[selSt] = slength;
                        }
                    }
                }
                altModeData.width = slength;
                this.selectAltBlocks(newDict);
            }
            this.SelectionStart = s;
            this.SelectionLength = 0;
            this.SelectionChanged += this.ExtRichTextBox_AltSelectionChanged;
            appEventCallBack(TypeNotification.EDisableChangedNotification);
        }

        private void selectAltBlocks(Dictionary<int, int> newDict)
        {
            foreach (KeyValuePair<int, int> it in altModeData.selBlocks)
            {
                if (newDict.ContainsKey(it.Key))
                {
                    if (newDict[it.Key] < it.Value)
                    {
                        altRemoveSelection(it.Key, it.Value);
                    }
                }
                else
                {
                    altRemoveSelection(it.Key, it.Value);
                }
            }
            foreach (KeyValuePair<int, int> it in newDict)
            {
                this.Select(it.Key, it.Value);
                this.SelectionBackColor = Color.DarkBlue;
                this.SelectionColor = Color.White;
            }
            this.altModeData.selBlocks = newDict;
        }

        private void altRemoveSelection(int st, int len)
        {
            this.Select(st, len);
            this.SelectionBackColor = this.BackColor;
            this.SelectionColor = this.ForeColor;
        }

        private void altRemoveAllSels()
        {
            foreach (KeyValuePair<int, int> it in altModeData.selBlocks)
            {
                altRemoveSelection(it.Key, it.Value);
            }
        }

        void ExtRichTextBox_AltKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Back && e.KeyCode != Keys.Delete)
            {
                if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Left
                    || e.KeyCode == Keys.Up || e.KeyCode == Keys.Down
                    || e.KeyCode == Keys.Escape || e.KeyCode == Keys.Enter)
                {
                    this.endAltMode();
                }
                return;
            }
            e.Handled = true;
            this.SelectionChanged -= ExtRichTextBox_AltSelectionChanged;

            if (altModeData.linsert == null || altModeData.linsert.Length == 0)
            {
                altModeData.linsert = (new List<int>(altModeData.selBlocks.Keys)).ToArray();
                Array.Sort(altModeData.linsert);
            }           
            for (int i = 0; i < altModeData.linsert.Length; i++)
            {
                if (altModeData.width > 1)
                {
                    altModeData.linsert[i] -= (altModeData.width * i);
                    this.SelectionStart = altModeData.linsert[i];
                    this.SelectionLength = altModeData.width > 1 ? altModeData.width : 1;
                    altModeData.linsert[i] += i;
                }
                else if (e.KeyCode == Keys.Back)
                {
                    if (this.Text.Length >= altModeData.linsert[i] - i - i
                        && altModeData.linsert[i] - i - i > 0
                        )
                    {
                        this.SelectionStart = altModeData.linsert[i] - i - i - 1;
                        this.SelectionLength = 1;
                        altModeData.linsert[i] -= (i + 1);
                    }
                    else return;
                }
                else if (e.KeyCode == Keys.Delete)
                {
                    if (this.Text.Length - 1 >= altModeData.linsert[i] - i - i)
                    {
                        this.SelectionStart = altModeData.linsert[i] - i - i;
                        this.SelectionLength = 1;
                        altModeData.linsert[i] -= i;
                    }
                    else return;
                }
                appEventCallBack(TypeNotification.EEnableChangedNotification);
                this.Cut();
                appEventCallBack(TypeNotification.EDisableChangedNotification);
            }
            altModeData.width = 1;
        }

        void ExtRichTextBox_AltKeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsLetterOrDigit(e.KeyChar) == false
                && char.IsPunctuation(e.KeyChar) == false
                && char.IsSeparator(e.KeyChar) == false
                && char.IsSymbol(e.KeyChar) == false)
            {
                return;
            }
            e.Handled = true;
            this.SelectionChanged -= ExtRichTextBox_AltSelectionChanged;
            this.altRemoveAllSels();
            Clipboard.SetData(DataFormats.Text, (Object)e.KeyChar);
            appEventCallBack(TypeNotification.EEnableChangedNotification);

            bool fstInit = false;

            if (altModeData.linsert == null || altModeData.linsert.Length == 0)
            {
                fstInit = true;
                altModeData.linsert = (new List<int>(altModeData.selBlocks.Keys)).ToArray();
                Array.Sort(altModeData.linsert);
            }
            for (int i = 0; i < altModeData.linsert.Length; i++)
            {
                if (altModeData.width > 1 || fstInit)
                {
                    altModeData.linsert[i] -= (altModeData.width * i - i);
                }
                this.SelectionStart = altModeData.linsert[i];
                this.SelectionLength = (altModeData.width > 1 || fstInit) ? altModeData.width : 0;
                altModeData.linsert[i] += i + 1;
                this.Paste();
            }
            altModeData.width = 1;
            appEventCallBack(TypeNotification.EDisableChangedNotification);
        }
        #endregion
    }
}