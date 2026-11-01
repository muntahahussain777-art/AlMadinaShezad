using System;

namespace AlMadinaShahzad.ReportForm
{
    public partial class frmStockDieselForm
    {
        public void SetStockReportFilters(DateTime fromDate, DateTime toDate, string searchText)
        {
            Load += (s, e) =>
            {
                fromdate.Value = fromDate;
                todate.Value = toDate;
                txtSearch.Text = searchText ?? string.Empty;
                btnSearch.PerformClick();
            };
        }
    }
}
