using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace AOS_VirtualCones_MCB.Helpers
{
    public class ToolBox
    {
        public static bool Arrays2DAreEqual(float[,] array1, float[,] array2)
        {
            if (array1.GetLength(0) != array2.GetLength(0) || array1.GetLength(1) != array2.GetLength(1))
            {
                return false;
            }

            for (int i = 0; i < array1.GetLength(0); i++)
            {
                for (int j = 0; j < array1.GetLength(1); j++)
                {
                    if (array1[i, j] != array2[i, j])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        #region <---Do Things Exist?--->

        public static bool DoesPatientExist(VMS.TPS.Common.Model.API.Application esapi, string PatientId)
        {
            if (esapi == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(PatientId))
            {
                return false;
            }

            if (esapi.PatientSummaries.Count(x => x.Id.ToUpper().Equals(PatientId.ToUpper())) == 0)
            {
                return false;
            }

            return true;
        }

        public static bool DoesCourseExist(VMS.TPS.Common.Model.API.Application esapi, string PatientId, string CourseId)
        {
            if (esapi.PatientSummaries.Count(x => x.Id.ToUpper().Equals(PatientId.ToUpper())) == 0)
            {
                return false;
            }

            esapi.ClosePatient();
            var pat = esapi.OpenPatientById(PatientId);

            if (pat.Courses.Count(x => x.Id.ToUpper().Equals(CourseId.ToUpper())) == 0)
            {
                return false;
            }

            esapi.ClosePatient();

            return true;
        }

        public static bool DoesPlanExist(VMS.TPS.Common.Model.API.Application esapi, string PatientId, string CourseId, string PlanId)
        {
            if (esapi.PatientSummaries.Count(x => x.Id.ToUpper().Equals(PatientId.ToUpper())) == 0)
            {
                return false;
            }

            esapi.ClosePatient();
            var pat = esapi.OpenPatientById(PatientId);

            if (pat.Courses.Count(x => x.Id.ToUpper().Equals(CourseId.ToUpper())) == 0)
            {
                return false;
            }

            var crs = pat.Courses.First(x => x.Id.ToUpper().Equals(CourseId.ToUpper()));

            if (crs.ExternalPlanSetups.Count(x => x.Id.ToUpper().Equals(CourseId.ToUpper())) == 0)
            {
                return false;
            }

            esapi.ClosePatient();

            return true;
        }

        public static bool DoesStructureSetExist(VMS.TPS.Common.Model.API.Application esapi, string PatientId, string StructureSetId)
        {
            if (esapi.PatientSummaries.Count(x => x.Id.ToUpper().Equals(PatientId.ToUpper())) == 0)
            {
                return false;
            }

            esapi.ClosePatient();
            var pat = esapi.OpenPatientById(PatientId);

            if (pat.StructureSets.Count(x => x.Id.ToUpper().Equals(StructureSetId.ToUpper())) == 0)
            {
                return false;
            }

            esapi.ClosePatient();

            return true;
        }

        #endregion <---Do Things Exist?--->

        public static OpenPatient GetOpenPatient(VMS.TPS.Common.Model.API.Application esapi,
            string PatientId)
        {
            return GetOpenPatient(esapi, PatientId, (false, ""), (false, ""), (false, ""));
        }

        public static OpenPatient GetOpenPatient(VMS.TPS.Common.Model.API.Application esapi,
           string PatientId, string CourseId)
        {
            return GetOpenPatient(esapi, PatientId, (false, ""), (true, CourseId), (false, ""));
        }

        public static OpenPatient GetOpenPatient(VMS.TPS.Common.Model.API.Application esapi,
           string PatientId, string CourseId, string PlanId)
        {
            return GetOpenPatient(esapi, PatientId, (false, ""), (true, CourseId), (true, PlanId));
        }

        public static OpenPatient GetOpenPatient(VMS.TPS.Common.Model.API.Application esapi,
            string PatientId, (bool, string) getStructureSet, (bool, string) getCourse, (bool, string) getPlan)
        {
            OpenPatient resultPatient = new OpenPatient();

            // Check if the patient exists in the system
            if (ToolBox.DoesPatientExist(esapi, PatientId))
            {
                resultPatient.patientExists = true;
                esapi.ClosePatient(); // Close any previously open patient
                resultPatient.patient = esapi.OpenPatientById(PatientId); // Open the specified patient
            }
            else
            {
                // If patient does not exist, set patientExists to false and return
                resultPatient.patientExists = false;
                resultPatient.patient = null;
                return resultPatient;
            }

            // Check if a specific StructureSet is requested
            if (getStructureSet.Item1)
            {
                // Check if the specified StructureSet exists for the patient
                if (resultPatient.patient.StructureSets.Count(x => x.Id.ToUpper().Equals(getStructureSet.Item2.ToUpper())) > 0)
                {
                    resultPatient.structureSetExists = true;
                    resultPatient.structureSet = resultPatient.patient.StructureSets
                        .First(x => x.Id.ToUpper().Equals(getStructureSet.Item2.ToUpper())); // Set the StructureSet
                }
                else
                {
                    // If the specified StructureSet does not exist, set structureSetExists to false
                    resultPatient.structureSetExists = false;
                    resultPatient.structureSet = null;
                }
            }

            // Check if a specific Plan is requested
            if (getPlan.Item1)
            {
                // Check if the specified Course exists for the patient
                if (resultPatient.patient.Courses.Count(x => x.Id.ToUpper().Equals(getCourse.Item2.ToUpper())) > 0)
                {
                    resultPatient.courseExists = true;
                    resultPatient.course = resultPatient.patient.Courses
                        .First(x => x.Id.ToUpper().Equals(getCourse.Item2.ToUpper())); // Set the Course

                    if (getPlan.Item2 == null)
                    {
                        // If the plan was not requested, then return false
                        resultPatient.planExists = false;
                        resultPatient.plan = null;
                        return resultPatient;
                    }
                    // Check if the specified Plan exists within the Course
                    if (resultPatient.course.PlanSetups.Count(x => x.Id.ToUpper().Equals(getPlan.Item2.ToUpper())) > 0)
                    {
                        resultPatient.planExists = true;
                        resultPatient.plan = resultPatient.course.ExternalPlanSetups
                            .First(x => x.Id.ToUpper().Equals(getPlan.Item2.ToUpper())); // Set the Plan
                        return resultPatient; // Return the result if the Plan is found
                    }
                    else
                    {
                        // If the specified Plan does not exist, set planExists to false and return
                        resultPatient.planExists = false;
                        resultPatient.plan = null;
                        return resultPatient;
                    }
                }
                else
                {
                    // If the specified Course does not exist, set courseExists and planExists to false
                    resultPatient.courseExists = false;
                    resultPatient.planExists = false;
                    resultPatient.course = null;
                    resultPatient.plan = null;
                }
            }

            // Check if a specific Course is requested
            if (getCourse.Item1)
            {
                // Check if the specified Course exists for the patient
                if (resultPatient.patient.Courses.Count(x => x.Id.ToUpper().Equals(getCourse.Item2.ToUpper())) > 0)
                {
                    resultPatient.courseExists = true;
                    resultPatient.course = resultPatient.patient.Courses
                        .First(x => x.Id.ToUpper().Equals(getCourse.Item2.ToUpper())); // Set the Course
                }
                else
                {
                    // If the specified Course does not exist, set courseExists to false
                    resultPatient.courseExists = false;
                    resultPatient.course = null;
                }
            }

            return resultPatient; // Return the resultPatient object
        }

        public class OpenPatient
        {
            public OpenPatient()
            {
            }

            public OpenPatient(Patient Patient, StructureSet StructureSet, Course Course, ExternalPlanSetup Plan)
            {
                if (Patient != null)
                {
                    patientExists = true;
                    patient = Patient;
                }

                if (StructureSet != null)
                {
                    structureSetExists = true;
                    structureSet = StructureSet;
                }

                if (Course != null)
                {
                    courseExists = true;
                    course = Course;
                }

                if (Plan != null)
                {
                    planExists = true;
                    plan = Plan;
                }
            }

            public VMS.TPS.Common.Model.API.Patient patient { get; set; }
            public VMS.TPS.Common.Model.API.StructureSet structureSet { get; set; }
            public VMS.TPS.Common.Model.API.Course course { get; set; }
            public VMS.TPS.Common.Model.API.ExternalPlanSetup plan { get; set; }

            public bool patientExists { get; set; }
            public bool structureSetExists { get; set; }
            public bool courseExists { get; set; }
            public bool planExists { get; set; }

            public bool AllExists()
            {
                return (patientExists && structureSetExists && courseExists && planExists);
            }

            public bool PlanFamilyExists()
            {
                return (patientExists && courseExists && planExists);
            }
        }



        public class CopyPatient
        {
            public VMS.TPS.Common.Model.API.Patient patient { get; set; }
            public VMS.TPS.Common.Model.API.Course courseSource { get; set; }
            public VMS.TPS.Common.Model.API.ExternalPlanSetup planSource { get; set; }

            public VMS.TPS.Common.Model.API.Course courseTarget { get; set; }
            public VMS.TPS.Common.Model.API.ExternalPlanSetup planTarget { get; set; }

            public bool patientExists { get; set; }
            public bool courseSourceExists { get; set; }
            public bool planSourceExists { get; set; }
            public bool courseTargetExists { get; set; }
            public bool planTargetExists { get; set; }

            public bool CopyFamilyExists()
            {
                return (patientExists && courseSourceExists && planSourceExists && courseTargetExists && planTargetExists);
            }
        }


        public static List<Beam> GetTreatmentBeams(ExternalPlanSetup planSetup)
        {
            return planSetup.Beams.Where(x => !x.IsSetupField && !x.IsImagingTreatmentField).ToList();
        }
        public static string GetNextCourseId(Patient Patient, string RootCourseId)
        {
            bool courseAlreadyExists = Patient.Courses.Count(x => x.Id.ToUpper().Equals(RootCourseId.ToUpper())) > 0;

            // If the plan already exists and we want to index the plan ID
            if (courseAlreadyExists)
            {
                int suffix = 1;
                string newCourseId = RootCourseId;
                // Generate a new plan ID by appending a numeric suffix until a unique ID is found
                while (Patient.Courses.Count(x => x.Id.ToUpper().Equals(RootCourseId.ToUpper())) > 0)
                {
                    newCourseId = $"{RootCourseId}{suffix}";
                    suffix++;
                }
                RootCourseId = newCourseId;
            }

            return RootCourseId;
        }

        public static string GetNextPlanId(Course course, string RootPlanId)
        {
            // Ensure the RootPlanId is no longer than 12 characters to allow space for at least one numeric suffix
            RootPlanId = RootPlanId.Length > 12 ? RootPlanId.Substring(0, 12) : RootPlanId;

            bool planAlreadyExists = course.PlanSetups.Count(x => x.Id.ToUpper().Equals(RootPlanId.ToUpper())) > 0;

            // If the plan already exists and we want to index the plan ID
            if (planAlreadyExists)
            {
                int suffix = 1;
                string newPlanId = RootPlanId;

                // Generate a new plan ID by appending a numeric suffix until a unique ID is found
                while (course.PlanSetups.Count(x => x.Id.ToUpper().Equals(newPlanId.ToUpper())) > 0)
                {
                    // Ensure the newPlanId is no longer than 13 characters with the suffix
                    string suffixStr = suffix.ToString();
                    int maxRootLength = 13 - suffixStr.Length;
                    string truncatedRoot = RootPlanId.Length > maxRootLength ? RootPlanId.Substring(0, maxRootLength) : RootPlanId;

                    newPlanId = $"{truncatedRoot}{suffixStr}";
                    suffix++;
                }
                RootPlanId = newPlanId;
            }

            return RootPlanId;
        }

        public static float Float2dMinumum(int targetRow, float[,] Float2d)
        {
            float minValue = float.MaxValue;

            for (int i = 0; i < Float2d.GetLength(1); i++)
            {
                if (Float2d[targetRow, i] < minValue)
                {
                    minValue = Float2d[targetRow, i];
                }
            }

            return minValue;
        }

        public static float Float2dMaximum(int targetRow, float[,] Float2d)
        {
            float maxValue = float.MinValue;

            for (int i = 0; i < Float2d.GetLength(1); i++)
            {
                if (Float2d[targetRow, i] > maxValue)
                {
                    maxValue = Float2d[targetRow, i];
                }
            }

            return maxValue;
        }

        public static VRect<double> GetMaximumJawPositions(Beam beam)
        {
            var x1 = beam.ControlPoints.Min(x => x.JawPositions.X1);
            var x2 = beam.ControlPoints.Max(x => x.JawPositions.X2);
            var y1 = beam.ControlPoints.Min(x => x.JawPositions.Y1);
            var y2 = beam.ControlPoints.Max(x => x.JawPositions.Y2);

            return new VRect<double>(x1, y1, x2, y2);

        }

        public static T DeserializeFromXmlFile<T>(string filePath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            using (StreamReader reader = new StreamReader(filePath))
            {
                return (T)serializer.Deserialize(reader);
            }
        }

        public static void SerializeToXmlFile<T>(T objectToSerialize, string filePath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                serializer.Serialize(writer, objectToSerialize);
            }
        }

        public static string GetUniqueFilePath(string directoryPath, string baseId, string fileExtension)
        {
            // Remove invalid characters from baseId
            string cleanedBaseId = RemoveInvalidFileNameChars(baseId);

            string fileName = $"{cleanedBaseId}{fileExtension}";
            string fullPath = Path.Combine(directoryPath, fileName);
            int counter = 1;

            // Check if the file already exists
            while (File.Exists(fullPath))
            {
                // Modify fileName by appending the counter
                fileName = $"{cleanedBaseId}_{counter}{fileExtension}";
                fullPath = Path.Combine(directoryPath, fileName);
                counter++;
            }

            return Path.Combine(directoryPath, fileName);
        }

        // Characters that Windows file names do not support
        private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

        // Method to remove invalid characters
        public static string RemoveInvalidFileNameChars(string input)
        {
            return Regex.Replace(input, $"[{Regex.Escape(new string(InvalidFileNameChars))}]", "");
        }


        //Returns a list of strings from a CSV
        public static List<string> CSVToList(string commaSeparated)
        {
            // Check if the input string is null or empty
            if (string.IsNullOrEmpty(commaSeparated))
            {
                return new List<string>(); // Return an empty list if null or empty
            }

            // Split the input string by commas, and convert it into a list
            List<string> result = new List<string>(commaSeparated.Split(','));

            // Optionally, you can trim spaces around each item
            for (int i = 0; i < result.Count; i++)
            {
                result[i] = result[i].Trim();
            }

            return result;
        }

    }
}
