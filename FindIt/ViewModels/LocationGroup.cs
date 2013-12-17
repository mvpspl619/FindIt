using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindIt.ViewModels
{
    public class LocationGroup
    {
        public LocationGroup()
        {
           Items = new List<LocationData>();
        }

        public List<LocationData> Items { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        //1. Famous (Saved Images)
        //2. Custom Saved (Image from IsoStore)
        //3. Pois (Custom images for categories)
    }
}
