using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ShubusViewer.Engines
{
    class DlgManager
    {
        private List<Form> mFormList = new List<Form>();
        private Form prnt;

        public DlgManager(Form aPrnt)
        {
            if (aPrnt == null)
            {
                throw new ArgumentException();
            }
            this.prnt = aPrnt;
        }

        public void UpdateTopStatus()
        {
            this.mFormList.ForEach(c =>
            { 
               if (c != null && c.TopMost != prnt.TopMost)
               {
                  c.TopMost = prnt.TopMost; 
               }});
        }

        public void Add(Form f)
        {
            if (f != null)
            {
                f.TopMost = this.prnt.TopMost;
                mFormList.Add(f);
            }
        }
    }
}