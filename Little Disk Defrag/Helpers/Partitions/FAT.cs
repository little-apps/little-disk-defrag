using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Little_Disk_Defrag.Misc;

namespace Little_Disk_Defrag.Helpers.Partitions
{
    public class FAT : PartInfo
    {
        public enum FATTypes { FAT12, FAT16, FAT32 };

        public FATTypes Type { get; set; }

        public ulong FirstDataSector { get; set; }

        public ulong DataSector { get; set; }

        public ulong RootDirSectors { get; set; }

        public FAT(DriveVolume volume) 
            : base(volume)
        {

        }

        public override bool GetPartitionDetails()
        {
            uint BytesRead;
            ulong FATSize;

            IntPtr BootSectorPtr = Marshal.AllocHGlobal(512);

            NativeOverlapped Overlapped = new NativeOverlapped();

            var result = PInvoke.ReadFile(Volume.Handle, BootSectorPtr, 512, out BytesRead, ref Overlapped);

            if (!result)
            {
                Debug.WriteLine("Error #{0} occurred trying to read volume {1}", Marshal.GetLastWin32Error(), Volume.RootPath);
                return false;
            }

            var BootSector = new PInvoke.FATBootSector(BootSectorPtr);

            if (BootSector.Signature != 0xAA55 || (((BootSector.BS_jmpBoot[0] != 0xEB) || (BootSector.BS_jmpBoot[2] != 0x90)) && (BootSector.BS_jmpBoot[0] != 0xE9)))
            {
                Debug.WriteLine("Volume is not a valid FAT partition");
                return false;
            }

            // Fetch values from the bootblock and determine what FAT this is, FAT12, FAT16, or FAT32.
            BytesPerSector = BootSector.BytesPerSector;
            if (BytesPerSector == 0) {
	            Debug.WriteLine("This is not a FAT disk (BytesPerSector is zero).");
	            return false;
            }

            SectorsPerCluster = BootSector.SectorsPerCluster;
            if (SectorsPerCluster == 0) {
                Debug.WriteLine("This is not a FAT disk (SectorsPerCluster is zero).");
	            return false;
            }

            TotalSectors = BootSector.TotalSectors16;
            if (TotalSectors == 0)
	            TotalSectors = BootSector.TotalSectors32;

            RootDirSectors = (ulong)((BootSector.RootEntries * 32) + (BootSector.BytesPerSector - 1)) / BootSector.BytesPerSector;

            uint SectorsPerFAT = BootSector.SectorsPerFAT;

            if (SectorsPerFAT == 0)
                SectorsPerFAT = BootSector.FAT1632Info.FAT32_SectorsPerFAT32;

            FirstDataSector = BootSector.ReservedSectors + (BootSector.NumberOfFATs * SectorsPerFAT) + RootDirSectors;
            DataSector = TotalSectors - (BootSector.ReservedSectors + (BootSector.NumberOfFATs * SectorsPerFAT) + RootDirSectors);
            ClusterCount = DataSector / BootSector.SectorsPerCluster;

            if (ClusterCount < 4085) {
		        Type = FATTypes.FAT12;

                Debug.WriteLine("This is a FAT12 disk.");
	        }
	        else if (ClusterCount < 65525) {
		        Type = FATTypes.FAT16;

		        Debug.WriteLine("This is a FAT16 disk.");
	        }
	        else {
		        Type = FATTypes.FAT32;

		        Debug.WriteLine("This is a FAT32 disk.");
	        }

            BytesPerCluster = BytesPerSector * SectorsPerCluster;

            TotalClusters = (uint)ClusterCount;

            Debug.WriteLine("  OEMName: {0}", BootSector.BS_OEMName);
	        Debug.WriteLine("  BytesPerSector: {0:d}", BytesPerSector);
            Debug.WriteLine("  TotalSectors: {0:d}", TotalSectors);
            Debug.WriteLine("  SectorsPerCluster: {0:d}", SectorsPerCluster);
            Debug.WriteLine("  RootDirSectors: {0:d}", RootDirSectors);
            Debug.WriteLine("  FATSz: {0:d}", SectorsPerFAT);
            Debug.WriteLine("  FirstDataSector: {0:d}", FirstDataSector);
            Debug.WriteLine("  DataSec: {0:d}", DataSector);
            Debug.WriteLine("  CountofClusters: {0:d}", ClusterCount);
            Debug.WriteLine("  ReservedSectors: {0:d}", BootSector.ReservedSectors);
            Debug.WriteLine("  NumberFATs: {0:d}", BootSector.NumberOfFATs);
            Debug.WriteLine("  RootEntriesCount: {0:d}", BootSector.RootEntries);
	        Debug.WriteLine("  MediaType: {0:X}", BootSector.MediaDescriptor);
            Debug.WriteLine("  SectorsPerTrack: {0:d}", BootSector.SectorsPerTrack);
            Debug.WriteLine("  NumberOfHeads: {0:d}", BootSector.Heads);
            Debug.WriteLine("  HiddenSectors: {0:d}", BootSector.HiddenSectors);
	        if (Type != FATTypes.FAT32) {
                Debug.WriteLine("  BS_DrvNum: {0:d}", BootSector.FAT1632Info.FAT16_LogicalDriveNumber);
                Debug.WriteLine("  BS_BootSig: {0:d}", BootSector.FAT1632Info.FAT16_ExtendedSignature);
                Debug.WriteLine("  BS_VolID: {0:d}", BootSector.FAT1632Info.FAT16_PartitionSerialNumber);
                Debug.WriteLine("  VolLab: {0}", BootSector.FAT1632Info.FAT16_VolumeName);
                Debug.WriteLine("  FilSysType: {0}", BootSector.FAT1632Info.FAT16_FSType);
	        }
	        else {
                Debug.WriteLine("  FATSz32: {0:d}", BootSector.FAT1632Info.FAT32_SectorsPerFAT32);
                Debug.WriteLine("  ExtFlags: {0:d}", BootSector.FAT1632Info.FAT32_ExtFlags);
                Debug.WriteLine("  FSVer: {0:d}", BootSector.FAT1632Info.FAT32_FSVer);
                Debug.WriteLine("  RootClus: {0:d}", BootSector.FAT1632Info.FAT32_RootDirStart);
                Debug.WriteLine("  FSInfo: {0:d}", BootSector.FAT1632Info.FAT32_FSInfoSector);
                Debug.WriteLine("  BkBootSec: {0:d}", BootSector.FAT1632Info.FAT32_BackupBootSector);
                Debug.WriteLine("  DrvNum: {0:d}", BootSector.FAT1632Info.FAT32_LogicalDriveNumber);
                Debug.WriteLine("  BootSig: {0:d}", BootSector.FAT1632Info.FAT32_ExtendedSignature);
                Debug.WriteLine("  VolID: {0:d}", BootSector.FAT1632Info.FAT32_PartitionSerialNumber);
                Debug.WriteLine("  VolLab: {0}", BootSector.FAT1632Info.FAT32_VolumeName);
                Debug.WriteLine("  FilSysType: {0}", BootSector.FAT1632Info.FAT32_FSType);
	        }

            switch (Type)
            {
                case FATTypes.FAT12:
                    {
                        FATSize = ClusterCount + 1; 
                        break;
                    }

                case FATTypes.FAT16:
                    {
                        FATSize = (ClusterCount + 1) * 2; 
                        break;
                    }

                case FATTypes.FAT32:
                    {
                        FATSize = (ClusterCount + 1) * 4; 
                        break;
                    }
                default:
                    {
                        FATSize = 0;
                        break;
                    }
            }

            if (FATSize % BytesPerSector > 0)
                FATSize = FATSize + BytesPerSector - FATSize % BytesPerSector;

            PInvoke.LARGE_INTEGER Trans = new PInvoke.LARGE_INTEGER { QuadPart = BootSector.ReservedSectors * BytesPerSector };

            IntPtr FATDataPtr = Marshal.AllocHGlobal((int)FATSize);

            NativeOverlapped nativeOverlapped = new NativeOverlapped
            {
                EventHandle = IntPtr.Zero,
                OffsetLow = (int) Trans.LowPart,
                OffsetHigh = Trans.HighPart
            };

            PInvoke.ReadFile(Volume.Handle, FATDataPtr, (uint)FATSize, out BytesRead, ref nativeOverlapped);

            PInvoke.FATData FatData = new PInvoke.FATData(FATDataPtr, Type, FATSize);

            return true;
        }

