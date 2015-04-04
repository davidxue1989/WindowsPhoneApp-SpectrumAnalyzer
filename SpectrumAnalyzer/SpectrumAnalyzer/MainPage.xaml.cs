using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

using Windows.UI.Core;

using System.Threading.Tasks;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SpectrumAnalyzer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public const int num_of_fft_bands = 256; //count of  bands returned by GetFFT
        List<Rectangle> rectangles = new List<Rectangle>();
        List<Rectangle> beats = new List<Rectangle>();
        private DispatcherTimer timer = new DispatcherTimer();

        MMAudioPlayer.Player player = new MMAudioPlayer.Player();

        NAudioWrapper audioWrapper;

        public MainPage()
        {
            this.InitializeComponent();


            if (audioWrapper == null)
            {
                audioWrapper = new NAudioWrapper(CoreWindow.GetForCurrentThread().Dispatcher);
            }
            audioWrapper.StartAsync();

            timer.Interval = TimeSpan.FromSeconds(1 / 60);
            timer.Tick += timer_Tick;
            timer.Start();



            double inc = 0;
            for (int i = 0; i < num_of_fft_bands; i++)
            {
                Rectangle rec = new Rectangle();
                SolidColorBrush myBrush = new SolidColorBrush(Windows.UI.Colors.Aqua);
                rec.Fill = myBrush;
                rec.HorizontalAlignment = HorizontalAlignment.Left;
                rec.Height = 0;
                rec.Margin = new Thickness(inc, 0, 0, 0);
                inc = inc + 3;
                rec.VerticalAlignment = VerticalAlignment.Bottom;
                rec.Width = 1;
                mainGrid.Children.Add(rec);
                rectangles.Add(rec);
            }

            inc = 0;
            Random rand = new Random();
            for (int j = 0; j < subbbands_count; j++)
            {
                Rectangle rec = new Rectangle();


                byte[] colorBytes = new byte[3];
                rand.NextBytes(colorBytes);
                Color randomColor = Color.FromArgb(255, colorBytes[0], colorBytes[1], colorBytes[2]);


                rec.Fill = new SolidColorBrush(randomColor); ;
                rec.HorizontalAlignment = HorizontalAlignment.Left;
                rec.Height = 0;
                rec.Margin = new Thickness(inc, 0, 0, 0);
                inc = inc + 30 + 3;
                rec.VerticalAlignment = VerticalAlignment.Bottom;
                rec.Width = 30;
                BandVisuals.Children.Add(rec);
                beats.Add(rec);
            }


            //StartAudio();
        }

        //void audioWrapper_FftCalculated(object sender, FftEventArgs e) {
        //    NAudio.Dsp.Complex[] fftResult = e.Result;
        //    for (int fftIdx = 0; fftIdx < Math.Min(num_of_fft_bands, fftResult.Length / 2); fftIdx++) {
        //        //calculate the power intensity = sqrt(real^2 + imag^2)*2
        //        double h = Math.Sqrt(fftResult[fftIdx].X*fftResult[fftIdx].X + fftResult[fftIdx].Y*fftResult[fftIdx].Y)*1000;
        //        if (h < 0)
        //            h = 0;
        //        rectangles[fftIdx].Height = h;
        //    }
        //}
        
        //void audioWrapper_MaximumCalculated(object sender, MaxSampleEventArgs e) {

        //}



        async private void StartAudio()
        {
            //var file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync("Assets\\440Hz-5sec.mp3");
            //var file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync("Assets\\sweep20-20klog.mp3");
            var file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync("Assets\\Surface-Movement.mp3");
            //var file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync("Assets\\1000hz.mp3");
            //var file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync("Assets\\Surface-Movement_wav.wav");
            var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);

            player.SetAudioData(stream);

            slider.Minimum = 0;
            slider.Maximum = player.Duration;
            slider_seek.Minimum = 0;
            slider_seek.Maximum = player.Duration;

            player.OnEndOfStream += player_OnEndOfStream;

            player.Start();

            timer.Interval = TimeSpan.FromSeconds(1 / 60);

            timer.Tick += timer_Tick;


            timer.Start();

        }

        void player_OnEndOfStream(MMAudioPlayer.EndOfStreamReason __param0)
        {
            if (__param0 == MMAudioPlayer.EndOfStreamReason.ok)
            {
                //ok, end of playing

                double pos = player.CurrentPosition;
            }
            else
            {
                //something wrong
            }
        }



        const int subbbands_count = 32;
        const int bands_history_count = 22;
        List<float[]> subbands_history = new List<float[]>();

        async void timer_Tick(object sender, object e)
        {

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, delegate
            {
                float vol = player.Vol;

                //if (player != null)
                if (audioWrapper != null && audioWrapper.fftResult != null)
                {

                    //slider.Value = player.CurrentPosition;


                    NAudio.Dsp.Complex[] fftResult = audioWrapper.fftResult;
                    for (int fftIdx = 0; fftIdx < Math.Min(num_of_fft_bands, fftResult.Length / 2); fftIdx++)
                    {
                        //calculate the power intensity = sqrt(real^2 + imag^2)*2
                        double h = Math.Sqrt(fftResult[fftIdx].X * fftResult[fftIdx].X + fftResult[fftIdx].Y * fftResult[fftIdx].Y) * 10000;
                        if (h < 0)
                            h = 0;
                        rectangles[fftIdx].Height = h;
                    }



                    //int cnt = 0;

                    //// frequency index for current FFT values returned by GetFFT is approx
                    //// [0] 60 Hz
                    //// [1] 110 Hz
                    //// [2] 150 Hz
                    //// [3] 220 Hz
                    //// [4] 360 Hz
                    //// [5] 440 Hz
                    //// [10] 880 Hz
                    //// [20] 1760 Hz
                    //// [41] 3520 Hz
                    //// try uncomment sine generator at SpectrumAnalyzerXAPO::Process
                    //// and run with some variants to see freq peaks
                    //var arr = player.GetFFT();


                    //DetectBeats(arr);


                    ////display FFT 
                    //foreach (var a in arr)
                    //{
                    //    double h = arr[cnt] * 1000;
                    //    if (h < 0)
                    //        h = 0;
                    //    rectangles[cnt].Height = h;

                    //    if (cnt < num_of_fft_bands)
                    //        cnt++;
                    //}
                }
            });

        }

        private void DetectBeats(float[] arr)
        {
            // simple (and may be slow!) beat detection algorithm
            // http://www.gamedev.net/page/resources/_/technical/math-and-physics/beat-detection-algorithms-r1952
            // good implementation need to be at least non linear in banding


            //animate bands
            foreach (var sq in beats)
            {
                if (sq.Height > 0)
                {
                    sq.Height = sq.Height - 20;
                    sq.Opacity = (sq.Height / 200);
                }
            }


            var subband_length = arr.Count() / subbbands_count;
            float[] band = new float[subbbands_count];

            for (int i = 0; i < subbbands_count; i++)
            {
                float subband_energy = 0;

                for (int j = 0; j < subband_length; j++)
                {
                    subband_energy = subband_energy + arr[i * subband_length + j];
                }

                band[i] = subband_energy;
            }

            if (subbands_history.Count < bands_history_count)
                subbands_history.Add(band);
            else
            {
                float[] historical_energy = new float[subbbands_count];

                foreach (var x in subbands_history)
                {
                    for (int m = 0; m < subbbands_count; m++)
                    {
                        historical_energy[m] = historical_energy[m] + x[m];
                    }
                }

                for (int q = 0; q < subbbands_count; q++)
                {
                    historical_energy[q] = historical_energy[q] / bands_history_count;

                    if (band[q] > 2.0 * historical_energy[q])
                    {
                        //beat detected
                        beats[q].Height = 200;
                        beats[q].Opacity = 1;
                    }
                }

                subbands_history.RemoveAt(0);

                subbands_history.Add(band);

            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }


        private void slider_seek_ValueChanged_1(object sender, RangeBaseValueChangedEventArgs e)
        {
            player.CurrentPosition = e.NewValue;
        }

        private void slider_volume_value_changed(object sender, RangeBaseValueChangedEventArgs e)
        {
            player.Vol = (float)e.NewValue;
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync("Assets\\Surface-Movement.mp3");
            var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
            slider_seek.Value = 0;
            player.SetAudioData(stream);
            slider.Minimum = 0;
            slider.Maximum = player.Duration;
            slider_seek.Minimum = 0;
            slider_seek.Maximum = player.Duration;


            //turn off vol 
            player.Vol = 0.0f;
            sliderVolume.Value = 0;


            player.Start();

        }

        private void Play_Button_Click(object sender, RoutedEventArgs e)
        {
            player.Start();
        }
        private void Stop_Button_Click(object sender, RoutedEventArgs e)
        {
            player.Stop();
        }

    }
}
