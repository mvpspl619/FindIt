using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Devices;
using Microsoft.Devices.Sensors;
using Microsoft.Xna.Framework;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.Windows.Media;
using FindIt.ViewModels;
using System.Device.Location;
using Matrix = Microsoft.Xna.Framework.Matrix;
using Color = System.Windows.Media.Color;
using AppsLah;
using System.IO.IsolatedStorage;
using System.IO;
using System.Windows.Media.Imaging;
using Windows.Devices.Geolocation;
using Newtonsoft.Json;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Toolkit;
using System.Threading;

namespace FindIt
{
    public partial class ViewDirection : PhoneApplicationPage
    {
        bool _isNewPageInstance = false;
        PhotoCamera camera;
        CancellationTokenSource cts = new CancellationTokenSource();
        System.Device.Location.GeoCoordinate currentLocation = new GeoCoordinate();
        string receivedStringData;
        //LabelGroup lg = new LabelGroup();
        LocationGroup receivedLocationGroupData = new LocationGroup();
        LocationGroupOC receivedLocationGroupOCData = new LocationGroupOC();
        double angle;
        Compass compass;
        //GeoCoordinateWatcher watcher;
        DisplayItem customItem;
        DispatcherTimer timer;
        Motion motion = new Motion();
        ViewPort viewport;
        Matrix projection;
        Matrix view;
        List<Vector3> points;
        List<UIElement> POIs;
        int WCSRadius = 10; //units
        bool everythingDone = false;
        double magneticHeading;
        double trueHeading;
        double headingAccuracy;
        Vector3 rawMagnetometerReading;
        bool isDataValid;
        bool calibrating = false;
        
        public ViewDirection()
        {
            InitializeComponent();
            
            ApplicationBar = new ApplicationBar();
            ApplicationBar.Mode = ApplicationBarMode.Default;
            ApplicationBar.Opacity = 0.8;
            ApplicationBar.IsVisible = true;
            ApplicationBar.IsMenuEnabled = false;

            ApplicationBarIconButton map = new ApplicationBarIconButton(new Uri("/Assets/Map.png", UriKind.RelativeOrAbsolute));
            ApplicationBarIconButton ar = new ApplicationBarIconButton(new Uri("/Assets/AR.png", UriKind.RelativeOrAbsolute));
            ar.IsEnabled = false;
            map.IsEnabled = false;
            ar.Text = "AR";
            map.Text = "Map";
            map.Click += map_Click;
            ar.Click += ar_Click;

            ApplicationBar.Buttons.Add(ar);
            ApplicationBar.Buttons.Add(map);
            ApplicationBar.ForegroundColor = Colors.White;

            _isNewPageInstance = true;

            points = new List<Vector3>();
            POIs = new List<UIElement>();
            Application.Current.Host.Settings.EnableFrameRateCounter = false;

            if (!Compass.IsSupported)
            {
                MessageBox.Show("Compass is not supported on your device. You will only be able to see services in map mode.");
            }
            else
            {
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(20);
                timer.Tick += timer_Tick;

                activateCompass();
            }
        }

