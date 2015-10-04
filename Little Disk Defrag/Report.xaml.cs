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

using System.ComponentModel;
using System.Windows;
using Little_Disk_Defrag.Helpers;

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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

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

        public string DriveLetter
        {
            get { return _driveLetter; }
            set
            {
                _driveLetter = value;
                OnPropertyChanged("DriveLetter");
            }
        }

        public string DirCount
        {
            get { return _dirCount; }
            set
            {
                _dirCount = value;
                OnPropertyChanged("DirCount");
            }
        }

        public string VolumeLabel
        {
            get { return _volLabel; }
            set
            {
                _volLabel = value;
                OnPropertyChanged("VolumeLabel");
            }
        }

        public string FilesCount
        {
            get { return _filesCount; }
            set
            {
                _filesCount = value;
                OnPropertyChanged("FilesCount");
            }
        }
        public string Serial
        {
            get { return _serial; }
            set
            {
                _serial = value;
                OnPropertyChanged("Serial");
            }
        }
        public string TotalSize
        {
            get { return _totalSize; }
            set
            {
                _totalSize = value;
                OnPropertyChanged("TotalSize");
            }
        }
        public string FileSystem
        {
            get { return _fileSystem; }
            set
            {
                _fileSystem = value;
                OnPropertyChanged("FileSystem");
            }
        }

        public string DiskSize
        {
            get { return _diskSize; }
            set
            {
                _diskSize = value;
                OnPropertyChanged("DiskSize");
            }
        }
        public string Capacity
        {
            get { return _capacity; }
            set
            {
                _capacity = value;
                OnPropertyChanged("Capacity");
            }
        }
        public string WastedSlack
        {
            get { return _wastedSlack; }
            set
            {
                _wastedSlack = value;
                OnPropertyChanged("WastedSlack");
            }
        }
        public string FreeSpace
        {
            get { return _freeSpace; }
            set
            {
                _freeSpace = value;
                OnPropertyChanged("FreeSpace");
            }
        }
        public string Fragmented
        {
            get { return _fragmented; }
            set
            {
                _fragmented = value;
                OnPropertyChanged("Fragmented");
            }
        }

        public string TotalClusters
        {
            get { return _totalClusters; }
            set
            {
                _totalClusters = value;
                OnPropertyChanged("TotalClusters");
            }
        }

        public string AvgFragments
        {
            get { return _avgFragments; }
            set
            {
                _avgFragments = value;
                OnPropertyChanged("AvgFragments");
            }
        }
        public string ClusterSize
        {
            get { return _clusterSize; }
            set
            {
                _clusterSize = value;
                OnPropertyChanged("ClusterSize");
            }
        }

        public string Recommendations
        {
            get { return _recommendations; }
            set
            {
                _recommendations = value;
                OnPropertyChanged("Recommendations");
            }
        }

        public int SelectedIndexUnit
        {
            get { return _selectedIndexUnit; }
            set
            {
                _selectedIndexUnit = value;

                if (Defragger != null)
                    UpdateData();

                OnPropertyChanged("SelectedIndexUnit");
            }
        }

        public string SelectedUnit => Units[SelectedIndexUnit];

        public string[] Units { get; }

        public Defragment Defragger { get; }

        public DefragReport DefragReport { get; }

        public Report(Defragment defragger)
        {
            InitializeComponent();

            Units = new[] { "Bytes", "Kilobytes", "Megabytes", "Gigabytes" };
            OnPropertyChanged("Units");

            SelectedIndexUnit = 0;

            Defragger = defragger;
            DefragReport = defragger.Report;

            UpdateData();
        }

        private void UpdateData()
        {
            bool Fractional = false;
            uint BytesDivisor;

            switch (SelectedUnit)
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
            DriveLetter = DefragReport.RootPath;

            // Volume label
            VolumeLabel = DefragReport.Label;

            // Volume Serial
            Serial = DefragReport.Serial;

            // File System
            FileSystem = DefragReport.FileSystem;

            // DiskSizeBytes
            if (Fractional)
                DiskSize = string.Format("{0:F2} {1}", DefragReport.DiskSizeBytes / (BytesDivisor / 1024D) / 1024.0D, SelectedUnit);
            else
                DiskSize = string.Format("{0:N0}", DefragReport.DiskSizeBytes / BytesDivisor) + " " + SelectedUnit;

            // DiskFreeBytes
            if (Fractional)
                FreeSpace = string.Format("{0:F2} {1}", DefragReport.FreeBytes / (BytesDivisor / 1024D) / 1024.0D, SelectedUnit);
            else
                FreeSpace = string.Format("{0:N0}", DefragReport.FreeBytes / BytesDivisor) + " " + SelectedUnit;

            // DiskSizeClusters
            TotalClusters = string.Format("{0:N0} clusters", DefragReport.ClusterCount);

            // DiskClusterSize
            ClusterSize = string.Format("{0} bytes", DefragReport.ClusterSize);

            // DirsCount
            DirCount = string.Format("{0:N0}", DefragReport.DirsCount);

            // FilesCount
            FilesCount = string.Format("{0:N0}", DefragReport.FilesCount);

            // FilesFragged
            Fragmented = string.Format("{0:F2}% {1:N0}", DefragReport.PercentFragged, DefragReport.FraggedFiles.Count);

            // Average Frags
            AvgFragments = string.Format("{0:F2}", DefragReport.AverageFragments);

            // FilesSizeBytes
            if (Fractional)
                TotalSize = string.Format("{0:F2} {1}", DefragReport.FilesSizeBytes / (BytesDivisor / 1024D) / 1024.0D, SelectedUnit);
            else
                TotalSize = string.Format("{0:N0} {1}", DefragReport.FilesSizeBytes / BytesDivisor, SelectedUnit);

            // Files SizeOnDisk
            if (Fractional)
                DiskSize = string.Format("{0:F2} {1}", (DefragReport.FilesSizeBytes + DefragReport.FilesSlackBytes) / (BytesDivisor / 1024D) / 1024.0D, SelectedUnit);
            else
                DiskSize = string.Format("{0:N0} {1}", (DefragReport.FilesSizeBytes + DefragReport.FilesSlackBytes) / BytesDivisor, SelectedUnit);

            // FilesSlackBytes
            if (Fractional)
                WastedSlack = string.Format("({2:F2}%) {0:F2} {1}", DefragReport.FilesSlackBytes / (BytesDivisor / 1024D) / 1024.0D, SelectedUnit, DefragReport.PercentSlack);
            else
                WastedSlack = string.Format("({2:F2}%) {0:N0} {1}", DefragReport.FilesSlackBytes / BytesDivisor, SelectedUnit, DefragReport.PercentSlack);

            // Recommendation
            bool PFRec = false; // Recommend based off percent fragged files?
            bool AFRec = false; // Recommend based off average fragments per file?

            if (DefragReport.PercentFragged >= 5.0D)
                PFRec = true;

            if (DefragReport.AverageFragments >= 1.1D)
                AFRec = true;

            Text = "* ";

            if (PFRec)
                Text += string.Format("{0:F2}% of the files on this volume are fragmented. ", DefragReport.PercentFragged);

            if (AFRec)
                Text += string.Format("The average fragments per file ({0:F2}) indicates a high degree of fragmentation. ", DefragReport.AverageFragments);

            if (DefragReport.PercentFragged < 5.0D && DefragReport.AverageFragments < 1.1D)
                Text = "* No defragmentation is necessary at this point.";
            else if (DefragReport.PercentFragged < 15.0D && DefragReport.AverageFragments < 1.3D)
                Text += "It is recommended that you perform a Fast Defrag.";
            else
                Text += "It is recommended that you perform an Extensive Defrag.";

            // Should we recommend a smaller cluster size?
            if (DefragReport.PercentSlack >= 10.0f)
            {
                Text += string.Format(
                    "\n* A large amount of disk space ({0:F2}%) is being lost " + 
                    "due to a large (%{0} bytes) cluster size. It is recommended " + 
                    "that you use a disk utility such as Partition Magic to " + 
                    "reduce the cluster size of this volume.",
                    DefragReport.PercentSlack,
                    DefragReport.ClusterSize
                    );
            }

            Recommendations = Text;
        }
    }
}
