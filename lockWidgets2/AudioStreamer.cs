using Microsoft.Phone.BackgroundAudio;
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
using System.Runtime;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;
using Microsoft.Phone.Controls;
using Microsoft.Phone.UserData;
using System.Windows.Media.Imaging;
using System.IO;
using System.IO.IsolatedStorage;

using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
namespace lockWidgets2
{

    /// <summary>
    /// A background agent that performs per-track streaming for playback
    /// </summary>
    public class AudioTrackStreamer : AudioStreamingAgent
    {
        
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

        public appdata appSettings;
        Imangodll instance;

        public TextBlock text(string str, int fontsize)
        {
            TextBlock title = new TextBlock();
            title.FontSize = fontsize;
            title.FontWeight = FontWeights.ExtraLight;
            title.Text = str;

            title.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

            return title;

        }

        public Rectangle rect(int width, int height)
        {
            Rectangle r = new Rectangle();
            r.Width = width;
            r.Height = height;
            r.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

            return r;
        }

        /// <summary>
        /// Called when a new track requires audio decoding
        /// (typically because it is about to start playing)
        /// </summary>
        /// <param name="track">
        /// The track that needs audio streaming
        /// </param>
        /// <param name="streamer">
        /// The AudioStreamer object to which a MediaStreamSource should be
        /// attached to commence playback
        /// </param>
        /// <remarks>
        /// To invoke this method for a track set the Source parameter of the AudioTrack to null
        /// before setting  into the Track property of the BackgroundAudioPlayer instance
        /// property set to true;
        /// otherwise it is assumed that the system will perform all streaming
        /// and decoding
        /// </remarks>
        /// 
        int o = 0;


