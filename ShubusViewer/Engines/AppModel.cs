using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using SharedData;
using BaseAbstractModel;
using System.Text.RegularExpressions;

namespace DataProcessor
{
    public class AppModel : AppAbstractModel
    {
        [Flags]
        private enum TNewLineEncoding
        {
            EUnknown = 0,            
            EUnixLinux = 1, // Unix, Linux, NET RichTextBox.
            EMacOS = 2,
            EMicrosoft = 3 // MSDOS, Windows.
        };

        private struct TLangAsianDetector
        {
            public UInt32 start;
            public UInt32 end;
            public int bytes;
            public int count;
            public string sFontName;

            public TLangAsianDetector(UInt32 s, UInt32 e, int b, int cnt, string name)
            {
                this.start = s;
                this.end = e;
                this.bytes = b;
                this.count = cnt;
                this.sFontName = name;
            }
        };
     
        public ModelInitData initData;
        private byte[] myBuff; // Binary container for file data.
        private bool ch;  // New data ready sign.

        private TNewLineEncoding nlEncoding = TNewLineEncoding.EUnknown;
        private Encoding curEncoding;
        private Dictionary<Encoding, byte[]> sigBase;

        private UInt16[,] sigRTL;
        private TLangAsianDetector[] SAsianLangList;

        private int bomLength = -1; // BOM unknown.
        private int detectBuffLength = 4096;
        private delegate Encoding DelegateEncoding();
        private DelegateEncoding detectEncoding;
        private const int minLengthBOM = 2; // Minimal length of scanned file.
        private bool detected = false; // If the BOM encoding is detected.
        private bool prevRTL = false;

        public AppModel(ModelInitData myFileData)
        {
            this.ch = false;
            this.initData = myFileData;
            // this.curEncoding = Encoding.UTF8;
            this.encoding = Encoding.UTF8;
            this.dCalc = new DumpParamCalculator();
            this.detectEncoding = this.getEncoding;

            // For BOMs (Byte Order Mask) detecting.
            this.sigBase = new Dictionary<Encoding, byte[]>();
            Encoding utf32BE = new UTF32Encoding(true, true);
            Encoding utf32LE = new UTF32Encoding(false, true);

            sigBase[utf32BE] = new byte[4] { 0x00, 0x00, 0xFE, 0xFF }; // BE
            sigBase[utf32LE] = new byte[4] { 0xFF, 0xFE, 0x00, 0x00 }; // LE

            sigBase[Encoding.Unicode] = new byte[2] { 0xFF, 0xFE };
            sigBase[Encoding.BigEndianUnicode] = new byte[2] { 0xFE, 0xFF };
            sigBase[Encoding.UTF7] = new byte[3]{ 0x2B, 0x2F, 0x76 };
            sigBase[Encoding.UTF8] = new byte[3]{ 0xEF, 0xBB, 0xBF };

            // Detect RTL langs.
            this.sigRTL = new UInt16[,]
            {
                { 0xD690, 0xD7BF } // Hebrew UTF-8
              , { 0xD880, 0xDBBF } // Arabic UTF-8
              , { 0xDC80, 0xDD8F } // Syriac UTF-8
              , { 0xDE80, 0xDEBF } // Thaana UTF-8
              , { 0x0590, 0x05FF } // Hebrew Unicode
              , { 0x0600, 0x06FF } // Arabic Unicode
              , { 0x0700, 0x074F } // Syriac Unicode
              , { 0x0780, 0x07BF } // Thaana Unicode
            };

            this.SAsianLangList = new TLangAsianDetector[]
            {
                new TLangAsianDetector(0xDE80, 0xDEBF, 2, 0, "TITUS Cyberbit Basic"),
                new TLangAsianDetector(0x0780, 0x07BF, 2, 0, "TITUS Cyberbit Basic"),
                new TLangAsianDetector(0xE18080, 0xE1829F, 3, 0, "Padauk Book"),
                new TLangAsianDetector(0x1000, 0x109F, 2, 0, "Padauk Book")
            };
        }

