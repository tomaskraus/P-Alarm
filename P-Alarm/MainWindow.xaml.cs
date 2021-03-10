using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        public static string SecsToTimeStr(int secs)
        {
            if (secs < 0) {
                return "";
            }
            return "" + ((int)(secs / 60))
                + ":" 
                + (
                  (secs % 60 < 10)
                    ? "0"
                    : ""
                )
                + (secs % 60);
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
        public int CALL_2ND_SECS;

        //how long to wait after the application called the action, without the stop button pressed
        public int NOT_STOPPED_ALARM_PERIOD_SECS;
        //how many NOT_STOPPED_ALARM_PERIOD to wait before return to the ALARM_PERIOD_SECS
        public int NOT_STOPPED_ALARM_PERIOD_REPEAT;       

        //how many seconds to beep before the application calls an action
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


            ALARM_PERIOD_SECS = 60 * int.Parse(data["DURATIONS"]["ALARM_PERIOD_MINS"]);
            CALL_ACTION_DELAY_SECS = int.Parse(data["DURATIONS"]["CALL_ACTION_DELAY_SECS"]);
            CALL_2ND_SECS = int.Parse(data["DURATIONS"]["2ND_CALL_DELAY_SECS"]);

            NOT_STOPPED_ALARM_PERIOD_SECS = 60 * int.Parse(data["DURATIONS"]["NOT_STOPPED_ALARM_PERIOD_MINS"]);
            NOT_STOPPED_ALARM_PERIOD_REPEAT = int.Parse(data["DURATIONS"]["NOT_STOPPED_ALARM_PERIOD_REPEAT"]);

            BEEP_COUNTDOWN_SECS = int.Parse(data["DURATIONS"]["BEEP_COUNTDOWN_SECS"]);

            ALARM_TEXT_DEFAULT = data["TEXTS"]["ALARM_TEXT_DEFAULT"];
            ALARM_TEXT_COUNTDOWN = data["TEXTS"]["ALARM_TEXT_COUNTDOWN"];
            ALARM_TEXT_CALL = data["TEXTS"]["ALARM_TEXT_CALL"];

            ALARM_ACTION_EXE = data["ACTION"]["ALARM_ACTION_EXE"];
            ALARM_ACTION_PARAMS = data["ACTION"]["2ND_CALL_DELAY_SECS"];

            Trace.WriteLine("Settings: CREATED");
        }
    }


    public interface IAlarmControl
    {
        void SetStatusText(string text);
        void SetCountDownValue(int secsRemaining);
        //void CallAction();
        void ShowAlarmWindow();
    }


    public class AlarmAction
    {
        const int UNDEF = 0;   //for next state consumed
        const int START = 1;
        const int RUN = 2;
        const int NOT_STOPPED = 3;
        const int COUNTDOWN = 4;
        const int ACTION = 5;
        const int WAIT2 = 6;
        const int ACTION2 = 7;

        private Settings settings;

        private int cntdCounter;
        private int repeatCounter;
        private int state;
        private int nextState;  //to deal with concurrent issues
        private string textToShow;
        private readonly IAlarmControl AlarmCtl;

        private void SetNextState(int next)
        {
            nextState = next;
            Trace.WriteLine("alarmAction nextState=" + next);
        }


        public AlarmAction(Settings settings, IAlarmControl alarmCtl)
        {
            this.settings = settings;
            AlarmCtl = alarmCtl;
            Start();
            Trace.WriteLine("alarmAction create");
        }

        public void Start()
        {
            SetNextState(START);
        }

        public void Reset()
        {
            SetNextState(START);
        }

        private void DoBeep()
        {
            Console.Beep(Settings.BEEP_PITCH, Settings.BEEP_DURATION_MS);
        }

        private void DoBeepLong()
        {
            Console.Beep(Settings.BEEP_PITCH, Settings.BEEP_DURATION_LONG_MS);
        }

        private void callScript()
        {
            DoBeepLong();
            Trace.WriteLine("callScript cmd = " + settings.ALARM_ACTION_EXE + " "
                + settings.ALARM_ACTION_PARAMS);

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(
                    Settings.Instance().ALARM_ACTION_EXE, Settings.Instance().ALARM_ACTION_PARAMS
                    );
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;

                using (Process process = Process.Start(startInfo))
                {
                    //
                    // Read in all the text from the process with the StreamReader.
                    //
                    using (StreamReader reader = process.StandardOutput)
                    {
                        string result = reader.ReadToEnd();
                        Console.Write(result);
                    }
                }
  
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

        private string getCountdownStr(int countdownValue)
        {
            return Regex.Replace(settings.ALARM_TEXT_COUNTDOWN, "\\$", countdownValue.ToString());
        }


        private void CountdownLogic(int StateAfterCountdown)
        {
            textToShow = getCountdownStr(cntdCounter);
            if (cntdCounter < Settings.Instance().BEEP_COUNTDOWN_SECS)
            {
                DoBeep();
            }
            if (cntdCounter <= 0)
            {
                state = StateAfterCountdown;
            }
        }
        

        public void Action(object sender, EventArgs e)
        {
            if (nextState != UNDEF)
            {
                //consume next state
                state = nextState;
                Trace.WriteLine("Action State was set to " + state);
                nextState = UNDEF;
            }
            

            if (state == START)
            {
                Trace.WriteLine("alarmAction START");
                textToShow = settings.ALARM_TEXT_DEFAULT;
                cntdCounter = settings.ALARM_PERIOD_SECS;
                repeatCounter = settings.NOT_STOPPED_ALARM_PERIOD_REPEAT;
                state = RUN;
            }
            else if (state == COUNTDOWN)
            {
                CountdownLogic(ACTION);
            }
            else if (state == ACTION)
            {
                Trace.WriteLine("alarmAction ACTION");
                textToShow = settings.ALARM_TEXT_CALL;
                callScript();
                if (settings.CALL_2ND_SECS > 0)
                {
                    cntdCounter = settings.CALL_2ND_SECS;
                    state = WAIT2;
                } else
                {
                    state = NOT_STOPPED;
                }
            }
            else if (state == WAIT2)
            {
                Trace.WriteLine("alarmAction COUNTDOWN2=" + getCountdownStr(cntdCounter));
                CountdownLogic(ACTION2);
            }
            else if (state == ACTION2)
            {
                Trace.WriteLine("alarmAction ACTION2");
                textToShow = settings.ALARM_TEXT_CALL;
                callScript();
                state = NOT_STOPPED;
            }
            else if (state == NOT_STOPPED)
            {
                repeatCounter--;
                Trace.WriteLine("alarmAction NOT_STOPPED. repeatCounter=" + repeatCounter);
                if (repeatCounter < 0)
                {
                    state = START;
                } else
                {
                    cntdCounter = settings.NOT_STOPPED_ALARM_PERIOD_SECS;
                    state = RUN;
                }
            }
            else if (state == RUN)
            {
                if (cntdCounter <= settings.CALL_ACTION_DELAY_SECS)
                {
                    state = COUNTDOWN;
                    AlarmCtl.ShowAlarmWindow();
                }
            }
            else
            {
                throw new Exception("illegal AlarmAction state: " + state);
            }

            AlarmCtl.SetStatusText(textToShow);
            AlarmCtl.SetCountDownValue(cntdCounter);
            cntdCounter--;
        }

        public void SpeedUpAction()
        {
            cntdCounter = settings.CALL_ACTION_DELAY_SECS + 3;
        }
    }

    /// <summary>
    /// Interakční logika pro MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IAlarmControl
    {
        private DispatcherTimer AlarmTimer;
        private AlarmAction alarmAction;

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                ShowAlarmWindow();               
                StartAlarmLoop();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                Application.Current.Shutdown();
            }
        }

        void StartAlarmLoop()
        {
            alarmAction = new AlarmAction(Settings.Instance(), this);
            AlarmTimer = Utils.CreateTimer(1, alarmAction.Action);
            AlarmTimer.Start();
            Trace.WriteLine("- - - Start Alarm Loop. Period: " + Settings.Instance().ALARM_PERIOD_SECS / 60 + " minute(s)");
        }

        

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            
            alarmAction.Reset();
            WindowState = WindowState.Minimized;
            Trace.WriteLine("Alarm has been snoozed");
        }

        private void SpeedupBtn_Click(object sender, RoutedEventArgs e)
        {
            alarmAction.SpeedUpAction();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            SetStatusText(Settings.Instance().ALARM_TEXT_DEFAULT);
            Trace.WriteLine("on activated");
        }

        public void SetStatusText(string text)
        {
            InfoLbl.Content = text;
        }

        public void SetCountDownValue(int secsRemaining)
        {
            CountdownLbl.Content = Utils.SecsToTimeStr(secsRemaining);
        }

        public void ShowAlarmWindow()
        {
            WindowState = WindowState.Normal;
        }


        //public void CallAction()
        //{
        //    throw new NotImplementedException();
        //}
    }
}
