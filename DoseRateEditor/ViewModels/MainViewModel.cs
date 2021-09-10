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
        public DelegateCommand OnPlanSelectCommandd { get; private set; }
        public DelegateCommand OnMethodSelectCommand { get; private set; }
        public DelegateCommand PrevBeamCommand { get; private set; }
        public DelegateCommand NextBeamCommand { get; private set; }
        public DelegateCommand PlotCurrentDRCommand { get; private set; }
        public DelegateCommand PlotCurrentGSCommand { get; private set; }
        public DelegateCommand PreviewGSCommand { get; private set; }
        public DelegateCommand PreviewDRCommand { get; private set; }
        public DelegateCommand PreviewdMUCommand { get; private set; }
        public DelegateCommand PlotCurrentdMUCommand { get; private set; }

        public IViewCommand<OxyMouseWheelEventArgs> TransScrollCommand { get; private set; } 
        public ObservableCollection<CourseModel> Courses { get; private set; }
	    public ObservableCollection<PlanningItem> Plans { get; private set; }
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


        private int _BeamIdx;

        public int BeamIdx
        {
            get { return _BeamIdx; }
            set 
            { 
                SetProperty(ref _BeamIdx, value);
                PrevBeamCommand.RaiseCanExecuteChanged();
                NextBeamCommand.RaiseCanExecuteChanged();
            }

        }

        private string _CurrentBeamId;
        public string CurrentBeamId { 
            get => _CurrentBeamId; 
            set => SetProperty(ref _CurrentBeamId, value);
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
                PrevBeamCommand.RaiseCanExecuteChanged();
                NextBeamCommand.RaiseCanExecuteChanged();
                PreviewDRCommand.RaiseCanExecuteChanged();
                PlotCurrentDRCommand.RaiseCanExecuteChanged();
                PreviewGSCommand.RaiseCanExecuteChanged();
                PlotCurrentGSCommand.RaiseCanExecuteChanged();
                PreviewdMUCommand.RaiseCanExecuteChanged();
                PlotCurrentdMUCommand.RaiseCanExecuteChanged();
            }
        }


        private PlotModel _DRPlot;

        public PlotModel DRPlot
        {
            get { return _DRPlot; }
            set { SetProperty(ref _DRPlot, value); }
        }

        private PlotModel _View1;

        public PlotModel View1
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
            OnPlanSelectCommandd = new DelegateCommand(OnPlanSelect, CanPlanSelect);
            OnMethodSelectCommand = new DelegateCommand(OnMethodSelect, CanMethodSelect);
            NextBeamCommand = new DelegateCommand(OnNextBeam, CanNextBeam);
            PrevBeamCommand = new DelegateCommand(OnPrevBeam, CanPrevBeam);
            TransScrollCommand = new DelegatePlotCommand<OxyMouseWheelEventArgs>(OnScroll);
            PlotCurrentDRCommand = new DelegateCommand(OnCurrentDR, CanCurrentDR);
            PreviewDRCommand = new DelegateCommand(OnPreviewDR, CanPreviewDR);
            PreviewGSCommand = new DelegateCommand(OnPreviewGS, CanPreviewGS);
            PlotCurrentGSCommand = new DelegateCommand(OnCurrentGS, CanCurrentGS);
            PreviewdMUCommand = new DelegateCommand(OnPreviewdMU, CanPreviewdMU);
            PlotCurrentdMUCommand = new DelegateCommand(OnCurrentdMU, CanCurrentdMU);
    

            Courses = new ObservableCollection<CourseModel>();
            Plans = new ObservableCollection<PlanningItem>();

            // DR PLOT
            DRPlot = new PlotModel { Title = "Doserate and Gantry Speed"};

            // Cosmo view 1
            View1 = BuildCosmoPlot();

            // Create the different axes with respective keys
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
            using (var stream = assembly.GetManifestResourceStream("DoseRateEditor.Resources.cosmo1.PNG"))
            {
                image = new OxyImage(stream);
            }

            // Centered in plot area, filling width
            plt.Annotations.Add(new ImageAnnotation
            {
                ImageSource = image,
                Opacity = 1,
                Interpolate = false,
                X = new PlotLength(0.5, PlotLengthUnit.RelativeToPlotArea),
                Y = new PlotLength(0.5, PlotLengthUnit.RelativeToPlotArea),
                Width = new PlotLength(1, PlotLengthUnit.RelativeToPlotArea),
                HorizontalAlignment = OxyPlot.HorizontalAlignment.Center,
                VerticalAlignment = OxyPlot.VerticalAlignment.Middle
            });

            return plt;
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

       
            /*private double[][,] BuildCTMatrix()
        {
            var image = SelectedPlan.StructureSet.Image;
            //var mtx = new double[image.XSize, image.YSize, image.ZSize];
            var mtx = new List<double[,]>();
            for(int sl = 0; sl < image.ZSize; sl++)
            {
                mtx.Add(GetCTData(sl)); 
            }
            return mtx.ToArray();
        }*/

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

        //private double[,] ConvertToCTMatrix(int[,] ints)
        //{
            //var ct = SelectedPlan.StructureSet.Image.Series.Images;
            //var ctMatrix = new double[ct.First().XSize, ct.First().YSize];
            //for (int i = 0; i < ct.First().XSize; i++)
            //    for (int j = 0; j < ct.First().YSize; j++)
            //        ctMatrix[i, j] = (double)ct.First().VoxelToDisplayValue(ints[i, j]);
            //return ctMatrix;
         
        //}

        private void OnNextBeam()
        {
            BeamIdx++;

            // Clear Inital DR plot
            DR0_series.Points.Clear();
            CurrentBeamId = DRCalc.InitialDRs.Keys.ToList()[BeamIdx];
            DR0_series.Points.AddRange(DRCalc.InitialDRs[CurrentBeamId]);
            
            // If we have a method selected, plot the final dr too
            if (SelectedMethod.HasValue)
            {
                DRf_series.Points.Clear();
                DRf_series.Points.AddRange(DRCalc.FinalDRs[CurrentBeamId]);
            }
        }

        private bool CanNextBeam()
        {
            if (SelectedPlan == null)
            {
                return false;
            }

            if (DRCalc == null)
            {
                return false;
            }

            return BeamIdx + 1 < DRCalc.numbeams;
        }

        private void OnPrevBeam()
        {
            // Move beam idx      
            BeamIdx--;

            DR0_series.Points.Clear();
            CurrentBeamId = DRCalc.InitialDRs.Keys.ToList()[BeamIdx];

            // Clear Inital DR plot
            DR0_series.Points.Clear();
            CurrentBeamId = DRCalc.InitialDRs.Keys.ToList()[BeamIdx];
            DR0_series.Points.AddRange(DRCalc.InitialDRs[CurrentBeamId]);

            // If we have a method selected, plot the final dr too
            if (SelectedMethod.HasValue)
            {
                DRf_series.Points.Clear();
                DRf_series.Points.AddRange(DRCalc.FinalDRs[CurrentBeamId]);
            }
        }

        private void OnCurrentdMU() {
            dMU0_series.Points.Clear();
            if (CurrentdMU)
            {
                dMU0_series.Points.AddRange(DRCalc.InitialdMU[CurrentBeamId]);
            }
            DRPlot.InvalidatePlot(true);
        }

        private bool CanCurrentdMU() { return CanCurrentDR(); }

        private void OnPreviewdMU() { return; }

        private bool CanPreviewdMU() { return false; }

        private bool CanPrevBeam()
        {
            if (SelectedPlan == null)
            {
                return false;
            }
            return BeamIdx > 0;
        }


        private void OnMethodSelect()
        {
            // Clear final gs and dr series
            GSf_series.Points.Clear();
            DRf_series.Points.Clear();

            // Calculate final dr and gs
            DRCalc.CalcFinalDR(SelectedPlan, SelectedMethod.Value);

            CurrentBeamId = DRCalc.FinalDRs.Keys.ToList()[BeamIdx];

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
                GS0_series.Points.AddRange(DRCalc.InitialGSs[CurrentBeamId]);
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
                DRf_series.Points.AddRange(DRCalc.FinalDRs[CurrentBeamId]);
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
                GSf_series.Points.AddRange(DRCalc.FinalGSs[CurrentBeamId]);
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
                DR0_series.Points.AddRange(DRCalc.InitialDRs[CurrentBeamId]);
            }
            else
            {
                DR0_series.Points.Clear();
            }
            DRPlot.InvalidatePlot(true);
        }

        private bool CanCurrentDR() { return SelectedPlan != null; }

        


        private void OnPlanSelect()
        {
            // Set beam index to first beam
            BeamIdx = 0; 

            // Create a DR calculator for selected plan
            DRCalc = new DRCalculator(SelectedPlan);

            // Clear the previous initial and final dr/gs plots

            DR0_series.Points.Clear();
            DRf_series.Points.Clear();
            GSf_series.Points.Clear();
            
            CurrentBeamId = DRCalc.InitialDRs.Keys.ToList()[BeamIdx];

            //DR0_series.Points.AddRange(DRCalc.InitialDRs[CurrentBeamId]);
            //DRPlot.InvalidatePlot(true);

            // Reset transaxial plot
            SelectedSlice = 0;
            UpdateSeries(TransPlot);

            /* REPLACE WITH DELEGATE FUNCTION CALLS FOR PLOTTING BASED ON CHECK BOX status
            // IF selected method is not null calc new DR
            if (SelectedMethod != null)
            {
                DRCalc.CalcFinalDR(SelectedPlan, SelectedMethod.Value);
                DRf_series.Points.AddRange(DRCalc.FinalDRs[CurrentBeamId]);
                DRPlot.InvalidatePlot(true);
            }
            
            // Update transplot
            SelectedSlice = 0;
            UpdateSeries(TransPlot);
            */

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

        }

        private bool CanSelectCourse()
        {
            return true;
        }


    }
}
