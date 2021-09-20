using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using System.Linq;
using System.Reflection;


namespace DoseRateEditor.Models
{
    public abstract class CosmoPlot : PlotModel
    {
        // The background phantom image
        public OxyImage image { get; private set; }

        public CosmoPlot(string title, string img_name) : base()
        {
            // Set plotmodel title
            Title = title;

            // Add the axis for plotting
            // X axis
            var xAxis = new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineThickness = 1,
                Minimum = -50,
                Maximum = 50,
                Position = AxisPosition.Bottom,
                IsAxisVisible = false,
                Tag = "XAX"

            };
            Axes.Add(xAxis);

            // Y axis
            var yAxes  = new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineThickness = 1,
                Minimum = -50,
                Maximum = 50,
                Position = AxisPosition.Left,
                IsAxisVisible = false,
                Tag = "YAX"
            };
            Axes.Add(yAxes);
            
            // Set the image
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(img_name)) 
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
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Middle,
                Layer = AnnotationLayer.BelowAxes
            });

            InvalidatePlot(true);

        }

        public void ClearPlot() 
        {
            // Clear annotations except image
            var img_list = Annotations.OfType<ImageAnnotation>().ToList();
            var anns = Annotations.Except(img_list).ToList();
            foreach(var ann in anns) { Annotations.Remove(ann); }

            // Refresh
            InvalidatePlot(true);
        }


    }

}
