using System;

using System.Collections;

using System.Data;

using AlMadinaShahzad;



namespace AlMadinaShahzad.Services

{

    public sealed class ProfitLossResult

    {

        public decimal CustomerSales { get; set; }

        public decimal CustomerLiters { get; set; }

        public decimal PurchaseValue { get; set; }

        public decimal PurchaseLiters { get; set; }

        public decimal CustomerAvgRate { get; set; }

        public decimal DealerAvgRate { get; set; }

        public decimal MarginPerLiter { get; set; }

        public decimal GrossProfit { get; set; }

        public decimal TotalExpense { get; set; }

        public decimal NetProfit { get; set; }

        public string ProfitMethod { get; set; } = "";

    }



    public sealed class BalanceSheetResult

    {

        public decimal CustomerReceivable { get; set; }

        public decimal DealerPayable { get; set; }

        public decimal BankBalance { get; set; }

        public decimal StockValue { get; set; }

        public decimal TotalAssets { get; set; }

        public decimal TotalLiabilities { get; set; }

        public decimal RetainedEarnings { get; set; }

        public decimal TotalEquity { get; set; }

    }



    public static class FinancialReportService

    {

        // Customer line amount: liter*rate, warna direct Amount, warna Balance

        private const string CustomerLineAmountSql = @"

            CASE

                WHEN IFNULL(Litter,0) > 0 AND IFNULL(Rate,0) > 0 THEN (Litter * Rate)

                WHEN IFNULL(Amount,0) > 0 THEN Amount

                ELSE IFNULL(Balance,0)

            END";



        // Dealer/Stock line amount: liter*rate, warna sirf amount fields

        private const string DealerLineAmountSql = @"

            CASE

                WHEN IFNULL(AddDisel,0) > 0 AND IFNULL(Rate,0) > 0 THEN (AddDisel * Rate)

                WHEN IFNULL(Rate,0) > 0 AND IFNULL(AddDisel,0) = 0 THEN Rate

                WHEN IFNULL(AddDisel,0) > 0 AND IFNULL(Rate,0) = 0 THEN AddDisel

                ELSE 0

            END";



        public static ProfitLossResult GetProfitLoss(DateTime startDate, DateTime endDate)

        {

            var result = new ProfitLossResult();

            var ht = new Hashtable

            {

                { "@StartDate", startDate.ToString("yyyy-MM-dd") },

                { "@EndDate", endDate.ToString("yyyy-MM-dd") }

            };



            string qSale = $@"

                SELECT

                    IFNULL(SUM({CustomerLineAmountSql}),0) AS TotalCustomerAmount,

                    IFNULL(SUM(CASE WHEN IFNULL(Litter,0) > 0 THEN Litter ELSE 0 END),0) AS TotalCustomerLitter

                FROM PetrolAdd

                WHERE date(Date) >= date(@StartDate) AND date(Date) <= date(@EndDate)

                  AND IFNULL(IsInitialEntry,0)=1;";



            DataTable dtSale = MainClass.ExecuteSelectQuery(qSale, ht);

            if (dtSale != null && dtSale.Rows.Count > 0)

            {

                result.CustomerSales = ToDec(dtSale.Rows[0]["TotalCustomerAmount"]);

                result.CustomerLiters = ToDec(dtSale.Rows[0]["TotalCustomerLitter"]);

            }



            string qPurchase = $@"

                SELECT

                    IFNULL(SUM(CASE WHEN IFNULL(AddDisel,0) > 0 THEN AddDisel ELSE 0 END),0) AS TotalLitterPurchase,

                    IFNULL(SUM({DealerLineAmountSql}),0) AS TotalValuePurchase

                FROM AddStock

                WHERE date(Date) >= date(@StartDate) AND date(Date) <= date(@EndDate);";



            DataTable dtPurchase = MainClass.ExecuteSelectQuery(qPurchase, ht);

            if (dtPurchase != null && dtPurchase.Rows.Count > 0)

            {

                result.PurchaseLiters = ToDec(dtPurchase.Rows[0]["TotalLitterPurchase"]);

                result.PurchaseValue = ToDec(dtPurchase.Rows[0]["TotalValuePurchase"]);

            }



            result.CustomerAvgRate = result.CustomerLiters > 0 ? result.CustomerSales / result.CustomerLiters : 0m;

            result.DealerAvgRate = result.PurchaseLiters > 0 ? result.PurchaseValue / result.PurchaseLiters : 0m;

            result.MarginPerLiter = result.CustomerAvgRate - result.DealerAvgRate;



            // Customer liters par profit (Closing2 jaisa). Agar liter nahi, direct amount difference.

            if (result.CustomerLiters > 0)

            {

                result.GrossProfit = result.MarginPerLiter * result.CustomerLiters;

                result.ProfitMethod = "Margin x Customer Liters";

            }

            else

            {

                result.GrossProfit = result.CustomerSales - result.PurchaseValue;

                result.ProfitMethod = "Direct Sales - Purchase (amount only entries)";

            }



            string qExpense = @"

                SELECT IFNULL(SUM(Amount),0) AS TotalExpense

                FROM Expensetable

                WHERE date(EDate) >= date(@StartDate) AND date(EDate) <= date(@EndDate);";



            DataTable dtExpense = MainClass.ExecuteSelectQuery(qExpense, ht);

            if (dtExpense != null && dtExpense.Rows.Count > 0)

                result.TotalExpense = ToDec(dtExpense.Rows[0]["TotalExpense"]);



            result.NetProfit = result.GrossProfit - result.TotalExpense;

            return result;

        }



