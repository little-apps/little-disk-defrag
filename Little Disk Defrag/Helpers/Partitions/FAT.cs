using Little_Disk_Defrag.Misc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Little_Disk_Defrag.Helpers.Partitions
{
    public class FAT : PartInfo
    {
        public enum FATTypes { FAT12, FAT16, FAT32 };

        private FATTypes _fatType;
        private ulong _firstDataSector;
        private ulong _dataSector;
        private ulong _rootDirSectors;

        public FATTypes Type
        {
            get { return this._fatType; }
            set { this._fatType = value; }
        }

        public ulong FirstDataSector
        {
            get { return this._firstDataSector; }
            set { this._firstDataSector = value; }
        }

        public ulong DataSector
        {
            get { return this._dataSector; }
            set { this._dataSector = value; }
        }

        public ulong RootDirSectors
        {
            get { return this._rootDirSectors; }
            set { this._rootDirSectors = value; }
        }

        public FAT(DriveVolume volume) 
            : base(volume)
        {

        }

        public override bool GetPartitionDetails()
        {
            uint BytesRead;
            ulong FATSize;
            bool result;

            IntPtr BootSectorPtr = Marshal.AllocHGlobal(512);
            PInvoke.FATBootSector BootSector;

            System.Threading.NativeOverlapped Overlapped = new System.Threading.NativeOverlapped();

            result = PInvoke.ReadFile(this.Volume.Handle, BootSectorPtr, (uint)512, out BytesRead, ref Overlapped);

            if (!result)
            {
                Debug.WriteLine("Error #{0} occurred trying to read volume {1}", Marshal.GetLastWin32Error(), this.Volume.RootPath);
                return false;
            }

            BootSector = new PInvoke.FATBootSector(BootSectorPtr);

            if (BootSector.Signature != 0xAA55 || (((BootSector.BS_jmpBoot[0] != 0xEB) || (BootSector.BS_jmpBoot[2] != 0x90)) && (BootSector.BS_jmpBoot[0] != 0xE9)))
            {
                Debug.WriteLine("Volume is not a valid FAT partition");
                return false;
            }

            // Fetch values from the bootblock and determine what FAT this is, FAT12, FAT16, or FAT32.
            this.BytesPerSector = BootSector.BytesPerSector;
            if (this.BytesPerSector == 0) {
	            Debug.WriteLine("This is not a FAT disk (BytesPerSector is zero).");
	            return false;
            }

            this.SectorsPerCluster = BootSector.SectorsPerCluster;
            if (this.SectorsPerCluster == 0) {
                Debug.WriteLine("This is not a FAT disk (SectorsPerCluster is zero).");
	            return false;
            }

            this.TotalSectors = BootSector.TotalSectors16;
            if (this.TotalSectors == 0)
	            this.TotalSectors = BootSector.TotalSectors32;

            this.RootDirSectors = (ulong)((BootSector.RootEntries * 32) + (BootSector.BytesPerSector - 1)) / BootSector.BytesPerSector;

            uint SectorsPerFAT = BootSector.SectorsPerFAT;

            if (SectorsPerFAT == 0)
                SectorsPerFAT = BootSector.FAT1632Info.FAT32_SectorsPerFAT32;

            this.FirstDataSector = BootSector.ReservedSectors + (BootSector.NumberOfFATs * SectorsPerFAT) + this.RootDirSectors;
            this.DataSector = this.TotalSectors - (BootSector.ReservedSectors + (BootSector.NumberOfFATs * SectorsPerFAT) + this.RootDirSectors);
            this.ClusterCount = this.DataSector / BootSector.SectorsPerCluster;

            if (this.ClusterCount < 4085) {
		        this.Type = FATTypes.FAT12;

                Debug.WriteLine("This is a FAT12 disk.");
	        }
	        else if (this.ClusterCount < 65525) {
		        this.Type = FATTypes.FAT16;

		        Debug.WriteLine("This is a FAT16 disk.");
	        }
	        else {
		        this.Type = FATTypes.FAT32;

		        Debug.WriteLine("This is a FAT32 disk.");
	        }

            this.BytesPerCluster = this.BytesPerSector * this.SectorsPerCluster;

            this.TotalClusters = (uint)this.ClusterCount;

            Debug.WriteLine("  OEMName: {0}", BootSector.BS_OEMName);
	        Debug.WriteLine("  BytesPerSector: {0:d}", this.BytesPerSector);
            Debug.WriteLine("  TotalSectors: {0:d}", this.TotalSectors);
            Debug.WriteLine("  SectorsPerCluster: {0:d}", this.SectorsPerCluster);
            Debug.WriteLine("  RootDirSectors: {0:d}", this.RootDirSectors);
            Debug.WriteLine("  FATSz: {0:d}", SectorsPerFAT);
            Debug.WriteLine("  FirstDataSector: {0:d}", this.FirstDataSector);
            Debug.WriteLine("  DataSec: {0:d}", this.DataSector);
            Debug.WriteLine("  CountofClusters: {0:d}", this.ClusterCount);
            Debug.WriteLine("  ReservedSectors: {0:d}", BootSector.ReservedSectors);
            Debug.WriteLine("  NumberFATs: {0:d}", BootSector.NumberOfFATs);
            Debug.WriteLine("  RootEntriesCount: {0:d}", BootSector.RootEntries);
	        Debug.WriteLine("  MediaType: {0:X}", BootSector.MediaDescriptor);
            Debug.WriteLine("  SectorsPerTrack: {0:d}", BootSector.SectorsPerTrack);
            Debug.WriteLine("  NumberOfHeads: {0:d}", BootSector.Heads);
            Debug.WriteLine("  HiddenSectors: {0:d}", BootSector.HiddenSectors);
	        if (this.Type != FATTypes.FAT32) {
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

            switch (this.Type)
            {
                case FATTypes.FAT12:
                    {
                        FATSize = this.ClusterCount + 1; 
                        break;
                    }

                case FATTypes.FAT16:
                    {
                        FATSize = (this.ClusterCount + 1) * 2; 
                        break;
                    }

                case FATTypes.FAT32:
                    {
                        FATSize = (this.ClusterCount + 1) * 4; 
                        break;
                    }
                default:
                    {
                        FATSize = 0;
                        break;
                    }
            }

            if (FATSize % this.BytesPerSector > 0)
                FATSize = (ulong)(FATSize + this.BytesPerSector - FATSize % this.BytesPerSector);

            PInvoke.LARGE_INTEGER Trans = new PInvoke.LARGE_INTEGER() { QuadPart = BootSector.ReservedSectors * this.BytesPerSector };

            IntPtr FATDataPtr = Marshal.AllocHGlobal((int)FATSize);

            System.Threading.NativeOverlapped nativeOverlapped = new System.Threading.NativeOverlapped();
            nativeOverlapped.EventHandle = IntPtr.Zero;
            nativeOverlapped.OffsetLow = (int)Trans.LowPart;
            nativeOverlapped.OffsetHigh = Trans.HighPart;

            PInvoke.ReadFile(this.Volume.Handle, FATDataPtr, (uint)FATSize, out BytesRead, ref nativeOverlapped);

            PInvoke.FATData FatData = new PInvoke.FATData(FATDataPtr, this.Type, FATSize);

            return true;
        }
    }
}
