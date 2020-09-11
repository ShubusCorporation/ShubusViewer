using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ShubusViewer.Interfaces;

namespace ShubusViewer.Components
{
    public class TextProcessor : ITextProcessor
    {
        public bool TextMoveRight(RichTextBox tB)
        {
            bool ret = false;

            if (tB.SelectionLength > 0)
            {
                int start = tB.SelectionStart;

                if (tB.GetFirstCharIndexOfCurrentLine() == start)
                {
                    string newtext = string.Empty;

                    if (tB.RightToLeft == RightToLeft.No)
                    {
                        newtext = this.DoTextMoveRight(tB.SelectedText);
                    }
                    else newtext = this.DoTextMoveLeft(tB.SelectedText);

                    this.pasteViaClipboard(newtext, tB);
                    tB.SelectionStart = start;
                    tB.SelectionLength = newtext.Length;
                    ret = true;
                }
            }
            return ret;
        }

        public bool TextMoveLeft(RichTextBox tB)
        {
            bool ret = false;

            if (tB.SelectionLength > 0)
            {
                int start = tB.SelectionStart;

                if (tB.GetFirstCharIndexOfCurrentLine() == start)
                {
                    string newtext = string.Empty;

                    if (tB.RightToLeft == RightToLeft.No)
                    {
                        newtext = this.DoTextMoveLeft(tB.SelectedText);
                    }
                    else newtext = this.DoTextMoveRight(tB.SelectedText);

                    if (newtext != tB.SelectedText)
                    {
                        this.pasteViaClipboard(newtext, tB);
                        tB.SelectionStart = start;
                        tB.SelectionLength = newtext.Length;
                        ret = true;
                    }
                }
            }
            return ret;
        }

        public string DoTextMoveRight(string text)
        {
            try
            {
                text = text.Insert(0, " ");

                for (int i = 0; i < text.Length; i++)
                {
                    char nextCh = (i < text.Length - 1) ? text[i + 1] : '\0';

                    if ((text[i] == '\r' || text[i] == '\n') && nextCh != '\r' && nextCh != '\n')
                    {
                        if (nextCh != '\0')
                            text = text.Insert(i + 1, " ");
                    }
                }
            }
            catch (Exception) { }
            return text;
        }

        private char[] _del = new char[] { ' ', '\t' };

        public string DoTextMoveLeft(string text)
        {
            try
            {
                if (_del.Contains(text[0]))
                    text = text.Remove(0, 1);

                for (int i = 0; i < text.Length; i++)
                {
                    char nextCh = (i < text.Length - 1) ? text[i + 1] : '\0';

                    if ((text[i] == '\r' || text[i] == '\n') && nextCh != '\r' && nextCh != '\n')
                    {
                        if (_del.Contains(nextCh))
                            text = text.Remove(i + 1, 1);
                    }
                }
            }
            catch (Exception) { }
            return text;
        }


        public void MoveCarretToNewLine(RichTextBox textBox)
        {
            try
            {
                int s = textBox.SelectionStart;
                // Add -1 for RichTextBox.
                int curLine = textBox.GetLineFromCharIndex(s) - 1;

                if (curLine < 0) return;   // Bug: Ctrl-M causes '\r' interception here.
                int firstCol = textBox.GetFirstCharIndexFromLine(curLine);
                if (firstCol < 0) return;

                string str = ""; // "\r\n"; //Fix for RichTextBox
                int index = firstCol;

                while (true)
                {
                    char cur = (index > textBox.Text.Length - 1) ?
                        '\r' : textBox.Text[index];

                    if (cur == ' ') str += " ";

                    if (cur != ' ' && cur != '\r' && cur != '\n')
                    {
                        break;
                    }
                    if (cur == '\r' || cur == '\n')
                    {
                        if (str == "") return; // Fix for RichTextBox.

                        str = "\n" + str;
                        textBox.SelectionStart = firstCol;
                        textBox.SelectionLength = str.Length;
                        break;
                    }
                    index++;
                }
                if (str != "")
                {
                    this.pasteViaClipboard(str, textBox);
                }
                //e.Handled = true; // Does't work for RichTextBox.
            }
            catch (Exception) { }
        }

        void pasteViaClipboard(string newtext, RichTextBox tb)
        {
            try
            {
                string tmp = Clipboard.GetText();
                Clipboard.SetText(newtext);
                tb.Paste();

                if (tmp != null && tmp != "")
                {
                    Clipboard.SetText(tmp);
                }
                else Clipboard.Clear();
            }
            catch (Exception) { }
        }
    }
}