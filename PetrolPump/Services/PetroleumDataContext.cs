using System;
using System.Collections;
using System.Data;
using System.Globalization;
using System.Text;
using AlMadinaShahzad;

namespace AlMadinaShahzad.Services
{
    /// <summary>
    /// Fetches real database numbers based on form names / keywords in the user's question.
    /// </summary>
    public static class PetroleumDataContext
    {
        private static readonly string[] DieselSaleKeys =
        {
            "daily diesel", "diesel sales", "diesel sale", "disel", "frmdiselview",
            "petroladd", "sale avg", "sale average", "customer sale"
        };

        private static readonly string[] StockKeys =
        {
            "dealeramount", "dealer amount", "stock", "frmstockview", "addstock",
            "purchase", "dealer purchase", "stock view", "add disel", "dealer stock"
        };

        private static readonly string[] ExpenseKeys =
        {
            "expense", "frmexpense", "expensetable", "kharcha", "kharch"
        };

        private static readonly string[] ClosingKeys =
        {
            "closing", "frmclosing", "profit", "loss", "faida", "net profit", "gross", "margin"
        };

        private static readonly string[] CustomerKeys =
        {
            "customer", "receivable", "wasool", "addcustomer", "credit customer", "frmcustomer"
        };

        private static readonly string[] DealerKeys =
        {
            "dealer", "payable", "adddealer", "dealer payout", "frmdealer"
        };

        public static string BuildContext(string question)
        {
            string q = (question ?? "").ToLowerInvariant();
            var sb = new StringBuilder();
            DateTime today = DateTime.Today;
            string todayStr = today.ToString("yyyy-MM-dd");
            string monthStart = new DateTime(today.Year, today.Month, 1).ToString("yyyy-MM-dd");

            sb.AppendLine("=== Al Madina Shezad Chhutta Services — LIVE DATABASE DATA ===");
            sb.AppendLine($"Today: {today:dd-MMM-yyyy}");
            sb.AppendLine();

            sb.AppendLine("--- FORM MAP (user says form name, you use this data) ---");
            sb.AppendLine("Daily Diesel Sales (frmDiselView) → PetrolAdd table (sales to customers)");
            sb.AppendLine("DealerAmount/Stock (frmStockView) → AddStock table (diesel purchased from dealers)");
            sb.AppendLine("Stock Diesel (frmStockDieselView) → StockDiesel table");
            sb.AppendLine("Expense (frmExpense) → Expensetable");
            sb.AppendLine("Closing Form (frmClosingformEntry) / Closing 2 (frmClosing2) → profit = (sale avg - dealer avg) × liters - expense");
            sb.AppendLine("Add Customer (frmCustomerView) → AddCustomer + PetrolAdd balances");
            sb.AppendLine("Add Dealer (frmDealerNameView) → AddDealer balances");
            sb.AppendLine();

            bool wantAll = q.Contains("sab") || q.Contains("all") || q.Contains("summary") || q.Length < 15;

            if (wantAll || Matches(q, DieselSaleKeys))
                AppendDieselSales(sb, todayStr, monthStart);

            if (wantAll || Matches(q, StockKeys))
                AppendStockPurchase(sb, todayStr, monthStart);

            if (wantAll || Matches(q, ExpenseKeys))
                AppendExpense(sb, todayStr, monthStart);

            if (wantAll || Matches(q, CustomerKeys))
                AppendCustomerReceivable(sb);

            if (wantAll || Matches(q, DealerKeys))
                AppendDealerPayable(sb);

            if (wantAll || Matches(q, ClosingKeys))
                AppendClosingProfit(sb, monthStart, todayStr);

            if (!wantAll && !Matches(q, DieselSaleKeys) && !Matches(q, StockKeys) &&
                !Matches(q, ExpenseKeys) && !Matches(q, CustomerKeys) &&
                !Matches(q, DealerKeys) && !Matches(q, ClosingKeys))
            {
                sb.AppendLine("--- GENERAL SUMMARY (question not matched to specific form) ---");
                AppendDieselSales(sb, todayStr, monthStart);
                AppendStockPurchase(sb, todayStr, monthStart);
                AppendExpense(sb, todayStr, monthStart);
            }

            return sb.ToString();
        }

