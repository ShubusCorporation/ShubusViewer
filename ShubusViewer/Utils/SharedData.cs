using ExtendedData;
using System;
using System.Windows.Forms;
using System.Configuration;
using System.IO;

namespace SharedData
{
    public class ModelInitData
    {
        public bool langRTL = false;    // Is RTL language detected?
        public bool binary = false;     // Is current file binary?
        public string container = "";   // Text content from CurFile.
        public string file = "";        // Full file name.
        public SHexDump hexDump;

        public string preferredFont = string.Empty;
        public string recommendedFont = string.Empty;

        public struct SHexDump
        {
            public int Length; //length of dump page
            public int startIndex;
            public int endIndex;
            public int fileSize;
            public bool isHex;
        };

        public ModelInitData()
        {
            this.file = Constants.STR_DEFAULT_FN;
            this.container = this.curFile = "";
            this.hexDump.startIndex = 0;
            this.hexDump.Length = 1024 * 30;
        }

        // Affects this.initData.container, so returns void.
        public void ReplaceToRN()
        {
            for (int i = 0; i < this.container.Length; i++)
            {
                if (this.container[i] == '\n')
                {
                    if (i == 0 || this.container[i - 1] != '\r')
                    {
                        this.container = this.container.Insert(i, "\r");
                    }
                }
                else if (this.container[i] == '\r')
                {
                    if (i == this.container.Length - 1 || this.container[i + 1] != '\n')
                    {
                        if (i == this.container.Length - 1)
                        {
                            this.container += "\n";
                        }
                        else this.container = this.container.Insert(i + 1, "\n");
                    }
                }
            }
        }

        public string curFile
        {
            get { return this.file; }
            set
            {
                if ("" == value)
                {
                    this.file = Constants.STR_DEFAULT_FN;
                }
                else this.file = value.Trim();
            }
        }
    }
}