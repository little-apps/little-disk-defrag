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

using Little_Disk_Defrag.Helpers;
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
using System.Windows.Shapes;

namespace Little_Disk_Defrag
{
    /// <summary>
    /// Interaction logic for Report.xaml
    /// </summary>
    public partial class Report : Window, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        private readonly Defragment _defragger; 
        private readonly DefragReport _defragReport;

        private string _driveLetter;
        private string _dirCount;
        private string _volLabel;
        private string _filesCount;
        private string _serial;
        private string _totalSize;
        private string _fileSystem;
        private string _diskSize;
        private string _capacity;
        private string _wastedSlack;
        private string _freeSpace;
        private string _fragmented;
        private string _totalClusters;
        private string _avgFragments;
        private string _clusterSize;
        private string _recommendations;
        private int _selectedIndexUnit;
        private readonly string[] _units;

        public string DriveLetter
        {
            get { return this._driveLetter; }
            set
            {
                this._driveLetter = value;
                this.OnPropertyChanged("DriveLetter");
            }
        }

        public string DirCount
        {
            get { return this._dirCount; }
            set
            {
                this._dirCount = value;
                this.OnPropertyChanged("DirCount");
            }
        }

        public string VolumeLabel
        {
            get { return this._volLabel; }
            set
            {
                this._volLabel = value;
                this.OnPropertyChanged("VolumeLabel");
            }
        }

        public string FilesCount
        {
            get { return this._filesCount; }
            set
            {
                this._filesCount = value;
                this.OnPropertyChanged("FilesCount");
            }
        }
        public string Serial
        {
            get { return this._serial; }
            set
            {
                this._serial = value;
                this.OnPropertyChanged("Serial");
            }
        }
        public string TotalSize
        {
            get { return this._totalSize; }
            set
            {
                this._totalSize = value;
                this.OnPropertyChanged("TotalSize");
            }
        }
        public string FileSystem
        {
            get { return this._fileSystem; }
            set
            {
                this._fileSystem = value;
                this.OnPropertyChanged("FileSystem");
            }
        }

        public string DiskSize
        {
            get { return this._diskSize; }
            set
            {
                this._diskSize = value;
                this.OnPropertyChanged("DiskSize");
            }
        }
        public string Capacity
        {
            get { return this._capacity; }
            set
            {
                this._capacity = value;
                this.OnPropertyChanged("Capacity");
            }
        }
        public string WastedSlack
        {
            get { return this._wastedSlack; }
            set
            {
                this._wastedSlack = value;
                this.OnPropertyChanged("WastedSlack");
            }
        }
        public string FreeSpace
        {
            get { return this._freeSpace; }
            set
            {
                this._freeSpace = value;
                this.OnPropertyChanged("FreeSpace");
            }
        }
        public string Fragmented
        {
            get { return this._fragmented; }
            set
            {
                this._fragmented = value;
                this.OnPropertyChanged("Fragmented");
            }
        }

        public string TotalClusters
        {
            get { return this._totalClusters; }
            set
            {
                this._totalClusters = value;
                this.OnPropertyChanged("TotalClusters");
            }
        }

        public string AvgFragments
        {
            get { return this._avgFragments; }
            set
            {
                this._avgFragments = value;
                this.OnPropertyChanged("AvgFragments");
            }
        }
        public string ClusterSize
        {
            get { return this._clusterSize; }
            set
            {
                this._clusterSize = value;
                this.OnPropertyChanged("ClusterSize");
            }
        }

        public string Recommendations
        {
            get { return this._recommendations; }
            set
            {
                this._recommendations = value;
                this.OnPropertyChanged("Recommendations");
            }
        }

        public int SelectedIndexUnit
        {
            get { return this._selectedIndexUnit; }
            set
            {
                this._selectedIndexUnit = value;

                if (this._defragger != null)
                    this.UpdateData();

                this.OnPropertyChanged("SelectedIndexUnit");
            }
        }

        public string SelectedUnit
        {
            get { return this.Units[this.SelectedIndexUnit]; }
        }

        public string[] Units
        {
            get { return this._units; }
        }

        public Defragment Defragger
        {
            get { return this._defragger; }
        }

        public DefragReport DefragReport
        {
            get { return this._defragReport; }
        }

        public Report(Defragment defragger)
        {
            InitializeComponent();

            this._units = new string[] { "Bytes", "Kilobytes", "Megabytes", "Gigabytes" };
            this.OnPropertyChanged("Units");

            this.SelectedIndexUnit = 0;

            this._defragger = defragger;
            this._defragReport = defragger.Report;

            this.UpdateData();
        }

