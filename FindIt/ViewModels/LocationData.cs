using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindIt.ViewModels
{
    public class LocationData
    {
        public string Title { get; set; }
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Uri { get; set; }
        public double DistanceToUser { get; set; }
    }
}
