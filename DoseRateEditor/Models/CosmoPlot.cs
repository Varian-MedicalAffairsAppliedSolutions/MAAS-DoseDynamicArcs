using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DoseRateEditor.Models
{
    public class CosmoPlot : PlotModel
    {
        // The background phantom image
        public OxyImage image { get; private set; }

        // Hold current rect annotations
        public List<PolygonAnnotation> Rects { get; private set; }

        public CosmoPlot(string title, string img_name) : base()
        {
            // Set plotmodel title
            Title = title;

            // Add the axis for plotting
            Axes.Add(new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineThickness = 1,
                Minimum = -50,
                Maximum = 50,
                Position = AxisPosition.Bottom,
                IsAxisVisible = false

            }) ;

            Axes.Add(new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineThickness = 1,
                Minimum = -50,
                Maximum = 50,
                Position = AxisPosition.Left,
                IsAxisVisible=false
            });
            
           

            // Set the image
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(img_name)) ///"DoseRateEditor.Resources.cosmo1.1.PNG"
            {
                image = new OxyImage(stream);
            }

            Annotations.Add(new ImageAnnotation
            {
                ImageSource = image,
                Opacity = 1,
                Interpolate = false,
                X = new PlotLength(0.5, PlotLengthUnit.RelativeToPlotArea),
                Y = new PlotLength(0.5, PlotLengthUnit.RelativeToPlotArea),
                Width = new PlotLength(1, PlotLengthUnit.RelativeToPlotArea),
                //Height = new PlotLength(1, PlotLengthUnit.RelativeToPlotArea), // CR added
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Middle,
                Layer = AnnotationLayer.BelowAxes
            });

            InvalidatePlot(true);

        }

        public void ClearRects() // Call whenever Rects set is called
        {
            // Clear annotations except image
            foreach (var ann in Annotations)
            {
                if (ann is ImageAnnotation)
                {
                    continue;
                }
                Annotations.Remove(ann);
            }

            Annotations.Clear();

            // Refresh
            InvalidatePlot(true);
        }

        public Tuple<List<DataPoint>, List<double>> BuildArc(int steps, int R)
        {
            var points = new List<DataPoint>();
            var slopes = new List<double>();

            var t0 = -1 * Math.PI / 2;
            var t1 = Math.PI / 2;

            var step = Math.Abs(t1 - t0) / steps;

            var t_curr = t0;

            while (t_curr <= t1)
            {
                var x = R * Math.Sin(t_curr);
                var y = R * Math.Cos(t_curr);

                points.Add(new DataPoint(x, y));

                var slope = -Math.Sin(t_curr) / Math.Cos(t_curr);
                slopes.Add(slope);

                t_curr += step;
            }

            // Annotations.Clear();
            /*var poly = new PolylineAnnotation();
            
            poly.Points.AddRange(points);
           
            
            poly.LineStyle = LineStyle.Dash;
            Annotations.Add(poly);
            poly.EnsureAxes();
            
            InvalidatePlot(true);*/
            return new Tuple<List<DataPoint>, List<double>>(points, slopes);
            

        }

        public void DrawRects(List<double> values, double startangle, double stopangle)
        {
            // Draw rects proportional to the values
            // Note: Values could be delta MU at each cp (which is prop to the DR)
            // Generate a list of polygon annotations given a double list
            
            // Build list of DataPoint[] in this loop
            var arc = BuildArc(values.Count, 45);
            var maxHeight = 12;
            var maxVal = values.Max();

            for(int i = 0; i < values.Count; i++)
            {
                // pi represents point on arc
                var pi = arc.Item1[i];

                // get tangent slope
                var slope = arc.Item2[i];

                // Get theta
                var theta = Math.Atan(slope);

                // Get the rect height
                var h = (values[i] / maxVal) * maxHeight;

                var polypoints = BuildRect(pi, 1, h, theta);

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

    /* OLD ///
    private PlotModel BuildCosmoPlot()
    {
        var plt = new PlotModel { Title = "View1" };

        // Add image
        OxyImage image;
        var assembly = Assembly.GetExecutingAssembly();
        using (var stream = assembly.GetManifestResourceStream("DoseRateEditor.Resources.cosmo1.1.PNG"))
        {
            image = new OxyImage(stream);
        }

        // Centered in plot area, filling width
        // From https://oxyplot.userecho.com/en/communities/1/topics/473-inserting-a-bitmap-into-axes

        plt.Annotations.Add(new ImageAnnotation
        {
            ImageSource = image,
            Opacity = 1,
            Interpolate = false,
            X = new PlotLength(0.5, PlotLengthUnit.RelativeToPlotArea),
            Y = new PlotLength(0.5, PlotLengthUnit.RelativeToPlotArea),
            Width = new PlotLength(1, PlotLengthUnit.RelativeToPlotArea),
            //Height = new PlotLength(1, PlotLengthUnit.RelativeToPlotArea), // CR added
            HorizontalAlignment = OxyPlot.HorizontalAlignment.Center,
            VerticalAlignment = OxyPlot.VerticalAlignment.Middle,
            Layer = AnnotationLayer.BelowAxes
        });

        // Add arc line
        var arcAxis = new AngleAxis
        {
            Minimum = 0,
            Maximum = 100,
            TickStyle = TickStyle.None,
            AxislineColor = OxyColors.Red,
            ExtraGridlineColor = OxyColors.Red,
            MajorGridlineColor = OxyColors.Red,
            MinorGridlineColor = OxyColors.Red,
            MinorTicklineColor = OxyColors.Red,
            TicklineColor = OxyColors.Red,
            AxislineThickness = 5,
            ExtraGridlineThickness = 5,
            MajorGridlineThickness = 5,
            MinorGridlineThickness = 5,
            Layer = AxisLayer.AboveSeries,
            EndAngle = 180,
            StartAngle = 0,
        };
        plt.Axes.Add(arcAxis);

        // Add mag axis (this appears to be required if we have angle axis)
        var magAxis = new MagnitudeAxis
        {
            Minimum = 0,
            Maximum = 100,
            TickStyle = TickStyle.None,
            AxislineColor = OxyColors.Red,
            ExtraGridlineColor = OxyColors.Red,
            MajorGridlineColor = OxyColors.Red,
            MinorGridlineColor = OxyColors.Red,
            MinorTicklineColor = OxyColors.Red,
            TicklineColor = OxyColors.Red,
            AxislineThickness = 5,
            ExtraGridlineThickness = 5,
            MajorGridlineThickness = 5,
            MinorGridlineThickness = 5,
            Layer = AxisLayer.AboveSeries,
            MajorStep = 500,

        };


        plt.Axes.Add(magAxis);

        //FunctionSeries f = new FunctionSeries((x) => 500, 0, 360, 0.1);
        //plt.Series.Add(f);
        // See: https://stackoverflow.com/questions/59501561/how-to-draw-a-circle-within-oxyplot-angleaxis-and-magnitudeaxis

        return plt;
    }*/

    /*
    private List<PolygonAnnotation> GenerateRects(List<double> dMU)
    {
        // Generate a list of polygon annotations given a dMU list
        // Outline as follows:
        // 1. How many dMU values (same as number of cps)
        // 2. Start and stop angle (in view one I believe this is the couch??? ask about that)
        // 3. Define max rect height to be the max dMU value
        // 4. For each dMU generate a rect with width = ? and height = (dMU current / dMU max) * Max heigth value
        // 5. Rotate appropriatly using some kind of transform (what angle tho???)
        // 6. Translate to be tangential to the pre drawn arc, we wont always need this to be drawn, but nice to have now.

        return new List<PolygonAnnotation>();
    }*/
}