        private void UpdateData()
        {
            bool Fractional = false;
            uint BytesDivisor;

            switch (this.SelectedUnit)
            {
                case "Bytes":
                    BytesDivisor = 1;
                    break;
                case "Kilobytes":
                    BytesDivisor = 1024;
                    break;
                case "Megabytes":
                    BytesDivisor = 1024 * 1024;
                    break;
                case "Gigabytes":
                    Fractional = true;
                    BytesDivisor = 1024 * 1024 * 1024;
                    break;

                default:
                    BytesDivisor = 1;
                    break;
            }

            string Text;

            // Volume name
            this.DriveLetter = this.DefragReport.RootPath;

            // Volume label
            this.VolumeLabel = this.DefragReport.Label;

            // Volume Serial
            this.Serial = this.DefragReport.Serial;

            // File System
            this.FileSystem = this.DefragReport.FileSystem;

            // DiskSizeBytes
            if (Fractional)
                this.DiskSize = string.Format("{0:F2} {1}", (double)(this.DefragReport.DiskSizeBytes / (BytesDivisor / 1024D)) / 1024.0D, this.SelectedUnit);
            else
                this.DiskSize = string.Format("{0:N0}", this.DefragReport.DiskSizeBytes / BytesDivisor) + " " + this.SelectedUnit;

            // DiskFreeBytes
            if (Fractional)
                this.FreeSpace = string.Format("{0:F2} {1}", (double)(this.DefragReport.FreeBytes / (BytesDivisor / 1024D)) / 1024.0D, this.SelectedUnit);
            else
                this.FreeSpace = string.Format("{0:N0}", this.DefragReport.FreeBytes / BytesDivisor) + " " + this.SelectedUnit;

            // DiskSizeClusters
            this.TotalClusters = string.Format("{0:N0} clusters", this.DefragReport.ClusterCount);

            // DiskClusterSize
            this.ClusterSize = string.Format("{0} bytes", this.DefragReport.ClusterSize);

            // DirsCount
            this.DirCount = string.Format("{0:N0}", this.DefragReport.DirsCount);

            // FilesCount
            this.FilesCount = string.Format("{0:N0}", this.DefragReport.FilesCount);

            // FilesFragged
            this.Fragmented = string.Format("{0:F2}% {1:N0}", this.DefragReport.PercentFragged, this.DefragReport.FraggedFiles.Count);

            // Average Frags
            this.AvgFragments = string.Format("{0:F2}", this.DefragReport.AverageFragments);

            // FilesSizeBytes
            if (Fractional)
                this.TotalSize = string.Format("{0:F2} {1}", (double)(this.DefragReport.FilesSizeBytes / (BytesDivisor / 1024D)) / 1024.0D, this.SelectedUnit);
            else
                this.TotalSize = string.Format("{0:N0} {1}", this.DefragReport.FilesSizeBytes / (UInt64)BytesDivisor, this.SelectedUnit);

            // Files SizeOnDisk
            if (Fractional)
                this.DiskSize = string.Format("{0:F2} {1}", (double)((this.DefragReport.FilesSizeBytes + this.DefragReport.FilesSlackBytes) / (BytesDivisor / 1024D)) / 1024.0D, this.SelectedUnit);
            else
                this.DiskSize = string.Format("{0:N0} {1}", (this.DefragReport.FilesSizeBytes + this.DefragReport.FilesSlackBytes) / (UInt64)BytesDivisor, this.SelectedUnit);

            // FilesSlackBytes
            if (Fractional)
                this.WastedSlack = string.Format("({2:F2}%) {0:F2} {1}", (double)(this.DefragReport.FilesSlackBytes / (BytesDivisor / 1024D)) / 1024.0D, this.SelectedUnit, this.DefragReport.PercentSlack);
            else
                this.WastedSlack = string.Format("({2:F2}%) {0:N0} {1}", this.DefragReport.FilesSlackBytes / (UInt64)BytesDivisor, this.SelectedUnit, this.DefragReport.PercentSlack);

            // Recommendation
            bool PFRec = false; // Recommend based off percent fragged files?
            bool AFRec = false; // Recommend based off average fragments per file?

            if (this.DefragReport.PercentFragged >= 5.0D)
                PFRec = true;

            if (this.DefragReport.AverageFragments >= 1.1D)
                AFRec = true;

            Text = "* ";

            if (PFRec)
                Text += string.Format("{0:F2}% of the files on this volume are fragmented. ", this.DefragReport.PercentFragged);

            if (AFRec)
                Text += string.Format("The average fragments per file ({0:F2}) indicates a high degree of fragmentation. ", this.DefragReport.AverageFragments);

            if (this.DefragReport.PercentFragged < 5.0D && this.DefragReport.AverageFragments < 1.1D)
                Text = "* No defragmentation is necessary at this point.";
            else if (this.DefragReport.PercentFragged < 15.0D && this.DefragReport.AverageFragments < 1.3D)
                Text += "It is recommended that you perform a Fast Defrag.";
            else
                Text += "It is recommended that you perform an Extensive Defrag.";

            // Should we recommend a smaller cluster size?
            if (this.DefragReport.PercentSlack >= 10.0f)
            {
                Text += string.Format(
                    "\n* A large amount of disk space ({0:F2}%) is being lost " + 
                    "due to a large (%{0} bytes) cluster size. It is recommended " + 
                    "that you use a disk utility such as Partition Magic to " + 
                    "reduce the cluster size of this volume.",
                    this.DefragReport.PercentSlack,
                    this.DefragReport.ClusterSize
                    );
            }

            this.Recommendations = Text;
        }
    }
}
