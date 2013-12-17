using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindIt.ViewModels
{
    public class PoiGroup
    {
        public PoiGroup()
        {
            Items = new ObservableCollection<PoiData>();
        }
        public ObservableCollection<PoiData> Items { get; set; }
        public string Title { get; set; }
    }
}
