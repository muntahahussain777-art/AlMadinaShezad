using Microsoft.Reporting.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AlMadinaShahzad.ReportForm
{
    public partial class ExpenseReportForm : Sample
    {
        public ExpenseReportForm()
        {
            InitializeComponent();
        }
        public string ReportName { get; set; }
        public DataTable ReportData { get; set; }
        public string ReportPath { get; set; }
        private void reportViewer1_Load(object sender, EventArgs e)
        {
            // Clear existing data sources
            reportViewer1.LocalReport.DataSources.Clear();

            // Set the report path
            reportViewer1.LocalReport.ReportPath = ReportPath;

            // Set the data source
            ReportDataSource dataSource = new ReportDataSource("DataSet1", ReportData);
            reportViewer1.LocalReport.DataSources.Add(dataSource);

            // Refresh the report


            this.reportViewer1.RefreshReport();
        }
    }
}
