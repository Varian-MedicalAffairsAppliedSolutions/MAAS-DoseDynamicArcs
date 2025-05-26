using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows;
using System.Xml.Serialization;
using VirtualCones_MCB.Helpers;
using VirtualCones_MCB.Models;

namespace VirtualCones_MCB.ViewModels
{
    public class ProgressViewModel : ObservableObject
    {
        public int _hostPID;
        public bool reminderMessageShown = false;
        public int runs = 1;
        private ObservableCollection<string> _allProgressSteps = new ObservableCollection<string>();
        private string _filePath;
        private bool _intervalEnabled = true;
        private Brush _progressColor;
        private string _progressString;
        private int _refreshInterval = 2;
        private string _titleString = "Progress";
        private Dispatcher dispatcher;
        private int progress;
        private string status;
        private DispatcherTimer timer;
        private DateTime DialogStartTime = DateTime.Now;
        private string mostRecentPtId;

        public ProgressViewModel(int hostPID)
        {
            Loggers loggers = new Loggers();
            _hostPID = hostPID;
            TitleString = $"Progress...";

            ProgressColor = new SolidColorBrush(Color.FromRgb(0, 167, 223));

            string location = System.Reflection.Assembly.GetExecutingAssembly().Location;


            suppressedMessages = new List<string>();
            string suppressedMessagesFilePath = Path.Combine(Path.GetDirectoryName(Loggers.ProgressLogPath), "SuppressedMessages.txt");
            if (File.Exists(suppressedMessagesFilePath))
            {
                suppressedMessages.AddRange(File.ReadAllLines(suppressedMessagesFilePath));
            }


            StartMonitoring();
        }

        public List<string> suppressedMessages = new List<string>();
        public ObservableCollection<string> AllProgressSteps
        {
            get => _allProgressSteps;
            set => SetProperty(ref _allProgressSteps, value);
        }



        public bool IntervalEnabled
        {
            get => _intervalEnabled;
            set => SetProperty(ref _intervalEnabled, value);
        }

        public int Progress
        {
            get => progress;
            set => SetProperty(ref progress, value);
        }

        public Brush ProgressColor
        {
            get { return _progressColor; }
            set
            {
                SetProperty(ref _progressColor, value);
            }
        }


        public string ProgressString
        {
            get => _progressString;
            set => SetProperty(ref _progressString, value);
        }

        public int RefreshInterval
        {
            get => _refreshInterval;
            set => SetProperty(ref _refreshInterval, value);
        }

        public string Status
        {
            get => status;
            set => SetProperty(ref status, value);
        }

        public ICommand StopParentApplicationCommand => new RelayCommand(StopParentApplication);

        public string TitleString
        {
            get => _titleString;
            set => SetProperty(ref _titleString, value);
        }

        public static bool IsFileAccessible(string filePath)
        {
            bool fileExists = File.Exists(filePath);
            if (!fileExists)
                return false;

            try
            {
                using (FileStream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    // File is accessible
                    return true;
                }
            }
            catch (IOException)
            {
                // File is not accessible
                return false;
            }
        }

        public void StopParentApplication()
        {
            try
            {
                Process mainProcess = Process.GetProcessById(_hostPID);
                if (MessageBox.Show($"This will close the full pram, the process: {mainProcess.Id}?\nIf you wish to close the dialog only, click the 'X.'\n\n" +
                    $"Would you like to close all the program?", "Stop?", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    mainProcess.Kill();
                }
            }
            catch (ArgumentException)
            {
                // Handle the case when the process with the given PID does not exist
            }
        }



        private void OnTimerElapsed(object sender, EventArgs e)
        {
            ReadAndUpdateProgress(Loggers.ProgressLogPath);
            CheckForDialogs();
        }

        private void ReadAndUpdateProgress(string filePath)
        {
            if (!File.Exists(filePath))
            {
                // timer.Stop();
                ProgressString = $"File not found. Runs:{runs}";
                runs++;
                Environment.Exit(0);
            }

            try
            {
                if (IsFileAccessible(Loggers.ProgressLogPath))
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        ProgressString = "Reading file...";

                        var progressItem = DeserializeProgressItem(filePath);

                        if (progressItem != null)
                        {
                            ProgressString = progressItem.Message;
                            Progress = (int)Math.Round(progressItem.Percentage, 0);
                        }


                    }
                }
            }
            catch
            {

            }
        }

