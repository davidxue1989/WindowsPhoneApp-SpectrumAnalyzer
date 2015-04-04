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
        public const int num_of_fft_bands = 1024; //count of  bands returned by GetFFT
        List<Rectangle> rectangles = new List<Rectangle>();

        NAudioWrapper audioWrapper;

        public MainPage()
        {
            this.InitializeComponent();


            if (audioWrapper == null)
            {
                audioWrapper = new NAudioWrapper(CoreWindow.GetForCurrentThread().Dispatcher);
                audioWrapper.FftCalculated_UI += (s, a) => onFftCalculated(a);
            }
            audioWrapper.StartAsync();


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
                rec.Width = 3;
                mainGrid.Children.Add(rec);
                rectangles.Add(rec);
            }

        }

        public async void onFftCalculated(FftEventArgs e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, delegate
            {
                NAudio.Dsp.Complex[] fftResult = audioWrapper.fftResult;
                for (int fftIdx = 0; fftIdx < Math.Min(num_of_fft_bands, fftResult.Length); fftIdx++)
                {
                    //calculate the power intensity = sqrt(real^2 + imag^2) = magnitude
                    //note: the frequency corresponding to each bin is: 
                    //http://stackoverflow.com/questions/4364823/how-do-i-obtain-the-frequencies-of-each-value-in-a-fft
                    //The first bin in the FFT is DC (0 Hz), the second bin is Fs / N, where Fs is the sample rate and N is the size of the FFT. The next bin is 2 * Fs / N. To express this in general terms, the nth bin is n * Fs / N.
                    double h = Math.Sqrt(fftResult[fftIdx].X * fftResult[fftIdx].X + fftResult[fftIdx].Y * fftResult[fftIdx].Y) * 10000;
                    if (h < 0)
                        h = 0;
                    rectangles[fftIdx].Height = h;
                }
            });
        }


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }


    }
}