        private void ar_Click(object sender, EventArgs e)
        {
            ApplicationBarIconButton arBtn = (ApplicationBarIconButton)ApplicationBar.Buttons[0];
            ApplicationBarIconButton mapBtn = (ApplicationBarIconButton)ApplicationBar.Buttons[1];

            arBtn.IsEnabled = false;
            mapBtn.IsEnabled = true;
            try
            {
                motion.CurrentValueChanged += motion_CurrentValueChanged;
                motion.Start();
                ContentPanel.Visibility = Visibility.Visible;
                MapPanel.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void map_Click(object sender, EventArgs e)
        {
            ApplicationBarIconButton arBtn = (ApplicationBarIconButton)ApplicationBar.Buttons[0];
            ApplicationBarIconButton mapBtn = (ApplicationBarIconButton)ApplicationBar.Buttons[1];

            try
            {
                motion.CurrentValueChanged += motion_CurrentValueNotChanged;
                motion.Stop();
                mapBtn.IsEnabled = false;
                arBtn.IsEnabled = true;
                ContentPanel.Visibility = Visibility.Collapsed;
                MapPanel.Visibility = Visibility.Visible;
                MapPanel.SetValue(Canvas.ZIndexProperty, 2);

                var mapCenter = new GeoCoordinate(currentLocation.Latitude, currentLocation.Longitude);
                MapLayer myLayer = new MapLayer();
                Pushpin pin = new Pushpin();
                pin.Content = "ME";

                //pin.GeoCoordinate = mapCenter;
                pin.GeoCoordinate = mapCenter;

                foreach (LocationData ld in receivedLocationGroupData.Items)
                {
                    MapOverlay newOverlay = new MapOverlay();
                    Pushpin newPin = new Pushpin();
                    newPin.Content = ld.Name;
                    newPin.GeoCoordinate = new GeoCoordinate { Latitude = ld.Latitude, Longitude = ld.Longitude };
                    newOverlay.Content = newPin;
                    newOverlay.GeoCoordinate = new GeoCoordinate { Latitude = ld.Latitude, Longitude = ld.Longitude };
                    newOverlay.PositionOrigin = new System.Windows.Point(0, 1);
                    myLayer.Add(newOverlay);
                }

                MapOverlay myOverlay = new MapOverlay();
                myOverlay.Content = pin;
                myOverlay.GeoCoordinate = mapCenter;
                myOverlay.PositionOrigin = new System.Windows.Point(0, 1);
                myLayer.Add(myOverlay);

                MapPanelMap.Layers.Add(myLayer);
                MapPanelMap.Center = mapCenter;
                double level = getTheZoomLevel();
                MapPanelMap.SetView(mapCenter, level, MapAnimationKind.Linear);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void InitViewport()
        {
            viewport = new ViewPort(0, 0, Convert.ToInt32(ActualWidth), Convert.ToInt32(ActualHeight));
            float aspect = viewport.AspectRatio;
            projection = Matrix.CreatePerspectiveFieldOfView(1, aspect, 1, 12);
            view = Matrix.CreateLookAt(Vector3.UnitZ, Vector3.Zero, Vector3.UnitX);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // If _isNewPageInstance is true, the page constructor has been called, so
            // state may need to be restored.
            if (Compass.IsSupported)
            {
                if (_isNewPageInstance)
                {
                    receivedStringData = NavigationContext.QueryString["serializedData"];
                    string isOC = NavigationContext.QueryString["isOC"];
                    if (isOC == "true")
                    {
                        receivedLocationGroupOCData = JsonConvert.DeserializeObject<LocationGroupOC>(receivedStringData);
                        receivedLocationGroupData.Title = receivedLocationGroupOCData.Title;
                        receivedLocationGroupData.Type = receivedLocationGroupOCData.Type;
                        foreach (LocationData ld in receivedLocationGroupOCData.Items)
                        {
                            receivedLocationGroupData.Items.Add(ld);
                        }
                    }
                    else
                    {
                        receivedLocationGroupData = JsonConvert.DeserializeObject<LocationGroup>(receivedStringData);
                    }
                    InitializeCamera();
                    // If the application member variable is not empty,
                    // set the page's data object from the application member variable.
                }
                else
                {
                    camera = new PhotoCamera();
                    viewfinderBrush.SetSource(camera);
                    
                }
                _isNewPageInstance = false;
            }
            else 
            {
                receivedStringData = NavigationContext.QueryString["serializedData"];
                string isOC = NavigationContext.QueryString["isOC"];
                if (isOC == "true")
                {
                    receivedLocationGroupOCData = JsonConvert.DeserializeObject<LocationGroupOC>(receivedStringData);
                    receivedLocationGroupData.Title = receivedLocationGroupOCData.Title;
                    receivedLocationGroupData.Type = receivedLocationGroupOCData.Type;
                    foreach (LocationData ld in receivedLocationGroupOCData.Items)
                    {
                        receivedLocationGroupData.Items.Add(ld);
                    }
                }
                else
                {
                    receivedLocationGroupData = JsonConvert.DeserializeObject<LocationGroup>(receivedStringData);
                }
                ApplicationBar.IsVisible = false;
                ContentPanel.Visibility = Visibility.Collapsed;
                MapPanel.Visibility = Visibility.Visible;
                MapPanel.SetValue(Canvas.ZIndexProperty, 2);

                GetOnlyLocation(cts.Token);
            }
            
            //base.OnNavigatedTo(e);
        }

        protected override void OnOrientationChanged(OrientationChangedEventArgs e)
        {
            if (camera != null)
            {
                Dispatcher.BeginInvoke(() =>
                    {
                        double rotation = camera.Orientation;
                        switch (this.Orientation)
                        { 
                            case PageOrientation.LandscapeLeft:
                                rotation = camera.Orientation - 90;
                                break;
                            case PageOrientation.LandscapeRight:
                                rotation = camera.Orientation + 270;
                                break;
                        }
                        viewfinderTransform.Rotation = rotation;
                    });
            }
        }

        private void showOnlyMap()
        {
            
            if (everythingDone == true)
            {
                try
                {
                    
                    var mapCenter = new GeoCoordinate(currentLocation.Latitude, currentLocation.Longitude);
                    MapLayer myLayer = new MapLayer();
                    Pushpin pin = new Pushpin();
                    pin.Content = "ME";

                    //pin.GeoCoordinate = mapCenter;
                    pin.GeoCoordinate = mapCenter;

                    foreach (LocationData ld in receivedLocationGroupData.Items)
                    {
                        MapOverlay newOverlay = new MapOverlay();
                        Pushpin newPin = new Pushpin();
                        newPin.Content = ld.Name;
                        newPin.GeoCoordinate = new GeoCoordinate { Latitude = ld.Latitude, Longitude = ld.Longitude };
                        newOverlay.Content = newPin;
                        newOverlay.GeoCoordinate = new GeoCoordinate { Latitude = ld.Latitude, Longitude = ld.Longitude };
                        newOverlay.PositionOrigin = new System.Windows.Point(0, 1);
                        myLayer.Add(newOverlay);
                    }

                    MapOverlay myOverlay = new MapOverlay();
                    myOverlay.Content = pin;
                    myOverlay.GeoCoordinate = mapCenter;
                    myOverlay.PositionOrigin = new System.Windows.Point(0, 1);
                    myLayer.Add(myOverlay);

                    MapPanelMap.Layers.Add(myLayer);
                    MapPanelMap.Center = mapCenter;
                    double level = getTheZoomLevel();
                    MapPanelMap.SetView(mapCenter,level, MapAnimationKind.Linear);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (cts != null)
            {
                cts.Cancel();
            }
            camera = null;
            base.OnNavigatedFrom(e);
        }

        private void InitializeCamera()
        {            
            camera = new PhotoCamera();
            viewfinderBrush.SetSource(camera);
            SystemTray.ProgressIndicator = new ProgressIndicator();
            
            if (Motion.IsSupported)
            {
                //motion = new Motion();
                motion.TimeBetweenUpdates = TimeSpan.FromMilliseconds(20);
                motion.CurrentValueChanged += new EventHandler<SensorReadingEventArgs<MotionReading>>(motion_CurrentValueChanged);
                motion.Start();
                addSignPosts();
                GetLocation(cts.Token);
                cts = null;
            }
            else
            {
                MessageBox.Show("Your device unfortunately does not support the Motion api");
            }
        }

        private async void GetOnlyLocation(CancellationToken ct)
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
                    Geolocator geoLocator = new Geolocator();

                    bool GPSAvailable = checkGPS();
                    if (GPSAvailable)
                    {
                        geoLocator.DesiredAccuracy = PositionAccuracy.High;
                        //geoLocator.MovementThreshold = 5;
                        //geo.ReportInterval = 200;
                        //geo.PositionChanged += geo_PositionChanged;
                        SystemTray.ProgressIndicator = new ProgressIndicator();
                        SetProgressIndicator(true);
                        SystemTray.ProgressIndicator.Text = "Searching for current location";

                        Geoposition position = await geoLocator.GetGeopositionAsync(
                            TimeSpan.FromMinutes(1),
                            TimeSpan.FromSeconds(60)).AsTask(ct);

                        SystemTray.ProgressIndicator.Text = "Acquired";
                        SetProgressIndicator(false);

                        currentLocation.Latitude = position.Coordinate.Latitude;
                        currentLocation.Longitude = position.Coordinate.Longitude;

                        Calculations.CalculatePosition(currentLocation);
                        everythingDone = true;
                        showOnlyMap();
                    }
                }
                catch
                {
                    MessageBox.Show("An error occured while getting GPS data. Please try agian.");
                }
            }
        }

        private async void GetLocation(CancellationToken ct)
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
                    Geolocator geoLocator = new Geolocator();

                    bool GPSAvailable = checkGPS();
                    if (GPSAvailable)
                    {
                        geoLocator.DesiredAccuracy = PositionAccuracy.High;
                        //geoLocator.MovementThreshold = 5;
                        //geo.ReportInterval = 200;
                        //geo.PositionChanged += geo_PositionChanged;
                        SystemTray.ProgressIndicator = new ProgressIndicator();
                        SetProgressIndicator(true);
                        SystemTray.ProgressIndicator.Text = "Searching for current location";

                        Geoposition position = await geoLocator.GetGeopositionAsync(
                            TimeSpan.FromMinutes(1),
                            TimeSpan.FromSeconds(60)).AsTask(ct);

                        SystemTray.ProgressIndicator.Text = "Acquired";
                        SetProgressIndicator(false);

                        currentLocation.Latitude = position.Coordinate.Latitude;
                        currentLocation.Longitude = position.Coordinate.Longitude;

                        Calculations.CalculatePosition(currentLocation);
                        everythingDone = true;
                        OrderData(currentLocation);
                        
                    }
                }
                catch
                {
                    MessageBox.Show("An error occured while getting GPS data. Please try agian.");
                }
            }
        }

