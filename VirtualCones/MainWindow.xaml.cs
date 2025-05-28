using VirtualCones_MCB.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
using System.Xml.Linq;
using VMS.TPS.Common.Model.API;

namespace VirtualCones_MCB
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        VMS.TPS.Common.Model.API.Application _app;
        VMS.TPS.Common.Model.API.Patient _pat;
        VMS.TPS.Common.Model.API.Course _course;
        VMS.TPS.Common.Model.API.PlanSetup _pln;
        IEnumerable<string> _list;

        MainWindowViewModel vm;

        private string _validationWarning = string.Empty;

        public string ValidationWarning
        {
            get { return _validationWarning; }
            set
            {
                _validationWarning = value;
                OnPropertyChanged("ValidationWarning");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public MainWindow(VMS.TPS.Common.Model.API.Application app, string[] args)
        {
            string computerName = Environment.MachineName;
            string parentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Handle command-line arguments if provided
            if (args != null && args.Length > 3)
            {
                parentDirectory = args[3];
            }

            MainWindowViewModel.ParentDirectory = parentDirectory;

            vm = new MainWindowViewModel();
            vm._esapiX = app;
            DataContext = vm;
            _app = app;

            InitializeComponent();

            // Check validation status
            CheckValidationStatus();

            this.Closing += MainWindow_Closing;

            // Handle patient/plan selection arguments if provided
            if (args != null && args.Length >= 4)
            {
                vm.SearchText = args[0];
                vm.UpdateFilteredPatientIDs();
                vm.PatientID = args[0];
                vm.CourseId = args[1];
                vm.PlanId = args[2];
                vm.PtCBOOpen = false;
            }

            if (computerName.Equals("CTSI-CLIENT16-2"))
            {
                vm.TestPatientVisibility = Visibility.Visible;
            }
            else
            {
                vm.TestPatientVisibility = Visibility.Collapsed;
            }
        }

        private void CheckValidationStatus()
        {
            try
            {
                var config = new AppConfig(Assembly.GetExecutingAssembly().Location);
                var validationSetting = config["softwareValidated"];

                // Default to showing the warning
                ValidationWarning = "*** NOT VALIDATED FOR CLINICAL USE ***";

                // If validation is explicitly set to "true", clear the warning
                if (!string.IsNullOrEmpty(validationSetting) &&
                    validationSetting.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    ValidationWarning = string.Empty;
                }
            }
            catch (Exception)
            {
                // Default to showing the warning on error
                ValidationWarning = "*** NOT VALIDATED FOR CLINICAL USE ***";
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                });
            }
            catch
            {
                MessageBox.Show("Could not open license URL.");
            }
            e.Handled = true;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            vm._esapiX.Dispose();

            //Close main Talos
            var process = Process.GetCurrentProcess();
            process.Kill();
        }

      }

    public class AppConfig
    {
        private readonly IDictionary<string, string> m_appSettings;

        /// <summary>
        /// Constructor for AppConfig with path to config file
        /// </summary>
        /// <param name="executingAssemblyLocation">Path of the executing assembly location</param>
        public AppConfig(string executingAssemblyLocation)
        {
            if (string.IsNullOrWhiteSpace(executingAssemblyLocation))
            {
                throw new ArgumentNullException();
            }

            var configPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(executingAssemblyLocation) ?? string.Empty,
                                         $"{System.IO.Path.GetFileName(executingAssemblyLocation)}.config");

            var doc = XDocument.Load(configPath);
            m_appSettings = doc
                .Descendants("configuration")
                .Descendants("appSettings")
                .Descendants()
                .ToDictionary(
                    xElement => xElement.Attribute("key")?.Value ?? string.Empty,
                    xElement => xElement.Attribute("value")?.Value ?? string.Empty);
        }

        /// <summary>
        /// Reads a config value by name.
        /// </summary>
        /// <param name="name">The name of the appSettings value</param>
        /// <returns>The config value</returns>
        public string this[string name]
        {
            get
            {
                var appSettingValue = m_appSettings.FirstOrDefault(kvp => kvp.Key == name);
                return appSettingValue.Equals(default(KeyValuePair<string, string>))
                       || appSettingValue.Equals(new KeyValuePair<string, string>(string.Empty, string.Empty))
                    ? null
                    : appSettingValue.Value;
            }
        }
    }
}