        // Data from the View to write to the file.
        public void updateData()
        {
            this.myBuff = this.curEncoding.GetBytes(this.initData.container);
        }

        // Checked by controler to apply data changes.
        public override bool changed
        {
            get { return this.ch; }
            set { this.ch = value; }
        }

        public Encoding encoding
        {
            get { return this.curEncoding; }
            set
            {
                if (this.curEncoding != value)
                {
                    if (this.myBuff == null && this.initData.container.Length > 0)
                    {
                        this.myBuff = this.curEncoding.GetBytes(this.initData.container);
                    }
                   // this.curEncoding = value;
                    this.curEncoding = Encoding.GetEncoding(value.CodePage,
                        new EncoderReplacementFallback("."),
                        new DecoderReplacementFallback("."));
                }
            }
        }

        private void readFile()
        {            
            FileInfo fInfo = new FileInfo(this.initData.curFile);
            long fSize = fInfo.Length;

            this.myBuff = new byte[fSize]; // Exception is handled in the AppController.

            // http://stackoverflow.com/questions/1158100/read-file-that-is-used-by-another-process
            using (FileStream fs = File.Open(this.initData.curFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Read(this.myBuff, 0, this.myBuff.Length);
            }
        }

        private bool isFileBinary()
        {
            if (myBuff == null || myBuff.Length < minLengthBOM)
                return (this.initData.binary = false);

            if (myBuff.Length > 3)
            {
                if (this.myBuff[0] == 0xFF && this.myBuff[1] == 0xFE)
                    return (this.initData.binary = false);

                if (this.myBuff[2] == 0xFE && this.myBuff[3] == 0xFF)
                    return (this.initData.binary = false);
            }
            int min = (myBuff.Length > 1024) ? 1024 : myBuff.Length;

            for (int i = 0; i < min - 1; i += 2)
            {
                if (this.myBuff[i] == '\0' && this.myBuff[i + 1] == '\0')
                {
                    return (this.initData.binary = true);
                }
            }
            return (this.initData.binary = false);
        }

        private class DumpParamCalculator
        {
            private int fileLength; // In bytes
            private int dumpLength; // Translated bytes
            private int dumpStart;  // Address in the file

            public int Length
            {
                get { return this.dumpLength; }
                set
                {
                    if (this.dumpStart >= 0)
                    {
                        if (value > this.fileLength - this.Start || value < 1)
                        {
                            value = this.fileLength - this.Start;
                        }
                    }
                    else if (this.dumpStart == -1)
                    {
                        if (value > this.fileLength || value < 1)
                        {
                            value = this.fileLength;
                        }
                        else value = this.fileLength % value;
                    }
                    this.dumpLength = value;
                }
            }

            public int FileLength
            {
                get { return this.fileLength; }
            }

            public int Start
            {
                get
                {
                    if (this.dumpStart > 0)
                    {
                        return this.dumpStart;
                    }
                    else if (dumpStart == -1)
                    {
                        return this.fileLength - this.dumpLength;
                    }
                    return 0;
                }
                set
                {
                    if (value > this.fileLength - 1) value = this.dumpStart;
                    else if (value < -1) value = 0;

                    this.dumpStart = value;

                    if (value == -1 || this.dumpLength > this.fileLength - this.dumpStart)
                    {
                        this.Length = this.dumpLength; // Recalculate.
                    }
                }
            }

            public void dCalcInit(int fLength, int dLength, int dStart)
            {
                this.fileLength = fLength;
                this.Start = dStart;
                this.Length = dLength;
            }
        }

        private DumpParamCalculator dCalc;

        public void makeHexDump()
        {
            if (this.myBuff == null || this.myBuff.Length == 0)
            {
                this.readFile();
            }
            this.initData.hexDump.fileSize = this.myBuff.Length;

            this.dCalc.dCalcInit(this.myBuff.Length, this.initData.hexDump.Length
                , this.initData.hexDump.startIndex);
            this.initData.hexDump.startIndex = this.dCalc.Start;

            this.initData.container = "";
            this.initData.hexDump.endIndex = this.dCalc.Start + this.dCalc.Length - 1;

            string str = "";
            int remainder = (dCalc.Length % 16 == 0) ? 0 : 1;
            int address = dCalc.Start;

            for (int j = 0; j < (dCalc.Length >> 4) + remainder; j++)
            {
                str = string.Format("{0:X8} :", address);
                string s1 = "";

                for (int i = address; i < address + 16; i++)
                {
                    if (i > this.myBuff.Length - 1)
                    {
                        str += "   "; continue;
                    }
                    str += string.Format(" {0:X2}", this.myBuff[i]);

                    if (char.IsControl((char)this.myBuff[i]) == false)
                        s1 += this.curEncoding.GetString(this.myBuff, i, 1);
                    else s1 += ".";
                }
                str += " | " + s1 + "\r\n";
                this.initData.container += str;
                address += 16;
            }
            this.initData.hexDump.isHex = true;
            this.initData.binary = true;
            this.changed = true;
        }

        public void makeBinDump()
        {
            if (this.myBuff == null || this.myBuff.Length == 0)
            {
                this.readFile();
            }
            this.initData.container = "";

            this.dCalc.dCalcInit(this.myBuff.Length, this.initData.hexDump.Length
                , this.initData.hexDump.startIndex);

            this.initData.hexDump.startIndex = this.dCalc.Start;
            this.initData.hexDump.endIndex = this.dCalc.Start + this.dCalc.Length - 1;

            this.initData.container = this.curEncoding.GetString(this.myBuff, this.dCalc.Start, this.dCalc.Length) + "\r\n";
            // http://stackoverflow.com/questions/5459641/replacing-characters-in-c-sharp-ascii
            this.initData.container = Regex.Replace(this.initData.container, "[«»\u0000\u201C\u201D\u201E\u201F\u2033\u2036]", " ");

            this.initData.hexDump.isHex = false;
            this.initData.binary = true;
            this.changed = true;
        }

        public bool searchInDump(string s, int start)
        {
            byte[] bytes = this.curEncoding.GetBytes(s);

            if (this.myBuff == null || this.myBuff.Length < bytes.Length)
            {
                return false;
            }
            int ind = Utils.Utils.IndexOf(this.myBuff, bytes, start);

            if (ind > -1)
            {
                int newInd = (ind > 16) ? ind - 16 : 0;

                this.dCalc.dCalcInit(this.myBuff.Length, this.initData.hexDump.Length, newInd);
                this.initData.hexDump.startIndex = this.dCalc.Start;
                this.initData.hexDump.endIndex = this.dCalc.Start + this.dCalc.Length - 1;
                return true;
            }
            return false;
        }

        private Encoding getEncoding()
        {
            /*
            UTF-8 has the following properties:

             UCS characters U+0000 to U+007F (ASCII) are encoded simply as bytes 0x00 to 0x7F (ASCII compatibility). This means that files and 
            strings which contain only 7-bit ASCII characters have the same encoding under both ASCII and UTF-8. All UCS characters >U+007F are
            encoded as a sequence of several bytes, each of which has the most significant bit set. Therefore, no ASCII byte (0x00-0x7F) can
            appear as part of any other character. The first byte of a multibyte sequence that represents a non-ASCII character is always in the
            range 0xC0 to 0xFD and it indicates how many bytes follow for this character. All further bytes in a multibyte sequence are in the
            range 0x80 to 0xBF. This allows easy resynchronization and makes the encoding stateless and robust against missing bytes.All possible
            231 UCS codes can be encoded. UTF-8 encoded characters may theoretically be up to six bytes long, however 16-bit BMP characters are
            only up to three bytes long. The sorting order of Bigendian UCS-4 byte strings is preserved. The bytes 0xFE and 0xFF are never used
            in the UTF-8 encoding.
            */

            if (myBuff == null || myBuff.GetLength(0) < minLengthBOM)
                return Encoding.Default;

            bool getEncodingError = false;
            var cntEnc1B = new Dictionary<Encoding, int>();
            var cntEnc2B = new Dictionary<Encoding, int>();

            this.nlEncoding = TNewLineEncoding.EUnknown;

            cntEnc2B[Encoding.Unicode] = new int();
            cntEnc1B[Encoding.UTF8] = new int();

            Encoding encoding866 = Encoding.Default;
            Encoding encoding1250 = Encoding.Default;
            Encoding encoding1251 = Encoding.Default;
            Encoding encoding1252 = Encoding.Default;
            Encoding encodingKoi8r = null;

            try
            {
                encoding866 = Encoding.GetEncoding(866);
                encoding1250 = Encoding.GetEncoding(1250);
                encoding1251 = Encoding.GetEncoding(1251);
                encoding1252 = Encoding.GetEncoding(1252);
                cntEnc1B[encoding866] = new int();
                cntEnc1B[encoding1250] = new int();
                cntEnc1B[encoding1251] = new int();
                cntEnc1B[encoding1252] = new int();
            }
            catch (Exception) { getEncodingError = true; }

            try { encodingKoi8r = Encoding.GetEncoding(20866); }
            catch { }

            int countRTL = 0;
            int countLatin = 0;
            int countNonLatin = 0;
            bool done = true;
            bool bDetected1250 = false;

            this.detected = false;
            Encoding myEncoding = encoding1252; // Encoding.Default;
            this.bomLength = 0;

            // Detect Encoding with BOMs.
            foreach (KeyValuePair<Encoding, byte[]> pair in sigBase)
            {
                for (var i = 0; i < pair.Value.GetLength(0); i++)
                {
                    if (myBuff[i] != pair.Value[i])
                    {
                        done = false;
                        break;
                    }
                }
                if (done)
                {
                    myEncoding = pair.Key;
                    this.bomLength = pair.Value.Length;
                    detected = true;
                    break;
                }
                else done = true;
            }
            // Detect encodings without BOM-s.
            int min = (myBuff.Length > this.detectBuffLength) ? this.detectBuffLength : myBuff.Length;

            for (int i = 0; i < min; i++)
            {
                // Detect RTL chars
                if (i < myBuff.Length - 1)
                {
                    UInt16 token = (UInt16)((UInt16)myBuff[i] * 256 + myBuff[i + 1]);

                    for (var j = 0; j < sigRTL.GetLength(0); j++)
                    {
                        if (token >= sigRTL[j, 0] && token <= sigRTL[j, 1])
                        {
                            countRTL++;
                        }
                    }
                }
                // Detect Asian letters.
                for (var itr = 0; itr < this.SAsianLangList.Length; itr++)
                {
                    if (i < myBuff.Length - this.SAsianLangList[itr].bytes + 1)
                    {
                        UInt32 token = 0;

                        for (int j = 0; j < this.SAsianLangList[itr].bytes; j++)
                        {
                            token = (UInt32)(token * 256 + (UInt32)myBuff[i + j]);
                        }
                        if (token >= this.SAsianLangList[itr].start && token <= this.SAsianLangList[itr].end)
                        {
                            this.SAsianLangList[itr].count++;
                        }
                    }
                }
                // Detect new line sign encoding style.
                if (myBuff[i] == '\n')
                {
                    this.nlEncoding |= TNewLineEncoding.EUnixLinux;
                }
                else if (myBuff[i] == '\r')
                {
                    this.nlEncoding |= TNewLineEncoding.EMacOS;
                }
                if (detected) continue;

                if (i < myBuff.Length - 1)
                {
                    // 110yyyyy 10zzzzzz - UTF8 mask.
                    if (((short)myBuff[i] & (short)0xE0) == 0xC0)
                    {
                        if (((short)myBuff[i + 1] & (short)0xC0) == 0x80)
                        {
                            cntEnc1B[Encoding.UTF8] += 2; continue;
                        }
                    }
                    // 00000000 0zzzzzzz - Unicode mask.
                    if (myBuff[i] == 0)
                    {
                        if (((short)myBuff[i + 1] != 0) && (((short)myBuff[i + 1] & (short)0x80) == 0))
                        {
                            cntEnc2B[Encoding.Unicode] += 2; continue;
                        }
                    }
                }
                if (i < myBuff.Length - 2)
                {
                    // 1110xxxx 10yyyyyy 10zzzzzz - UTF8 mask.
                    if (((short)myBuff[i] & (short)0xF0) == 0xE0)
                    {
                        if (((short)myBuff[i + 1] & (short)0xC0) == 0x80)
                        {
                            if (((short)myBuff[i + 2] & (short)0xC0) == 0x80)
                            {
                                cntEnc1B[Encoding.UTF8] += 3; continue;
                            }
                        }
                    }
                }
                if (getEncodingError) continue;

                byte nextByte = (i > myBuff.Length - 2) ? (byte)0 : myBuff[i + 1];
                byte prevByte = (i < 1) ? (byte)0 : myBuff[i - 1];
                byte curr = myBuff[i];

                if (char.IsLetter((char)myBuff[i]))
                {
                    if (isLatin(myBuff[i])) countLatin++;
                    else countNonLatin++;

                    if (false == bDetected1250)
                    {                        
                        if (nextByte == 0x65 && (myBuff[i] == 0xE8 || myBuff[i] == 0xC8))
                            bDetected1250 = true;
                    }
                }
                else
                {
                    if (countLatin > 0 && countNonLatin > 0)
                    {
                        if (countLatin > countNonLatin)
                        {
                            cntEnc1B[encoding1252] += (countLatin + countNonLatin);
                        }
                    }
                    countLatin = countNonLatin = 0;
                }
                if (myBuff[i] >= 0x80 && myBuff[i] <= 0xAF && (myBuff[i] < 0x93 || myBuff[i] > 0x96))
                {
                    if (isLatin(prevByte) == false && isLatin(nextByte) == false)
                        cntEnc1B[encoding866]++;
                }
                else if ((myBuff[i] >= 0xC0 && myBuff[i] < 0xE0)
                      || (myBuff[i] >= 0xF0 && myBuff[i] <= 0xFF))
                {
                    bool bCurr = (curr == 0xDB || curr == 0xDC || curr == 0xDD || curr == 0xDE || curr == 0xDF);
                    bool bNext = (nextByte == 0xDB || nextByte == 0xDC || nextByte == 0xDD || nextByte == 0xDE || nextByte == 0xDF);

                    if ((bCurr && bNext) || ((curr == nextByte) && (curr == 0xC4 || bCurr)))
                    {
                        cntEnc1B[encoding866] += 2;
                    }
                    else
                    {
                        cntEnc1B[encoding1251]++;
                    }
                }
                else if (curr == nextByte && (curr == 0xB0 || curr == 0xB1 || curr == 0xB2))
                    cntEnc1B[encoding866] += 2;
            }
            // Scaning results processing.
            this.initData.langRTL = false;

            if (countRTL > min / 4 + 1)
            {
                this.initData.langRTL = true;
            }
            if (detected == false)
            {
                int j = 0;
                int total2B = cntEnc2B[Encoding.Unicode];

                if (bDetected1250)
                {
                    cntEnc1B[encoding1250] = cntEnc1B[encoding1252];
                    cntEnc1B[encoding1252] = 0;
                }
                Dictionary<Encoding, int> resultDictionary = (total2B > min - total2B) ? cntEnc2B : cntEnc1B;

                foreach (KeyValuePair<Encoding, int> pair in resultDictionary)
                {
                    if (pair.Value > j)
                    {
                        j = pair.Value;
                        myEncoding = pair.Key;
                    }
                }
                if (myEncoding == encoding1251)
                {
                    CheckRussForKoi8R(myBuff, encoding1251, encodingKoi8r,  out myEncoding);
                }
            }
            this.initData.recommendedFont = this.initData.preferredFont;

            if (myEncoding.CodePage == 1200 || myEncoding.CodePage == 1201
                || myEncoding.CodePage == 65000 || myEncoding.CodePage == 65001)
            {
                int j = 0;

                for (int i = 0; i < this.SAsianLangList.Length; i++)
                {
                    if (this.SAsianLangList[i].count > j)
                    {
                        j = this.SAsianLangList[i].count;
                        this.initData.recommendedFont = this.SAsianLangList[i].sFontName;
                    }
                }
            }
            else // Non UTF is not detecting as RTL, alas.
            {
                this.initData.langRTL = false;
            }
            for (int i = 0; i < this.SAsianLangList.Length; i++) // CR-058
                this.SAsianLangList[i].count = 0;

            this.prevRTL = this.initData.langRTL;
            return myEncoding;
        }

        private const int MAX_LEN_DETECT = 400;
        private const int MIN_LEN_DETECT = 100;
        private static Regex rx1 = new Regex("[аеёиоуыэюя]", RegexOptions.IgnoreCase | RegexOptions.Compiled); // 0.57 for "[бвгджзйклмнпрстфхцчшщъь]";

        private void CheckRussForKoi8R(byte[] myBuff, Encoding e1251, Encoding eKoi, out Encoding myEncoding)
        {
            myEncoding = e1251;
            if (myBuff.Length < MIN_LEN_DETECT || eKoi == null) return;

            int min = (myBuff.Length > MAX_LEN_DETECT) ? MAX_LEN_DETECT : myBuff.Length;
            byte[] test = new byte[min];
            int i, j;

            for (i = 0, j = 0; j < myBuff.Length && i < min; j++)
            {
                if (myBuff[j] >= 0xC0 || myBuff[j] == 0xA8 || myBuff[j] == 0xB8
                    || myBuff[j] == 0xA3 || myBuff[j] == 0xB3)
                {
                    test[i++] = myBuff[j];
                }
            }
            if (i >= MIN_LEN_DETECT)
            {
                string s1 = e1251.GetString(test);
                string s2 = eKoi.GetString(test);
                double rg1 = Math.Abs(0.43 - ((double)rx1.Matches(s1).Count / (double)i));
                double rg2 = Math.Abs(0.43 - ((double)rx1.Matches(s2).Count / (double)i));

                if (rg2 < rg1) myEncoding = eKoi;
            }
        }
        

        private bool isLatin(byte ch)
        {
            return ((ch >= 0x41 && ch <= 0x5A) || (ch >= 0x61 && ch <= 0x7A));
        }

        public void openFileWithoutDetect()
        {
            this.detectEncoding = () =>
            {
                Encoding enc = this.curEncoding;
                this.getEncoding();
                return enc;
            };
            //this.initData.langRTL = this.prevRTL;
            this.openFile();
            this.detectEncoding = this.getEncoding;
        }

        public override void openFile()
        {          
            this.readFile();
            this.dCalc = new DumpParamCalculator();
            this.initData.hexDump.startIndex = 0;

            if (myBuff.Length == 0)
            {
                this.changed = false;
                return;
            }
            string consoleFont = "Lucida Console";

            if (this.isFileBinary())
            {
                this.initData.recommendedFont = consoleFont;
                this.makeHexDump();
            }
            else
            {
                this.encoding = this.detectEncoding();
                if (this.bomLength < 0) this.bomLength = 0;

                this.initData.container = this.curEncoding.GetString(myBuff, bomLength, myBuff.Length - bomLength );

                //if (this.initData.recommendedFont == "TITUS Cyberbit Basic") { }
                // Max. 3 bytes per symbol in the utf-8. 
                if (this.detected == false && this.strlen(this.initData.container) < (myBuff.Length - this.bomLength) / 3)
                {
                    this.initData.binary = true;
                    this.initData.recommendedFont = consoleFont;
                    this.makeHexDump();
                }
                else
                {
                    this.initData.container = this.initData.container.Replace("\0", string.Empty);
                    this.detected = false;
                }
            }
            this.changed = true;
        }

        private int strlen(string s)  
        {   
            string temp = s + "\0";
            int length = 0;
            
            while (temp[length++] != '\0');           
            return length;
        }

        public void saveFile(string path)
        {
            if (this.checkBeforeSave() == false)
            {
                throw new System.InvalidOperationException(ExtendedData.Constants.STR_ENCODINGWRONG_MSG);
            }
            FileAttributes oldAttr = FileAttributes.Normal;
            this.initData.curFile = path;

            try
            {
                if (this.nlEncoding == TNewLineEncoding.EMicrosoft
                    || this.nlEncoding == TNewLineEncoding.EUnknown)
                {
                    this.initData.ReplaceToRN();
                }
                else if (this.nlEncoding == TNewLineEncoding.EMacOS)
                {
                    this.initData.container = this.initData.container.Replace('\n', '\r');
                }
                FileAttributes newAttr = FileAttributes.Normal;

                if (File.Exists(this.initData.curFile))
                {
                    try
                    {
                        oldAttr = File.GetAttributes(this.initData.curFile);
                        File.SetAttributes(this.initData.curFile, newAttr);
                    }
                    catch (Exception) { }
                }
                Encoding saveEnc = this.curEncoding;
                // http://stackoverflow.com/questions/2502990/create-text-file-without-bom
                if (this.bomLength == 0)
                {
                    if (this.curEncoding.BodyName == Encoding.UTF8.BodyName)
                    {
                        saveEnc = new UTF8Encoding(false);
                    }
                }
                // http://stackoverflow.com/questions/5958495/append-data-to-byte-array
                using (MemoryStream ms = new MemoryStream())
                {
                    using (StreamWriter writer = new StreamWriter(ms, saveEnc))
                    {
                        this.myBuff = null;
                        writer.Write(this.initData.container);
                    }
                    this.myBuff = ms.ToArray();
                    File.WriteAllBytes(this.initData.curFile, this.myBuff);
                }
                // Calculate new BOM length:
                this.bomLength = 0;

                foreach (KeyValuePair<Encoding, byte[]> pair in sigBase)
                {
                    if (pair.Key.BodyName == this.curEncoding.BodyName)
                    {
                        this.bomLength = pair.Value.Length;
                        break;
                    }
                }
            }
            finally
            {
                try { File.SetAttributes(this.initData.curFile, oldAttr); }
                catch (Exception) { }
            }
        }

        public void newFile()
        {
            this.clearData();
            this.initData.curFile = "";
            this.initData.binary = false;

            this.nlEncoding = TNewLineEncoding.EUnknown;            
            this.curEncoding = Encoding.UTF8;
            this.changed = true;
        }

        public void clearData()
        {
            this.myBuff = null;
            this.bomLength = -1; // Unknown;
            this.initData.container = "";
            this.dCalc = new DumpParamCalculator();
            this.initData.hexDump.startIndex = 0;
        }

        public void encodeFile()
        {
            this.changed = false;

            if (this.myBuff == null || this.myBuff.Length == 0)
                return;

            this.initData.container = this.curEncoding.GetString(this.myBuff, this.bomLength, myBuff.Length - bomLength);
            this.changed = true; // To notify View.
        }

        public void previewBeforeSave()
        {
            byte[] previewBuff = this.curEncoding.GetBytes(this.initData.container);
            this.initData.container = this.curEncoding.GetString(previewBuff);
            this.changed = true;
        }

        private bool checkBeforeSave()
        {
            byte[] previewBuff = this.curEncoding.GetBytes(this.initData.container);
            string tmp = this.curEncoding.GetString(previewBuff);

            return (tmp == this.initData.container);
        }
    }
}