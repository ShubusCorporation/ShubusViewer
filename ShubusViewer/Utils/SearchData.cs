using System;
using System.Windows.Forms;
using System.Collections.Generic;
using ExtendedData;

namespace SearchData
{
    public delegate void searchCallback();

    public class SearchDialogData
    {
        public string txtFind = string.Empty;
        public bool findSameCase = false;
        public bool replaceWholeWord = false;
        public bool replaceSameCase = false;
        public string txtWhat = string.Empty;
        public string txtWith = string.Empty;
        public bool dirty = false;

        public StringComparison sComparison
        {
            get
            {
                if (this.findSameCase)
                {
                    return StringComparison.CurrentCulture;
                }
                else
                {
                    return StringComparison.InvariantCultureIgnoreCase;
                }
            }
        }

        public StringComparison rComparison
        {
            get
            {
                if (this.replaceSameCase)
                {
                    return StringComparison.CurrentCulture;
                }
                else
                {
                    return StringComparison.InvariantCultureIgnoreCase;
                }
            }
        }
    }
}