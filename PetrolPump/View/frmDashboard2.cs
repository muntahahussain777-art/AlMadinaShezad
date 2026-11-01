using AlMadinaShahzad.ProjectConnection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace AlMadinaShahzad.View
{
    public partial class frmDashboard2 : Sample
    {
        public frmDashboard2()
        {
            InitializeComponent();
        }

        private void frmDashboard2_Load(object sender, EventArgs e)
        {
            chartload1();
            chartload2();
            chartload3();
        }

        private void chartload1()
        {
            string query = @"
        SELECT 
            MONTH(Date) AS MonthNumber,
            DATENAME(MONTH, Date) AS MonthName,
            SUM(CASE WHEN Peterleium = 'Petrol' THEN PurchaseRate ELSE 0 END) AS PetrolTotalPurchase,
            SUM(CASE WHEN Peterleium = 'Diesel' THEN PurchaseRate ELSE 0 END) AS DieselTotalPurchase
        FROM PurchaseLedger
        WHERE YEAR(Date) = YEAR(GETDATE())  -- Filter by current year, adjust as needed
        GROUP BY 
            MONTH(Date), 
            DATENAME(MONTH, Date)
        ORDER BY MonthNumber;";

            DataTable dt = new DataTable();

            string connectionString = projectconnection.conReturn();

            // SqlConnection object banayein
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, con);
                SqlDataAdapter adapter = new SqlDataAdapter(command);

                con.Open();
                adapter.Fill(dt);
                con.Close();
            }

            chart1.Series.Clear();
            chart1.ChartAreas.Clear();
            chart1.ChartAreas.Add(new ChartArea());
            chart1.ChartAreas[0].AxisX.Title = "Month";
            chart1.ChartAreas[0].AxisY.Title = "Total Purchase Rate";
            chart1.Titles.Add("Monthly Buy Rates for Petrol and Diesel");

            chart1.BackColor = Color.FromArgb(32, 36, 61);
            chart1.ChartAreas[0].BackColor = Color.FromArgb(32, 36, 61);
            chart1.ChartAreas[0].AxisX.LabelStyle.ForeColor = Color.White;
            chart1.ChartAreas[0].AxisY.LabelStyle.ForeColor = Color.White;
            chart1.ChartAreas[0].AxisX.TitleForeColor = Color.White;
            chart1.ChartAreas[0].AxisY.TitleForeColor = Color.White;
            chart1.Titles[0].ForeColor = Color.White;

            Series seriesPetrolTotal = new Series();
            seriesPetrolTotal.Name = "Petrol Total Purchase";
            seriesPetrolTotal.ChartType = SeriesChartType.Column;
            seriesPetrolTotal.Color = Color.Blue;

            Series seriesDieselTotal = new Series();
            seriesDieselTotal.Name = "Diesel Total Purchase";
            seriesDieselTotal.ChartType = SeriesChartType.Column;
            seriesDieselTotal.Color = Color.Red;

            foreach (DataRow row in dt.Rows)
            {
                string monthName = row["MonthName"].ToString();
                double petrolTotal = Convert.ToDouble(row["PetrolTotalPurchase"]);
                double dieselTotal = Convert.ToDouble(row["DieselTotalPurchase"]);

                seriesPetrolTotal.Points.AddXY(monthName, petrolTotal);
                seriesPetrolTotal.Points.Last().Label = petrolTotal.ToString();
                seriesPetrolTotal.LabelForeColor = Color.White;

                seriesDieselTotal.Points.AddXY(monthName, dieselTotal);
                seriesDieselTotal.Points.Last().Label = dieselTotal.ToString();
                seriesDieselTotal.LabelForeColor = Color.White;
            }

            chart1.Series.Add(seriesPetrolTotal);
            chart1.Series.Add(seriesDieselTotal);

            chart1.Legends.Add(new Legend("Legend"));
            chart1.Series["Petrol Total Purchase"].Legend = "Legend";
            chart1.Series["Diesel Total Purchase"].Legend = "Legend";
            chart1.Legends["Legend"].Docking = Docking.Bottom;
            chart1.Legends["Legend"].ForeColor = Color.White;
            chart1.Legends["Legend"].BackColor = Color.FromArgb(32, 36, 61);

            // Grid lines ko safed (white) karne ke liye
            chart1.ChartAreas[0].AxisX.LineColor = Color.White;
            chart1.ChartAreas[0].AxisY.LineColor = Color.White;
            chart1.ChartAreas[0].AxisX.MajorGrid.LineColor = Color.White;
            chart1.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.White;
        }

        private void chartload2()
        {
            string query = @"
        SELECT 
            MONTH(Date) AS MonthNumber,
            DATENAME(MONTH, Date) AS MonthName,
            SUM(CASE WHEN Petrol = 'Petrol' THEN SoldLitter ELSE 0 END) AS PetrolTotalSold,
            SUM(CASE WHEN Disel = 'Diesel' THEN SoldLitter ELSE 0 END) AS DieselTotalSold
        FROM Supplier
        WHERE YEAR(Date) = YEAR(GETDATE())  -- Filter by current year, adjust as needed
        GROUP BY 
            MONTH(Date), 
            DATENAME(MONTH, Date)
        ORDER BY MonthNumber;";

            DataTable dt = new DataTable();

            string connectionString = projectconnection.conReturn();

            // SqlConnection object banayein
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, con);
                SqlDataAdapter adapter = new SqlDataAdapter(command);

                con.Open();
                adapter.Fill(dt);
                con.Close();
            }

            chart2.Series.Clear();
            chart2.ChartAreas.Clear();
            chart2.ChartAreas.Add(new ChartArea());
            chart2.ChartAreas[0].AxisX.Title = "Month";
            chart2.ChartAreas[0].AxisY.Title = "Total Sold Litter";
            chart2.Titles.Add("Monthly Sold Unit for Petrol and Diesel");

            chart2.BackColor = Color.FromArgb(32, 36, 61);
            chart2.ChartAreas[0].BackColor = Color.FromArgb(32, 36, 61);
            chart2.ChartAreas[0].AxisX.LabelStyle.ForeColor = Color.White;
            chart2.ChartAreas[0].AxisY.LabelStyle.ForeColor = Color.White;
            chart2.ChartAreas[0].AxisX.TitleForeColor = Color.White;
            chart2.ChartAreas[0].AxisY.TitleForeColor = Color.White;
            chart2.Titles[0].ForeColor = Color.White;

            Series seriesPetrolSold = new Series();
            seriesPetrolSold.Name = "Petrol Total Sold";
            seriesPetrolSold.ChartType = SeriesChartType.Column;
            seriesPetrolSold.Color = Color.Blue;

            Series seriesDieselSold = new Series();
            seriesDieselSold.Name = "Diesel Total Sold";
            seriesDieselSold.ChartType = SeriesChartType.Column;
            seriesDieselSold.Color = Color.Red;

            foreach (DataRow row in dt.Rows)
            {
                string monthName = row["MonthName"].ToString();
                double petrolTotal = Convert.ToDouble(row["PetrolTotalSold"]);
                double dieselTotal = Convert.ToDouble(row["DieselTotalSold"]);

                seriesPetrolSold.Points.AddXY(monthName, petrolTotal);
                seriesPetrolSold.Points.Last().Label = petrolTotal.ToString();
                seriesPetrolSold.LabelForeColor = Color.White;

                seriesDieselSold.Points.AddXY(monthName, dieselTotal);
                seriesDieselSold.Points.Last().Label = dieselTotal.ToString();
                seriesDieselSold.LabelForeColor = Color.White;
            }

            chart2.Series.Add(seriesPetrolSold);
            chart2.Series.Add(seriesDieselSold);

            chart2.Legends.Add(new Legend("Legend"));
            chart2.Series["Petrol Total Sold"].Legend = "Legend";
            chart2.Series["Diesel Total Sold"].Legend = "Legend";
            chart2.Legends["Legend"].Docking = Docking.Bottom;
            chart2.Legends["Legend"].ForeColor = Color.White;
            chart2.Legends["Legend"].BackColor = Color.FromArgb(32, 36, 61);

            // Grid lines ko safed (white) karne ke liye
            chart2.ChartAreas[0].AxisX.LineColor = Color.White;
            chart2.ChartAreas[0].AxisY.LineColor = Color.White;
            chart2.ChartAreas[0].AxisX.MajorGrid.LineColor = Color.White;
            chart2.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.White;
        }

        private void chartload3()
        {
            string query = @"
        SELECT 
            MONTH(Date) AS MonthNumber,
            DATENAME(MONTH, Date) AS MonthName,
            SUM(CASE WHEN Peterleium = 'Petrol' THEN Quantity ELSE 0 END) AS PetrolTotalQuantity,
            SUM(CASE WHEN Peterleium = 'Diesel' THEN Quantity ELSE 0 END) AS DieselTotalQuantity
        FROM PurchaseLedger
        WHERE YEAR(Date) = YEAR(GETDATE())  -- Filter by current year, adjust as needed
        GROUP BY 
            MONTH(Date), 
            DATENAME(MONTH, Date)
        ORDER BY MonthNumber;";

            DataTable dt = new DataTable();

            string connectionString = projectconnection.conReturn();

            // SqlConnection object banayein
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, con);
                SqlDataAdapter adapter = new SqlDataAdapter(command);

                con.Open();
                adapter.Fill(dt);
                con.Close();
            }

            chart3.Series.Clear();
            chart3.ChartAreas.Clear();
            chart3.ChartAreas.Add(new ChartArea());
            chart3.ChartAreas[0].AxisX.Title = "Month";
            chart3.ChartAreas[0].AxisY.Title = "Total Quantity";
            chart3.Titles.Add("Stock for Petrol and Diesel");

            chart3.BackColor = Color.FromArgb(32, 36, 61);
            chart3.ChartAreas[0].BackColor = Color.FromArgb(32, 36, 61);
            chart3.ChartAreas[0].AxisX.LabelStyle.ForeColor = Color.White;
            chart3.ChartAreas[0].AxisY.LabelStyle.ForeColor = Color.White;
            chart3.ChartAreas[0].AxisX.TitleForeColor = Color.White;
            chart3.ChartAreas[0].AxisY.TitleForeColor = Color.White;
            chart3.Titles[0].ForeColor = Color.White;

            Series seriesPetrolQuantity = new Series();
            seriesPetrolQuantity.Name = "Petrol Total Quantity";
            seriesPetrolQuantity.ChartType = SeriesChartType.Column;
            seriesPetrolQuantity.Color = Color.Blue;

            Series seriesDieselQuantity = new Series();
            seriesDieselQuantity.Name = "Diesel Total Quantity";
            seriesDieselQuantity.ChartType = SeriesChartType.Column;
            seriesDieselQuantity.Color = Color.Red;

            foreach (DataRow row in dt.Rows)
            {
                string monthName = row["MonthName"].ToString();
                double petrolTotal = Convert.ToDouble(row["PetrolTotalQuantity"]);
                double dieselTotal = Convert.ToDouble(row["DieselTotalQuantity"]);

                seriesPetrolQuantity.Points.AddXY(monthName, petrolTotal);
                seriesPetrolQuantity.Points.Last().Label = petrolTotal.ToString();
                seriesPetrolQuantity.LabelForeColor = Color.White;

                seriesDieselQuantity.Points.AddXY(monthName, dieselTotal);
                seriesDieselQuantity.Points.Last().Label = dieselTotal.ToString();
                seriesDieselQuantity.LabelForeColor = Color.White;
            }

            chart3.Series.Add(seriesPetrolQuantity);
            chart3.Series.Add(seriesDieselQuantity);

            chart3.Legends.Add(new Legend("Legend"));
            chart3.Series["Petrol Total Quantity"].Legend = "Legend";
            chart3.Series["Diesel Total Quantity"].Legend = "Legend";
            chart3.Legends["Legend"].Docking = Docking.Bottom;
            chart3.Legends["Legend"].ForeColor = Color.White;
            chart3.Legends["Legend"].BackColor = Color.FromArgb(32, 36, 61);

            // Grid lines ko safed (white) karne ke liye
            chart3.ChartAreas[0].AxisX.LineColor = Color.White;
            chart3.ChartAreas[0].AxisY.LineColor = Color.White;
            chart3.ChartAreas[0].AxisX.MajorGrid.LineColor = Color.White;
            chart3.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.White;

        }
    }
}
