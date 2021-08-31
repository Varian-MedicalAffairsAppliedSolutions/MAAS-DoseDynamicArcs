using OxyPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using VMS.TPS.Common.Model.API;

namespace DoseRateEditor.Models
{
    public class DRCalculator
    {
        
        public delegate double func(double theta);
        public enum DRMethod
        {
            Sin,
            Parabola,
            Cosmic,
            Juha
        }

        private static func sinfunc = (theta) => Math.Sin((theta * Math.PI) / 180);
        private static func cosmicfunc = (theta) => (16 * theta * (Math.PI - theta)) / ((5 * Math.PI * Math.PI) - 4 * theta * (Math.PI - theta));
        private static func parafunc = (theta) => (theta * (180 - theta)) / (90 * 90);

        private static Dictionary<DRMethod, func> Delagate_dictionary = new Dictionary<DRMethod, func>
        {
            { DRMethod.Sin, sinfunc},
            { DRMethod.Cosmic, cosmicfunc},
            { DRMethod.Parabola, parafunc}
        };

        public Dictionary<string, List<DataPoint>> InitialDRs { get; set; }

        public Dictionary<string, List<DataPoint>> FinalDRs { get; set; }

        public Dictionary<string, List<double>> FinalMSWS { get; set; }

        public Dictionary<string, List<DataPoint>> FinalGSs { get; set; }

        public int numbeams;

        private ExternalPlanSetup Plan { get; set; }
        public DRCalculator(ExternalPlanSetup plan)
        {
            Plan = plan;

            numbeams = Plan.Beams.Count();
            
            // Compute the initial DR for each beam in the plan
            InitialDRs = new Dictionary<string, List<DataPoint>>();
            FinalMSWS = new Dictionary<string, List<double>>();
            FinalDRs = new Dictionary<string, List<DataPoint>>();
            FinalGSs = new Dictionary<string, List<DataPoint>>();

            foreach (Beam b in Plan.Beams)
            {
                InitialDRs.Add(b.Id, ComputeDRBeam(b));
            }


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


            if (method == DRMethod.Juha)
            {
                return; // Not implemented yet
            }

            // Else compute and save Final DRS and GSs
            foreach(Beam b in Plan.Beams)
            {
                var msws_new = GenerateMSWS(b, Delagate_dictionary[method], "deg");
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
        }

        private Tuple<List<DataPoint>, List<DataPoint>> ComputeDRFromMSWS(List<double> msws, double bm_meterset, List<double> gantry_angles, double gantry_speed_max=4.8, double DR_max=2400)
        {
            var DRs = new List<DataPoint>();
            var GSs = new List<DataPoint>();

            for(int i=1; i<msws.Count; i++)
            {
                var mu_last = msws[i - 1] * bm_meterset;
                var mu_current = msws[i] * bm_meterset;
                var delta_mu = mu_current - mu_last;

                var delta_gantry = Math.Abs(gantry_angles[i] - gantry_angles[i - 1]);
                var rot_time = delta_gantry / gantry_speed_max;
                var calcd_mu_rate = (delta_mu * 60) / rot_time;

                if (calcd_mu_rate < DR_max)
                {
                    DRs.Add(new DataPoint(i - 1, calcd_mu_rate));
                    GSs.Add(new DataPoint(i - 1, gantry_speed_max));
                }
                else
                {
                    DRs.Add(new DataPoint(i - 1, DR_max));
                    var L_i = (bm_meterset / delta_gantry) * (msws[i] - msws[i - 1]);
                    var GS = DR_max / L_i;
                    GS /= 60;
                    GSs.Add(new DataPoint(i - 1, GS));
                }
            }

            return new Tuple<List<DataPoint>, List<DataPoint>>(DRs, GSs);
        }

 
        private List<double> GenerateMSWS (Beam bm, func f, string mode="rad")
        {
            var msws = new List<double> { 0 };

            // Check mode is valid
            if (mode != "rad" && mode != "deg")
            {
                throw new Exception("Mode must be deg or rad");
            }

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
                if (mode == "rad")
                {
                    gan *= (Math.PI / 180);
                }

                var delta = Math.Abs(f(arg));
                
                // Assert delta > 0
                if ((delta < 0)) {
                    throw new Exception("delta not gt 0!");
                }

                dmsw.Add(delta);
                
            }

            var beta = dmsw.Sum();
            for(int j=1; j<dmsw.Count; j++) 
            { 
                dmsw[j] /= beta; 
            }

            // build msws
            foreach(var delta in dmsw)
            {
                msws.Add(msws.Last() + delta);
            }

            return msws;

        }

        private List<DataPoint> ComputeDRBeam(Beam bm, double gantry_speed_max=4.8, double DR_max=2400) // Calculates current msws given an existing beam
        {

            var DRs = new List<DataPoint>();

            var cps = bm.GetEditableParameters().ControlPoints.ToList();

            for (int i = 0; i < cps.Count(); i++)
            {
                if (i > 0) // Skip first CP
                {
                    // 1. Calc delta MU
                    var mu_last = cps[i - 1].MetersetWeight * bm.Meterset.Value;
                    var mu_current = cps[i].MetersetWeight * bm.Meterset.Value;
                    var delta_mu = mu_current - mu_last;

                    // 2. Calc d(MU)/dt from the time it takes to move between cps
                    var delta_gantry = Math.Abs(cps[i].GantryAngle - cps[i-1].GantryAngle); // TODO: fix for angle devs that cross 0/360 mark
                    var rotation_time = delta_gantry / gantry_speed_max;
                    var calcd_mu_rate = (delta_mu * 60) / rotation_time;

                    if (calcd_mu_rate < DR_max)
                    {
                        DRs.Add(new DataPoint(i, calcd_mu_rate));
                    }
                    else
                    {
                        DRs.Add(new DataPoint(i, DR_max));
                    }

                }
            }

            return DRs;
        }
    }
}
