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
using Little_Disk_Defrag.Helpers;

namespace Little_Disk_Defrag
{
    /// <summary>
    /// Interaction logic for Report.xaml
    /// </summary>
    public partial class Report : INotifyPropertyChanged
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
                DiskSize = $"{DefragReport.DiskSizeBytes/(BytesDivisor/1024D)/1024.0D:F2} {SelectedUnit}";
            else
                DiskSize = $"{DefragReport.DiskSizeBytes/BytesDivisor:N0}" + " " + SelectedUnit;

            // DiskFreeBytes
            if (Fractional)
                FreeSpace = $"{DefragReport.FreeBytes/(BytesDivisor/1024D)/1024.0D:F2} {SelectedUnit}";
            else
                FreeSpace = $"{DefragReport.FreeBytes/BytesDivisor:N0}" + " " + SelectedUnit;

            // DiskSizeClusters
            TotalClusters = $"{DefragReport.ClusterCount:N0} clusters";

            // DiskClusterSize
            ClusterSize = $"{DefragReport.ClusterSize} bytes";

            // DirsCount
            DirCount = $"{DefragReport.DirsCount:N0}";

            // FilesCount
            FilesCount = $"{DefragReport.FilesCount:N0}";

            // FilesFragged
            Fragmented = $"{DefragReport.PercentFragged:F2}% {DefragReport.FraggedFiles.Count:N0}";

            // Average Frags
            AvgFragments = $"{DefragReport.AverageFragments:F2}";

            // FilesSizeBytes
            TotalSize = Fractional ? $"{DefragReport.FilesSizeBytes/(BytesDivisor/1024D)/1024.0D:F2} {SelectedUnit}"
                : $"{DefragReport.FilesSizeBytes/BytesDivisor:N0} {SelectedUnit}";

            // Files SizeOnDisk
            DiskSize = Fractional ?
                $"{(DefragReport.FilesSizeBytes + DefragReport.FilesSlackBytes)/(BytesDivisor/1024D)/1024.0D:F2} {SelectedUnit}"
                : $"{(DefragReport.FilesSizeBytes + DefragReport.FilesSlackBytes)/BytesDivisor:N0} {SelectedUnit}";

            // FilesSlackBytes
            WastedSlack = Fractional ? string.Format("({2:F2}%) {0:F2} {1}", DefragReport.FilesSlackBytes / (BytesDivisor / 1024D) / 1024.0D, SelectedUnit, DefragReport.PercentSlack) : string.Format("({2:F2}%) {0:N0} {1}", DefragReport.FilesSlackBytes / BytesDivisor, SelectedUnit, DefragReport.PercentSlack);

            // Recommendation
            bool PFRec = false; // Recommend based off percent fragged files?
            bool AFRec = false; // Recommend based off average fragments per file?

            if (DefragReport.PercentFragged >= 5.0D)
                PFRec = true;

            if (DefragReport.AverageFragments >= 1.1D)
                AFRec = true;

            var Text = "* ";

            if (PFRec)
                Text += $"{DefragReport.PercentFragged:F2}% of the files on this volume are fragmented. ";

            if (AFRec)
                Text +=
                    $"The average fragments per file ({DefragReport.AverageFragments:F2}) indicates a high degree of fragmentation. ";

            if (DefragReport.PercentFragged < 5.0D && DefragReport.AverageFragments < 1.1D)
                Text = "* No defragmentation is necessary at this point.";
            else if (DefragReport.PercentFragged < 15.0D && DefragReport.AverageFragments < 1.3D)
                Text += "It is recommended that you perform a Fast Defrag.";
            else
                Text += "It is recommended that you perform an Extensive Defrag.";

            // Should we recommend a smaller cluster size?
            if (DefragReport.PercentSlack >= 10.0f)
            {
                Text += $"\n* A large amount of disk space ({DefragReport.PercentSlack:F2}%) is being lost " +
                        $"due to a large (%{DefragReport.ClusterSize} bytes) cluster size. It is recommended " +
                        "that you use a disk utility such as Partition Magic to " +
                        "reduce the cluster size of this volume.";
            }

            Recommendations = Text;
        }
    }
}