        private byte[] LoadDir(PInvoke.FATData fatData, ulong startCluster, out ulong outLen)
        {
            ulong bufLen = 0;
            ulong i;
            ulong cluster = startCluster;


            outLen = 0;

            if (startCluster == 0)
                return new byte[0];

            for (i = 0; i < ClusterCount + 1; i++)
            {
                // Have we reached the end of the clusters?
                if (((Type == FATTypes.FAT12) && (cluster >= 0xFF8)) ||
                    ((Type == FATTypes.FAT16) && (cluster >= 0xFFF8)) ||
                    ((Type == FATTypes.FAT32) && (cluster >= 0xFFFFFF8)))
                    break;

                if ((cluster < 2) || (cluster > ClusterCount + 1))
                    return new byte[0];

                bufLen = bufLen + SectorsPerCluster * BytesPerSector;

                switch (Type)
                {
                    case FATTypes.FAT12:
                        {
                            if ((cluster & 1) == 1)
                                cluster = (ulong)fatData.FAT12[cluster] >> 4;
                            else
                                cluster = (ulong)fatData.FAT12[cluster] & 0xFFF;

                            break;
                        }
                    case FATTypes.FAT16:
                        {
                            cluster = fatData.FAT16[cluster];
                            break;
                        }
                    case FATTypes.FAT32:
                        {
                            cluster = (ulong)fatData.FAT32[cluster] & 0xFFFFFFF;
                            break;
                        }
                }
            }

            if (i >= ClusterCount + 1)
            {
                Debug.WriteLine("Reached end of cluster count. This could mean the disk is corrupt.");
                return new byte[0];
            }

            if (bufLen > uint.MaxValue)
            {
                Debug.WriteLine("The directory is {0} bytes, which is too big", bufLen);
                return new byte[0];
            }

            return new byte[0];
        }
    }
}
