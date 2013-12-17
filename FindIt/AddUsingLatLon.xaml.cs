using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Coding4Fun.Toolkit.Controls;
using FindIt.ViewModels;
using System.IO.IsolatedStorage;
using Newtonsoft.Json;

namespace FindIt
{
    public partial class AddUsingLatLon : PhoneApplicationPage
    {
        double latitude = 0;
        double longitude = 0;
        public AddUsingLatLon()
        {
            InitializeComponent();
        }

        private void Add_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (Latitude.Text == "")
            {
                MessageBox.Show("Please enter a value for latitude");
            }
            else if (Longitude.Text == "")
            {
                MessageBox.Show("Please enter a value for longitude");
            }
            else
            {

                latitude = Convert.ToDouble(Latitude.Text);
                longitude = Convert.ToDouble(Longitude.Text);

                if (-90 <= latitude && latitude <= 90)
                {
                    if (-180 <= longitude && longitude <= 180)
                    {

                        InputPrompt textbox = new InputPrompt();
                        textbox.Title = "Name of this location ?";
                        textbox.Completed += textbox_Completed;
                        textbox.Show();
                    }
                    else
                    {
                        MessageBox.Show("The value of longitude ranges from -180 to 180");
                    }
                }
                else
                {
                    MessageBox.Show("The value of latitude ranges from -90 to 90.");
                }
            }
        }

        void textbox_Completed(object sender, PopUpEventArgs<string, PopUpResult> e)
        {
            if (e.PopUpResult == PopUpResult.Ok)
            { 
                System.Text.RegularExpressions.Regex regEx = new System.Text.RegularExpressions.Regex("^[a-zA-Z][a-zA-Z\\s]+$");
                string strText1 = e.Result;

                if (!regEx.IsMatch(strText1))
                    MessageBox.Show("Only alphabets and spaces allowed");
                else
                {
                    string title = e.Result;
                    title = title.ToLower();

                    LocationData locationData = new LocationData();
                    locationData.Title = title;
                    locationData.Latitude = latitude;
                    locationData.Longitude = longitude;
                    locationData.Name = title;
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
    }
}