        private static bool Matches(string q, string[] keys)
        {
            foreach (string k in keys)
                if (q.Contains(k)) return true;
            return false;
        }

        private static void AppendDieselSales(StringBuilder sb, string today, string monthStart)
        {
            sb.AppendLine("--- DAILY DIESEL SALES (frmDiselView / PetrolAdd) ---");

            AppendSaleRange(sb, "Today", today, today);
            AppendSaleRange(sb, "This Month", monthStart, today);
            AppendSaleRange(sb, "All Time", null, null);

            string topCustomers = @"
                SELECT c.Name, COUNT(*) AS Entries,
                    IFNULL(SUM(IFNULL(p.Litter,0)),0) AS TotalLiters,
                    IFNULL(SUM(CASE WHEN IFNULL(p.Litter,0)=0 OR IFNULL(p.Rate,0)=0
                        THEN IFNULL(p.Balance,0) ELSE p.Litter*p.Rate END),0) AS TotalAmount
                FROM PetrolAdd p INNER JOIN AddCustomer c ON c.id=p.CustomerId
                WHERE IFNULL(p.IsInitialEntry,0)=1
                GROUP BY c.id ORDER BY TotalAmount DESC LIMIT 5;";
            AppendQueryResult(sb, "Top 5 Customers (All Time)", topCustomers, null);
        }

        private static void AppendSaleRange(StringBuilder sb, string label, string start, string end)
        {
            string q = @"
                SELECT
                    IFNULL(SUM(CASE WHEN IFNULL(Litter,0)=0 OR IFNULL(Rate,0)=0
                        THEN IFNULL(Balance,0) ELSE Litter*Rate END),0) AS TotalAmount,
                    IFNULL(SUM(IFNULL(Litter,0)),0) AS TotalLiters,
                    COUNT(*) AS TotalEntries
                FROM PetrolAdd
                WHERE IFNULL(IsInitialEntry,0)=1";
            Hashtable ht = null;
            if (start != null)
            {
                q += " AND Date >= @Start AND Date <= @End";
                ht = new Hashtable { { "@Start", start }, { "@End", end } };
            }

            DataTable dt = ht == null ? MainClass.GetData(q) : MainClass.ExecuteSelectQuery(q, ht);
            if (dt == null || dt.Rows.Count == 0) return;

            decimal amt = ToDec(dt.Rows[0]["TotalAmount"]);
            decimal lit = ToDec(dt.Rows[0]["TotalLiters"]);
            int entries = Convert.ToInt32(dt.Rows[0]["TotalEntries"]);
            decimal avg = lit > 0 ? amt / lit : 0m;

            sb.AppendLine($"{label}: Entries={entries}, Total Liters={lit:N2}, Total Amount={amt:N2} Rs, Avg Rate={avg:N3} Rs/L");
        }

        private static void AppendStockPurchase(StringBuilder sb, string today, string monthStart)
        {
            sb.AppendLine("--- DEALER STOCK / PURCHASE (frmStockView / AddStock) ---");

            AppendPurchaseRange(sb, "Today", today, today);
            AppendPurchaseRange(sb, "This Month", monthStart, today);
            AppendPurchaseRange(sb, "All Time", null, null);
        }

        private static void AppendPurchaseRange(StringBuilder sb, string label, string start, string end)
        {
            string q = @"
                SELECT IFNULL(SUM(AddDisel),0) AS TotalLiters,
                    IFNULL(SUM(AddDisel*Rate),0) AS TotalAmount,
                    COUNT(*) AS TotalEntries
                FROM AddStock WHERE 1=1";
            Hashtable ht = null;
            if (start != null)
            {
                q += " AND Date >= @Start AND Date <= @End";
                ht = new Hashtable { { "@Start", start }, { "@End", end } };
            }

            DataTable dt = ht == null ? MainClass.GetData(q) : MainClass.ExecuteSelectQuery(q, ht);
            if (dt == null || dt.Rows.Count == 0) return;

            decimal amt = ToDec(dt.Rows[0]["TotalAmount"]);
            decimal lit = ToDec(dt.Rows[0]["TotalLiters"]);
            decimal avg = lit > 0 ? amt / lit : 0m;
            sb.AppendLine($"{label}: Liters={lit:N2}, Amount={amt:N2} Rs, Avg Purchase Rate={avg:N3} Rs/L");
        }