        public static BalanceSheetResult GetBalanceSheet(DateTime startDate, DateTime endDate)

        {

            var result = new BalanceSheetResult();

            var ht = new Hashtable

            {

                { "@StartDate", startDate.ToString("yyyy-MM-dd") },

                { "@EndDate", endDate.ToString("yyyy-MM-dd") }

            };



            string qRecv = @"

                SELECT IFNULL(SUM(

                    IFNULL(p.Amount,0)+IFNULL(p.Advance,0)-IFNULL(p.Credit,0)

                ),0) AS Total

                FROM PetrolAdd p

                WHERE date(p.Date) >= date(@StartDate) AND date(p.Date) <= date(@EndDate);";

            DataTable dtRecv = MainClass.ExecuteSelectQuery(qRecv, ht);

            if (dtRecv != null && dtRecv.Rows.Count > 0)

                result.CustomerReceivable = Math.Max(0m, ToDec(dtRecv.Rows[0]["Total"]));



            string qDealer = @"

                SELECT IFNULL(SUM(IFNULL(DDAmount,0)-IFNULL(DAmount,0)),0) AS Total

                FROM AddDealer

                WHERE date(Date) >= date(@StartDate) AND date(Date) <= date(@EndDate);";

            DataTable dtDealer = MainClass.ExecuteSelectQuery(qDealer, ht);

            if (dtDealer != null && dtDealer.Rows.Count > 0)

                result.DealerPayable = Math.Max(0m, ToDec(dtDealer.Rows[0]["Total"]));



            string qBank = @"

                SELECT IFNULL(SUM(

                    CASE WHEN IFNULL(TransactionType,'') LIKE '%Credit%' THEN IFNULL(Amount,0)

                         WHEN IFNULL(TransactionType,'') LIKE '%Debit%' THEN -IFNULL(Amount,0)

                         ELSE IFNULL(Amount,0) END

                ),0) AS Total

                FROM BankTransactions

                WHERE date(TransactionDate) >= date(@StartDate) AND date(TransactionDate) <= date(@EndDate);";

            DataTable dtBank = MainClass.ExecuteSelectQuery(qBank, ht);

            if (dtBank != null && dtBank.Rows.Count > 0)

                result.BankBalance = ToDec(dtBank.Rows[0]["Total"]);



            string qStock = $@"

                SELECT IFNULL(SUM({DealerLineAmountSql}),0) AS Total

                FROM AddStock

                WHERE date(Date) >= date(@StartDate) AND date(Date) <= date(@EndDate);";

            DataTable dtStock = MainClass.ExecuteSelectQuery(qStock, ht);

            if (dtStock != null && dtStock.Rows.Count > 0)

                result.StockValue = ToDec(dtStock.Rows[0]["Total"]);



            result.TotalAssets = result.CustomerReceivable + result.BankBalance + result.StockValue;

            result.TotalLiabilities = result.DealerPayable;



            var pl = GetProfitLoss(startDate, endDate);

            result.RetainedEarnings = pl.NetProfit;

            result.TotalEquity = result.RetainedEarnings;

            return result;

        }



