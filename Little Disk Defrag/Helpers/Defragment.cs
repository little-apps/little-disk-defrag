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
using System.Threading;
using Little_Disk_Defrag.Misc;

namespace Little_Disk_Defrag.Helpers
{
    public class Defragment
    {
        public enum DefragMethod { ANALYZE, FASTDEFRAG, NORMDEFRAG };

        private DateTime _lastBMPUpdate; // Last time volume bitmap was updated
        private DefragReport _defragReport;
        private bool _doLimitLength;
        private DefragMethod _method;
        private string _driveName;
        private DriveVolume _driveVolume;
        private string _statusString;
        private double _statusPercent;
        private bool _showReport;
        private bool _updateDrawing;

        private object _lock = new object();

        public bool Error;
        public bool Done;
        public bool PleaseStop;
        public bool PleasePause;

        public DefragMethod DefragType => _method;

        /// <summary>
        /// Limit length of status string to 70 chars?
        /// </summary>
        public bool DoLimitLength
        {
            get { return _doLimitLength; }
            set { _doLimitLength = value; }
        }

        public bool IsDoneYet => Done;

        public bool HasError => Error;

        public bool ShowReport
        {
            get { return _showReport; }
            set { _showReport = value; }
        }

        public string StatusString
        {
            get 
            {
                lock (_lock)
                {
                    return _statusString;
                }
            }
            set
            {
                lock (_lock)
                {
                    _statusString = value;
                }
            }
        }

        public double StatusPercent
        {
            get { return _statusPercent; }
            set
            {
                if (value < 0 || value > 100)
                    return;

                _statusPercent = value;
            }
        }

        public string DriveName => _driveName;

        public DefragReport Report => _defragReport;
        public DriveVolume Volume => _driveVolume;

        public bool VolumeOpen { get; set; }

        public bool UpdateDrawing
        {
            get { return _updateDrawing; }
            set { _updateDrawing = value; }
        }

        public Defragment()
        {
            _defragReport = new DefragReport();

            Reset();
            
            _lastBMPUpdate = DateTime.Now;
        }

        public void Reset()
        {
            DoLimitLength = true;
            Error = false;
            Done = false;
            PleaseStop = false;
            PleasePause = false;

            StatusPercent = 0;
        }

        public void Open(string volume, bool silent = false)
        {
            _driveVolume = new DriveVolume();

            _driveName = volume;

            if (!silent)
                StatusString = "Opening volume " + volume;

            if (!Volume.Open(volume))
            {
                if (!silent)
                {
                    StatusString = "Error opening volume " + volume;
                    StatusPercent = 100.0f;
                }

                Error = true;
                Done = true;

                VolumeOpen = false;

                return;
            }

            VolumeOpen = true;

            if (!silent)
                StatusString = "Getting volume bitmap";

            if (!Volume.GetBitmap())
            {
                if (!silent)
                    StatusString = "Could not get volume " + DriveName + " bitmap";

                Error = true;

                Close();

                return;
            }

            UpdateDrawing = true;
        }

        public void SetMethod(DefragMethod method)
        {
            _method = method;
        }

        public void CloseVolume()
        {
            if (!VolumeOpen)
                return;

            if (_driveVolume == null)
                return;

            Volume.Dispose();
        }

        public void Close()
        {
            if (!Done)
            {
                string OldStatus;

                OldStatus = StatusString;
                StatusPercent = 99.999999f;

                StatusString = "Closing volume " + DriveName;

                CloseVolume();

                StatusPercent = 100.0f;

                // If there was an error then the wstring has already been set
                if (Error)
                    StatusString = OldStatus;
                else if (PleaseStop)
                    StatusString = "Volume " + DriveName + " defragmentation was stopped";
                else
                    StatusString = "Finished defragmenting " + DriveName;

                _driveName = "";

                if (!Error && !PleaseStop)
                    ShowReport = true; // causes report window to open

                Done = true;
            }
        }

