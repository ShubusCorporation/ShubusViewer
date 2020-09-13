using SearchData;
using System;
using System.Windows.Forms;
using ExtendedData;
using StateMachine;

namespace ShubusViewer
{
    public partial class Form1 : Form
	{
        public SearchDialogData searchDTO = new SearchDialogData();

        DlgSearch CreateSearchDlg()
        {
            Properties.Settings ps = Properties.Settings.Default;

            this.searchDTO.txtFind = this.textBox1.SelectionLength > 0
                ? this.textBox1.SelectedText
                : ps.textDlgSearch;

            this.searchDTO.findSameCase = ps.cb1DlgSearch;
            this.searchDTO.replaceWholeWord = ps.cb2DlgSearch;
            this.searchDTO.replaceSameCase = ps.cb3DlgSearch;

            var dlgSearch = new DlgSearch(this.searchDTO, this.SearchCallback, this.replaceCallback);
            return dlgSearch;
        }

        public void SearchCallback()
        {
            if (this.txtIndex > -1)
            {
                if (this.findNext() == false)
                    this.doNewSearch();
            }
            else this.findFirst();
        }

        private void doNewSearch()
        {
            if (string.IsNullOrEmpty(this.searchDTO.txtFind)) {
                return;
            }
            string msg = "Data:\r\n\r\n" + this.searchDTO.txtFind + "\r\n\r\n" + Constants.STR_LASTDATA_MSG;

            if (MessageBox.Show(msg, Constants.STR_APP_TITLE
              , MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                this.findFirst();
            }
            else textBox1.Focus();
        }

        private void findFirst()
        {
            if (this.myViewManager.getMode(
                ViewManager.EViewMode.EModeBinary) ==
                ViewManager.EViewMode.EModeBinary)
            {
                findFirstBin();
            }
            else
            {
                findFirstText();
            }
        }

        private void findFirstBin()
        {
            if (this.myController.searchInDump(this.searchDTO.txtFind, 0))
            {
                this.myController.processOperation(TypeAction.EDumpUpdate);

                if (this.mySharedData.basicInfo.hexDump.startIndex > 0)
                {
                    this.txtIndex = 16;
                }
                else this.txtIndex = 0;

                this.textBox1.SelectionStart = this.txtIndex;

                if (this.mySharedData.basicInfo.hexDump.isHex == false)
                {
                    this.textBox1.SelectionLength = this.searchDTO.txtFind.Length;
                }
            }
            else
            {
                this.txtIndex = -1;

                MessageBox.Show(Constants.STR_NOTFOUND_MSG + this.searchDTO.txtFind, Constants.STR_APP_TITLE
                  , MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void findFirstText()
        {
            this.txtIndex = textBox1.Text.IndexOf(this.searchDTO.txtFind, 0, this.searchDTO.sComparison);

            if (this.txtIndex == -1)
            {
                MessageBox.Show(Constants.STR_NOTFOUND_MSG + this.searchDTO.txtFind, Constants.STR_APP_TITLE
                  , MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            this.textBox1.SelectionStart = txtIndex;
            this.textBox1.SelectionLength = this.searchDTO.txtFind.Length;
            this.textBox1.ScrollToCaret();
            this.txtIndex += this.searchDTO.txtFind.Length;
        }

        private void replaceCallback()
        {
            int occur = 0;
            string news = "";
            string olds = textBox1.Text;
            int oldSelection = this.textBox1.SelectionStart;
            int oldSelectionL = this.textBox1.SelectionLength;

            if (string.IsNullOrEmpty(this.searchDTO.txtWhat)) return;
            char chLeft = ' ';

            while (true)
            {
                int ind = olds.IndexOf(this.searchDTO.txtWhat, 0, this.searchDTO.rComparison);

                if (ind < 0)
                {
                    news += olds;
                    break;
                }
                if (this.searchDTO.replaceWholeWord)
                {
                    chLeft = (ind > 0) ? olds[ind - 1] : chLeft;
                    char chRight = (ind + this.searchDTO.txtWhat.Length > olds.Length - 1) ?
                        ' ' : olds[ind + this.searchDTO.txtWhat.Length];

                    if (char.IsLetterOrDigit(chLeft) || char.IsLetterOrDigit(chRight))
                    {
                        news += olds.Substring(0, ind + this.searchDTO.txtWhat.Length);
                        chLeft = olds[ind + this.searchDTO.txtWhat.Length - 1];
                        olds = olds.Substring(ind + this.searchDTO.txtWhat.Length);
                        continue;
                    }
                    else occur++;
                }
                else occur++;

                news += olds.Substring(0, ind);
                news += this.searchDTO.txtWith;
                chLeft = olds[ind + this.searchDTO.txtWhat.Length - 1];
                olds = olds.Substring(ind + this.searchDTO.txtWhat.Length);
            }
            if (occur > 0)
                textBox1.Text = news;

            try
            {
                this.textBox1.SelectionStart = oldSelection;
                this.textBox1.SelectionLength = oldSelectionL;
            }
            catch
            {
                this.textBox1.SelectionStart = textBox1.SelectionLength = 0;
            }
            MessageBox.Show(occur.ToString() + Constants.STR_REPLACE_DLG
            , Constants.STR_APP_TITLE, MessageBoxButtons.OK
            , MessageBoxIcon.Information);
        }

        private bool findNext()
        {
            if (this.myViewManager.getMode(
                ViewManager.EViewMode.EModeBinary) ==
                ViewManager.EViewMode.EModeBinary)
            {
                return findNextBin();
            }
            return findNextText();
        }

        private bool findNextBin()
        {
            if (this.myController.searchInDump(this.searchDTO.txtFind,
                this.mySharedData.basicInfo.hexDump.startIndex +
                this.searchDTO.txtFind.Length +
                this.txtIndex))
            {
                this.myController.processOperation(TypeAction.EDumpUpdate);

                if (this.mySharedData.basicInfo.hexDump.startIndex > 0)
                {
                    this.txtIndex = 16;
                }
                else this.txtIndex = 0;

                if (this.mySharedData.basicInfo.hexDump.isHex == false)
                {
                    string strSub = textBox1.Text.Substring(0, this.txtIndex + this.searchDTO.txtFind.Length);

                    int ind = strSub.LastIndexOf(
                        this.searchDTO.txtFind,
                        searchDTO.sComparison
                        );

                    this.textBox1.SelectionStart = ind;
                    this.textBox1.SelectionLength = this.searchDTO.txtFind.Length;
                }
                else
                {
                    this.textBox1.SelectionStart = 139;
                    this.textBox1.SelectionLength = 1;
                }
            }
            else
            {
                this.txtIndex = -1;
                return false;
            }
            return true;
        }

        private bool findNextText()
        {
            if (this.txtIndex == -1)
                return false;

            this.txtIndex = textBox1.Text.IndexOf(searchDTO.txtFind
                , this.txtIndex, searchDTO.sComparison);

            if (this.txtIndex == -1)
                return false;

            this.textBox1.SelectionStart = txtIndex;
            this.textBox1.SelectionLength = searchDTO.txtFind.Length;
            this.textBox1.ScrollToCaret();
            this.txtIndex += searchDTO.txtFind.Length;
            return true;
        }

        private void showFindDialog(int aTab)
        {
            bool secondTab = (this.myViewManager.getMode(
                ViewManager.EViewMode.EModeTextLocked) ==
                ViewManager.EViewMode.EModeNone
                );

            if (secondTab == false && aTab == 1) return;

            var dlgSearch = CreateSearchDlg();
            dlgSearch.secondTab = secondTab;
            dlgSearch.ActiveTab = aTab;

            dlgSearch.caseSensitive = (this.myViewManager.getMode(
             ViewManager.EViewMode.EModeBinary) ==
             ViewManager.EViewMode.EModeNone
             );

            dlgSearch.Show(this);
        }
    }
}