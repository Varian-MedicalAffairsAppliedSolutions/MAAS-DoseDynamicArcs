using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualCones_MCB.Models
{
    public class StructureMatch : ObservableObject
    {

        public StructureMatch()
        {

        }
        public string StructureID { get; set; }

        private string _rapidPlanStructureID;
        public string RapidPlanStructureID
        {
            get { return _rapidPlanStructureID; }
            set
            {
                //if (_rapidPlanStructureID != value)
                //{
                _rapidPlanStructureID = value;
                OnPropertyChanged("RapidPlanStructureID");
                //}
            }
        }
    }

    public class TargetMatch : StructureMatch
    {
        public TargetMatch()
        {

        }

        private double _targetDoseinCGY;
        public double TargetDoseInCGy
        {
            get { return _targetDoseinCGY; }
            set
            {
                if (_targetDoseinCGY != value)
                {
                    _targetDoseinCGY = value;
                    OnPropertyChanged("TargetDoseInCGy");
                }
            }
        }

    }
}