        private static void AppendExpense(StringBuilder sb, string today, string monthStart)
        {
            sb.AppendLine("--- EXPENSE (frmExpense / Expensetable) ---");

            AppendExpenseRange(sb, "Today", today, today);
            AppendExpenseRange(sb, "This Month", monthStart, today);
            AppendExpenseRange(sb, "All Time", null, null);
        }

        private static void AppendExpenseRange(StringBuilder sb, string label, string start, string end)
        {
            string q = @"SELECT IFNULL(SUM(Amount),0) AS TotalExpense, COUNT(*) AS Entries FROM Expensetable WHERE 1=1";
            Hashtable ht = null;
            if (start != null)
            {
                q += " AND EDate >= @Start AND EDate <= @End";
                ht = new Hashtable { { "@Start", start }, { "@End", end } };
            }

            DataTable dt = ht == null ? MainClass.GetData(q) : MainClass.ExecuteSelectQuery(q, ht);
            if (dt == null || dt.Rows.Count == 0) return;

            sb.AppendLine($"{label}: Total Expense={ToDec(dt.Rows[0]["TotalExpense"]):N2} Rs, Entries={dt.Rows[0]["Entries"]}");
        }

        private static void AppendCustomerReceivable(StringBuilder sb)
        {
            sb.AppendLine("--- CUSTOMER RECEIVABLE (positive balances) ---");
            string q = @"
                SELECT c.Name,
                    IFNULL(SUM(IFNULL(p.Amount,0)+IFNULL(p.Advance,0)-IFNULL(p.Credit,0)),0) AS Receivable
                FROM PetrolAdd p INNER JOIN AddCustomer c ON c.id=p.CustomerId
                GROUP BY c.id HAVING Receivable > 0 ORDER BY Receivable DESC LIMIT 10;";
            AppendQueryResult(sb, "Top 10 Customers Owed", q, null);

            string totalQ = @"
                SELECT IFNULL(SUM(sub.Receivable),0) AS GrandTotal FROM (
                    SELECT IFNULL(SUM(IFNULL(p.Amount,0)+IFNULL(p.Advance,0)-IFNULL(p.Credit,0)),0) AS Receivable
                    FROM PetrolAdd p GROUP BY p.CustomerId HAVING Receivable > 0
                ) sub;";
            DataTable dt = MainClass.GetData(totalQ);
            if (dt != null && dt.Rows.Count > 0)
                sb.AppendLine($"Total Customer Receivable: {ToDec(dt.Rows[0]["GrandTotal"]):N2} Rs");
        }

        private static void AppendDealerPayable(StringBuilder sb)
        {
            sb.AppendLine("--- DEALER PAYABLE (positive balances) ---");
            string q = @"
                SELECT DealerName, (IFNULL(DDAmount,0)-IFNULL(DAmount,0)) AS Payable
                FROM AddDealer WHERE (IFNULL(DDAmount,0)-IFNULL(DAmount,0)) > 0
                ORDER BY Payable DESC LIMIT 10;";
            AppendQueryResult(sb, "Top 10 Dealers Owed", q, null);

            string totalQ = @"SELECT IFNULL(SUM(IFNULL(DDAmount,0)-IFNULL(DAmount,0)),0) AS GrandTotal
                FROM AddDealer WHERE (IFNULL(DDAmount,0)-IFNULL(DAmount,0)) > 0;";
            DataTable dt = MainClass.GetData(totalQ);
            if (dt != null && dt.Rows.Count > 0)
                sb.AppendLine($"Total Dealer Payable: {ToDec(dt.Rows[0]["GrandTotal"]):N2} Rs");
        }

