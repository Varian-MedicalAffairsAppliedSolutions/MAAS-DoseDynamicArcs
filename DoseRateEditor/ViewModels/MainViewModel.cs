using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using DoseRateEditor.Models;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using Prism.Commands;
using VMS.TPS.Common.Model.API;
using static DoseRateEditor.Models.DRCalculator;

// TODO: add check to see if DR is modulated and warn user if this is the case
// TODO make buttons do something
// Check machine if TB -> maxGS=6
// if constant do nothing

namespace DoseRateEditor.ViewModels
{
    public class MainViewModel: Prism.Mvvm.BindableBase
    {
        private VMS.TPS.Common.Model.API.Application _app;

        public DelegateCommand OpenPatientCommand { get; private set; } // 1) in dcmd
        public DelegateCommand ViewCourseCommand { get; private set; }
        public DelegateCommand EditDRCommand { get; private set; }
        public DelegateCommand OnPlanSelectCommand { get; private set; }
        public DelegateCommand OnMethodSelectCommand { get; private set; }
        
        public DelegateCommand OnBeamSelectCommand { get; private set; }
        public DelegateCommand PlotCurrentDRCommand { get; private set; }
        public DelegateCommand PlotCurrentGSCommand { get; private set; }
        public DelegateCommand PreviewGSCommand { get; private set; }
        public DelegateCommand PreviewDRCommand { get; private set; }
        public DelegateCommand PreviewdMUCommand { get; private set; }
        public DelegateCommand PlotCurrentdMUCommand { get; private set; }

        public IViewCommand<OxyMouseWheelEventArgs> TransScrollCommand { get; private set; } 
        public ObservableCollection<CourseModel> Courses { get; private set; }
	    public ObservableCollection<PlanningItem> Plans { get; private set; }
        public ObservableCollection<Beam> Beams { get; private set; }
        public ObservableCollection<Nullable<DRMethod>> DRMethods { get; private set; }


        // CHECKBOXES
        private bool _PreviewDR;
        public bool PreviewDR
        {
            get { return _PreviewDR; }
            set { SetProperty(ref _PreviewDR, value); }
        }
        
        private bool _PreviewGS;
        public bool PreviewGS
        {
            get { return _PreviewGS; }
            set { SetProperty(ref _PreviewGS, value); }
        }

        private bool _CurrentDR;
        public bool CurrentDR
        {
            get { return _CurrentDR; }
            set { SetProperty(ref _CurrentDR, value); }
        }

        private bool _CurrentGS;
        public bool CurrentGS
        {
            get { return _CurrentGS; }
            set { SetProperty(ref _CurrentGS, value); }
        }

        private bool _CurrentdMU;
        public bool CurrentdMU
        {
            get { return _CurrentdMU; }
            set { SetProperty(ref _CurrentdMU, value); }
        }

        private bool _PreviewdMU;
        public bool PreviewdMU
        {
            get { return _PreviewdMU; }
            set { SetProperty(ref _PreviewdMU, value); }
        }
        // END CHECKBOXES


        private IPlotController _TransController;
        public IPlotController TransController { 
            get => _TransController; 
            set => SetProperty(ref _TransController, value); 
        }

        private int _SelectedSlice;

        public int SelectedSlice
        {
            get { return _SelectedSlice; }
            set { SetProperty(ref _SelectedSlice, value); }
        }

        /*
        private int _BeamIdx;

        public int BeamIdx
        {
            get { return _BeamIdx; }
            set 
            { 
                SetProperty(ref _BeamIdx, value);
            }

        }*/

        private PlotModel _TransPlot;

        public PlotModel TransPlot
        {
            get { return _TransPlot; }
            set { SetProperty(ref _TransPlot, value); }
        }

        private double[][,] _CT;

        public double[][,] CT
        {
            get { return _CT; }
            set { SetProperty(ref _CT, value); }
        }


        private Nullable<DRMethod> _SelectedMethod;

