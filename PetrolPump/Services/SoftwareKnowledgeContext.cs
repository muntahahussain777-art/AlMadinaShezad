using System.Text;

namespace AlMadinaShahzad.Services
{
    /// <summary>
    /// Complete software guide for Gemini — forms, menus, workflows, formulas.
    /// </summary>
    public static class SoftwareKnowledgeContext
    {
        public static string BuildContext()
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== Al Madina Shezad Chhutta Services — COMPLETE SOFTWARE GUIDE ===");
            sb.AppendLine("Developer: Irtaza Hussain");
            sb.AppendLine("Type: Windows Desktop Petrol Pump Management Software (.NET WinForms, SQLite database)");
            sb.AppendLine("Login: LoginForm se username/password. Roles: Admin, Manager, Cashier.");
            sb.AppendLine("Role permissions: RoleAccessView se forms ki ijazat set hoti hai.");
            sb.AppendLine();

            sb.AppendLine("--- TOP BAR (frmMain right side) ---");
            sb.AppendLine("⛽ VIP Calc (btnPetroleumCalc): Petroleum calculator — liters, rate, margin, profit calculations.");
            sb.AppendLine("🤖 AI Help (btnGeminiAi): Yehi Gemini AI chat — software, database aur general sawal.");
            sb.AppendLine();

            sb.AppendLine("--- LEFT SIDE MENU (frmMain) — Button → Form ---");
            sb.AppendLine("Home → frmDashBoard (dashboard summary)");
            sb.AppendLine("Add Customer → frmCustomerView (customer add/edit, PetrolAdd entries)");
            sb.AppendLine("Daily Diesel Sales → frmDiselView (rozana customer ko diesel sale — liters, rate, amount, credit)");
            sb.AppendLine("Credit Customer → frmCreditAdjust (customer credit adjust / wasooli)");
            sb.AppendLine("CustomerToCustomer → frmCustomerToCustomerView (ek customer se doosre ko transfer)");
            sb.AppendLine("CustomerToDealer → frmDieselLedger (customer se dealer payment / ledger)");
            sb.AppendLine("Add Dealer → frmDealerNameView (dealer add, DDAmount/DAmount balance)");
            sb.AppendLine("Dealer Payout → frmDieselLedgerView (dealer ko payment / payout entries)");
            sb.AppendLine("DealerAmount → frmStockView (dealer se diesel PURCHASE/stock — AddStock table, AddDisel liters + Rate)");
            sb.AppendLine("DealerToDealer → frmDealertoDealerView (dealer se dealer transfer)");
            sb.AppendLine("Direct Dealer Amount → FrmDirectDealerPaymentAmountView (direct dealer payment)");
            sb.AppendLine("Stock → frmStockDieselView (StockDiesel table — stock diesel entries)");
            sb.AppendLine("Closing Form → frmClosingformEntry (monthly closing, profit/loss calculation)");
            sb.AppendLine("Closing 2 → frmClosing2 (alternate closing form with profit breakdown)");
            sb.AppendLine("Bank Account → frmBankAccountView (bank transactions)");
            sb.AppendLine("Expense → frmExpense (kharcha / Expensetable entries)");
            sb.AppendLine("Report → ReportAndBackup (sari reports + database backup/restore)");
            sb.AppendLine("Password Change → changepassword");
            sb.AppendLine("Logout → LoginForm par wapas");
            sb.AppendLine();

            sb.AppendLine("--- MAIN DATABASE TABLES (SQLite) ---");
            sb.AppendLine("AddCustomer: Customer master (Name, Phone, Address, etc.)");
            sb.AppendLine("PetrolAdd: Daily diesel SALES to customers (Date, CustomerId, Litter, Rate, Amount, Advance, Credit, Balance, ReceiptNo, vehicle, IsInitialEntry)");
            sb.AppendLine("AddDealer: Dealer master (DealerName, DDAmount=total owed, DAmount=paid, balance = DDAmount - DAmount)");
            sb.AppendLine("AddStock: Dealer se PURCHASE (Date, AddDisel=liters, Rate, dealer info) — DealerAmount form");
            sb.AppendLine("StockDiesel: Stock diesel entries (Litter, Rate, Date) — Stock form");
            sb.AppendLine("Expensetable: Expenses (EDate, Amount, description)");
            sb.AppendLine("BankTransactions: Bank entries");
            sb.AppendLine("tblUser: Users (login, uRole)");
            sb.AppendLine("RolePermissions: Role-based form access");
            sb.AppendLine("AuditLog: System audit trail");
            sb.AppendLine();

