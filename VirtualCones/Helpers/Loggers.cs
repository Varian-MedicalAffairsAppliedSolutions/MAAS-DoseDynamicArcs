using VirtualCones_MCB.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace VirtualCones_MCB.Helpers
{
    public class Loggers
    {
        public static string QueueLogPath { get; set; }
        public static string DebugLogPath { get; set; }
        public static void InitializeTempLogger(string UniqueProgressPath)
        {
            string executablePath = AppDomain.CurrentDomain.BaseDirectory;
            string directoryPath = Path.Combine(executablePath, "Temp");

            string inTempLogPath = Path.GetFileNameWithoutExtension(UniqueProgressPath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            TempLogPath = Path.Combine(directoryPath, inTempLogPath + ".txt");
        }

        public static void InitializeProgressLogger(string inProgressLogPath)
        {
            ProgressLogPath = inProgressLogPath;
        }

        public static void InitializeProgressLogger(string inProgressLogPath, string inQueueProgressLogPath)
        {
            ProgressLogPath = inProgressLogPath;
            QueueLogPath = inQueueProgressLogPath;
        }


        public static string ProgressLogPath { get; set; }

        public static int CurrentItem { get; set; }
        public static int TotalItems { get; set; }
        public static void ProgressLog(int increment, string Message)
        {
            CurrentItem += increment;
            if (CurrentItem > TotalItems)
            {
                CurrentItem = TotalItems;
            }

            var currentProgress = new ProgressItem(CurrentItem, TotalItems, $"{Message} ({CurrentItem}/{TotalItems})");

            // Serialize initialProgress into an XML file
            var serializer = new XmlSerializer(typeof(ProgressItem));
            try
            {
                using (var stream = new FileStream(ProgressLogPath, FileMode.Create))
                {
                    serializer.Serialize(stream, currentProgress);
                }
            }
            catch { }
        }
        public static int CurrentQItem { get; set; }
        public static int TotalQItems { get; set; }

        public static void QueueProgressLog(int increment, string Message)
        {
            CurrentQItem += increment;
            if (CurrentQItem > TotalQItems)
            {
                CurrentQItem = TotalQItems;
            }

            var currentProgress = new ProgressItem(CurrentQItem, TotalQItems, $"{Message} ({CurrentQItem}/{TotalQItems})");

            // Serialize initialProgress into an XML file
            var serializer = new XmlSerializer(typeof(ProgressItem));

            try
            {
                using (var stream = new FileStream(QueueLogPath, FileMode.Create))
                {
                    serializer.Serialize(stream, currentProgress);
                }
            }
            catch { }
        }

        public void WriteToFullLog(string PatientID, string message, bool ERROR)
        {
            string callingMethodName = "NA";
            try
            {
                // Get the stack trace for the current thread
                StackTrace stackTrace = new StackTrace();
                // Get the calling method from the stack trace (the method at index 1, since the method at index 0 is WriteToFullLog itself)
                StackFrame callingFrame = stackTrace.GetFrame(1);
                MethodBase callingMethod = callingFrame.GetMethod();
                callingMethodName = callingMethod.Name;
                //// Combine the calling method information with the message
                //string fullMessage = $"{callingMethod.DeclaringType.FullName}.{callingMethod.Name}: {message}";
            }
            catch
            {
            }

            string FullLogPath = "";
            try
            {
                if (string.IsNullOrEmpty(FullLogPath))
                {
                    string executablePath = AppDomain.CurrentDomain.BaseDirectory;
                    FullLogPath = Path.Combine(executablePath, "FullLog.txt");
                }

                string patID = "NA";
                string userName = "NA";
                if (PatientID != null)
                {
                    patID = PatientID;
                }

                string error = "";
                if (ERROR)
                {
                    error = "ERROR!: ";
                }

                string line = $"{DateTime.Now},{patID},{callingMethodName}: {error}{message}";

                using (StreamWriter writer = new StreamWriter(FullLogPath, true))
                {
                    writer.WriteLine(line);
                }
            }
            catch
            {
            }
        }

        public static string TempLogPath { get; private set; }

        public void TempLog(string PatientID, string message, bool ERROR)
        {
            string callingMethodName = "NA";
            try
            {
                // Get the stack trace for the current thread
                StackTrace stackTrace = new StackTrace();
                // Get the calling method from the stack trace (the method at index 1, since the method at index 0 is WriteToFullLog itself)
                StackFrame callingFrame = stackTrace.GetFrame(1);
                MethodBase callingMethod = callingFrame.GetMethod();
                callingMethodName = callingMethod.Name;
                //// Combine the calling method information with the message
                //string fullMessage = $"{callingMethod.DeclaringType.FullName}.{callingMethod.Name}: {message}";
            }
            catch
            {
            }

            string FullLogPath = "";
            try
            {

                string patID = "NA";
                string userName = "NA";
                if (PatientID != null)
                {
                    patID = PatientID;
                }

                string error = "";
                if (ERROR)
                {
                    error = "ERROR!: ";
                }

                string line = $"{DateTime.Now},{patID},{callingMethodName}: {error}{message}";

                using (StreamWriter writer = new StreamWriter(TempLogPath, true))
                {
                    writer.WriteLine(line);
                }
            }
            catch
            {
            }
        }
    }
}
