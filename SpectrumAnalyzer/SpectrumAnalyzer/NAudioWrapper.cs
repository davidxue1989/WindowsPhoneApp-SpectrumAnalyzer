using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Wave;
using NAudio.MediaFoundation;
using System.IO;

using NAudio.Win8;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Storage.Pickers;

using NAudio.Win8.Wave.WaveOutputs;
using NAudio.CoreAudioApi;

using NAudio.Dsp;
using NAudio.Wave.SampleProviders;

using Windows.UI.Core;

namespace SpectrumAnalyzer
{
    class NAudioWrapper
    {
        private WaveStream reader;
        int counter;
        private IWaveIn recorder;
        //private MemoryStream recordStream;

        private IRandomAccessStream selectedStream;

        private IWavePlayer player;

        private SampleAggregator aggregator;
        public Complex[] fftResult;

        WaveFileWriter writer;

        private readonly CoreDispatcher dispatcher;

        public event EventHandler<FftEventArgs> FftCalculated_UI;


        public NAudioWrapper(CoreDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            MediaFoundationApi.Startup();
        }

        private async Task OnUiThread(Action action)
        {
            await this.dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action());
        }

        public async void StartAsync()
        {
#if false
            await LoadAsync();

            //Save("test");
            //Play();
            PlayFFT();

#else
            Record();
#endif
        }

        public void Record()
        {
            if (recorder == null)
            {
                recorder = new WasapiCaptureRT();
                recorder.RecordingStopped += RecorderOnRecordingStopped;
                recorder.DataAvailable += RecorderOnDataAvailable;
            }

            if (reader != null)
            {
                reader.Dispose();
                reader = null;
            }

            counter = 0;
            recorder.StartRecording();
        }

        private async void RecorderOnDataAvailable(object sender, WaveInEventArgs waveInEventArgs)
        {
            //dx: playback fft
            //if (reader == null)
            //{
            //    recordStream = new MemoryStream();
            //    reader = new RawSourceWaveStream(recordStream, (recorder.WaveFormat as WaveFormatExtensible).ToStandardWaveFormat()); //dx: this will make the waveformat encoding "pcm or ieeefloat" instead of the annoying microsoft's "extensible" (which is just pcm / ieeefloat under the hood)
            //    CreateReaderFFT(); //dx: so we can have a stream for FFT analysis in real-time
            //}
            //await recordStream.WriteAsync(waveInEventArgs.Buffer, 0, waveInEventArgs.BytesRecorded);
            //counter++;
            //if (counter == 500)
            //{
            //    StopRecording();
            //    PlayFFT();
            //    await OnUiThread(async () =>
            //    {
            //        await Save("test");
            //    });
            //    counter = 0;
            //}

            //dx: real-time fft
            if (aggregator == null)
            {
                aggregator = new SampleAggregator();
                aggregator.PerformFFT = true;
                aggregator.FftCalculated += (s, a) => OnFftCalculated(a);
            }
            float[] fftBuffer = new float[waveInEventArgs.BytesRecorded / 4];
            int outputIndex = 0;
            for (int n = 0; n < waveInEventArgs.BytesRecorded; n += 4)
            {
                fftBuffer[outputIndex++] = BitConverter.ToSingle(waveInEventArgs.Buffer, n);
            }
            for (int n = 0; n < waveInEventArgs.BytesRecorded / 4; n += (sender as IWaveIn).WaveFormat.Channels)
            {
                aggregator.Add(fftBuffer[n]);
            }
            counter += waveInEventArgs.BytesRecorded;
        }

        public void StopRecording()
        {
            if (recorder != null)
            {
                recorder.StopRecording();
            }
        }

        private void RecorderOnRecordingStopped(object sender, StoppedEventArgs stoppedEventArgs)
        {
            if (recorder != null)
            {
                recorder.Dispose();
                recorder = null;
            }
        }

        private IWaveProvider CreateReader()
        {
            if (reader is RawSourceWaveStream)
            {
                reader.Position = 0;
            }
            else
            {
                reader = new MediaFoundationReaderRT(selectedStream);
            }
            return reader;
        }

        private IWaveProvider CreateReaderFFT()
        {
            if (aggregator == null)
            {
                aggregator = new SampleAggregator(WaveExtensionMethods.ToSampleProvider(CreateReader()));
                aggregator.PerformFFT = true;
                aggregator.FftCalculated += (s, a) => OnFftCalculated(a);
            }
            return new SampleToWaveProvider(aggregator);
        }

        public void Play()
        {
            if (player == null)
            {
                // Exclusive mode - fails with a weird buffer alignment error
                player = new WasapiOutRT(AudioClientShareMode.Shared, 200);
                player.Init(CreateReader);

                player.PlaybackStopped += PlayerOnPlaybackStopped;
            }

            if (player.PlaybackState != PlaybackState.Playing)
            {
                //reader.Seek(0, SeekOrigin.Begin);
                player.Play();
            }
        }


