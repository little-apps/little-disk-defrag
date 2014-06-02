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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Little_Disk_Defrag.Helpers
{
    public class DriveVolume : IDisposable
    {
        public IntPtr Handle;
        private byte[] BitmapDetail;
        private string _rootPath;
        private PInvoke.DISK_GEOMETRY Geometry;
        private readonly VolumeInfo _volInfo;
        private readonly List<string> _directoryList;
        private readonly List<FileInfo> _fileList;

        public VolumeInfo VolInfo
        {
            get { return this._volInfo; }
        }

        public string RootPath
        {
            get { return this._rootPath; }
        }

        public List<string> Directories
        {
            get { return this._directoryList; }
        }

        public List<FileInfo> Files
        {
            get { return this._fileList; }
        }

        public int DBFileCount
        {
            get { return this._fileList.Count; }
        }

        public int DBDirCount
        {
            get { return this._directoryList.Count; }
        }

        public bool BitmapLoaded
        {
            get
            {
                return (!((this.BitmapDetail == null) || this.BitmapDetail.Length == 0));
            }
        }

        public DriveVolume()
        {
            this._volInfo = new VolumeInfo();
            this._directoryList = new List<string>();
            this._fileList = new List<FileInfo>();
            
        }

        ~DriveVolume()
        {
            this.Dispose(false);
        }

        #region IDisposable Implementation
        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (Handle.ToInt32() != -1)
                        PInvoke.CloseHandle(Handle);
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        public bool Open(string name)
        {
            bool retVal;
            string FileName = "\\\\.\\" + name;
            this._rootPath = name + "\\";

            this.Handle = PInvoke.CreateFile(
                FileName,
                //MAXIMUM_ALLOWED,                                  // access
		        PInvoke.GENERIC_READ,
                PInvoke.FILE_SHARE_READ | PInvoke.FILE_SHARE_WRITE, // share type
                IntPtr.Zero,                                        // security descriptor
                PInvoke.OPEN_EXISTING,                              // open type
                0,                                                  // attributes (none)
                IntPtr.Zero                                         // template
            );

            if (this.Handle.ToInt32() == -1)
            {
                Debug.WriteLine("Unable to open volume. Error #{0} occurred", Marshal.GetLastWin32Error());

		        retVal = false;
            }
            else
            {
                StringBuilder VolName = new StringBuilder(64);
                uint VolSN;
                uint VolMaxFileLen;
                PInvoke.FileSystemFeature FSFlags;
                StringBuilder FSName = new StringBuilder(64);

                if (PInvoke.GetVolumeInformation(RootPath, VolName, VolName.Capacity, out VolSN, out VolMaxFileLen, out FSFlags, FSName, FSName.Capacity))
                {
                    VolInfo.FileSystem = FSName.ToString();
                    VolInfo.MaxNameLen = VolMaxFileLen;
                    VolInfo.Name       = VolName.ToString();
                    VolInfo.Serial     = string.Format("{0:X}-{1:X}", (VolSN & 0xffff0000) >> 16, VolSN & 0x0000ffff);
                }
                else
                {
                    VolInfo.FileSystem = "(Unknown)";
                    VolInfo.MaxNameLen = 255;
                    VolInfo.Name       = "(Unknown)";
                    VolInfo.Serial     = "(Unknown)";
                }

                retVal = true;
            }

            return retVal;
        }

        /// <summary>
        /// Retrieves drive geometry
        /// </summary>
        /// <returns></returns>
        public bool ObtainInfo() 
        {
            bool Result;
            uint BytesGot;
            UInt64 nan;

            int GeometrySize = Marshal.SizeOf(typeof(PInvoke.DISK_GEOMETRY));
            IntPtr GeometryPtr = Marshal.AllocHGlobal(GeometrySize);

            BytesGot = 0;
            Result = PInvoke.DeviceIoControl
            (
                Handle,
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

            this.Geometry = (PInvoke.DISK_GEOMETRY)Marshal.PtrToStructure(GeometryPtr, typeof(PInvoke.DISK_GEOMETRY));

            uint SectorsPerCluster;
            uint BytesPerSector;
            uint FreeClusters;
            uint TotalClusters;

            Result = PInvoke.GetDiskFreeSpace
            (
                RootPath,
                out SectorsPerCluster,
                out BytesPerSector,
                out FreeClusters,
                out TotalClusters
            );

            // Failed? Weird.
            if (!Result)
                return (false);

            VolInfo.ClusterSize = SectorsPerCluster * BytesPerSector;

            ulong totalBytes, freeBytes;

            Result = PInvoke.GetDiskFreeSpaceEx
            (
                RootPath,
                out nan,
                out totalBytes,
                out freeBytes
            );

            this.VolInfo.TotalBytes = totalBytes;
            this.VolInfo.FreeBytes = freeBytes;

            return true;
        }

        /// <summary>
        /// Gets drive bitmap
        /// </summary>
        /// <returns></returns>
        public bool GetBitmap()
        {
            Int64 StartingLCN = 0;
            IntPtr StartingLCNPtr;
            PInvoke.VOLUME_BITMAP_BUFFER Bitmap;
            UInt32 BitmapSize = 0;
            uint BytesReturned = 0;
            bool Result;
            IntPtr pDest;

            GCHandle handle = GCHandle.Alloc(StartingLCN, GCHandleType.Pinned);
            StartingLCNPtr = handle.AddrOfPinnedObject();

            //BitmapSize = (uint)Marshal.SizeOf(typeof(PInvoke.VOLUME_BITMAP_BUFFER)) + 4;
            BitmapSize = 28;

            pDest = Marshal.AllocHGlobal((int)BitmapSize);

            Result = PInvoke.DeviceIoControl(
                    Handle,
                    PInvoke.FSConstants.FSCTL_GET_VOLUME_BITMAP,
                    StartingLCNPtr,
                    (uint)Marshal.SizeOf(StartingLCN),
                    pDest,
                    BitmapSize,
                    ref BytesReturned,
                    IntPtr.Zero);

            // Bad result?
            if (Result == false && Marshal.GetLastWin32Error() != PInvoke.ERROR_MORE_DATA)
            {
                Marshal.FreeHGlobal(pDest);
                return false;
            }

            Bitmap = new PInvoke.VOLUME_BITMAP_BUFFER(pDest, false);

            BitmapSize = (uint)Marshal.SizeOf(typeof(PInvoke.VOLUME_BITMAP_BUFFER)) + ((uint)Bitmap.BitmapSize.QuadPart / 8) + 1;
            pDest = Marshal.ReAllocHGlobal(pDest, (IntPtr)BitmapSize);

            Result = PInvoke.DeviceIoControl(
                    Handle,
                    PInvoke.FSConstants.FSCTL_GET_VOLUME_BITMAP,
                    StartingLCNPtr,
                    (uint)Marshal.SizeOf(StartingLCN),
                    pDest,
                    BitmapSize,
                    ref BytesReturned,
                    IntPtr.Zero);

            if (Result == false)
            {
                Debug.WriteLine("Couldn't properly read volume bitmap");
                Marshal.FreeHGlobal(pDest);
                return false;
            }

            Bitmap = new PInvoke.VOLUME_BITMAP_BUFFER(pDest, true);
            BitmapSize = (uint)Marshal.SizeOf(typeof(PInvoke.VOLUME_BITMAP_BUFFER)) + ((uint)Bitmap.BitmapSize.QuadPart / 8) + 1;

            this.VolInfo.ClusterCount = (ulong)Bitmap.BitmapSize.QuadPart;

            this.BitmapDetail = Bitmap.Buffer;

            Marshal.FreeHGlobal(pDest);

            return true;
        }

        public bool IsClusterUsed (ulong Cluster)
        {
            int[] BitShift = new int[] { 1, 2, 4, 8, 16, 32, 64, 128 };
            int IsUsed = (this.BitmapDetail[Cluster / 8] & BitShift[Cluster % 8]);
            return (IsUsed > 0);
        }

        public void SetCluster(ulong Cluster, bool Used)
        {
            if (Used)
                this.BitmapDetail[Cluster / 8] = 0xFF; // 0xFF is always greater than 0 when AND with  1, 2, 4, 8, 16, 32, 64, and 128
            else
                this.BitmapDetail[Cluster / 8] = 0;
        }

        public bool BuildFileList (Defragment defrag)
        {
            Files.Clear();
            Directories.Clear();
            Directories.Add(RootPath);

            BuildDBInfo Info = new BuildDBInfo(defrag, this, (this.VolInfo.TotalBytes - this.VolInfo.FreeBytes) / (UInt64)this.VolInfo.ClusterSize);

            ScanDirectory (RootPath, BuildDBCallback, Info);

            if (defrag.PleaseStop)
            {
                Directories.Clear();
                Files.Clear();
            }

            return (true);
        }

        bool BuildDBCallback(ref FileInfo Info, ref IntPtr FileHandle, ref BuildDBInfo DBInfo)
        {
            DriveVolume Vol = DBInfo.Volume;

            Vol.Files.Add (Info);

            if (DBInfo.QuitMonitor)
                return (false);

            DBInfo.ClusterProgress += (UInt64)Info.Clusters;
            DBInfo.Percent = ((double)DBInfo.ClusterProgress / (double)DBInfo.ClusterCount) * 100.0f;

            return (true);
        }

        public delegate bool ScanCallback(ref FileInfo Info, ref IntPtr FileHandle, ref BuildDBInfo DBInfo);

        public bool ScanDirectory(string DirPrefix, ScanCallback Callback, BuildDBInfo UserData)
        {
            PInvoke.WIN32_FIND_DATA FindData;
            IntPtr FindHandle;
            string SearchString;
            uint DirIndice;

            DirIndice = (uint)Directories.Count - 1;

            SearchString = DirPrefix + "*";

            FindHandle = PInvoke.FindFirstFile(SearchString, out FindData);

            if (FindHandle == PInvoke.INVALID_HANDLE_VALUE)
                return false;

            do
            {
                UInt64 FileSize = ((FindData.nFileSizeHigh << 32) | FindData.nFileSizeLow);

                FileInfo Info = new FileInfo(FindData.cFileName, DirIndice, FileSize, FindData.dwFileAttributes);
                IntPtr Handle = PInvoke.INVALID_HANDLE_VALUE;
                bool CallbackResult;

                // DonLL't ever include '.L' and '..'
                if (Info.Name == "."  || Info.Name == "..")
                    continue;

                //Info.FullName = DirPrefix + Info.Name;

                Info.Clusters = 0;
                if (GetClusterInfo(Info, Handle))
                {
                    UInt64 TotalClusters = 0;

                    foreach (Extent ext in Info.Fragments) {
                        TotalClusters += (ulong)ext.Length;
                    }

                    Info.Clusters = TotalClusters;
                }
                else
                {
                    Info.Attributes.Unmovable = true;
                    Info.Attributes.Process = false;
                }

                if (Info.Attributes.Process)
                    Info.Attributes.Process = ShouldProcess(Info.Attributes);

                // Run the user-defined callback function
                CallbackResult = Callback(ref Info, ref Handle, ref UserData);

                if (Handle != PInvoke.INVALID_HANDLE_VALUE)
                    PInvoke.CloseHandle (Handle);

                if (!CallbackResult)
                    break;

                // If directory, perform recursion
                if (Info.Attributes.Directory)
                {
                    string Dir;

                    Dir = GetDBDir (Info.DirIndice);
                    Dir += Info.Name;
                    Dir += "\\";

                    Directories.Add(Dir);
                    ScanDirectory (Dir, Callback, UserData);
                }

            } while (PInvoke.FindNextFile (FindHandle, out FindData));

            PInvoke.FindClose (FindHandle);
            return (false);
        }

        public string GetDBDir(uint index)
        {
            return this.Directories[(int)index];
        }

        public FileInfo GetDBFile(uint index)
        {
            return this.Files[(int)index];
        }

        private bool ShouldProcess (FileAttr Attr)
        {
            if (Attr.Offline || Attr.Reparse || Attr.Temporary)
                return false;

            return (true);
        }

        private bool GetClusterInfo (FileInfo Info, IntPtr HandleResult) {
            Info.Fragments.Clear();

            bool Result;
            IntPtr Handle;
            string FullName = this.GetDBDir(Info.DirIndice) + Info.Name;
            PInvoke.BY_HANDLE_FILE_INFORMATION FileInfo;

            Handle = PInvoke.CreateFile
            (
                FullName,
                PInvoke.GENERIC_READ,
                PInvoke.FILE_SHARE_READ | PInvoke.FILE_SHARE_WRITE,
                IntPtr.Zero,
                PInvoke.OPEN_EXISTING,
                (Info.Attributes.Directory) ? PInvoke.FILE_FLAG_BACKUP_SEMANTICS : 0,
                IntPtr.Zero
            );

            if (Handle == PInvoke.INVALID_HANDLE_VALUE)
            {
                Debug.WriteLine("Error #{0} occurred trying to open file '{1}'", Marshal.GetLastWin32Error(), FullName);

                Info.Attributes.AccessDenied = true;
                return false;
            }

            Result = PInvoke.GetFileInformationByHandle (Handle, out FileInfo);

            if (!Result)
            {
                Info.Attributes.AccessDenied = true;
                Debug.WriteLine("GetFileInformationByHandle ('{0}') failed\n", FullName);

                PInvoke.CloseHandle(Handle);
                return false;
            }

            // Get cluster allocation information
            PInvoke.LARGE_INTEGER StartingVCN = new PInvoke.LARGE_INTEGER();
            PInvoke.RETRIEVAL_POINTERS_BUFFER Retrieval;
            IntPtr pDest = IntPtr.Zero;
            uint RetSize;
            uint Extents;
            uint BytesReturned = 0;

            // Grab info one extent at a time, until it's done grabbing all the extent data
            // Yeah, well it doesn't give us a way to ask L"how many extents?" that I know of ...
            // btw, the Extents variable tends to only reflect memory usage, so when we have
            // all the extents we look at the structure Win32 gives us for the REAL count!
            Extents = 10;
            RetSize = 0;

            const uint RETRIEVAL_POINTERS_BUFFER_SIZE = 28;

            StartingVCN.QuadPart = 0;

            GCHandle handle = GCHandle.Alloc(StartingVCN, GCHandleType.Pinned);
            IntPtr StartingVCNPtr = handle.AddrOfPinnedObject();

            do
            {
                Extents *= 2;
                RetSize = RETRIEVAL_POINTERS_BUFFER_SIZE + (uint)((Extents - 1) * Marshal.SizeOf(typeof(PInvoke.LARGE_INTEGER)) * 2);

                if (pDest != IntPtr.Zero)
                    pDest = Marshal.ReAllocHGlobal(pDest, (IntPtr)RetSize);
                else
                    pDest = Marshal.AllocHGlobal((int)RetSize);

                Result = PInvoke.DeviceIoControl
                (
                    Handle,
                    PInvoke.FSConstants.FSCTL_GET_RETRIEVAL_POINTERS,
                    StartingVCNPtr,
                    (uint)Marshal.SizeOf(typeof(PInvoke.LARGE_INTEGER)),
                    pDest,
                    RetSize,
                    ref BytesReturned,
                    IntPtr.Zero
                );

                if (!Result)
                {
                    if (Marshal.GetLastWin32Error() != PInvoke.ERROR_MORE_DATA)
                    {
                        Debug.WriteLine("Error #{0} occurred trying to get retrieval pointers for file '{1}", Marshal.GetLastWin32Error(), FullName);

                        Info.Clusters = 0;
                        Info.Attributes.AccessDenied = true;
                        Info.Attributes.Process = false;
                        Info.Fragments.Clear();
                        PInvoke.CloseHandle(Handle);
                        Marshal.FreeHGlobal(pDest);

                        return false;
                    }

                    Extents++;
                }
            } while (!Result);

            Retrieval = new PInvoke.RETRIEVAL_POINTERS_BUFFER(pDest);

            // Readjust extents, as it only reflects how much memory was allocated and may not
            // be accurate
            Extents = (uint)Retrieval.ExtentCount;

            // Ok, we have the info. Now translate it. hrmrmr

            Info.Fragments.Clear();
            for (int i = 0; i < Extents; i++)
            {
                Extent Add;

                Add.StartLCN = Retrieval.Extents[(int)i].Lcn.QuadPart;
                if (i != 0)
                    Add.Length = Retrieval.Extents[(int)i].NextVcn.QuadPart - (ulong)Retrieval.Extents[(int)i - 1].NextVcn.QuadPart;
                else
                    Add.Length = Retrieval.Extents[(int)i].NextVcn.QuadPart - Retrieval.StartingVcn.QuadPart;

                Info.Fragments.Add(Add);
            }

            Marshal.FreeHGlobal(pDest);
            HandleResult = Handle;
            return true;
        }

        public bool FindFreeRange (ulong StartLCN, ulong ReqLength, out ulong LCNResult)
        {
            ulong Max;
            ulong i;
            ulong j;

            LCNResult = 0;

            for (i = StartLCN; i < this.VolInfo.ClusterCount; i++)
            {
                bool Found = true;

                // First check the first cluster
                if (IsClusterUsed(i))
                    Found = false;
                else
                // Then check the last cluster
                if (IsClusterUsed(i + ReqLength - 1))
                    Found = false;
                else
                // Check the whole darn range.
                for (j = (i + 1); j < (i + ReqLength - 2); j++)
                {
                    if (IsClusterUsed(j) == true)
                    {
                        Found = false;
                        break;
                    }
                }

                if (!Found)
                    continue;
                else
                {
                    LCNResult = i;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Move file to new LCN
        /// </summary>
        /// <remarks>we have to move each fragment of the file, as per the Win32 API</remarks>
        /// <param name="FileIndice">File index</param>
        /// <param name="NewLCN">LCN</param>
        /// <returns>True if file was moved</returns>
        public bool MoveFileDumb (int FileIndice, ulong NewLCN)
        {
            bool ReturnVal = false;
            FileInfo Info;
            IntPtr FileHandle;
            string FullName;
            PInvoke.MoveFileData MoveData = new PInvoke.MoveFileData();
            ulong CurrentLCN;
            ulong CurrentVCN;

            // NewLCN can't be less than 0
            if (NewLCN == 0)
                return false;

            // Set up variables
            Info = GetDBFile ((uint)FileIndice);
            FullName = GetDBDir (Info.DirIndice);
            FullName += Info.Name;
            CurrentLCN = NewLCN;
            CurrentVCN = 0;

            /*
            if (Info.Attributes.Directory == 1)
            {
                //
            }
            */

            // Open file
            FileHandle = PInvoke.CreateFile
            (
                FullName,
                PInvoke.GENERIC_READ,
                PInvoke.FILE_SHARE_READ,
                IntPtr.Zero,
                PInvoke.OPEN_EXISTING,
                (Info.Attributes.Directory) ? PInvoke.FILE_FLAG_BACKUP_SEMANTICS : 0,
                IntPtr.Zero
            );

            if (FileHandle == PInvoke.INVALID_HANDLE_VALUE)
            {
                Debug.WriteLine("Error #{0} occurred trying to get file handle for '{1}'", Marshal.GetLastWin32Error(), FullName);

                ReturnVal = false;
            }
            else
            {
                ReturnVal = true; // innocent until proven guilty ...

                try
                {
                    for (int i = 0; i < Info.Fragments.Count; i++)
                    {
                        bool Result;
                        uint BytesReturned = 0;

                        //wprintf (L"%3u", i);

                        MoveData.ClusterCount = (uint)Info.Fragments[i].Length;
                        MoveData.StartingLCN.QuadPart = CurrentLCN;
                        MoveData.StartingVCN.QuadPart = CurrentVCN;

                        MoveData.hFile = FileHandle;

                        GCHandle handle = GCHandle.Alloc(MoveData, GCHandleType.Pinned);
                        IntPtr pInput = handle.AddrOfPinnedObject();
                        uint bufSize = (uint)Marshal.SizeOf(MoveData);

                        /*
                        wprintf (L"\n");
                        wprintf (L"StartLCN: %I64u\n", MoveData.StartingLcn.QuadPart);
                        wprintf (L"StartVCN: %I64u\n", MoveData.StartingVcn.QuadPart);
                        wprintf (L"Clusters: %u (%I64u-%I64u --> %I64u-%I64u)\n", MoveData.ClusterCount,
                            Info.Fragments[i].StartLCN,
                            Info.Fragments[i].StartLCN + MoveData.ClusterCount,
                            MoveData.StartingLcn.QuadPart,
                            MoveData.StartingLcn.QuadPart + MoveData.ClusterCount - 1);
                        wprintf (L"\n");
                        */

                        Result = PInvoke.DeviceIoControl
                        (
                            Handle,
                            PInvoke.FSConstants.FSCTL_MOVE_FILE,
                            pInput,
                            bufSize,
                            IntPtr.Zero,
                            0,
                            ref BytesReturned,
                            IntPtr.Zero
                        );

                        //wprintf (L"\b\b\b");

                        if (!Result)
                        {
                            Debug.WriteLine("Error #{0} occurred trying to move file '{1}'", Marshal.GetLastWin32Error(), FullName);

                            ReturnVal = false;
                            break;  // yeah, bite me
                        }

                        // Ok good. Now update our drive bitmap and file infos.
                        ulong j;
                        for (j = 0; j < Info.Fragments[i].Length; j++)
                        {
                            SetCluster(Info.Fragments[i].StartLCN + j, false);
                            SetCluster(CurrentLCN + j, true);
                            //BitmapDetail[Info.Fragments[i].StartLCN + j].Allocated = false;
                            //BitmapDetail[CurrentLCN + j].Allocated = true;
                        }

                        CurrentLCN += (ulong)Info.Fragments[i].Length;
                        CurrentVCN += (ulong)Info.Fragments[i].Length;
                    }
                }
                catch
                {

                }
                finally
                {
                    // Update file info either way
                    PInvoke.CloseHandle (FileHandle);
                    FileHandle = IntPtr.Zero;
                    FileInfo fileInfo = Files[FileIndice];
                    GetClusterInfo (fileInfo, FileHandle);
                    PInvoke.CloseHandle(FileHandle);
                }

            }

            return ReturnVal;
        }
    }
}
