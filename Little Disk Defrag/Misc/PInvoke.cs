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
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Little_Disk_Defrag.Helpers.Partitions;
using Microsoft.Win32.SafeHandles;

namespace Little_Disk_Defrag.Misc
{
    public static class PInvoke
    {
        #region Defines
        internal const uint FILE_SHARE_READ = 0x00000001;
        internal const uint FILE_SHARE_WRITE = 0x00000002;
        internal const uint FILE_SHARE_DELETE = 0x00000004;
        internal const uint OPEN_EXISTING = 3;

        internal const uint GENERIC_READ = (0x80000000);
        internal const uint GENERIC_WRITE = (0x40000000);

        internal const uint FILE_FLAG_NO_BUFFERING = 0x20000000;
        internal const uint FILE_READ_ATTRIBUTES = (0x0080);
        internal const uint FILE_WRITE_ATTRIBUTES = 0x0100;
        internal const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;

        internal const uint ERROR_HANDLE_EOF = 38;
        internal const uint ERROR_INSUFFICIENT_BUFFER = 122;
        internal const uint ERROR_MORE_DATA = 234;

        internal static IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        internal const int MAX_PATH = 260;
        internal const int MAX_ALTERNATE = 14;

        internal const int SE_PRIVILEGE_ENABLED = 0x00000002;
        internal const int TOKEN_QUERY = 0x00000008;
        internal const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;

        /// <summary>
        /// constants lifted from winioctl.h from platform sdk
        /// </summary>
        internal class FSConstants
        {
            const uint FILE_DEVICE_DISK = 0x00000007;
            const uint IOCTL_DISK_BASE = FILE_DEVICE_DISK;
            const uint FILE_DEVICE_FILE_SYSTEM = 0x00000009;

            const uint METHOD_NEITHER = 3;
            const uint METHOD_BUFFERED = 0;

            const uint FILE_ANY_ACCESS = 0;
            const uint FILE_SPECIAL_ACCESS = FILE_ANY_ACCESS;

