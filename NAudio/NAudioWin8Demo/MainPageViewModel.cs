﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Win8.Wave.WaveOutputs;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Controls;
using System.IO;
using NAudio.MediaFoundation;

using NAudio.Win8;
using SpectrumAnalyzer;

namespace NAudioWin8Demo
{
    class MainPageViewModel : ViewModelBase
    {
        private IWavePlayer player;
        private WaveStream reader;
        private IWaveIn recorder;
        private MemoryStream recordStream;
        private IRandomAccessStream selectedStream;

        public MainPageViewModel()
        {
            LoadCommand = new DelegateCommand(Load);
            PlayCommand = new DelegateCommand(Play) { IsEnabled = false };
            PauseCommand = new DelegateCommand(Pause) { IsEnabled = false };
            StopCommand = new DelegateCommand(Stop) { IsEnabled = false };
            RecordCommand = new DelegateCommand(Record);
            StopRecordingCommand = new DelegateCommand(StopRecording) { IsEnabled = false };
            MediaFoundationApi.Startup();
        }
        
        private void Stop()
        {
            if (player != null)
            {
                player.Stop();
            }
        }

        private void Pause()
        {
            if (player != null)
            {
                player.Pause();
            }
        }

        private async void Play()
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
                StopCommand.IsEnabled = true;
                PauseCommand.IsEnabled = true;
                LoadCommand.IsEnabled = false;
            }
        }

        private IWaveProvider CreateReader()
        {
            if (reader is RawSourceWaveStream)
            {
                reader.Position = 0;
                return reader;
            }
            reader = new MediaFoundationReaderRT(selectedStream);
            return reader;
        }        

        private void Record()
        {
            if (recorder == null)
            {
                recorder = new WasapiCaptureRT();

                //dx: added a specification for recorder's WaveFormat so it can be played back in real time
                //int sampleRate = 44100;
                //int bitDepth = 16;
                //int channelCount = 2;
                //recorder.WaveFormat = new WaveFormat(sampleRate, bitDepth, channelCount);
                //dx: actually, as long as your playback device's sample rate, channels, and bitdepth matche those of the recording device, the real time play back work without the above codes
                
                recorder.RecordingStopped += RecorderOnRecordingStopped;
                recorder.DataAvailable += RecorderOnDataAvailable;               
            }

            if (reader != null)
            {
                reader.Dispose();
                reader = null;
            }
            
            recorder.StartRecording();

            RecordCommand.IsEnabled = false;
            StopRecordingCommand.IsEnabled = true;
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
            RecordCommand.IsEnabled = true;
            StopRecordingCommand.IsEnabled = false;            
            PlayCommand.IsEnabled = true;    
        }


        private void PlayerOnPlaybackStopped(object sender, StoppedEventArgs stoppedEventArgs)
        {
            LoadCommand.IsEnabled = true;
            StopCommand.IsEnabled = false;
            PauseCommand.IsEnabled = false;
            if (reader != null)
            {
                reader.Position = 0;
            }
        }

        private async void Load()
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
            PlayCommand.IsEnabled = true;
        }



        public DelegateCommand LoadCommand { get; private set; }
        public DelegateCommand PlayCommand { get; private set; }
        public DelegateCommand PauseCommand { get; private set; }
        public DelegateCommand StopCommand { get; private set; }
        public DelegateCommand RecordCommand { get; private set; }
        public DelegateCommand StopRecordingCommand { get; private set; }

        public MediaElement MediaElement { get; set; }
    }


}
