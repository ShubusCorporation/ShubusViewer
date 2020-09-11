using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Text;

namespace ShubusViewer
{
    class AppRecent
    {
        public struct SRecentInfo
        {
            public int enc;
            public int sel;
        }

        private class SRecentItem
        {
            public string path;
            public string descr;
            public string tag;
            public string enc;
            
            public SRecentItem(string path, string descr, string tag, string enc)
            {
                this.path = path;
                this.descr = descr;
                this.tag = tag;
                this.enc = enc;
            }

            public SRecentItem()
                : this(string.Empty, string.Empty, string.Empty, string.Empty)
            {
            }
        }

        private string stgPath = string.Empty;
        private List<SRecentItem> MRUlist = new List<SRecentItem>();
        private ContextMenuStrip myMenuStrip;
        private const string recentFile = "\\Recent.dat";
        private EventHandler myHandler;
        private FileSystemWatcher watcher;
        private Form pForm = null; // for Invoke launch.

        public AppRecent(Form parent, ContextMenuStrip menuStrip, EventHandler onClick, string cfgPath)
        {
            // http://stackoverflow.com/questions/884260/how-can-i-watch-the-user-config-file-and-reload-the-settings-when-it-changes
            // The following code will return the path to the user.config file. You need to add a reference to System.Configuration.dll

            this.stgPath = cfgPath;
            this.myMenuStrip = menuStrip;
            this.myHandler = onClick;

            try
            {
                if (Directory.Exists(stgPath) == false)
                    Directory.CreateDirectory(stgPath);

                this.LoadRecentList();
                this.buildMenuStrip();
            }
            catch (Exception ee) { MessageBox.Show(ee.Message + "\r\n" + ee.StackTrace); }

            this.pForm = parent;
            this.watcher = new FileSystemWatcher(this.stgPath, recentFile.Trim(new char[]{' ', '\\'}));
            this.watcher.Changed += new FileSystemEventHandler(watcher_Changed);
            this.watcher.EnableRaisingEvents = true;
        }

        void watcher_Changed(object sender, FileSystemEventArgs e)
        {
            // http://social.msdn.microsoft.com/Forums/en/csharpgeneral/thread/f68a4510-2264-41f2-b611-9f1633bca21d
            try
            {
                ((FileSystemWatcher)sender).EnableRaisingEvents = false; // Will rise exception on error.

                this.pForm.Invoke(new Action(() =>
                    {
                        this.LoadRecentList();
                        this.buildMenuStrip();
                    }));
                this.watcher.EnableRaisingEvents = true; 
            }
            catch (Exception ee) { MessageBox.Show(ee.Message + "\r\n" + ee.StackTrace); }
        }

        private void buildMenuStrip()
        {
            for (int i = 0; i < this.myMenuStrip.Items.Count; i++ )
            {
                if (this.myMenuStrip.Items[i] is ToolStripItem)
                {
                    while (i < this.myMenuStrip.Items.Count && this.myMenuStrip.Items[i].Tag != null)
                    {
                        this.myMenuStrip.Items.Remove(this.myMenuStrip.Items[i]);
                    }
                }
            }
            for (int i = 0; i < this.myMenuStrip.Items.Count; i++)
            {
                while (i < this.myMenuStrip.Items.Count
                       && this.myMenuStrip.Items[i] is ToolStripSeparator
                       && this.myMenuStrip.Items[i].Name == "")
                {
                    this.myMenuStrip.Items.Remove(this.myMenuStrip.Items[i]);
                }
            }
            if (this.MRUlist.Count <= 0) return;
            this.MRUlist = this.MRUlist.OrderBy(x => Utils.Utils.getExtension(x.path)).ThenBy(x => x.descr.ToLower()).ToList();
            string curExt = Utils.Utils.getExtension(this.MRUlist.ElementAt(0).path);
            int fCount = 0;

            foreach (SRecentItem item in this.MRUlist)
            {
                string newExt = Utils.Utils.getExtension(item.path);

                if (newExt != curExt) fCount = 0;

                ToolStripMenuItem fileItem = new ToolStripMenuItem((++fCount).ToString() + ".  " + item.descr, null, this.myHandler);
                ExtendedData.SRecentTag stag;

                stag.sel = Convert.ToInt32(item.tag);
                stag.encoding = Convert.ToInt32(item.enc);                
                fileItem.ToolTipText = item.path;

                if (newExt != curExt)
                {
                    curExt = newExt;
                    ToolStripSeparator sep = new ToolStripSeparator();
                    this.myMenuStrip.Items.Add(sep);
                }
                fileItem.Tag =  (object)stag;
                this.myMenuStrip.Items.Add(fileItem);
            }
        }

