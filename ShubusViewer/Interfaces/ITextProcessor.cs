using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ShubusViewer.Interfaces
{
    public interface ITextProcessor
    {
        bool TextMoveRight(RichTextBox tBox);
        bool TextMoveLeft(RichTextBox tBox);
        void MoveCarretToNewLine(RichTextBox textBox);
    }
}
