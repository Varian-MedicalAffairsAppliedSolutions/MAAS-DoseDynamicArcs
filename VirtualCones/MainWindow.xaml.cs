using AOS_VirtualCones_MCB.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
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
using VMS.TPS.Common.Model.API;

namespace AOS_VirtualCones_MCB
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        VMS.TPS.Common.Model.API.Application _app;
        VMS.TPS.Common.Model.API.Patient _pat;
        VMS.TPS.Common.Model.API.Course _course;
        VMS.TPS.Common.Model.API.PlanSetup _pln;
        IEnumerable<string> _list;

        MainWindowViewModel vm;

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

            if(computerName.Equals("CTSI-CLIENT16-2"))
            {
                vm.TestPatientVisibility = Visibility.Visible;
            }
            else
            {
                vm.TestPatientVisibility = Visibility.Collapsed;
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            vm._esapiX.Dispose();

            //Close main Talos
            var process = Process.GetCurrentProcess();
            process.Kill();
        }
    }
}