        public void PlayFFT()
        {
            if (player == null)
            {
                // Exclusive mode - fails with a weird buffer alignment error
                player = new WasapiOutRT(AudioClientShareMode.Shared, 200);
                player.Init(CreateReaderFFT);

                player.PlaybackStopped += PlayerOnPlaybackStopped;
            }

            if (player.PlaybackState != PlaybackState.Playing)
            {
                //reader.Seek(0, SeekOrigin.Begin);
                player.Play();
            }

        }

        private void PlayerOnPlaybackStopped(object sender, StoppedEventArgs stoppedEventArgs)
        {
            if (reader != null)
            {
                reader.Position = 0;
            }
        }

        public async Task LoadAsync()
        {
            if (player != null)
            {
                player.Dispose();
                player = null;
            }
            reader = null; // will be disposed by player

            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
            picker.FileTypeFilter.Add("*");
            var file = await picker.PickSingleFileAsync();
            if (file == null) return;
            var stream = await file.OpenAsync(FileAccessMode.Read);
            if (stream == null) return;
            this.selectedStream = stream;
        }

        protected virtual void OnFftCalculated(FftEventArgs e)
        {
            //save the fft result
            fftResult = new Complex[e.Result.Length];
            for (int i = 0; i < fftResult.Length; i++)
            {
                fftResult[i] = e.Result[i];
            }

            //update UI
            if (FftCalculated_UI!=null)
                FftCalculated_UI(this, e);
        }


        public async Task Save(string filename)
        {
            writer = new WaveFileWriter();
            await writer.CreateWaveFile(filename, CreateReader());
            writer.Dispose();
            writer = null;
        }


        private async Task CreateFile(string fileName)
        {
            FileSavePicker savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
            savePicker.SuggestedFileName = fileName;
            savePicker.FileTypeChoices.Clear();
            savePicker.FileTypeChoices.Add("WAV", new List<string>() { ".wav" });

            // Open the file save picker.
            var file = await savePicker.PickSaveFileAsync();
            if (file == null) return;
            IRandomAccessStream stream;
            stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
            if (stream == null) return;

            return;
        }

    }
    public class SampleAggregator : ISampleProvider
    {

        // FFT
        public event EventHandler<FftEventArgs> FftCalculated;
        public bool PerformFFT { get; set; }
        private readonly Complex[] fftBuffer;
        private readonly FftEventArgs fftArgs;
        private int fftPos;
        private readonly int fftLength;
        private int m;
        private readonly ISampleProvider source;

        private readonly int channels;

        public SampleAggregator(int fftLength = 1024) //dx: for real-time fft, we don't need a source stream, we feed the buffer ourselves
        {
            if (!IsPowerOfTwo(fftLength))
            {
                throw new ArgumentException("FFT Length must be a power of two");
            }
            this.m = (int)Math.Log(fftLength, 2.0);
            this.fftLength = fftLength;
            this.fftBuffer = new Complex[fftLength];
            this.fftArgs = new FftEventArgs(fftBuffer);
        }

        public SampleAggregator(ISampleProvider source, int fftLength = 1024)
        {
            channels = source.WaveFormat.Channels;
            if (!IsPowerOfTwo(fftLength))
            {
                throw new ArgumentException("FFT Length must be a power of two");
            }
            this.m = (int)Math.Log(fftLength, 2.0);
            this.fftLength = fftLength;
            this.fftBuffer = new Complex[fftLength];
            this.fftArgs = new FftEventArgs(fftBuffer);
            this.source = source;
        }

        bool IsPowerOfTwo(int x)
        {
            return (x & (x - 1)) == 0;
        }

        public void Add(float value)
        {
            if (PerformFFT && FftCalculated != null)
            {
                fftBuffer[fftPos].X = (float)(value * FastFourierTransform.HammingWindow(fftPos, fftLength));
                fftBuffer[fftPos].Y = 0;
                fftPos++;
                if (fftPos >= fftBuffer.Length)
                {
                    fftPos = 0;
                    // 1024 = 2^10
                    FastFourierTransform.FFT(true, m, fftBuffer);
                    FftCalculated(this, fftArgs);
                }
            }
        }

        public WaveFormat WaveFormat { get { return source.WaveFormat; } }

        public int Read(float[] buffer, int offset, int count)
        {
            var samplesRead = source.Read(buffer, offset, count);

            for (int n = 0; n < samplesRead; n += channels)
            {
                Add(buffer[n + offset]);
            }
            return samplesRead;
        }
    }

    public class FftEventArgs : EventArgs
    {
        public FftEventArgs(Complex[] result)
        {
            this.Result = result;
        }
        public Complex[] Result { get; private set; }
    }

}
