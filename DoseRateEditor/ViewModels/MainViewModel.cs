using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Windows.Navigation;
using DoseRateEditor.Models;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Prism.Commands;
using VMS.TPS.Common.Model.API;
using static DoseRateEditor.Models.DRCalculator;

// TODO: Scale y axis to max DR
// TODO: Color underline cb's as legend
// TODO: Crop pictures to "Nose only" on coro/sag views
// TODO: Fix id of new plan

// TODO: add check to see if DR is modulated and warn user if this is the case
// TODO make buttons do something
// TODO: add credit pane for selected DR edit method
// Check machine if TB -> maxGS=6
// TODO: add not for clinical use warning sign
// TODO: add waiver key in config to switch off the date dialog box and the not validated for clinical use banner
// Add liscence .txt
// if constant do nothing

namespace DoseRateEditor.ViewModels
{
    public class MainViewModel: Prism.Mvvm.BindableBase
    {
        private Application _app;
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
        public ObservableCollection<Tuple<Nullable<DRMethod>, Nullable<bool>>> DRMethods { get; private set; }

        // DR EDIT METHOD CREDIT TEXT
        private string _CreditText;
        public string CreditText
        {
            get { return _CreditText; }
            set { SetProperty(ref _CreditText, value); }
        }


        // CHECKBOXES
        private bool _PreviewDR;
        public bool PreviewDR
        {
            get { return _PreviewDR; }
            set { SetProperty(ref _PreviewDR, value); }
        }

        private string _AppTitle;
        public string AppTitle
        {
            get { return _AppTitle; }
            set { SetProperty(ref _AppTitle, value); }
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
                PreviewdMUCommand.RaiseCanExecuteChanged();
                PreviewDRCommand.RaiseCanExecuteChanged();
                PreviewGSCommand.RaiseCanExecuteChanged();

            }
        }



        private PlotModel _DRPlot;

        public PlotModel DRPlot
        {
            get { return _DRPlot; }
            set { SetProperty(ref _DRPlot, value); }
        }

        // Cosmo views
        private CosmoTrans _View1;

        public CosmoTrans View1
        {
            get { return _View1; }
            set { SetProperty(ref _View1, value); }
        }

        private CosmoCoro _View2;

        public CosmoCoro View2
        {
            get { return _View2; }
            set { SetProperty(ref _View2, value); }
        }

        private CosmoSag _View3;

        public CosmoSag View3
        {
            get { return _View3; }
            set { SetProperty(ref _View3, value); }
        }

        private LinearAxis DRAxis;

        private LinearAxis dMUAxis;

        private LinearAxis GSAxis;

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


        public MainViewModel(Application app, Patient patient, Course course, PlanSetup plan)
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
            OnBeamSelectCommand = new DelegateCommand(OnBeamSelect, CanBeamSelect);


            AppTitle = "Doserate Editor";
            if (!(ConfigurationManager.AppSettings["Validated"] == "false"))
            {
                AppTitle += " *** NOT VALIDATED FOR CLINICAL USE ***";
            }
            var ver = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            AppTitle += " ";
            AppTitle += ver;




            Courses = new ObservableCollection<CourseModel>();
            Plans = new ObservableCollection<PlanningItem>();
            Beams = new ObservableCollection<Beam>();

            CreditText = "";

            // DR PLOT
            DRPlot = new PlotModel { Title = "Doserate and Gantry Speed"};
            //DRPlot.Legends.Add(new
            //{
            //    LegendTitle = "Legend",
            //    LegendPosition = LegendPosition.RightBottom,
            //});

            // Instantiate cosmo views
            View1 = new CosmoTrans();
            View2 = new CosmoCoro();
            View3 = new CosmoSag();
            
            // Create the different axes with respect/ive keys
            DRAxis = new LinearAxis
            {
                IsAxisVisible = true,
                Title = "Doserate (MU/min)",
                Position = AxisPosition.Left,
                Key = "DRAxis",
                TitleColor = OxyColor.Parse("#e60909"),
                AbsoluteMinimum =0,
                AbsoluteMaximum=2500
            };
            DRPlot.Axes.Add(DRAxis);

            GSAxis = new LinearAxis
            {
                Title = "Gantry Speed (deg/sec)",
                Position = AxisPosition.Right,
                Key = "GSAxis",
                TitleColor = OxyColor.Parse("#3283a8"),
                AbsoluteMinimum =0,
                AbsoluteMaximum=6,
                Minimum=0,
                Maximum=6
            };
            DRPlot.Axes.Add(GSAxis);

            dMUAxis = new LinearAxis
            {
                //Title = "Delta MU",
                IsAxisVisible = false,
                Position = AxisPosition.Left,
                TickStyle = TickStyle.None,
                Key = "dMUAxis"
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

            DRMethods = new ObservableCollection<Tuple<Nullable<DRMethod>, Nullable<bool>>>
            {
               new Tuple<Nullable<DRMethod>, Nullable<bool>>(DRMethod.Sin, true),
               new Tuple<Nullable<DRMethod>, Nullable<bool>>(DRMethod.Cosmic, true),
               new Tuple<Nullable<DRMethod>, Nullable<bool>>(DRMethod.Parabola, true),
               new Tuple<Nullable<DRMethod>, Nullable<bool>>(DRMethod.Juha, false)
            };


            PatientId = patient.Id;
            OpenPatient(patient);
            SelectedCourse = Courses.FirstOrDefault(x => x.Id == course.Id);
            OnSelectCourse();
            SelectedPlan = Plans.FirstOrDefault(x => x.Id == plan.Id) as ExternalPlanSetup;
            OnPlanSelect();

        }

