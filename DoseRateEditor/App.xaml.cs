using Autofac;
using DoseRateEditor.Startup;
using DoseRateEditor.ViewModels;
using DoseRateEditor.Views;
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

        private void shutdown()
        {
            _app.Dispose();

            App.Current.Shutdown();

        }

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
                    App.Current.Shutdown();
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
                    App.Current.Shutdown();
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
            }
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            
            // Check for NOEXPIRE file
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var noexp_path = Path.Combine(path, "NOEXPIRE");
            bool foundNoExpire = File.Exists(noexp_path);

            // search for json config in current dir
            //var json_path = Path.Combine(path, "config.json");
            //if (!File.Exists(json_path)) { throw new Exception($"Could not locate json path {json_path}"); }

            // Test
            // Create serialized verion of settings
            /*
            var settings = new SettingsClass();
            settings.Debug = false;
            settings.EULAAgreed = false;
            settings.Validated= false;
            settings.ExpirationDate = DateTime.Parse("1/1/2024");
            File.WriteAllText(Path.Combine(path, "config.json"), JsonConvert.SerializeObject(settings));*/

            //var asmCa = typeof(StartupCore).Assembly.CustomAttributes.FirstOrDefault(ca => ca.AttributeType == typeof(AssemblyExpirationDate));
            //if (configUpdate != null && DateTime.TryParse(asmCa.ConstructorArguments.FirstOrDefault().Value as string, provider, DateTimeStyles.None, out endDate) && eulaValue == "true")

            var asmCa = Assembly.GetExecutingAssembly().CustomAttributes.FirstOrDefault(ca => ca.AttributeType == typeof(AssemblyExpirationDate));
            DateTime exp;
            var provider = new CultureInfo("en-US");
            DateTime.TryParse(asmCa.ConstructorArguments.FirstOrDefault().Value as string, provider, DateTimeStyles.None, out exp);

            if (exp < DateTime.Now && !foundNoExpire)
            {
                MessageBox.Show("Application has expired. Newer builds with future expiration dates can be found here: https://github.com/Varian-Innovation-Center/MAAS-DoseDynamicArcs");
                return;
            }

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            bool EULAAgreed = config.AppSettings.Settings["EULAAgreed"].Value.ToLower() == "true";


            // Initial EULA agreement
            if (!EULAAgreed)
            {
                var msg0 = "You are bound by the terms of the Varian Limited Use Software License Agreement (LULSA). \"To stop viewing this message set EULA to \"true\" in DoseRateEditor.exe.config\"\nShow license agreement?";
                string title = "Varian LULSA";
                var buttons = System.Windows.MessageBoxButton.YesNo;
                var result = MessageBox.Show(msg0, title, buttons);
                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    Process.Start("notepad.exe", Path.Combine(path, "license.txt"));
                }
                
                // Save that they have seen EULA
                config.AppSettings.Settings["EULAAgreed"].Value = "true";
                config.Save(ConfigurationSaveMode.Modified);
                
            }

            // Display opening msg
            string msg = $"The current DoseDynamicArcs application is provided AS IS as a non-clinical, research only tool in evaluation only. The current " +
            $"application will only be available until {exp.Date} after which the application will be unavailable. " +
            "By Clicking 'Yes' you agree that this application will be evaluated and not utilized in providing planning decision support\n\n" +
            "Newer builds with future expiration dates can be found here: https://github.com/Varian-Innovation-Center/MAAS-DoseDynamicArcs\n\n" +
            "See the FAQ for more information on how to remove this pop-up and expiration";

            string msg2 = $"Application will only be available until {exp.Date} after which the application will be unavailable. " +
            "By Clicking 'Yes' you agree that this application will be evaluated and not utilized in providing planning decision support\n\n" +
            "Newer builds with future expiration dates can be found here: https://github.com/Varian-Innovation-Center/MAAS-DoseDynamicArcs\n\n" +
            "See the FAQ for more information on how to remove this pop-up and expiration";

            bool isValidated = config.AppSettings.Settings["Validated"].Value == "true";

            if (!foundNoExpire)
            {
                if (!isValidated)
                {
                    var res = MessageBox.Show(msg, "Agreement  ", MessageBoxButton.YesNo);
                    if (res == MessageBoxResult.No)
                    {
                        return;
                    }
                }
                else if (isValidated)
                {
                    var res = MessageBox.Show(msg2, "Agreement  ", MessageBoxButton.YesNo);
                    if (res == MessageBoxResult.No)
                    {
                        return;
                    }
                }
            }

            // If we make it this far start the app
            start(sender, e);
        }

    }
}
