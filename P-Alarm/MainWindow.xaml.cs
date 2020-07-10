using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Windows.Threading;

namespace P_Alarm
{

    public class Settings
    {
        public int ALARM_PERIOD_SECS = 10;
        public int CALL_ACTION_DELAY_SECS = 5;
        public string ALARM_TEXT_DELAY = "Začnu volat sestru za: {} sekund.";
        public string ALARM_ACTION = "git -version";


        private static Settings instance = null;

        public static Settings Instance()
        {
            if (instance == null)
            {
                instance = new Settings();
            }
            return instance;
        }

        private Settings()
        {
            ALARM_PERIOD_SECS = 10;
            CALL_ACTION_DELAY_SECS = 5;
            ALARM_TEXT_DELAY = "Začnu volat sestru za: {} sekund.";
            ALARM_ACTION = "git -version";
    }
}

    /// <summary>
    /// Interakční logika pro MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            CreateTimer(Settings.Instance().ALARM_PERIOD_SECS, Timer_Tick).Start();
        }

        private DispatcherTimer CreateTimer(int seconds, System.EventHandler handler)
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(seconds);
            timer.Tick += handler;
            
            return timer;
        }

        public static void ResetTimer(DispatcherTimer timer)
        {
            timer.Stop();
            timer.Start();
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show("Ahoj");
            this.Hide();
        }

        void Timer_Tick(object sender, EventArgs e)
        {
            InfoLbl.Content = DateTime.Now.ToLongTimeString();
            this.Show();
            //MessageBox.Show("Ahoj");
        }
    }
}
