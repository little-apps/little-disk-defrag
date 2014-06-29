using Little_Disk_Defrag.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Little_Disk_Defrag.Helpers.Partitions
{
    public abstract class PartInfo
    {
        protected DriveVolume _vol;

        private string _name;
        private string _serial;
        private ulong _maxNameLen;
        private string _fileSystem;
        private ulong _clusterCount;
        private uint _clusterSize;
        private ulong _totalBytes;
        private ulong _freeBytes;
        private uint _sectorsPerCluster;
        private ulong _bytesPerCluster;
        private uint _bytesPerSector;
        private uint _freeClusters;
        private uint _totalSectors;
        private uint _totalClusters;

        public string Name
        {
            get { return this._name; }
            set { this._name = value; }
        }
        public string Serial
        {
            get { return this._serial; }
            set { this._serial = value; }
        }
        public ulong MaxNameLen
        {
            get { return this._maxNameLen; }
            set { this._maxNameLen = value; }
        }
        public string FileSystem
        {
            get { return this._fileSystem; }
            set { this._fileSystem = value; }
        }
        public UInt64 ClusterCount
        {
            get { return this._clusterCount; }
            set { this._clusterCount = value; }
        }
        public uint ClusterSize
        {
            get { return this._clusterSize; }
            set { this._clusterSize = value; }
        }
        public UInt64 TotalBytes
        {
            get { return this._totalBytes; }
            set { this._totalBytes = value; }
        }
        public UInt64 FreeBytes
        {
            get { return this._freeBytes; }
            set { this._freeBytes = value; }
        }

        public uint SectorsPerCluster
        {
            get { return this._sectorsPerCluster; }
            set { this._sectorsPerCluster = value; }
        }
        public ulong BytesPerCluster
        {
            get { return this._bytesPerCluster; }
            set { this._bytesPerCluster = value; }
        }
        public uint BytesPerSector
        {
            get { return this._bytesPerSector; }
            set { this._bytesPerSector = value; }
        }
        public uint FreeClusters
        {
            get { return this._freeClusters; }
            set { this._freeClusters = value; }
        }
        public uint TotalSectors
        {
            get { return this._totalSectors; }
            set { this._totalSectors = value; }
        }

        public uint TotalClusters
        {
            get { return this._totalClusters; }
            set { this._totalClusters = value; }
        }

        public DriveVolume Volume
        {
            get { return this._vol; }
        }

        public PartInfo(DriveVolume volume)
        {
            this._vol = volume;
        }

        public bool GetPartitionInfo()
        {
            StringBuilder VolName = new StringBuilder(64);
            uint VolSN;
            uint VolMaxFileLen;
            PInvoke.FileSystemFeature FSFlags;
            StringBuilder FSName = new StringBuilder(64);

            bool Result;
            uint BytesGot;
            UInt64 nan;

            Result = PInvoke.GetVolumeInformation(this.Volume.RootPath, VolName, VolName.Capacity, out VolSN, out VolMaxFileLen, out FSFlags, FSName, FSName.Capacity);

            if (Result)
            {
                this.FileSystem = FSName.ToString();
                this.MaxNameLen = VolMaxFileLen;
                this.Name = VolName.ToString();
                this.Serial = string.Format("{0:X}-{1:X}", (VolSN & 0xffff0000) >> 16, VolSN & 0x0000ffff);
            }
            else
            {
                this.FileSystem = "(Unknown)";
                this.MaxNameLen = 255;
                this.Name = "(Unknown)";
                this.Serial = "(Unknown)";
            }

            int GeometrySize = Marshal.SizeOf(typeof(PInvoke.DISK_GEOMETRY));
            IntPtr GeometryPtr = Marshal.AllocHGlobal(GeometrySize);

            BytesGot = 0;
            Result = PInvoke.DeviceIoControl(
                this.Volume.Handle,
                PInvoke.FSConstants.IOCTL_DISK_GET_DRIVE_GEOMETRY,
                IntPtr.Zero,
                0,
                GeometryPtr,
                (uint)GeometrySize,
                ref BytesGot,
                IntPtr.Zero
            );

            // Call failed? Aww :(
            if (!Result)
                return false;

            this.Volume.Geometry = (PInvoke.DISK_GEOMETRY)Marshal.PtrToStructure(GeometryPtr, typeof(PInvoke.DISK_GEOMETRY));

            uint SectorsPerCluster;
            uint BytesPerSector;
            uint FreeClusters;
            uint TotalClusters;

            Result = PInvoke.GetDiskFreeSpace(
                this.Volume.RootPath,
                out SectorsPerCluster,
                out BytesPerSector,
                out FreeClusters,
                out TotalClusters
            );

            this.SectorsPerCluster = SectorsPerCluster;
            this.BytesPerSector = BytesPerSector;
            this.FreeClusters = FreeClusters;
            this.TotalClusters = TotalClusters;

            // Failed? Weird.
            if (!Result)
                return (false);

            this.ClusterSize = SectorsPerCluster * BytesPerSector;

            ulong totalBytes, freeBytes;

            Result = PInvoke.GetDiskFreeSpaceEx(
                this.Volume.RootPath,
                out nan,
                out totalBytes,
                out freeBytes
            );

            this.TotalBytes = totalBytes;
            this.FreeBytes = freeBytes;

            return true;
        }

        public abstract bool GetPartitionDetails();
    }
}
