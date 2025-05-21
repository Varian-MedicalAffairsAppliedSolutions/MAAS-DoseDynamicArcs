using System.Windows;
using System.Runtime.CompilerServices;
using VMS.TPS.Common.Model.API;
using System.Diagnostics;
using System.IO;
using System;

namespace VMS.TPS
{
    public class Script
    {
        public Script()
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Execute(ScriptContext context)
        {
            try
            {
                if (context.PlanSetup != null)
                {
                    string launcherPath = Path.GetDirectoryName(GetSourceFilePath());
                    string esapiStandaloneExecutable = @"AOS_VirtualCones.exe";
                    string arguments = context.PlanSetup == null
                                        ? string.Empty
                                        : string.Format("\"{0}\"\"{1}\"\"{2}\"\"{3}\"", context.Patient.Id, context.Course.Id, context.PlanSetup.Id, launcherPath);
                    Process.Start(Path.Combine(launcherPath, esapiStandaloneExecutable), arguments);
                }
                else
                {
                    MessageBox.Show("Please load a plan into the context.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                
            }
            catch (Exception)
            {
                MessageBox.Show("Failed to start application.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public string GetSourceFilePath([CallerFilePath] string sourceFilePath = "")
        {
            return sourceFilePath;
        }
    }
}