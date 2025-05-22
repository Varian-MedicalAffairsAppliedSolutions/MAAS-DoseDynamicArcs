using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOS_VirtualCones_MCB.Models
{
    public class PlanInfo : ObservableObject
    {
        public PlanInfo()
        {
           

        }

        private string _patientId;
        public string PatientId
        {
            get { return _patientId; }
            set { SetProperty(ref _patientId, value); }
        }

        private string _planId;
        public string PlanId
        {
            get { return _planId; }
            set { SetProperty(ref _planId, value); }
        }

        private string _courseId;
        public string CourseId
        {
            get { return _courseId; }
            set { SetProperty(ref _courseId, value); }
        }

    }


}
