using System;
using System.Collections;
using System.Data;
using System.Globalization;
using System.Text;

namespace AlMadinaShahzad.Services
{
    public static class LocalCalculationAssistant
    {
        private enum QueryIntent
        {
            DieselSales,
            DealerStock,
            StockDiesel,
            Expense,
            Closing,
            CustomerReceivable,
            DealerPayable,
            Summary
        }

        public static string Answer(string question)
        {
            string q = Normalize(question);
            var sb = new StringBuilder();
            sb.AppendLine("📊 Database se direct calculation:");
            sb.AppendLine();

            QueryIntent intent = DetectIntent(q);

            switch (intent)
            {
                case QueryIntent.DealerStock:
                    sb.Append(FormatStockPurchase());
                    break;
                case QueryIntent.StockDiesel:
                    sb.Append(FormatStockDiesel());
                    break;
                case QueryIntent.DieselSales:
                    sb.Append(FormatDieselSales());
                    break;
                case QueryIntent.Expense:
                    sb.Append(FormatExpense());
                    break;
                case QueryIntent.Closing:
                    sb.Append(FormatClosingProfit());
                    break;
                case QueryIntent.CustomerReceivable:
                    sb.Append(FormatCustomerReceivable());
                    break;
                case QueryIntent.DealerPayable:
                    sb.Append(FormatDealerPayable());
                    break;
                default:
                    sb.Append(FormatDieselSales());
                    sb.AppendLine();
                    sb.Append(FormatStockPurchase());
                    sb.AppendLine();
                    sb.Append(FormatExpense());
                    break;
            }

            return sb.ToString().TrimEnd();
        }

        private static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.ToLowerInvariant()
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("-", "");
        }

        private static QueryIntent DetectIntent(string q)
        {
            // ── Priority 1: DealerAmount button = frmStockView / AddStock ──
            if (ContainsAny(q, "dealeramount", "frmstockview", "addstock",
                "dealerstock", "dealerpurchase", "dealersidepurchase"))
                return QueryIntent.DealerStock;

            // dealer + liters = purchase side (not sales, not payable balance)
            if (ContainsAny(q, "dealer") && ContainsAny(q, "litter", "liter", "litre", "adddisel"))
                return QueryIntent.DealerStock;

            // ── Stock Diesel form ──
            if (ContainsAny(q, "stockdiesel", "frmstockdieselview", "btnstock"))
                return QueryIntent.StockDiesel;

            // ── Daily Diesel Sales ──
            if (ContainsAny(q, "dailydiesel", "dieselsale", "dieselsales", "frmdiselview",
                "petroladd", "customersale", "saleavg", "salesaverage"))
                return QueryIntent.DieselSales;

            if (ContainsAny(q, "sale", "diesel", "disel") &&
                !ContainsAny(q, "dealer", "dealeramount", "purchase", "stock"))
                return QueryIntent.DieselSales;

            // ── Expense ──
            if (ContainsAny(q, "expense", "kharcha", "kharch", "frmexpense", "expensetable"))
                return QueryIntent.Expense;

            // ── Closing / Profit ──
            if (ContainsAny(q, "closing", "frmclosing", "profit", "loss", "faida",
                "netprofit", "grossprofit", "margin"))
                return QueryIntent.Closing;

            // ── Customer Receivable ──
            if (ContainsAny(q, "customer", "receivable", "wasool", "frmcustomer",
                "addcustomer", "creditcustomer"))
                return QueryIntent.CustomerReceivable;

            // ── Dealer Payable (balance owed to dealers) ──
            if (ContainsAny(q, "payable", "payout", "dealerpayable", "dealerpayout",
                "ddamount", "denahai", "denahe"))
                return QueryIntent.DealerPayable;

            if (ContainsAny(q, "dealer", "frmdealer", "adddealer", "dealername"))
                return QueryIntent.DealerPayable;

            // liters/average alone → show summary of main forms
            if (ContainsAny(q, "litter", "liter", "litre", "average", "avg", "kitne"))
                return QueryIntent.Summary;

            return QueryIntent.Summary;
        }

