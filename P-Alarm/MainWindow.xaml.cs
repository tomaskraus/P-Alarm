using IniParser;
using IniParser.Model;
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
        public const int BEEP_PITCH = 1720;
        public const int BEEP_DURATION_MS = 200;
        public const int BEEP_DURATION_LONG_MS = 1250;
        
        //-----------------------------------

        public int ALARM_PERIOD_SECS;
        public int CALL_ACTION_DELAY_SECS;

        //how many seconds to beep before application calls an action
        public int BEEP_COUNTDOWN_SECS;
        public string ALARM_TEXT_DEFAULT;
        public string ALARM_TEXT_COUNTDOWN;
        public string ALARM_TEXT_CALL;
        public string ALARM_ACTION_EXE;
        public string ALARM_ACTION_PARAMS;

        private IniData data;

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
            var Parser = new FileIniDataParser();
            data = Parser.ReadFile("config.ini", Encoding.UTF8);


            ALARM_PERIOD_SECS = int.Parse(data["DURATIONS"]["ALARM_PERIOD_SECS"]);
            CALL_ACTION_DELAY_SECS = int.Parse(data["DURATIONS"]["CALL_ACTION_DELAY_SECS"]);
            BEEP_COUNTDOWN_SECS = int.Parse(data["DURATIONS"]["BEEP_COUNTDOWN_SECS"]);

            ALARM_TEXT_DEFAULT = data["TEXTS"]["ALARM_TEXT_DEFAULT"];
            ALARM_TEXT_COUNTDOWN = data["TEXTS"]["ALARM_TEXT_COUNTDOWN"];
            ALARM_TEXT_CALL = data["TEXTS"]["ALARM_TEXT_CALL"];

            ALARM_ACTION_EXE = data["ACTION"]["ALARM_ACTION_EXE"];
            ALARM_ACTION_PARAMS = data["ACTION"]["ALARM_ACTION_PARAMS"];
        }
    }

    public class AlarmAction
    {
        const int INIT = 0;
        const int COUNTDOWN = 1;
        const int COUNTDOWN_BEEP = 2;
        const int ACTION = 3;
        const int END = 4;
        const int STOPPED = 5;

        private Settings settings;
        private DispatcherTimer timer;
        private int cntdCounter;
        private int state;
        private readonly Action<string> UpdateTextHandler;

        public AlarmAction(Settings settings, Action<string> updateTextHandler)
        {
            this.settings = settings;
            this.UpdateTextHandler = updateTextHandler;
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

        private void doBeep()
        {
            Console.Beep(Settings.BEEP_PITCH, Settings.BEEP_DURATION_MS);
        }

        private void doBeepLong()
        {
            Console.Beep(Settings.BEEP_PITCH, Settings.BEEP_DURATION_LONG_MS);
        }

        private void callScript()
        {
            doBeepLong();
            Trace.WriteLine("callScript cmd = " + settings.ALARM_ACTION_EXE + " "
                + settings.ALARM_ACTION_PARAMS);

            try
            {

                var proc = System.Diagnostics.Process.Start(Settings.Instance().ALARM_ACTION_EXE, Settings.Instance().ALARM_ACTION_PARAMS);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message 
                    + ":\n" + 
                    Settings.Instance().ALARM_ACTION_EXE + Settings.Instance().ALARM_ACTION_PARAMS, 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                Application.Current.Shutdown();
            }

            Trace.WriteLine("callScript END");
        }


        public void Action(object sender, EventArgs e)
        {
            if (state == INIT)
            {
                Trace.WriteLine("alarmAction INIT");
                cntdCounter = settings.CALL_ACTION_DELAY_SECS;
                state = COUNTDOWN;
            }
            else if (state == COUNTDOWN || state == COUNTDOWN_BEEP)
            {
                string info = Regex.Replace(settings.ALARM_TEXT_COUNTDOWN, "\\$", cntdCounter.ToString());
                UpdateTextHandler(info);
                Trace.WriteLine("alarmAction COUNTDOWN=" + info);
                cntdCounter--;
                if (cntdCounter < Settings.Instance().BEEP_COUNTDOWN_SECS)
                {
                    doBeep();
                }
                if (cntdCounter <= 0)
                {
                    state = ACTION;
                }
            }
            else if (state == ACTION)
            {
                Trace.WriteLine("alarmAction");
                UpdateTextHandler(settings.ALARM_TEXT_CALL);
                callScript();
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

            try
            {
                AlarmTimer = Utils.CreateTimer(Settings.Instance().ALARM_PERIOD_SECS, DoAlarmShow);
                AlarmTimer.Start();

                alarmAction = new AlarmAction(Settings.Instance(), UpdateInfoLabel);

                ShowWindow();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                Application.Current.Shutdown();
            }
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
            ShowWindow();
        }

        void ShowWindow()
        {
            InfoLbl.Content = Settings.Instance().ALARM_TEXT_DEFAULT;
            alarmAction.Start();
            this.Show();
        }

        void UpdateInfoLabel(string caption)
        {
            InfoLbl.Content = caption;
        }
    }
}
