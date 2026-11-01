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
    public partial class SampleView : Sample
    {
        public SampleView()
        {
            InitializeComponent();
            KeyPreview = true;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.N) && btnAdd != null && btnAdd.Enabled && btnAdd.Visible)
            {
                btnAdd_Click(btnAdd, EventArgs.Empty);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

      

        public virtual void btnAdd_Click(object sender, EventArgs e)
        {

        }

        public virtual void txtSearch_TextChanged_1(object sender, EventArgs e)
        {

        }
    }
}
