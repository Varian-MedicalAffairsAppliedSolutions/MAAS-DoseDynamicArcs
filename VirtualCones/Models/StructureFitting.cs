using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOS_VirtualCones_MCB.Models
{
    public class StructureFitting
    {
        public StructureInfo TargetVolume { get; set; }
        public bool SymmetricMargin { get; set; }
        public OpenMLCMeetingPoint OpenMLCMeetingPoint { get; set; }
        public CloseMLCMeetingPoint CloseMLCMeetingPoint { get; set; }
        public bool OptimizeCollimator { get; set; }
        public string JawFittingMode { get; set; } // 0 = none, 1 = recommended, 2 = fit to structure
        public double? Left { get; set; }
        public double? Right { get; set; }
        public double? Top { get; set; }
        public double? Bottom { get; set; }
    }

    public enum OpenMLCMeetingPoint
    {
        Inside,
        Middle,
        Outside
    }

    public enum CloseMLCMeetingPoint
    {
        BankA,
        BankB,
        Center
    }
}
