using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Media.SpeechRecognition;
using Windows.Storage;
using Windows.ApplicationModel;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.ApplicationModel.Core;
using Windows.Data.Json;
using Windows.System.Threading;

namespace ControlHome
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public class RootObject
    {

        public string field1 { get; set; }
        public string field2 { get; set; }
    }
    public sealed partial class MainPage : Page
    {
        SpeechRecognizer Rec;
        public MainPage()
        {
            this.InitializeComponent();

            ThreadPoolTimer timer = ThreadPoolTimer.CreatePeriodicTimer((t) =>
            {
                httpget();
            }, TimeSpan.FromMinutes(0.05));

        }

        /// <summary>
        /// Code for working with server.
        /// </summary>
        //Code to get new data from the server
        public async void httpget()
        {
            Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();
            Uri requestUri = new Uri("https://api.thingspeak.com/channels/231958/feeds/last");
            Windows.Web.Http.HttpResponseMessage httpResponse;
            try {
                httpResponse = new Windows.Web.Http.HttpResponseMessage();
                httpResponse = await httpClient.GetAsync(requestUri);
                httpResponse.EnsureSuccessStatusCode();
            }
            catch
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                  () =>
                  {
                      status.Text = "Internet is not working!";
                  });
                return;
            }
            var jsonString = await httpResponse.Content.ReadAsStringAsync();
            JsonObject root = JsonObject.Parse(jsonString);
            RootObject r = new RootObject();
            foreach (var item in root)
            {
                if (item.Key == "field1")
                    r.field1 = item.Value.ToString();
                if (item.Key == "field2")
                    r.field2 = item.Value.ToString();
            }
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                  () =>
                  {
                      SwitchStatus(Int32.Parse(r.field1[1].ToString()), Int32.Parse(r.field2[1].ToString()));
                      status.Text = r.field1 + r.field2;
                  });
        }

        //Code to send data to the server. Return true if value is updated on server or false if not.
        public async Task<bool> http(string field)
        {
            Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();
            Uri requestUri = new Uri("https://api.thingspeak.com/update?api_key=CI57AXMBOSM1VQN1&" + field);

            string httpResponseBody = "";
            Windows.Web.Http.HttpResponseMessage httpResponse;
            try {
                httpResponse = new Windows.Web.Http.HttpResponseMessage();
                httpResponse = await httpClient.GetAsync(requestUri);
                httpResponse.EnsureSuccessStatusCode();
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
            }
            catch
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                  () =>
                  {
                      status.Text = "Internet is not working!";
                  });
                return false;
            }
            status.Text = httpResponseBody;
            httpClient = null;
            if (httpResponseBody == "0")
                return false;
            else
                return true;
        }

        /// <summary>
        /// Code for voice recognition.
        /// </summary>
        //To initialize Speech Recognizer
        public async void InitSpeechRecognizer(int n)
        {
	        if(n==0)
	        {
		        Rec.Dispose();
		        return;
            }
            Rec = new SpeechRecognizer();
            Rec.ContinuousRecognitionSession.ResultGenerated += Rec_ResultGenerated;

            StorageFile Store = await Package.Current.InstalledLocation.GetFileAsync(@"GrammarFile.xml");
            SpeechRecognitionGrammarFileConstraint constraint = new SpeechRecognitionGrammarFileConstraint(Store);
            Rec.Constraints.Add(constraint);
            SpeechRecognitionCompilationResult result = await Rec.CompileConstraintsAsync();
            if (result.Status == SpeechRecognitionResultStatus.Success)
            {
                status.Text = "Speech Recognition started.";
                tts(status.Text);
                Rec.UIOptions.AudiblePrompt = "Speech Recognition started.";
                await Rec.ContinuousRecognitionSession.StartAsync();
            }
        }

        //To handle Event by Speech Recognizer. Call TextToSpeech on the basis of that value is updated on server or not and switch the lights.
        private async void Rec_ResultGenerated(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {

            switch (args.Result.Text)
            {
                case "Turn on light of room one":
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                      async () =>
                      {
                          bool b = await light1Switch(1);
                          string s = b ? "I have turned on the light of room one" : "Sorry server can not be updated due to time limit.";
                          tts(s);
                      });

                    break;
                case "Turn off light of room one":
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                      async () =>
                      {
                          bool b = await light1Switch(0);
                          string s = b ? "I have turned off the light of room one" : "Sorry server can not be updated due to time limit.";
                          tts(s);
                      });

                    break;
                case "Turn on light of room two":
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                    async () =>
                    {
                        bool b = await light2Switch(1);
                        string s = b ? "I have turned on the light of room two" : "Sorry, server can not be updated due to time limit.";
                        tts(s);
                    });
                    break;
                case "Turn off light of room two":
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                    async () =>
                    {
                        bool b = await light2Switch(0);
                        string s = b ? "I have turned off the light of room two" : "Sorry, server can not be updated due to time limit.";
                        tts(s);
                    });
                    break;
                case "Turn on all the lights":
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                    async () =>
                    {
                        bool b = await allLightSwitch(1);
                        string s = b ? "I have turned on all the lights" : "Sorry, server can not be updated due to time limit.";
                        tts(s);
                    });
                    break;
                case "Turn off all the lights":
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                    async () =>
                    {
                        bool b = await allLightSwitch(0);
                        string s = b ? "I have turned off all the lights" : "Sorry, server can not be updated due to time limit.";
                        tts(s);
                    });
                    break;
                default:
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                   () =>
                   {
                       tts("Sorry I didn't get you.");
                   });
                    break;
            }
        }

        //Code for Text to Speech
        public async void tts(string text)
        {
            var media = new MediaElement();
            var s = new Windows.Media.SpeechSynthesis.SpeechSynthesizer();
            var stream = await s.SynthesizeTextToStreamAsync(text);
            media.SetSource(stream, stream.ContentType);
            media.Play();
        }

        //Code for Updating UI and sending values to server when all lights are switched. Return string value true if value is updated on server or false if not.
        public async Task<bool> allLightSwitch(int n)
        {
            string s = "";
            if (n == 1)
            {
                room1Switch.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                room2Switch.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                s = "field1=1&field2=1";
                bool b = await http(s);
                status.Text = b ? "All lights turned on." : "Sorry, server cannot be updated due to time limit.";
                return b;
            }
            else
            {
                room1Switch.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                room2Switch.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                s = "field1=0&field2=0";
                bool b = await http(s);
                status.Text = b ? "All lights turned off." : "Sorry, server cannot be updated due to time limit.";
                return b;
            }


        }

        //Code for Updating UI and sending values to server when room1 light is switched. Return string value true if value is updated on server or false if not.
        public async Task<bool> light1Switch(int n)
        {
            string s = (room2Switch.Background as SolidColorBrush).Color == Windows.UI.Colors.Green ? "&field2=1" : "&field2=0";
            if (n == 1)
            {
                s = "field1=1" + s;
                room1Switch.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                Debug.Write(s+"1234");
                bool b = await http(s);
                status.Text = b ? "Room 1 light turned on." : "Sorry, server cannot be updated due to time limit.";
                return b;
            }
            else
            {
                s = "field1=0" + s;
                room1Switch.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                Debug.Write(s);
                bool b = await http(s);
                status.Text = b ? "Room 1 light turned off." : "Sorry, server cannot be updated due to time limit.";
                return b;
            }
        }

        //Code to change status of rooms
        public void SwitchStatus(int n1,int n2)
        {
            if (n1 == 1)
                room1Switch.Background = new SolidColorBrush(Windows.UI.Colors.Green);
            else if(n1==0)
                room1Switch.Background = new SolidColorBrush(Windows.UI.Colors.Red);
            if (n2 == 1)
                room2Switch.Background = new SolidColorBrush(Windows.UI.Colors.Green);
            else if(n2==0)
                room2Switch.Background = new SolidColorBrush(Windows.UI.Colors.Red);
        }


        //Code for Updating UI and sending values to server when room2 light is switched. Return string value true if value is updated on server or false if not.
        public async Task<bool> light2Switch(int n)
        {
            
            string s = (room1Switch.Background as SolidColorBrush).Color == Windows.UI.Colors.Green ? "field1=1" : "field1=0";
            if (n == 1)
            {
                s = s + "&field2=1";
                Debug.Write(s);
                bool b = await http(s);
                status.Text = b ? "Room 2 light turned on." : "Sorry, server cannot be updated due to time limit.";
		if(b)
			room2Switch.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                return b;
            }
            else
            {
                s = s + "&field2=0";
                Debug.Write(s);
                bool b = await http(s);
                status.Text = b ? "Room 2 light turned off." : "Sorry, server cannot be updated due to time limit.";
		if(b)
			room2Switch.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                return b;
            }
        }

        private async void room1switch_Click(object sender, RoutedEventArgs e)
        {
            if ((room1Switch.Background as SolidColorBrush).Color != Windows.UI.Colors.Green)
                await light1Switch(1);
            else
                await light1Switch(0);
        }


        private async void room2Switch_Click(object sender, RoutedEventArgs e)
        {
            if ((room2Switch.Background as SolidColorBrush).Color != Windows.UI.Colors.Green)
                await light2Switch(1);
            else
                await light2Switch(0);
        }

        

       
        private void speechRecognizerSwitch_Click(object sender, RoutedEventArgs e)
        {
            if ((speechRecognizerSwitch.Background as SolidColorBrush).Color != Windows.UI.Colors.Green)
            {
                InitSpeechRecognizer(1);
                speechRecognizerSwitch.Background = new SolidColorBrush(Windows.UI.Colors.Green);
            }
            else
            {
                InitSpeechRecognizer(0);
                speechRecognizerSwitch.Background = new SolidColorBrush(Windows.UI.Colors.Red);
            }

        }
        
        private void button_Click(object sender, RoutedEventArgs e1)
        {
            ThreadPoolTimer timer = ThreadPoolTimer.CreatePeriodicTimer((t) =>
            {
                httpget();
            }, TimeSpan.FromMinutes(0.1));
        }
    }
}
