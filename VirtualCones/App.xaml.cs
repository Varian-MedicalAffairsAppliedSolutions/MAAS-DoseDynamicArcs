﻿using VirtualCones_MCB.Helpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using MAAS.Common.EulaVerification;
using Prism.Events;

namespace VirtualCones_MCB
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    /// 

    public partial class App : Application
    {
        private const string PROJECT_NAME = "DoseDynamicArcs";
        private const string PROJECT_VERSION = "1.0.0";
        private const string LICENSE_URL = "https://varian-medicalaffairsappliedsolutions.github.io/MAAS-DoseDynamicArcs";
        private const string GITHUB_URL = "https://github.com/Varian-MedicalAffairsAppliedSolutions/MAAS-DoseDynamicArcs";

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            try
            {
                // Create event aggregator for application events
                IEventAggregator eventAggregator = new EventAggregator();
                
                // Get the assembly path
                var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                
                // Check for NOEXPIRE file
                var noexp_path = Path.Combine(path, "NOEXPIRE");
                bool bNoExpire = File.Exists(noexp_path);
                
                // Check for NoAgree.txt file
                bool skipAgree = File.Exists(Path.Combine(path, "NoAgree.txt"));

                // Initialize EULA verification
                var eulaVerifier = new EulaVerifier(PROJECT_NAME, PROJECT_VERSION, LICENSE_URL);
                
                // Get access to the EulaConfig
                var eulaConfig = EulaConfig.Load(PROJECT_NAME);
                if (eulaConfig.Settings == null)
                {
                    eulaConfig.Settings = new ApplicationSettings();
                }

                // Consolidated license validation check
                bool eulaRequired = !skipAgree &&
                                    !eulaVerifier.IsEulaAccepted() &&
                                    !eulaConfig.Settings.EULAAgreed;

                if (eulaRequired)
                {
                    // Load QR code image
                    BitmapImage qrCode = null;
                    try
                    {
                        string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
                        qrCode = new BitmapImage(new Uri($"pack://application:,,,/{assemblyName};component/Resources/qrcode.bmp"));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading QR code: {ex.Message}");
                    }

                    // Show dialog and check result
                    if (eulaVerifier.ShowEulaDialog(qrCode))
                    {
                        eulaConfig.Settings.EULAAgreed = true;
                        eulaConfig.Settings.Validated = false;
                        eulaConfig.Save();
                    }
                    else
                    {
                        MessageBox.Show("You must accept the license to use this application.");
                        Current.Shutdown();
                        return;
                    }
                }

                // Check expiration
                var asmCa = typeof(App).Assembly.CustomAttributes
                    .FirstOrDefault(ca => ca.AttributeType == typeof(AssemblyExpirationDate));
                
                if (asmCa != null &&
                    DateTime.TryParse(asmCa.ConstructorArguments.FirstOrDefault().Value as string,
                        new CultureInfo("en-US"), DateTimeStyles.None, out DateTime endDate))
                {
                    if (DateTime.Now > endDate && !bNoExpire)
                    {
                        MessageBox.Show($"Application has expired. Newer builds with future expiration dates can be found here: {GITHUB_URL}");
                        Current.Shutdown();
                        return;
                    }



                    // Show expiration notice based on validation status
                    if (!bNoExpire && !skipAgree)
                    {
                        string msg;

                        if (!eulaConfig.Settings.Validated)
                        {
                            // First-time message
                            msg = $"The current DoseDynamicArcs application is provided AS IS as a non-clinical, research only tool in evaluation only. The current " +
                            $"application will only be available until {endDate.Date} after which the application will be unavailable. " +
                            "By Clicking 'Yes' you agree that this application will be evaluated and not utilized in providing planning decision support\n\n" +
                            $"Newer builds with future expiration dates can be found here: {GITHUB_URL}\n\n" +
                            "See the FAQ for more information on how to remove this pop-up and expiration";
                        }
                        else
                        {
                            // Returning user message
                            msg = $"Application will only be available until {endDate.Date} after which the application will be unavailable. " +
                            "By Clicking 'Yes' you agree that this application will be evaluated and not utilized in providing planning decision support\n\n" +
                            $"Newer builds with future expiration dates can be found here: {GITHUB_URL}\n\n" +
                            "See the FAQ for more information on how to remove this pop-up and expiration";
                        }

                        if (MessageBox.Show(msg, "Agreement", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                        {
                            Current.Shutdown();
                            return;
                        }
                    }
                }

                // Original VirtualCones application logic
                var app = VMS.TPS.Common.Model.API.Application.CreateApplication();

                string progressPath = null;
                
                // Check arguments (script mode) or standalone mode
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
            catch (Exception ex)
            {
                // Check if Debug is enabled
                bool debugEnabled = false;
                try 
                {
                    debugEnabled = ConfigurationManager.AppSettings["Debug"] == "true";
                }
                catch 
                {
                    // If config access fails, default to false
                    debugEnabled = false;
                }

                if (debugEnabled)
                {
                    MessageBox.Show(ex.ToString());
                }
                else
                {
                    MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                Current.Shutdown();
            }
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

    public class CloseEulaEvent : PubSubEvent
    {
    }
}
