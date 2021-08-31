using Autofac;
using DoseRateEditor.ViewModels;
using DoseRateEditor.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;

namespace DoseRateEditor.Startup
{
    public class Bootstrapper
    {
        public IContainer Bootstrap(User user, Application app)
        {
            var container = new ContainerBuilder();
            //esapi components.            
            container.RegisterInstance<User>(user);
            container.RegisterInstance<Application>(app);

            //startup components.
            container.RegisterType<MainView>().AsSelf();
            container.RegisterType<MainViewModel>().AsSelf();

            return container.Build();
        }
    }
}
