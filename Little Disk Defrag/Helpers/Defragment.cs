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

using Little_Disk_Defrag.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        public DefragMethod DefragType
        {
            get { return _method; }
        }

        /// <summary>
        /// Limit length of status string to 70 chars?
        /// </summary>
        public bool DoLimitLength
        {
            get { return this._doLimitLength; }
            set { this._doLimitLength = value; }
        }

        public bool IsDoneYet
        {
            get { return this.Done; }
        }

        public bool HasError
        {
            get { return this.Error; }
        }

        public bool IsActive
        {
            get {  return (this._driveVolume != null); }
        }

        public bool ShowReport
        {
            get { return this._showReport; }
            set { this._showReport = value; }
        }

        public string StatusString
        {
            get 
            {
                lock (this._lock)
                {
                    return this._statusString;
                }
            }
            set
            {
                lock (this._lock)
                {
                    this._statusString = value;
                }
            }
        }

        public double StatusPercent
        {
            get { return this._statusPercent; }
            set
            {
                if (value < 0 || value > 100)
                    return;

                this._statusPercent = value;
            }
        }

        public string DriveName
        {
            get { return this._driveName; }
        }

        public DefragReport Report
        { 
            get { return this._defragReport; }
        }
        public DriveVolume Volume
        {
            get { return this._driveVolume; }
        }

        public bool VolumeOpen { get; set; }

        public bool UpdateDrawing
        {
            get { return this._updateDrawing; }
            set { this._updateDrawing = value; }
        }

        public Defragment()
        {
            this._defragReport = new DefragReport();

            this.Reset();
            
            this._lastBMPUpdate = DateTime.Now;
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
            this._driveVolume = new DriveVolume();

            this._driveName = volume;

            if (!silent)
                this.StatusString = "Opening volume " + volume;

            if (!Volume.Open(volume))
            {
                if (!silent)
                {
                    this.StatusString = "Error opening volume " + volume;
                    this.StatusPercent = 100.0f;
                }

                Error = true;
                Done = true;

                this.VolumeOpen = false;

                return;
            }

            this.VolumeOpen = true;

            if (!silent)
                this.StatusString = "Getting volume bitmap";

            if (!Volume.GetBitmap())
            {
                if (!silent)
                    this.StatusString = "Could not get volume " + DriveName + " bitmap";

                Error = true;

                this.Close();

                return;
            }

            this.UpdateDrawing = true;
        }

        public void SetMethod(DefragMethod method)
        {
            this._method = method;
        }

        public void CloseVolume()
        {
            if (!this.VolumeOpen)
                return;

            if (this._driveVolume == null)
                return;

            this.Volume.Dispose();
        }

        public void Close()
        {
            if (!Done)
            {
                string OldStatus;

                OldStatus = this.StatusString;
                StatusPercent = 99.999999f;

                this.StatusString = "Closing volume " + DriveName;

                this.CloseVolume();

                StatusPercent = 100.0f;

                // If there was an error then the wstring has already been set
                if (Error)
                    this.StatusString = OldStatus;
                else if (PleaseStop)
                    this.StatusString = "Volume " + DriveName + " defragmentation was stopped";
                else
                    this.StatusString = "Finished defragmenting " + DriveName;

                this._driveName = "";

                if (!Error && !PleaseStop)
                    this.ShowReport = true; // causes report window to open

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
                    this.Close();
                    return;
                }

                // First thing: build a file list.
                this.StatusString = "Getting volume bitmap";
                if (!Volume.GetBitmap())
                {
                    this.StatusString = "Could not get volume " + DriveName + " bitmap";
                    Error = true;
                    this.Close();
                    return;
                }

                this.UpdateDrawing = true;

                this._lastBMPUpdate = DateTime.Now;

                if (PleaseStop)
                {
                    this.Close();
                    return;
                }

                this.StatusString = "Obtaining volume geometry";
                if (!Volume.ObtainInfo())
                {
                    this.StatusString = "Could not obtain volume " + DriveName + " geometry";
                    Error = true;
                    this.Close();
                    return;
                }

                if (PleaseStop)
                {
                    this.Close();
                    return;
                }

                this.StatusString = "Building file database for volume " + DriveName;
                if (!Volume.BuildFileList(this))
                {
                    this.StatusString = "Could not build file database for volume " + DriveName;
                    Error = true;
                    this.Close();
                    return;
                }

                if (PleaseStop)
                {
                    this.Close();
                    return;
                }

                this.StatusString = "Analyzing database for " + DriveName;
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
                    this.Close();
                    return;
                }

                // Analyze?
                if (this._method == DefragMethod.ANALYZE)
                {
                    uint j;

                    Report.RootPath = Volume.RootPath;
                    Report.Label = Volume.VolInfo.Name;
                    Report.Serial = Volume.VolInfo.Serial;
                    Report.FileSystem = Volume.VolInfo.FileSystem;
                    Report.FreeBytes = Volume.VolInfo.FreeBytes;
                    Report.ClusterCount = Volume.VolInfo.ClusterCount;
                    Report.ClusterSize = Volume.VolInfo.ClusterSize;

                    Report.FraggedFiles.Clear();
                    Report.UnfraggedFiles.Clear();
                    Report.UnmovableFiles.Clear();

                    Report.FilesCount = (ulong)Volume.DBFileCount - (ulong)Volume.DBDirCount;
                    Report.DirsCount = (ulong)Volume.DBDirCount;
                    Report.DiskSizeBytes = Volume.VolInfo.TotalBytes;

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

                        this.StatusString = Volume.GetDBDir(Info.DirIndice) + Info.Name;

                        Report.FilesSizeClusters += Info.Clusters;
                        Report.FilesSizeBytes += Info.Size;

                        if (Info.Attributes.Unmovable)
                            Report.UnmovableFiles.Add(j);

                        if (Info.Fragments.Count > 1)
                            Report.FraggedFiles.Add(j);
                        else
                            Report.UnfraggedFiles.Add(j);

                        StatusPercent = ((double)j / (double)Report.FilesCount) * 100.0f;
                    }

                    Report.FilesSizeOnDisk = Report.FilesSizeClusters * (UInt64)Volume.VolInfo.ClusterSize;
                    Report.FilesSlackBytes = Report.FilesSizeOnDisk - Report.FilesSizeBytes;
                    Report.PercentFragged = 100.0f * ((double)Report.FraggedFiles.Count / (double)Report.FilesCount);

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
                            this.StatusString = "Paused";
                            PleasePause = false;

                            while (PleasePause == false)
                            {
                                Thread.Sleep(50);
                            }

                            PleasePause = false;
                        }

                        if (PleaseStop)
                        {
                            this.StatusString = "Stopping";
                            break;
                        }

                        //
                        Info = Volume.GetDBFile(i);

                        PreviousClusters = ClustersProgress;
                        ClustersProgress += Info.Clusters;

                        if (!Info.Attributes.Process)
                            continue;

                        if (!DoLimitLength)
                            this.StatusString = Volume.GetDBDir(Info.DirIndice) + Info.Name;
                        else
                        {
                            PrintName = Utils.FitName(Volume.GetDBDir(Info.DirIndice), Info.Name, Width);
                            this.StatusString = PrintName;
                        }

                        // Calculate percentage complete
                        StatusPercent = 100.0f * (double)((double)PreviousClusters / (double)TotalClusters);

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
                        if (Info.Fragments.Count == 1 && this._method == DefragMethod.FASTDEFRAG)
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
                                if (this._method == DefragMethod.NORMDEFRAG && Info.Fragments.Count == 1 && TargetLCN > (ulong)Info.Fragments[0].StartLCN)
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
                            if (DateTime.Now.Subtract(this._lastBMPUpdate).TotalSeconds < 15000)
                                Retry = 1;
                            else
                            if (!Result  ||  Retry != 1)
                            {   // hmm. Wait for a moment, then update the drive bitmap
                                //SetStatusString (L"(Reobtaining volume " + DriveName + L" bitmap)");

                                if (!DoLimitLength)
                                {
                                    this.StatusString += " .";
                                }

                                if (Volume.GetBitmap ())
                                {
                                    this._lastBMPUpdate = DateTime.Now;

                                    if (!DoLimitLength)
                                        this.StatusString = Volume.GetDBDir (Info.DirIndice) + Info.Name;
                                    else
                                        this.StatusString = PrintName;

                                    Volume.FindFreeRange (0, 1, out FirstFreeLCN);
                                }
                                else
                                {
                                    this.StatusString = "Could not re-obtain volume " + DriveName + " bitmap";
                                    Error = true;
                                }
                            }

                            Retry--;
                        }

                        if (Error == true)
                            break;
                    }
                }
            }
            catch (System.Threading.ThreadAbortException)
            {

            }
            finally
            {
                this.Close();
            }
        }
        public void TogglePause()
        {
            lock (this._lock)
            {
                this.StatusString = "Pausing ...";
                this.PleasePause = true;
            }
        }
        public void Stop()
        {
            lock (this._lock)
            {
                this.StatusString = "Stopping ...";
                this.PleaseStop = true;
            }
        }

    }
}
