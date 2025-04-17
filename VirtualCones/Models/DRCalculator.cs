using OxyPlot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace AOS_VirtualCones_MCB.Models
{
    public class DRCalculator
    {


        private static int initNone = 0;


        public Dictionary<string, List<DataPoint>> InitialDRs { get; set; }

        public Dictionary<string, List<DataPoint>> InitialGSs { get; set; }

        public Dictionary<string, List<DataPoint>> InitialdMU { get; set; }

        public Dictionary<string, List<double>> FinalMSWS { get; set; }


        public int numbeams;

        private VMS.TPS.Common.Model.API.Application _app { get; set; }

        private ExternalPlanSetup Plan { get; set; }


        public DRCalculator(ExternalPlanSetup plan, VMS.TPS.Common.Model.API.Application app, List<BeamInfo> beamInfos)
        {
            _app = app;
            Plan = plan;

            numbeams = Plan.Beams.Count();

            // Compute the initial DR for each beam in the plan
            InitialDRs = new Dictionary<string, List<DataPoint>>();
            InitialGSs = new Dictionary<string, List<DataPoint>>();
            InitialdMU = new Dictionary<string, List<DataPoint>>();

            FinalMSWS = new Dictionary<string, List<double>>();

            // Get gantry speed max and dr max from machine

            foreach (Beam b in Plan.Beams)
            {
                var maxDR = b.DoseRate;
                var tup = ComputeDRBeam(b, DR_max: maxDR);
                InitialDRs.Add(b.Id, tup.Item1);
                InitialGSs.Add(b.Id, tup.Item2);
                InitialdMU.Add(b.Id, tup.Item3);
            }

        }

        public void ClearFinal()
        {
            // Clear all final DR and GS calculations
            // To be called when DR edit method is changed
            FinalMSWS.Clear();
        }

        public void CalcFinalDR(ExternalPlanSetup plan, List<BeamInfo> BeamInfos)
        {


            // Else compute and save Final DRS and GSs
            foreach (Beam b in Plan.Beams)
            {
                var bi = BeamInfos.Single(x => x.BeamID.ToUpper().Equals(b.Id.ToUpper()));                

                var msws_new = GenerateMSWS(b,bi.Map);
                FinalMSWS.Add(b.Id, msws_new);

                var gantry = new List<double>();
                foreach (var cp in b.GetEditableParameters().ControlPoints)
                {
                    gantry.Add(cp.GantryAngle);
                }

                var dr_gs = ComputeDRFromMSWS(msws_new, b.Meterset.Value, gantry);
            }


        }

        private Tuple<List<DataPoint>, List<DataPoint>> ComputeDRFromMSWS(List<double> msws, double bm_meterset, List<double> gantry_angles, double gantry_speed_max = 6, double DR_max = 2400)
        {
            var DRs = new List<DataPoint>();
            var GSs = new List<DataPoint>();

            double my_mod(double a, int n)
            {
                return a - (Math.Floor(a / n) * n);
            }

            for (int i = 1; i < msws.Count; i++)
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

        private List<double> GenerateMSWS(Beam bm, GantryWeightMap DoseRateMap)
        {
            initNone = 0;
            var msws = new List<double> { 0 };

            if (bm.IsSetupField)
            {
                return msws;
            }

            var cps = bm.GetEditableParameters().ControlPoints.ToList();
            var N = cps.Count();
            var dmsw = new List<double>();

            for (int i = 1; i < N; i++)
            {
                var gan = (cps[i].GantryAngle + cps[i - 1].GantryAngle) / 2;

                double delta=-1;



                delta = GetInterpolatedWeight(DoseRateMap, gan);



                if (delta < 0)
                {
                    throw new Exception("delta not greater than 0!");
                }

                dmsw.Add(delta);
            }

            var beta = dmsw.Sum();
            for (int j = 0; j < dmsw.Count; j++)
            {
                dmsw[j] /= beta;
            }

            foreach (var delta in dmsw)
            {
                msws.Add(msws.Last() + delta);
            }

            return msws;
        }

        private Tuple<List<DataPoint>, List<DataPoint>, List<DataPoint>>
            ComputeDRBeam(Beam bm, double gantry_speed_max = 6, double DR_max = 2400) // Calculates current msws given an existing beam
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

                    if (bm.Meterset.Value <= 0)
                    {
                        MessageBox.Show($"Warning: meterset value is {bm.Meterset.Value}");
                    }

                    var mu_last = msw_prev * bm.Meterset.Value; // TODO: if dose not calculated bm.Meterset is null -> inaccurate DR calculation.
                    var mu_current = msw_current * bm.Meterset.Value;
                    var delta_mu = mu_current - mu_last;

                    // 2. Calc d(MU)/dt from the time it takes to move between cps

                    var delta_gantry0 = cps[i].GantryAngle - cps[i - 1].GantryAngle;
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


        public static double GetInterpolatedWeight(GantryWeightMap map, double gantry)
        {
            // Ensure map has at least two points for interpolation
            if (map.pairs.Count < 2)
                throw new InvalidOperationException("The GantryWeightMap must contain at least two points for interpolation.");

            // Sort pairs by Gantry to ensure correct interpolation
            var sortedPairs = map.pairs.OrderBy(p => p.Gantry).ToList();

            // If the gantry is exactly one of the map points, return its weight
            var exactMatch = sortedPairs.FirstOrDefault(p => Math.Abs(p.Gantry - gantry) < 1e-6);
            if (exactMatch != null)
                return exactMatch.Weight;

            // Find the two points closest to the input gantry
            GantryWeightPair lower = null, upper = null;
            for (int i = 0; i < sortedPairs.Count - 1; i++)
            {
                if (sortedPairs[i].Gantry <= gantry && sortedPairs[i + 1].Gantry >= gantry)
                {
                    lower = sortedPairs[i];
                    upper = sortedPairs[i + 1];
                    break;
                }
            }

            // Handle edge cases: if gantry is outside the range, use the closest boundary values
            if (lower == null || upper == null)
            {
                lower = sortedPairs.First();
                upper = sortedPairs.Last();
                if (gantry < lower.Gantry)
                    return lower.Weight;
                if (gantry > upper.Gantry)
                    return upper.Weight;
            }

            // Linear interpolation
            double interpolatedWeight = lower.Weight +
                ((gantry - lower.Gantry) / (upper.Gantry - lower.Gantry)) * (upper.Weight - lower.Weight);

            return interpolatedWeight;
        }
        private bool CheckIsClosed(Beam bm)
        {
            // Get the cps from beam
            var edits = bm.GetEditableParameters();
            var cps = edits.ControlPoints;

            foreach (var cp in cps)
            {
                var leaves = cp.LeafPositions;
                for (int i = 0; i < 60; i++)
                {
                    var bankA = leaves[0, i];
                    var bankB = leaves[1, i];

                    if (bankA - bankB != 0)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private void AddGap(Beam bm)
        {
            // Build leaf bank
            var leaves = new float[2, 60];
            for (int i = 0; i < 60; i++)
            {
                leaves[0, i] = -37.5F;
                leaves[1, i] = -37.5F;
            }
            leaves[0, 30] = -1.05F;
            leaves[1, 30] = 1.05F;
            leaves[0, 29] = -1.05F;
            leaves[1, 29] = 1.05F;

            // Get the cps from beam
            var edits = bm.GetEditableParameters();
            edits.SetAllLeafPositions(leaves);
            bm.ApplyParameters(edits);
        }

        private Tuple<ExternalPlanSetup, bool> ConvertToDynamic(ExternalPlanSetup plan, Course newcourse, bool toVmat = true)
        {
            var newplan = newcourse.CopyPlanSetup(plan) as ExternalPlanSetup;

            string candidatePlanId = "DNU";
            int inc = 0;
            while (newcourse.PlanSetups.Select(x => x.Id.ToUpper()).Count(x => x.ToUpper().Equals(candidatePlanId.ToUpper())) > 0)
            {
                candidatePlanId = "DNU" + inc.ToString();
                inc++;
            }

            newplan.Id = candidatePlanId;

            int replaceCount = 0;
            bool agreeConvert = false;
            // Loop through each field in the plan and copy it to dynamic version

            // Make list of beams we want to change
            var to_modify = newplan.Beams.Where(b => !b.IsSetupField).ToList();

            foreach (var bm in to_modify)
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
                            return new Tuple<ExternalPlanSetup, bool>(newplan, false);
                        }

                        agreeConvert = res == MessageBoxResult.OK;
                    }

                    // Handle static
                    // Create arc beam
                    var unpack = GetFluenceEnergyMode(bm);
                    string primary_fluence_mode = unpack.Item1;
                    string energy_mode_id = unpack.Item2;

                    var ebmp = new ExternalBeamMachineParameters(
                        bm.TreatmentUnit.Id,
                        energy_mode_id,
                        bm.DoseRate,
                        "SRS ARC",
                        primary_fluence_mode
                    );

                    var d_theta = 180 - Math.Abs(Math.Abs(gantry_angles.First() - gantry_angles.Last()) - 180);
                    if (bm.GantryDirection == GantryDirection.Clockwise)
                    {
                        d_theta = 360 - d_theta;
                    }
                    int n_cps = (int)Math.Ceiling(d_theta / 2) + 1;

                    Beam new_bm = null;
                    if (!toVmat)
                    {
                        // Conf arc
                        new_bm = newplan.AddConformalArcBeam(
                            ebmp,
                            col_angles.First(),
                            n_cps,
                            gantry_angles.First(),
                            gantry_angles.Last(),
                            bm.GantryDirection,
                            couch_angles.First(),
                            bm.IsocenterPosition
                        );
                    }
                    else
                    {
                        // MSWS
                        var msws = new List<double>();
                        for (int i = 0; i < 100; i++)
                        {
                            // Make 100 MSWS for now
                            msws.Add((double)i);
                        }
                        var max_msw = msws.Max();
                        for (int j = 0; j < msws.Count(); j++)
                        {
                            msws[j] /= max_msw;
                        }

                        // VMAT
                        new_bm = newplan.AddVMATBeam(
                            ebmp,
                            msws,
                            col_angles.First(),
                            gantry_angles.First(),
                            gantry_angles.Last(),
                            bm.GantryDirection,
                            couch_angles.First(),
                            bm.IsocenterPosition
                        );
                    }

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

                    // If agree convert save original beam id and rename as old, then rename new beam
                    if (agreeConvert)
                    {
                        var orig_id = bm.Id;
                        bm.Id = bm.Id + "d";
                        new_bm.Id = orig_id;
                    }

                    replaceCount++;
                }
            }

            if (agreeConvert)
            {
                foreach (var bm in to_modify)
                {
                    newplan.RemoveBeam(bm);
                }
            }

            if (replaceCount > 0)
            {
                MessageBox.Show($"Converted {replaceCount} beams to dynamic arcs");
            }
            return new Tuple<ExternalPlanSetup, bool>(newplan, true);
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

            return new Tuple<string, string>(primary_fluence_mode, energy_mode_id);
        }

        // Helper for copying beam
        private void copy_beam(Beam bm, List<double> msws, bool delete_original = false, ExternalPlanSetup new_plan = null)
        {
            //(string primary_fluence_mode, string energy_mode_id) = GetFluenceEnergyMode(bm);
            var unpack_getFluenceEnergyMode = GetFluenceEnergyMode(bm);
            string primary_fluence_mode = unpack_getFluenceEnergyMode.Item1;
            string energy_mode_id = unpack_getFluenceEnergyMode.Item2;

            // ASSERT
            if (!new String[] { "", "FFF", "SRS" }.Contains(primary_fluence_mode))
            {
                throw new Exception($"Primary fluence mode {primary_fluence_mode} not one of the valid options");
            }

            var angles = Utils.GetBeamAngles(bm);

            var gantry_angles = angles.Item1;
            var col_angles = angles.Item2;
            var couch_angles = angles.Item3;
            var cps = angles.Item4;

            var ebmp = new ExternalBeamMachineParameters(
                bm.TreatmentUnit.Id,
                energy_mode_id,
                bm.DoseRate,
                "SRS ARC",
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

            //// Write msws to a text file
            //string fileName = $"{bm.Id}.txt";
            //string filePath = Path.Combine(Environment.CurrentDirectory, fileName);
            //File.WriteAllLines(filePath, msws.Select(x => x.ToString()));

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
            new_bm.Id = bm.Id; //+ "_new"; // Truncate and add 'new' to the name

            // Delete original beam if it's called for
            if (delete_original)
            {
                var orig_plan = bm.Plan as ExternalPlanSetup;
                orig_plan.RemoveBeam(bm);
            }
        }

       

        private bool CheckValidMLC(IEnumerable<Beam> beams)
        {
            var hdstring = "Varian High Definition 120";
            var milstring = "Varian Millennium 120";
            var milstring2 = "Millennium 120";
            foreach (var bm in beams.Where(b => !b.IsSetupField).ToList())
            {
                var mlc = bm.MLC;
                if (mlc.Model != hdstring && mlc.Model != milstring && mlc.Model != milstring2)
                {
                    MessageBox.Show($"Invalid MLC model: {mlc.Model}. DREditor only designed for {hdstring} or {milstring} or {milstring2}.");
                    return false;
                }
            }

            return true;
        }

        private bool CheckClosed(Patient pat, ExternalPlanSetup plan)
        {
            // -- Check closed to set target mlc
            List<bool> beamsClosed = new List<bool>();
            var beams = plan.Beams.Where(b => !b.IsSetupField).ToList();
            foreach (var bm in beams)
            {
                var isclosed = CheckIsClosed(bm);
                beamsClosed.Add(isclosed);
            }

            // Check how many beams are closed
            var count_closed = beamsClosed.Where(val => val == true).Count();

            // If all are closed and leaves are HD, give option to create gap
            if (count_closed == beamsClosed.Count)
            {
                if (beams.First().MLC.Model == "Varian High Definition 120")
                {
                    // All closed on HD machine, propose gap
                    var res = MessageBox.Show("All fields have closed MLC, would you like to create 2.1mm opening in center 2 leaf pairs?", "Closed HD MLC", MessageBoxButton.YesNo);
                    if (res == MessageBoxResult.Yes)
                    {
                        pat.BeginModifications();
                        foreach (var bm in plan.Beams.ToList())
                        {
                            AddGap(bm);
                        }
                        return false;
                    }
                    return true;
                }
                else
                {
                    MessageBox.Show("Exiting because all fields have closed MLCs. Plese use HD-MLC or create aperture with Millenium 120 MLC.");
                    return true; // Call the whole thing off
                }
            }
            else if (count_closed > 0)
            {
                // Some closed (warning)
                MessageBox.Show("Some arcs in this plan have a closed MLC. Please try again with an aperture on all fields or a plan with all MLCs closed.");
                return true;
            }
            return false;
        }

        public void CreateNewPlanWithMethod(List<BeamInfo> BeamInfos) // TODO fix deletion within loop crash by tagging all beams to delete (somehow) and then deleing them after loop
        {
            // Check that we have a valid MLC
            if (!CheckValidMLC(Plan.Beams))
            {
                return;
            }

            // Call begin mods
            var pat = Plan.Course.Patient;
            pat.BeginModifications();

            // Create new course with unique ID

            Course newcourse = Plan.Course;

            var newplan = Plan;

            _app.SaveModifications();

            //It appears that the CopyPlan method auto increments, so no need to do it manually.

            // Check closed (ON NEW PLAN!)
            bool isClosed = CheckClosed(pat, newplan);
            if (isClosed)
            {
                return;  // Exit if things are closed
            }

            _app.SaveModifications();

            // Check static or dynamic (does the mlc have more than 2 control points)
            // If static convert to dynamic (copy pattern to every cp) (if dynamic do nothing)
            var unpack = ConvertToDynamic(newplan, newcourse);
            var dynPlan = unpack.Item1;
            var success = unpack.Item2;
            Plan = dynPlan;
            if (!success)
            {
                MessageBox.Show("Exiting, cant perform DR edit on static plan.");
                return; // Can't perform dr edit on a static plan
            }

            _app.SaveModifications();

            // Compute the final DR using selected method
            CalcFinalDR(Plan, BeamInfos);

            // TODO remove the beams from newplan
            foreach (var copiedbeam in newplan.Beams.ToList())
            {
                newplan.RemoveBeam(copiedbeam);
            }

            // Loop through each beam and copy it with new msws edit the msws
            foreach (var bm in Plan.Beams)
            {
                var new_msws = FinalMSWS[bm.Id];
                //this is where the new field weighting is added.
                copy_beam(bm, new_msws, false, newplan);
            }

            newcourse.RemovePlanSetup(dynPlan);

            //MessageBox.Show(
            //    $"Dose rates assigned to the Plan: {newplan.Course.Id} > {newplan.Id}." +
            //    $"\nExit the virtual cone script and reload patient in Eclipse." +
            //    $"\nSelect course, select the plan, calculate dose, and follow review/approval procedures.", "Done!",
            //    MessageBoxButton.OK, MessageBoxImage.Information);
        }


    }
}