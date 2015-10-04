/*
    Little Disk Defrag
    Copyright (C) 2008 Little Apps (http://www.little-apps.com/)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Little_Disk_Defrag.Helpers;

namespace Little_Disk_Defrag
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        private string _selectedDrive;
        private string _selectedAction;
        private string _selectedPriority;

        private double _progressBarValue;
        private string _statusText;

        private bool Stopping;
        private bool QuitWhenDone;
        private bool ReEntrance;

        private readonly Defragment _defragger = new Defragment();
        private Thread _thread;

        private readonly DispatcherTimer _resizeTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 1500), IsEnabled = false };

        public List<string> Drives { get; }

        public List<string> Actions { get; }

        public List<string> Priorities { get; }

        public bool IsDefragmentActive => ((_thread != null) && _thread.IsAlive);

        public string SelectedDrive
        {
            get { return _selectedDrive; }
            set
            {
                _selectedDrive = value;
                OnPropertyChanged("SelectedDrive");

                _defragger.CloseVolume();

                _defragger.Open(_selectedDrive, true);
                ShowDrawing(_defragger.Volume);
            }
        }

        public string SelectedAction
        {
            get { return _selectedAction; }
            set
            {
                _selectedAction = value;
                OnPropertyChanged("SelectedAction");

                Defragment.DefragMethod method;

                if (SelectedAction == "Analyze")
                    method = Defragment.DefragMethod.ANALYZE;
                else if (SelectedAction == "Fast Defrag")
                    method = Defragment.DefragMethod.FASTDEFRAG;
                else
                    method = Defragment.DefragMethod.NORMDEFRAG;

                _defragger.SetMethod(method);
            }
        }

        public string SelectedPriority
        {
            get { return _selectedPriority; }
            set
            {
                _selectedPriority = value;
                OnPropertyChanged("SelectedPriority");
            }
        }

        public double ProgressBarValue
        {
            get { return _progressBarValue; }
            set
            {
                if (value < 0 || value > 100)
                    return;

                _progressBarValue = value;

                OnPropertyChanged("ProgressBarValue");
                OnPropertyChanged("ProgressBarText");
            }
        }

        public string ProgressBarText => Math.Round(_progressBarValue,2) + "%";

        public string StatusText
        {
            get { return _statusText; }
            set
            {
                _statusText = value;
                OnPropertyChanged("StatusText");
            }
        }

        public string StartStopText
        {
            get
            {
                if (IsDefragmentActive)
                    return "Stop";
                return "Start";
            }
        }

        public bool ControlsEnabled => (!(IsDefragmentActive && !_defragger.IsDoneYet && !_defragger.HasError));

        public string WindowTitle
        {
            get
            {
                string DefragText = Application.ResourceAssembly.GetName().Name + " v" + Application.ResourceAssembly.GetName().Version.Major + "." + Application.ResourceAssembly.GetName().Version.Minor;

                string Percent = $"{ProgressBarValue:F2}%";

                if (IsDefragmentActive)
                    DefragText = Percent + " - " + _defragger.Volume.RootPath + " - " + DefragText;

                return DefragText;
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            _resizeTimer.Tick += _resizeTimer_Tick;

            var timer = new Timer(UpdateDialog);
            timer.Change(0, 25);

            Drives = new List<string>();

            foreach (DriveInfo di in DriveInfo.GetDrives())
            {
                //if (di.IsReady && di.DriveType == DriveType.Fixed && di.AvailableFreeSpace > 0)
                    Drives.Add(di.ToString().Substring(0,2));
            }

            Actions = new List<string>(new[] { "Analyze", "Fast Defrag", "Normal Defrag" });
            Priorities = new List<string>(new[] { "High", "Above Normal", "Normal", "Below Normal", "Idle" });

            OnPropertyChanged("Drives");
            OnPropertyChanged("Actions");
            OnPropertyChanged("Priorities");

            SelectedDrive = Drives[0];
            SelectedAction = "Analyze";
            SelectedPriority = "Normal";

            ProgressBarValue = 0;

            _defragger.Open(SelectedDrive, true);

            ShowDrawing(_defragger.Volume);
        }

        private void UpdateDialog(object o)
        {
            OnPropertyChanged("WindowTitle");
            OnPropertyChanged("StartStopText");
            OnPropertyChanged("ControlsEnabled");

            StatusText = _defragger.StatusString;
            ProgressBarValue = _defragger.StatusPercent;

            if (!ReEntrance)
            {
                //if (this._defragger.Volume != null && this._defragger.UpdateDrawing)
                //{
                //    this.ReEntrance = true;

                //    this.ShowDrawing(this._defragger.Volume);

                //    this.ReEntrance = false;

                //    this._defragger.UpdateDrawing = false;
                //}

                if (_defragger.ShowReport)
                {
                    // Show report
                    ReEntrance = true;

                    ShowReport(_defragger);

                    _defragger.ShowReport = false;

                    ReEntrance = false;
                }

                if (_defragger.IsDoneYet || _defragger.HasError && Stopping)
                {
                    // This is the code executed when defragging is finished (or stopped :)
                    Stopping = false;

                    if (QuitWhenDone)
                        btnExit_Click(new object(), new RoutedEventArgs());
                }
            }
        }

        private void ShowDrawing(DriveVolume volume)
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.Invoke(new Action<DriveVolume>(ShowDrawing), volume);
                return;
            }

            drawing.SetDriveVolume(volume);
        }

        private void ShowReport(Defragment defragger)
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.Invoke(new Action<Defragment>(ShowReport), defragger);
                return;
            }

            Report report = new Report(defragger);
            report.ShowDialog();
        }

        private void btnStartStop_Click(object sender, RoutedEventArgs e)
        {
            if (!IsDefragmentActive)
            {
                _defragger.Reset();

                _defragger.Open(SelectedDrive);

                _thread = new Thread(_defragger.Start);

                switch (SelectedPriority)
                {
                    case "High":
                        _thread.Priority = ThreadPriority.Highest;
                        break;
                    case "Above Normal":
                        _thread.Priority = ThreadPriority.AboveNormal;
                        break;
                    case "Normal":
                        _thread.Priority = ThreadPriority.Normal;
                        break;
                    case "Below Normal":
                        _thread.Priority = ThreadPriority.BelowNormal;
                        break;
                    case "Idle":
                        _thread.Priority = ThreadPriority.Lowest;
                        break;

                    default:
                        _thread.Priority = ThreadPriority.Normal;
                        break;
                }

                _thread.Start();
            }
            else
            {
                Stopping = true;
                _defragger.Stop();
            }
            
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            if (!IsDefragmentActive)
            {   
                // This is the code executing when quitting
                Close();
            }
            else
            {   // Tell defragging to finish and disable our button
                QuitWhenDone = true;
                btnStartStop_Click(new object(), new RoutedEventArgs());
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!e.HeightChanged && !e.WidthChanged)
                return;

            if (double.IsNaN(e.NewSize.Width) || double.IsNaN(e.NewSize.Height))
                return;

            Width = e.NewSize.Width;
            Height = e.NewSize.Height;

            _resizeTimer.IsEnabled = true;
            _resizeTimer.Stop();
            _resizeTimer.Start();
        }

        void _resizeTimer_Tick(object sender, EventArgs e)
        {
            _resizeTimer.IsEnabled = false;

            double w = Width;
            double h = (Content as Grid).RowDefinitions[0].ActualHeight;

            drawing.ChangeSize(w, h);

            drawing.Redraw();
        }
    }
}
