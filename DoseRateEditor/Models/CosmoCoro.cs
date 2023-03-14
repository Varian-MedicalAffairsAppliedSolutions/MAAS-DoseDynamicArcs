using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoseRateEditor.Models
{
    public class CosmoCoro : CosmoPlot
    {
        public CosmoCoro() : base("Coronal", "DoseRateEditor.Resources.Frontal_crop.png") 
        {
           
        }

        public void DrawAngle(double angle)
        {
            // Add angle plot (line) to coronal view
            var line = new OxyPlot.Annotations.PolylineAnnotation
            {
                Color = OxyColors.Red,
                LineStyle = LineStyle.Dash,
                StrokeThickness = 4,
            };

            //line.Points.Add()
            var R = (int)(1.1 * 50);

            // TODO - handle 0 case
            var dx = R * Math.Cos(angle * (Math.PI / 180));
            
            if (angle < 90)
            {
                dx *= -1;
            }

            var dy = Math.Abs(R * Math.Sin(angle * (Math.PI / 180)));

            if (angle == 0)
            {
                line.Points.Add(new DataPoint(-(int)dx, 0));
                line.Points.Add(new DataPoint((int)dx, 0));
            }

            else
            {
                line.Points.Add(new DataPoint(0, 0));
                line.Points.Add(new DataPoint((int)dx, (int)dy));
            }

            // Add line and refresh
            Annotations.Add(line);
            InvalidatePlot(true);
        }
    }
}
