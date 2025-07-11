using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.Diagnostics;
using System.IO;

namespace VMS.TPS
{
  public class Script
  {
    public Script()
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Execute(ScriptContext context /*, System.Windows.Window window, ScriptEnvironment environment*/)
    {
        try
        {
            //Context validation checks
            if (context.Patient == null)
            {
                MessageBox.Show("Error: No patient is loaded. Please open a patient before running this script.",
                                "Patient Context Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (context.Course == null)
            {
                MessageBox.Show("Error: No course is loaded. Please open a course before running this script.",
                                "Course Context Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (context.PlanSetup == null)
            {
                MessageBox.Show("Error: No plan is loaded. Please open a plan before running this script.",
                                "Plan Context Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //Prepare and launch application
            string launcherPath = Path.GetDirectoryName(GetSourceFilePath());
            string esapiStandaloneExecutable = @"AOS_VirtualCones.exe";
            string executablePath = Path.Combine(launcherPath, esapiStandaloneExecutable);

            // Validates the executable path
            if (!File.Exists(executablePath))
            {
                MessageBox.Show(string.Format("Error: The executable '{0}' was not found at '{1}'.", esapiStandaloneExecutable, launcherPath),
                                "Executable Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Setup process start info with arguments
            ProcessStartInfo startInfo = new ProcessStartInfo(executablePath);
            startInfo.Arguments = "\"" + context.PlanSetup.Course.Patient.Id + "\"" + " " + "\"" +
                    context.PlanSetup.Course.Id + "\"" + " " + "\"" + context.PlanSetup.Id + "\"" + " " + "\"" + launcherPath + "\"";
            
            // Starts the process
            Process process = Process.Start(startInfo);
            if (process == null)
            {
                throw new ApplicationException("Failed to start the AOS_VirtualCones application process.");
            }
        }
        catch (ApplicationException appEx)
        {
            MessageBox.Show(string.Format("Application Error: {0}", appEx.Message), "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(string.Format("Unexpected Error: {0}", ex.Message), "Unexpected Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public string GetSourceFilePath([CallerFilePath] string sourceFilePath = "")
    {
        return sourceFilePath;
    }
  }
}
