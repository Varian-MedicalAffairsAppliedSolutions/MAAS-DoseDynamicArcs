using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualCones_MCB.Models
{

    public class GantryWeightMap : ObservableObject
    {
        public GantryWeightMap()
        {
            pairs = new List<GantryWeightPair>();
        }

        private string mapId = string.Empty;
        public string MapId
        {
            get => mapId;
            set => SetProperty(ref mapId, value);
        }
 

        public List<GantryWeightPair> pairs { get; set; }
    }

    public class GantryWeightPair
    {
        public GantryWeightPair() { }

        public double Gantry { get; set; }
        public double Weight { get; set; }
    }
}