        public Nullable<DRMethod> SelectedMethod
        {
            get { return  _SelectedMethod; }
            set { 
                SetProperty(ref _SelectedMethod, value); 
                EditDRCommand.RaiseCanExecuteChanged();
                PreviewDRCommand.RaiseCanExecuteChanged();
                PreviewGSCommand.RaiseCanExecuteChanged();
            }
        }


        private string _patientId;
        public string PatientId
        {
            get { return _patientId; }
            set { SetProperty (ref _patientId, value); }
        }

        private CourseModel _SelectedCourse;

        public CourseModel SelectedCourse
        {
            get { return _SelectedCourse; }
            set { SetProperty( ref _SelectedCourse, value); EditDRCommand.RaiseCanExecuteChanged(); }
        }

        private ExternalPlanSetup _SelectedPlan;

        public ExternalPlanSetup SelectedPlan
        {
            get { return _SelectedPlan; }
            set { 
                SetProperty(ref _SelectedPlan, value); 
                EditDRCommand.RaiseCanExecuteChanged();
                PreviewDRCommand.RaiseCanExecuteChanged();
                PlotCurrentDRCommand.RaiseCanExecuteChanged();
                PreviewGSCommand.RaiseCanExecuteChanged();
                PlotCurrentGSCommand.RaiseCanExecuteChanged();
                PreviewdMUCommand.RaiseCanExecuteChanged();
                PlotCurrentdMUCommand.RaiseCanExecuteChanged();
            }
        }

        private Beam _SelectedBeam;

        public Beam SelectedBeam
        {
            get { return _SelectedBeam; }
            set { 
                SetProperty(ref _SelectedBeam, value);
                PlotCurrentdMUCommand.RaiseCanExecuteChanged();
                PlotCurrentDRCommand.RaiseCanExecuteChanged();
                PlotCurrentGSCommand.RaiseCanExecuteChanged();
            }
        }



        private PlotModel _DRPlot;

        public PlotModel DRPlot
        {
            get { return _DRPlot; }
            set { SetProperty(ref _DRPlot, value); }
        }

        private CosmoPlot _View1;

        public CosmoPlot View1
        {
            get { return _View1; }
            set { SetProperty(ref _View1, value); }
        }


        private DRCalculator _DRCalc;

        public DRCalculator DRCalc
        {
            get { return _DRCalc; }
            set { SetProperty(ref _DRCalc, value); }
        }

        private LineSeries _DR0_series;

        public LineSeries DR0_series
        {
            get { return _DR0_series; }
            set { SetProperty(ref _DR0_series, value); }
        }

        private LineSeries _DRf_series;
        public LineSeries DRf_series
        {
            get { return _DRf_series; }
            set { SetProperty(ref _DRf_series, value); }
        }

        private LineSeries _GSf_series;
        public LineSeries GSf_series
        {
            get { return _GSf_series; }
            set { SetProperty(ref _GSf_series, value); }
        }

        private LineSeries _GS0_series;
        public LineSeries GS0_series
        {
            get { return _GS0_series; }
            set { SetProperty(ref _GS0_series, value); }
        }

        private LineSeries _dMU0_series;
        public LineSeries dMU0_series
        {
            get { return _dMU0_series; }
            set { SetProperty(ref _dMU0_series, value); }
        }

        private LineSeries _dMUf_series;
        public LineSeries dMUf_series
        {
            get { return _dMUf_series; }
            set { SetProperty(ref _dMUf_series, value); }
        }



