using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using VMS.TPS.Common.Model.API;

namespace DoseRateEditor.Models
{
    public class CourseModel
    {
        public string Id { get; private set; }
        public Course Course { get; private set; }

        public Dictionary<string, ExternalPlanSetup> Plans;
        public CourseModel(Course crs)
        {
            Course = crs;
            Id = crs.Id;
            Plans = new Dictionary<string, ExternalPlanSetup>();
            var PlanList = this.Course.ExternalPlanSetups.ToList();
            foreach(var plan in PlanList)
            {
                // Check for plan name collision because of dicom write into Aria (rare)
                if (Plans.Keys.Contains(plan.Id)) {
                    continue;
                }

                //MessageBox.Show(plan.Beams.FirstOrDefault().Technique.Id);
                // Check plan type
                //if (plan.Beams.FirstOrDefault().Technique.Id.Contains("ARC")) {
                Plans.Add(plan.Id, plan);
                //}
            }
        }
    }
}
