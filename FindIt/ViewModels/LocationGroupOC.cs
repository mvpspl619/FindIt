using System;
using System.Collections.Generic;

using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindIt.ViewModels
{
    public class LocationGroupOC
    {
        public LocationGroupOC()
        {
           Items = new ObservableCollection<LocationData>();
        }

        public ObservableCollection<LocationData> Items { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
    }
}