        public MainViewModel(VMS.TPS.Common.Model.API.Application app)
        {
            _app = app;

            // Create delegate cmd
            //OpenPatientCommand = new DelegateCommand(OnOpenPatient); // DELETE!
            OpenPatientCommand = new DelegateCommand(OnOpenPatient, CanOpenPatient);
            ViewCourseCommand = new DelegateCommand(OnSelectCourse, CanSelectCourse);
            EditDRCommand = new DelegateCommand(OnEditDR, CanEditDR);
            OnPlanSelectCommand = new DelegateCommand(OnPlanSelect, CanPlanSelect);
            OnMethodSelectCommand = new DelegateCommand(OnMethodSelect, CanMethodSelect);
            TransScrollCommand = new DelegatePlotCommand<OxyMouseWheelEventArgs>(OnScroll);
            PlotCurrentDRCommand = new DelegateCommand(OnCurrentDR, CanCurrentDR);
            PreviewDRCommand = new DelegateCommand(OnPreviewDR, CanPreviewDR);
            PreviewGSCommand = new DelegateCommand(OnPreviewGS, CanPreviewGS);
            PlotCurrentGSCommand = new DelegateCommand(OnCurrentGS, CanCurrentGS);
            PreviewdMUCommand = new DelegateCommand(OnPreviewdMU, CanPreviewdMU);
            PlotCurrentdMUCommand = new DelegateCommand(OnCurrentdMU, CanCurrentdMU);
    

            Courses = new ObservableCollection<CourseModel>();
            Plans = new ObservableCollection<PlanningItem>();
            Beams = new ObservableCollection<Beam>();

            // DR PLOT
            DRPlot = new PlotModel { Title = "Doserate and Gantry Speed"};

            // Cosmo view 1
            View1 = new CosmoPlot("", "DoseRateEditor.Resources.cosmo1.1.PNG");
            //View1.DrawRects(new List<double> { 1, 2, 3, 4, 5, 6, 7, 8 }, 4, 4);
            

            // Create the different axes with respect/ive keys
            var DRAxis = new LinearAxis
            {
                Title = "Doserate",
                Position = AxisPosition.Left,
                Key = "DRAxis"   
            };
            DRPlot.Axes.Add(DRAxis);

            var GSAxis = new LinearAxis
            {
                Title = "Gantry Speed",
                Position = AxisPosition.Right,
                Key = "GSAxis"
            };
            DRPlot.Axes.Add(GSAxis);

            var dMUAxis = new LinearAxis
            {
                //Title = "Delta MU",
                Position = AxisPosition.Left,
                TickStyle = TickStyle.None,
                Key = "dMUAxis",
           
            };
            DRPlot.Axes.Add(dMUAxis);

            // Add the series
            DR0_series = new LineSeries
            {
                YAxisKey = "DRAxis",
                Color = OxyColors.Red
            };
            DRPlot.Series.Add(DR0_series);

            GS0_series = new LineSeries
            {
                YAxisKey = "GSAxis",
                Color = OxyColors.Blue
            };
            DRPlot.Series.Add(GS0_series);

            DRf_series = new LineSeries
            {
                YAxisKey = "DRAxis",
                Color = OxyColors.OrangeRed,
                LineStyle = LineStyle.Dot,
                StrokeThickness = 4
            };
            DRPlot.Series.Add(DRf_series);

            GSf_series = new LineSeries { 
                YAxisKey = "GSAxis",
                Color = OxyColors.DeepSkyBlue,
                LineStyle = LineStyle.Dot,
                StrokeThickness = 4
            };
            DRPlot.Series.Add(GSf_series);

            dMU0_series = new LineSeries
            {
                YAxisKey = "dMUAxis",
                Color = OxyColors.Gold
            };
            DRPlot.Series.Add(dMU0_series);

            dMUf_series = new LineSeries { 
                YAxisKey = "dMU Axis",
                Color = OxyColors.LightGoldenrodYellow,
                LineStyle = LineStyle.Dot,
                StrokeThickness = 4 
            };


            // TRANS PLOT
            TransPlot = new PlotModel { Title = "IsoPlot Transverse" };
            AddAxes(TransPlot);
            TransController = new PlotController();

            TransController.UnbindAll();
            TransController.BindMouseWheel(TransScrollCommand);
            CT = new double[0][,];

            // Set the DR EDIT methods
            
            DRMethods = new ObservableCollection<Nullable<DRMethod>>
            {
               DRCalculator.DRMethod.Sin,
               DRCalculator.DRMethod.Cosmic,
               DRCalculator.DRMethod.Parabola,
               DRCalculator.DRMethod.Juha
            };


        }


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
        }

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
        }

        private void OnScroll(IPlotView arg1, IController arg2, OxyMouseWheelEventArgs arg3)
        {
            if (arg3.Delta > 0)
            {
                if (SelectedSlice + 1 >= SelectedPlan.StructureSet.Image.ZSize)
                {
                    return;
                }
                SelectedSlice++;
            }

            if (arg3.Delta < 0)
            {
                if (SelectedSlice < 1)
                {
                    return;
                }
                SelectedSlice--;
            }

            TransPlot.InvalidatePlot(false);
            UpdateSeries(TransPlot);

        }

        private void AddAxes(PlotModel plotModel) // Lifted from CJA blog "Working with various plot types"
        {
            var xAxis = new LinearAxis
            { Title = "X", Position = AxisPosition.Bottom, };
            plotModel.Axes.Add(xAxis);

            var yAxis = new LinearAxis
            { Title = "Y", Position = AxisPosition.Left };
            plotModel.Axes.Add(yAxis);

            var zAxis = new LinearColorAxis
            {
                Title = "CT", // TODO - Change!
                Position = AxisPosition.Top,
                Palette = OxyPalettes.Gray(256),
                Maximum = 1000, // TODO - Change!
                Minimum = 0
            };
            plotModel.Axes.Add(zAxis);
        }


        private void UpdateSeries(PlotModel plotModel)
        {
            plotModel.Series.Clear();
            plotModel.Series.Add(CreateHeatMap(SelectedSlice));
            plotModel.InvalidatePlot(true);
        }

       

        private HeatMapSeries CreateHeatMap(int slice)
        {
            var data = GetCTData(slice);
            double[,] data_double = new double[data.GetLength(0), data.GetLength(1)];
            Array.Copy(data, data_double, data.Length);
            //Array.Reverse(data_double);
            return new HeatMapSeries
            {
                X0 = data.GetLength(1),
                X1 = 0,
                Y0 = data.GetLength(0),
                Y1 = 0,
                Data = data_double
            };
        }

        private int[,] GetCTData(int slice)
        {

            var imgs = SelectedPlan.StructureSet.Image.Series.Images;
            var data = new int[imgs.First().XSize, imgs.First().YSize];
            imgs.First().GetVoxels((int)slice, data);
            //var newArray = Array.ConvertAll(array, item => (NewType)item);
            return data;

        }


        private void OnCurrentdMU() {
            dMU0_series.Points.Clear();
            View1.ClearRects();
            if (CurrentdMU)
            {
                var dMU = DRCalc.InitialdMU[SelectedBeam.Id];
                dMU0_series.Points.AddRange(dMU);
                View1.DrawRects(dMU.Select(x => x.Y).ToList(), 5, 5);
            }
            DRPlot.InvalidatePlot(true);
        }

        private bool CanCurrentdMU() { return CanCurrentDR(); }

        private void OnPreviewdMU() { return; }

        private bool CanPreviewdMU() { return false; }


        private void OnMethodSelect()
        {
            // Clear final gs and dr series
            GSf_series.Points.Clear();
            DRf_series.Points.Clear();

            // Calculate final dr and gs
            DRCalc.CalcFinalDR(SelectedPlan, SelectedMethod.Value);

            //SelectedBeam.Id = DRCalc.FinalDRs.Keys.ToList()[BeamIdx];

            DRPlot.InvalidatePlot(true);

        }

        private bool CanMethodSelect()
        {
            return CanEditDR();
        }

        // Delegate methods for plotting ...
        private void OnCurrentGS()
        {
            if (CurrentGS) // If checkbox is checked
            {
                GS0_series.Points.AddRange(DRCalc.InitialGSs[SelectedBeam.Id]);
            }
            else
            {
                GS0_series.Points.Clear();
            }
            DRPlot.InvalidatePlot(true);
        }

        private bool CanCurrentGS()
        {
            return CanCurrentDR();
        }
        private void OnPreviewDR()
        {
            if (PreviewDR) // See if check button is checked
            {
                if (!DRCalc.LastMethodCalculated.HasValue) // if the DRCalc dosen't already have the DRf calculated
                {
                    if (DRCalc.LastMethodCalculated != SelectedMethod.Value)
                    {
                        // Calculate final DR
                        DRCalc.CalcFinalDR(SelectedPlan, SelectedMethod.Value);
                    }
                }

                // Plot final DR
                DRf_series.Points.AddRange(DRCalc.FinalDRs[SelectedBeam.Id]);
            }
            else
            {
                // Else clear DRf
                DRf_series.Points.Clear();
            }

            DRPlot.InvalidatePlot(true);

        }

        private bool CanPreviewDR() {
            return SelectedMethod.HasValue && (SelectedPlan != null);
        }

        private void OnPreviewGS()
        {   
            if (PreviewGS) // See if check button is checked, else delete GSf plot
            {
                if (!DRCalc.LastMethodCalculated.HasValue)
                {
                    if (DRCalc.LastMethodCalculated != SelectedMethod.Value)
                    {
                        // Calculate final DR
                        DRCalc.CalcFinalDR(SelectedPlan, SelectedMethod.Value);
                    }
                }
                GSf_series.Points.AddRange(DRCalc.FinalGSs[SelectedBeam.Id]);
            }
            else
            {
                GSf_series.Points.Clear();
            }
            

            
            DRPlot.InvalidatePlot(true);

        }

        private bool CanPreviewGS() { return CanPreviewDR(); }

        private void OnCurrentDR()
        {
            if (CurrentDR) // If checkbox is checked
            {
                DR0_series.Points.AddRange(DRCalc.InitialDRs[SelectedBeam.Id]);
            }
            else
            {
                DR0_series.Points.Clear();
            }
            DRPlot.InvalidatePlot(true);
        }

        private bool CanCurrentDR() { return SelectedPlan != null && SelectedBeam != null; }

        


        private void OnPlanSelect()
        {

            // Create a DR calculator for selected plan
            DRCalc = new DRCalculator(SelectedPlan);

            // Clear the previous initial and final dr/gs plots
            DR0_series.Points.Clear();
            DRf_series.Points.Clear();
            GSf_series.Points.Clear();

            // Clear prev list of beams
            Beams.Clear();
            foreach(var bm in SelectedPlan.Beams) { Beams.Add(bm); }

            // Reset transaxial plot
            SelectedSlice = 0;
            UpdateSeries(TransPlot);

        }

        private bool CanPlanSelect()
        {
            return (SelectedCourse != null) && (SelectedPlan != null);
        }

        private void OnEditDR()
        {
            DRCalc.CreateNewPlanWithMethod(SelectedMethod.Value);
            _app.SaveModifications();
        }

        private bool CanEditDR()
        {
            return (SelectedMethod != null) && (SelectedCourse != null) && (SelectedPlan != null);
        }

        private void OnOpenPatient() // 2) in dcmd
        {
            _app.ClosePatient();
            var pat = _app.OpenPatientById(PatientId);
            
            // List all courses
            Courses.Clear();
            Plans.Clear();
            Beams.Clear();
            SelectedCourse = null;
            SelectedPlan = null;
            SelectedMethod = null;

            foreach (Course crs in pat.Courses)
            {
                Courses.Add(new CourseModel(crs));
            }

        }

        private bool CanOpenPatient()
        {
            return true; // For now just say we can and handle consequences
        }

        private void OnSelectCourse()
        {
            // To be called by dcmd when course is selected
            //MessageBox.Show($@"Calling OnSelect course for {SelectedCourse.Id}");

            Plans.Clear();
            foreach(var p in SelectedCourse.Plans.Values)
            {
                Plans.Add(p);
            }

            Beams.Clear();


        }

        private bool CanSelectCourse()
        {
            return true;
        }


    }
}
