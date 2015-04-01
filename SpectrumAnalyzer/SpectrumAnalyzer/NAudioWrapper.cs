using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Wave;
using NAudio.MediaFoundation;
using System.IO;

using NAudio.Win8.Wave.WaveOutputs;
using NAudio.CoreAudioApi;


namespace SpectrumAnalyzer
{
    class NAudioWrapper
    {
        private WaveStream reader;
        private IWaveIn recorder;
        private MemoryStream recordStream;

        private IWavePlayer player;

        int counter;

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
            if (reader == null)
            {
                recordStream = new MemoryStream();
                reader = new RawSourceWaveStream(recordStream, recorder.WaveFormat);
            }

            await recordStream.WriteAsync(waveInEventArgs.Buffer, 0, waveInEventArgs.BytesRecorded);
            counter++;
            if (counter == 1000)
            {
                StopRecording();
                Play();
                counter = 0;
            }
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

        }

        private IWaveProvider CreateReader()
        {
            if (reader is RawSourceWaveStream)
            {
                reader.Position = 0;
                return reader;
            }
            else
            {
                return null; //dx: just a placeholder, this is for reading from file implementation, not used for now
            }
        }

        public async void Play()
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


        private void PlayerOnPlaybackStopped(object sender, StoppedEventArgs stoppedEventArgs)
        {
            if (reader != null)
            {
                reader.Position = 0;
            }
        }
    }
}
