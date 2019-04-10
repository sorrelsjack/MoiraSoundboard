using HtmlAgilityPack;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MoiraSoundboard {
    public partial class MainWindow : Window {
        public HtmlNodeCollection collection;

        public MainWindow() {
            InitializeComponent();
            GetSounds();
        }

        public async Task GetSounds() {
            HttpClient client = new HttpClient();
            try {
                HttpResponseMessage response = await client.GetAsync("https://overwatch.gamepedia.com/Moira/Quotes");
                Stream stream = await response.Content.ReadAsStreamAsync();

                HtmlDocument doc = new HtmlDocument();
                doc.Load(stream);
                collection = doc.DocumentNode.SelectNodes("//audio/@src");
            }
            catch(Exception e) {
                Debug.WriteLine(e.Message);
            }

            await PutSounds();
        }

        public async Task<string> DownloadSound(string url) {
            HttpClient client = new HttpClient();
            try {
                HttpResponseMessage response = await client.GetAsync(url);
                Stream stream = await response.Content.ReadAsStreamAsync();

                int indexOfSlash = url.LastIndexOf("/", url.Length - 1);
                string fileName = url.Substring(indexOfSlash + 1, url.Length - indexOfSlash - 1);

                if (!File.Exists(@"C:\temp\" + fileName)) {
                    FileStream fileStream = File.Create(@"C:\temp\" + fileName);
                    stream.CopyTo(fileStream);
                }
                /*if (!File.Exists($@"..\..\Sounds\\{fileName}")) {
                    FileStream fileStream = File.Create($@"..\..\Sounds\\{fileName}");
                    stream.CopyTo(fileStream);
                }*/

                return fileName;
                return $@"../../Sounds/{fileName}";
            }
            catch (Exception e) {
                Debug.WriteLine(e.Message);
                return null;
            }

        }

        private string FileNameToLabel(string fileName) {
            int moiraIndex = fileName.IndexOf("Moira_");
            string noMoira = fileName.Substring(moiraIndex + 6);

            int oggIndex = noMoira.IndexOf(".ogg");
            string noOgg = noMoira.Substring(0, oggIndex);

            return Regex.Replace(noOgg, "_", " ");
        }

        public async Task PutSounds() {
            foreach(var node in collection) {
                string baseString = Regex.Match(node.OuterHtml, @"https(.*)(ogg)").Value;
                int oggIndex = baseString.IndexOf("ogg");
                string soundUrl = baseString.Substring(0, oggIndex) + "ogg";

                string fileName = await DownloadSound(soundUrl);
                string filePath = @"C:\temp\" + fileName;

                SoundButton button = new SoundButton(FileNameToLabel(fileName), filePath);
                button.Click += new RoutedEventHandler(ButtonClicked);
                buttonsPanel.Children.Add(button);
            }
        }

        private void ButtonClicked(object sender, RoutedEventArgs e) {
            SoundButton button = sender as SoundButton;
            WaveOutEvent waveOut = new WaveOutEvent();

            FileStream fileStream = new FileStream(button.SoundClip, FileMode.Open, FileAccess.Read);
            MemoryStream memoryStream = new MemoryStream();
            fileStream.CopyTo(memoryStream);

            try {
                var vorbisStream = new NAudio.Vorbis.VorbisWaveReader(memoryStream);
                vorbisStream.Position = 0;
                waveOut.Init(vorbisStream);
                waveOut.PlaybackStopped += new EventHandler(AudioPlaybackStopped);
                waveOut.Play();
            }
            catch (Exception ex) {
                Debug.WriteLine(ex);
            }
        }

        private void AudioPlaybackStopped(object sender, EventArgs e) {
            WaveOutEvent player = sender as WaveOutEvent;
            player.Dispose();
        }
    }
}
