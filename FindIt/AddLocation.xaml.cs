using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Windows.Devices.Geolocation;
using Coding4Fun.Toolkit.Controls;
using FindIt.ViewModels;
using System.IO.IsolatedStorage;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Device.Location;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Toolkit;

namespace FindIt
{
    public partial class AddLocation : PhoneApplicationPage
    {
        CancellationTokenSource cts;
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string title { get; set; }

        public AddLocation()
        {
            InitializeComponent();
            Loaded += AddLocation_Loaded;
            
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }

        private async void AddLocation_Loaded(object sender, RoutedEventArgs e)
        {
            cts = new CancellationTokenSource();
            await GetUserLocation(cts.Token);
            cts = null;
        }

        private async Task GetUserLocation(CancellationToken ct)
        {
            bool isLocationEnabled = true;
            IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;
            if (appSettings.Contains("locationEnabled"))
                isLocationEnabled = Convert.ToBoolean(appSettings["locationEnabled"]);
            if (!isLocationEnabled)
                MessageBox.Show("Please allow access to location in settings");
            else
            {
                try
                {
                    MessageDisplayBox.Visibility = Visibility.Collapsed;
                    latitude = 0;
                    longitude = 0;
                    SystemTray.ProgressIndicator = new ProgressIndicator();
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
                    else
                    {
                        geoLocator.DesiredAccuracyInMeters = 50;
                        while (geoLocator.LocationStatus == PositionStatus.Initializing)
                        {
                            System.Threading.Thread.Sleep(100);
                        }
                        SetProgressIndicator(true);
                        SystemTray.ProgressIndicator.Text = "Acquiring GPS location";
                        Geoposition position = await geoLocator.GetGeopositionAsync(
                            TimeSpan.FromMinutes(1),
                            TimeSpan.FromSeconds(30)).AsTask(ct);
                        SystemTray.ProgressIndicator.Text = "Acquired";
                        MessageDisplayBox.Text = "Location acquired";
                        MessageDisplayBox.Visibility = Visibility.Visible;
                        SetProgressIndicator(false);
                        latitude = position.Coordinate.Latitude;
                        longitude = position.Coordinate.Longitude;
                        LoadMap();
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
        }

        private void LoadMap()
        {
            var mapCenter = new GeoCoordinate(latitude, longitude);
            MapLayer myLayer = new MapLayer();
            Pushpin pin = new Pushpin();
            pin.Content = String.Format("ME");

            //pin.GeoCoordinate = mapCenter;
            pin.GeoCoordinate = mapCenter;

            MapOverlay myOverlay = new MapOverlay();
            myOverlay.Content = pin;
            myOverlay.GeoCoordinate = mapCenter;
            myOverlay.PositionOrigin = new Point(0, 1);
            myLayer.Add(myOverlay);

            LocationMap.Visibility = Visibility.Visible;
            LocationMap.Layers.Add(myLayer);
            LocationMap.Center = mapCenter;
            LocationMap.ZoomLevel = 10;
        }

        private void UseCurrentLocation_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            //Send the lat long data for saving
            
            InputPrompt fileName = new InputPrompt();

            fileName.Title = "Name of this location ?";
            fileName.Completed += fileName_Completed;
            fileName.Show();
        }

        private void fileName_Completed(object sender, PopUpEventArgs<string, PopUpResult> e)
        {
            
            if (e.PopUpResult == PopUpResult.Ok)
            {
                System.Text.RegularExpressions.Regex regEx = new System.Text.RegularExpressions.Regex("^[a-zA-Z][a-zA-Z\\s]+$");
                string strText1 = e.Result;

                if (!regEx.IsMatch(strText1))
                    MessageBox.Show("Only alphabets and spaces allowed");
                else
                {
                    title = e.Result;
                    title = title.ToLower();

                    LocationData locationData = new LocationData();
                    locationData.Title = title;
                    if (latitude == 0 && longitude == 0)
                    {
                        Geolocator geo = new Geolocator();
                        if (geo.LocationStatus == PositionStatus.Disabled)
                        {
                            MessageBox.Show("Please enable location services and try again.");
                        }
                        else
                        {
                            MessageBox.Show("Please wait until we detect your location");
                        }
                        return;
                    }
                    locationData.Latitude = latitude;
                    locationData.Longitude = longitude;
                    locationData.Name = "custom";

                    SaveLocationData(locationData);
                }
            }
            return;
        }

        private void SaveLocationData(LocationData locationData)
        {
            IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

            try
            {
                App.ViewModel.Custom.Items.Add(locationData);
                var data = JsonConvert.SerializeObject(App.ViewModel.Custom);

                appSettings[LocationModel.CustomKey] = data;
                appSettings.Save();

                //Notify that data is changed
                App.ViewModel.LoadData();
                NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.RelativeOrAbsolute));
            }
            catch(IsolatedStorageException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        

        private void SetProgressIndicator(bool IsVisible)
        {
            SystemTray.ProgressIndicator.IsIndeterminate = IsVisible;
            SystemTray.ProgressIndicator.IsVisible = IsVisible;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (cts != null)
            {
                cts.Cancel();
            }
            base.OnNavigatedFrom(e);
        }

        private void SearchForLocation_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            SystemTray.ProgressIndicator = new ProgressIndicator();
            SetProgressIndicator(false);
            if (cts != null)
            {
                cts.Cancel();
            }
            NavigationService.Navigate(new Uri("/SearchLocation.xaml", UriKind.RelativeOrAbsolute));
        }

        private void AddUsingLatLon_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            SystemTray.ProgressIndicator = new ProgressIndicator();
            SetProgressIndicator(false);
            if (cts != null)
            {
                cts.Cancel();
            }
            NavigationService.Navigate(new Uri("/AddUsingLatLon.xaml", UriKind.RelativeOrAbsolute));
        }

        private void LocationMap_Loaded(object sender, RoutedEventArgs e)
        {
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.ApplicationId = "422b0fd4-e12b-44c3-a0f7-246907c7639b";
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.AuthenticationToken = "mO07flYO2QgCoqtiTjmAhw";
        }
    }
}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             