        private void OrderData(GeoCoordinate currentLocation)
        {
            if (receivedLocationGroupData != null)
            {
                if (receivedLocationGroupData.Items.Count != 0)
                {
                    foreach (LocationData ld in receivedLocationGroupData.Items)
                    {
                        GeoCoordinate scrap = new GeoCoordinate();
                        scrap.Latitude = ld.Latitude;
                        scrap.Longitude = ld.Longitude;
                        ld.DistanceToUser = scrap.GetDistanceTo(currentLocation);
                    }
                    List<LocationData> sortedList = receivedLocationGroupData.Items.OrderBy(x => x.DistanceToUser).ToList();
                    receivedLocationGroupData.Items = sortedList;
                    ShowCustomLabels();
                }
            }
        }

        private void ShowCustomLabels()
        {
            if (everythingDone)
            {
                if (receivedLocationGroupData != null)
                {
                    //do some coding to show labels based on different types of passed data: saved, famous and pois
                    //even take care of showing images
                    double count;
                    IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;
                    if (appSettings.Contains("searchCount"))
                    {
                        count = Convert.ToDouble(appSettings["searchCount"]);
                    }
                    else
                        count = 10;
                    //group = new ViewGroup();
                    string category = receivedLocationGroupData.Type;
                    switch (category)
                    { 
                        case "saved":
                            foreach (LocationData ld in receivedLocationGroupData.Items)
                                {
                                    //lg.Title = "savedData";
                                    customItem = new DisplayItem();
                                    customItem.name = ld.Name;
                                    customItem.Location = new GeoCoordinate(ld.Latitude, ld.Longitude, 0);
                                    System.Device.Location.GeoCoordinate itemLocation = new GeoCoordinate { Latitude = ld.Latitude, Longitude = ld.Longitude };
                                    string savedUrl = "/Assets/Icons/FindIt.jpg";
                                    try
                                    {
                                        using (IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication())
                                        {
                                            if (iso.FileExists(string.Format("{0}.jpeg", ld.Title)))
                                            {
                                                string fileName = string.Format("{0}.jpeg", ld.Title);
                                                savedUrl = iso.GetType().GetField("m_RootDir", System.Reflection.BindingFlags.NonPublic |
                                                    System.Reflection.BindingFlags.Instance).GetValue(iso).ToString() + fileName;
                                            }
                                        }
                                    }
                                    catch
                                    { }
                                    addSignPosts();
                                    if (!String.IsNullOrEmpty(savedUrl))
                                    {
                                        addLabel(ARHelper.AngleToVector(customItem.Bearing, WCSRadius), customItem.name, savedUrl, Math.Round(ld.DistanceToUser, 0));
                                        //group.points.Add(new ViewPoints { X = ARHelper.AngleToVector(customItem.Bearing, WCSRadius).X, Y = ARHelper.AngleToVector(customItem.Bearing, WCSRadius).Y, Z = ARHelper.AngleToVector(customItem.Bearing, WCSRadius).Z });
                                        //lg.Items.Add(new LabelItem { Position = ARHelper.AngleToVector(customItem.Bearing, WCSRadius), Name = customItem.name, imageUrl = savedUrl, Distance = distance });
                                    }
                                }
                            break;
                        case "famous":
                            foreach (LocationData ld in receivedLocationGroupData.Items)
                                {
                                    //lg.Title = "savedData";
                                    customItem = new DisplayItem();
                                    customItem.name = ld.Name;
                                    customItem.Location = new GeoCoordinate(ld.Latitude, ld.Longitude, 0);
                                    System.Device.Location.GeoCoordinate itemLocation = new GeoCoordinate { Latitude = ld.Latitude, Longitude = ld.Longitude };
                                    string famousUrl = string.Format("/Assets/Famous/{0}.jpg", ld.Name);
                                    addSignPosts();
                                    addLabel(ARHelper.AngleToVector(customItem.Bearing, WCSRadius), ld.Title, famousUrl, Math.Round(ld.DistanceToUser, 0));
                                    //group.points.Add(new ViewPoints { X = ARHelper.AngleToVector(customItem.Bearing, WCSRadius).X, Y = ARHelper.AngleToVector(customItem.Bearing, WCSRadius).Y, Z = ARHelper.AngleToVector(customItem.Bearing, WCSRadius).Z });
                                    //lg.Items.Add(new LabelItem { Position = ARHelper.AngleToVector(customItem.Bearing, WCSRadius), Name = ld.Title, imageUrl = famousUrl, Distance = distance });
                                }
                            break;
                        case "pois":
                            double loop = 0;
                            foreach (LocationData ld in receivedLocationGroupData.Items)
                                {
                                    if (loop <= count)
                                    {
                                        //lg.Title = "savedData";
                                        customItem = new DisplayItem();
                                        customItem.name = ld.Name;
                                        customItem.Location = new GeoCoordinate(ld.Latitude, ld.Longitude, 0);
                                        System.Device.Location.GeoCoordinate itemLocation = new GeoCoordinate { Latitude = ld.Latitude, Longitude = ld.Longitude };
                                        string poisUrl = GetFilePathForPoi(receivedLocationGroupData.Title);
                                        addSignPosts();
                                        addLabel(ARHelper.AngleToVector(customItem.Bearing, WCSRadius), customItem.name, poisUrl, Math.Round(ld.DistanceToUser, 0));
                                    }
                                    loop++;
                                }
                            break;
                        default:
                            break;
                    }
                }
                ApplicationBarIconButton mapBtn = (ApplicationBarIconButton)ApplicationBar.Buttons[1];
                mapBtn.IsEnabled = true;
                
            }
            else
                return;
        }

