using NAudio.CoreAudioApi;
using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Management;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace Desko
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SerialPort _port;

        public Color _currentColor;
        public Color _staticColor;

        ComboboxItem<int>? _profile;
        ComboboxItem<int>? _previousProfile;

        private AudioMeterInformation _volume;

        private CSGOProfile _CSGOProfile;

        private ManagementEventWatcher _startWatcher;
        private ManagementEventWatcher _stopWatcher;


        Random rnd = new Random();

        public MainWindow()
        {
            InitializeComponent();

            LoadProfiles();

            GetSerialDevices();
            GetVolumeChannels();

            //Profile.SelectedIndex = 0; // Set to Rainbow profile.
        }

        private void LoadProfiles()
        {
            Profile.Items.Add(new ComboboxItem<int>("Static", 0));
            Profile.Items.Add(new ComboboxItem<int>("Rainbow", 1));
            Profile.Items.Add(new ComboboxItem<int>("Volume Sync", 2));
            Profile.Items.Add(new ComboboxItem<int>("CS:GO Integration", 3));
            Profile.Items.Add(new ComboboxItem<int>("GTA V Integration", 4));
        }

        private async Task UpdateProfile(ComboboxItem<int> profile)
        {
            if (_profile is not null)
                if (_profile!.Value == profile.Value)
                    return;

            Profile.SelectedIndex = profile.Value;
            if (_profile is not null)
                _previousProfile = _profile;

            _profile = profile;

            switch (profile.Text)
            {
                case "Static":
                    await StaticColorEffect();
                    break;
                case "Rainbow":
                    await RainbowEffect();
                    break;
                case "Volume Sync":
                    await VolumeSyncEffect();
                    break;
                case "CS:GO Integration":
                    await CSGOSyncEffect();
                    break;
                case "GTA V Integration":
                    await GtaVSyncEffect();
                    break;
            }
        }

        private async Task CSGOSyncEffect()
        {
            _CSGOProfile = new CSGOProfile(this);
            _CSGOProfile.StartCSGOGameStateSyncProfile();
            Debug.WriteLine("CS:GO PROFILE IS ACTIVE!!!");
        }

        private async Task GtaVSyncEffect()
        {
            while (_profile.Text == "GTA V Integration")
            {
                Debug.WriteLine("GTA V PROFILE IS ACTIVE!!!");
                await Task.Delay(500);
            }
        }

        //this method will get all the serial devices on the computer (your computer, probably...)
        private void GetSerialDevices()
        {
            foreach (string deviceport in SerialPort.GetPortNames())
            {
                Ports.Items.Add(deviceport);
            }
        }

        private void GetVolumeChannels()
        {
            //VolumeChannel.Visibility = Visibility.Hidden;
            Dispatcher.Invoke(VolumeChannel.Items.Clear);

            var deviceEnumerator = new MMDeviceEnumerator();
            MMDevice device = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            VolumeChannel.Items.Add(
                new ComboboxItem<AudioMeterInformation>("~ General ~", device.AudioMeterInformation));
            for (var i = 0; i < device.AudioSessionManager.Sessions.Count; i++)
            {
                AudioSessionControl session = device.AudioSessionManager.Sessions[i];
                Process process = Process.GetProcessById((int)session.GetProcessID);
                VolumeChannel.Items.Add(
                    new ComboboxItem<AudioMeterInformation>(process.ProcessName, session.AudioMeterInformation));
            }
        }

        private void SetVolumeChannel(AudioMeterInformation channel)
            => _volume = channel;

        private async void Ports_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _port = new SerialPort(Ports.SelectedItem.ToString()!, 9600); //Set your board COM
            _port.Open();

            //await Fade(Color.FromRgb(255, 255, 255), Color.FromRgb(0, 255, 0), TimeSpan.FromSeconds(5));
            //await Fade(Color.FromRgb(255, 0, 0), Color.FromRgb(0, 0, 255), TimeSpan.FromSeconds(5));

            UpdateProfile(new ComboboxItem<int>("Static", 0));
        }

        private async void Profiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (_profile is not null)
            //    if (_previousProfile is not null)
            //        if (_profile.Value == _previousProfile.Value)
            //            return;

            Debug.WriteLine("ohno");

            await UpdateProfile((ComboboxItem<int>)Profile.SelectedItem);
        }

        private void ColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            try
            {
                if (e.NewValue.HasValue)
                    _staticColor = e.NewValue!.Value;
            }
            catch
            {
            }
        }


        public void SetColor(byte red, byte green, byte blue, double brightness = 255)
        {
            _currentColor = Color.FromRgb(red, green, blue);

            Dispatcher.Invoke(() =>
            {
                //RedSlider.Value = _currentColor.R;
                //GreenSlider.Value = _currentColor.G;
                //BlueSlider.Value = _currentColor.B;
                //CurrentColor.Fill = new SolidColorBrush(_currentColor); // Rectangle

                RedBar.Value = _currentColor.R;
                RedValueLabel.Content = RedBar.Value;

                GreenBar.Value = _currentColor.G;
                GreenValueLabel.Content = GreenBar.Value;

                BlueBar.Value = _currentColor.B;
                BlueValueLabel.Content = BlueBar.Value;

                CurrentColor.Foreground = new SolidColorBrush(_currentColor);
                CurrentColor.Value = brightness;
            });

            _port.Write(new[] { _currentColor.R, _currentColor.G, _currentColor.B }, 0, 3);
        }

        private void SetColor(object red, object green, object blue, double brightness = 255)
            => SetColor(Convert.ToByte(red), Convert.ToByte(green), Convert.ToByte(blue), brightness);

        public void SetColor(Color color, double brightness = 255)
            => SetColor(color.R, color.G, color.B, brightness);

        public async Task Fade(Color startColor, Color endColor, double duration = 2000)
        {
            int redDiff = endColor.R - startColor.R;
            int greenDiff = endColor.G - startColor.G;
            int blueDiff = endColor.B - startColor.B;

            int delay = 20;
            int steps = (int)duration / delay;

            int redValue, greenValue, blueValue;

            for (int i = 0; i < steps - 1; ++i)
            {
                redValue = startColor.R + (redDiff * i / steps);
                greenValue = startColor.G + (greenDiff * i / steps);
                blueValue = startColor.B + (blueDiff * i / steps);

                SetColor(redValue, greenValue, blueValue);
                await Task.Delay(delay);
            }

            SetColor(endColor);
        }

        public async Task Fade(Color startColor, Color endColor, TimeSpan duration)
            => await Fade(startColor, endColor, duration.TotalMilliseconds);


        private async Task StaticColorEffect()
        {
            while (_profile.Text == "Static")
            {
                SetColor(_staticColor);
                await Task.Delay(1);
            }
        }

        private async Task RainbowEffect()
        {
            while (_profile.Text == "Rainbow")
            {
                Debug.WriteLine("Rainbow!!!");

                // Write color to Arduino
                byte[] rgbColour = new byte[3];
                // Start off with red.
                rgbColour[0] = 255;
                rgbColour[1] = 0;
                rgbColour[2] = 0;
                // Choose the colours to increment and decrement.
                for (int decColour = 0; decColour < 3; decColour += 1)
                {
                    int incColour = decColour == 2 ? 0 : decColour + 1;
                    // cross-fade the two colours.
                    for (int i = 0; i < 255; i += 1)
                    {
                        rgbColour[decColour] -= 1;
                        rgbColour[incColour] += 1;
                        SetColor(rgbColour[0], rgbColour[1], rgbColour[2]);
                        Console.WriteLine($"R = {rgbColour[0]}, G = {rgbColour[1]}, B = {rgbColour[2]}");
                        await Task.Delay(5);
                    }
                }
                //Console.WriteLine($"{color.Name}: R = {color.R}, G = {color.G}, B = {color.B}");
                //port.Write(new[] { color.R, color.G, color.B }, 0, 3);
            }
        }

        private async Task VolumeSyncEffect()
        {
            VolumeChannel.Visibility = Visibility.Visible;
            RefreshChannels.Visibility = Visibility.Visible;

            Dispatcher.Invoke(() => VolumeChannel.SelectedIndex = 0);

            while (_profile.Text == "Volume Sync")
            {
                double volume = Math.Round((_volume.MasterPeakValue * 1000) * 1.25) * IntensitySlider.Value;
                double outVolume = (double)(volume > 255 ? rnd.Next(180, 255) : volume);

                float intensity = (float)outVolume / 255;

                //Debug.WriteLine($"volume: {volume} | outVolume: {outVolume}");
                SetColor(Color.Multiply(_staticColor, intensity), outVolume);

                await Task.Delay(1);
            }

            VolumeChannel.Visibility = Visibility.Hidden;
            RefreshChannels.Visibility = Visibility.Hidden;
        }


        private void GameSync_Click(object sender, RoutedEventArgs e)
        {
            if (GameSync.IsChecked!.Value)
            {
                WqlEventQuery startQuery =
                    new WqlEventQuery("__InstanceCreationEvent",
                        new TimeSpan(0, 0, 1),
                        "TargetInstance isa \"Win32_Process\"");

                _startWatcher = new ManagementEventWatcher(startQuery);
                _startWatcher.EventArrived += new EventArrivedEventHandler(Watcher_WindowOpenedEvent);


                WqlEventQuery stopQuery = new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace");

                _stopWatcher = new ManagementEventWatcher(stopQuery);
                _stopWatcher.EventArrived += new EventArrivedEventHandler(Watcher_WindowClosedEvent);


                _startWatcher.Start();
                _stopWatcher.Start();
            }
            else
            {
                _startWatcher?.Stop();
                _stopWatcher?.Stop();
            }
        }

        private void Watcher_WindowOpenedEvent(object sender, EventArrivedEventArgs e)
        {
            try
            {
                string pname = ((ManagementBaseObject)e.NewEvent["TargetInstance"])["Name"].ToString()!;
                switch (pname)
                {
                    case "csgo.exe":
                        {
                            Debug.WriteLine("CS:GO *OPENING* DETECTED!!!!!!");
                            if (_profile.Value != 3)
                                Dispatcher.Invoke(
                                    () => Profile.SelectedIndex =
                                        3 /*UpdateProfile(new ComboboxItem<int>("CS:GO Integration", 3))*/);
                        }
                        break;

                    default:
                        Debug.WriteLine($"Detected a new process OPENING: {pname}");
                        break;
                }
            }
            catch
            {
            }
        }

        private void Watcher_WindowClosedEvent(object sender, EventArrivedEventArgs e)
        {
            string pname = (string)e.NewEvent["ProcessName"];

            switch (pname)
            {
                case "csgo.exe":
                    {
                        Debug.WriteLine("CS:GO *CLOSING* DETECTED!!!!!!");
                        _CSGOProfile.StopCSGOGameStateSyncProfile();

                        Dispatcher.Invoke(() => UpdateProfile(_previousProfile));
                    }
                    break;

                default:
                    Debug.WriteLine($"Detected a new process CLOSING: {pname}");
                    break;
            }
        }


        private void VolumeChannel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (VolumeChannel.SelectedItem is not null)
                    SetVolumeChannel(((ComboboxItem<AudioMeterInformation>)VolumeChannel.SelectedItem).Value);
                else
                    return;
            }
            catch (NullReferenceException)
            {
            }
        }


        private void RefreshChannels_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(GetVolumeChannels);
            VolumeChannel.SelectedIndex = 0;
        }

        //private void RedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        //{
        //    _currentColor.R = (byte)e.NewValue;
        //    RedValueLabel.Content = (int)RedSlider.Value;
        //}

        //private void GreenSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        //{
        //    try
        //    {
        //        _currentColor.G = (byte)e.NewValue;
        //        GreenValueLabel.Content = (int)GreenSlider.Value;
        //    }
        //    catch { }
        //}

        //private void BlueSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        //{
        //    try
        //    {
        //        _currentColor.B = (byte)e.NewValue;
        //        BlueValueLabel.Content = (int)BlueSlider!.Value!;

        //    }
        //    catch { }
        //}


    }
}