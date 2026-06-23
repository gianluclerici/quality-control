using System;
using System.Linq;
using System.Windows.Forms;

namespace Ficep.RobServer
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            //devDept.LicenseManager.Unlock("EU23-126M9-KK77V-JXTY-RMK2");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            string[] args = Environment.GetCommandLineArgs();
            args = args.Skip(1).ToArray();
            if (args.Count() > 0 && args[0] == "/dstv")
                new Form1(args);
            else
            { 
                Application.Run(new Form1(args));
            }
        }
    }
}
