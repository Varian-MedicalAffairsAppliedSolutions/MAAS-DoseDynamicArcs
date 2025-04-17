using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOS_VirtualCones_MCB.Models
{
    public class GapSettings
    {
        public GapSettings() { }
        //public double GapSize { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public bool EnableSlidingLeaf { get; set; }
        public double SlidingLeafGapSize { get; set; }
        public List<GapPair> AvailableGapsMM { get; set; }
        public bool RemoveTempPlan { get; set; }
    }

    public class GapPair : ObservableObject
    {
        public GapPair()
            { }

        private string _energyMode;
        private double _gapSizeMM;
        private int _numberOfLeaves;

        public string EnergyMode
        {
            get => _energyMode;
            set { _energyMode = value; SetProperty(ref _energyMode, value); }
        }

        public double GapSizeMM
        {
            get => _gapSizeMM;
            set { _gapSizeMM = value; SetProperty(ref _gapSizeMM,value); }
        }

        public int NumberOfLeaves
        {
            get => _numberOfLeaves;
            set { SetProperty(ref _numberOfLeaves, value); }
        }

        public string Description
        {
            get => $"{EnergyMode}, {GapSizeMM} mm, {NumberOfLeaves} leaves";
        }

        // Override Equals
        public override bool Equals(object obj)
        {
            if (obj is GapPair other)
            {
                return this.GapSizeMM == other.GapSizeMM &&
                       this.NumberOfLeaves == other.NumberOfLeaves
                       && this.EnergyMode == other.EnergyMode;
            }
            return false;
        }

        // Override GetHashCode
        public override int GetHashCode()
        {
            unchecked
            {
                // Combine the hash codes of GapSizeMM, NumberOfLeaves, and EnergyMode
                int hash = 17;
                hash = hash * 23 + GapSizeMM.GetHashCode();
                hash = hash * 23 + NumberOfLeaves.GetHashCode();
                hash = hash * 23 + (EnergyMode?.GetHashCode() ?? 0); // Handle null for EnergyMode
                return hash;
            }
        }

    }
}
