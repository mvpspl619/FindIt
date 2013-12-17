using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindIt.ViewModels
{
    public class LocationModel
    {
        public LocationGroupOC Custom { get; set; }
        public LocationGroup Famous { get; set; }
        public PoiGroup Poi { get; set; }
        public LocationGroup GooglePois { get; set; }

        public const string CustomKey = "CustomLocation";

        
        public bool IsDataLoaded { get; set; }

        public void LoadData()
        {
            Custom = LoadCustomLocations();
            Famous = LoadFamousLocations();
            Poi = LoadPoi();
            GooglePois = LoadGooglePois();
            IsDataLoaded = true;
            
        }

        private LocationGroup LoadGooglePois()
        {
            LocationGroup data = new LocationGroup();
            return data;
        }

        private PoiGroup LoadPoi()
        {
            PoiGroup data = new PoiGroup();

            data.Items.Add(new PoiData {  Title="ATMs", Type="atm" });
            data.Items.Add(new PoiData { Title = "Restaurants", Type = "food" });
            data.Items.Add(new PoiData { Title = "Hospitals", Type = "hospital" });
            data.Items.Add(new PoiData { Title = "Police", Type = "police" });
            data.Items.Add(new PoiData { Title = "Pharmacy", Type = "pharmacy" });
            data.Items.Add(new PoiData { Title = "Fuel", Type = "gas_station" });
            data.Items.Add(new PoiData { Title = "Temple", Type = "hindu_temple" });
            data.Items.Add(new PoiData { Title = "Train Station", Type = "train_station" });
            data.Items.Add(new PoiData { Title = "Movie Theater", Type = "movie_theater" });
            return data;
        }

        private LocationGroupOC LoadCustomLocations()
        {
            string dataFromAppSettings;
            LocationGroupOC data;

            if (IsolatedStorageSettings.ApplicationSettings.TryGetValue(CustomKey, out dataFromAppSettings))
            {
                data = JsonConvert.DeserializeObject<LocationGroupOC>(dataFromAppSettings);
            }
            else
            { 
                data = new LocationGroupOC();
            }

            return data;
        }

        private LocationGroup LoadFamousLocations()
        {
            LocationGroup data = new LocationGroup();

            data.Items.Add(new LocationData { Title = "Taj Mahal", Latitude = 27.1731476, Longitude = 78.0420685, Name = "TajMahal", Uri = "/Assets/Famous/TajMahal.jpeg"});
            data.Items.Add(new LocationData { Title = "Colosseum", Latitude = 41.8902680, Longitude = 12.4923149, Name = "Colosseum", Uri = "/Assets/Famous/Colosseum.jpeg"});
            data.Items.Add(new LocationData { Title = "Great Sphinx", Latitude = 29.975271, Longitude = 31.137551, Name = "GreatSphinx", Uri = "/Assets/Famous/GreatSphinx.jpeg"});
            data.Items.Add(new LocationData { Title = "Stone Henge", Latitude = 51.1788997, Longitude = -1.8261171, Name = "StoneHenge", Uri = "/Assets/Famous/StoneHenge.jpeg"});
            data.Items.Add(new LocationData { Title = "Leaning Tower", Latitude = 43.722952, Longitude = 10.3965969, Name = "LeaningTower", Uri = "/Assets/Famous/LeaningTower.jpeg"});
            data.Items.Add(new LocationData { Title = "Great Wall", Latitude = 15.073070, Longitude = 102.2174129, Name = "GreatWall", Uri = "/Assets/Famous/GreatWall.jpeg" });
            return data;
        }
    }


}