        private static string FormatDieselSales()
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══ DAILY DIESEL SALES (frmDiselView) ═══");
            AppendPeriod(sb, "Aaj (Today)", GetToday(), GetToday(), true);
            AppendPeriod(sb, "Is Mahine", GetMonthStart(), GetToday(), true);
            AppendPeriod(sb, "Total (All Time)", null, null, true);
            return sb.ToString();
        }

        private static string FormatStockPurchase()
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══ DEALER AMOUNT / STOCK (frmStockView → AddStock) ═══");
            sb.AppendLine("  (Yahan dealer se kitna diesel purchase hua — liters + amount)");
            AppendPeriod(sb, "Aaj (Today)", GetToday(), GetToday(), false);
            AppendPeriod(sb, "Is Mahine", GetMonthStart(), GetToday(), false);
            AppendPeriod(sb, "Total (All Time)", null, null, false);
            return sb.ToString();
        }

        private static string FormatStockDiesel()
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══ STOCK DIESEL (frmStockDieselView → StockDiesel) ═══");

            foreach (var p in new[] { ("Aaj", GetToday(), GetToday()), ("Is Mahine", GetMonthStart(), GetToday()), ("Total", null, null) })
            {
                string q = @"SELECT IFNULL(SUM(Litter),0) AS Lit, IFNULL(SUM(Litter*Rate),0) AS Amt,
                    COUNT(*) AS Ent FROM StockDiesel WHERE 1=1";
                Hashtable ht = null;
                if (p.Item2 != null) { q += " AND Date>=@S AND Date<=@E"; ht = new Hashtable { { "@S", p.Item2 }, { "@E", p.Item3 } }; }
                DataTable dt = ht == null ? MainClass.GetData(q) : MainClass.ExecuteSelectQuery(q, ht);
                decimal lit = dt?.Rows.Count > 0 ? ToDec(dt.Rows[0]["Lit"]) : 0;
                decimal amt = dt?.Rows.Count > 0 ? ToDec(dt.Rows[0]["Amt"]) : 0;
                decimal avg = lit > 0 ? amt / lit : 0;
                sb.AppendLine($"  {p.Item1}: Liters={lit:N2} L, Amount={amt:N2} Rs, Avg={avg:N3} Rs/L");
            }
            sb.AppendLine();
            return sb.ToString();
        }

        private static string FormatExpense()
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══ EXPENSE (frmExpense) ═══");
            foreach (var p in new[] { ("Aaj", GetToday(), GetToday()), ("Is Mahine", GetMonthStart(), GetToday()), ("Total", null, null) })
            {
                decimal exp = QueryExpense(p.Item2, p.Item3);
                sb.AppendLine($"  {p.Item1}: {exp:N2} Rs");
            }
            sb.AppendLine();
            return sb.ToString();
        }

        private static string FormatClosingProfit()
        {
            string start = GetMonthStart();
            string end = GetToday();
            decimal saleAmt = 0, saleLit = 0, purAmt = 0, purLit = 0, expense = 0;
            var ht = new Hashtable { { "@Start", start }, { "@End", end } };

            DataTable dtS = MainClass.ExecuteSelectQuery(@"
                SELECT IFNULL(SUM(CASE WHEN IFNULL(Litter,0)=0 OR IFNULL(Rate,0)=0
                    THEN IFNULL(Balance,0) ELSE Litter*Rate END),0) AS Amt,
                    IFNULL(SUM(IFNULL(Litter,0)),0) AS Lit
                FROM PetrolAdd WHERE Date>=@Start AND Date<=@End AND IFNULL(IsInitialEntry,0)=1", ht);

            DataTable dtP = MainClass.ExecuteSelectQuery(@"
                SELECT IFNULL(SUM(AddDisel*Rate),0) AS Amt, IFNULL(SUM(AddDisel),0) AS Lit
                FROM AddStock WHERE Date>=@Start AND Date<=@End", ht);

            DataTable dtE = MainClass.ExecuteSelectQuery(@"
                SELECT IFNULL(SUM(Amount),0) AS Exp FROM Expensetable
                WHERE EDate>=@Start AND EDate<=@End", ht);

            if (dtS?.Rows.Count > 0) { saleAmt = ToDec(dtS.Rows[0]["Amt"]); saleLit = ToDec(dtS.Rows[0]["Lit"]); }
            if (dtP?.Rows.Count > 0) { purAmt = ToDec(dtP.Rows[0]["Amt"]); purLit = ToDec(dtP.Rows[0]["Lit"]); }
            if (dtE?.Rows.Count > 0) expense = ToDec(dtE.Rows[0]["Exp"]);

            decimal custAvg = saleLit > 0 ? saleAmt / saleLit : 0;
            decimal dealAvg = purLit > 0 ? purAmt / purLit : 0;
            decimal margin = custAvg - dealAvg;
            decimal gross = margin * saleLit;
            decimal net = gross - expense;

            var sb = new StringBuilder();
            sb.AppendLine($"═══ CLOSING / PROFIT (Is Mahine: {start} se {end}) ═══");
            sb.AppendLine($"  Customer Sale Avg:   {custAvg:N3} Rs/L  (Amt: {saleAmt:N2}, Lit: {saleLit:N2})");
            sb.AppendLine($"  Dealer Purchase Avg: {dealAvg:N3} Rs/L  (Amt: {purAmt:N2}, Lit: {purLit:N2})");
            sb.AppendLine($"  Margin per Liter:    {margin:N3} Rs/L");
            sb.AppendLine($"  Gross Profit:        {gross:N2} Rs");
            sb.AppendLine($"  Total Expense:       {expense:N2} Rs");
            sb.AppendLine($"  ★ NET PROFIT:        {net:N2} Rs");
            sb.AppendLine();
            return sb.ToString();
        }

        private static string FormatCustomerReceivable()
        {
            DataTable dt = MainClass.GetData(@"
                SELECT IFNULL(SUM(sub.R),0) AS Total FROM (
                    SELECT IFNULL(SUM(IFNULL(p.Amount,0)+IFNULL(p.Advance,0)-IFNULL(p.Credit,0)),0) AS R
                    FROM PetrolAdd p GROUP BY p.CustomerId HAVING R > 0
                ) sub;");
            decimal total = dt?.Rows.Count > 0 ? ToDec(dt.Rows[0]["Total"]) : 0;

            var sb = new StringBuilder();
            sb.AppendLine("═══ CUSTOMER RECEIVABLE (frmCustomerView) ═══");
            sb.AppendLine($"  Total Customer Receivable: {total:N2} Rs");
            sb.AppendLine();
            return sb.ToString();
        }

        private static string FormatDealerPayable()
        {
            DataTable dt = MainClass.GetData(@"
                SELECT IFNULL(SUM(IFNULL(DDAmount,0)-IFNULL(DAmount,0)),0) AS Total
                FROM AddDealer WHERE (IFNULL(DDAmount,0)-IFNULL(DAmount,0)) > 0;");
            decimal total = dt?.Rows.Count > 0 ? ToDec(dt.Rows[0]["Total"]) : 0;

            var sb = new StringBuilder();
            sb.AppendLine("═══ DEALER PAYABLE (AddDealer balance — kitna dena hai) ═══");
            sb.AppendLine($"  Total Dealer Payable: {total:N2} Rs");
            sb.AppendLine("  (Note: Ye liters nahi — ye dealer ko dena wala balance hai)");
            sb.AppendLine();
            return sb.ToString();
        }

        private static void AppendPeriod(StringBuilder sb, string label, string start, string end, bool isSale)
        {
            decimal amt, lit;
            if (isSale) QuerySale(start, end, out amt, out lit);
            else QueryPurchase(start, end, out amt, out lit);

            decimal avg = lit > 0 ? amt / lit : 0;
            sb.AppendLine($"  {label}:");
            sb.AppendLine($"    Liters  = {lit:N2} L");
            sb.AppendLine($"    Amount  = {amt:N2} Rs");
            sb.AppendLine($"    ★ Average = {avg:N3} Rs/L");
            sb.AppendLine();
        }

        private static void QuerySale(string start, string end, out decimal amt, out decimal lit)
        {
            string q = @"SELECT IFNULL(SUM(CASE WHEN IFNULL(Litter,0)=0 OR IFNULL(Rate,0)=0
                THEN IFNULL(Balance,0) ELSE Litter*Rate END),0) AS Amt,
                IFNULL(SUM(IFNULL(Litter,0)),0) AS Lit FROM PetrolAdd
                WHERE IFNULL(IsInitialEntry,0)=1";
            Hashtable ht = null;
            if (start != null) { q += " AND Date>=@Start AND Date<=@End"; ht = new Hashtable { { "@Start", start }, { "@End", end } }; }
            DataTable dt = ht == null ? MainClass.GetData(q) : MainClass.ExecuteSelectQuery(q, ht);
            amt = dt?.Rows.Count > 0 ? ToDec(dt.Rows[0]["Amt"]) : 0;
            lit = dt?.Rows.Count > 0 ? ToDec(dt.Rows[0]["Lit"]) : 0;
        }

        private static void QueryPurchase(string start, string end, out decimal amt, out decimal lit)
        {
            string q = @"SELECT IFNULL(SUM(AddDisel*Rate),0) AS Amt, IFNULL(SUM(AddDisel),0) AS Lit
                FROM AddStock WHERE 1=1";
            Hashtable ht = null;
            if (start != null) { q += " AND Date>=@Start AND Date<=@End"; ht = new Hashtable { { "@Start", start }, { "@End", end } }; }
            DataTable dt = ht == null ? MainClass.GetData(q) : MainClass.ExecuteSelectQuery(q, ht);
            amt = dt?.Rows.Count > 0 ? ToDec(dt.Rows[0]["Amt"]) : 0;
            lit = dt?.Rows.Count > 0 ? ToDec(dt.Rows[0]["Lit"]) : 0;
        }

        private static decimal QueryExpense(string start, string end)
        {
            string q = "SELECT IFNULL(SUM(Amount),0) AS E FROM Expensetable WHERE 1=1";
            Hashtable ht = null;
            if (start != null) { q += " AND EDate>=@Start AND EDate<=@End"; ht = new Hashtable { { "@Start", start }, { "@End", end } }; }
            DataTable dt = ht == null ? MainClass.GetData(q) : MainClass.ExecuteSelectQuery(q, ht);
            return dt?.Rows.Count > 0 ? ToDec(dt.Rows[0]["E"]) : 0;
        }

        private static string GetToday() => DateTime.Today.ToString("yyyy-MM-dd");
        private static string GetMonthStart() => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).ToString("yyyy-MM-dd");

        private static bool ContainsAny(string q, params string[] keys)
        {
            foreach (string k in keys)
                if (q.Contains(k)) return true;
            return false;
        }

        private static decimal ToDec(object val)
        {
            if (val == null || val == DBNull.Value) return 0m;
            if (decimal.TryParse(Convert.ToString(val), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal r)) return r;
            decimal.TryParse(Convert.ToString(val), NumberStyles.Any, CultureInfo.CurrentCulture, out r);
            return r;
        }
    }
}
