using MahApps.Metro.IconPacks;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.SessionState;
using System.Windows;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using static DoseRateEditor.Models.Utils;

namespace DoseRateEditor.Models
{
    public class DRCalculator
    {
        
        public delegate double func(double theta);
        public enum DRMethod
        {
            Sin,
            Bhaskara,
            Cosmic
        // Juha
        }

        private static string sincred = @"
Richard A. Popple, Xingen Wu, Ivan A. Brezovich, James M. Markert
, Barton L. Guthrie, Evan M. Thomas, Markus Bredel, John B. Fiveash,
The virtual cone: A novel technique to generate 
spherical dose distributions using
a multileaf collimator and standardized control-point 
sequence for small target radiation surgery,
Advances in Radiation Oncology,
Volume 3, Issue 3,
2018,
Pages 421-430,
ISSN 2452-1094,
https://doi.org/10.1016/j.adro.2018.02.011.
(https://www.sciencedirect.com/science/article/pii/S2452109418300368)" + "\n\nDR(gantry) = Math.Sin((gantry * Math.PI) / 180);";

        private static string bfstring = @"""
public static double BFFunc(double th_deg)
{
    var theta = (th_deg * Math.PI) / 180;
    var retval = 16 * theta * (Math.PI - theta);
    var denom = 5 * Math.PI * Math.PI;
    denom -= 4 * theta * (Math.PI - theta);
    retval /= denom;

    if(th_deg < 180)
    {
        return retval;
    }
    else
    {
        return -1 * BFFunc(th_deg - 180);

    }
}
""";

        private static string cosstring = @"""
public static double cosmicFunc(double th_deg)
{
    if(th_deg < 180) { 
        return (th_deg * (180 - th_deg)) / (90 * 90);
    }
    else
    {
        return cosmicFunc(th_deg - 180);
    }
}
""";
        private static string BF_cred = $"https://digitalcommons.ursinus.edu/cgi/viewcontent.cgi\n?article=1015&context=triumphs_calculus\n\n{bfstring}";
        private static string cosmic_cred = $"https://en.formulasearchengine.com/wiki\n/Small-angle_approximation\n\n{cosstring}";

        private static string juhacred = @"Method and apparatus to deliver therapeutic radiation to a patient using field geography-based dose optimization
            Inventors: Juha Kauppinen Anthony Magliari Martin SABEL Amir Talakoub. 
            (https://patents.google.com/patent/WO2022063684A1)";

        public static Dictionary<DRMethod, string> DRCredits = new Dictionary<DRMethod, string>
        {
            { DRMethod.Sin, sincred },
            { DRMethod.Bhaskara, BF_cred },
            { DRMethod.Cosmic, cosmic_cred }
            //{ DRMethod.Juha, juhacred }
        };

        public static double BFFunc(double th_deg)
        {
            var theta = (th_deg * Math.PI) / 180;
            var retval = 16 * theta * (Math.PI - theta);
            var denom = 5 * Math.PI * Math.PI;
            denom -= 4 * theta * (Math.PI - theta);
            retval /= denom;

            if(th_deg < 180)
            {
                return retval;
            }
            else
            {
                return -1 * BFFunc(th_deg - 180);

            }
        }

        public static double cosmicFunc(double th_deg)
        {
            if(th_deg < 180) { 
                return (th_deg * (180 - th_deg)) / (90 * 90);
            }
            else
            {
                return cosmicFunc(th_deg - 180);
            }
        }


        public string DRCreditsString;
        public Nullable<DRMethod> LastMethodCalculated;

        private static func rad = (theta) => (theta * Math.PI)/180;

        private static func sinfunc = (theta) => Math.Sin(rad(theta));
        private static func BFfunc = (theta) => BFFunc(theta);
        private static func cosmicfunc = (theta) => cosmicFunc(theta);

        private static Dictionary<DRMethod, func> Delagate_dictionary = new Dictionary<DRMethod, func>
        {
            { DRMethod.Sin, sinfunc},
            { DRMethod.Cosmic, cosmicfunc},
            { DRMethod.Bhaskara, BFfunc}
        };

        public Dictionary<string, List<DataPoint>> InitialDRs { get; set; }
        
        public Dictionary<string, List<DataPoint>> InitialGSs { get; set; }

        public Dictionary<string, List<DataPoint>> InitialdMU { get; set; }

        public Dictionary<string, List<DataPoint>> FinalDRs { get; set; }

        public Dictionary<string, List<double>> FinalMSWS { get; set; }

        public Dictionary<string, List<DataPoint>> FinalGSs { get; set; }

        public int numbeams;

        private ExternalPlanSetup Plan { get; set; }
        public DRCalculator(ExternalPlanSetup plan)
        {
            Plan = plan;

            if (plan.Dose == null)
            {
                // Warn user that beam.Meterset will be null
                MessageBox.Show("Warning: Plan dose not calculated, this will lead to innacurate DR and GS calculations.");
            }

            numbeams = Plan.Beams.Count();
            
            // Compute the initial DR for each beam in the plan
            InitialDRs = new Dictionary<string, List<DataPoint>>();
            InitialGSs = new Dictionary<string, List<DataPoint>>();
            InitialdMU = new Dictionary<string, List<DataPoint>>();

            FinalMSWS = new Dictionary<string, List<double>>();
            FinalDRs = new Dictionary<string, List<DataPoint>>();
            FinalGSs = new Dictionary<string, List<DataPoint>>();

            // Get gantry speed max and dr max from machine


            foreach (Beam b in Plan.Beams)
            {
                var maxDR = b.DoseRate;
                var tup = ComputeDRBeam(b, DR_max: maxDR);
                InitialDRs.Add(b.Id, tup.Item1);
                InitialGSs.Add(b.Id, tup.Item2);
                InitialdMU.Add(b.Id, tup.Item3);
            }

            LastMethodCalculated = null; 
            
        }

        public void ClearFinal()
        {
            // Clear all final DR and GS calculations
            // To be called when DR edit method is changed
            FinalGSs.Clear();
            FinalMSWS.Clear();
            FinalDRs.Clear();
        }

        public void CalcFinalDR(ExternalPlanSetup plan, DRMethod method)
        {
            // Clear current results
            FinalGSs.Clear();
            FinalDRs.Clear();
            FinalMSWS.Clear();

            /*if (method == DRMethod.Juha)
            {
                return; // Not implemented yet
            }*/

            // Else compute and save Final DRS and GSs
            foreach(Beam b in Plan.Beams)
            {
                var msws_new = GenerateMSWS(b, Delagate_dictionary[method]);
                FinalMSWS.Add(b.Id, msws_new); // store the msws

                var gantry = new List<double>();
                foreach(var cp in b.GetEditableParameters().ControlPoints)
                {
                    gantry.Add(cp.GantryAngle);
                }

                var dr_gs = ComputeDRFromMSWS(msws_new, b.Meterset.Value, gantry);

                FinalDRs.Add(b.Id, dr_gs.Item1);
                FinalGSs.Add(b.Id, dr_gs.Item2);
            
            }

            // Set variabl LastCalcdMethod to show that we have the DR and GS for a final method
            LastMethodCalculated = method;
            DRCreditsString = DRCredits[method];
        }

        private Tuple<List<DataPoint>, List<DataPoint>> ComputeDRFromMSWS(List<double> msws, double bm_meterset, List<double> gantry_angles, double gantry_speed_max=4.8, double DR_max=2400)
        {
            var DRs = new List<DataPoint>();
            var GSs = new List<DataPoint>();

            double my_mod(double a, int n)
            {
                return a - (Math.Floor(a / n) * n);
            }

            for (int i=1; i < msws.Count; i++)
            {
                var mu_last = msws[i - 1] * bm_meterset;
                var mu_current = msws[i] * bm_meterset;
                var delta_mu = mu_current - mu_last;

                var delta_gantry0 = gantry_angles[i] - gantry_angles[i - 1];
                var delta_gantry = Math.Abs(my_mod((delta_gantry0 + 180), 360) - 180);
                var rot_time = delta_gantry / gantry_speed_max;
                var calcd_mu_rate = (delta_mu * 60) / rot_time;


                if (calcd_mu_rate < DR_max)
                {
                    DRs.Add(new DataPoint(i, calcd_mu_rate));
                    GSs.Add(new DataPoint(i, gantry_speed_max)); // TODO COPY GS CALC LOGIC TO DR0 CALC BELOW
                }
                else
                {
                    DRs.Add(new DataPoint(i, DR_max));
                    var L_i = (bm_meterset / delta_gantry) * (msws[i] - msws[i - 1]);
                    var GS = DR_max / L_i;
                    GS /= 60;
                    GSs.Add(new DataPoint(i, GS));
                }
            }

            return new Tuple<List<DataPoint>, List<DataPoint>>(DRs, GSs);
        }
 
        private List<double> GenerateMSWS (Beam bm, func f)
        {
            var msws = new List<double> { 0 };

            if (bm.IsSetupField) {
                return msws;
            }

            var edits = bm.GetEditableParameters();
            var cps = edits.ControlPoints.ToList();
            var N = cps.Count();
            var dmsw = new List<double>();

            for (int i=1; i<N; i++)
            {
                var gan = (cps[i].GantryAngle + cps[i-1].GantryAngle) / 2;

                double arg = gan;

                var delta = Math.Abs(f(arg));
                
                // Assert delta > 0
                if (delta < 0) {
                    throw new Exception("delta not gt 0!");
                }

                dmsw.Add(delta);
                
            }

            var beta = dmsw.Sum();
            for(int j=0; j<dmsw.Count; j++) 
            { 
                dmsw[j] /= beta; 
            }

            //MessageBox.Show($"Sum of dmsw is {dmsw.Sum()}");

            // build msws
            foreach(var delta in dmsw)
            {
                msws.Add(msws.Last() + delta);
            }

            return msws;

        }

        private Tuple<List<DataPoint>, List<DataPoint>, List<DataPoint>>
            ComputeDRBeam(Beam bm, double gantry_speed_max=4.8, double DR_max=2400) // Calculates current msws given an existing beam
        {

            var DRs = new List<DataPoint>();
            var dMU = new List<DataPoint>();
            var GSs = new List<DataPoint>();

            var cps = bm.GetEditableParameters().ControlPoints.ToList();

            double my_mod(double a, int n)
            {
                return a - Math.Floor(a / n) * n;
            }

            for (int i = 0; i < cps.Count(); i++)
            {
                if (i > 0) // Skip first CP
                {
                    // 1. Calc delta MU
                    var msw_current = cps[i].MetersetWeight;
                    var msw_prev = cps[i - 1].MetersetWeight;

                    var mu_last = msw_prev * bm.Meterset.Value; // TODO: if dose not calculated bm.Meterset is null -> inaccurate DR calculation.
                    var mu_current = msw_current * bm.Meterset.Value;
                    var delta_mu = mu_current - mu_last;

                    // 2. Calc d(MU)/dt from the time it takes to move between cps
                    
                    var delta_gantry0 = cps[i].GantryAngle - cps[i-1].GantryAngle;
                    var delta_gantry = Math.Abs(my_mod((delta_gantry0 + 180), 360) - 180); // corrected for passing 0/360 mark

                    //MessageBox.Show($"{delta_gantry0}, {delta_gantry}");

                    var rotation_time = delta_gantry / gantry_speed_max;
                    var calcd_mu_rate = (delta_mu * 60) / rotation_time;

                    if (calcd_mu_rate < DR_max)
                    {
                        DRs.Add(new DataPoint(i, calcd_mu_rate));
                        GSs.Add(new DataPoint(i, gantry_speed_max));
                    }
                    else
                    {
                        DRs.Add(new DataPoint(i, DR_max));
                        var bm_meterset = bm.Meterset.Value;
                        var L_i = (bm_meterset / delta_gantry) * (msw_current - msw_prev);
                        var GS = DR_max / L_i;
                        GS /= 60;
                        GSs.Add(new DataPoint(i, GS));
                    }
                    dMU.Add(new DataPoint(i, delta_mu));

                }
            }

            return new Tuple<List<DataPoint>, List<DataPoint>, List<DataPoint>>(DRs, GSs, dMU);
        }
    
       

        private bool CheckIsClosed(Beam bm)
        {
            // Get the cps from beam
            var edits = bm.GetEditableParameters();
            var cps = edits.ControlPoints;

            foreach (var cp in cps)
            {
                var leaves = cp.LeafPositions;
                for(int i=0; i < 60; i++)
                {
                    var bankA = leaves[0, i];
                    var bankB = leaves[1, i];

                    if (bankA + bankB != 0)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private void AddGap(Beam bm)
        {
            // Get the cps from beam
            var edits = bm.GetEditableParameters();
            var cps = edits.ControlPoints;

            foreach (var cp in cps)
            {
                var leaves = cp.LeafPositions;
                
                leaves[0, 30] = 0.105F;
                leaves[1, 30] = 0.105F;

                leaves[0, 31] = 0.105F;
                leaves[1, 31] = 0.105F;
            }

            bm.ApplyParameters(edits);
        }

        private Tuple<ExternalPlanSetup, bool> ConvertToDynamic(ExternalPlanSetup plan, Course newcourse)
        {
            var newplan = newcourse.CopyPlanSetup(plan) as ExternalPlanSetup;
            newplan.Id = "temp_dynamic";

            int replaceCount = 0;
            bool agreeConvert = false;
            // Loop through each field in the plan and copy it to dynamic version

            // Make list of beams we want to change
            var to_modify = newplan.Beams.Where(b => !b.IsSetupField).ToList();

            foreach(var bm in to_modify)
            {

                // Get info about the beam
                var angles = Utils.GetBeamAngles(bm);

                var gantry_angles = angles.Item1;
                var col_angles = angles.Item2;
                var couch_angles = angles.Item3;
                var cps = angles.Item4;
             
                // Check if static
                if (cps.Count() <= 2)
                {
                    if (!agreeConvert)
                    {
                        var res = MessageBox.Show($"Must convert all fields from static to dynamic to edit dose rate, would you like to continue?");
                        if (res == MessageBoxResult.Cancel)
                        {
                            return new Tuple<ExternalPlanSetup, bool> (newplan, false);
                        }

                        agreeConvert = res == MessageBoxResult.OK;
                    }

                    // Handle static
                    // Create arc beam
                    (string primary_fluence_mode, string energy_mode_id) = GetFluenceEnergyMode(bm);

                    var ebmp = new ExternalBeamMachineParameters(
                        bm.TreatmentUnit.Id,
                        energy_mode_id,
                        bm.DoseRate,
                        "ARC",
                        primary_fluence_mode
                    );


                    var d_theta = 180 - Math.Abs(Math.Abs(gantry_angles.First() - gantry_angles.Last()) - 180);
                    if (bm.GantryDirection == GantryDirection.Clockwise)
                    {
                        d_theta = 360 - d_theta;
                    }
                    int n_cps = (int)Math.Ceiling(d_theta / 2) + 1;

                    var new_bm = newplan.AddConformalArcBeam(
                        ebmp,
                        col_angles.First(),
                        n_cps,
                        gantry_angles.First(),
                        gantry_angles.Last(),
                        bm.GantryDirection,
                        couch_angles.First(),
                        bm.IsocenterPosition
                    );

                    // Now copy mlc positions of bm to new_bm
                    var target_mlc = cps.First().LeafPositions;
                    var target_jaws = cps.First().JawPositions;

                    var edits_new = new_bm.GetEditableParameters();
                    foreach (var cp in edits_new.ControlPoints)
                    {
                        cp.LeafPositions = target_mlc; // Copy over mlc 
                        cp.JawPositions = target_jaws;
                    }

                    new_bm.ApplyParameters(edits_new);
                    replaceCount++;
                }
            }

            foreach(var bm in to_modify)
            {
                newplan.RemoveBeam(bm);
            }

            if(replaceCount> 0)
            {
                MessageBox.Show($"Converted {replaceCount} beams to dynamic arcs");
            }
            return new Tuple<ExternalPlanSetup, bool> (newplan, true) ;

        }

        private Tuple<string, string> GetFluenceEnergyMode(Beam bm)
        {
            // Lifted from my python code @ craman96/MAAS
            var energy_mode_splits = bm.EnergyModeDisplayName.Split('-');

            var energy_mode_id = energy_mode_splits[0];

            var primary_fluence_mode = "";
            if (energy_mode_splits.Length > 1)
            {
                primary_fluence_mode = energy_mode_splits[1];
            }

            return new Tuple<string, string> (primary_fluence_mode, energy_mode_id);
        }

        public void CreateNewPlanWithMethod(DRMethod method) // TODO fix deletion within loop crash by tagging all beams to delete (somehow) and then deleing them after loop
        {
            // Copy the plan, delete all beams
            // Call begin mods
            var pat = Plan.Course.Patient;
            pat.BeginModifications();

            // Create new course
            var newcourse = pat.AddCourse();
            var dt = DateTime.UtcNow.ToString("d");

            // Anthony fix for static mlc
            var newplan = newcourse.CopyPlanSetup(Plan) as ExternalPlanSetup;
            newplan.Id = Plan.Id;

            // Helper for copying beam
            void copy_beam(Beam bm, List<double> msws, bool delete_original=false, ExternalPlanSetup new_plan=null)
            {
                

                (string primary_fluence_mode, string energy_mode_id) = GetFluenceEnergyMode(bm);

                // ASSERT
                if (!new String[] {"", "FFF", "SRS"}.Contains(primary_fluence_mode))
                {
                    throw new Exception($"Primary fluence mode {primary_fluence_mode} not one of the valid options");
                }

                var angles = Utils.GetBeamAngles(bm);

                var gantry_angles = angles.Item1;
                var col_angles = angles.Item2;
                var couch_angles = angles.Item3;
                var cps = angles.Item4;

                /*
                var technique_id = "STATIC";
                for (int i = 1; i < cps.Count(); i ++)
                {
                    if (gantry_angles[i] != gantry_angles[i - 1])
                    {
                        technique_id = "ARC";
                        break;
                    }

                }*/

                var ebmp = new ExternalBeamMachineParameters(
                    bm.TreatmentUnit.Id,
                    energy_mode_id,
                    bm.DoseRate,
                    "ARC",
                    primary_fluence_mode
                );

                ExternalPlanSetup plan = null;
                if (new_plan == null)
                {
                    plan = (ExternalPlanSetup)bm.Plan;
                }
                else
                {
                    plan = new_plan;
                }


                var new_bm = plan.AddVMATBeam(
                    ebmp,
                    msws,
                    col_angles.First(),
                    gantry_angles.First(),
                    gantry_angles.Last(),
                    bm.GantryDirection,
                    couch_angles.First(),
                    bm.IsocenterPosition
                    );

                var edits_new = new_bm.GetEditableParameters();
                var cps_new = edits_new.ControlPoints.ToList();

                for (int j = 0; j < cps_new.Count(); j++)
                {
                    cps_new[j].JawPositions = cps[j].JawPositions;
                    cps_new[j].LeafPositions = cps[j].LeafPositions;
                }

                new_bm.ApplyParameters(edits_new);
                new_bm.Id = bm.Id +"_new"; // Truncate and add 'new' to the name

                // Delete original beam if it's called for
                if (delete_original)
                {
                    var orig_plan = bm.Plan as ExternalPlanSetup;
                    orig_plan.RemoveBeam(bm);
                }

            }


            

            // Check static or dynamic (does the mlc have more than 2 control points)
            // If static convert to dynamic (copy pattern to every cp) (if dynamic do nothing)
            (var dynPlan, var success) = ConvertToDynamic(Plan, newcourse);
            Plan = dynPlan;
            if (!success)
            {
                return; // Can't perform dr edit on a static plan
            }


            // -- Check closed --
            /*
            List<bool> beamsClosed = new List<bool>();
            foreach(var bm in Plan.Beams.Where(b => !b.IsSetupField).ToList())
            {
                beamsClosed.Add(CheckIsClosed(bm));
            }

            var count_closed = beamsClosed.Where(val => val == true).Count();
            
            if (count_closed == beamsClosed.Count)
            {
                // All closed
                var res = MessageBox.Show("All fields have closed MLC, would you like to create 2.1mm opening in center 2 leaf pairs?", "Closed MLC", MessageBoxButton.YesNo);
                if(res == MessageBoxResult.Yes)
                {
                    foreach(var bm in Plan.Beams.Where(b => !b.IsSetupField).ToList())
                    {
                        AddGap(bm);
                    }
                }
                else
                {
                    return; // Call the whole thing off
                }
            }
            else if (count_closed > 0)
            {
                // Some closed (warning)
                MessageBox.Show("Some arcs in this plan have a closed MLC. Please try again with an aperture on all fields or a plan with all MLCs closed.");
                return; 
            }*/



            // Compute the final DR using selected method
            CalcFinalDR(Plan, method);


            // Rename course
            string proposed_name;
            for(int n = 1; n < 100; n++)
            {
                proposed_name = $"EditDR_{n}";
                if (pat.Courses.Count(c => c.Id == proposed_name) == 0) // If we don't find that name
                {
                    // use proposed name and break the loop
                    newcourse.Id = proposed_name;
                    break;  
                }else if (n == 99) {
                    throw new Exception("Maximum new course index reached (99)");
                }
           
            }

            
            // TODO remove the beams from newplan
            foreach (var copiedbeam in newplan.Beams.ToList())
            {
                newplan.RemoveBeam(copiedbeam);
            }

            // Loop through each beam and copy it with new msws edit the msws
            foreach (var bm in Plan.Beams)
            {
                var new_msws = FinalMSWS[bm.Id];

                copy_beam(bm, new_msws, false, newplan);
            }

            

            MessageBox.Show(
                $"New plan created with id: {newplan.Id} in course {newcourse.Id}" +
                $"\nExit Dose Rate Editor and reload patient in Eclipse " +
                $"to select new course and view plan");

            
        }
    }
}
