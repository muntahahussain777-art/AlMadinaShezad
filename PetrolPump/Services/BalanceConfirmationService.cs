using AlMadinaShahzad.Model;
using System;
using System.Collections;
using System.Data;
using System.Windows.Forms;

namespace AlMadinaShahzad.Services
{
    /// <summary>
    /// Balance 0 ya negative hone par save se pehle Yes/No confirmation.
    /// Daily Diesel Sales aur Dealer Amount forms excluded (unke save handlers mein call nahi).
    /// </summary>
    public static class BalanceConfirmationService
    {
        public static decimal GetCustomerPetrolRemaining(int customerId)
        {
            string query = @"SELECT 
                IFNULL(SUM(IFNULL(Amount,0) - IFNULL(Advance,0)),0) AS TotalAmount,
                IFNULL(SUM(IFNULL(Credit,0)),0) AS TotalCredit
                FROM PetrolAdd WHERE CustomerId = @id";
            var ht = new Hashtable { { "@id", customerId } };
            DataTable dt = MainClass.ExecuteSelectQuery(query, ht);
            if (dt == null || dt.Rows.Count == 0) return 0;
            decimal totalAmount = Convert.ToDecimal(dt.Rows[0]["TotalAmount"]);
            decimal totalCredit = Convert.ToDecimal(dt.Rows[0]["TotalCredit"]);
            return totalAmount - totalCredit;
        }

        public static decimal GetCustomerLedgerRemaining(int customerId)
        {
            var ht = new Hashtable { { "@customerId", customerId } };

            string initialQuery = @"SELECT IFNULL(SUM(Balance),0) AS InitialBalance 
                FROM PetrolAdd WHERE CustomerId = @customerId AND IsInitialEntry = 1";
            DataTable dtInitial = MainClass.ExecuteSelectQuery(initialQuery, ht);
            decimal initial = 0;
            if (dtInitial != null && dtInitial.Rows.Count > 0)
                initial = Convert.ToDecimal(dtInitial.Rows[0]["InitialBalance"]);

            string creditQuery = @"SELECT IFNULL(SUM(Credit),0) AS TotalCredit 
                FROM PetrolAdd WHERE CustomerId = @customerId AND IsInitialEntry = 0";
            DataTable dtCredit = MainClass.ExecuteSelectQuery(creditQuery, ht);
            decimal credits = 0;
            if (dtCredit != null && dtCredit.Rows.Count > 0)
                credits = Convert.ToDecimal(dtCredit.Rows[0]["TotalCredit"]);

            return initial - credits;
        }

        public static decimal GetDealerCreditBalance(int dealerId)
        {
            string query = "SELECT IFNULL(DAmount,0) AS DAmount, IFNULL(DDAmount,0) AS DDAmount FROM AddDealer WHERE Did = @id";
            var ht = new Hashtable { { "@id", dealerId } };
            DataTable dt = MainClass.ExecuteSelectQuery(query, ht);
            if (dt == null || dt.Rows.Count == 0) return 0;
            decimal dAmount = Convert.ToDecimal(dt.Rows[0]["DAmount"]);
            decimal ddAmount = Convert.ToDecimal(dt.Rows[0]["DDAmount"]);
            return ddAmount - dAmount;
        }

        public static decimal GetBankBalance(string bankName, int excludeTransactionId = 0)
        {
            if (string.IsNullOrWhiteSpace(bankName)) return 0;
            string query = excludeTransactionId > 0
                ? "SELECT IFNULL(SUM(CAST(Amount AS REAL)),0) FROM BankTransactions WHERE BankName = @bank AND Id <> @excludeId"
                : "SELECT IFNULL(SUM(CAST(Amount AS REAL)),0) FROM BankTransactions WHERE BankName = @bank";
            var ht = new Hashtable { { "@bank", bankName.Trim() } };
            if (excludeTransactionId > 0)
                ht.Add("@excludeId", excludeTransactionId);
            DataTable dt = MainClass.ExecuteSelectQuery(query, ht);
            if (dt == null || dt.Rows.Count == 0) return 0;
            return Convert.ToDecimal(dt.Rows[0][0]);
        }

        public static decimal NetDeduction(decimal newDeduction, decimal oldDeduction, bool isEditSameEntity)
        {
            if (!isEditSameEntity) return newDeduction;
            return newDeduction - oldDeduction;
        }

        public static bool ConfirmCustomerPetrolCredit(int customerId, decimal creditAmount, string displayName = null)
        {
            if (creditAmount <= 0) return true;
            string name = string.IsNullOrWhiteSpace(displayName) ? "Customer" : displayName.Trim();
            return ConfirmDeduction(name, GetCustomerPetrolRemaining(customerId), creditAmount);
        }

        public static bool ConfirmCustomerLedgerDeduction(int customerId, decimal deduction, string displayName = null)
        {
            if (deduction <= 0) return true;
            string name = string.IsNullOrWhiteSpace(displayName) ? "Customer" : displayName.Trim();
            return ConfirmDeduction(name, GetCustomerLedgerRemaining(customerId), deduction);
        }

        public static bool ConfirmDealerCreditDeduction(int dealerId, decimal deduction, string displayName = null)
        {
            if (deduction <= 0) return true;
            string name = string.IsNullOrWhiteSpace(displayName) ? "Dealer" : displayName.Trim();
            return ConfirmDeduction(name, GetDealerCreditBalance(dealerId), deduction);
        }

        public static bool ConfirmBankOutTransaction(string bankName, decimal storedAmount, int excludeTransactionId = 0)
        {
            if (storedAmount >= 0) return true;

            decimal current = GetBankBalance(bankName, excludeTransactionId);
            decimal projected = current + storedAmount;

            // Sirf jab balance 0 ya negative ho — har Out par nahi
            if (projected > 0) return true;

            string bank = string.IsNullOrWhiteSpace(bankName) ? "Bank" : bankName.Trim();
            string message;

            if (projected == 0)
            {
                message = bank + " ka balance 0 ho jayega.\n" +
                          "(Abhi: " + current.ToString("N2") + " — Out: " + Math.Abs(storedAmount).ToString("N2") + ")\n\n" +
                          "Kya aap phir bhi ye entry karna chahte hain?";
            }
            else
            {
                message = bank + " ka balance negative ho jayega (" + projected.ToString("N2") + ").\n" +
                          "(Abhi: " + current.ToString("N2") + " — Out: " + Math.Abs(storedAmount).ToString("N2") + ")\n\n" +
                          "Kya aap phir bhi ye entry karna chahte hain?";
            }

            using (var yn = new YesOrNoMessage(message, "Confirm Bank Entry"))
                return yn.ShowDialog() == DialogResult.Yes;
        }

        private static bool ConfirmDeduction(string entityLabel, decimal currentBalance, decimal deduction)
        {
            decimal projected = currentBalance - deduction;
            if (projected > 0) return true;

            string message;
            if (projected == 0)
            {
                message = entityLabel + " ka amount 0 ho jayega.\n" +
                          "(Abhi: " + currentBalance.ToString("N2") + " — Entry: " + deduction.ToString("N2") + ")\n\n" +
                          "Kya aap phir bhi entry karna chahte hain?";
            }
            else
            {
                message = entityLabel + " ka record khatam ho chuka hai (balance " + projected.ToString("N2") + " ho jayega).\n\n" +
                          "Kya aap entry phir bhi karna chahte hain?";
            }

            using (var yn = new YesOrNoMessage(message, "Confirm Entry"))
                return yn.ShowDialog() == DialogResult.Yes;
        }
    }
}
