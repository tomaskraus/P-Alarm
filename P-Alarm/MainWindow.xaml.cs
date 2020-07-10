using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    public class Utils
    {
        public static DispatcherTimer CreateTimer(int seconds, System.EventHandler handler)
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(seconds);
            timer.Tick += handler;

            return timer;
        }
    }


    public class Settings
    {
        public int ALARM_PERIOD_SECS;
        public int CALL_ACTION_DELAY_SECS;
        public string ALARM_TEXT_COUNTDOWN;
        public string ALARM_TEXT_CALL;
        public string ALARM_ACTION;


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
            CALL_ACTION_DELAY_SECS = 3;
            ALARM_TEXT_COUNTDOWN = "Začnu volat sestru za: $ sekund.";
            ALARM_TEXT_CALL = "Volám sestru...";
            ALARM_ACTION = "git -version";
    }
}

    public class AlarmAction
    {
        const int INIT = 0;
        const int COUNTDOWN = 1;
        const int ACTION = 2;
        const int END = 3;
        const int STOPPED = 4;

        private Settings settings;
        private DispatcherTimer timer;
        private int cntdCounter;
        private int state;
        private Action<string> updateTextHandler;

        public AlarmAction(Settings settings, Action<string> updateTextHandler)
        {
            this.settings = settings;
            this.updateTextHandler = updateTextHandler;
            timer = Utils.CreateTimer(1, Action);
            Trace.WriteLine("alarmAction create");
        }

        public void Start()
        {
            state = INIT;
            timer.Start();
            Trace.WriteLine("action start");
        }

        public void Stop()
        {
            state = STOPPED;           
            Trace.WriteLine("action stop");
        }

        public void Action(object sender, EventArgs e)
        {
            if (state == INIT)
            {
                updateTextHandler("");
                Trace.WriteLine("alarmAction INIT");
                cntdCounter = settings.CALL_ACTION_DELAY_SECS;
                state = COUNTDOWN;
            }
            else if (state == COUNTDOWN)
            {
                string info = Regex.Replace(settings.ALARM_TEXT_COUNTDOWN, "\\$", cntdCounter.ToString());
                updateTextHandler(info);

                Trace.WriteLine("alarmAction COUNTDOWN=" + info);

                cntdCounter--;
                if (cntdCounter <= 0)
                {
                    state = ACTION;
                }
            }
            else if (state == ACTION)
            {
                Trace.WriteLine("alarmAction ACTION=" + settings.ALARM_ACTION);
                updateTextHandler(settings.ALARM_TEXT_CALL);
                state = END;
            }
            else if (state == END)
            {
                timer.Stop();
                Trace.WriteLine("alarmAction END");
            }
            else if (state == STOPPED)
            {
                timer.Stop();             
                Trace.WriteLine("alarmAction STOPPED");
            }
            else
            {
                throw new Exception("illegal AlarmAction state: " + state);
            }
           
        }
    }

    /// <summary>
    /// Interakční logika pro MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer AlarmTimer;
        private AlarmAction alarmAction;

        public MainWindow()
        {
            InitializeComponent();

            AlarmTimer = Utils.CreateTimer(Settings.Instance().ALARM_PERIOD_SECS, DoAlarmShow);
            AlarmTimer.Start();

            alarmAction = new AlarmAction(Settings.Instance(), UpdateInfoLabel);
        }

        public static void ResetTimer(DispatcherTimer timer)
        {
            timer.Stop();
            timer.Start();
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            ResetTimer(AlarmTimer);
            alarmAction.Stop();
            this.Hide();
            Trace.WriteLine("Alarm has been snoozed");
        }

        void DoAlarmShow(object sender, EventArgs e)
        {
            InfoLbl.Content = "";
            alarmAction.Start();
            this.Show();
            
        }

        void UpdateInfoLabel(string caption)
        {
            InfoLbl.Content = caption;
        }
    }
}
