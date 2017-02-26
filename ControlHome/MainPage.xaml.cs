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
using Windows.Devices.Sensors;
using Windows.Devices.Sensors.Custom;
using Windows.ApplicationModel.Core;
using Windows.Data.Json;
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

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
        public MainPage()
        {
            this.InitializeComponent();
            
        }

        public async void httpget()
        {
            Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();
            Uri requestUri = new Uri("https://api.thingspeak.com/channels/231958/feeds/last");
            Windows.Web.Http.HttpResponseMessage httpResponse = new Windows.Web.Http.HttpResponseMessage();
            httpResponse = await httpClient.GetAsync(requestUri);
            httpResponse.EnsureSuccessStatusCode();
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
                      //status.Text = r.field1 + " " + r.field2;
                      if (r.field1 == "\"1\"")
                          room1Switch.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                      else if (r.field1 == "\"0\"")
                          room1Switch.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                      if (r.field2 == "\"1\"")
                          room2Switch.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                      else if (r.field2 == "\"0\"")
                          room2Switch.Background = new SolidColorBrush(Windows.UI.Colors.Red);

                  });

        }

        private void room1switch_Click(object sender, RoutedEventArgs e)
        {
            string s = "";
            if ((room2Switch.Background as SolidColorBrush).Color == Windows.UI.Colors.Green)
                s = "&field2=1";
            else
                s = "&field2=0";
            if ((room1Switch.Background as SolidColorBrush).Color != Windows.UI.Colors.Green)
            {

                room1Switch.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                s = "field1=1" + s;
                http(s);
            }
            else
            {
                room1Switch.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                s = "field1=0" + s;
                http(s);
            }
            status.Text = "Room 1 light switched";
            //status.Text = s;
        }

        private void room2Switch_Click(object sender, RoutedEventArgs e)
        {
            string s = "";
            if ((room1Switch.Background as SolidColorBrush).Color == Windows.UI.Colors.Green)
                s = "field1=1";
            else
                s = "field1=0";
            if ((room2Switch.Background as SolidColorBrush).Color != Windows.UI.Colors.Green)
            {
                s = s + "&field2=1";
                http(s);
                room2Switch.Background = new SolidColorBrush(Windows.UI.Colors.Green);
            }
            else
            {
                room2Switch.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                s=s + "&field2=0";
                http(s);
            }
            status.Text = "Room 2 light switched";
            //status.Text = s;
        }

        public void allLightSwitch()
        {

            string s = "";
            if ((room1Switch.Background as SolidColorBrush).Color != Windows.UI.Colors.Green)
            {

                room1Switch.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                s = "field1=1";
            }
            else
            {
                room1Switch.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                s = "field1=0" ;
            }

            if ((room2Switch.Background as SolidColorBrush).Color != Windows.UI.Colors.Green)
            {
                s = s + "&field2=1";
                room2Switch.Background = new SolidColorBrush(Windows.UI.Colors.Green);
            }
            else
            {
                room2Switch.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                s = s + "&field2=0";
            }
            http(s);
            status.Text = "All lights switched.";
        }

        public async void http(string field)
        {
            Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();
            Uri requestUri = new Uri("https://api.thingspeak.com/update?api_key=CI57AXMBOSM1VQN1&"+field);
            Windows.Web.Http.HttpResponseMessage httpResponse = new Windows.Web.Http.HttpResponseMessage();
            string httpResponseBody = "";
            httpResponse = await httpClient.GetAsync(requestUri);
            httpResponse.EnsureSuccessStatusCode();
            httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
            status.Text = httpResponseBody;
            httpClient = null;
        }

        public async void tts(string text)
        {
            var media = new MediaElement();
            var s = new Windows.Media.SpeechSynthesis.SpeechSynthesizer();
            var stream = await s.SynthesizeTextToStreamAsync(text);
            media.SetSource(stream, stream.ContentType);
            media.Play();
        }

        private void speechRecognizerSwitch_Click(object sender, RoutedEventArgs e)
        {
            InitSpeechRecognizer();
            if ((speechRecognizerSwitch.Background as SolidColorBrush).Color != Windows.UI.Colors.Green)
                speechRecognizerSwitch.Background = new SolidColorBrush(Windows.UI.Colors.Green);
            else
                speechRecognizerSwitch.Background = new SolidColorBrush(Windows.UI.Colors.Red);

        }
        public async void InitSpeechRecognizer()
        {
            SpeechRecognizer Rec = new SpeechRecognizer();
            //Event Handlers
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
        private async void Rec_ResultGenerated(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {

            switch (args.Result.Text)
            {
                case "switch light of room one":
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                      () =>
                      {
                          room1switch_Click(null, null);
                          tts("I have switched the light of room one");
                      });

                    break;
                case "switch light of room two":
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                    () =>
                    {
                        room2Switch_Click(null, null);
                        tts("I have switched the light of room two");
                    });
                    break;
                case "switch all lights":
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                    () =>
                    {
                        allLightSwitch();
                        tts("I have switched the light of both rooms");
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

        private void button_Click(object sender, RoutedEventArgs e1)
        {
            var timer = new System.Threading.Timer((e) =>
            {
                httpget();
            }, null, 0, Convert.ToInt32(TimeSpan.FromMinutes(0.016).TotalMilliseconds));
        }
    }
}