        private string GetFilePathForPoi(string p)
        {
            switch (p)
            { 
                case "atm":
                    return "/Assets/Icons/ATM.jpg";
                case "restaurant":
                    return "/Assets/Icons/Restaurant.jpg";
                case "hospital":
                    return "/Assets/Icons/Hospitals.jpg";
                case "police":
                    return "/Assets/Icons/Police.jpg";
                case "pharmacy":
                    return "/Assets/Icons/Pharmacy.jpg";
                case "gas station":
                    return "/Assets/Icons/Fuel.jpg";
                case "train station":
                    return "/Assets/Icons/Train.jpg";
                case "movie theater":
                    return "/Assets/Icons/Movie.jpg";
                default:
                    return "/Assets/FindIt.jpg";
            }
        }

        private double calculateDistance(GeoCoordinate itemLocation)
        {
            if (currentLocation != null)
            {
                return currentLocation.GetDistanceTo(itemLocation);
            }
            return 0;
        }

        private void motion_CurrentValueChanged(object sender, SensorReadingEventArgs<MotionReading> e)
        {
            Dispatcher.BeginInvoke(() => CurrentValueChanged(e.SensorReading));
        }

        private void motion_CurrentValueNotChanged(object sender, SensorReadingEventArgs<MotionReading> e)
        {
        }

