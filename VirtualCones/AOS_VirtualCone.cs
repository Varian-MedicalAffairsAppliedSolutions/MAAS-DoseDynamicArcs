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
        // insert path to run here. This way it doesn't have to be local to the root
        string path = @"C:\Users\chris.brewer\source\repos\VirtualCones_MCB\bin\Debug\AOS_VirtualCones.exe";
        string directoryPath = Path.GetDirectoryName(path);
        ProcessStartInfo startInfo = new ProcessStartInfo(path);
        
        if (context.PlanSetup != null)
        {
            startInfo.Arguments = "\"" + context.PlanSetup.Course.Patient.Id + "\"" + " " + "\"" +
                    context.PlanSetup.Course.Id + "\"" + " " + "\"" + context.PlanSetup.Id + "\"" + " " + "\"" + directoryPath +"\"";
            Process.Start(startInfo);
        }
        else
        {
                MessageBox.Show("Please load a plan into the context.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
        }

    }
  }
}
