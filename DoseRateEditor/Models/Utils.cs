using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;

namespace DoseRateEditor.Models
{
    public static class Utils
    {
        public static Tuple<List<double>, List<double>, List<double>, List<ControlPointParameters>> GetBeamAngles(Beam beam)
        {
            var edits = beam.GetEditableParameters();
            var cps = edits.ControlPoints.ToList();

            var gantry_angles = cps.Select(x => x.GantryAngle).ToList();
            var col_angles = cps.Select(x => x.CollimatorAngle).ToList();
            var couch_angles = cps.Select(x => x.PatientSupportAngle).ToList();

            return new Tuple<List<double>, List<double>, List<double>, List<ControlPointParameters>> (gantry_angles, col_angles, couch_angles, cps);
        }
    }
}
