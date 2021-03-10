using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace P_Alarm
{
    /// <summary>
    /// Interakční logika pro App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow wnd = new MainWindow();
            wnd.Show();
            {
                if (e.Args.Length == 1 && e.Args[0] == "-t")
                {
                    //test
                    Trace.WriteLine("  --- test");
                }                    
            }
        }
    }
}
