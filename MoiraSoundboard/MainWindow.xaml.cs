using HtmlAgilityPack;
using NAudio.Wave;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

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

                if (!File.Exists($@"..\..\Sounds\\{fileName}")) {
                    FileStream fileStream = File.Create($@"..\..\Sounds\\{fileName}");
                    stream.CopyTo(fileStream);
                }

                return fileName;
            }
            catch (Exception e) {
                Debug.WriteLine(e.Message);
                return null;
            }

        }

        private string FileNameToLabel(string fileName) {
            int moiraIndex = fileName.Contains("Moira_") ? fileName.IndexOf("Moira_") : 0;
            string noMoira = fileName.Substring(moiraIndex + (fileName.Contains("Moira_") ? 6 : 0));

            if (fileName.Contains(".ogg")) {
                int oggIndex = noMoira.IndexOf(".ogg");
                string noOgg = noMoira.Substring(0, oggIndex);
                return Regex.Replace(noOgg, "_", " ");
            }
            else if (fileName.Contains(".flac")) {
                int flacIndex = noMoira.IndexOf(".flac");
                string noFlac = noMoira.Substring(0, flacIndex);
                return Regex.Replace(noFlac, "_", "");
            }
            else if (fileName.Contains(".mp3")) {
                int mp3Index = noMoira.IndexOf(".mp3");
                string noMp3 = noMoira.Substring(0, mp3Index);
                return Regex.Replace(noMp3, "_", "");
            }

            return null;
        }

        public async Task PutSounds() {
            BrushConverter brushConverter = new BrushConverter();
            string soundUrl = "";

            foreach(var node in collection) {
                try {
                    if (node.OuterHtml.Contains(".ogg")) {
                        string baseString = Regex.Match(node.OuterHtml, @"https(.*)(ogg)").Value;
                        int oggIndex = baseString.IndexOf("ogg");
                        soundUrl = baseString.Substring(0, oggIndex) + "ogg";
                    }
                    else if (node.OuterHtml.Contains(".mp3")) {
                        string baseString = Regex.Match(node.OuterHtml, @"https(.*)(mp3)").Value;
                        int oggIndex = baseString.IndexOf("mp3");
                        soundUrl = baseString.Substring(0, oggIndex) + "mp3";
                    }
                    else if (node.OuterHtml.Contains(".flac")) {
                        string baseString = Regex.Match(node.OuterHtml, @"https(.*)(flac)").Value;
                        int oggIndex = baseString.IndexOf("flac");
                        soundUrl = baseString.Substring(0, oggIndex) + "flac";
                    }

                    string fileName = await DownloadSound(soundUrl);
                    string filePath = $@"../../Sounds/{fileName}";

                    SoundButton button = new SoundButton(FileNameToLabel(fileName), filePath);
                    button.Margin = new Thickness(5.0, 5.0, 10.0, 10.0);
                    button.Background = (Brush)brushConverter.ConvertFrom("#FF4A2371");
                    button.Foreground = Brushes.White;
                    button.Click += new RoutedEventHandler(ButtonClicked);
                    buttonsPanel.Children.Add(button);
                }
                catch(Exception ex) {
                    Debug.WriteLine($"Failure at {node.InnerText}");
                }
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
                waveOut.Dispose();
            }
        }

        private void AudioPlaybackStopped(object sender, EventArgs e) {
            WaveOutEvent player = sender as WaveOutEvent;
            player.Dispose();
        }

        private void HeaderDrag(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void CloseClicked(object sender, RoutedEventArgs e) {
            Close();
        }

        private void MinimizeClicked(object sender, RoutedEventArgs e) {
            this.WindowState = WindowState.Minimized;
        }
    }
}
