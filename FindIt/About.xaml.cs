using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Windows.System;

namespace FindIt
{
    public partial class About : PhoneApplicationPage
    {
        public About()
        {
            InitializeComponent();
        }
        
        private void ContactButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            EmailComposeTask emailComposeTask = new EmailComposeTask();

            emailComposeTask.Subject = "Contact Developer - FIND IT";
            emailComposeTask.To = "mvpspl619@gmail.com";
            emailComposeTask.Cc = "venkatapathiraju.mandapati@gmail.com";

            emailComposeTask.Show();
        }

        private void ReportButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            EmailComposeTask emailComposeTask = new EmailComposeTask();

            emailComposeTask.Subject = "Report Bug/Suggestions - FIND IT";
            emailComposeTask.To = "mvpspl619@gmail.com";
            emailComposeTask.Cc = "venkatapathiraju.mandapati@gmail.com";

            emailComposeTask.Show();
        }

        private async void Facebook_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("http://www.facebook.com/venkatapathiraju.mandapati"));
        }

        private async void Twitter_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("http://www.twitter.com/mvpspl619"));
        }

        private async void SO_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("http://stackoverflow.com/users/2710175/venkatapathi-raju-m"));
        }
    }
}