            public static uint FSCTL_GET_VOLUME_BITMAP = CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 27, METHOD_NEITHER, FILE_ANY_ACCESS);
            public static uint FSCTL_GET_RETRIEVAL_POINTERS = CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 28, METHOD_NEITHER, FILE_ANY_ACCESS);
            public static uint FSCTL_MOVE_FILE = CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 29, METHOD_BUFFERED, FILE_SPECIAL_ACCESS);

            public static uint IOCTL_DISK_GET_DRIVE_GEOMETRY = CTL_CODE(IOCTL_DISK_BASE, 0x0000, METHOD_BUFFERED, FILE_ANY_ACCESS);

            static uint CTL_CODE(uint DeviceType, uint Function, uint Method, uint Access)
            {
                return ((DeviceType) << 16) | ((Access) << 14) | ((Function) << 2) | (Method);
            }
        }
        #endregion

        #region DLL Functions
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool ReadFile(SafeFileHandle hFile, [Out] IntPtr lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, [In] ref NativeOverlapped lpOverlapped);

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int SetFilePointer(SafeFileHandle handle, int lDistanceToMove, out int lpDistanceToMoveHigh, uint dwMoveMethod);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool DeviceIoControl(SafeFileHandle hDevice, uint dwIoControlCode, IntPtr lpInBuffer, uint nInBufferSize, [Out] IntPtr lpOutBuffer, uint nOutBufferSize, ref uint lpBytesReturned, IntPtr lpOverlapped);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetVolumeInformation(string RootPathName, StringBuilder VolumeNameBuffer, int VolumeNameSize, out uint VolumeSerialNumber, out uint MaximumComponentLength, out FileSystemFeature FileSystemFlags, StringBuilder FileSystemNameBuffer, int nFileSystemNameSize);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool GetDiskFreeSpace(string lpRootPathName, out uint lpSectorsPerCluster, out uint lpBytesPerSector, out uint lpNumberOfFreeClusters, out uint lpTotalNumberOfClusters);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll")]
        internal static extern bool FindClose(IntPtr hFindFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetFileInformationByHandle(SafeFileHandle hFile, out BY_HANDLE_FILE_INFORMATION lpFileInformation);

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern bool AdjustTokenPrivileges(IntPtr htok, bool disall, ref TokPriv1Luid newst, int len, IntPtr prev, IntPtr relen);

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern bool OpenProcessToken(IntPtr h, int acc, ref IntPtr phtok);

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool LookupPrivilegeValue(string host, string name, ref long pluid);
        #endregion

        #region Structures
        [StructLayout(LayoutKind.Sequential)]
        public struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct BY_HANDLE_FILE_INFORMATION
        {
            public uint FileAttributes;
            public FILETIME CreationTime;
            public FILETIME LastAccessTime;
            public FILETIME LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            public uint FileIndexHigh;
            public uint FileIndexLow;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WIN32_FIND_DATA
        {
            public FileAttributes dwFileAttributes;
            public FILETIME ftCreationTime;
            public FILETIME ftLastAccessTime;
            public FILETIME ftLastWriteTime;
            public uint nFileSizeHigh; //changed all to uint, otherwise you run into unexpected overflow
            public uint nFileSizeLow;  //|
            public uint dwReserved0;   //|
            public uint dwReserved1;   //v
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_ALTERNATE)]
            public string cAlternate;
        }

        [StructLayout(LayoutKind.Explicit, Size = 8)]
        public struct LARGE_INTEGER
        {
            [FieldOffset(0)]
            public UInt64 QuadPart;
            [FieldOffset(0)]
            public UInt32 LowPart;
            [FieldOffset(4)]
            public Int32 HighPart;
        }

        //[StructLayout(LayoutKind.Explicit, Size = 24)]
        internal struct VOLUME_BITMAP_BUFFER
        {
            //[FieldOffset(0)]
            public LARGE_INTEGER StartingLcn;
            //[FieldOffset(8)]
            public LARGE_INTEGER BitmapSize;
            //[FieldOffset(16)]
            public byte[] Buffer;

            public VOLUME_BITMAP_BUFFER(IntPtr pBitmap, bool getBuffer)
            {
                StartingLcn = (LARGE_INTEGER)Marshal.PtrToStructure(pBitmap, typeof(LARGE_INTEGER));
                BitmapSize = (LARGE_INTEGER)Marshal.PtrToStructure(IntPtr.Add(pBitmap, 8), typeof(LARGE_INTEGER));

                if (getBuffer)
                {
                    int bufferSize = (int)(BitmapSize.QuadPart / 8);
                    Buffer = new byte[bufferSize];

                    Marshal.Copy(IntPtr.Add(pBitmap, 16), Buffer, 0, bufferSize);
                }
                else
                {
                    Buffer = null;
                }
            }

            public VOLUME_BITMAP_BUFFER(IntPtr pBitmap, int bufferSize)
            {
                StartingLcn = (LARGE_INTEGER)Marshal.PtrToStructure(pBitmap, typeof(LARGE_INTEGER));
                BitmapSize = (LARGE_INTEGER)Marshal.PtrToStructure(IntPtr.Add(pBitmap, 8), typeof(LARGE_INTEGER));

                Buffer = new byte[bufferSize];

                Marshal.Copy(IntPtr.Add(pBitmap, 16), Buffer, 0, bufferSize);
            }
        }

        [StructLayout(LayoutKind.Explicit, Size=16)]
        internal struct NTFS_FILE_RECORD_OUTPUT_BUFFER
        {
            [FieldOffset(0)] readonly LARGE_INTEGER FileReferenceNumber;
            [FieldOffset(8)] readonly ulong FileRecordLength;
            [FieldOffset(12)] readonly byte[] FileRecordBuffer;
        }

        [StructLayout(LayoutKind.Sequential, Size = 512, Pack=1, CharSet=CharSet.Ansi)]
        internal struct FATBootSector
        {
            public byte[] BS_jmpBoot; // 0
            public string BS_OEMName; // 3
            public ushort BytesPerSector; // 11
            public byte SectorsPerCluster; // 13
            public ushort ReservedSectors; // 14
            public byte NumberOfFATs; // 16
            public ushort RootEntries; // 17
            public ushort TotalSectors16; // 19
            public byte MediaDescriptor; // 21
            public ushort SectorsPerFAT; // 22
            public ushort SectorsPerTrack; // 24
            public ushort Heads; // 26
            public uint HiddenSectors; // 28
            public uint TotalSectors32; // 32

            public FAT1632Info FAT1632Info;

            public ushort Signature;

            public FATBootSector(IntPtr ptr)
            {
                int i;

                BS_jmpBoot = new byte[3];

                for (i = 0; i < 3; i++)
                {
                    BS_jmpBoot[i] = Marshal.ReadByte(ptr);
                    ptr = IntPtr.Add(ptr, 1);
                }

                StringBuilder oemInfo = new StringBuilder(8);

                for (i = 0; i < 8; i++)
                {
                    char c = (char)Marshal.ReadByte(ptr);
                    oemInfo.Append(c);
                    ptr = IntPtr.Add(ptr, 1);
                }

                BS_OEMName = oemInfo.ToString();

                BytesPerSector = (ushort)Marshal.ReadInt16(ptr);
                ptr = IntPtr.Add(ptr, 2);

                SectorsPerCluster = Marshal.ReadByte(ptr);
                ptr = IntPtr.Add(ptr, 1);

                ReservedSectors = (ushort)Marshal.ReadInt16(ptr);
                ptr = IntPtr.Add(ptr, 2);

                NumberOfFATs = Marshal.ReadByte(ptr);
                ptr = IntPtr.Add(ptr, 1);

                RootEntries = (ushort)Marshal.ReadInt16(ptr);
                ptr = IntPtr.Add(ptr, 2);

                TotalSectors16 = (ushort)Marshal.ReadInt16(ptr);
                ptr = IntPtr.Add(ptr, 2);

                MediaDescriptor = Marshal.ReadByte(ptr);
                ptr = IntPtr.Add(ptr, 1);

                SectorsPerFAT = (ushort)Marshal.ReadInt16(ptr);
                ptr = IntPtr.Add(ptr, 2);

                SectorsPerTrack = (ushort)Marshal.ReadInt16(ptr);
                ptr = IntPtr.Add(ptr, 2);

                Heads = (ushort)Marshal.ReadInt16(ptr);
                ptr = IntPtr.Add(ptr, 2);

                HiddenSectors = (uint)Marshal.ReadInt32(ptr);
                ptr = IntPtr.Add(ptr, 4);

                TotalSectors32 = (uint)Marshal.ReadInt32(ptr);
                ptr = IntPtr.Add(ptr, 4);

                FAT1632Info = new FAT1632Info(ptr);
                ptr = IntPtr.Add(ptr, 474);

                Signature = (ushort)Marshal.ReadInt16(ptr);
                ptr = IntPtr.Add(ptr, 2);
            }
        }

        [StructLayout(LayoutKind.Sequential, Size=474, Pack=1)]
        internal struct FAT1632Info
        {
            // FAT16
            public byte FAT16_LogicalDriveNumber; // 36
            public byte FAT16_Reserved1; // 37
            public byte FAT16_ExtendedSignature; // 38
            public uint FAT16_PartitionSerialNumber; // 39
            public string FAT16_VolumeName; // 43
            public string FAT16_FSType; // 54
            public byte[] FAT16_Reserved2; // 62

            // FAT32
            public uint FAT32_SectorsPerFAT32; // 36
            public ushort FAT32_ExtFlags; // 40
            public ushort FAT32_FSVer; // 42
            public uint FAT32_RootDirStart; // 44
            public ushort FAT32_FSInfoSector; // 48
            public ushort FAT32_BackupBootSector; // 50
            public byte[] FAT32_Reserved1; // 52
            public byte FAT32_LogicalDriveNumber; // 64
            public byte FAT32_Reserved2; // 65
            public byte FAT32_ExtendedSignature; // 66
            public uint FAT32_PartitionSerialNumber; // 67
            public string FAT32_VolumeName; // 71
            public string FAT32_FSType; // 82
            public byte[] FAT32_Reserved3; // 90

            public FAT1632Info(IntPtr ptr)
            {
                int i;
                IntPtr startPtr = ptr;

                // FAT 16
                FAT16_LogicalDriveNumber = Marshal.ReadByte(ptr); // 0
                ptr = IntPtr.Add(ptr, 1);

                FAT16_Reserved1 = Marshal.ReadByte(ptr); // 1
                ptr = IntPtr.Add(ptr, 1);

                FAT16_ExtendedSignature = Marshal.ReadByte(ptr); // 2
                ptr = IntPtr.Add(ptr, 1);

                FAT16_PartitionSerialNumber = (uint)Marshal.ReadInt32(ptr); // 3
                ptr = IntPtr.Add(ptr, 4);

                StringBuilder volName16 = new StringBuilder(11);

                for (i = 0; i < 11; i++)
                {
                    char c = (char)Marshal.ReadByte(ptr);
                    volName16.Append(c);

                    ptr = IntPtr.Add(ptr, 1);
                }

                FAT16_VolumeName = volName16.ToString();

                StringBuilder fileSystemType16 = new StringBuilder(8);

                for (i = 0; i < 8; i++)
                {
                    char c = (char)Marshal.ReadByte(ptr);
                    fileSystemType16.Append(c);

                    ptr = IntPtr.Add(ptr, 1);
                }

                FAT16_FSType = fileSystemType16.ToString();

                FAT16_Reserved2 = new byte[448];

                for (i = 0; i < 448; i++)
                {
                    FAT16_Reserved2[i] = Marshal.ReadByte(ptr);
                    ptr = IntPtr.Add(ptr, 1);
                }

                // FAT32
                ptr = startPtr;

                FAT32_SectorsPerFAT32 = (uint)Marshal.ReadInt32(ptr); // 36, 4
                ptr = IntPtr.Add(ptr, 4);

                FAT32_ExtFlags = (ushort)Marshal.ReadInt16(ptr); // 40, 2
                ptr = IntPtr.Add(ptr, 2);

                FAT32_FSVer = (ushort)Marshal.ReadInt16(ptr); // 42, 2
                ptr = IntPtr.Add(ptr, 2);

                FAT32_RootDirStart = (uint)Marshal.ReadInt32(ptr); // 44, 4
                ptr = IntPtr.Add(ptr, 4);

                FAT32_FSInfoSector = (ushort)Marshal.ReadInt16(ptr); // 48, 2
                ptr = IntPtr.Add(ptr, 2);

                FAT32_BackupBootSector = (ushort)Marshal.ReadInt16(ptr); // 50, 2
                ptr = IntPtr.Add(ptr, 2);

                FAT32_Reserved1 = new byte[12];  // 52, 12

                for (i=0;i<12;i++) 
                {
                    FAT32_Reserved1[i] = Marshal.ReadByte(ptr);
                    ptr = IntPtr.Add(ptr, 1);
                }

                FAT32_LogicalDriveNumber = Marshal.ReadByte(ptr); // 64, 1
                ptr = IntPtr.Add(ptr, 1);

                FAT32_Reserved2 = Marshal.ReadByte(ptr); // 65, 1
                ptr = IntPtr.Add(ptr, 1);

                FAT32_ExtendedSignature = Marshal.ReadByte(ptr); // 66, 1
                ptr = IntPtr.Add(ptr, 1);

                FAT32_PartitionSerialNumber = (uint)Marshal.ReadInt32(ptr); // 67, 1
                ptr = IntPtr.Add(ptr, 4);

                StringBuilder volName32 = new StringBuilder(11); // 71, 11

                for (i = 0; i < 11; i++)
                {
                    char c = (char)Marshal.ReadByte(ptr);
                    volName32.Append(c);
                    ptr = IntPtr.Add(ptr, 1);
                }

                FAT32_VolumeName = volName32.ToString();

                StringBuilder fileSystemType32 = new StringBuilder(8);   // 82, 8

                for (i = 0; i < 8; i++)
                {
                    char c = (char)Marshal.ReadByte(ptr);
                    fileSystemType32.Append(c);
                    ptr = IntPtr.Add(ptr, 1);
                }

                FAT32_FSType = fileSystemType32.ToString();

                FAT32_Reserved3 = new byte[420]; // 90, 420

                for (i = 0; i < 420; i++)
                {
                    FAT32_Reserved3[i] = Marshal.ReadByte(ptr);
                    ptr = IntPtr.Add(ptr, 1);
                }
            }
        }

        internal struct FATData
        {
            public byte[] FAT12;
            public ushort[] FAT16;
            public uint[] FAT32;

            public FATData(IntPtr ptr, FAT.FATTypes type, ulong size)
            {
                ulong i;

                FAT12 = new byte[1];
                FAT16 = new ushort[1];
                FAT32 = new uint[1];

                if (type == FAT.FATTypes.FAT12)
                {
                    List<byte> arr = new List<byte>();

                    for (i = 0; i < size; i += 1)
                    {
                        arr.Add(Marshal.ReadByte(ptr));

                        ptr = IntPtr.Add(ptr, 1);
                    }

                    FAT12 = arr.ToArray();
                }
                else if (type == FAT.FATTypes.FAT16)
                {
                    List<ushort> arr = new List<ushort>();

                    for (i = 0; i < size; i += 2)
                    {
                        arr.Add((ushort)Marshal.PtrToStructure(ptr, typeof(ushort)));

                        ptr = IntPtr.Add(ptr, 2);
                    }

                    FAT16 = arr.ToArray();
                }
                else if (type == FAT.FATTypes.FAT32)
                {
                    List<uint> arr = new List<uint>();

                    for (i = 0; i < size; i += 4)
                    {
                        arr.Add((uint)Marshal.PtrToStructure(ptr, typeof(uint)));

                        ptr = IntPtr.Add(ptr, 4);
                    }

                    FAT32 = arr.ToArray();
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct FatDirStruct
        {
            [MarshalAs(UnmanagedType.LPStr, SizeConst=11)]
            public string  DIR_Name;           // 0   File name, 8 + 3.
            public byte DIR_Attr;               // 11  File attributes.
            public byte DIR_NTRes;              // 12  Reserved.
            public byte DIR_CrtTimeTenth;       // 13  Creation time, tenths of a second, 0...199.
            public ushort DIR_CrtTime;            // 14  Creation time.
            public ushort DIR_CrtDate;            // 16  Creation date.
            public ushort DIR_LstAccDate;         // 18  Last access date.
            public ushort DIR_FstClusHI;          // 20  First cluster number, high word.
            public ushort DIR_WrtTime;            // 22  Last write time.
            public ushort DIR_WrtDate;            // 24  Last write date.
            public ushort DIR_FstClusLO;          // 26  First cluster number, low word.
            public uint DIR_FileSize;           // 28  File size in bytes.
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct FatLongNameDirStruct {
            public byte LDIR_Ord;                // 0   Sequence number
            [MarshalAs(UnmanagedType.LPWStr, SizeConst=5)]
            public string LDIR_Name1;           // 1   Characters 1-5 in name
            public byte LDIR_Attr;               // 11  Attribute, must be ATTR_LONG_NAME
            public byte LDIR_Type;               // 12  Always zero
            public byte LDIR_Chksum;             // 13  Checksum
            [MarshalAs(UnmanagedType.LPWStr, SizeConst=6)]
            public string LDIR_Name2;           // 14  Characters 6-11
            [MarshalAs(UnmanagedType.LPStr, SizeConst=2)]
            public string LDIR_FstClusLO;       // 26  Always zero
            [MarshalAs(UnmanagedType.LPWStr, SizeConst=2)]
            public string LDIR_Name3;           // 28  Characters 12-13
        }

        internal struct Extent
        {
            public LARGE_INTEGER NextVcn;
            public LARGE_INTEGER Lcn;

            public Extent(IntPtr ptr)
            {
                IntPtr extentPtr = ptr;

                NextVcn = (LARGE_INTEGER)Marshal.PtrToStructure(extentPtr, typeof(LARGE_INTEGER));
                Lcn = (LARGE_INTEGER)Marshal.PtrToStructure(IntPtr.Add(extentPtr, 8), typeof(LARGE_INTEGER));
            }
        }

        internal struct RETRIEVAL_POINTERS_BUFFER
        {
            public int ExtentCount;
            public LARGE_INTEGER StartingVcn;
            public List<Extent> Extents;

            public RETRIEVAL_POINTERS_BUFFER(IntPtr ptr)
            {
                ExtentCount = (int)Marshal.PtrToStructure(ptr, typeof(int));

                ptr = IntPtr.Add(ptr, 8); // Added additional 4 bytes because of padding
                
                // StartingVcn
                StartingVcn = (LARGE_INTEGER)Marshal.PtrToStructure(ptr, typeof(LARGE_INTEGER));

                ptr = IntPtr.Add(ptr, 8); 

                Extents = new List<Extent>();

                for (int i = 0; i < ExtentCount; i++)
                {
                    Extents.Add(new Extent(ptr));

                    ptr = IntPtr.Add(ptr, 16);
                }
            }
        }

        internal struct MoveFileData
        {
            public IntPtr hFile;
            public LARGE_INTEGER StartingVCN;
            public LARGE_INTEGER StartingLCN;
            public uint ClusterCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DISK_GEOMETRY
        {
            public LARGE_INTEGER Cylinders;
            public MEDIA_TYPE MediaType;
            public uint TracksPerCylinder;
            public uint SectorsPerTrack;
            public uint BytesPerSector;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct TokPriv1Luid
        {
            public int Count;
            public long Luid;
            public int Attr;
        }
        #endregion

        #region Enums
        [Flags]
        internal enum FileSystemFeature : uint
        {
            /// <summary>
            /// The file system supports case-sensitive file names.
            /// </summary>
            CaseSensitiveSearch = 1,
            /// <summary>
            /// The file system preserves the case of file names when it places a name on disk.
            /// </summary>
            CasePreservedNames = 2,
            /// <summary>
            /// The file system supports Unicode in file names as they appear on disk.
            /// </summary>
            UnicodeOnDisk = 4,
            /// <summary>
            /// The file system preserves and enforces access control lists (ACL).
            /// </summary>
            PersistentACLS = 8,
            /// <summary>
            /// The file system supports file-based compression.
            /// </summary>
            FileCompression = 0x10,
            /// <summary>
            /// The file system supports disk quotas.
            /// </summary>
            VolumeQuotas = 0x20,
            /// <summary>
            /// The file system supports sparse files.
            /// </summary>
            SupportsSparseFiles = 0x40,
            /// <summary>
            /// The file system supports re-parse points.
            /// </summary>
            SupportsReparsePoints = 0x80,
            /// <summary>
            /// The specified volume is a compressed volume, for example, a DoubleSpace volume.
            /// </summary>
            VolumeIsCompressed = 0x8000,
            /// <summary>
            /// The file system supports object identifiers.
            /// </summary>
            SupportsObjectIDs = 0x10000,
            /// <summary>
            /// The file system supports the Encrypted File System (EFS).
            /// </summary>
            SupportsEncryption = 0x20000,
            /// <summary>
            /// The file system supports named streams.
            /// </summary>
            NamedStreams = 0x40000,
            /// <summary>
            /// The specified volume is read-only.
            /// </summary>
            ReadOnlyVolume = 0x80000,
            /// <summary>
            /// The volume supports a single sequential write.
            /// </summary>
            SequentialWriteOnce = 0x100000,
            /// <summary>
            /// The volume supports transactions.
            /// </summary>
            SupportsTransactions = 0x200000
        }

        public enum MEDIA_TYPE : uint
        {
            Unknown,
            F5_1Pt2_512,
            F3_1Pt44_512,
            F3_2Pt88_512,
            F3_20Pt8_512,
            F3_720_512,
            F5_360_512,
            F5_320_512,
            F5_320_1024,
            F5_180_512,
            F5_160_512,
            RemovableMedia,
            FixedMedia,
            F3_120M_512,
            F3_640_512,
            F5_640_512,
            F5_720_512,
            F3_1Pt2_512,
            F3_1Pt23_1024,
            F5_1Pt23_1024,
            F3_128Mb_512,
            F3_230Mb_512,
            F8_256_128,
            F3_200Mb_512,
            F3_240M_512,
            F3_32M_512
        }
        #endregion

    }
}
