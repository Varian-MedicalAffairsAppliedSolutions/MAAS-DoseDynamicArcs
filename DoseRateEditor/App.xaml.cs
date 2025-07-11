using Autofac;
using ControlzEx.Standard;
using DoseRateEditor.Startup;
using DoseRateEditor.ViewModels;
using DoseRateEditor.Views;
using MAAS.Common.EulaVerification;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using VMS.TPS.Common.Model.API;
using MessageBox = System.Windows.MessageBox;

[assembly: ESAPIScript(IsWriteable = true)]
namespace DoseRateEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    /// <summary>

    /// Interaction logic for App.xaml

    /// </summary>
    // Allow script to edit patient data
    
    public partial class App : System.Windows.Application

    {

        private VMS.TPS.Common.Model.API.Application _app;
        private MainView MV;
        private string _patientId;
        private string _courseId;
        private string _planId;
        private Patient _patient;
        private Course _course;
        private PlanSetup _plan;

        // Define the project information for EULA verification
        private const string PROJECT_NAME = "DoseDynamicArcs";
        private const string PROJECT_VERSION = "1.0.0";
        private const string LICENSE_URL = "https://varian-medicalaffairsappliedsolutions.github.io/MAAS-DoseDynamicArcs";
        private const string GITHUB_URL = "https://github.com/Varian-MedicalAffairsAppliedSolutions/MAAS-DoseDynamicArcs";

        private void start(object sender, StartupEventArgs e)
        {
            using (_app = VMS.TPS.Common.Model.API.Application.CreateApplication())
            {
                if (e.Args.Count() > 0 && !String.IsNullOrWhiteSpace(e.Args.First()))
                {
                    _patientId = e.Args.First().Split(';').First().Trim('\"');
                }
                else
                {
                    MessageBox.Show("Patient not specified at application start.");
                    System.Windows.Application.Current.Shutdown();
                    return;
                }

                if (e.Args.First().Split(';').Count() > 1)
                {
                    _courseId = e.Args.First().Split(';').ElementAt(1).TrimEnd('\"');
                }
                if (e.Args.First().Split(';').Count() > 2)
                {
                    _planId = e.Args.First().Split(';').ElementAt(2).TrimEnd('\"');
                }
                if (String.IsNullOrWhiteSpace(_patientId) || String.IsNullOrWhiteSpace(_courseId))
                {
                    MessageBox.Show("Patient and/or Course not specified at application start. Please open a patient and course.");
                    System.Windows.Application.Current.Shutdown();
                    return;
                }
                _patient = _app.OpenPatientById(_patientId);

                if (!String.IsNullOrWhiteSpace(_courseId))
                {
                    _course = _patient.Courses.FirstOrDefault(x => x.Id == _courseId);
                }
                if (!String.IsNullOrEmpty(_planId))
                {
                    _plan = _course.PlanSetups.FirstOrDefault(x => x.Id == _planId);
                }

                var bootstrap = new Bootstrapper();
                var container = bootstrap.Bootstrap(_app.CurrentUser, _app, _patient, _course, _plan);

                MV = container.Resolve<MainView>();
                MV.DataContext = container.Resolve<MainViewModel>();
                MV.ShowDialog();

                _app.ClosePatient();
                System.Windows.Application.Current.Shutdown();
            }
        }

        public Configuration GetUpdatedConfigFile()
        {
            var exePath = Assembly.GetExecutingAssembly().Location;
            var configPath = exePath + ".config";
            using (var fileStream = new FileStream(configPath, FileMode.Open))
            {
                if (!fileStream.CanWrite)
                {
                    System.Windows.MessageBox.Show($"Cannot update config file.\nUser does not have rights to {configPath}");
                    return null;
                }
            }
            //this needs to be the path running the application
            return ConfigurationManager.OpenExeConfiguration(exePath);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            try
            {
                IEventAggregator eventAggregator = new EventAggregator();

                // Check for NOEXPIRE file
                var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var noexp_path = Path.Combine(path, "NOEXPIRE");
                bool foundNoExpire = File.Exists(noexp_path);
                
                // Check for NoAgree.txt file 
                bool skipAgree = File.Exists(Path.Combine(path, "NoAgree.txt"));

                // Verify EULA acceptance with the JotForm verification system
                var eulaVerifier = new EulaVerifier(PROJECT_NAME, PROJECT_VERSION, LICENSE_URL);
                
                // Get access to the EulaConfig
                var eulaConfig = EulaConfig.Load(PROJECT_NAME);
                if (eulaConfig.Settings == null)
                {
                    eulaConfig.Settings = new ApplicationSettings();
                }
                
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
                        System.Windows.Application.Current.Shutdown();
                        return;
                    }
                }

                // Get expiration date from assembly attribute
                var asmCa = Assembly.GetExecutingAssembly().CustomAttributes.FirstOrDefault(ca => ca.AttributeType == typeof(AssemblyExpirationDate));
                DateTime exp;
                var provider = new CultureInfo("en-US");
                
                // Try to parse expiration date
                if (asmCa != null && DateTime.TryParse(asmCa.ConstructorArguments.FirstOrDefault().Value as string, provider, DateTimeStyles.None, out exp))
                {
                    // Check exp date
                    if (exp < DateTime.Now && !foundNoExpire)
                    {
                        MessageBox.Show($"Application has expired. Newer builds with future expiration dates can be found here: {GITHUB_URL}");
                        System.Windows.Application.Current.Shutdown();
                        return;
                    }



                    // Show expiration notice based on validation status
                    if (!foundNoExpire && !skipAgree)
                    {
                        string msg;

                        if (!eulaConfig.Settings.Validated)
                        {
                            // First-time message
                            msg = $"The current DoseDynamicArcs application is provided AS IS as a non-clinical, research only tool in evaluation only. The current " +
                            $"application will only be available until {exp.Date} after which the application will be unavailable. " +
                            "By Clicking 'Yes' you agree that this application will be evaluated and not utilized in providing planning decision support\n\n" +
                            $"Newer builds with future expiration dates can be found here: {GITHUB_URL}\n\n" +
                            "See the FAQ for more information on how to remove this pop-up and expiration";
                        }
                        else
                        {
                            // Returning user message
                            msg = $"Application will only be available until {exp.Date} after which the application will be unavailable. " +
                            "By Clicking 'Yes' you agree that this application will be evaluated and not utilized in providing planning decision support\n\n" +
                            $"Newer builds with future expiration dates can be found here: {GITHUB_URL}\n\n" +
                            "See the FAQ for more information on how to remove this pop-up and expiration";
                        }

                        if (MessageBox.Show(msg, "Agreement", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                        {
                            System.Windows.Application.Current.Shutdown();
                            return;
                        }
                    }

                    // If we make it this far start the app
                    start(sender, e);
                }
                else
                {
                    MessageBox.Show("Unable to determine application expiration date. The application will now close.");
                    System.Windows.Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                if (ConfigurationManager.AppSettings["Debug"] == "true")
                {
                    MessageBox.Show(ex.ToString());
                }
                else
                {
                    MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                
                if (_app != null)
                {
                    _app.Dispose();
                }
                
                System.Windows.Application.Current.Shutdown();
            }
        }
    }
}

public class CloseEulaEvent : Prism.Events.PubSubEvent
{
}
