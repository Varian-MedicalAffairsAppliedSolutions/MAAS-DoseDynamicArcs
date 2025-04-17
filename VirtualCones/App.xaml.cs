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

            Loggers.ProgressLogPath = GetProgressPath(e.Args[3]);

            MainWindow mainWindow = new MainWindow(app, e.Args);
            mainWindow.Show();
        }

        private string GetProgressPath(string currentDir)
        {
            string progressPath = Path.Combine(currentDir, "Progress.xml");

                if (!File.Exists(progressPath))
                {
                    File.Delete(progressPath);
                }

            return progressPath;

        }        


    }
}
