using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Collections.ObjectModel;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using System.Windows.Media.Imaging;


using System.IO;
using System.IO.IsolatedStorage;

namespace lockWidgets
{
    public partial class MainPage : PhoneApplicationPage
    {
        Imangodll instance;

        string latestSMSBody = "";
        string latestSMSFrom = "";

        string curWOEID = "";

        appdata appSettings;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var stream = new IsolatedStorageFileStream("widgets.xml", FileMode.OpenOrCreate, store))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        try
                        {
                            var serial = new XmlSerializer(typeof(appdata));
                            appSettings = (appdata)serial.Deserialize(stream);
                            
                        }
                        catch
                        {
                            appSettings = new appdata();
                            appSettings.city = "";
                        }
                    }
                }

            }
            if (appSettings.useBing)
            {
                radioBing.IsChecked = true;
            }
            else
            {
                radioOwn.IsChecked = true;
            }

            chkShowBattery.IsChecked = appSettings.chkBattery;
            chkShowSMS.IsChecked = appSettings.chkSMS;
            chkShowWeather.IsChecked = appSettings.chkWeather;
            chkRAM.IsChecked = appSettings.chkRAM;
            chkWeatherEffects.IsChecked = appSettings.enableWeatherEffects;
            chkCelsius.IsChecked = appSettings.celsius;

            curWOEID = appSettings.woeid;

            txtCity.Text = appSettings.city;
        }
        void saveSettings()
        {
            
            appSettings.chkBattery = chkShowBattery.IsChecked.Value;
            appSettings.chkSMS = chkShowSMS.IsChecked.Value;
            appSettings.chkWeather = chkShowWeather.IsChecked.Value;
            appSettings.useBing = radioBing.IsChecked.Value;
            appSettings.chkRAM = chkRAM.IsChecked.Value;
            appSettings.enableWeatherEffects = chkWeatherEffects.IsChecked.Value;
            appSettings.celsius = chkCelsius.IsChecked.Value;

            appSettings.woeid = curWOEID;

            appSettings.city = txtCity.Text;

            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                try
                {
                    store.DeleteFile("widgets.xml");
                }
                catch
                {
                }
                using (var stream = new IsolatedStorageFileStream("widgets.xml", FileMode.OpenOrCreate, store))
                {
                    var serial = new XmlSerializer(typeof(appdata));
                    serial.Serialize(stream, appSettings);
                    stream.Close();
                }
            }
        }
        void startBGAgent()
        {
            Microsoft.Phone.BackgroundAudio.BackgroundAudioPlayer.Instance.Play();
            //Microsoft.Phone.BackgroundAudio.BackgroundAudioPlayer.Instance.SkipNext();
        }
        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            //debug only
            //startBGAgent();
        }

  

        private void textBlock2_Tap(object sender, GestureEventArgs e)
        {
            Microsoft.Phone.Tasks.WebBrowserTask t = new Microsoft.Phone.Tasks.WebBrowserTask();
            t.Uri = new Uri("http://windowsphonehacker.com/", UriKind.Absolute);
            t.Show();
        }

        private void radioOwn_Checked(object sender, RoutedEventArgs e)
        {
            btnChoose.Visibility = System.Windows.Visibility.Visible;
        }

        private void radioOwn_Unchecked(object sender, RoutedEventArgs e)
        {
            btnChoose.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void btnChoose_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Phone.Tasks.PhotoChooserTask t = new Microsoft.Phone.Tasks.PhotoChooserTask();
            t.Completed += new EventHandler<Microsoft.Phone.Tasks.PhotoResult>(t_Completed);
            t.Show();
        }

        void t_Completed(object sender, Microsoft.Phone.Tasks.PhotoResult e)
        {
            if (e.TaskResult == Microsoft.Phone.Tasks.TaskResult.OK)
            {
                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (store.FileExists("mybg.jpg"))
                        store.DeleteFile("mybg.jpg");

                    using (var stream = new IsolatedStorageFileStream("mybg.jpg", FileMode.OpenOrCreate, store))
                    {
                        e.ChosenPhoto.CopyTo(stream);
                    }

                }
            }
            else
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show("Oops, looks like your phone is syncing. Disconnect it and try again.");
                });
            }
        }

        private void settingChanged(object sender, RoutedEventArgs e)
        {
            //saveSettings();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            saveSettings();
            
            startBGAgent();
            controls.Visibility = System.Windows.Visibility.Collapsed;
            suspended.Visibility = System.Windows.Visibility.Visible;

        }


        private void ApplicationBarMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("LockWidgets 2.0 final\n\nUses fiinix's DllImport library for battery/ram data\n\nDeveloped by Jaxbot @ windowsphonehacker");
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            startBGAgent();
        }

        private void txtCity_LostFocus(object sender, RoutedEventArgs e)
        {
            if (chkShowWeather.IsChecked.Value)
            {
                
                WebClient c = new WebClient();
                c.DownloadStringAsync(new Uri("http://where.yahooapis.com/geocode?q=" + txtCity.Text, UriKind.Absolute));
                c.DownloadStringCompleted += new DownloadStringCompletedEventHandler(c_DownloadStringCompleted);
                
            }
        }

        void c_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Result != null)
            {
                try
                {
                    string[] s = e.Result.Split(new string[] { "<woeid>" }, StringSplitOptions.None);
                    s = s[1].Split("<".ToCharArray());
                    curWOEID = s[0];
                    System.Diagnostics.Debug.WriteLine(s[0]);
                }
                catch
                {
                    MessageBox.Show("Bad city name / couldnt get WOEID: " + e.Result);
                }
               
            }
            else
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show("Couldn't get WOEID for that city (network issue)");
                });
            }

        }

        private void chkCelsius_Checked(object sender, RoutedEventArgs e)
        {

        }

    }
    public class appdata
    {
        public bool chkSMS;
        public bool chkRAM;
        public bool chkWeather;
        public bool chkBattery;

        public bool enableWeatherEffects;
        public bool celsius;
        public bool useBing;

        public string city;

        public string woeid;
    }
}