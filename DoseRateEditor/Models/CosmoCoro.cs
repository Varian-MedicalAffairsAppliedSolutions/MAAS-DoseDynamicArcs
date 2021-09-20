using OxyPlot;
using OxyPlot.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoseRateEditor.Models
{
    public class CosmoCoro : CosmoPlot
    {
        public CosmoCoro() : base("Cosmo Coro", "DoseRateEditor.Resources.cosmo_coro.PNG") 
        {
           
        }

        public void DrawAngle(double angle, double R)
        {
            // Add angle plot (line) to coronal view
            var line = new LineAnnotation
            {
                Type = LineAnnotationType.LinearEquation,
                Slope = 1,
                Intercept = 0,
                Color = OxyColors.Red,
                LineStyle = LineStyle.Dash,
                StrokeThickness = 5,
            };

            // Add line and refresh
            Annotations.Add(line);
            InvalidatePlot(true);
        }
    }
}