        void CurrentValueChanged(MotionReading reading)
        {
            if (viewport.Width == 0)
                InitViewport();

            //rotate the readings from the device to xna coordinate frame
            //90deg around x axis
            Matrix attitude = Matrix.CreateRotationX(MathHelper.PiOver2) * reading.Attitude.RotationMatrix;
            for (int i = 0; i < points.Count; i++)
            {
                //for each of the WCS points, we need to convert it to XNA coordinates
                Matrix world = Matrix.CreateWorld(points[i], Vector3.UnitZ, Vector3.UnitX);
                //need to project it from 3D space in wcs to 2D space on the screen coordinates
                //need to rotate back for phone
                Vector3 projected = viewport.Project(Vector3.Zero, projection, view, world * attitude);
                //adjust the labels such that only those that are infront the camera are being shown
                if (projected.Z > 1 || projected.Z < 0)
                {
                    POIs[i].Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    POIs[i].Visibility = System.Windows.Visibility.Visible;
                    //move the UIElement according to the x, y coordinates of the projection vector
                    //we would want to center the lables
                    TranslateTransform tt = new TranslateTransform();
                    tt.X = projected.X - (POIs[i].RenderSize.Width / 2);
                    tt.Y = projected.Y - (POIs[i].RenderSize.Height/2);
                    POIs[i].RenderTransform = tt;
                }
            }


        }
        void addSignPosts()
        {
            //north is 0 deg.... in vector?
            addLabel(ARHelper.AngleToVector(0, WCSRadius), "N");
            addLabel(ARHelper.AngleToVector(90, WCSRadius), "E");
            addLabel(ARHelper.AngleToVector(180, WCSRadius), "S");
            addLabel(ARHelper.AngleToVector(270, WCSRadius), "W");
        }

