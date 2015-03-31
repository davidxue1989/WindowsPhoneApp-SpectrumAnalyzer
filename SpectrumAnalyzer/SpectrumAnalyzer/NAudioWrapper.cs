using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Wave;
using NAudio.MediaFoundation;
using System.IO;


namespace SpectrumAnalyzer
{
    class NAudioWrapper
    {
        private WaveStream reader;
        private IWaveIn recorder;
        private MemoryStream recordStream;


        private void Record()
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
        }

        private void StopRecording()
        {
            if (recorder != null)
            {
                recorder.StopRecording();
            }
        }

        private void RecorderOnRecordingStopped(object sender, StoppedEventArgs stoppedEventArgs)
        {

        }
    }
}
