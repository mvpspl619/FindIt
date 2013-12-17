using AppsLah;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindIt.ViewModels
{
    public class DisplayItem
    {
        public DisplayItem() { }
       
        public string name { get; set; }
        private GeoCoordinate location;
        public GeoCoordinate Location
        {
            get
            {
                return location;
            }
            set
            {
                location = value;
            }
        }

        public double Distance
        {
            get
            {
                return Math.Round(Location.GetDistanceTo(Calculations.CurrentLocation));
            }
        }

        public double Bearing
        {
            get 
            {
                return Calculations.CalculateBearing(Location, Calculations.CurrentLocation);
            }
        }

        

    }

    public class Calculations
    {
        public static GeoCoordinate CurrentLocation { get; set; }
        //public void CalculatePosition(GeoCoordinate origin, int radius)
        //{
        //    CurrentLocation = origin;
        //}

        public static void CalculatePosition(GeoCoordinate geoCoordinate)
        {
            CurrentLocation = geoCoordinate;
        }

        public static double CalculateBearing(GeoCoordinate destination, GeoCoordinate current)
        {
            double Line1X1 = current.Longitude;
            double Line1Y1 = current.Latitude;

            double Line1X2 = current.Longitude;
            double Line1Y2 = current.Latitude + 50;

            double Line2X1 = current.Longitude;
            double Line2Y1 = current.Latitude;

            double Line2X2 = destination.Longitude;
            double Line2Y2 = destination.Latitude;

            double angle1 = Math.Atan2(Line1Y1 - Line1Y2, Line1X1 - Line1X2);
            double angle2 = Math.Atan2(Line2Y1 - Line2Y2, Line2X1 - Line2X2);

            return ARHelper.RadianToDegree(angle1 - angle2);
        }

    }
}