        private void ReadAndUpdateQueueProgress(string queueFilePath)
        {
            if (!File.Exists(queueFilePath))
            {
                // timer.Stop();
                ProgressString = $"File not found. Runs:{runs}";
                runs++;
                Environment.Exit(0);
            }

            try
            {
                if (IsFileAccessible(Loggers.QueueLogPath))
                {
                    using (var fileStream = new FileStream(queueFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        var progressItem = DeserializeProgressItem(queueFilePath);
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }


        private void StartMonitoring()
        {
            IntervalEnabled = false;
            dispatcher = Dispatcher.CurrentDispatcher;
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(RefreshInterval);
            timer.Tick += OnTimerElapsed;
            timer.Start(); // Start the timer.
        }



        #region <---Suppress Dialogs

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        const uint WM_CLOSE = 0x0010;

        private void CheckForDialogs()
        {
            IntPtr handle = IntPtr.Zero;
            do
            {
                handle = FindWindowEx(IntPtr.Zero, handle, "#32770", null); // Dialog box class
                if (handle != IntPtr.Zero)
                {
                    IntPtr childHandle = IntPtr.Zero;
                    do
                    {
                        // Find the next child window
                        childHandle = FindWindowEx(handle, childHandle, null, null);
                        if (childHandle != IntPtr.Zero)
                        {
                            int length = GetWindowTextLength(childHandle);
                            StringBuilder sb = new StringBuilder(length + 1);
                            GetWindowText(childHandle, sb, sb.Capacity);

                            string messageContent = sb.ToString();
                            if (!string.IsNullOrEmpty(messageContent) && ShouldSuppress(messageContent))
                            {
                                LogSuppressedDialog(messageContent);
                                PostMessage(handle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                                break; // Break if a dialog is found and closed
                            }
                        }
                    } while (childHandle != IntPtr.Zero);
                }
            } while (handle != IntPtr.Zero);
        }

        private bool ShouldSuppress(string message)
        {
            // Convert the message to upper case for case-insensitive comparison
            string upperMessage = message.ToUpper();

            // Check if any clip in SuppressedMessages is a substring of the message
            foreach (var clip in suppressedMessages)
            {
                if (upperMessage.Contains(clip.ToUpper()))
                {
                    return true; // Suppress if any clip is found in the message
                }
            }

            return false; // Don't suppress if none of the clips are found
        }


        private void LogSuppressedDialog(string messageContent)
        {
            string exeDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string suppressedMessagesDirectory = Path.Combine(exeDirectory, "SuppressedMessages");

            // Ensure the directory exists
            if (!Directory.Exists(suppressedMessagesDirectory))
            {
                Directory.CreateDirectory(suppressedMessagesDirectory);
            }

            string suppressedMessagesFilePath = Path.Combine(suppressedMessagesDirectory, Path.GetFileNameWithoutExtension(Loggers.ProgressLogPath) + ".txt");

            // Write the message to the file
            using (StreamWriter sw = new StreamWriter(suppressedMessagesFilePath, true))
            {
                sw.WriteLine($"{DateTime.Now}: MostRecentPtId: {mostRecentPtId} -> {messageContent}");
            }
        }

        public ProgressItem DeserializeProgressItem(string filePath)
        {
            ProgressItem progressItem = null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ProgressItem));
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
                {
                    progressItem = (ProgressItem)serializer.Deserialize(fileStream);
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during deserialization
                Console.WriteLine($"Error deserializing XML file: {ex.Message}");
            }
            return progressItem;
        }



        #endregion Suppress Dialogs--->

    }
}