        private static void AppendClosingProfit(StringBuilder sb, string monthStart, string today)
        {
            sb.AppendLine("--- CLOSING / PROFIT CALCULATION (frmClosing2 formula) ---");
            Hashtable ht = new Hashtable { { "@Start", monthStart }, { "@End", today } };

            string qSale = @"
                SELECT IFNULL(SUM(CASE WHEN IFNULL(Litter,0)=0 OR IFNULL(Rate,0)=0
                    THEN IFNULL(Balance,0) ELSE Litter*Rate END),0) AS Amt,
                    IFNULL(SUM(IFNULL(Litter,0)),0) AS Lit
                FROM PetrolAdd WHERE Date>=@Start AND Date<=@End AND IFNULL(IsInitialEntry,0)=1;";

            string qPurchase = @"
                SELECT IFNULL(SUM(AddDisel*Rate),0) AS Amt, IFNULL(SUM(AddDisel),0) AS Lit
                FROM AddStock WHERE Date>=@Start AND Date<=@End;";

            string qExpense = @"
                SELECT IFNULL(SUM(Amount),0) AS Exp FROM Expensetable
                WHERE EDate>=@Start AND EDate<=@End;";

            DataTable dtSale = MainClass.ExecuteSelectQuery(qSale, ht);
            DataTable dtPur = MainClass.ExecuteSelectQuery(qPurchase, ht);
            DataTable dtExp = MainClass.ExecuteSelectQuery(qExpense, ht);

            decimal saleAmt = dtSale?.Rows.Count > 0 ? ToDec(dtSale.Rows[0]["Amt"]) : 0;
            decimal saleLit = dtSale?.Rows.Count > 0 ? ToDec(dtSale.Rows[0]["Lit"]) : 0;
            decimal purAmt = dtPur?.Rows.Count > 0 ? ToDec(dtPur.Rows[0]["Amt"]) : 0;
            decimal purLit = dtPur?.Rows.Count > 0 ? ToDec(dtPur.Rows[0]["Lit"]) : 0;
            decimal expense = dtExp?.Rows.Count > 0 ? ToDec(dtExp.Rows[0]["Exp"]) : 0;

            decimal custAvg = saleLit > 0 ? saleAmt / saleLit : 0;
            decimal dealAvg = purLit > 0 ? purAmt / purLit : 0;
            decimal margin = custAvg - dealAvg;
            decimal gross = margin * saleLit;
            decimal net = gross - expense;

            sb.AppendLine($"Period: {monthStart} to {today}");
            sb.AppendLine($"Customer Sale Avg: {custAvg:N3} Rs/L (Amount={saleAmt:N2}, Liters={saleLit:N2})");
            sb.AppendLine($"Dealer Purchase Avg: {dealAvg:N3} Rs/L (Amount={purAmt:N2}, Liters={purLit:N2})");
            sb.AppendLine($"Margin per Liter: {margin:N3} Rs/L");
            sb.AppendLine($"Gross Profit (Margin × Liters): {gross:N2} Rs");
            sb.AppendLine($"Total Expense: {expense:N2} Rs");
            sb.AppendLine($"NET PROFIT: {net:N2} Rs");
        }

        private static void AppendQueryResult(StringBuilder sb, string title, string query, Hashtable ht)
        {
            sb.AppendLine(title + ":");
            try
            {
                DataTable dt = ht == null ? MainClass.GetData(query) : MainClass.ExecuteSelectQuery(query, ht);
                if (dt == null || dt.Rows.Count == 0)
                {
                    sb.AppendLine("  (no data)");
                    return;
                }
                foreach (DataRow row in dt.Rows)
                {
                    var parts = new StringBuilder("  ");
                    foreach (DataColumn col in dt.Columns)
                        parts.Append($"{col.ColumnName}={row[col]}  ");
                    sb.AppendLine(parts.ToString());
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"  (query error: {ex.Message})");
            }
            sb.AppendLine();
        }

        private static decimal ToDec(object val)
        {
            if (val == null || val == DBNull.Value) return 0m;
            if (decimal.TryParse(Convert.ToString(val), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal r))
                return r;
            decimal.TryParse(Convert.ToString(val), NumberStyles.Any, CultureInfo.CurrentCulture, out r);
            return r;
        }
    }
}
