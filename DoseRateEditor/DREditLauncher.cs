﻿using System.Windows;
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

                // Note: PlanSetup is optional for DoseRateEditor, so we don't validate it as required

                //Prepare and launch application
                string launcherPath = Path.GetDirectoryName(GetSourceFilePath());
                string esapiStandaloneExecutable = @"DoseRateEditor.exe";
                
                // Constructs the arguments for the executable (conditional based on PlanSetup availability)
                string arguments = context.PlanSetup == null
                                    ? string.Format("\"{0};{1};\"", context.Patient.Id, context.Course.Id)
                                    : string.Format("\"{0};{1};{2}\"", context.Patient.Id, context.Course.Id, context.PlanSetup.Id);

                // Validates the executable path
                string executablePath = Path.Combine(launcherPath, esapiStandaloneExecutable);
                if (!File.Exists(executablePath))
                {
                    MessageBox.Show(string.Format("Error: The executable '{0}' was not found at '{1}'.", esapiStandaloneExecutable, launcherPath),
                                    "Executable Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Starts the process
                Process process = Process.Start(executablePath, arguments);
                if (process == null)
                {
                    throw new ApplicationException("Failed to start the DoseRateEditor application process.");
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
