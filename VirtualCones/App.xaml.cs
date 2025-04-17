using AOS_VirtualCones_MCB.Helpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Windows;

namespace AOS_VirtualCones_MCB
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    /// 

    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var app = VMS.TPS.Common.Model.API.Application.CreateApplication();

            string progressPath = null;
            
            // Check if we have enough arguments (script mode) or standalone mode
            if (e.Args != null && e.Args.Length > 3)
            {
                // Running as a script with arguments
                progressPath = GetProgressPath(e.Args[3]);
            }
            else
            {
                // Running as standalone app
                string appPath = AppDomain.CurrentDomain.BaseDirectory;
                progressPath = Path.Combine(appPath, "Progress.xml");
            }

            Loggers.ProgressLogPath = progressPath;

            // Pass the args array even if empty/null
            MainWindow mainWindow = new MainWindow(app, e.Args ?? new string[0]);
            mainWindow.Show();
        }

        private string GetProgressPath(string currentDir)
        {
            string progressPath = Path.Combine(currentDir, "Progress.xml");

            // Delete the file if it exists to start fresh
            if (File.Exists(progressPath))
            {
                try
                {
                    File.Delete(progressPath);
                }
                catch (Exception ex)
                {
                    // Just log the error but continue
                    Console.WriteLine($"Could not delete existing Progress.xml: {ex.Message}");
                }
            }

            return progressPath;
        }        


    }
}
