using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Services.Description;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace AlMadinaShahzad.Model
{
    public partial class YesOrNoMessage: Sample
    {
        public YesOrNoMessage(string message, string title)
        {
            InitializeComponent();
            MessageFormLogoHelper.Apply(pictureBox1);
            lblM.Text = message;
            this.Text = title;

            // Auto size set karne ke liye
            lblM.AutoSize = true;
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            // Optional: Agar aap chahte hain ke thodi padding ho to:
            this.Padding = new Padding(10);
        }

        private void btnYes_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Yes;
            this.Close();
        }

        private void btnNo_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.No;
            this.Close();
        }
    }
}
