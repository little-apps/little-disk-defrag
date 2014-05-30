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
using System.IO;
using System.Threading;
using Little_Disk_Defrag.Helpers;

namespace Little_Disk_Defrag
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        private readonly List<string> _drives;
        private readonly List<string> _actions;
        private readonly List<string> _priorities;

        private string _selectedDrive;
        private string _selectedAction;
        private string _selectedPriority;

        private double _progressBarValue;
        private string _statusText;

        private bool Stopping = false;
        private bool QuitWhenDone = false;
        private bool ReEntrance = false;

        private readonly Defragment _defragger = new Defragment();
        private Thread _thread;
        private Timer _timer;

        public List<string> Drives
        {
            get { return this._drives; }
        }
        public List<string> Actions
        {
            get { return this._actions; }
        }
        public List<string> Priorities
        {
            get { return this._priorities; }
        }

        public string SelectedDrive
        {
            get { return this._selectedDrive; }
            set
            {
                this._selectedDrive = value;
                this.OnPropertyChanged("SelectedDrive");
            }
        }

        public string SelectedAction
        {
            get { return this._selectedAction; }
            set
            {
                this._selectedAction = value;
                this.OnPropertyChanged("SelectedAction");
            }
        }

        public string SelectedPriority
        {
            get { return this._selectedPriority; }
            set
            {
                this._selectedPriority = value;
                this.OnPropertyChanged("SelectedPriority");
            }
        }

        public double ProgressBarValue
        {
            get { return this._progressBarValue; }
            set
            {
                if (value < 0 || value > 100)
                    return;

                this._progressBarValue = value;

                this.OnPropertyChanged("ProgressBarValue");
                this.OnPropertyChanged("ProgressBarText");
            }
        }

        public string ProgressBarText
        {
            get
            {
                return Math.Round(this._progressBarValue,2).ToString() + "%";
            }
        }

        public string StatusText
        {
            get { return this._statusText; }
            set
            {
                this._statusText = value;
                this.OnPropertyChanged("StatusText");
            }
        }

        public string StartStopText
        {
            get
            {
                if (this._defragger.IsActive && !this._defragger.IsDoneYet && !this._defragger.HasError)
                    return "Stop";
                else
                    return "Start";
            }
        }

        public bool ControlsEnabled
        {
            get
            {
                return (!(this._defragger.IsActive && !this._defragger.IsDoneYet && !this._defragger.HasError));
            }
        }

        public string WindowTitle
        {
            get
            {
                string DefragText = Application.ResourceAssembly.GetName().Name + " v" + Application.ResourceAssembly.GetName().Version.Major + "." + Application.ResourceAssembly.GetName().Version.Minor;

                string Percent = string.Format("{0:F2}%", this.ProgressBarValue);

                if (this._defragger.IsActive)
                    DefragText = Percent + " - " + this._defragger.Volume.RootPath + " - " + DefragText;

                return DefragText;
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            this._timer = new Timer(new TimerCallback(UpdateDialog));
            this._timer.Change(0, 25);

            this._drives = new List<string>();

            foreach (DriveInfo di in DriveInfo.GetDrives())
            {
                if (di.IsReady && di.DriveType == DriveType.Fixed && di.AvailableFreeSpace > 0)
                    this.Drives.Add(di.ToString().Substring(0,2));
            }

            this._actions = new List<string>(new string[] { "Analyze", "Fast Defrag", "Normal Defrag" });
            this._priorities = new List<string>(new string[] { "High", "Above Normal", "Normal", "Below Normal", "Idle" });

            this.OnPropertyChanged("Drives");
            this.OnPropertyChanged("Actions");
            this.OnPropertyChanged("Priorities");

            this.SelectedDrive = this.Drives[0];
            this.SelectedAction = "Analyze";
            this.SelectedPriority = "Normal";

            this.ProgressBarValue = 0;
        }

        private void UpdateDialog(object o)
        {
            this.OnPropertyChanged("WindowTitle");
            this.OnPropertyChanged("StartStopText");
            this.OnPropertyChanged("ControlsEnabled");

            this.StatusText = this._defragger.StatusString;
            this.ProgressBarValue = this._defragger.StatusPercent;

            if (!this.ReEntrance)
            {
                if (this._defragger.ShowReport)
                {
                    // Show report
                    this.ReEntrance = true;

                    this.ShowReport(this._defragger);

                    this._defragger.ShowReport = false;

                    this.ReEntrance = false;
                }

                if (this._defragger.IsDoneYet || this._defragger.HasError && Stopping)
                {
                    // This is the code executed when defragging is finished (or stopped :)
                    Stopping = false;

                    if (QuitWhenDone)
                        this.btnExit_Click(new object(), new RoutedEventArgs());
                }
            }
        }

        private void ShowReport(Defragment defragger)
        {
            if (this.Dispatcher.Thread != Thread.CurrentThread)
            {
                this.Dispatcher.Invoke(new Action<Defragment>(ShowReport), defragger);
                return;
            }

            Report report = new Report(defragger);
            report.ShowDialog();
        }

        private void btnStartStop_Click(object sender, RoutedEventArgs e)
        {
            if (!this._defragger.IsActive)
            {
                this._defragger.Reset();

                Defragment.DefragMethod method;

                if (this.SelectedAction == "Analyze")
                    method = Defragment.DefragMethod.ANALYZE;
                else if (this.SelectedAction == "Fast Defrag")
                    method = Defragment.DefragMethod.FASTDEFRAG;
                else
                    method = Defragment.DefragMethod.NORMDEFRAG;

                this._defragger.Open(this.SelectedDrive, method);

                this._thread = new Thread(new ThreadStart(this._defragger.Start));

                switch (this.SelectedPriority)
                {
                    case "High":
                        this._thread.Priority = ThreadPriority.Highest;
                        break;
                    case "Above Normal":
                        this._thread.Priority = ThreadPriority.AboveNormal;
                        break;
                    case "Normal":
                        this._thread.Priority = ThreadPriority.Normal;
                        break;
                    case "Below Normal":
                        this._thread.Priority = ThreadPriority.BelowNormal;
                        break;
                    case "Idle":
                        this._thread.Priority = ThreadPriority.Lowest;
                        break;

                    default:
                        this._thread.Priority = ThreadPriority.Normal;
                        break;
                }

                this._thread.Start();
            }
            else
            {
                Stopping = true;
                this._defragger.Stop();
            }
            
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            if (!this._defragger.IsActive)
            {   
                // This is the code executing when quitting
                this.Close();
            }
            else
            {   // Tell defragging to finish and disable our button
                QuitWhenDone = true;
                this.btnStartStop_Click(new object(), new RoutedEventArgs());
            }
        }
    }
}
