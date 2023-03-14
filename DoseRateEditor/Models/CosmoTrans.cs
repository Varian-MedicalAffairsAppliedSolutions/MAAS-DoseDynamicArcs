using OxyPlot;
using OxyPlot.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using VMS.TPS.Common.Model.Types;

namespace DoseRateEditor.Models
{
    public class CosmoTrans : CosmoPlot
    {

        public CosmoTrans() : base("Transverse", "DoseRateEditor.Resources.cosmo_trans.PNG")
        {
            
        }
        public Tuple<List<DataPoint>, List<double>> BuildArc(int steps, int R, double start_angle_deg, double stop_angle_deg, double plane_angle, GantryDirection gan_dir)
        {
            var points = new List<DataPoint>();
            var slopes = new List<double>();


            // Need fxn to return t_range
            // f(181 179, CW) -> 358
            /*
            double[] get_t_range(double start, double stop, GantryDirection dir)
            {
                var retval = new double[2];
                retval[0] = Math.PI * (start / 180);

                var delta = dir == GantryDirection.Clockwise? (Math.PI/180) * (start + stop) : (Math.PI/180) * Math.Abs(start - stop);
                retval[1] = retval[0] + delta;

                return retval;
            }*/

            

            if (plane_angle == 0)
            {
                stop_angle_deg *= -1;
            }

            
            var t_range = new double[] { 
                Math.PI* start_angle_deg / 180,
                Math.PI* stop_angle_deg / 180
            };

            //var t_range = get_t_range(start_angle_deg, stop_angle_deg, gan_dir);

            var step = Math.Abs(t_range[1] - t_range[0]) / steps;

            var t_curr = Math.Min(t_range[0], t_range[1]); // Start with the minimum t (could be negative)
            var t_max = Math.Max(t_range[0], t_range[1]);

            var plane_factor = Math.Cos(Math.PI * plane_angle / 180);
            if (plane_angle > 180)
            {
                plane_factor *= -1;
            }

            while (t_curr <= t_max)
            {
               

                var x = plane_factor * R * Math.Sin(t_curr);
                var y = R * Math.Cos(t_curr);

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

            // Build list of DataPoint[] in this loop
            //MessageBox.Show($"start and stop {startangle} - {stopangle} ");

            

            if (plane_angle == 90)
            {
                var line = new OxyPlot.Annotations.PolylineAnnotation
                {
                    Color = OxyColors.Red,
                    LineStyle = LineStyle.Dash,
                    StrokeThickness = 4,
                };

                //line.Points.Add()
                var R = (int)(1.1 * 50);

                line.Points.Add(new DataPoint(0, -R));
                line.Points.Add(new DataPoint(0, R));

                Annotations.Add(line);
                InvalidatePlot(true);
                return;
            }

            var arc = BuildArc(values.Count(), 40, startangle, stopangle, plane_angle, gan_dir);
            var maxHeight = 15;
            var maxVal = values.Max();

            for (int i = 0; i < values.Count(); i++)
            {
                // pi represents point on arc
                var pi = arc.Item1[i];

                // get tangent slope
                var slope = arc.Item2[i];

                // Get theta
                var theta = Math.Atan(slope);

                // Get the rect height
                var h = values[i] / maxVal * maxHeight;

                var polypoints = BuildRect(pi, h, 2, theta);

                // Add the annotation
                var poly = new PolygonAnnotation();
                poly.Fill = OxyColors.Transparent;
                poly.LineStyle = LineStyle.Solid;
                poly.Stroke = OxyColors.Red;
                poly.StrokeThickness = 1;
                poly.Points.AddRange(polypoints);
                Annotations.Add(poly);

            }

            InvalidatePlot(true);


        }

        private DataPoint[] BuildRect(DataPoint centerpoint, double width, double height, double theta)
        {
            var retval = new DataPoint[]
            {
                //RotatePoint(new DataPoint(centerpoint.X - (width/2), centerpoint.Y + (height/2)), centerpoint, theta),
                //RotatePoint(new DataPoint(centerpoint.X + (width/2), centerpoint.Y + (height/2)), centerpoint, theta),
                //RotatePoint(new DataPoint(centerpoint.X + (width/2), centerpoint.Y - (height/2)), centerpoint, theta),
                //RotatePoint(new DataPoint(centerpoint.X - (width/2), centerpoint.Y - (height/2)), centerpoint, theta),
                new DataPoint(centerpoint.X - (width/2), centerpoint.Y + (height/2)),
                new DataPoint(centerpoint.X + (width/2), centerpoint.Y + (height/2)),
                new DataPoint(centerpoint.X + (width/2), centerpoint.Y - (height/2)),
                new DataPoint(centerpoint.X - (width/2), centerpoint.Y - (height/2))
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
