using System;
using System.Globalization;

namespace AlMadinaShahzad.Services
{
    public static class PetroleumCalculatorEngine
    {
        public static decimal Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0m;
            text = text.Trim().Replace(",", "");
            return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0m;
        }

        public static string Format(decimal value)
        {
            return value.ToString("N2", CultureInfo.InvariantCulture);
        }

        public static decimal SaleAmount(decimal liters, decimal rate, decimal advance)
        {
            return liters * rate + advance;
        }

        public static decimal Balance(decimal amount, decimal credit)
        {
            return amount - credit;
        }

        public static decimal GrossProfit(decimal saleRate, decimal purchaseRate, decimal liters)
        {
            return (saleRate - purchaseRate) * liters;
        }

        public static decimal NetProfit(decimal grossProfit, decimal expense)
        {
            return grossProfit - expense;
        }

        public static decimal ProfitPercent(decimal profit, decimal revenue)
        {
            if (revenue == 0m) return 0m;
            return Math.Round(profit / revenue * 100m, 2);
        }

        public static decimal MarginPercent(decimal saleRate, decimal purchaseRate)
        {
            if (purchaseRate == 0m) return 0m;
            return Math.Round((saleRate - purchaseRate) / purchaseRate * 100m, 2);
        }

        public static decimal WeightedAverageRate(decimal totalLiters, decimal totalAmount)
        {
            if (totalLiters == 0m) return 0m;
            return Math.Round(totalAmount / totalLiters, 2);
        }

        public static decimal CustomerNet(decimal debit, decimal credit)
        {
            return debit - credit;
        }

        public static decimal CustomerNetPercent(decimal net, decimal debit)
        {
            if (debit == 0m) return 0m;
            return Math.Round(net / debit * 100m, 2);
        }

        public static decimal RateDifference(decimal saleRate, decimal purchaseRate)
        {
            return saleRate - purchaseRate;
        }

        public static decimal EvaluateExpression(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) return 0m;
            expression = expression.Trim()
                .Replace("×", "*")
                .Replace("÷", "/")
                .Replace(" ", "");
            if (expression.Length == 0) return 0m;

            var table = new System.Data.DataTable();
            var result = table.Compute(expression, null);
            return Convert.ToDecimal(result, CultureInfo.InvariantCulture);
        }
    }
}
