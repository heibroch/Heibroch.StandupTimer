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
        private readonly Dictionary<int, StandupParticipant> timeSpentPerPerson;
        private int imageIndex = -1;
        private DispatcherTimer dispatcherTimer;
        private const double TickSize = 0.25;
        private const int DefaultStandupTimeInSeconds = 60;

        public MainWindowViewModel()
        {
            ResetValues();

            NextCommand = new ActionCommand(ExecuteNextCommand);
            SkipCommand = new ActionCommand(ExecuteSkipCommand);
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

            timeSpentPerPerson = new Dictionary<int, StandupParticipant>();
        }

        private void ExecuteResetCommand(object obj)
        {
            if (dispatcherTimer == null) return;
            dispatcherTimer.Stop();
            ResetValues();
            dispatcherTimer.Start();
        }
        private void ExecutePauseCommand(object obj)
        {
            if (dispatcherTimer == null) return;
            if (dispatcherTimer.IsEnabled)
                dispatcherTimer.Stop();
            else dispatcherTimer.Start();
        }
        private void ExecuteSkipCommand(object obj)
        {
            timeSpentPerPerson[imageIndex].Skipped = true;
            StartNewPerson();
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

            StartNewPerson();
        }

        private void StartNewPerson()
        {
            dispatcherTimer.Stop();
            SetNextImage();
            ResetValues();
            dispatcherTimer.Start();
        }

        private void OnDispatcherTimerTick(object sender, EventArgs e)
        {
            CurrentValue -= TickSize;

            if (timeSpentPerPerson.ContainsKey(imageIndex))
                timeSpentPerPerson[imageIndex].TimeSpent += TickSize;


            RaisePropertyChanged(nameof(CurrentValue));
            RaisePropertyChanged(nameof(TimeLeft));
            RaisePropertyChanged(nameof(IsOverTime));
        }

        private void ResetValues()
        {
            MaxValue = DefaultStandupTimeInSeconds;
            CurrentValue = DefaultStandupTimeInSeconds;
            MinValue = 0;
            Achieved = false;
            Goal = false;
            Impediment = false;
            RaisePropertyChanged(nameof(Achieved));
            RaisePropertyChanged(nameof(Goal));
            RaisePropertyChanged(nameof(Impediment));
        }
        private void SetNextImage()
        {
            imageIndex++;

            if (imageIndex >= imageList.Count)
            {
                var peopleDuringDaily = timeSpentPerPerson.Where(x => x.Value.Skipped == false);
                if (peopleDuringDaily.Count() != 0)
                {
                    MessageBox.Show("Total time spent on stand up: " + TimeSpan.FromSeconds(peopleDuringDaily.Sum(x => x.Value.TimeSpent)) + Environment.NewLine +
                                    "Average time spent per person: " + TimeSpan.FromSeconds(peopleDuringDaily.Sum(x => x.Value.TimeSpent) / imageList.Count) + Environment.NewLine +
                                    "Most time used: " + TimeSpan.FromSeconds(peopleDuringDaily.Max(x => x.Value.TimeSpent)) + Environment.NewLine +
                                    "Least time used: " + TimeSpan.FromSeconds(peopleDuringDaily.Min(x => x.Value.TimeSpent)) + Environment.NewLine);
                }
                else
                    MessageBox.Show("No one took part in the daily.");
                dispatcherTimer.Stop();

                return;
            }

            CurrentImage = new BitmapImage(new Uri(imageList[imageIndex]));
            Name = Path.GetFileNameWithoutExtension(imageList[imageIndex]);
            timeSpentPerPerson.Add(imageIndex, new StandupParticipant());
            
            RaisePropertyChanged(nameof(CurrentImage));
            RaisePropertyChanged(nameof(Name));
            RaisePropertyChanged(nameof(IsOverTime));
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
                return timeSpan < TimeSpan.FromSeconds(0)
                    ? $"-{Math.Abs(timeSpan.Minutes):00}:{Math.Abs(timeSpan.Seconds):00}"
                    : $"{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
            }
        }

        public bool IsOverTime => CurrentValue < 0;

        public ImageSource CurrentImage { get; set; }

        public ActionCommand NextCommand { get; set; }
        public ActionCommand PauseCommand { get; set; }
        public ActionCommand ResetCommand { get; set; }
        public ActionCommand SkipCommand { get; set; }

        public bool Achieved { get; set; }
        public bool Goal { get; set; }
        public bool Impediment { get; set; }
    }
}
