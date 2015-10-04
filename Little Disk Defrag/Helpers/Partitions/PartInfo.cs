using System;
using System.Runtime.InteropServices;
using System.Text;
using Little_Disk_Defrag.Misc;

namespace Little_Disk_Defrag.Helpers.Partitions
{
    public abstract class PartInfo
    {
        protected DriveVolume _vol;

        public string Name { get; set; }

        public string Serial { get; set; }

        public ulong MaxNameLen { get; set; }

        public string FileSystem { get; set; }

        public UInt64 ClusterCount { get; set; }

        public uint ClusterSize { get; set; }

        public UInt64 TotalBytes { get; set; }

        public UInt64 FreeBytes { get; set; }

        public uint SectorsPerCluster { get; set; }

        public ulong BytesPerCluster { get; set; }

        public uint BytesPerSector { get; set; }

        public uint FreeClusters { get; set; }

        public uint TotalSectors { get; set; }

        public uint TotalClusters { get; set; }

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