        private void OnBeamSelect()
        {
            if (SelectedBeam == null)
            {
                return;
            }

            // Clear all DR related plots (3 of them!)
            ResetPlot();

            // Check what is checked and Replot if needed
            OnCurrentdMU();
            OnCurrentDR();
            OnCurrentGS();

            // Cosmoplot stuff ...
            // View1 = Trans, View2 = Coro, View3 = Sag
            View1.ClearPlot();
            View2.ClearPlot();
            View3.ClearPlot();

            var dMU = DRCalc.InitialdMU[SelectedBeam.Id];
            var angles = Utils.GetBeamAngles(SelectedBeam);

            //View1.DrawRects(dMU.Select(x => x.Y).ToList(), 5, 5, 5);
            //var smallangle = Math.Min(angles.Item1.First(), angles.Item1.Last());
            //var largeangle = Math.Max(angles.Item1.First(), angles.Item1.Last());

            var deltaMU = dMU.Select(x => x.Y).ToList();
            View3.DrawRects(deltaMU, angles.Item1.First(), angles.Item1.Last(), angles.Item3[0], angles.Item5);
            View2.DrawAngle(angles.Item3[0]);
            View1.DrawRects(deltaMU, angles.Item1.First(), angles.Item1.Last(), angles.Item3[0], angles.Item5);

            // Update all DR, GS, dMU plots
            OnPreviewGS();
            OnPreviewDR();
            //OnPreviewdMU();
            


        }

        private bool CanBeamSelect()
        {
            return true;
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

        // function to set axis range to avoid small fluctuations
        private void PlotWithScale(LinearAxis axis, LineSeries series, PlotModel plot, List<DataPoint> pts, double tolerance)
        {
            var yvals = pts.Select(pt => pt.Y);
            var range = Math.Abs(yvals.Max() - yvals.Min());

            var axismin = yvals.Min();
            var axismax = yvals.Max();
            if (range < tolerance)
            {
                axismin = yvals.Average() - 50;
                axismax = yvals.Average() + 50;
            }
            axis.Minimum = axismin;
            axis.Maximum = axismax;
            series.Points.AddRange(pts);
            plot.InvalidatePlot(true);
        }


        private void OnCurrentdMU() {

            // Clear line plot of dMU
            dMU0_series.Points.Clear();


            if (CurrentdMU)
            {
                var dMU = DRCalc.InitialdMU[SelectedBeam.Id];
                PlotWithScale(dMUAxis, dMU0_series, DRPlot, dMU, 3);

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

            // Update Credit Text
            CreditText = DRCalc.DRCreditsString;

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
                //GS0_series.Points.AddRange(DRCalc.InitialGSs[SelectedBeam.Id]);
                PlotWithScale(GSAxis, GS0_series, DRPlot, DRCalc.InitialGSs[SelectedBeam.Id], 3);
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
                var DRf = DRCalc.FinalDRs[SelectedBeam.Id];
                PlotWithScale(DRAxis, DRf_series, DRPlot, DRf, 3);
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
                var GSf = DRCalc.FinalGSs[SelectedBeam.Id];
                PlotWithScale(GSAxis, GSf_series, DRPlot, GSf, 3);

            }
            else
            {
                GSf_series.Points.Clear();
            }
            

            
            DRPlot.InvalidatePlot(true);

        }

        private bool CanPreviewGS() {
            var res = SelectedMethod.HasValue && (SelectedPlan != null);
            return res; 
        }

        private void OnCurrentDR()
        {
            if (CurrentDR) // If checkbox is checked
            {
                var DR0 = DRCalc.InitialDRs[SelectedBeam.Id];
                PlotWithScale(DRAxis, DR0_series, DRPlot, DR0, 3);
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
            // Unselect Beam
            _SelectedBeam = null;

            // Create a DR calculator for selected plan
            DRCalc = new DRCalculator(SelectedPlan);

            // Clear the previous initial and final dr/gs plots
            DR0_series.Points.Clear();
            DRf_series.Points.Clear();
            GSf_series.Points.Clear();

            // Clear prev list of beams
            Beams.Clear();
            bool isEmpty = true;

            foreach(var bm in SelectedPlan.Beams) {
                if (bm.Technique.Id.ToLower().Contains("arc"))
                {
                    Beams.Add(bm);
                    isEmpty = false;
                } 
            }
            if (isEmpty)
            {
                System.Windows.MessageBox.Show($"Plan {SelectedPlan.Id} does not contain any fields with SRS ARC or ARC technique");

            }
            ResetPlot();


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
            OpenPatient(pat);

        }

        private void OpenPatient(Patient pat)
        {
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
            if (SelectedCourse != null) {
                foreach (var p in SelectedCourse.Plans.Values)
                {
                    Plans.Add(p);
                }
            }

            ResetPlot();
            

            Beams.Clear();


        }

        private bool CanSelectCourse()
        {
            return true;
        }


        private void ResetPlot()
        {
            DR0_series.Points.Clear();
            DRf_series.Points.Clear();
            GS0_series.Points.Clear();
            GSf_series.Points.Clear();
            dMU0_series.Points.Clear();
            dMUf_series.Points.Clear();

            //CurrentDR = false;
            //PreviewDR = false;
            //CurrentGS = false;
            //PreviewGS = false;
            //CurrentdMU = false;
            //PreviewdMU = false;

            DRPlot.InvalidatePlot(true);
        }

      

    }
}
