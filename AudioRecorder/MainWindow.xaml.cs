using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using NAudio.Wave;
using NAudio.Lame;
using System.IO;
using Microsoft.Win32;
using System.Windows.Threading;
using System.Threading;

namespace AudioRecorder
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string _tempPath = System.IO.Path.GetTempPath();
        string _fileName = "tempRecording.wav";
        WasapiLoopbackCapture _capture;
        WaveFileWriter _waveFileWriter;
        DispatcherTimer _timer;
        TimeSpan _recordingTime;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnRecordClick(object sender, RoutedEventArgs e)
        {
            _recordingTime = new TimeSpan();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(1000);
            _timer.Tick += new EventHandler(TickHandler);

            _capture = new WasapiLoopbackCapture();
            _capture.WaveFormat = new WaveFormat(48000, 32, 2);
            _waveFileWriter = new WaveFileWriter($"{_tempPath}{_fileName}", _capture.WaveFormat);

            _capture.DataAvailable += DataAvailableHandler;
            _capture.RecordingStopped += RecordingStoppedHandler;

            _capture.StartRecording();
            _timer.Start();
            btnRecord.IsEnabled = false;
        }

        private void TickHandler(object sender, EventArgs e)
        {
            _recordingTime += TimeSpan.FromSeconds(1);
            txtTimer.Text = $"{_recordingTime.Minutes:00}:{_recordingTime.Seconds:00}";
        }

        private void btnStopClick(object sender, RoutedEventArgs e)
        {
            if (_capture == null) return;

            _capture.StopRecording();
        }

        private void DataAvailableHandler(object sender, WaveInEventArgs e)
        {
            _waveFileWriter.Write(e.Buffer, 0, e.BytesRecorded);
        }

        private void RecordingStoppedHandler(object sender, StoppedEventArgs e)
        {
            _capture.DataAvailable -= DataAvailableHandler;
            _capture.RecordingStopped -= RecordingStoppedHandler;

            _timer?.Stop();
            _timer = null;
            _waveFileWriter?.Dispose();
            _waveFileWriter = null;
            _capture?.Dispose();

            if (File.Exists($"{_tempPath}{_fileName}"))
            {
                SaveFileDialog saveDialog = new SaveFileDialog();
                saveDialog.Filter = "mp3 files (*.mp3)|*.mp3";
                saveDialog.FileName = $"recording_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}";
                saveDialog.FilterIndex = 1;
                saveDialog.RestoreDirectory = true;

                if (saveDialog.ShowDialog() == true)
                {
                    using (var reader = new AudioFileReader($"{_tempPath}{_fileName}"))
                    {
                        using (var writer = new LameMP3FileWriter(saveDialog.FileName, reader.WaveFormat, 320))
                        {
                            reader.CopyTo(writer);
                        }
                    }
                }

                File.Delete($"{_tempPath}{_fileName}");
            }

            btnRecord.IsEnabled = true;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_capture != null)
            {
                _capture.StopRecording();

                _waveFileWriter?.Dispose();
                _waveFileWriter = null;
                _capture.Dispose();
            }

            if (File.Exists($"{_tempPath}{_fileName}"))
            {
                File.Delete($"{_tempPath}{_fileName}");
            }
        }

        private void Window_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            this.Opacity = 0.5;
        }

        private void Window_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            this.Opacity = 1.0;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }
    }
}
