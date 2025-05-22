using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOS_VirtualCones_MCB.Models
{
    public class StructureInfo : ICloneable
    {
        public string StructureID { get; set; }
        public string Type { get; set; }
        public List<StructureCoding> StructureCodes { get; set; }

        public object Clone()
        {
            var cloned = new StructureInfo();

            cloned.StructureID = StructureID;
            cloned.Type = Type;
            if (StructureCodes != null)
            {
                cloned.StructureCodes = new List<StructureCoding>(StructureCodes?.Select(sc => (StructureCoding)sc.Clone()));
            }

            return cloned;
        }
    }

    public class StructureCoding : ICloneable
    {
        public string Code { get; set; }
        public string CodeScheme { get; set; }
        public string CodeSchemeVersion { get; set; }

        public object Clone()
        {
            return new StructureCoding
            {
                Code = Code,
                CodeScheme = CodeScheme,
                CodeSchemeVersion = CodeSchemeVersion
            };
        }
    }
}

