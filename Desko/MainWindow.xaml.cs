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
        SerialPort port;
        public Color _currentColor;

        string? _profile;
        string? _previousProfile;

        private ManagementEventWatcher watcher = null;

        Random rnd = new Random();

        public MainWindow()
        {
            InitializeComponent();
            GetSerialDevices();

            //Profile.SelectedIndex = 0; // Set to Rainbow profile.
        }

        private async void OnWindowOpenedOrClosed(object sender, AutomationEventArgs automationEventArgs)
        {
            try
            {
                var element = sender as AutomationElement;

                if (element != null)
                {
                    if (automationEventArgs.EventId == WindowPattern.WindowOpenedEvent)
                    {
                        switch (element.Current.Name)
                        {
                            case string e when element.Current.Name.StartsWith("Counter-Strike: Global Offensive - Steam"):
                                {
                                    Debug.WriteLine("CS:GO DETECTED!!!!!!");
                                    Dispatcher.Invoke(() => Profile.SelectedIndex = 3);


                                }
                                break;
                        }

                        Debug.WriteLine("New Window opened: {0} | {1}", element.Current.Name, element.Current.ProcessId);
                    }
                    else if (automationEventArgs.EventId == WindowPattern.WindowClosedEvent)
                    {
                        Debug.WriteLine($"{element.Current.Name} CLOSED!!!");
                    }

                    Automation.AddAutomationEventHandler(
                        eventId: WindowPattern.WindowClosedEvent,
                        element: element,
                        scope: TreeScope.Element,
                        eventHandler: OnWindowOpenedOrClosed);


                }
            }
            catch (ElementNotAvailableException)
            {
            }
        }

        private async Task UpdateProfile(string profile)
        {
            _previousProfile = _profile;
            _profile = profile;

            switch (profile)
            {
                case "Static":
                    await StaticColorEffect();
                    break;
                case "Rainbow":
                    await RainbowEffect();
                    break;
                case "Music Sync":
                    await MusicSyncEffect();
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
            while (_profile == "CS:GO Integration")
            {
                Debug.WriteLine("CS:GO PROFILE IS ACTIVE!!!");
                await Task.Delay(500);
            }
        }

        private async Task GtaVSyncEffect()
        {
            while (_profile == "GTA V Integration")
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

        private void Ports_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            port = new SerialPort(Ports.SelectedItem.ToString()!, 9600); //Set your board COM
            port.Open();
        }

        private async void Profiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string profile = ((ComboBoxItem)Profile.SelectedItem).Content.ToString()!;
            await UpdateProfile(profile);
        }

        private void ColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            _currentColor = e.NewValue!.Value;
        }

        private void SetColor(byte red, byte green, byte blue)
        {
            port.Write(new[] { red, green, blue }, 0, 3);
            _currentColor = Color.FromRgb(red, green, blue);
            CurrentColor.Fill = new SolidColorBrush(_currentColor);
        }

        private void SetColor(Color color)
            => SetColor(color.R, color.G, color.B);

        private async Task StaticColorEffect()
        {
            while (_profile == "Static")
            {
                SetColor(_currentColor);
                await Task.Delay(1);
            }
        }

        private async Task RainbowEffect()
        {
            while (_profile == "Rainbow")
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

        private async Task MusicSyncEffect()
        {
            MMDeviceEnumerator devEnum = new MMDeviceEnumerator();
            MMDevice defaultDevice = devEnum.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            while (_profile == "Music Sync")
            {
                double volume = Math.Round((defaultDevice.AudioMeterInformation.MasterPeakValue * 1000) * 1.25);
                byte outVolume = (byte)(volume > 255 ? rnd.Next(180, 255) : volume);

                //Debug.WriteLine($"volume: {volume} | outVolume: {outVolume}");

                SetColor(outVolume, outVolume, (byte)0);

                await Task.Delay(1);
            }
        }


        private void GameSync_Click(object sender, RoutedEventArgs e)
        {
            if (GameSync.IsChecked!.Value)
            {
                WqlEventQuery query = new WqlEventQuery("Select * From __InstanceCreationEvent Within 2 Where TargetInstance Isa 'Win32_Process'");

                watcher = new ManagementEventWatcher(query);
                //watcher.EventArrived += new EventArrivedEventHandler(watcher_EventArrived);

                Console.WriteLine("Waiting ...");

                watcher.Start();

            }
            else
            {
                Automation.RemoveAllEventHandlers();
            }
        }
    }
}
