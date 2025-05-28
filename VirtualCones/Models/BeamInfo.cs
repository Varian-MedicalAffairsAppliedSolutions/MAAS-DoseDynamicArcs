using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace VirtualCones_MCB.Models
{
    public class BeamInfo : ObservableObject, ICloneable
    {
        public object Clone()
        {
            var clone = new BeamInfo
            {
                BeamID = BeamID,
                EnergyValue = EnergyValue,
                EnergyDisplay = EnergyDisplay,
                GantryStart = GantryStart,
                GantryStop = GantryStop,
                Collimator = Collimator,
                Table = Table,
                X1 = X1,
                X2 = X2,
                Y1 = Y1,
                Y2 = Y2,
                DoseRate = DoseRate,
                IsSetup = IsSetup,
                ToleranceTable = ToleranceTable,
                GantryRotation = GantryRotation,
                IsocenterCoordinates = IsocenterCoordinates,
                TreatmentTechnique = TreatmentTechnique,
                NumberOfControlPoints = NumberOfControlPoints,
                StructureFitting = StructureFitting
            };

            return clone;
        }

        public string BeamID { get; set; }
        public int EnergyValue { get; set; }

        private string _energyDisplay;

        public string EnergyDisplay
        {
            get { return _energyDisplay; }
            set
            {
                SetProperty(ref _energyDisplay, value);
            }
        }

        public double? GantryStart { get; set; }
        public double? GantryStop { get; set; }
        public double? Collimator { get; set; }
        public double? Table { get; set; }
        public double? X1 { get; set; }
        public double? X2 { get; set; }
        public double? Y1 { get; set; }
        public double? Y2 { get; set; }
        private int? _doseRate;

        public int? DoseRate
        {
            get { return _doseRate; }
            set
            {
                SetProperty(ref _doseRate, value);
            }
        }

        public bool IsSetup { get; set; }
        public string ToleranceTable { get; set; }
        public GantryRotation GantryRotation { get; set; }
        public IsocenterCoordinates IsocenterCoordinates { get; set; }
        public TreatmentTechnique TreatmentTechnique { get; set; }
        public int NumberOfControlPoints { get; set; }

        public List<double> ControlPoints
        {
            get
            {
                List<double> cps = new List<double>();

                for (int i = 0; i < NumberOfControlPoints; i++)
                {
                    cps.Add(i);
                }

                return cps;
            }
        }


        public GantryWeightMap Map { get; set; }

        public string MapId { get; set; }

        public double Gap { get; set; }

        public double Weight { get; set; }

        public StructureFitting StructureFitting { get; set; }

        public static TreatmentTechnique TechniqueSelector(string technique)
        {
            TreatmentTechnique result = TreatmentTechnique.STATIC;

            switch (technique)
            {
                case "ARC": result = TreatmentTechnique.ARC; break;
                case "SRS ARC": result = TreatmentTechnique.SRS_ARC; break;
                case "SRS STATIC": result = TreatmentTechnique.SRS_STATIC; break;
                default: break;
            }

            return result;
        }

        public static string TreatmentTechiqueForPlan(TreatmentTechnique technique)
        {
            string result = "STATIC";

            switch (technique)
            {
                case TreatmentTechnique.ARC: result = "ARC"; break;
                case TreatmentTechnique.SRS_ARC: result = "SRS ARC"; break;
                case TreatmentTechnique.STATIC: result = "STATIC"; break;
                case TreatmentTechnique.SRS_STATIC: result = "SRS STATIC"; break;

                default: break;
            }

            return result;
        }

        public static GantryRotation GantryRotationSelector(string rotation)
        {
            GantryRotation result = GantryRotation.NONE;

            switch (rotation)
            {
                case "Clockwise": result = GantryRotation.CW; break;
                case "CounterClockwise": result = GantryRotation.CCW; break;

                default: break;
            }

            return result;
        }

        public static int SetEnergyValue(string energy)
        {
            string resultString = Regex.Match(energy, @"\d+").Value;
            try
            {
                return Convert.ToInt32(resultString);
            }
            catch
            {
                return 0;
            }
        }
    }

    public enum GantryRotation
    {
        NONE,
        CW,
        CCW
    }

    public enum FluenceMode
    {
        FFF,
        SRS
    }

    public enum TreatmentTechnique
    {
        STATIC,
        ARC,
        SRS_ARC,
        SRS_STATIC
    }

    public class IsocenterCoordinates
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }

    public class BeamTemplate :ObservableObject
    {
        public BeamTemplate()
        {
            BeamInfos = new List<BeamInfo>();
        }

        public string BeamTemplateId { get; set; }

        private GapPair _gapSize;
        public GapPair GapSize
        {   
            get => _gapSize;
            set => SetProperty(ref _gapSize, value);
        }
        public List<BeamInfo> BeamInfos { get; set; }
    }

    public class BeamTemplatesCollection
    {
        public BeamTemplatesCollection()
        {
            Templates = new ObservableCollection<BeamTemplate>();
        }

        public ObservableCollection<BeamTemplate> Templates { get; set; }
    }
}