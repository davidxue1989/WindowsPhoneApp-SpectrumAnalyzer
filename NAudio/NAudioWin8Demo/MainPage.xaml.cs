using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Windows.UI.Core;

using SpectrumAnalyzer;
using System.Threading.Tasks;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace NAudioWin8Demo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        NAudioWrapper audioWrapper;


        public MainPage()
        {
            this.InitializeComponent();


            audioWrapper = new NAudioWrapper(CoreWindow.GetForCurrentThread().Dispatcher);
            audioWrapper.StartAsync();

            this.DataContext = new MainPageViewModel() {MediaElement = me};
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }


        void audioWrapper_FftCalculated(object sender, FftEventArgs e)
        {
            NAudio.Dsp.Complex[] fftResult = e.Result;
            int a = 0;
        }

        void audioWrapper_MaximumCalculated(object sender, MaxSampleEventArgs e)
        {

        }
    }
}