            sb.AppendLine("--- KEY BUSINESS FORMULAS ---");
            sb.AppendLine("Sale Amount (PetrolAdd): agar Litter aur Rate dono hain → Litter × Rate; warna direct Balance/Amount use karo.");
            sb.AppendLine("Customer Receivable: SUM(Amount + Advance - Credit) per customer, positive = customer ne dena hai.");
            sb.AppendLine("Dealer Payable: DDAmount - DAmount per dealer, positive = humein dealer ko dena hai.");
            sb.AppendLine("Customer Sale Average: Total Sale Amount ÷ Total Sale Liters");
            sb.AppendLine("Dealer Purchase Average: Total Purchase Amount ÷ Total Purchase Liters (AddStock)");
            sb.AppendLine("CLOSING PROFIT (frmClosing2 / Closing Form):");
            sb.AppendLine("  Margin per Liter = Customer Sale Avg - Dealer Purchase Avg");
            sb.AppendLine("  Gross Profit = Margin × Total Sale Liters");
            sb.AppendLine("  NET PROFIT = Gross Profit - Total Expense");
            sb.AppendLine();

            sb.AppendLine("--- REPORTS (ReportAndBackup form se) ---");
            sb.AppendLine("Customer Ledger, Dealer Reports, Profit/Loss, Balance Sheet, Stock Reports,");
            sb.AppendLine("Attendance, Transaction RDLC, Current Date All Entry, Dealer Complete Package,");
            sb.AppendLine("Customer Profit/Loss Percentage, Benefits reports, Two Table Joining, etc.");
            sb.AppendLine("Backup/Restore: database backup aur restore bhi Report form se.");
            sb.AppendLine();

            sb.AppendLine("--- COMMON USER TASKS (kaise karein) ---");
            sb.AppendLine("Naya customer add: Add Customer → naam likhein → save.");
            sb.AppendLine("Aaj ki sale entry: Daily Diesel Sales → customer select → liters, rate, credit fill → save.");
            sb.AppendLine("Dealer se diesel purchase: DealerAmount → dealer, liters (AddDisel), rate → save.");
            sb.AppendLine("Kharcha add: Expense → date, amount, detail → save.");
            sb.AppendLine("Profit dekhna: Closing Form ya Closing 2 → month select → profit breakdown.");
            sb.AppendLine("Customer balance: Add Customer ya Credit Customer form se.");
            sb.AppendLine("Dealer balance: Add Dealer ya Dealer Payout se.");
            sb.AppendLine("Report print: Report menu → report select → date range → print.");
            sb.AppendLine();

            sb.AppendLine("--- AI CHAT COMMANDS ---");
            sb.AppendLine("/software <sawal> → Software ke bare mein (forms, buttons, kaise use karein)");
            sb.AppendLine("/database <sawal> → Live database numbers (sales, stock, expense, profit, balances)");
            sb.AppendLine("Bina prefix ke → Koi bhi general sawal (Gemini jawab dega)");
            sb.AppendLine();

            sb.AppendLine("--- IMPORTANT DISTINCTIONS ---");
            sb.AppendLine("DealerAmount (frmStockView) = PURCHASE liters from dealer (AddStock) — ye SALE nahi hai.");
            sb.AppendLine("Daily Diesel Sales (frmDiselView) = Customer ko diesel SELL (PetrolAdd).");
            sb.AppendLine("Dealer Payable balance ≠ purchase liters — ye paise ka balance hai.");
            sb.AppendLine("Stock (frmStockDieselView) alag table hai StockDiesel — DealerAmount se different.");

            return sb.ToString();
        }
    }
}