        public void Start() {
            uint i;
            UInt64 FirstFreeLCN = 0;
            UInt64 TotalClusters;
            UInt64 ClustersProgress;
            string PrintName = string.Empty;
            int Width = 70;

            try
            {
                if (Error) 
                {
                    Close();
                    return;
                }

                // First thing: build a file list.
                StatusString = "Getting volume bitmap";
                if (!Volume.GetBitmap())
                {
                    StatusString = "Could not get volume " + DriveName + " bitmap";
                    Error = true;
                    Close();
                    return;
                }

                UpdateDrawing = true;

                _lastBMPUpdate = DateTime.Now;

                if (PleaseStop)
                {
                    Close();
                    return;
                }

                StatusString = "Obtaining volume geometry";
                if (!Volume.PartInfo.GetPartitionInfo())
                {
                    StatusString = "Could not obtain volume " + DriveName + " geometry";
                    Error = true;

                    Close();

                    return;
                }

                if (PleaseStop)
                {
                    Close();
                    return;
                }

                StatusString = "Obtaining partition information";
                if (!Volume.PartInfo.GetPartitionDetails())
                {
                    StatusString = "Could not obtain partition " + DriveName + " information";
                    Error = true;

                    Close();

                    return;
                }

                if (PleaseStop)
                {
                    Close();
                    return;
                }

                StatusString = "Building file database for volume " + DriveName;
                if (!Volume.BuildFileList(this))
                {
                    StatusString = "Could not build file database for volume " + DriveName;
                    Error = true;
                    Close();
                    return;
                }

                if (PleaseStop)
                {
                    Close();
                    return;
                }

                StatusString = "Analyzing database for " + DriveName;
                TotalClusters = 0;
                for (i = 0; i < Volume.DBFileCount; i++)
                {
                    TotalClusters += Volume.GetDBFile(i).Clusters;
                }

                // Defragment!
                ClustersProgress = 0;

                // Find first free LCN for speedier searches ...
                Volume.FindFreeRange(0, 1, out FirstFreeLCN);

                if (PleaseStop)
                {
                    Close();
                    return;
                }

                // Analyze?
                if (_method == DefragMethod.ANALYZE)
                {
                    uint j;

                    Report.RootPath = Volume.RootPath;
                    Report.Label = Volume.PartInfo.Name;
                    Report.Serial = Volume.PartInfo.Serial;
                    Report.FileSystem = Volume.PartInfo.FileSystem;
                    Report.FreeBytes = Volume.PartInfo.FreeBytes;
                    Report.ClusterCount = Volume.PartInfo.ClusterCount;
                    Report.ClusterSize = Volume.PartInfo.ClusterSize;

                    Report.FraggedFiles.Clear();
                    Report.UnfraggedFiles.Clear();
                    Report.UnmovableFiles.Clear();

                    Report.FilesCount = (ulong)Volume.DBFileCount - (ulong)Volume.DBDirCount;
                    Report.DirsCount = (ulong)Volume.DBDirCount;
                    Report.DiskSizeBytes = Volume.PartInfo.TotalBytes;

                    Report.FilesSizeClusters = 0;
                    Report.FilesSlackBytes = 0;
                    Report.FilesSizeBytes = 0;
                    Report.FilesFragments = 0;

                    for (j = 0; j < Volume.DBFileCount; j++)
                    {
                        FileInfo Info;

                        Info = Volume.GetDBFile(j);

                        Report.FilesFragments += (uint)Utils.Max(1UL, (ulong)Info.Fragments.Count); // add 1 fragment even for 0 bytes/0 cluster files

                        if (!Info.Attributes.Process)
                            continue;

                        StatusString = Volume.GetDBDir(Info.DirIndice) + Info.Name;

                        Report.FilesSizeClusters += Info.Clusters;
                        Report.FilesSizeBytes += Info.Size;

                        if (Info.Attributes.Unmovable)
                            Report.UnmovableFiles.Add(j);

                        if (Info.Fragments.Count > 1)
                            Report.FraggedFiles.Add(j);
                        else
                            Report.UnfraggedFiles.Add(j);

                        StatusPercent = (j / (double)Report.FilesCount) * 100.0f;
                    }

                    Report.FilesSizeOnDisk = Report.FilesSizeClusters * Volume.PartInfo.ClusterSize;
                    Report.FilesSlackBytes = Report.FilesSizeOnDisk - Report.FilesSizeBytes;
                    Report.PercentFragged = 100.0f * (Report.FraggedFiles.Count / (double)Report.FilesCount);

                    UInt64 Percent;
                    Percent = (10000 * Report.FilesSlackBytes) / Report.FilesSizeOnDisk;
                    Report.PercentSlack = (double)Percent / 100.0f;
                }
                else
                {
                    // Go through all the files and ... defragment them!
                    for (i = 0; i < Volume.DBFileCount; i++)
                    {
                        FileInfo Info;
                        bool Result;
                        UInt64 TargetLCN = 0;
                        UInt64 PreviousClusters;

                        // What? They want us to pause? Oh ok.
                        if (PleasePause)
                        {
                            StatusString = "Paused";
                            PleasePause = false;

                            while (PleasePause == false)
                            {
                                Thread.Sleep(50);
                            }

                            PleasePause = false;
                        }

                        if (PleaseStop)
                        {
                            StatusString = "Stopping";
                            break;
                        }

                        //
                        Info = Volume.GetDBFile(i);

                        PreviousClusters = ClustersProgress;
                        ClustersProgress += Info.Clusters;

                        if (!Info.Attributes.Process)
                            continue;

                        if (!DoLimitLength)
                            StatusString = Volume.GetDBDir(Info.DirIndice) + Info.Name;
                        else
                        {
                            PrintName = Utils.FitName(Volume.GetDBDir(Info.DirIndice), Info.Name, Width);
                            StatusString = PrintName;
                        }

                        // Calculate percentage complete
                        StatusPercent = 100.0f * (PreviousClusters / (double)TotalClusters);

                        // Can't defrag directories yet
                        if (Info.Attributes.Directory)
                            continue;

                        // Can't defrag 0 byte files :)
                        if (Info.Fragments.Count == 0)
                            continue;

                        // If doing fast defrag, skip non-fragmented files
                        // Note: This assumes that the extents stored in Info.Fragments
                        //       are consolidated. I.e. we assume it is NOT the case that
                        //       two extents account for a sequential range of (non-
                        //       fragmented) clusters.
                        if (Info.Fragments.Count == 1 && _method == DefragMethod.FASTDEFRAG)
                            continue;

                        // Otherwise, defrag0rize it!
                        int Retry = 3;  // retry a few times
                        while (Retry > 0)
                        {
                            // Find a place that can fit the file
                            Result = Volume.FindFreeRange(FirstFreeLCN, Info.Clusters, out TargetLCN);

                            // If yes, try moving it
                            if (Result)
                            {
                                // If we're doing an extensive defrag and the file is already defragmented
                                // and if its new location would be after its current location, don't
                                // move it.
                                if (_method == DefragMethod.NORMDEFRAG && Info.Fragments.Count == 1 && TargetLCN > Info.Fragments[0].StartLCN)
                                {
                                    Retry = 1;
                                }
                                else
                                {
                                    if (Volume.MoveFileDumb((int)i, TargetLCN))
                                    {
                                        Retry = 1; // yay, all done with this file.
                                        Volume.FindFreeRange(0, 1, out FirstFreeLCN);
                                    }
                                }
                            }

                            // Only update bitmap if it's older than 15 seconds
                            if (DateTime.Now.Subtract(_lastBMPUpdate).TotalSeconds < 15000)
                                Retry = 1;
                            else
                            if (!Result  ||  Retry != 1)
                            {   // hmm. Wait for a moment, then update the drive bitmap
                                //SetStatusString (L"(Reobtaining volume " + DriveName + L" bitmap)");

                                if (!DoLimitLength)
                                {
                                    StatusString += " .";
                                }

                                if (Volume.GetBitmap ())
                                {
                                    _lastBMPUpdate = DateTime.Now;

                                    if (!DoLimitLength)
                                        StatusString = Volume.GetDBDir (Info.DirIndice) + Info.Name;
                                    else
                                        StatusString = PrintName;

                                    Volume.FindFreeRange (0, 1, out FirstFreeLCN);
                                }
                                else
                                {
                                    StatusString = "Could not re-obtain volume " + DriveName + " bitmap";
                                    Error = true;
                                }
                            }

                            Retry--;
                        }

                        if (Error)
                            break;
                    }
                }
            }
            catch (ThreadAbortException)
            {

            }
            finally
            {
                Close();
            }
        }
        public void TogglePause()
        {
            lock (_lock)
            {
                StatusString = "Pausing ...";
                PleasePause = true;
            }
        }
        public void Stop()
        {
            lock (_lock)
            {
                StatusString = "Stopping ...";
                PleaseStop = true;
            }
        }

    }
}
