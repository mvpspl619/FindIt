using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Net.Http;
using Newtonsoft.Json;
using FindIt.ViewModels;
using FindIt.Resources;
using System.Device.Location;
using Microsoft.Phone.Maps.Toolkit;
using Microsoft.Phone.Maps.Controls;
using Coding4Fun.Toolkit.Controls;
using System.IO.IsolatedStorage;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Diagnostics;

namespace FindIt
{
    public partial class SearchLocation : PhoneApplicationPage
    {
        double latitude;
        double longitude;
        string locationName;
        string title;
        string reference;
        string imageUri;
        
        public SearchLocation()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }

        private void Search_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            
            bool connectionStatus = checkConnection();
            if (!connectionStatus)
            {
                MessageDisplayBox.Visibility = Visibility.Visible;
                MessageDisplayBox.Text = "!! Please check your internet connection !!";
            }
            else
                performSearch();
        }

        private async void performSearch()
        {
            bool photoFound = false;
            MessageDisplayBox.Visibility = Visibility.Collapsed;
            if (HelpText.Height != 0)
            {
                HideTextBlock.Begin();
            }

            SystemTray.ProgressIndicator = new ProgressIndicator();

            SetProgressIndicator(true);
            SystemTray.ProgressIndicator.Text = "Searching";

            string searchText = SearchText.Text;
            //Enforce string checking algorithms

            searchText = searchText.Replace(" ", "+");
            //http://maps.googleapis.com/maps/api/geocode/json?address=1600+Amphitheatre+Parkway,+Mountain+View,+CA&sensor=true_or_false

            HttpClient client = new HttpClient();

            string url = "http://maps.googleapis.com/maps/api/geocode/json" +
                "?address={0}" +
                "&sensor=false";
            string baseUrl = string.Format(url, searchText);

            string googleResult = await client.GetStringAsync(baseUrl);
            SystemTray.ProgressIndicator.Text = "Done";
            SetProgressIndicator(false);

            GoogleData apiData = JsonConvert.DeserializeObject<GoogleData>(googleResult);

            if (apiData.status == "OK")
            {
                foreach (FindIt.ViewModels.Result data in apiData.results)
                {
                    latitude = data.geometry.location.lat;
                    longitude = data.geometry.location.lng;

                    foreach (FindIt.ViewModels.AddressComponent addressData in data.address_components)
                    {
                        locationName = addressData.long_name;
                        break;
                    }
                    string photoBaseUrl = string.Format("https://maps.googleapis.com/maps/api/place/nearbysearch/json" 
                        + "?key=AIzaSyAA7S81XKyj4zbp6-ZkYk_zPWkUHRgb2oo" 
                        + "&location={0},{1}" 
                        + "&radius=500" 
                        + "&sensor=false",latitude, longitude);
                    string googlePhotoResult = await client.GetStringAsync(photoBaseUrl);
                    GooglePhotoData photoData = JsonConvert.DeserializeObject<GooglePhotoData>(googlePhotoResult);
                    if (photoData.results.Count != 0)
                    {
                        foreach (FindIt.Result resultData in photoData.results)
                        {
                            if (photoFound)
                                break;
                            else
                            {
                                if (resultData.photos != null)
                                {
                                    foreach (FindIt.Photo photoResult in resultData.photos)
                                    {
                                        reference = photoResult.photo_reference;
                                        //PhotoResultTextBlock.Text = reference;
                                        
                                        //ResultImage.Source = LoadImageFromIsolatedStorage();
                                        photoFound = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Sorry, no images are found for the searched location");
                        imageUri = "";//a local uri
                    }
                    break;
                }

                var mapCenter = new GeoCoordinate(latitude, longitude);
                MapLayer myLayer = new MapLayer();
                Pushpin pin = new Pushpin();
                pin.Content = locationName;

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
            else
            {
                MessageDisplayBox.Visibility = Visibility.Visible;
                MessageDisplayBox.Text = "!! Enter a place name to search !!";
                UnhideTextBlock.Begin();
            }
        }

        private void SaveImageToIsolatedStorage(string imageUri)
        {
            // Use WebClient to download web server's images.
            WebClient webClientImg = new WebClient();
            webClientImg.OpenReadCompleted += new OpenReadCompletedEventHandler(client_OpenReadCompleted);
            webClientImg.OpenReadAsync(new Uri(imageUri, UriKind.Absolute));

        }

        void client_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            // Save the returned image stream into a jpeg picture.
            SaveToJpeg(e.Result);
        }

        private void SaveToJpeg(Stream stream)
        {
            using (IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (iso.FileExists(string.Format("{0}.jpeg", title)))
                    iso.DeleteFile(string.Format("{0}.jpeg", title));
                using (IsolatedStorageFileStream isostream = iso.CreateFile(string.Format("{0}.jpeg",title)))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.SetSource(stream);
                    WriteableBitmap wb = new WriteableBitmap(bitmap);

                    // Encode WriteableBitmap object to a JPEG stream.
                    Extensions.SaveJpeg(wb, isostream, wb.PixelWidth, wb.PixelHeight, 0, 85);
                    isostream.Close();
                }
            }
        }

        private BitmapImage LoadImageFromIsolatedStorage()
        {
            // The image will be read from isolated storage into the following byte array
            byte[] data;
            BitmapImage bi = new BitmapImage();


            // Read the entire image in one go into a byte array
            try
            {
                using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    // Open the file - error handling omitted for brevity
                    // Note: If the image does not exist in isolated storage the following exception will be generated:
                    // System.IO.IsolatedStorage.IsolatedStorageException was unhandled 
                    // Message=Operation not permitted on IsolatedStorageFileStream 
                    using (IsolatedStorageFileStream isfs = isf.OpenFile(string.Format("{0}.jpeg",title), FileMode.Open, FileAccess.Read))
                    {
                        // Allocate an array large enough for the entire file
                        data = new byte[isfs.Length];

                        // Read the entire file and then close it
                        isfs.Read(data, 0, data.Length);
                        isfs.Close();
                    }
                }

                // Create memory stream and bitmap
                MemoryStream ms = new MemoryStream(data);
                
                // Set bitmap source to memory stream
                bi.SetSource(ms);
            }
            catch (Exception)
            {
                
            }
            return bi;
        }
        
        private string GetImageUri(string reference)
        {
            string uri = string.Format("https://maps.googleapis.com/maps/api/place/photo" 
                + "?maxwidth=400"
                + "&photoreference={0}"
                + "&sensor=false"
                + "&key=AIzaSyAA7S81XKyj4zbp6-ZkYk_zPWkUHRgb2oo", reference);
            return uri;
        }

        private static bool checkConnection()
        {
            return Microsoft.Phone.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
        }

        private void Add_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (latitude == 0 && longitude == 0)
            {
                MessageDisplayBox.Visibility = Visibility.Visible;
                MessageDisplayBox.Text = "!! You need to search for location before adding !!";
                return;
            }
            else
            {
                InputPrompt titleBox = new InputPrompt();

                titleBox.Title = "Name of this location ?";
                titleBox.Show();
                titleBox.Completed += titleBox_Completed;
            }
        }

        private void titleBox_Completed(object sender, PopUpEventArgs<string, PopUpResult> e)
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
                    LocationData locationData = new LocationData();
                    locationData.Title = title;
                    if (latitude == 0 && longitude == 0)
                    {
                        MessageBox.Show("Please search for the location again");
                        return;
                    }
                    locationData.Name = locationName;
                    locationData.Latitude = latitude;
                    locationData.Longitude = longitude;

                    if (reference != null)
                    {
                        imageUri = GetImageUri(reference);
                        SaveImageToIsolatedStorage(imageUri);
                    }
                    SaveLocationData(locationData);
                }
            }
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
            catch (IsolatedStorageException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void SetProgressIndicator(bool IsVisible)
        {
            SystemTray.ProgressIndicator.IsIndeterminate = IsVisible;
            SystemTray.ProgressIndicator.IsVisible = IsVisible;
        }

        private void LocationMap_Loaded(object sender, RoutedEventArgs e)
        {
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.ApplicationId = "422b0fd4-e12b-44c3-a0f7-246907c7639b";
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.AuthenticationToken = "mO07flYO2QgCoqtiTjmAhw";
        }
    }
}