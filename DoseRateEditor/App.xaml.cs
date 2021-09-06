using Autofac;
using DoseRateEditor.Startup;
using DoseRateEditor.ViewModels;
using DoseRateEditor.Views;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using VMS.TPS.Common.Model.API;

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

        private void Application_Startup(object sender, StartupEventArgs e)

        {

            try

            {

                var provider = new CultureInfo("en-US");

                DateTime endDate = DateTime.Now;

                if (DateTime.TryParse("1/30/2022", provider, DateTimeStyles.None, out endDate))

                {

                    if (DateTime.Now <= endDate)

                    {

                        string msg = $"The current planscorecard application is provided AS IS as a non-clinical, research only tool in evaluation only. The current " +

                            $"application will only be available until {endDate.Date} after which the application will be unavailable." +

                            $"By Clicking 'Yes' you agree that this application will be evaluated and not utilized in providing planning decision support";

                        if (MessageBox.Show(msg, "Agreement  ", MessageBoxButton.YesNo) == MessageBoxResult.Yes)

                        {

                            using (_app = VMS.TPS.Common.Model.API.Application.CreateApplication())

                            {


                                var bootstrap = new Bootstrapper();

                                var container = bootstrap.Bootstrap(_app.CurrentUser, _app);

                                MV = container.Resolve<MainView>();

                                MV.DataContext = container.Resolve<MainViewModel>();

                                MV.ShowDialog();

                                _app.ClosePatient();

                            }

                        }



                    }

                    else

                    {

                        MessageBox.Show("Application expiration date has been surpassed.");

                    }

                }

            }

            catch (Exception ex)

            {

                //throw new ApplicationException(ex.Message);

                if (ConfigurationManager.AppSettings["Debug"] == "true")

                {

                    MessageBox.Show(ex.ToString());

                }

                //_app.ClosePatient();

                _app.Dispose();

                App.Current.Shutdown();

            }

        }

    }
}
