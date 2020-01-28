using NLog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Drawing;

namespace LightActivityTracker
{
    public class Program
    {
        static void Main(string[] args)
        {
            var track = new Tracker();

            AppDomain.CurrentDomain.UnhandledException += track.AppDomain_CurrentDomain_UnhandledException;
            Application.Run();
        }
    }

    public class ActivityEvent
    {
        public DateTime dt { get; set; }
        public string Process { get; set; }
        public string Title { get; set; }
        public bool IsAFK_Event { get; set; }
    }


    public class Filter
    {
        public string ProcessRegEx { get; set; }
        public string TitleRegEx { get; set; }
        public string RedactedProcess { get; set; }
        public string RedactedTitle { get; set; }
    }


    public class Tracker
    {
        ActivityEvent lastActivitiy;
        public void AppDomain_CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // use logger here to log the events exception object
            // before the application quits
            _log.Error(e.ExceptionObject.ToString());
            
        }

        public static string myPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        public Logger _log;

        Thread window_watcher;

        DateTime lastAction;

        Filter[] filter_expressions;

        void LogActivity(ActivityEvent act)
        {
            var f = Path.Combine(myPath, "activities.csv");
            _log.Info("activity change detected");

            bool isNew = File.Exists(f);
            lock (this)
            {
                using (var writer = new StreamWriter(f, true))
                using (var csv = new CsvHelper.CsvWriter(writer, new CsvHelper.Configuration.Configuration() { Encoding = Encoding.UTF8, HasHeaderRecord = !isNew, CultureInfo = System.Globalization.CultureInfo.CurrentCulture }))
                {
                    csv.Configuration.TypeConverterOptionsCache.GetOptions<DateTime>().Formats = new[] { "yyyy-MM-dd HH:mm:ss.fff" };
                    csv.WriteRecords(new List<ActivityEvent>() { act });
                    csv.Flush();
                }
            }
            //activity change
            lastActivitiy = act;
        }

        public Tracker()
        {
            //detect log on/off events
            Microsoft.Win32.SystemEvents.PowerModeChanged += new Microsoft.Win32.PowerModeChangedEventHandler((o, e) =>
            {
                var act = new ActivityEvent() { dt = DateTime.Now, Process = "Manual_PowerModeChanged", IsAFK_Event = true };
                switch (e.Mode)
                {
                    case Microsoft.Win32.PowerModes.Resume:
                        _log.Info("resume");
                        act.Title = "resume";
                        break;
                    case Microsoft.Win32.PowerModes.Suspend:
                        _log.Info("sleep");
                        act.Title = "sleep";
                        break;
                }
                LogActivity(act);
            });

            Microsoft.Win32.SystemEvents.SessionEnding += new Microsoft.Win32.SessionEndingEventHandler((o, e) =>
            {
                var act = new ActivityEvent() { dt = DateTime.Now, Process = "Manual_SessionEnding", IsAFK_Event = true };

                switch (e.Reason)
                {
                    case Microsoft.Win32.SessionEndReasons.Logoff:
                        _log.Info("logoff");
                        act.Title = "logoff";
                        break;
                    case Microsoft.Win32.SessionEndReasons.SystemShutdown:
                        _log.Info("shutdown");
                        act.Title = "shutdown";
                        break;
                }
                LogActivity(act);
            });


           filter_expressions = Newtonsoft.Json.JsonConvert.DeserializeObject<Filter[]>(File.ReadAllText(Path.Combine(myPath, "filters.json"), Encoding.UTF8));
            lastAction = DateTime.Now;
            var config = new NLog.Config.LoggingConfiguration();

            // Targets where to log to: File and Console
            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = System.IO.Path.Combine(myPath, "log_file.txt"), ArchiveAboveSize = 5*1024*1024, MaxArchiveFiles = 2 };
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

            // Rules for mapping loggers to targets            
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);
            

            // Apply config           
            NLog.LogManager.Configuration = config;
            
            _log = NLog.LogManager.GetCurrentClassLogger();

            lastActivitiy = null;

            var f = Path.Combine(myPath, "activities.csv");
            //backup
            if (File.Exists(f))
                File.Copy(f, f + ".bak", true);

            NotifyIcon trayIcon = new NotifyIcon
            {
                Text = "ActivityWatch",
                Icon = new Icon("icon.ico", 64, 64)
            };
            trayIcon.Text = $"LightActivityTracker";
            ContextMenu trayMenu = new ContextMenu();

            trayMenu.MenuItems.Add("Quit", new EventHandler((o, e) => 
            {
                _log.Info("exiting");
                var act = new ActivityEvent() { dt = DateTime.Now, Process = "Manual_Exit", IsAFK_Event = true };
                LogActivity(act);
                Environment.Exit(1);
            }));

            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;

            window_watcher = new Thread(new ThreadStart(() =>
            {
                while (true)
                {
                    try
                    {
                        WindowWatcher.GetActiveWindowTitleAndProcess();
                    }
                    catch (Exception ex)
                    {
                        _log.Error($"error in get active window {ex.ToString()}");
                    }
                    

                    //_log.Info($"{WindowWatcher.ActiveWindowProcess} - {WindowWatcher.ActiveWindowTitle}");
                    //log events on window change
                    //log afk event if afk
                    

                    var act = new ActivityEvent()
                    {
                        dt = DateTime.Now,
                        IsAFK_Event = false,
                        Process = WindowWatcher.ActiveWindowProcess,
                        Title = WindowWatcher.ActiveWindowTitle
                    };

                    if ((DateTime.Now - lastAction).TotalSeconds > 60)
                    {
                        _log.Info($"afk since {lastAction.ToShortTimeString()}");
                        act.dt = lastAction;
                        if (act.dt < lastActivitiy.dt)
                            act.dt = lastActivitiy.dt + TimeSpan.FromSeconds(1);
                        act.IsAFK_Event = true;
                    }

                    //filter
                    foreach (var filter in filter_expressions)
                    {
                        if (Regex.IsMatch(act.Process, filter.ProcessRegEx, RegexOptions.IgnoreCase) &&
                            Regex.IsMatch(act.Title, filter.TitleRegEx, RegexOptions.IgnoreCase))
                        {
                            act.Title = filter.RedactedTitle;
                            act.Process = filter.RedactedProcess;
                        }
                    }
                    
                    if (lastActivitiy == null || lastActivitiy.Process != act.Process || lastActivitiy.Title != act.Title || lastActivitiy.IsAFK_Event != act.IsAFK_Event)
                    {
                        LogActivity(act);
                    }


                    Thread.Sleep(450);
                }
            }));

            window_watcher.Start();


            KeyboardHook.HotKeyPressed += new EventHandler<int>((sender, e) => {
                //_log.Info("keypress");
                lastAction = DateTime.Now;
            });

#if !DEBUG
            MouseHook.MouseAction += new EventHandler((object sender, EventArgs e) => {
                //_log.Info("mouse click");
                lastAction = DateTime.Now;
            });
#endif

            var act2 = new ActivityEvent() { dt = DateTime.Now, Process = "Manual_Startup", IsAFK_Event = true };
            LogActivity(act2);
        }


    }

}
