using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SharedData;

namespace FSProvider
{
    class DirectoryExplorer
    {
        static DirectoryExplorer obj = null;
        ModelInitData sharedData = null;

        static string curFile = string.Empty;
        int curFileNumber;
        List<string> files = new List<string>();

        // Same as "((\\.jpg)|(\\.jpeg)|(\\.bmp)|(\\.gif)|(\\.png)|(\\.ico)|(\\.emf)|(\\.wmf)|(\\.webp))$" from AppWebModel
        static string[] pictureExts = { ".jpg", ".jpeg", ".bmp", ".gif", ".png", ".ico", ".emf", ".wmf", ".webp" };
        public static IEnumerable<FileInfo> GetFilesByType(DirectoryInfo dir, string ext)
        {
            if (string.IsNullOrEmpty(ext) || string.IsNullOrWhiteSpace(ext)) {
                ext = string.Empty;
            }
            if (pictureExts.Any(pext => pext.Contains(ext.ToLower())))
                return GetFilesByExtensions(dir, pictureExts);
            return GetFilesByExtensions(dir, ext);
        }

        // https://stackoverflow.com/questions/3527203/getfiles-with-multiple-extensions
        public static IEnumerable<FileInfo> GetFilesByExtensions(DirectoryInfo dir, params string[] extensions)
        {
            IEnumerable<FileInfo> ret = null;

            if (extensions.Length == 1)
            {
                var extMask = extensions[0];
                extMask = string.IsNullOrEmpty(extMask) ? "*" : "*." + extMask;
                ret = dir.GetFiles(extMask);
            }
            else if (extensions.Length > 1)
            {
                ret = dir.EnumerateFiles().Where(f => extensions.Contains(f.Extension));
            }
            return ret;
        }

        private void Init(string fn)
        {
            int i = 0;
            string curPath = Utils.Utils.getPath(fn);
            if (curPath[curPath.Length - 1] == ':') curPath += "\\"; // Fix it for Win7.

            curFile = fn;
            DirectoryInfo dir = new DirectoryInfo(curPath);
            this.files.Clear();
            curFileNumber = 0;

            string extMask = Utils.Utils.getExtension(fn);
            
            foreach (FileInfo file in GetFilesByType(dir, extMask))
            {
                if (extMask == "*" && !string.IsNullOrEmpty(Utils.Utils.getExtension(file.FullName)))
                {
                    continue;
                }
                if (string.Compare(fn, file.FullName, true) == 0)
                {
                    curFileNumber = i;
                }
                this.files.Add(file.FullName);
                i++;
            }
        }

        private DirectoryExplorer(ModelInitData shData)
        {
            this.sharedData = shData;
            Init(this.sharedData.curFile);
        }

        private void Check4ReLoad()
        {
            if (string.Compare(Utils.Utils.getPath(curFile), Utils.Utils.getPath(this.sharedData.curFile), true) != 0
                || string.Compare(Utils.Utils.getExtension(curFile), Utils.Utils.getExtension(this.sharedData.curFile), true) != 0)
            {
                this.Init(this.sharedData.curFile);
            }
        }

        private void CheckIfExists()
        {
            if (!File.Exists(this.files.ElementAt(this.curFileNumber)))
            {
                this.files.RemoveAt(this.curFileNumber);

                if (this.curFileNumber >= this.files.Count)
                {
                    this.curFileNumber = 0;
                }
            }
        }

        public string getNext()
        {
            this.Check4ReLoad();

            if (this.curFileNumber >= files.Count - 1)
            {
                this.curFileNumber = 0;
            }
            else this.curFileNumber++;

            CheckIfExists();
            return this.files.Count > 0 ?
                this.files.ElementAt(this.curFileNumber) : "";
        }

        public string getPrev()
        {
            this.Check4ReLoad();

            if (this.curFileNumber <= 0)
            {
                this.curFileNumber = this.files.Count - 1;
            }
            else this.curFileNumber--;

            CheckIfExists();
            return this.files.Count > 0 ?
                this.files.ElementAt(this.curFileNumber) : "";
        }

        public static DirectoryExplorer CreateObject(ModelInitData data)
        {
            if (string.Compare(data.curFile, curFile, true) != 0)
            {
                try
                {
                    obj = new DirectoryExplorer(data);
                }
                catch{}
            }
            return obj;
        }
    }
}