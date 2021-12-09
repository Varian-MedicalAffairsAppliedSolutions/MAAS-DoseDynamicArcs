using OxyPlot;
using OxyPlot.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.Types;

namespace DoseRateEditor.Models
{
    public class CosmoSag : CosmoPlot
    {
        public CosmoSag() : base("Sagittal", "DoseRateEditor.Resources.cosmo_sag.PNG")
        {

        }

        public Tuple<List<DataPoint>, List<double>> BuildArc(int steps, int R, double start_angle_deg, double stop_angle_deg, double plane_angle, GantryDirection gan_dir)
        {
            // TODO - start angle and delta angle instead of start/stop angle.

            var points = new List<DataPoint>();
            var slopes = new List<double>();

            var t0 = Math.PI * start_angle_deg / 180;
            var t1 = Math.PI * stop_angle_deg / 180;

            var step = Math.Abs(t1 - t0) / steps;

            var t_curr = Math.Min(t0, t1);


            while (t_curr <= Math.Max(t0, t1))
            {
                var x = R * Math.Sin(t_curr);
                var y = Math.Abs(Math.Sin(Math.PI * plane_angle / 180)) * R * Math.Cos(t_curr);

                points.Add(new DataPoint(x, y));

                var slope = -Math.Sin(t_curr) / Math.Cos(t_curr);
                slopes.Add(slope);

                t_curr += step;
            }


            return new Tuple<List<DataPoint>, List<double>>(points, slopes);


        }
        public void DrawRects(List<double> values, double startangle, double stopangle, double plane_angle, GantryDirection gan_dir)
        {
            // Draw rects proportional to the values
            // Note: Values could be delta MU at each cp (which is prop to the DR)
            // Generate a list of polygon annotations given a double list

            // plan angle will be table angle in this case ...

            // Build list of DataPoint[] in this loop
            //MessageBox.Show($"start and stop {startangle} - {stopangle} ");

            var arc = BuildArc(values.Count, 42, startangle-90, stopangle-90, plane_angle, gan_dir); // minus 90 to fix angles
            var maxHeight = 12 * Math.Sin(Math.PI * plane_angle / 180);
            var maxVal = values.Max();

            for (int i = 0; i < values.Count; i++)
            {
                // pi represents point on arc
                var pi = arc.Item1[i];

                // get tangent slope
                var slope = arc.Item2[i];

                // Get theta
                var theta = Math.Atan(slope);

                // Get the rect height
                var h = values[i] / maxVal * maxHeight;

                var polypoints = BuildRect(pi, 2, h, theta);

                // Add the annotation
                var poly = new PolygonAnnotation();
                poly.Fill = OxyColors.Red;
                poly.Points.AddRange(polypoints);
                Annotations.Add(poly);

            }

            InvalidatePlot(true);


        }

        private DataPoint[] BuildRect(DataPoint centerpoint, double width, double height, double theta)
        {
            var retval = new DataPoint[]
            {
                RotatePoint(new DataPoint(centerpoint.X - (width/2), centerpoint.Y + (height/2)), centerpoint, theta),
                RotatePoint(new DataPoint(centerpoint.X + (width/2), centerpoint.Y + (height/2)), centerpoint, theta),
                RotatePoint(new DataPoint(centerpoint.X + (width/2), centerpoint.Y - (height/2)), centerpoint, theta),
                RotatePoint(new DataPoint(centerpoint.X - (width/2), centerpoint.Y - (height/2)), centerpoint, theta),
            };

            return retval;

        }

        private DataPoint RotatePoint(DataPoint toRotate, DataPoint origin, double theta)
        {
            var pt = new DataPoint(toRotate.X - origin.X, toRotate.Y - origin.Y);

            var s = Math.Sin(theta);
            var c = Math.Cos(theta);

            var xnew = pt.X * c - pt.Y * s;
            var ynew = pt.X * s + pt.Y * c;

            return new DataPoint(xnew + origin.X, ynew + origin.Y);

        }
    }
}

