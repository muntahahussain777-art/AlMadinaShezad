using AlMadinaShahzad.Model;
using AlMadinaShahzad.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AlMadinaShahzad
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) =>
            {
                MessageBox.Show("Unexpected error: " + e.Exception.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                MessageBox.Show("Unexpected error: " + (ex != null ? ex.Message : "Unknown"),
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };
            Application.Run(new LoginForm());
        }
    }
}
