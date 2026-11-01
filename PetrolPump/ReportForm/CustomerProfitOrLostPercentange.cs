using Microsoft.Reporting.WinForms;
using AlMadinaShahzad.ProjectConnection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AlMadinaShahzad.ReportForm
{
    public partial class CustomerProfitOrLostPercentange: Sample
    {
        public CustomerProfitOrLostPercentange()
        {
            InitializeComponent();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string searchText = txtSearch.Text;

            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                con.Open();

                SQLiteCommand cmd = new SQLiteCommand(@"
            SELECT 
                p.CustomerId,
                c.Name AS CustomerName,

                -- 🔹 TotalBalance = total debit (amount + advance)
                SUM(
                    CASE 
                        WHEN IFNULL(p.Litter, 0) = 0 
                             AND IFNULL(p.Rate, 0) = 0
                            THEN IFNULL(p.Amount, 0)
                        ELSE (p.Litter * p.Rate + IFNULL(p.Advance, 0))
                    END
                ) AS TotalBalance,

                -- 🔹 TotalCredit
                SUM(IFNULL(p.Credit, 0)) AS TotalCredit,

                -- 🔹 Net profit / loss
                SUM(
                    CASE 
                        WHEN IFNULL(p.Litter, 0) = 0 
                             AND IFNULL(p.Rate, 0) = 0
                            THEN IFNULL(p.Amount, 0)
                        ELSE (p.Litter * p.Rate + IFNULL(p.Advance, 0))
                    END
                    - IFNULL(p.Credit, 0)
                ) AS CustomerProfitOrLoss,

                -- 🔹 Percentage
                CASE 
                    WHEN SUM(
                           CASE 
                               WHEN IFNULL(p.Litter, 0) = 0 
                                    AND IFNULL(p.Rate, 0) = 0
                                   THEN IFNULL(p.Amount, 0)
                               ELSE (p.Litter * p.Rate + IFNULL(p.Advance, 0))
                           END
                        ) = 0 
                    THEN 0
                    ELSE ROUND(
                          SUM(
                              CASE 
                                  WHEN IFNULL(p.Litter, 0) = 0 
                                       AND IFNULL(p.Rate, 0) = 0
                                      THEN IFNULL(p.Amount, 0)
                                  ELSE (p.Litter * p.Rate + IFNULL(p.Advance, 0))
                              END
                              - IFNULL(p.Credit, 0)
                          )
                          / 
                          SUM(
                              CASE 
                                  WHEN IFNULL(p.Litter, 0) = 0 
                                       AND IFNULL(p.Rate, 0) = 0
                                      THEN IFNULL(p.Amount, 0)
                                  ELSE (p.Litter * p.Rate + IFNULL(p.Advance, 0))
                              END
                          ) * 100
                        , 2)
                END AS ProfitOrLossPercentage

            FROM 
                PetrolAdd p
            LEFT JOIN 
                AddCustomer c ON p.CustomerId = c.id
            WHERE 
                (@SearchText = '' OR TRIM(c.Name) = @SearchText COLLATE NOCASE)
                AND date(p.Date) >= date(@FromDate) AND date(p.Date) <= date(@ToDate)
            " + ReportOfficeHelper.Sql(this, "p.OfficeName") + @"
            GROUP BY 
                p.CustomerId;
        ", con);

                ReportDateRangeHelper.AddToCommand(cmd, fromdate, todate);
                ReportSearchHelper.BindSearch(cmd, searchText);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("Koi data nahi mila is date range ya search ke liye.",
                                    "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "PercentageCustomerReport.rdlc");
                reportViewer1.LocalReport.ReportPath = reportPath;

                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(rds);
                reportViewer1.RefreshReport();
            }
        }
        private void CustomerProfitOrLostPercentange_Load(object sender, EventArgs e)
        {
            fromdate.Value = DateTime.Now;  // Default date set کریں
            todate.Value = DateTime.Now;
            this.reportViewer1.RefreshReport();
            LoadAllRecords();
        }
        private void LoadAllRecords()
        {
            string connectionString = projectconnection.conReturn();

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                SQLiteCommand cmd = new SQLiteCommand(@"
            SELECT 
                p.CustomerId,
                c.Name AS CustomerName,

                -- 🔹 TotalBalance = total debit (amount + advance)
                SUM(
                    CASE 
                        WHEN IFNULL(p.Litter, 0) = 0 
                             AND IFNULL(p.Rate, 0) = 0
                            THEN IFNULL(p.Amount, 0)              -- direct amount entry
                        ELSE (p.Litter * p.Rate + IFNULL(p.Advance, 0))
                    END
                ) AS TotalBalance,

                -- 🔹 TotalCredit
                SUM(IFNULL(p.Credit, 0)) AS TotalCredit,

                -- 🔹 Net profit / loss = debit - credit
                SUM(
                    CASE 
                        WHEN IFNULL(p.Litter, 0) = 0 
                             AND IFNULL(p.Rate, 0) = 0
                            THEN IFNULL(p.Amount, 0)
                        ELSE (p.Litter * p.Rate + IFNULL(p.Advance, 0))
                    END
                    - IFNULL(p.Credit, 0)
                ) AS CustomerProfitOrLoss,

                -- 🔹 Percentage = (net / debit) * 100
                CASE 
                    WHEN SUM(
                           CASE 
                               WHEN IFNULL(p.Litter, 0) = 0 
                                    AND IFNULL(p.Rate, 0) = 0
                                   THEN IFNULL(p.Amount, 0)
                               ELSE (p.Litter * p.Rate + IFNULL(p.Advance, 0))
                           END
                        ) = 0 
                    THEN 0
                    ELSE ROUND(
                          SUM(
                              CASE 
                                  WHEN IFNULL(p.Litter, 0) = 0 
                                       AND IFNULL(p.Rate, 0) = 0
                                      THEN IFNULL(p.Amount, 0)
                                  ELSE (p.Litter * p.Rate + IFNULL(p.Advance, 0))
                              END
                              - IFNULL(p.Credit, 0)
                          )
                          / 
                          SUM(
                              CASE 
                                  WHEN IFNULL(p.Litter, 0) = 0 
                                       AND IFNULL(p.Rate, 0) = 0
                                      THEN IFNULL(p.Amount, 0)
                                  ELSE (p.Litter * p.Rate + IFNULL(p.Advance, 0))
                              END
                          ) * 100
                        , 2)
                END AS ProfitOrLossPercentage

            FROM 
                PetrolAdd p
            LEFT JOIN 
                AddCustomer c ON p.CustomerId = c.id
            WHERE 1=1" + ReportOfficeHelper.Sql(this, "p.OfficeName") + @"
            GROUP BY 
                p.CustomerId;
        ", con);

                SQLiteDataAdapter sd = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                sd.Fill(dt);

                ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                string reportPath = Path.Combine(Application.StartupPath, "Reports", "PercentageCustomerReport.rdlc");
                reportViewer1.LocalReport.ReportPath = reportPath;

                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(rds);
                reportViewer1.RefreshReport();
            }
        }
    }
}
