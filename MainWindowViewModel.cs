using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Heibroch.Common.Wpf;

namespace Heibroch.StandupTimer
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly List<string> imageList;
        private readonly Dictionary<int, double> timeSpentPerPerson;
        private int imageIndex = -1;
        private DispatcherTimer dispatcherTimer;
        private const double TickSize = 0.25;

        public MainWindowViewModel()
        {
            ResetValues();

            NextCommand = new ActionCommand(ExecuteNextCommand);
            PauseCommand = new ActionCommand(ExecutePauseCommand);
            ResetCommand = new ActionCommand(ExecuteResetCommand);

            var imagesPath = Environment.CurrentDirectory + "\\Images";
            if (!Directory.Exists(imagesPath))
            {
                Directory.CreateDirectory(imagesPath);
            }

            var files = Directory.GetFiles(imagesPath);
            if (files.Length <= 0)
            {
                MessageBox.Show("Images folder contains no images");
                return;
            }
            
            imageList = new List<string>(files);
            Shuffle(imageList);

            timeSpentPerPerson = new Dictionary<int, double>();
        }

        private void ExecuteResetCommand(object obj)
        {
            dispatcherTimer.Stop();
            ResetValues();
            dispatcherTimer.Start();
        }
        private void ExecutePauseCommand(object obj)
        {
            if (dispatcherTimer.IsEnabled)
                dispatcherTimer.Stop();
            else dispatcherTimer.Start();
        }
        private void ExecuteNextCommand(object obj)
        {
            if (dispatcherTimer == null)
            {
                dispatcherTimer = new DispatcherTimer(DispatcherPriority.Background);
                dispatcherTimer.Interval = TimeSpan.FromSeconds(TickSize);
                dispatcherTimer.Tick += OnDispatcherTimerTick;
                dispatcherTimer.Start();
            }

            dispatcherTimer.Stop();
            SetNextImage();
            ResetValues();
            dispatcherTimer.Start();
        }
        
        private void OnDispatcherTimerTick(object sender, EventArgs e)
        {
            CurrentValue -= TickSize;
            timeSpentPerPerson[imageIndex] += TickSize;
            RaisePropertyChanged(nameof(CurrentValue));
            RaisePropertyChanged(nameof(TimeLeft));
        }
        
        private void ResetValues()
        {
            MaxValue = 120;
            CurrentValue = 120;
            MinValue = 0;
        }
        private void SetNextImage()
        {
            imageIndex++;

            if (imageIndex >= imageList.Count)
            {
                MessageBox.Show("Total time spent on stand up: " + TimeSpan.FromSeconds(timeSpentPerPerson.Sum(x => x.Value)) + Environment.NewLine +
                                "Average time spent per person: " + TimeSpan.FromSeconds(timeSpentPerPerson.Sum(x => x.Value) / imageList.Count) + Environment.NewLine + 
                                "Most time used: " + TimeSpan.FromSeconds(timeSpentPerPerson.Max(x => x.Value)) + Environment.NewLine +
                                "Least time used: " + TimeSpan.FromSeconds(timeSpentPerPerson.Min(x => x.Value)) + Environment.NewLine);

                dispatcherTimer.Stop();

                return;
            }

            CurrentImage = new BitmapImage(new Uri(imageList[imageIndex]));
            Name = Path.GetFileNameWithoutExtension(imageList[imageIndex]);
            timeSpentPerPerson.Add(imageIndex, 0);
            
            RaisePropertyChanged(nameof(CurrentImage));
            RaisePropertyChanged(nameof(Name));
        }

        private static void Shuffle<T>(IList<T> list)
        {
            var provider = new RNGCryptoServiceProvider();
            var n = list.Count;
            while (n > 1)
            {
                byte[] box = new byte[1];
                do provider.GetBytes(box);
                while (!(box[0] < n * (Byte.MaxValue / n)));
                int k = (box[0] % n);
                n--;
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public double MaxValue { get; set; }
        public double MinValue { get; set; }
        public double CurrentValue { get; set; }

        public string Name { get; set; }

        public string TimeLeft
        {
            get
            {
                var timeSpan = TimeSpan.FromSeconds(CurrentValue);
                return $"{timeSpan.Hours}:{timeSpan.Minutes}:{timeSpan.Seconds}";
            }
        } 

        public ImageSource CurrentImage { get; set; }

        public ActionCommand NextCommand { get; set; }
        public ActionCommand PauseCommand { get; set; }
        public ActionCommand ResetCommand { get; set; }

    }
}