        private void LoadRecentList()
        {
            MRUlist.Clear();
            // http://social.msdn.microsoft.com/Forums/vstudio/en-US/6b8c4dd6-3c63-4c90-a98a-478c07ada10f/pause-streamreader-the-process-cannot-access-the-file-because-it-is-being-used-by-another?forum=csharpgeneral
            using (FileStream stream = new FileStream(this.stgPath + recentFile, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader listToRead = new StreamReader(stream))
                {
                    try
                    {
                        while (true)
                        {
                            SRecentItem record = new SRecentItem();
                            string test = listToRead.ReadLine();

                            if (test == null) break;

                            // Compability with old versions
                            if (Regex.IsMatch(test, "^[\\d]+$"))
                            {
                                record.tag = test;

                                if ((record.enc = listToRead.ReadLine()) == null
                                    || (record.path = listToRead.ReadLine()) == null)
                                {
                                    break;
                                }
                                record.descr = Utils.Utils.getShortName(record.path);
                            }
                            else // new vesion format
                            {
                                record.descr = test;

                                if ((record.tag = listToRead.ReadLine()) == null
                                    || (record.enc = listToRead.ReadLine()) == null
                                    || (record.path = listToRead.ReadLine()) == null)
                                {
                                    break;
                                }
                            }
                            this.MRUlist.Add(record);
                        }
                        listToRead.Close();
                    }
                    catch (Exception ee) { MessageBox.Show(ee.Message + "\r\n" + ee.StackTrace); }
                }
            }
        }

        public bool isFileRecent(string path)
        {
            foreach (SRecentItem item in this.MRUlist)
            {
                if (string.Compare(item.path, path, true) == 0)
                    return true;
            }
            return false;
        }

        public SRecentInfo getRecentInfo(string path)
        {
            SRecentInfo ret = new SRecentInfo();
            ret.enc = -1;
            ret.sel = 0;

            foreach (SRecentItem item in this.MRUlist)
            {
                try
                {
                    if (string.Compare(item.path, path, true) == 0)
                    {
                        ret.enc = Convert.ToInt32(item.enc);
                        ret.sel = Convert.ToInt32(item.tag);
                    }
                }
                catch {}
            }
            return ret;
        }

        public bool isListNotEmpty()
        {
            return (this.MRUlist.Count > 0);
        }

        public void AddRecentFile(string path, string descr, int tag, int enc)
        {
            if (this.isFileRecent(path) == false) //prevent duplication on recent list
            {
                SRecentItem item = new SRecentItem(path, descr, tag.ToString(), enc.ToString());
                MRUlist.Add(item);
            }
            this.saveRecentFile();
        }

        public void DeleteRecentFile(string path)
        {
            SRecentItem delIt = new SRecentItem();

            foreach (SRecentItem item in this.MRUlist)
            {
                if (string.Compare(item.path, path, true) == 0)
                {
                    delIt = item;
                }
            }
            if (delIt.path == "") return;
 
            this.MRUlist.Remove(delIt);
            this.saveRecentFile();
        }

        public void updateRecentFile(string path, int tag, int enc)
        {
            for (int i = 0; i < this.MRUlist.Count; i++)
            {
                if (this.MRUlist[i].path == path)
                {
                    string descr = this.MRUlist[i].descr;

                    this.MRUlist[i] = new SRecentItem(path, descr, tag.ToString(), enc.ToString());
                    this.buildMenuStrip();
                    this.saveRecentFile();
                    break;
                }
            }
        }

        private void saveRecentFile()
        {
            try
            {
                this.watcher.EnableRaisingEvents = false;
                string rFile = this.stgPath + recentFile;
                StreamWriter itemToWrite = new StreamWriter(rFile);
                StringBuilder bld = new StringBuilder();

                foreach (SRecentItem item in MRUlist)
                {
                    bld.AppendLine(item.descr);
                    bld.AppendLine(item.tag);
                    bld.AppendLine(item.enc);
                    bld.AppendLine(item.path);
                }
                itemToWrite.Write(bld.ToString());
                itemToWrite.Flush(); // Write stream to the file
                itemToWrite.Close(); // Close the stream and reclaim memory
                this.watcher.EnableRaisingEvents = true;
                this.buildMenuStrip();
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message, ExtendedData.Constants.STR_APP_TITLE,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}