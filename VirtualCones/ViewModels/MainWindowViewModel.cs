using AOS_VirtualCones_MCB.Helpers;
using AOS_VirtualCones_MCB.Models;
using AOS_VirtualCones_MCB.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Runtime;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.RightsManagement;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Serialization;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using static AOS_VirtualCones_MCB.Models.DRCalculator;

[assembly: ESAPIScript(IsWriteable = true)]

namespace AOS_VirtualCones_MCB.ViewModels
{

    public class MainWindowViewModel : ObservableObject
    {
      
        public MainWindowViewModel()
        {
            TestForNecessaryFiles();
            LoadBeamTemplates();
            ImportSettings();
            GetGantryWeightMaps();

            GSf_series = new LineSeries();
            DRf_series = new LineSeries();
        }

        private void TestForNecessaryFiles()
        {

            List<string> missingItems = new List<string>();
            List<string> filePaths = new List<string>();
            List<string> dirPaths = new List<string>();
            // Get directory of executable
            string executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string executableFolder = Path.GetDirectoryName(executablePath);


            if (!File.Exists(Path.Combine(executableFolder, "BeamTemplateCollections.xml")))
            {
                CreateNewBeamCollectionsFile();

                MessageBox.Show("The BeamTemplateCollections.xml file was not found.  An empty one was created.",
                    "BeamTemplateCollections.xml", MessageBoxButton.OK, MessageBoxImage.Information);
            }


            if (!File.Exists(Path.Combine(executableFolder, "Settings.xml")))
            {
                CreateNewSettingsFile();
                MessageBox.Show("The Settings.xml file was not found.  An default one was created.",
                    "Settings.xml", MessageBoxButton.OK, MessageBoxImage.Information);
            }


            if (!Directory.Exists(Path.Combine(executableFolder, "Maps")))
            {
                Directory.CreateDirectory(Path.Combine(executableFolder, "Maps"));
                MessageBox.Show("The MAPS directory was not found.  An empty one was created.",
                        "MAPS", MessageBoxButton.OK, MessageBoxImage.Information);
            }


            if(missingItems.Count > 0)
            {
                MessageBox.Show($"The following items are missing:\n{string.Join("\n", missingItems)}", "Missing Items", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void writetest()
        {
            GantryWeightMap map = new GantryWeightMap();
            map.MapId = "Test Map";

            for (int i = 0; i <= 180; i += 2)
            {
                GantryWeightPair temp = new GantryWeightPair();
                temp.Gantry = i;
                double iPi = i / 360.0 * 2 * Math.PI;
                temp.Weight = Math.Sin(iPi);
                map.pairs.Add(temp);
            }

            for (int i = 182; i <= 358; i += 2)
            {
                GantryWeightPair temp = new GantryWeightPair();
                temp.Gantry = i;
                double iPi = i / 360.0 * 2 * Math.PI;
                temp.Weight = -Math.Sin(iPi);
                map.pairs.Add(temp);
            }

            // Save gantry weight pairs to an XML
            XmlSerializer serializer = new XmlSerializer(typeof(GantryWeightMap));
            using (TextWriter writer = new StreamWriter("test_map.xml"))
            {
                serializer.Serialize(writer, map);
            }
        }

        public VMS.TPS.Common.Model.API.Application _esapiX;

        private Visibility _testPatientVisibility;

        public Visibility TestPatientVisibility
        {
            get => _testPatientVisibility;
            set => SetProperty(ref _testPatientVisibility, value);
        }

        public static string ParentDirectory;

        private string _searchText;

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
            }
        }

        public ICommand OpenGuideCommand => new RelayCommand(OpenGuide);

        private void OpenGuide()
        {
            string guidePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VirtualCone_Guide.pdf");
            Process.Start(guidePath);
        }

        public ICommand RefreshPatientCommand => new RelayCommand(RefreshPatient);
        public ICommand FilterPatientMainCommand => new RelayCommand(UpdateFilteredPatientIDs);
        public bool expandPatientComboBox = true;
        private bool _ptCBOOpen;

        public void RefreshPatient()
        {
            string ptID = PatientID;
            PatientID = null;
            PatientID = ptID;
        }

        public bool PtCBOOpen
        {
            get { return _ptCBOOpen; }
            set { SetProperty(ref _ptCBOOpen, value); }
        }

        public void UpdateFilteredPatientIDs()
        {
            if (!string.IsNullOrEmpty(SearchText))
            {
                FilteredAvailablePatientIDs = new ObservableCollection<string>(_esapiX.PatientSummaries.Select(x => x.Id).Where(patientId =>
                        patientId.ToLower().Contains(SearchText.ToLower())));

                if (expandPatientComboBox)
                {
                    PtCBOOpen = true;
                }

                expandPatientComboBox = true;
                OnPropertyChanged(nameof(FilteredAvailablePatientIDs));
            }
        }

        private ObservableCollection<string> _filteredAvailablePatientIDs = new ObservableCollection<string>();

        public ObservableCollection<string> FilteredAvailablePatientIDs
        {
            get { return _filteredAvailablePatientIDs; }
            set
            {
                if (value != _filteredAvailablePatientIDs)
                {
                    _filteredAvailablePatientIDs = value;
                    OnPropertyChanged(nameof(FilteredAvailablePatientIDs));
                }
            }
        }

        private string _patientID;

        public string PatientID
        {
            get { return _patientID; }
            set
            {
                if (_patientID != value)
                {
                    _patientID = value;
                    OnPropertyChanged("PatientID");
                }

                UpdateCourses();
            }
        }

        private string _planStructureSetId;

        public string PlanStructureSetId
        {
            get { return _planStructureSetId; }
            set
            {
                SetProperty(ref _planStructureSetId, value);
            }
        }

        private string _courseId;

        public string CourseId
        {
            get { return _courseId; }
            set
            {
                _courseId = value;
                OnPropertyChanged("CourseId");
                //ReportMatches("BeforeUpdatePlanIds1932");
                UpdatePlanIds();
                //ReportMatches("AfterUpdatePlanIds1932");
            }
        }

        private ObservableCollection<string> _availableCourseIds = new ObservableCollection<string>();

        public ObservableCollection<string> AvailableCourseIds
        {
            get
            {
                return _availableCourseIds;
            }
            set
            {
                _availableCourseIds = value;
                OnPropertyChanged("AvailableCourseIds");
            }
        }

        private ObservableCollection<string> _availablePlanIds = new ObservableCollection<string>();

        public ObservableCollection<string> AvailablePlanIds
        {
            get
            {
                return _availablePlanIds;
            }
            set
            {
                _availablePlanIds = value;
                OnPropertyChanged("AvailablePlanIds");
            }
        }

        private string _planId;

        public string PlanId
        {
            get { return _planId; }
            set
            {
                _planId = value;
                OnPropertyChanged("PlanId");
                UpdateStructureSetLabel();
                UpdateNumOfBeams();
                FilterBeamTemplates();
            }
        }

        private ObservableCollection<PlanInfo> _planInfos = new ObservableCollection<PlanInfo>();

        public ObservableCollection<PlanInfo> PlanInfos
        {
            get { return _planInfos; }
            set
            {
                SetProperty(ref _planInfos, value);
            }
        }

        private PlanInfo _selectedPlanInfo = new PlanInfo();

        public PlanInfo SelectedPlanInfo
        {

            get { return _selectedPlanInfo; }
            set
            {
                SetProperty(ref _selectedPlanInfo, value);
                try
                {
                    if (SelectedPlanInfo != null)
                    {
                        DVHListLabel = $"DVHs for {SelectedPlanInfo.PatientId}>{SelectedPlanInfo.CourseId}{SelectedPlanInfo.PlanId}";
                    }
                    else
                    {
                        DVHListLabel = "Select Plan...";
                    }
                }
                catch
                {
                    DVHListLabel = "";
                }
            }
        }

        private string _dVHListLabel = "Select Plan...";

        public string DVHListLabel
        {
            get { return _dVHListLabel; }
            set
            {
                SetProperty(ref _dVHListLabel, value);
            }
        }

        private void UpdateStructureSetLabel()
        {
            if (PatientID != null && PatientID != "" && CourseId != null && CourseId != "" && PlanId != null && PlanId != "")
            {
                try
                {
                    _esapiX.ClosePatient();
                    var pat = _esapiX.OpenPatientById(PatientID);
                    var course = pat.Courses.First(x => x.Id.Equals(CourseId));
                    var plan = course.PlanSetups.First(x => x.Id.Equals(PlanId));
                    PlanStructureSetId = plan.StructureSet.Id;
                }
                catch (Exception ex)
                {
                }
            }
        }

        private void UpdateNumOfBeams()
        {
            if (PatientID != null && PatientID != "" && CourseId != null && CourseId != "" && PlanId != null && PlanId != "")
            {
                try
                {
                    _esapiX.ClosePatient();
                    var pat = _esapiX.OpenPatientById(PatientID);
                    var course = pat.Courses.First(x => x.Id.Equals(CourseId));
                    var plan = course.PlanSetups.First(x => x.Id.Equals(PlanId));
                    NumberOfBeams = plan.Beams.Count(x => !x.IsSetupField);
                }
                catch (Exception ex)
                {
                }
            }
        }

        private bool IsInsertBeams;
        private void UpdateCourses()
        {
            string holdCourseName = "";
            bool repopulateCourseName = false;
            if (CourseId != null)
            {
                repopulateCourseName = true;
                holdCourseName = CourseId;
            }

            string holdPlanName = "";
            bool repopulatePlanName = false;
            if (PlanId != null)
            {
                repopulatePlanName = true;
                holdPlanName = PlanId;
            }

            if (PatientID != null)
            {
                _esapiX.ClosePatient();
                var patient = _esapiX.OpenPatientById(PatientID);
                AvailableCourseIds = new ObservableCollection<string>();
                AvailableCourseIds.Clear();
                AvailablePlanIds = new ObservableCollection<string>();
                AvailablePlanIds.Clear();
                AvailableCourseIds = new ObservableCollection<string>(patient.Courses.Select(x => x.Id).ToList());

                _esapiX.ClosePatient();
            }

            if (repopulateCourseName)
            {
                try
                {
                    CourseId = AvailableCourseIds.First(id => id.Equals(holdCourseName));
                }
                catch
                {
                    // CourseId = "Auto";
                }
                CourseId = holdCourseName;
            }
            if (repopulatePlanName)
            {
                try
                {
                    PlanId = AvailablePlanIds.First(id => id.Equals(holdPlanName));
                }
                catch
                {
                    // PlanId = "Auto";
                }
            }
        }

        private void UpdatePlanIds()
        {
            if (PatientID != null)
            {
                if (CourseId != null && CourseId != "")
                {
                    _esapiX.ClosePatient();
                    var patient = _esapiX.OpenPatientById(PatientID);

                    if (patient.Courses.Count(x => x.Id.Equals(CourseId)) > 0)
                    {
                        var course = patient.Courses.First(x => x.Id.Equals(CourseId));

                        if (course != null)
                        {
                            AvailablePlanIds = new ObservableCollection<string>(course.PlanSetups.Select(x => x.Id).ToList());
                        }
                        else
                        {
                            AvailablePlanIds = new ObservableCollection<string>();
                            AvailablePlanIds.Clear();
                        }
                    }
                    else
                    {
                        AvailablePlanIds = new ObservableCollection<string>();
                        AvailablePlanIds.Clear();
                    }
                    _esapiX.ClosePatient();
                }
            }
        }

        public ICommand ExtractCommand => new RelayCommand(DefaultPlan);

        public void DefaultPlan()
        {
            SearchText = "0025";
            UpdateFilteredPatientIDs();
            PatientID = "USLV-CS-0025";
            CourseId = "VC";
            PlanId = "VC";
        }

        public ICommand InsertBeamsCommand => new RelayCommand(InsertBeams);

        public ICommand CreateTemplateCommand => new RelayCommand(CreateBeamTemplate);
        public ICommand UpdateTemplateCommand => new RelayCommand(UpdateTemplate);

        public ICommand DeleteTemplateCommand => new RelayCommand(DeleteBeamTemplate);
        //public ICommand EditDRCommand => new RelayCommand(AssignDoseRates);

        private BeamTemplate _selectedBeamTemplate = new BeamTemplate();

        public BeamTemplate SelectedBeamTemplate
        {
            get { return _selectedBeamTemplate; }
            set 
            { 
                SetProperty(ref _selectedBeamTemplate, value);
            }
        }

        private bool _beamTemplateCreationMode;
        public bool BeamTemplateCreationMode
        {
            get => _beamTemplateCreationMode;
            set
                { SetProperty(ref _beamTemplateCreationMode, value); FilterBeamTemplates(); }
        }

        private ObservableCollection<BeamTemplate> _beamTemplates = new ObservableCollection<BeamTemplate>();

        public ObservableCollection<BeamTemplate> BeamTemplates
        {
            get { return _beamTemplates; }
            set
            {
                SetProperty(ref _beamTemplates, value);
            }
        }


        private ObservableCollection<BeamTemplate> _filteredBeamTemplates = new ObservableCollection<BeamTemplate>();

        public ObservableCollection<BeamTemplate> FilteredBeamTemplates
        {
            get { return _filteredBeamTemplates; }
            set
            {
                SetProperty(ref _filteredBeamTemplates, value);
            }
        }

        private ObservableCollection<GapPair> _filteredAvailableGaps = new ObservableCollection<GapPair>();

        public ObservableCollection<GapPair> FilteredAvailableGaps
        {
            get { return _filteredAvailableGaps; }
            set
            {
                SetProperty(ref _filteredAvailableGaps, value);
            }
        }

        private void FilterBeamTemplates()
        {
            if(IsInsertBeams)
            {
                return;
            }
            try
            {
                if (string.IsNullOrEmpty(PatientID)) return;
                if (string.IsNullOrEmpty(CourseId)) return;
                if (string.IsNullOrEmpty(PlanId)) return;

                // Clear existing filtered collection
                _filteredBeamTemplates.Clear();
                _filteredAvailableGaps.Clear();

                // Assuming you have a method to get the EnergyMode from the beams
                string energyMode = GetEnergyModeFromPlan();


                if (!BeamTemplateCreationMode)
                {
                    // Filter BeamTemplates by EnergyMode and add to the filtered collection
                    foreach (var beamTemplate in _beamTemplates.
                        Where(bt => bt.GapSize != null && bt.GapSize.EnergyMode != null).
                        Where(bt => bt.GapSize.EnergyMode.ToUpper() == energyMode.ToUpper()))
                    {
                        _filteredBeamTemplates.Add(beamTemplate);
                    }

                    foreach(var gap in GapSettings.AvailableGapsMM.Where(x=>x.EnergyMode.ToUpper().Equals(energyMode.ToUpper())))
                    {                        
                        _filteredAvailableGaps.Add(gap);
                    }

                }
                else
                {
                    foreach (var beamTemplate in _beamTemplates)
                    {
                        _filteredBeamTemplates.Add(beamTemplate);
                    }

                    foreach (var gap in GapSettings.AvailableGapsMM)
                    {
                        _filteredAvailableGaps.Add(gap);
                    }
                }            
            }
            catch(Exception ex)
            {

            }
        }


        public string GetEnergyModeFromPlan()
        {
            try
            {
                _esapiX.ClosePatient();
                var firstBeam = _esapiX.OpenPatientById(PatientID).Courses.
                    Single(x => x.Id.ToUpper().Equals(CourseId.ToUpper())).PlanSetups.
                    Single(x => x.Id.ToUpper().Equals(PlanId.ToUpper())).Beams.First(x => !x.IsSetupField && !x.IsImagingTreatmentField);
                var energyMode = firstBeam.EnergyModeDisplayName;

                _esapiX.ClosePatient();
                return energyMode;
            }
            catch (Exception ex)
            {
            }

            return "";
        }

        private GapSettings _gapSettings;

        public GapSettings GapSettings
        {
            get { return _gapSettings; }
            set
            {
                SetProperty(ref _gapSettings, value);
                UpdateSettingsLabel();
            }
        }

        private void UpdateSettingsLabel()
        {
            if (GapSettings != null)
            {
                SettingsLabel = $"X-Jaw: {GapSettings.X}mm\n" +
                    $"Y-Jaw: {GapSettings.Y}mm\n" +
                   // $"Gap Width: {GapSettings.GapSize}mm\n" +
                    $"Sliding Gap Enabled: {GapSettings.EnableSlidingLeaf}\n" +
                    $"Sliding Gap Width: {GapSettings.SlidingLeafGapSize}mm";
            }
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

        //private Nullable<DRMethod> _SelectedMethod;

        //public Nullable<DRMethod> SelectedMethod
        //{
        //    get { return _SelectedMethod; }
        //    set
        //    {
        //        SetProperty(ref _SelectedMethod, value);
        //        UpdateCredit();
        //    }
        //}

        public int _numOfBeams;

        public int NumberOfBeams
        {
            get { return _numOfBeams; }
            set
            {
                SetProperty(ref _numOfBeams, value);
            }
        }

        //        private void UpdateCredit()
        //        {
        //            string sincred =    "Richard A. Popple, Xingen Wu, Ivan A. Brezovich,\n" +
        //                                "James M. Markert, Barton L. Guthrie,\n"+
        //                                "Evan M. Thomas, Markus Bredel, John B. Fiveash,\n"+
        //                                "The virtual cone: A novel technique to generate\n"+
        //                                "spherical dose distributions using\n"+
        //                               "a multileaf collimator and standardized control-point\n"+
        //                                "sequence for small target radiation surgery,\n"+
        //                                "Advances in Radiation Oncology,\n"+
        //                                "Volume 3, Issue 3,\n"+
        //                                "2018,\n"+
        //                                "Pages 421-430,\n"+
        //                                "ISSN 2452-1094,\n"+
        //                                "https://doi.org/10.1016/j.adro.2018.02.011\n"+
        //                                "(https://www.sciencedirect.com/science/article/pii/S2452109418300368)"
        //                                 + "\n\nDR(gantry) = Math.Sin((gantry * Math.PI) / 180);";

        //            string bfstring =
        //@"public static double BFFunc(double th_deg)
        //{
        //    var theta = (th_deg * Math.PI) / 180;
        //    var retval = 16 * theta * (Math.PI - theta);
        //    var denom = 5 * Math.PI * Math.PI;
        //    denom -= 4 * theta * (Math.PI - theta);
        //    retval /= denom;

        //    if(th_deg < 180)
        //    {
        //        return retval;
        //    }
        //    else
        //    {
        //        return -1 * BFFunc(th_deg - 180);
        //    }
        //}
        //";

        //            string cosstring =
        //@"public static double cosmicFunc(double th_deg)
        //{
        //    if(th_deg < 180)
        //    {
        //        return (th_deg * (180 - th_deg)) / (90 * 90);
        //    }
        //    else
        //    {
        //        return cosmicFunc(th_deg - 180);
        //    }
        //}
        //";

        //            string BF_cred = $"https://digitalcommons.ursinus.edu/cgi/viewcontent.cgi\n?article=1015&context=triumphs_calculus\n\n{bfstring}";
        //            string cosmic_cred = $"https://en.formulasearchengine.com/wiki\n/Small-angle_approximation\n\n{cosstring}";

        //            string juhacred = @"Method and apparatus to deliver therapeutic radiation to a patient using field geography-based dose optimization
        //            Inventors: Juha Kauppinen Anthony Magliari Martin SABEL Amir Talakoub.
        //            (https://patents.google.com/patent/WO2022063684A1)";

        //            string QACred = $"WARNING: This is only used for QA. Control Point\ndose rates are alternated 1.0 and 0.95.";

        //            switch (SelectedMethod)
        //            {
        //                case DRMethod.Sine:
        //                    CreditText = sincred;
        //                    break;
        //                case DRMethod.Cosmic:
        //                    CreditText = cosmic_cred;
        //                    break;
        //                case DRMethod.Bhaskara:
        //                    CreditText = BF_cred;
        //                    break;
        //                case DRMethod.QA:
        //                    CreditText = QACred;
        //                    break;
        //                default:
        //                    CreditText = "N/A";
        //                    break;
        //            }
        //        }

        //        private string _CreditText;

        //        public string CreditText
        //        {
        //            get { return _CreditText; }
        //            set { SetProperty(ref _CreditText, value); }
        //        }

        private string _SettingsLabel;

        public string SettingsLabel
        {
            get { return _SettingsLabel; }
            set { SetProperty(ref _SettingsLabel, value); }
        }

        //public ObservableCollection<DRMethod> DRMethods { get; private set; }

        //private ExternalPlanSetup _SelectedPlan;
        //public ExternalPlanSetup SelectedPlan
        //{
        //    get { return _SelectedPlan; }
        //    set
        //    {
        //        SetProperty(ref _SelectedPlan, value);
        //    }
        //}

        private static string defaultTemplateId = "Template Id";

        private string _newBeamTemplateId = defaultTemplateId;

        public string NewBeamTemplateId
        {
            get { return _newBeamTemplateId; }
            set { SetProperty(ref _newBeamTemplateId, value); }
        }

        private void DeleteBeamTemplate()
        {
            if (MessageBox.Show("Are you sure you wish to delete the Beam Template?", "Delete?",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No
                )
            {
                return;
            }

            if (SelectedBeamTemplate != null)
            {
                if (BeamTemplates.Contains(SelectedBeamTemplate))
                {
                    BeamTemplates.Remove(SelectedBeamTemplate);
                    FilterBeamTemplates();
                }
            }

            BeamTemplatesCollection beamTemplatesCollection = new BeamTemplatesCollection();

            beamTemplatesCollection.Templates = FastDeepCloner.DeepCloner.Clone(BeamTemplates);

            if(FilteredBeamTemplates.Count()>0)
            {
                SelectedBeamTemplate = FilteredBeamTemplates.FirstOrDefault();
            }
            else
            {
                SelectedBeamTemplate = new BeamTemplate();
            }

            SerializeToXmlFile(beamTemplatesCollection, "BeamTemplateCollections.xml");
        }

        public (bool Pass, string energyMode) CheckEnergyVsBeamTemplate()
        {
            var energyMode = GetEnergyModeFromPlan();

            return (energyMode.ToUpper().Equals(SelectedBeamTemplate.GapSize.EnergyMode.ToUpper()), energyMode);
        }

        private void InsertBeams()
        {

            IsInsertBeams = true;

            if (BeamTemplateCreationMode)
            {
                MessageBox.Show($"Please disable Beam Template Creation Mode before proceeding.",
                    "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                IsInsertBeams = false;
                return;
            }

            var energyCheck = CheckEnergyVsBeamTemplate();
            if (!CheckEnergyVsBeamTemplate().Pass)
            {
                MessageBox.Show($"The beam energy of the stag plan {energyCheck.energyMode} " +
                    $"does not match the chosen Virtual Cone Size, {SelectedBeamTemplate.GapSize.EnergyMode}", 
                    "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                IsInsertBeams = false;
                return;
            }

            LaunchProgressView();
            _esapiX.ClosePatient();
            Patient pat = _esapiX.OpenPatientById(PatientID);
            pat.BeginModifications();
            string targetCourseId = "VirtualCone";
            string newPlanId = "VirtualCone";

            if (pat == null)
            {
                MessageBox.Show("Patient not found in Eclipse. Check the patient ID and try again.",
                                       "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                IsInsertBeams = false;
                return;
            }

            Course course = pat.Courses.FirstOrDefault(x => x.Id == CourseId);

            if (course == null)
            {
                MessageBox.Show("Course not found in Eclipse. Check the course ID and try again.",
                                                          "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                IsInsertBeams = false;
                return;
            }

            ExternalPlanSetup SelectedPlan = course.ExternalPlanSetups.FirstOrDefault(x => x.Id == PlanId);

            if (SelectedBeamTemplate.BeamInfos.Count() == 0)
            {
                MessageBox.Show("There are no beams in the chosen template. Choose another and try again.",
                    "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                IsInsertBeams = false;
                return;
            }

            if (SelectedBeamTemplate.BeamInfos.Count(x => x.TreatmentTechnique != TreatmentTechnique.SRS_ARC) > 0)
            {
                MessageBox.Show("There is at least one beam in this template that does not have a technique of SRS-ARC; therefore, it is not supported.",
                    "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                IsInsertBeams = false;
                return;
            }

            string fieldErrorMessage = "The plan in Eclipse must contain a field, with:\n" +
                    "*MLC of model type: Varian High Definition 120\n" +
                    "*Energy\n" +
                    "*Dose Rate\n" +
                    "*Isocenter\n" +
                    "*Machine";

            if (SelectedPlan.Beams.Where(x => !x.IsSetupField).Count() == 0)
            {
                MessageBox.Show(fieldErrorMessage, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                IsInsertBeams = false;
                return;
            }

            var firstBeam = SelectedPlan.Beams.First(x => !x.IsSetupField);

            if (firstBeam.MLC == null || firstBeam.MLC.Model != "Varian High Definition 120")
            {
                MessageBox.Show(fieldErrorMessage, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                IsInsertBeams = false;
                return;
            }

            if (firstBeam.EnergyModeDisplayName == null || firstBeam.EnergyModeDisplayName == "")
            {
                MessageBox.Show(fieldErrorMessage, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                IsInsertBeams = false;
                return;
            }

            if (firstBeam.TreatmentUnit == null || firstBeam.TreatmentUnit.Id == "")
            {
                MessageBox.Show(fieldErrorMessage, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                IsInsertBeams = false;
                return;
            }

            //Create course if it doesn't exist and then select it.
            if (pat.Courses.Count(x => x.Id == targetCourseId) == 0)
            {
                Course newCourse = pat.AddCourse();
                newCourse.Id = targetCourseId;
                _esapiX.SaveModifications();
            }

            Course targetCourse = pat.Courses.FirstOrDefault(x => x.Id == targetCourseId);

            int planNumber = 1;
            while (targetCourse.ExternalPlanSetups.Any(x => x.Id == newPlanId))
            {
                newPlanId = $"VirtualCone{planNumber}";
                planNumber++;
            }

            PlanSetup newPlan = targetCourse.CopyPlanSetup(SelectedPlan);
            newPlan.Id = newPlanId;
            SelectedPlan = targetCourse.ExternalPlanSetups.First(x => x.Id.Equals(newPlan.Id));

            var origBeams = SelectedPlan.Beams.ToList();
            var machine = firstBeam.TreatmentUnit.Id;
            var mlcId = firstBeam.MLC.Id;
            var isocenter = firstBeam.IsocenterPosition;
            var energyDisp = firstBeam.EnergyModeDisplayName;
            var doseRate = firstBeam.DoseRate;

            SelectedPlan.Course.Patient.BeginModifications();

            var removeBeamList = SelectedPlan.Beams.ToList();

            try
            {
                foreach (var rb in removeBeamList)
                {
                    SelectedPlan.RemoveBeam(rb);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            string fluence = "";
            if (energyDisp.ToUpper().Contains("FFF"))
                fluence = "FFF";
            if (energyDisp.ToUpper().Contains("SRS"))
                fluence = "SRS";

            var setEnergy = energyDisp;

            if (fluence == "FFF")
                setEnergy = setEnergy.Replace("-FFF", "");

;
            //Initialize the progress log variables.
            Loggers.TotalItems = 100;
            Loggers.CurrentItem = 0;
            var totalBeams = SelectedBeamTemplate.BeamInfos.Count();
            int incProg = (int)Math.Round(90.0 / totalBeams, 0); ;

            DateTime startTime = DateTime.Now;

            foreach (var beam in SelectedBeamTemplate.BeamInfos.OrderBy(x => x.BeamID))
            {
                Loggers.ProgressLog(0, $"Adding {beam.BeamID}...");
                GantryDirection direction = GantryDirection.None;
                if (beam.GantryRotation == GantryRotation.CCW)
                    direction = GantryDirection.CounterClockwise;
                if (beam.GantryRotation == GantryRotation.CW)
                    direction = GantryDirection.Clockwise;

                //string technique = "STATIC";
                //if (beam.TreatmentTechnique == TreatmentTechnique.SRS_ARC || beam.TreatmentTechnique == TreatmentTechnique.ARC)
                //The team decided that we should force users to use SRS
                string technique = "SRS ARC";

                var externalParams = new ExternalBeamMachineParameters(machine,
                    setEnergy, doseRate, technique, fluence);

                Beam field = null;

                // add sample amount of control points for the beam before fitting the MLC
                List<double> cps = new List<double>();
                int n = 0;
                try
                {
                    if (!beam.IsSetup)
                    {

                        // SRS ARC
                        if (beam.TreatmentTechnique == TreatmentTechnique.SRS_ARC)
                        {
                            var numOfCps = (int)(CalculateDegrees((double)beam.GantryStart, (double)beam.GantryStop, beam.GantryRotation == GantryRotation.CW) / 2 + 1);

                            if (numOfCps > 180)
                            {
                                numOfCps = 180;
                            }

                            for (int i = 0; i < numOfCps; i++)
                            {
                                cps.Add(i);
                            }

                            n = 1;

                            double tableAngle = beam.Table ?? 0.0;
                            field = SelectedPlan.AddVMATBeam(externalParams, cps, beam.Collimator.Value, beam.GantryStart.Value, beam.GantryStop ?? (beam.GantryStart ?? 0.0), direction, tableAngle, isocenter);
                            if (field.TreatmentUnit.MachineScaleDisplayName.ToUpper().Equals("VARIAN IEC"))
                            {
                                tableAngle = ConvertBetweenIEC61217andVarianIEC(tableAngle);
                                SelectedPlan.RemoveBeam(field);
                                field = SelectedPlan.AddVMATBeam(externalParams, cps, beam.Collimator.Value, beam.GantryStart.Value, beam.GantryStop ?? (beam.GantryStart ?? 0.0), direction, tableAngle, isocenter);
                            }

                            n = 2;

                            double JawX = GapSettings.X / 2;
                            double JawY = GapSettings.Y / 2;

                            var editParams = field.GetEditableParameters();
                            editParams.SetJawPositions(new VRect<double>(-JawX, -JawY, JawX, JawY));
                            field.ApplyParameters(editParams);

                            n = 3;

                            // Get the cps from beam
                            var edits = field.GetEditableParameters();

                            n = 4;
                            //var beamGap = SelectedBeamTemplate.BeamInfos.Single(x => x.BeamID.ToUpper().Equals(beam.BeamID.ToUpper())).Gap;
                            
                            edits.SetAllLeafPositions(GetMLCLeafPositions(field.MLC.Model, SelectedBeamTemplate.GapSize));
                            field.ApplyParameters(edits);
                        }
                        try
                        {
                            field.Id = beam.BeamID;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }
                    else
                    {
                    }
                }
                catch (Exception ex)
                {
                    //exception messagebox
                    MessageBox.Show($"1254:{ex.Message}, {n}");
                }

                if (GapSettings.EnableSlidingLeaf)
                {
                    foreach (var b in SelectedPlan.Beams.Where(x => !x.IsSetupField))
                    {
                        BeamParameters editableParameters = b.GetEditableParameters();

                        int numCPs = b.ControlPoints.Count();
                        double incrementDouble = 140.0 / (double)numCPs;

                        float setPosition = -70F;
                        float increment = (float)incrementDouble;
                        foreach (var cp in b.ControlPoints)
                        {
                            ControlPointParameters editCP = editableParameters.ControlPoints.ElementAt(cp.Index);

                            var halfX = GapSettings.X / 2;
                            var halfY = GapSettings.Y / 2;
                            editCP.JawPositions = new VRect<double>(-halfX, -halfY, halfX, halfY);
                            //editParams.SetJawPositions(new VRect<double>(beam.X1 ?? 50.0, beam.Y1 ?? 50.0, beam.X2 ?? -50.0, beam.Y2 ?? -50.0));
                            editCP.LeafPositions = GetMLCPositions(cp.LeafPositions, setPosition, (float)GapSettings.SlidingLeafGapSize);
                            b.ApplyParameters(editableParameters);
                            setPosition += increment;
                        }
                    }
                }

                Loggers.ProgressLog(incProg, "");
                _esapiX.SaveModifications();
            }

            //MessageBox.Show($"{DateTime.Now.Subtract(startTime).TotalSeconds}");

            NumberOfBeams = SelectedPlan.Beams.Count(x => !x.IsSetupField);

            _esapiX.SaveModifications();

            _esapiX.ClosePatient();

            UpdateCourses();
            CourseId = targetCourseId;
            PlanId = newPlanId;

            Loggers.ProgressLog(0, "Assigning dose rates...");
            AssignDoseRates();
            Loggers.ProgressLog(10, "Assigning field weights...");
            AssignFieldWeights();

            Loggers.ProgressLog(10, "Done!");

            //OnPlanSelect();
            MessageBox.Show($"The fields have been inserted into the plan, {CourseId}>{PlanId}." +
                $"\n\nThe selections have been updated automatically.", "Complete!", MessageBoxButton.OK, MessageBoxImage.Information);
            IsInsertBeams = false;
        }

        public static double CalculateDegrees(double start, double end, bool clockwise)
        {
            // Special case: if moving from 0 to 360 or 360 to 0, we treat it as a full 360 degrees
            if (start == 0 && end == 360)
            {
                if (clockwise)
                {
                    return 360;
                }
                else
                {
                    return 0;
                }
            }

            if (start == 360 && end == 0)
            {
                if (clockwise)
                {
                    return 0;
                }
                else
                {
                    return 360;
                }
            }

            // Define a tolerance for comparing near-equal angles
            const double tolerance = 0.001;

            // Normalize the input values to the range [0, 360)
            start = (start % 360 + 360) % 360;
            end = (end % 360 + 360) % 360;

            // Special case: if moving from near 360 to near 0, treat it as a full 360 degrees
            if (Math.Abs(start - 360) < tolerance && Math.Abs(end - 0) < tolerance)
            {
                return clockwise ? 0 : 360;
            }

            // If clockwise, calculate the forward difference
            if (clockwise)
            {
                return (end >= start) ? end - start : 360 - (start - end);
            }
            else
            {
                // If counterclockwise, calculate the backward difference
                return (start >= end) ? start - end : 360 - (end - start);
            }
        }

        private float[,] GetMLCPositions(float[,] originalLeaves, float setPosition, float slidingLeafGap)
        {
            // Copy original array
            float[,] copyLeaves = (float[,])originalLeaves.Clone();

            // Modify specific indices
            copyLeaves[0, 0] = setPosition;
            copyLeaves[1, 0] = slidingLeafGap + setPosition;

            return copyLeaves;
        }

        private void CreateBeamTemplate()
        {
            if(SelectedBeamTemplate == null)
            {
                SelectedBeamTemplate = new BeamTemplate();
                MessageBox.Show($"Something went wrong.  Please try again.",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (NewBeamTemplateId == "")
            {
                MessageBox.Show($"The candidate name is blank.  Please enter an Id and try again.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (BeamTemplates.Select(x => x.BeamTemplateId.ToUpper()).Contains(NewBeamTemplateId.ToUpper()))
            {
                MessageBox.Show($"That Id is already in use.  Please choose a unique Id and try again.",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (NewBeamTemplateId.ToUpper().Equals(defaultTemplateId.ToUpper()))
            {
                MessageBox.Show($"'Template Id' cannot be used.  Please choose a unique Id and try again.",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (SelectedBeamTemplate.GapSize == null || SelectedBeamTemplate.GapSize.NumberOfLeaves == 0 ||
                SelectedBeamTemplate.GapSize.GapSizeMM == 0 || SelectedBeamTemplate.GapSize.NumberOfLeaves % 2 !=0)
            {
                MessageBox.Show($"Please select a gap size first.  Ensure the number of leaves is even.",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //Extract beam information from selected plan
            BeamTemplate beamTemplate = new BeamTemplate();
            beamTemplate.BeamInfos = GetBeamInfos();
            beamTemplate.BeamTemplateId = NewBeamTemplateId;
            beamTemplate.GapSize = FastDeepCloner.DeepCloner.Clone(SelectedBeamTemplate.GapSize);


            foreach(var b in beamTemplate.BeamInfos)
            {
                try
                {
                    b.Weight = 1.0;
                    b.MapId = Maps.Single(x => x.MapId.ToUpper().Equals("SIN360")).MapId;
                }
                catch(Exception ex)
                {

                }
            }

            BeamTemplates.Add(beamTemplate);

            BeamTemplatesCollection beamTemplatesCollection = new BeamTemplatesCollection();

            beamTemplatesCollection.Templates = FastDeepCloner.DeepCloner.Clone(BeamTemplates);

            SerializeToXmlFile(beamTemplatesCollection, "BeamTemplateCollections.xml");

            FilterBeamTemplates();

            SelectedBeamTemplate = beamTemplate;
        }

        public ICommand DuplicateTemplateCommand => new RelayCommand(DuplicateTemplate);

        private void DuplicateTemplate()
        {
            if (SelectedBeamTemplate == null || string.IsNullOrEmpty(SelectedBeamTemplate.BeamTemplateId))
            {
                MessageBox.Show("Please select a template.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (BeamTemplates.Select(x => x.BeamTemplateId.ToUpper()).Any(x => x.Equals(NewBeamTemplateId.ToUpper())))
            {
                MessageBox.Show("Please enter a unique Beam Template Id.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (NewBeamTemplateId.ToUpper().Equals("TEMPLATE ID"))
            {
                MessageBox.Show("Please enter a unique Beam Template Id.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var mbr = MessageBox.Show("Are you sure?", "Duplicate?", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (mbr == MessageBoxResult.No)
            {
                return;
            }

            var duplicate = FastDeepCloner.DeepCloner.Clone(SelectedBeamTemplate);
            duplicate.BeamTemplateId = NewBeamTemplateId;
            BeamTemplates.Add(duplicate);
            SelectedBeamTemplate = duplicate;

            BeamTemplatesCollection beamTemplatesCollection = new BeamTemplatesCollection();
            beamTemplatesCollection.Templates = FastDeepCloner.DeepCloner.Clone(BeamTemplates);
            SerializeToXmlFile(beamTemplatesCollection, "BeamTemplateCollections.xml");
            FilterBeamTemplates();
            SelectedBeamTemplate = duplicate;

            MessageBox.Show("Saved!", "Duplicate created!", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpdateTemplate()
        {
            var mbr = MessageBox.Show("Are you sure?", "Save?", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (mbr == MessageBoxResult.No)
            {
                return;
            }

            BeamTemplatesCollection beamTemplatesCollection = new BeamTemplatesCollection();
            beamTemplatesCollection.Templates = FastDeepCloner.DeepCloner.Clone(BeamTemplates);
            SerializeToXmlFile(beamTemplatesCollection, "BeamTemplateCollections.xml");

            MessageBox.Show("Saved!", "Saved!", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SerializeToXmlFile(BeamTemplatesCollection beamTemplatesCollection, string fileName)
        {
            try
            {
                string currentDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string filePath = Path.Combine(currentDirectory, fileName);

                XmlSerializer serializer = new XmlSerializer(typeof(BeamTemplatesCollection));
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    serializer.Serialize(writer, beamTemplatesCollection);
                }
            }
            catch (Exception ex)
            {
                // Handle the exception if any during the serialization process
                MessageBox.Show($"An error occurred while trying to save: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private float[,] GetMLCLeafPositions(string MLC_Model, GapPair gapPair)
        {
            double gapMM = gapPair.GapSizeMM;
            int numberOfLeaves = gapPair.NumberOfLeaves;

            float negSide = 0;
            float posSide = 0;

            double halfGap = gapMM / 2;
            negSide = (float)-halfGap;
            posSide = (float)halfGap;

            if (MLC_Model == "Varian High Definition 120")
            {
                // Build leaf bank
                var leaves = new float[2, 60];
                for (int i = 0; i < 60; i++)
                {
                    leaves[0, i] = -37.5F;
                    leaves[1, i] = -37.5F;
                }

                // Calculate central indices
                int centralIndex = 30;
                int leafPairs = numberOfLeaves / 2;

                // Assign positions to the central leaf pairs
                for (int i = 0; i < leafPairs; i++)
                {
                    leaves[0, centralIndex + i] = negSide; // A bank  // 30, 31
                    leaves[1, centralIndex + i] = posSide; // B bank // 30, 31
                    leaves[0, centralIndex - i - 1] = negSide; // A bank // 29, 28
                    leaves[1, centralIndex - i - 1] = posSide; // B bank // 29, 28
                }

                return leaves;
            }

            return null;
        }

        //private float[,] GetMLCLeafPositions(string MLC_Model, GapPair gapPair)
        //{
        //    double gapMM = gapPair.GapSizeMM;
        //    int numberOfLeaves = gapPair.NumberOfLeaves;

        //    float negSide = 0;
        //    float posSide = 0;

        //    //double halfGap = GapSettings.GapSize / 2;
        //    double halfGap = gapMM / 2;
        //    negSide = (float)-halfGap;
        //    posSide = (float)halfGap;

        //    if (MLC_Model == "Varian High Definition 120")
        //    {
        //        // Build leaf bank
        //        var leaves = new float[2, 60];
        //        for (int i = 0; i < 60; i++)
        //        {
        //            leaves[0, i] = -37.5F;
        //            leaves[1, i] = -37.5F;
        //        }
        //        leaves[0, 30] = negSide;
        //        leaves[1, 30] = posSide;
        //        leaves[0, 29] = negSide;
        //        leaves[1, 29] = posSide;

        //        return leaves;
        //    }

        //    return null;
        //}

        public List<BeamInfo> GetBeamInfos()
        {
            _esapiX.ClosePatient();
            Patient pat = _esapiX.OpenPatientById(PatientID);

            if (pat == null)
            {
                MessageBox.Show("Patient not found in Eclipse. Check the patient ID and try again.",
                                       "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<BeamInfo>();
            }

            Course course = pat.Courses.FirstOrDefault(x => x.Id == CourseId);

            if (course == null)
            {
                MessageBox.Show("Course not found in Eclipse. Check the course ID and try again.",
                                                          "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<BeamInfo>();
            }

            ExternalPlanSetup SelectedPlan = course.ExternalPlanSetups.FirstOrDefault(x => x.Id == PlanId);

            if (SelectedPlan.Beams.Where(x => !x.IsSetupField).Count(x => !x.Technique.Id.Equals("SRS ARC")) > 0)
            {
                MessageBox.Show("All treatment fields must have a technique of SRS ARC.",
                                                         "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<BeamInfo>();
            }

            if (SelectedPlan != null)
            {
                var bis = new List<BeamInfo>();
                foreach (var b in SelectedPlan.Beams)
                {
                    BeamInfo beam = new BeamInfo()
                    {
                        IsSetup = b.IsSetupField,
                        BeamID = b.Id,
                        Collimator = b.CollimatorAngleToUser(b.ControlPoints.First().CollimatorAngle),
                        DoseRate = b.DoseRate,

                        EnergyValue = BeamInfo.SetEnergyValue(b.EnergyModeDisplayName),
                        EnergyDisplay = b.EnergyModeDisplayName,

                        GantryRotation = BeamInfo.GantryRotationSelector(b.GantryDirection.ToString()),
                        GantryStart = b.GantryAngleToUser(b.ControlPoints.FirstOrDefault().GantryAngle),
                        GantryStop = b.GantryAngleToUser(b.ControlPoints.LastOrDefault().GantryAngle),

                        NumberOfControlPoints = b.ControlPoints.Count(),

                        IsocenterCoordinates = new IsocenterCoordinates()
                        {
                            X = b.IsocenterPosition.x,
                            Y = b.IsocenterPosition.y,
                            Z = b.IsocenterPosition.z,
                        },

                        Table = b.PatientSupportAngleToUser(b.ControlPoints.First().PatientSupportAngle),
                        ToleranceTable = b.ToleranceTableLabel,
                        X1 = b.ControlPoints.FirstOrDefault().JawPositions.X1,
                        Y1 = b.ControlPoints.FirstOrDefault().JawPositions.Y1,
                        X2 = b.ControlPoints.FirstOrDefault().JawPositions.X2,
                        Y2 = b.ControlPoints.FirstOrDefault().JawPositions.Y2,

                        // set some defaults for the user. Can change in the XML
                        StructureFitting = new StructureFitting()
                        {
                            JawFittingMode = "1",
                            OpenMLCMeetingPoint = OpenMLCMeetingPoint.Inside,
                            CloseMLCMeetingPoint = CloseMLCMeetingPoint.BankB,
                            Left = 5.0,
                            Right = 5.0,
                            Top = 5.0,
                            Bottom = 5.0,
                            OptimizeCollimator = false,
                            SymmetricMargin = true
                            //,TargetVolume = pp.TargetVolume
                        },

                        TreatmentTechnique = BeamInfo.TechniqueSelector(b.Technique.ToString()),
                    };

                    bis.Add(beam);
                }

                return bis;
            }

            _esapiX.ClosePatient();
            return null;
        }

        private DRCalculator _DRCalc;

        public DRCalculator DRCalc
        {
            get { return _DRCalc; }
            set { SetProperty(ref _DRCalc, value); }
        }

        private void AssignDoseRates()
        {

            //The dose rate Map Id is received from the UI, but the actual map is not attached.
            //The map is attached here.
            foreach (var b in SelectedBeamTemplate.BeamInfos)
            {
                b.Map = Maps.Single(x => x.MapId.Equals(b.MapId));
            }

            bool isConstant = true;

            _esapiX.ClosePatient();
            Patient pat = _esapiX.OpenPatientById(PatientID);

            if (pat == null)
            {
                MessageBox.Show("Patient not found in Eclipse. Check the patient ID and try again.",
                                       "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Course course = pat.Courses.FirstOrDefault(x => x.Id == CourseId);

            if (course == null)
            {
                MessageBox.Show("Course not found in Eclipse. Check the course ID and try again.",
                                                          "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ExternalPlanSetup SelectedPlan = course.ExternalPlanSetups.FirstOrDefault(x => x.Id == PlanId);

            DRCalc = new DRCalculator(SelectedPlan, _esapiX, SelectedBeamTemplate.BeamInfos.ToList());

            foreach (var key in DRCalc.InitialDRs.Keys)
            {
                var DRs = DRCalc.InitialDRs[key].Select(pt => pt.Y);
                if (Math.Abs(DRs.Max() - DRs.Min()) > 1)
                {
                    isConstant = false;
                    break;
                }
            }

            if (!isConstant)
            {
                var msg = "Warning: plan already contains non-constant dose rate. Are you sure you want to apply this function? Results may be unexpected.";
                var res = System.Windows.MessageBox.Show(msg, "Warning", System.Windows.MessageBoxButton.YesNo);
                if (res == System.Windows.MessageBoxResult.No)
                {
                    return;
                }
            }

            DRCalc.CreateNewPlanWithMethod(SelectedBeamTemplate.BeamInfos);
            _esapiX.SaveModifications();

            //OnPlanSelect(); // Calling this so that the selected plan isn't disposed...
        }

        private void AssignFieldWeights()
        {
            _esapiX.ClosePatient();
            Patient pat = _esapiX.OpenPatientById(PatientID);

            if (pat == null)
            {
                MessageBox.Show("Patient not found in Eclipse. Check the patient ID and try again.",
                                       "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Course course = pat.Courses.FirstOrDefault(x => x.Id == CourseId);

            if (course == null)
            {
                MessageBox.Show("Course not found in Eclipse. Check the course ID and try again.",
                                                          "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ExternalPlanSetup SelectedPlan = course.ExternalPlanSetups.FirstOrDefault(x => x.Id == PlanId);

            pat.BeginModifications();
            foreach(var b in SelectedPlan.Beams)
            {
                var fw = SelectedBeamTemplate.BeamInfos.Single(x => x.BeamID.Equals(b.Id)).Weight;
                var edp = b.GetEditableParameters();
                edp.WeightFactor = fw;
                b.ApplyParameters(edp);
            }

            _esapiX.SaveModifications();
            _esapiX.ClosePatient();

        }

        public void LoadBeamTemplates()
        {
          
            string filePath = Path.Combine(ParentDirectory, "BeamTemplateCollections.xml");

            XmlSerializer serializer = new XmlSerializer(typeof(BeamTemplatesCollection));

            BeamTemplatesCollection beamTemplateCollection = new BeamTemplatesCollection();

            try
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    beamTemplateCollection = (BeamTemplatesCollection)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            foreach (var b in beamTemplateCollection.Templates)
            {
                BeamTemplates.Add(FastDeepCloner.DeepCloner.Clone(b));
            }
        }

        private void CreateNewBeamCollectionsFile()
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, "BeamTemplateCollections.xml");


            if (!File.Exists(filePath))
            {

                var beamTemplateCollection = new BeamTemplatesCollection();
                ToolBox.SerializeToXmlFile<BeamTemplatesCollection>(beamTemplateCollection, filePath);
            }
        }


        private void CreateNewSettingsFile()
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, "Settings.xml");


            if (!File.Exists(filePath))
            {
                GapSettings defaultSettings = new GapSettings();
                defaultSettings.EnableSlidingLeaf = true;
                //defaultSettings.GapSize = 2.1;
                defaultSettings.X = 20;
                defaultSettings.Y = 20;
                defaultSettings.SlidingLeafGapSize = 2;

                ToolBox.SerializeToXmlFile<GapSettings>(defaultSettings, filePath);
            }
        }

        public void ImportSettings()
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, "Settings.xml");


            if (!File.Exists(filePath))
            {
                CreateNewSettingsFile();
            }

            if (File.Exists(filePath))
            {
                GapSettings = new GapSettings();
                var serializer = new XmlSerializer(typeof(GapSettings));
                using (var reader = new StreamReader(filePath))
                {
                    GapSettings = (GapSettings)serializer.Deserialize(reader);
                }

                //GapPair gp = new GapPair();
                //gp.NumberOfLeaves = 2;
                //gp.GapSizeMM = 2.5;

                //GapSettings.AvailableGapsMM.Add(gp);

                //gp.NumberOfLeaves = 4;
                //gp.GapSizeMM = 8;

                //GapSettings.AvailableGapsMM.Add(gp);

                //using (var writer = new StreamWriter(filePath))
                //{
                //    serializer.Serialize(writer, GapSettings);
                //}

            }
            else
            {
                MessageBox.Show("The settings.xml file does not exist.", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region <---Maps--->

        private ObservableCollection<GantryWeightMap> maps = new ObservableCollection<GantryWeightMap>();
        public ObservableCollection<GantryWeightMap> Maps
        {
            get => maps;
            set => SetProperty(ref maps, value);
        }


        public void GetGantryWeightMaps()
        {
            // Get the path of the "Maps" folder in the same directory as the executable
            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Maps");
            try
            {
                if (!Directory.Exists(folderPath))
                {
                    //MessageBox.Show("The folder 'Maps' does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var txtFiles = Directory.GetFiles(folderPath, "*.txt");

                foreach (var filePath in txtFiles)
                {
                    try
                    {
                        var gantryWeightMap = new GantryWeightMap
                        {
                            MapId = Path.GetFileNameWithoutExtension(filePath)
                        };

                        var lines = File.ReadAllLines(filePath);
                        foreach (var line in lines)
                        {
                            if (string.IsNullOrWhiteSpace(line))
                                continue;

                            var parts = line.Split(',');
                            if (parts.Length != 2)
                                continue;

                            if (double.TryParse(parts[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double gantry) &&
                                double.TryParse(parts[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double weight))
                            {
                                gantryWeightMap.pairs.Add(new GantryWeightPair
                                {
                                    Gantry = gantry,
                                    Weight = weight
                                });
                            }
                            else
                            {
                                MessageBox.Show($"Error parsing values in file: {filePath}. Ensure all lines contain two valid numbers.",
                                                "Parsing Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }

                        if (gantryWeightMap.pairs.Any())
                        {
                            Maps.Add(gantryWeightMap);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occurred while reading file '{filePath}': {ex.Message}",
                                        "File Read Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while accessing the folder '{folderPath}': {ex.Message}",
                                "Folder Access Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion <---Maps--->


        #region <---Progress--->

        private Thread newWindowThread = null; // Keep a reference to the thread
        private int? progressViewProcessId = null; // To store the process ID

        private void LaunchProgressView()
        {
            // Check if the Progress View is already open
            if (newWindowThread != null && newWindowThread.IsAlive)
            {
                // If the window is already open, log the process ID and do not open a new window
                return;
            }

            newWindowThread = new Thread(new ThreadStart(() =>
            {
                // Create our context, and install it:
                SynchronizationContext.SetSynchronizationContext(
                    new DispatcherSynchronizationContext(
                        Dispatcher.CurrentDispatcher));

                ProgressViewModel progressVM = new ProgressViewModel(Process.GetCurrentProcess().Id);
                ProgressView progressView = new ProgressView
                {
                    DataContext = progressVM
                };

                //progressView.Top = thisTop;
                //progressView.Left = thisLeft + (thisWidth - 1000) / 2;

                progressView.Show();

                // Capture the process ID
                progressViewProcessId = Process.GetCurrentProcess().Id;

                // When the window closes (returns), close the dispatcher
                progressView.Closed += (s, e) =>
                {
                    Dispatcher.CurrentDispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
                    progressViewProcessId = null; // Reset the process ID
                };

                // Start the Dispatcher Processing
                Dispatcher.Run();
            }));

            // Set the apartment state
            newWindowThread.SetApartmentState(ApartmentState.STA);

            // Make the thread a background thread
            newWindowThread.IsBackground = true;

            // Start the thread
            newWindowThread.Start();
        }

        private void UpdateProgress(int currentItem, int TotalItems, string Message)
        {
            var currentProgress = new ProgressItem(currentItem, TotalItems, Message);

            // Serialize initialProgress into an XML file
            var serializer = new XmlSerializer(typeof(ProgressItem));
            using (var stream = new FileStream(Loggers.ProgressLogPath, FileMode.Create))
            {
                serializer.Serialize(stream, currentProgress);
            }
        }


        public void DeleteFileOnExit()
        {
            try
            {
                if (File.Exists(Loggers.ProgressLogPath))
                {
                    File.Delete(Loggers.ProgressLogPath);
                }
            }
            catch (Exception ex)
            {
                // Log or handle the exception if necessary
                Console.WriteLine($"Failed to delete file on exit: {ex.Message}");
            }
        }

        #endregion <---Progress--->

        #region <---Scale Conversions

        public static double ConvertBetweenIEC61217andVarianIEC(double originalAngle)
        {
            double resultAngle = 360 - originalAngle;

            if (resultAngle == 360)
            {
                return 0;
            }

            return resultAngle;
        }

        public static double ConvertBetweenIEC61217andVarianStandard(double originalAngle)
        {
            // Subtract the original angle from 180 to mirror it
            double mirroredAngle = 180 - originalAngle;

            // If the result is negative, add 360 to bring it within the 0-360 range
            if (mirroredAngle < 0)
            {
                mirroredAngle += 360;
            }

            return mirroredAngle;
        }

        #endregion <---Scale Conversions

        #region <---Other

        public ICommand OpenDirectoryCommand => new RelayCommand(OpenDirectory);

        public void OpenDirectory()
        {
            string directoryPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Process.Start(directoryPath);
        }

        #endregion <---Other
    }
}