        public static DataTable BuildProfitLossTable(ProfitLossResult r)

        {

            var dt = new DataTable();

            dt.Columns.Add("Section", typeof(string));

            dt.Columns.Add("LineItem", typeof(string));

            dt.Columns.Add("Amount", typeof(decimal));



            AddLine(dt, "Income", "Customer Diesel Sales", r.CustomerSales);

            AddLine(dt, "Cost", "Dealer Stock Purchase", r.PurchaseValue);

            AddLine(dt, "Info", "Customer Liters (jahan litter diya)", r.CustomerLiters);

            AddLine(dt, "Info", "Dealer Purchase Liters", r.PurchaseLiters);

            AddLine(dt, "Info", "Customer Avg Rate / Liter", r.CustomerAvgRate);

            AddLine(dt, "Info", "Dealer Avg Rate / Liter", r.DealerAvgRate);

            AddLine(dt, "Info", "Margin Per Liter", r.MarginPerLiter);

            AddLine(dt, "Profit", "Gross Profit", r.GrossProfit);

            AddLine(dt, "Info", "Formula Used", 0m, r.ProfitMethod);

            AddLine(dt, "Expense", "Operating Expenses", r.TotalExpense);

            AddLine(dt, "Result", "NET PROFIT / LOSS", r.NetProfit);

            return dt;

        }



        public static DataTable BuildBalanceSheetTable(BalanceSheetResult r)

        {

            var dt = new DataTable();

            dt.Columns.Add("Section", typeof(string));

            dt.Columns.Add("LineItem", typeof(string));

            dt.Columns.Add("Amount", typeof(decimal));



            AddLine(dt, "Assets", "Customer Receivable (date range)", r.CustomerReceivable);

            AddLine(dt, "Assets", "Cash & Bank Balance", r.BankBalance);

            AddLine(dt, "Assets", "Stock / Inventory Value", r.StockValue);

            AddLine(dt, "Assets", "TOTAL ASSETS", r.TotalAssets);

            AddLine(dt, "Liabilities", "Dealer Payable", r.DealerPayable);

            AddLine(dt, "Liabilities", "TOTAL LIABILITIES", r.TotalLiabilities);

            AddLine(dt, "Equity", "Retained Earnings (Net Profit in range)", r.RetainedEarnings);

            AddLine(dt, "Equity", "TOTAL EQUITY", r.TotalEquity);

            AddLine(dt, "Check", "Assets - (Liabilities + Equity)", r.TotalAssets - (r.TotalLiabilities + r.TotalEquity));

            return dt;

        }



        private static void AddLine(DataTable dt, string section, string line, decimal amount, string note = null)

        {

            if (!string.IsNullOrEmpty(note))

            {

                var row = dt.NewRow();

                row["Section"] = section;

                row["LineItem"] = line + " : " + note;

                row["Amount"] = amount;

                dt.Rows.Add(row);

                return;

            }

            dt.Rows.Add(section, line, amount);

        }



        private static decimal ToDec(object value)

        {

            if (value == null || value == DBNull.Value) return 0m;

            decimal.TryParse(value.ToString(), out decimal d);

            return d;

        }

    }

}


