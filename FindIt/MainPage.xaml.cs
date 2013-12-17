    using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Windows.Devices.Geolocation;
using System.Device.Location;
using FindIt.Resources;
using FindIt.ViewModels;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO.IsolatedStorage;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Net.Http;
using System.Threading;
using System.Windows.Media;
using Windows.Foundation;

namespace FindIt
{
    public partial class MainPage : PhoneApplicationPage
    {
        const string key = "AIzaSyAA7S81XKyj4zbp6-ZkYk_zPWkUHRgb2oo";
        double load = 1;
        CancellationTokenSource ctsSearch = new CancellationTokenSource();
        CancellationTokenSource ctsPoi = new CancellationTokenSource();
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Set the data context of the listbox control to the sample data
            DataContext = App.ViewModel;
            
            Loaded +=MainPage_Loaded;
            BuildLocalizedApplicationBar();
        }

        private void BuildLocalizedApplicationBar()
        {
            return;
        }

        void appBarButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/AddLocation.xaml", UriKind.RelativeOrAbsolute));
        }

        async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (load == 1)
            {
                IsolatedStorageSettings iso = IsolatedStorageSettings.ApplicationSettings;
                if (iso.Contains("loadNumber"))
                {
                    double temp = Convert.ToDouble(iso["loadNumber"]);
                    iso["loadNumber"] = ++temp;
                    iso.Save();
                }
                else
                {
                    iso["loadNumber"] = 1;
                }
                if (!iso.Contains("hasReviewed"))
                {
                    iso["hasReviewed"] = false;
                }

                if (!iso.Contains("newFeaturesShown"))
                {
                    iso["newFeaturesShown"] = true;
                    MessageBox.Show("Now you can set the number of search results that you see. Check the new option in settings page. And we added some magic to show only the results closest to you :)", "New features!", MessageBoxButton.OK);
                }
                ++load;


                Geolocator geoLocator = new Geolocator();

                if (geoLocator.LocationStatus == PositionStatus.Disabled)
                {
                    MessageBoxResult result = MessageBox.Show("Your location services are turned off. To turn them on, click on OK.", "Location services are off", MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.OK)
                    {
                        var launcher = await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-location:"));
                    }
                    return;
                }
                return;
            }
        }
        
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            while (NavigationService.CanGoBack)
            {
                NavigationService.RemoveBackEntry();
            }
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            LocationData locationData = (sender as MenuItem).DataContext as LocationData;
            MessageBoxResult result = MessageBox.Show("Do you want to delete this location?","Are you sure?", MessageBoxButton.OKCancel);

            if (result == MessageBoxResult.OK)
            {
                DeleteLocationData(locationData);
            }
        }

        private void DeleteLocationData(LocationData locationData)
        {
            IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

            try
            {
                using (IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (iso.FileExists(string.Format("{0}.jpeg", locationData.Title)))
                        iso.DeleteFile(string.Format("{0}.jpeg", locationData.Title));
                }

                App.ViewModel.Custom.Items.Remove(locationData);
                var data = JsonConvert.SerializeObject(App.ViewModel.Custom);
                appSettings[LocationModel.CustomKey] = data;
                appSettings.Save();
                NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.RelativeOrAbsolute));
            }
            catch (IsolatedStorageException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void LongListSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LongListSelector selector = sender as LongListSelector;

            // verifying our sender is actually a LongListSelector
            if (selector == null)
                return;
            LocationData data = selector.SelectedItem as LocationData;
            if (data != null)
            {
                LocationGroupOC locGroup = new LocationGroupOC();
                locGroup.Items.Add(data);
                locGroup.Type = "saved";
                string serializedLocationGroup = JsonConvert.SerializeObject(locGroup);
                string navigateUrl = string.Format("/ViewDirection.xaml?serializedData={0}&isOC=true", serializedLocationGroup);
                NavigationService.Navigate(new Uri(navigateUrl, UriKind.RelativeOrAbsolute));
            }
            selector.SelectedItem = null;
        }

        private async Task<GeoCoordinate> GetLocation(CancellationToken ct)
        {
            GeoCoordinate location = new GeoCoordinate();
            bool isLocationEnabled = true;
            IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

            if(appSettings.Contains("locationEnabled"))
                isLocationEnabled = Convert.ToBoolean(appSettings["locationEnabled"]);
            if(!isLocationEnabled)
                MessageBox.Show("Please allow access to location in settings");
            else
            {
            if (!checkLocationAvailability())
                {
                    MessageBoxResult result = MessageBox.Show("Your location services are turned off. To turn them on, click on OK.", "Location services are off", MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.OK)
                    {
                        var launcher = await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-location:"));
                    }
                }
                else
                {
                    Geolocator geolocator = new Geolocator();
                    geolocator.DesiredAccuracy = PositionAccuracy.Default;

                    IAsyncOperation<Geoposition> locationTask = null;
                    Geoposition position;
                    try
                    {
                        locationTask = geolocator.GetGeopositionAsync(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(15));
                        position = await locationTask;
                    }
                    finally
                    {
                        if (locationTask != null)
                        {
                            if (locationTask.Status == AsyncStatus.Started)
                                locationTask.Cancel();

                            locationTask.Close();
                        }
                    }
                    
                    SystemTray.ProgressIndicator.Text = "Acquiring GPS location";
                    location.Latitude = position.Coordinate.Latitude;
                    location.Longitude = position.Coordinate.Longitude;
                }
            }
            
            return location;    
        }

        void geoLocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            
        }


        private string GenerateUrl(GeoCoordinate coordinate, string category)
        {
            double radius;
            string url = "";
            IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;
            if (!appSettings.Contains("radius"))
                radius = 500;
            else
            {
                radius = (double)appSettings["radius"];
                radius = (radius) * 200;
                radius = Math.Round(radius, 0);
            }
            url = string.Format("https://maps.googleapis.com/maps/api/place/nearbysearch/json?"
                + "location={0},{1}"
                + "&radius={2}"
                + "&keyword={3}"
                + "&sensor=false"
                + "&key={4}", coordinate.Latitude, coordinate.Longitude, radius, category, key);
            return url;
        }

       private static async Task<string> GetJsonDataFromGoogle(string navigateUrl, string category)
        {
            string jsonResult = "";
            try
            {
                if (!checkConnection())
                {
                    MessageBox.Show("Please check your internet connection and try again");
                    return null;
                }
                else
                {
                    if (navigateUrl != null)
                    {
                        //SystemTray.ProgressIndicator = new ProgressIndicator();
                        //SetProgressIndicator(true);
                        //SystemTray.ProgressIndicator.Text = "Getting data from google";
                        string baseUrl = navigateUrl;
                        HttpClient client = new HttpClient();
                        SystemTray.ProgressIndicator.Text = "Getting data from Google";
                        jsonResult = await client.GetStringAsync(baseUrl);
                        //SystemTray.ProgressIndicator.Text = "Done";
                        //SetProgressIndicator(false);
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("An error occured while sending the request. Please try again.");
                SetProgressIndicator(false);
            }
            return jsonResult;
        }

        private string SerializeJsonData(string jsonData, string category)
        {
            string uri = "";
            if (jsonData == "")
                return uri;
            else
            {
                GooglePhotoData poiData = JsonConvert.DeserializeObject<GooglePhotoData>(jsonData);
                if (poiData.results.Count != 0)
                {
                    LocationGroup poisGroup = new LocationGroup();
                    poisGroup.Title = category;
                    foreach (FindIt.Result resultData in poiData.results)
                    {
                        poisGroup.Items.Add(new LocationData
                        {
                            Latitude = resultData.geometry.location.lat,
                            Longitude = resultData.geometry.location.lng,
                            Name = resultData.name,
                            Title = resultData.name
                        });
                    }
                    poisGroup.Type = "pois";
                    uri = JsonConvert.SerializeObject(poisGroup);

                    string changedUri = uri.Replace("&", " ");

                    return changedUri;
                }
                else
                {
                    MessageBox.Show("Sorry, we couldn't find what you wanted. Either your location services are off or your search radius is too small.", "Sorry!", MessageBoxButton.OK );
                    return null;
                }
            }
        }

        
        private static void SetProgressIndicator(bool IsVisible)
        {
 	        SystemTray.ProgressIndicator.IsIndeterminate = IsVisible;
            SystemTray.ProgressIndicator.IsVisible = IsVisible;
        }

        private void settingsMenu_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.RelativeOrAbsolute));
        }

        private static bool checkConnection()
        {
            return Microsoft.Phone.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
        }

        private static bool checkLocationAvailability()
        {
            Geolocator geo = new Geolocator();
            if (geo.LocationStatus == PositionStatus.Disabled)
                return false;
            else
                return true;
        }

        private async void SearchForPoi_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                if (ctsPoi != null)
                    ctsPoi.Cancel();
                string queryText = SearchTermBox.Text;
                if (queryText == "")
                    MessageBox.Show("Please enter a search term.");
                else
                {
                    SystemTray.ProgressIndicator = new ProgressIndicator();
                    SetProgressIndicator(true);
                    SystemTray.ProgressIndicator.Text = string.Format("Looking for {0}", queryText);
                    GeoCoordinate coordinate = await GetLocation(ctsPoi.Token);
                    if (coordinate.IsUnknown != true)
                    {
                        string passedUrl = GenerateUrl(coordinate, queryText);
                        if (passedUrl != null && passedUrl != "")
                        {
                            string jsonData = await GetJsonDataFromGoogle(passedUrl, queryText);
                            if (jsonData != null && jsonData != "")
                            {
                                string url = SerializeJsonData(jsonData, queryText);
                                if (url != null && url != "")
                                {
                                    string navigateUrl = string.Format("/ViewDirection.xaml?serializedData={0}&isOC=false", url);
                                    (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri(navigateUrl, UriKind.RelativeOrAbsolute));
                                }
                            }
                        }
                    }
                    SetProgressIndicator(false);
                }
            }
            catch
            {
                MessageBox.Show("Oops, an error occured!");
                SystemTray.ProgressIndicator = new ProgressIndicator();
                SetProgressIndicator(false);
            }
        }

        private async void ATMGrid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                if (ctsPoi != null)
                    ctsPoi.Cancel();
                SystemTray.ProgressIndicator = new ProgressIndicator();
                SetProgressIndicator(true);
                SystemTray.ProgressIndicator.Text = "Looking for atms";
                string type = "atm";
                GeoCoordinate coordinate = await GetLocation(ctsPoi.Token);
                if (coordinate.IsUnknown != true)
                {
                    string passedUrl = GenerateUrl(coordinate, type);
                    if (passedUrl != null && passedUrl != "")
                    {
                        string jsonData = await GetJsonDataFromGoogle(passedUrl, type);
                        if (jsonData != null && jsonData != "")
                        {
                            string url = SerializeJsonData(jsonData, type);
                            if (url != null && url != "")
                            {
                                string navigateUrl = string.Format("/ViewDirection.xaml?serializedData={0}&isOC=false", url);
                                //ATMBar.Visibility = Visibility.Collapsed;
                                //ATMGrid.Opacity = 1;
                                NavigationService.Navigate(new Uri(navigateUrl, UriKind.RelativeOrAbsolute));
                            }
                        }
                    }
                }
                SetProgressIndicator(false);
            }
            catch
            {
                MessageBox.Show("Oops, an error occured!");
                SystemTray.ProgressIndicator = new ProgressIndicator();
                SetProgressIndicator(false);
            }
        }

        private async void RestaurantGrid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                if (ctsPoi != null)
                    ctsPoi.Cancel();
                SystemTray.ProgressIndicator = new ProgressIndicator();
                SetProgressIndicator(true);
                SystemTray.ProgressIndicator.Text = "Looking for restaurantss";
                string type = "restaurant";
                GeoCoordinate coordinate = await GetLocation(ctsPoi.Token);
                if (coordinate.IsUnknown != true)
                {
                    string passedUrl = GenerateUrl(coordinate, type);
                    if (passedUrl != null && passedUrl != "")
                    {
                        string jsonData = await GetJsonDataFromGoogle(passedUrl, type);
                        if (jsonData != null && jsonData != "")
                        {
                            string url = SerializeJsonData(jsonData, type);
                            if (url != null && url != "")
                            {
                                string navigateUrl = string.Format("/ViewDirection.xaml?serializedData={0}&isOC=false", url);
                                NavigationService.Navigate(new Uri(navigateUrl, UriKind.RelativeOrAbsolute));
                            }
                        }
                    }
                }
                SetProgressIndicator(false);
            }
            catch
            {
                MessageBox.Show("Oops, an error occured!");
                SystemTray.ProgressIndicator = new ProgressIndicator();
                SetProgressIndicator(false);
            }
        }

        private async void HospitalGrid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                if (ctsPoi != null)
                    ctsPoi.Cancel();
                SystemTray.ProgressIndicator = new ProgressIndicator();
                SetProgressIndicator(true);
                SystemTray.ProgressIndicator.Text = "Looking for hospitals";
                string type = "hospital";
                GeoCoordinate coordinate = await GetLocation(ctsPoi.Token);
                if (coordinate.IsUnknown != true)
                {
                    string passedUrl = GenerateUrl(coordinate, type);
                    if (passedUrl != null && passedUrl != "")
                    {
                        string jsonData = await GetJsonDataFromGoogle(passedUrl, type);
                        if (jsonData != null && jsonData != "")
                        {
                            string url = SerializeJsonData(jsonData, type);
                            if (url != null && url != "")
                            {
                                string navigateUrl = string.Format("/ViewDirection.xaml?serializedData={0}&isOC=false", url);
                                NavigationService.Navigate(new Uri(navigateUrl, UriKind.RelativeOrAbsolute));
                            }
                        }
                    }
                }
                SetProgressIndicator(false);
            }
            catch
            {
                MessageBox.Show("Oops, an error occured!");
                SystemTray.ProgressIndicator = new ProgressIndicator();
                SetProgressIndicator(false);
            }
        }

        private async void PoliceGrid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                if (ctsPoi != null)
                    ctsPoi.Cancel();
                SystemTray.ProgressIndicator = new ProgressIndicator();
                SetProgressIndicator(true);
                SystemTray.ProgressIndicator.Text = "Looking for police";
                string type = "police";
                GeoCoordinate coordinate = await GetLocation(ctsPoi.Token);
                if (coordinate.IsUnknown != true)
                {
                    string passedUrl = GenerateUrl(coordinate, type);
                    if (passedUrl != null && passedUrl != "")
                    {
                        string jsonData = await GetJsonDataFromGoogle(passedUrl, type);
                        if (jsonData != null && jsonData != "")
                        {
                            string url = SerializeJsonData(jsonData, type);
                            if (url != null && url != "")
                            {
                                string navigateUrl = string.Format("/ViewDirection.xaml?serializedData={0}&isOC=false", url);
                                NavigationService.Navigate(new Uri(navigateUrl, UriKind.RelativeOrAbsolute));
                            }
                        }
                    }
                }
                SetProgressIndicator(false);
            }
            catch
            {
                MessageBox.Show("Oops, an error occured!");
                SystemTray.ProgressIndicator = new ProgressIndicator();
                SetProgressIndicator(false);
            }
        }

        private async void PharmacyGrid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                if (ctsPoi != null)
                    ctsPoi.Cancel();
                SystemTray.ProgressIndicator = new ProgressIndicator();
                SetProgressIndicator(true);
                SystemTray.ProgressIndicator.Text = "Looking for pharmacy";
                string type = "pharmacy";
                GeoCoordinate coordinate = await GetLocation(ctsPoi.Token);
                if (coordinate.IsUnknown != true)
                {
                    string passedUrl = GenerateUrl(coordinate, type);
                    if (passedUrl != null && passedUrl != "")
                    {
                        string jsonData = await GetJsonDataFromGoogle(passedUrl, type);
                        if (jsonData != null && jsonData != "")
                        {
                            string url = SerializeJsonData(jsonData, type);
                            if (url != null && url != "")
                            {
                                string navigateUrl = string.Format("/ViewDirection.xaml?serializedData={0}&isOC=false", url);
                                NavigationService.Navigate(new Uri(navigateUrl, UriKind.RelativeOrAbsolute));
                            }
                        }
                    }
                }
                SetProgressIndicator(false);
            }
            catch
            {
                MessageBox.Show("Oops, an error occured!");
                SystemTray.ProgressIndicator = new ProgressIndicator();
                SetProgressIndicator(false);
            }
        }

        private async void FuelGrid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                if (ctsPoi != null)
                    ctsPoi.Cancel();
                SystemTray.ProgressIndicator = new ProgressIndicator();
                SetProgressIndicator(true);
                SystemTray.ProgressIndicator.Text = "Looking for fuel stations";
                string type = "gas station";
                GeoCoordinate coordinate = await GetLocation(ctsPoi.Token);
                if (coordinate.IsUnknown != true)
                {
                    string passedUrl = GenerateUrl(coordinate, type);
                    if (passedUrl != null && passedUrl != "")
                    {
                        string jsonData = await GetJsonDataFromGoogle(passedUrl, type);
                        if (jsonData != null && jsonData != "")
                        {
                            string url = SerializeJsonData(jsonData, type);
                            if (url != null && url != "")
                            {
                                string navigateUrl = string.Format("/ViewDirection.xaml?serializedData={0}&isOC=false", url);
                                NavigationService.Navigate(new Uri(navigateUrl, UriKind.RelativeOrAbsolute));
                            }
                        }
                    }
                }
                SetProgressIndicator(false);
            }
            catch
            {
                MessageBox.Show("Oops, an error occured!");
                SystemTray.ProgressIndicator = new ProgressIndicator();
                SetProgressIndicator(false);
            }
        }

        private async void TrainGrid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                if (ctsPoi != null)
                    ctsPoi.Cancel();
                SystemTray.ProgressIndicator = new ProgressIndicator();
                SetProgressIndicator(true);
                SystemTray.ProgressIndicator.Text = "Looking for train stations";
                string type = "train station";
                GeoCoordinate coordinate = await GetLocation(ctsPoi.Token);
                if (coordinate.IsUnknown != true)
                {
                    string passedUrl = GenerateUrl(coordinate, type);
                    if (passedUrl != null && passedUrl != "")
                    {
                        string jsonData = await GetJsonDataFromGoogle(passedUrl, type);
                        if (jsonData != null && jsonData != "")
                        {
                            string url = SerializeJsonData(jsonData, type);
                            if (url != null && url != "")
                            {
                                string navigateUrl = string.Format("/ViewDirection.xaml?serializedData={0}&isOC=false", url);
                                NavigationService.Navigate(new Uri(navigateUrl, UriKind.RelativeOrAbsolute));
                            }
                        }
                    }
                }
                SetProgressIndicator(false);
            }
            catch
            {
                MessageBox.Show("Oops, an error occured!");
                SystemTray.ProgressIndicator = new ProgressIndicator();
                SetProgressIndicator(false);
            }
        }

        private async void MovieGrid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                if (ctsPoi != null)
                    ctsPoi.Cancel();
                SystemTray.ProgressIndicator = new ProgressIndicator();
                SetProgressIndicator(true);
                SystemTray.ProgressIndicator.Text = "Looking for movie theaters";
                string type = "movie theater";
                GeoCoordinate coordinate = await GetLocation(ctsPoi.Token);
                if (coordinate.IsUnknown != true)
                {
                    string passedUrl = GenerateUrl(coordinate, type);
                    if (passedUrl != null && passedUrl != "")
                    {
                        string jsonData = await GetJsonDataFromGoogle(passedUrl, type);
                        if (jsonData != null && jsonData != "")
                        {
                            string url = SerializeJsonData(jsonData, type);
                            if (url != null && url != "")
                            {
                                string navigateUrl = string.Format("/ViewDirection.xaml?serializedData={0}&isOC=false", url);
                                NavigationService.Navigate(new Uri(navigateUrl, UriKind.RelativeOrAbsolute));
                            }
                        }
                    }
                }
                SetProgressIndicator(false);
            }
            catch
            {
                MessageBox.Show("Oops, an error occured!");
                SystemTray.ProgressIndicator = new ProgressIndicator();
                SetProgressIndicator(false);
            }
        }

        private void aboutMenu_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/About.xaml", UriKind.RelativeOrAbsolute));
        }

        private void ATMGrid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ATMGrid.Background = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0));
        }

        private void ATMGrid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ATMGrid.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        }

        private void RestaurantGrid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            RestaurantGrid.Background = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0));
        }

        private void RestaurantGrid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            RestaurantGrid.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        }

        private void HospitalGrid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            HospitalGrid.Background = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0));
        }

        private void HospitalGrid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            HospitalGrid.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        }

        private void PoliceGrid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            PoliceGrid.Background = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0));
        }

        private void PoliceGrid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            PoliceGrid.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        }

        private void PharmacyGrid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            PharmacyGrid.Background = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0));
        }

        private void PharmacyGrid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            PharmacyGrid.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        }

        private void FuelGrid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            FuelGrid.Background = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0));
        }

        private void FuelGrid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            FuelGrid.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        }

        private void TrainGrid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrainGrid.Background = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0));
        }

        private void TrainGrid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrainGrid.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        }

        private void MovieGrid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            MovieGrid.Background = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0));
        }

        private void MovieGrid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            MovieGrid.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, CancelEventArgs e)
        {
            IsolatedStorageSettings iso = IsolatedStorageSettings.ApplicationSettings;
            bool review = Convert.ToBoolean(iso["hasReviewed"]);
            MessageBoxResult result = MessageBox.Show("Are you sure you want to exit ?", "Exit app !", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                if (review == false)
                {
                    double temp = Convert.ToDouble(iso["loadNumber"]);
                    if (temp == 1 || temp == 6 || temp == 15)
                    {
                        MessageBoxResult rslt = MessageBox.Show("If you like our app, would you like to rate it ? Reviews inspire us to develop more useful applications.", "Rate and Review", MessageBoxButton.OKCancel);
                        if (rslt == MessageBoxResult.OK)
                        {
                            MarketplaceReviewTask mktp = new MarketplaceReviewTask();
                            mktp.Show();
                            iso["hasReviewed"] = true;
                            iso.Save();
                        }
                    }
                }
            }
            else if (result == MessageBoxResult.Cancel)
                e.Cancel = true;
        }
    }
}