        protected override void OnBeginStreaming(AudioTrack track, AudioStreamer streamer)
        {
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
                        }
                    }
                }

            }

            Assembly a = Assembly.Load("Microsoft.Phone.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=24eec0d8c86cda1e");
            Type comBridgeType = a.GetType("Microsoft.Phone.InteropServices.ComBridge");
            MethodInfo dynMethod = comBridgeType.GetMethod("RegisterComDll", BindingFlags.Public | BindingFlags.Static);
            object retValue = dynMethod.Invoke(null, new object[] { "liblw.dll", new Guid("E79018CB-46A6-432D-8077-8C0863533001") });  
            
            instance = (Imangodll)new Cmangodll();

            instance.MessageBox7("BG Agent Fired", "lockwidgets", 0, out o);

            checkForDataChange();

            while (true)
            {
                System.Threading.Thread.Sleep(5000);

                if (instance.StringCall("aygshell", "SHIsLocked", "") == 1)
                {
                    checkForDataChange();
                }
                
                GC.Collect();
                
                System.Diagnostics.Debug.WriteLine("1: " + Microsoft.Phone.Info.DeviceExtendedProperties.GetValue("ApplicationCurrentMemoryUsage"));
                System.Diagnostics.Debug.WriteLine("1: " + Microsoft.Phone.Info.DeviceExtendedProperties.GetValue("ApplicationPeakMemoryUsage"));
            }
        }

        int counter = 0;

        int lastSMSCount = -1;

        string lastSMSFrom = "";
        string lastSMSBody = "";

        string current = "";
        string currenttemp = "";
        string todayHigh = "";
        string todayLow = "";
        string tomorrowHigh = "";
        string tomorrowCondition = "";


        bool bingUpdateStatePending = false; //this semaphore prevents wallpaper updates during bing downloads
        DateTime lastbing;

        int weatherdata = 1;

        void msgbox(string str)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                int q = 0;
                instance.MessageBox7(str, "lockwidgets", 0, out q);
            });
        }

        void checkForDataChange()
        {
            try
            {
                bool update = false;

                if (appSettings.useBing)
                {
                    System.Diagnostics.Debug.WriteLine(DateTime.Now.Subtract(lastbing).Hours);

                    if (DateTime.Now.Subtract(lastbing).Hours > 6)
                    {
                        lastbing = DateTime.Now;
                        updateBingWallpaper();
                        bingUpdateStatePending = true;
                    }
                }


                if (bingUpdateStatePending)
                    return;

                if (appSettings.chkSMS)
                {
                    int count = 0;
                    instance.getUnreadSMSCount(out count);

                    if (count != lastSMSCount)
                    {
                        string data = instance.getLatestSMS();
                        lastSMSBody = data.Substring(data.IndexOf(":") + 1);
                        string number = data.Substring(0, data.IndexOf(":"));

                        Microsoft.Phone.UserData.Contacts cc = new Microsoft.Phone.UserData.Contacts();
                        cc.SearchCompleted += new EventHandler<Microsoft.Phone.UserData.ContactsSearchEventArgs>((object o, Microsoft.Phone.UserData.ContactsSearchEventArgs e) =>
                        {
                            if (e.Results.Count() > 0)
                            {
                                lastSMSFrom = e.Results.First().DisplayName;
                            }
                            else
                            {
                                lastSMSFrom = number;
                            }
                            updateWidgets();
                        });
                        cc.SearchAsync(number, Microsoft.Phone.UserData.FilterKind.PhoneNumber, null);

                        lastSMSCount = count;
                    }
                }

                if (appSettings.chkWeather)
                {
                    if (weatherdata == 1)
                    {
                        weatherdata = 0;

                        string query = "http://weather.yahooapis.com/forecastrss?w=" + appSettings.woeid + "&nocache=" + DateTime.Now.Millisecond.ToString() + DateTime.Now.Minute.ToString() + DateTime.Now.Hour.ToString();

                        System.Diagnostics.Debug.WriteLine(query);

                        WebClient cl = new System.Net.WebClient();
                        cl.DownloadStringAsync(new Uri(query));
                        cl.DownloadStringCompleted += new System.Net.DownloadStringCompletedEventHandler(cl_DownloadStringCompleted);

                        return;
                    }
                }


                if (counter > 400)
                {
                    updateWidgets();
                    counter = 1;
                    if (weatherdata == 0)
                    {
                        weatherdata = 1;
                    }
                }

                if (lastSMSCount == 0 && counter == 0 && appSettings.chkWeather == false) // first time thing
                {
                    updateWidgets();
                }

                counter++;
            }
            catch (Exception ex)
            {
                msgbox("Actual Exception in checkForDataChange(): " + ex.Message);
            }
            
        }
        void updateBingWallpaper()
        {
            try
            {
                WebClient client = new System.Net.WebClient();
                client.OpenReadCompleted += new System.Net.OpenReadCompletedEventHandler(client_OpenReadCompleted);
                client.OpenReadAsync(new Uri("http://appserver.m.bing.net/BackgroundImageService/TodayImageService.svc/GetTodayImage?dateOffset=-0&urlEncodeHeaders=true&osName=wince&osVersion=7.10&orientation=480x800&mkt=en-US&deviceName=donttreadonme&AppId=homebrew&UserId=rules&nocache=" + DateTime.Now.Millisecond.ToString() + DateTime.Now.Second.ToString() + DateTime.Now.DayOfYear.ToString(), UriKind.Absolute));
            }
            catch (Exception ex)
            {
                msgbox("Exception in bing dler: " + ex.Message);
            }
        }

        void client_OpenReadCompleted(object sender, System.Net.OpenReadCompletedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("1: " + Microsoft.Phone.Info.DeviceExtendedProperties.GetValue("ApplicationCurrentMemoryUsage"));
                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (var stream = new IsolatedStorageFileStream("mybg.jpg", FileMode.OpenOrCreate, store))
                    {
                        e.Result.CopyTo(stream);
                    }
                }
                System.Diagnostics.Debug.WriteLine("1: " + Microsoft.Phone.Info.DeviceExtendedProperties.GetValue("ApplicationCurrentMemoryUsage"));
                e = null;
                sender = null;

                GC.Collect();
            }
            catch (Exception ex)
            {
                msgbox("Exception in bing saver: " + ex.Message);
            }

            bingUpdateStatePending = false;
        }
        
        void cl_DownloadStringCompleted(object sender, System.Net.DownloadStringCompletedEventArgs e)
        {
            try
            {
                string s = delimit(e.Result, "<item>", "</item>");
                System.Diagnostics.Debug.WriteLine(s);
                current = delimit(s, "condition  text=\"", "\"");
                currenttemp = delimit(s, "temp=\"", "\"");
                s = delimit(s, "yweather:forecast", "<guid");
                s = delimit(s, "forecast", ">");
                tomorrowCondition = delimit(s, "text=\"", "\"");
                tomorrowHigh = delimit(s, "high=\"", "\"");


                sender = null;
                GC.Collect();

                updateWidgets();
            }
            catch (Exception ex)
            {
                msgbox("Exception in weather downloader: " + ex.Message);
            }

        }
        string temperatureString(string str)
        {
            int temp = Convert.ToInt16(str);
            string tempStr;

            if (appSettings.celsius)
            {
                temp = (int)((double)(5.0 / 9) * (temp - 32));
            }

            return temp + "°";
        }
        string delimit(string str, string beg, string end)
        {
            str = str.Substring(str.IndexOf(beg) + beg.Length);
            return str.Substring(0, str.IndexOf(end));
        }
        void updateWidgets()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("updating widgets...");

                    WriteableBitmap wb = new System.Windows.Media.Imaging.WriteableBitmap(480, 800);
                    
                    using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        using (var file = store.OpenFile("mybg.jpg", FileMode.Open, FileAccess.Read))
                        {
                            wb.LoadJpeg(file);
                        }
                    }

                    wb.Invalidate();

                    if (appSettings.enableWeatherEffects)
                    {
                        string overlay = "";
                        if (current.ToLower().Contains("rain"))
                        {
                            overlay = "raindrops";
                        }
                        if (current.ToLower().Contains("cloud"))
                        {
                            overlay = "clouds";
                        }
                        if (current.ToLower().Contains("sun"))
                        {
                            overlay = "sun";
                        }
                        if (current.ToLower().Contains("thunder"))
                        {
                            overlay = "lightning";
                        }
                        if (overlay != "")
                        {
                            BitmapImage rainimg = new BitmapImage();
                            rainimg.SetSource(Application.GetResourceStream(new Uri("img/" + overlay + ".png", UriKind.Relative)).Stream);
                            Image rain = new Image();
                            rain.Source = rainimg;
                            wb.Render(rain, null);
                            rainimg = null;
                        }
                    }

                    
                    if (appSettings.chkSMS)
                    {
                        if (lastSMSCount > 0)
                        {
                            wb.Render(text(lastSMSFrom, 30), new TranslateTransform { X = 20, Y = 300 });
                            wb.Render(text(lastSMSBody, 20), new TranslateTransform { X = 20, Y = 340 });
                            //wb.Render(text(lastSMSCount + " new messages", 20), new TranslateTransform { X = 290, Y = 345 });
                        }
                    }

                    if (appSettings.chkBattery)
                    {
                        try
                        {
                            byte percentb = 0;
                            instance.GetSystemPowerStatusEx7(out percentb);

                            double percent = percentb / 100.0;
                            System.Diagnostics.Debug.WriteLine(percent);

                            wb.Render(rect(100, 10), new TranslateTransform { X = 350, Y = 470 });

                            //bottom
                            wb.Render(rect(100, 10), new TranslateTransform { X = 350, Y = 710 });

                            //tip thingy
                            wb.Render(rect(40, 15), new TranslateTransform { X = 385, Y = 455 });

                            wb.Render(rect(10, 240), new TranslateTransform { X = 350, Y = 470 });
                            wb.Render(rect(10, 250), new TranslateTransform { X = 450, Y = 470 });

                            int h = (int)(210.0 * percent);

                            wb.Render(rect(70, h), new TranslateTransform { X = 370, Y = 490 + (210 - h)});
                        }
                        catch (Exception bex)
                        {
                            msgbox("exception while updating battery: " + bex.Message);
                        }
                    }

                    if (appSettings.chkRAM)
                    {
                        byte per = 0;
                        instance.getMemoryLoad(out per);

                        wb.Render(text(per.ToString() + "%", 20), new TranslateTransform { X = 115, Y = 7 });
                    }
                    if (appSettings.chkWeather)
                    {
                        if (current != "")
                        { 
                            wb.Render(text(temperatureString(currenttemp), 70), new TranslateTransform { X = 350, Y = 60 });
                            wb.Render(text(current.ToLower(), 22), new TranslateTransform { X = 350, Y = 130 });


                            
                            wb.Render(text("tomorrow", 20), new TranslateTransform { X = 350, Y = 200 });
                            wb.Render(text(temperatureString(tomorrowHigh), 50), new TranslateTransform { X = 350, Y = 220 });
                            wb.Render(text(tomorrowCondition.ToLower(), 20), new TranslateTransform { X = 350, Y = 267 });
                        }
                        
                    }

                                        
                    wb.Invalidate();
                    
                    GC.Collect();

                    using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        var filename = "image2.jpg";

                        if (store.FileExists(filename))
                            store.DeleteFile(filename);
                        
                        using (var st = new IsolatedStorageFileStream(filename, FileMode.Create, FileAccess.Write, store))
                        {
                            System.Windows.Media.Imaging.Extensions.SaveJpeg(wb, st, 480, 800, 0, 100);
                        }
                        wb = null;
                    }

                    string image = @"Applications\Data\af2b38c2-e921-4c2c-b658-ccd4439a6ee0\Data\IsolatedStore\image2.jpg";

                    instance.StringCall("aygshell", "SetCurrentWallpaper", image);
                                        
                    
                }
                catch (Exception ex)
                {
                    msgbox("exception in updatewidgets: " + ex.Message + ex.InnerException.Message + ex.StackTrace);
                }
                GC.Collect();

            });
        }

        
        /// <summary>
        /// Called when the agent request is getting cancelled
        /// </summary>
        protected override void OnCancel()
        {
            msgbox("I was told to cancel.");
            //base.OnCancel();
        }
    }
}
