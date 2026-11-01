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
    public partial class Sample : Form
    {
        public Sample()
        {
            InitializeComponent();
            Load += Sample_Load;
        }

        private void Sample_Load(object sender, EventArgs e)
        {
            ReportPinHelper.Attach(this);
            ReportForm.ReportHeaderBrandHelper.Attach(this);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (ReportPinHelper.TryHandleKey(this, keyData))
                return true;

            if (Services.FormNavigationService.TryHandle(keyData))
                return true;

            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
