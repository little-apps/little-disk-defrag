using System;
using System.Runtime.InteropServices;
using System.Text;
using Little_Disk_Defrag.Misc;

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
            get { return _name; }
            set { _name = value; }
        }
        public string Serial
        {
            get { return _serial; }
            set { _serial = value; }
        }
        public ulong MaxNameLen
        {
            get { return _maxNameLen; }
            set { _maxNameLen = value; }
        }
        public string FileSystem
        {
            get { return _fileSystem; }
            set { _fileSystem = value; }
        }
        public UInt64 ClusterCount
        {
            get { return _clusterCount; }
            set { _clusterCount = value; }
        }
        public uint ClusterSize
        {
            get { return _clusterSize; }
            set { _clusterSize = value; }
        }
        public UInt64 TotalBytes
        {
            get { return _totalBytes; }
            set { _totalBytes = value; }
        }
        public UInt64 FreeBytes
        {
            get { return _freeBytes; }
            set { _freeBytes = value; }
        }

        public uint SectorsPerCluster
        {
            get { return _sectorsPerCluster; }
            set { _sectorsPerCluster = value; }
        }
        public ulong BytesPerCluster
        {
            get { return _bytesPerCluster; }
            set { _bytesPerCluster = value; }
        }
        public uint BytesPerSector
        {
            get { return _bytesPerSector; }
            set { _bytesPerSector = value; }
        }
        public uint FreeClusters
        {
            get { return _freeClusters; }
            set { _freeClusters = value; }
        }
        public uint TotalSectors
        {
            get { return _totalSectors; }
            set { _totalSectors = value; }
        }

        public uint TotalClusters
        {
            get { return _totalClusters; }
            set { _totalClusters = value; }
        }

        public DriveVolume Volume => _vol;

        public PartInfo(DriveVolume volume)
        {
            _vol = volume;
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

            Result = PInvoke.GetVolumeInformation(Volume.RootPath, VolName, VolName.Capacity, out VolSN, out VolMaxFileLen, out FSFlags, FSName, FSName.Capacity);

            if (Result)
            {
                FileSystem = FSName.ToString();
                MaxNameLen = VolMaxFileLen;
                Name = VolName.ToString();
                Serial = string.Format("{0:X}-{1:X}", (VolSN & 0xffff0000) >> 16, VolSN & 0x0000ffff);
            }
            else
            {
                FileSystem = "(Unknown)";
                MaxNameLen = 255;
                Name = "(Unknown)";
                Serial = "(Unknown)";
            }

            int GeometrySize = Marshal.SizeOf(typeof(PInvoke.DISK_GEOMETRY));
            IntPtr GeometryPtr = Marshal.AllocHGlobal(GeometrySize);

            BytesGot = 0;
            Result = PInvoke.DeviceIoControl(
                Volume.Handle,
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

            Volume.Geometry = (PInvoke.DISK_GEOMETRY)Marshal.PtrToStructure(GeometryPtr, typeof(PInvoke.DISK_GEOMETRY));

            uint SectorsPerCluster;
            uint BytesPerSector;
            uint FreeClusters;
            uint TotalClusters;

            Result = PInvoke.GetDiskFreeSpace(
                Volume.RootPath,
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

            ClusterSize = SectorsPerCluster * BytesPerSector;

            ulong totalBytes, freeBytes;

            Result = PInvoke.GetDiskFreeSpaceEx(
                Volume.RootPath,
                out nan,
                out totalBytes,
                out freeBytes
            );

            TotalBytes = totalBytes;
            FreeBytes = freeBytes;

            return true;
        }

        public abstract bool GetPartitionDetails();
    }
}
