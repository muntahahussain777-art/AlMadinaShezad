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
    public partial class CustomMessageForCLosing : Sample
    {
        // User ne kya choose kiya
        public enum CustomMessageResult
        {
            None,
            Customer,
            Dealer,
            Cancel
        }

        public CustomMessageResult Result { get; private set; } = CustomMessageResult.None;

        public CustomMessageForCLosing(string message, string title)
        {
            InitializeComponent();
            MessageFormLogoHelper.Apply(pictureBox1);

            // Label aur title set
            lblM.Text = message;
            this.Text = title;

            // Auto size settings
            lblM.AutoSize = true;
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.Padding = new Padding(10);

            // Buttons ka text (agar designer se na kiya ho)
            btnCustomer.Text = "Customer";
            btnDealer.Text = "Dealer";
            btnCancel.Text = "Cancel";
        }
        private void btnCustomer_Click(object sender, EventArgs e)
        {
            Result = CustomMessageResult.Customer;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnDealer_Click(object sender, EventArgs e)
        {
            Result = CustomMessageResult.Dealer;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Result = CustomMessageResult.Cancel;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
