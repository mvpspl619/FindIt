using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;

namespace FindIt
{
    public partial class Settings : PhoneApplicationPage
    {
        public Settings()
        {
            InitializeComponent();
            
            Loaded += Settings_Loaded;
        }

        void Settings_Loaded(object sender, RoutedEventArgs e)
        {
            RadiusSlider.Maximum = 25;
            RadiusSlider.Minimum = 1;
            IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

            if (!appSettings.Contains("locationEnabled"))
            {
                appSettings["locationEnabled"] = true;
                AccessLocationSwitch.IsChecked = true;
            }
            else if (appSettings.Contains("locationEnabled"))
            {
                AccessLocationSwitch.IsChecked = Convert.ToBoolean(appSettings["locationEnabled"]);
            }

            if (!appSettings.Contains("radius"))
            {
                appSettings["radius"] = 1;
                ShowRadius.Text = string.Format("Radius for POI search - 200 m");
                RadiusSlider.Value = 1;
            }
            else if (appSettings.Contains("radius"))
            {
                RadiusSlider.Value = (double)appSettings["radius"];
                double realRadius = (RadiusSlider.Value) * 200;
                if (realRadius < 999)
                    ShowRadius.Text = string.Format("Radius for POI search - {0} m", Math.Round(realRadius, 0));
                else
                {
                    double kmRadius = realRadius / 1000;
                    ShowRadius.Text = string.Format("Radius for POI search - {0} km", Math.Round(kmRadius, 2));
                }
            }
            
            if (appSettings.Contains("searchCount"))
            {
                int value = Convert.ToInt32(appSettings["searchCount"]);
                switch (value)
                {
                    case 5:
                        ListerPickerItem.SelectedItem = five;
                        break;
                    case 10:
                        ListerPickerItem.SelectedItem = ten;
                        break;
                    case 15:
                        ListerPickerItem.SelectedItem = fifteen;
                        break;
                    case 20:
                        ListerPickerItem.SelectedItem = twenty;
                        break;
                    default:
                        ListerPickerItem.SelectedItem = ten;
                        break;
                }
            }
            else
            {
                appSettings["searchCount"] = 5;
            }
            appSettings.Save();
            ListerPickerItem.SelectionChanged += ListPicker_SelectionChanged;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

            if (!appSettings.Contains("radius"))
            {
                appSettings["radius"] = 1;
                ShowRadius.Text = string.Format("Radius for POI search - 200 m");
                RadiusSlider.Value = 1;
            }
            else if (appSettings.Contains("radius"))
            {
                RadiusSlider.Value = (double)appSettings["radius"];
                double realRadius = (RadiusSlider.Value) * 200;
                if (realRadius < 999)
                    ShowRadius.Text = string.Format("Radius for POI search - {0} m", Math.Round(realRadius, 0));
                else
                {
                    double kmRadius = realRadius / 1000;
                    ShowRadius.Text = string.Format("Radius for POI search - {0} km", Math.Round(kmRadius, 2));
                }
            } 

            base.OnNavigatedTo(e);
        }

        private void RadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;
            appSettings["radius"] = RadiusSlider.Value;
            double realRadius = (RadiusSlider.Value) * 200;
            if (realRadius < 999)
                ShowRadius.Text = string.Format("Radius for POI search - {0} m", Math.Round(realRadius, 0));
            else
            {
                double kmRadius = realRadius / 1000;
                ShowRadius.Text = string.Format("Radius for POI search - {0} km", Math.Round(kmRadius, 2));
            }
        }

        private void AccessLocationSwitch_Checked(object sender, RoutedEventArgs e)
        {
            IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;
            appSettings["locationEnabled"] = true;
            appSettings.Save();
        }

        private void AccessLocationSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;
            appSettings["locationEnabled"] = false;
            appSettings.Save();
        }

        private void ListPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListerPickerItem != null)
            {
                if (ListerPickerItem.SelectedItem != null)
                {
                    if (ListerPickerItem.SelectedIndex != -1)
                    {
                        var selectedIndex = ListerPickerItem.SelectedIndex;
                        IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;
                        if (appSettings.Contains("searchCount"))
                        {
                            switch (selectedIndex)
                            {
                                case 0:
                                    appSettings["searchCount"] = 5;
                                    break;
                                case 1:
                                    appSettings["searchCount"] = 10;
                                    break;
                                case 2:
                                    appSettings["searchCount"] = 15;
                                    break;
                                case 3:
                                    appSettings["searchCount"] = 20;
                                    break;
                                default:
                                    appSettings["searchCount"] = 5;
                                    break;
                            }
                        }
                        else
                        {
                            appSettings["searchCount"] = 5; ;
                        }
                        appSettings.Save();
                    }
                }
            }
        }



    }
}