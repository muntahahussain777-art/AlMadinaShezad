using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AlMadinaShahzad
{
    public partial class SampleAdd1 : Sample
    {
        public SampleAdd1()
        {
            InitializeComponent();
            KeyPreview = true;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                Close();
                return true;
            }

            if (keyData == (Keys.Control | Keys.S) && btnSave != null && btnSave.Enabled && btnSave.Visible)
            {
                btnSave_Click(btnSave, EventArgs.Empty);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        public virtual void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        public virtual void btnDel_Click(object sender, EventArgs e)
        {

        }
        protected virtual void btnSave_Click(object sender, EventArgs e)
        {

        }
    }
}