        void addLabel(Vector3 position, string title)
        {
           var layout = new Grid();
           layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
           layout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
           var txtblk = new TextBlock { Text = title };
           txtblk.FontSize = 25;
           var border1 = new Grid { Background = new SolidColorBrush(Colors.Black) };
           txtblk.Foreground = new SolidColorBrush(Colors.White);

           border1.VerticalAlignment = VerticalAlignment.Top;
           border1.HorizontalAlignment = HorizontalAlignment.Left;
           border1.Children.Add(txtblk);
           Grid.SetRow(border1, 0);
           Grid.SetColumn(border1, 0);
           layout.Children.Add(border1);
           border1.SetValue(Canvas.ZIndexProperty, 1);
           LayoutRoot.Children.Add(layout);
           points.Add(position);
           POIs.Add(border1);
                
        }

        void addLabel(Vector3 position, string Name, string imageUrl, double distance)
        {
            
            var layout = new Grid();

                    layout.VerticalAlignment = VerticalAlignment.Top;
                    layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(75) });
                    layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });
                    layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });

                    Image img = new Image { Source = new BitmapImage { UriSource = new Uri(imageUrl, UriKind.RelativeOrAbsolute) }, Height = 65, Width = 65 };
                    img.Margin = new Thickness(0, 0, 0, 0);

                    var imgGrid = new Grid();
                    imgGrid.VerticalAlignment = VerticalAlignment.Top;
                    BitmapImage bmi = new BitmapImage(new Uri("/Assets/ViewDirection.png", UriKind.RelativeOrAbsolute));
                    ImageBrush brush = new ImageBrush();
                    imgGrid.Height = 75;
                    imgGrid.Width = 75;
                    brush.ImageSource = bmi;
                    imgGrid.Background = brush;
                    imgGrid.Children.Add(img);

                    var txtblk = new TextBlock { Text = Name };
                    txtblk.FontSize = 16;
                    txtblk.Foreground = new SolidColorBrush(Colors.White);
                    txtblk.HorizontalAlignment = HorizontalAlignment.Center;

                    var txtGrid = new Grid();
                    txtGrid.Background = new SolidColorBrush(Colors.Black);
                    txtGrid.Children.Add(txtblk);

                    var distblk = new TextBlock();
                    if (distance < 1000)
                    {
                        distblk.Text = String.Format("{0}m", distance);
                    }
                    else
                    {
                        distblk.Text = String.Format("{0}km", distance / 1000);
                    }
                    distblk.FontSize = 16;
                    distblk.Foreground = new SolidColorBrush(Colors.White);
                    distblk.HorizontalAlignment = HorizontalAlignment.Center;

                    var distGrid = new Grid();
                    distGrid.Background = new SolidColorBrush(Colors.Black);
                    distGrid.Children.Add(distblk);

                    Grid.SetRow(imgGrid, 0);
                    Grid.SetRow(txtGrid, 1);
                    Grid.SetRow(distGrid, 2);
                    layout.Children.Add(imgGrid);
                    layout.Children.Add(txtGrid);
                    layout.Children.Add(distGrid);
                    layout.HorizontalAlignment = HorizontalAlignment.Left;
                    layout.SetValue(Canvas.ZIndexProperty, 1);
                    LayoutRoot.Children.Add(layout);
                    POIs.Add(layout);
                    points.Add(position);
        }
        
        private void activateCompass()
        {
            if (compass != null && compass.IsDataValid)
            {
                compass.Stop();
                timer.Stop();
                MessageBox.Show("Compass stopped");
                //accelerometer.Stop();
            }
            else
            {
                if (compass == null)
                {
                    compass = new Compass();
                    compass.TimeBetweenUpdates = TimeSpan.FromMilliseconds(10);

                    compass.CurrentValueChanged += new EventHandler<SensorReadingEventArgs<CompassReading>>(compass_CurrentValueChanged);
                    compass.Calibrate += new EventHandler<CalibrationEventArgs>(compass_Calibrate);

                }
                try
                {
                    compass.Start();
                    timer.Start();
                    //accelerometer = new Accelerometer();
                    //accelerometer.CurrentValueChanged += new EventHandler<SensorReadingEventArgs<AccelerometerReading>>(accelerometer_CurrentValueChanged);
                    //accelerometer.Start();
                }
                catch(InvalidOperationException)
                {
                    MessageBox.Show("Cannot start compass");
                }
            }
        }

        void compass_CurrentValueChanged(object sender, SensorReadingEventArgs<CompassReading> e)
        {
            isDataValid = compass.IsDataValid;

            trueHeading = 360 - e.SensorReading.TrueHeading;
            magneticHeading = e.SensorReading.MagneticHeading;
            headingAccuracy = Math.Abs(e.SensorReading.HeadingAccuracy);
            rawMagnetometerReading = e.SensorReading.MagnetometerReading;
        }

        void timer_Tick(object sender, EventArgs e)
        {
            
            if (!calibrating)
            {
                //UPDATE THE VIEW
                if (0 < trueHeading && trueHeading < 90)
                {
                    angle = trueHeading + 270;
                }
                else if (90 < trueHeading && trueHeading < 180)
                {
                    angle = trueHeading - 90;
                }
                else if (180 < trueHeading && trueHeading < 270)
                {
                    angle = trueHeading - 90;
                }
                else if (270 < trueHeading && trueHeading < 360)
                {
                    angle = trueHeading - 90;
                }
                compositeTransform.Rotation = angle;
                
            }
            else
            {
                if (headingAccuracy <= 10)
                {
                    calibrationStackPanel.Visibility = Visibility.Collapsed;
                    calibrating = false;
                }
            }
            
        }

        private void compass_Calibrate(object sender, CalibrationEventArgs e)
        {
            Dispatcher.BeginInvoke(() => { calibrationStackPanel.Visibility = Visibility.Visible; });
            calibrating = true;
        }

        private void SetProgressIndicator(bool IsVisible)
        {
            SystemTray.ProgressIndicator.IsIndeterminate = IsVisible;
            SystemTray.ProgressIndicator.IsVisible = IsVisible;
        }

        private bool checkGPS()
        {
            Geolocator geoLocator = new Geolocator();

            if (geoLocator.LocationStatus == PositionStatus.Disabled)
            {
                MessageBoxResult result = MessageBox.Show("Your location services are turned off. To turn them on, tap on OK", "Location services are off", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    var launcher = Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-location:"));
                    return true;
                }
                else
                    return false;
            }
            return true;
        }

        private static ScreenResolution getScreenResolution()
        {
            ScreenResolution res = new ScreenResolution();
            double width = 0;
            double height = 0;
            if(App.Current.Host.Content.ScaleFactor == 100)
            {
                width = Application.Current.Host.Content.ActualWidth;  
                height = Application.Current.Host.Content.ActualHeight;
            }
            else if (App.Current.Host.Content.ScaleFactor == 160)
            {
                width = Application.Current.Host.Content.ActualWidth;  
                height = Application.Current.Host.Content.ActualHeight;
            }
            else if (App.Current.Host.Content.ScaleFactor == 150)
            {
                width = Application.Current.Host.Content.ActualWidth;  
                height = Application.Current.Host.Content.ActualHeight;
            }
            res.height = height;
            res.width = width;
            return res;
        }

        private double calculateFarthestDistance()
        {
            LocationGroup loc = receivedLocationGroupData;
            int count = receivedLocationGroupData.Items.Count;
            double dist = 0;
            if (count > 1)
            {
                for (int i = 0; i < count; i++)
                {
                    LocationData initial = loc.Items.ElementAt(i);
                    for (int j = 0; j < count; j++)
                    {
                        LocationData final = loc.Items.ElementAt(j);
                        GeoCoordinate initGeo = new GeoCoordinate { Latitude = initial.Latitude, Longitude = initial.Longitude };
                        GeoCoordinate finGeo = new GeoCoordinate { Latitude = final.Latitude, Longitude = final.Longitude };
                        double newDistance = initGeo.GetDistanceTo(finGeo);
                        if (newDistance > dist)
                            dist = newDistance;
                    }
                }
            }
            else
            {
                LocationData one = loc.Items.ElementAt(0);
                GeoCoordinate onlyOne = new GeoCoordinate { Latitude = one.Latitude, Longitude = one.Longitude };
                dist = currentLocation.GetDistanceTo(onlyOne);
            }
            return dist;
        }

        private double getTheZoomLevel()
        {
            ScreenResolution sr = getScreenResolution();
            double distance = calculateFarthestDistance();
            double pixel;
            double meter = distance;
            pixel = sr.width;
            double ratio = distance / pixel;

            double zoomLevel = 15;

            if (0 < ratio && ratio <= 0.3)
                zoomLevel = 19;
            else if (0.3 < ratio && ratio <= 0.6)
                zoomLevel = 18;
            else if (0.6 < ratio && ratio <= 1.19)
                zoomLevel = 17;
            else if (1.19 < ratio && ratio <= 2.39)
                zoomLevel = 16;
            else if (2.39 < ratio && ratio <= 4.78)
                zoomLevel = 15;
            else if (4.78 < ratio && ratio <= 9.55)
                zoomLevel = 14;
            else if (9.55 < ratio && ratio <= 19.11)
                zoomLevel = 13;
            else if (19.11 < ratio && ratio <= 38.22)
                zoomLevel = 12;
            else if (38.22 < ratio && ratio <= 76.44)
                zoomLevel = 11;
            else if (76.44 < ratio && ratio <= 152.87)
                zoomLevel = 10;
            else if (152.87 < ratio && ratio <= 305.75)
                zoomLevel = 9;
            else if (305.75 < ratio && ratio <= 611.50)
                zoomLevel = 8;
            else if (611.50 < ratio && ratio <= 1222.99)
                zoomLevel = 7;
            else if (1222.99 < ratio && ratio <= 2445.98)
                zoomLevel = 6;
            else if (2445.98 < ratio && ratio <= 4891.97)
                zoomLevel = 5;
            else if (4891.97 < ratio && ratio <= 9783.94)
                zoomLevel = 4;
            else if (9783.94 < ratio && ratio <= 19567.88)
                zoomLevel = 3;
            else if (19567.88 < ratio && ratio <= 39135.76)
                zoomLevel = 2;
            else if (39135.76 < ratio && ratio <= 78271.52)
                zoomLevel = 1;
            else if (78271.52 < ratio)
                zoomLevel = 1;

            return zoomLevel;
        }

        private void MapPanelMap_Loaded(object sender, RoutedEventArgs e)
        {
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.ApplicationId = "422b0fd4-e12b-44c3-a0f7-246907c7639b";
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.AuthenticationToken = "mO07flYO2QgCoqtiTjmAhw";
        }
    }
}