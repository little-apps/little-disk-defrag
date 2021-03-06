﻿/*
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Little_Disk_Defrag.Helpers.Partitions;
using Little_Disk_Defrag.Misc;
using Microsoft.Win32.SafeHandles;

namespace Little_Disk_Defrag.Helpers
{
    public class DriveVolume : IDisposable
    {
        public SafeFileHandle Handle;
        private byte[] BitmapDetail;

        public PartInfo PartInfo { get; private set; }

        public FileStream DriveStream { get; private set; }

        public PInvoke.DISK_GEOMETRY Geometry { get; set; }

        /// <summary>
        /// Location of volume (with leading slash)
        /// </summary>
        /// <example>C:\</example>
        public string RootPath { get; private set; }

        public List<string> Directories { get; }

        public List<FileInfo> Files { get; }

        public int DBFileCount => Files.Count;

        public int DBDirCount => Directories.Count;

        public bool BitmapLoaded => (!((BitmapDetail == null) || BitmapDetail.Length == 0));

        public DriveInfo DriveInfo { get; private set; }

        public DriveVolume()
        {
            Directories = new List<string>();
            Files = new List<FileInfo>();
            
        }

        ~DriveVolume()
        {
            Dispose(false);
        }

        #region IDisposable Implementation
        private bool disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (!Handle.IsClosed && !Handle.IsInvalid)
                        Handle.Close();
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
            RootPath = name + "\\";

            Handle = PInvoke.CreateFile(
                FileName,
                //MAXIMUM_ALLOWED,                                  // access
		        PInvoke.GENERIC_READ,
                PInvoke.FILE_SHARE_READ | PInvoke.FILE_SHARE_WRITE, // share type
                IntPtr.Zero,                                        // security descriptor
                PInvoke.OPEN_EXISTING,                              // open type
                0,                                                  // attributes (none)
                IntPtr.Zero                                         // template
            );

            if (Handle.IsClosed || Handle.IsInvalid)
            {
                Debug.WriteLine("Unable to open volume. Error #{0} occurred", Marshal.GetLastWin32Error());

		        retVal = false;
            }
            else
            {
                // Get DriveInfo
                DriveInfo = new DriveInfo(RootPath);
                DriveStream = new FileStream(Handle, FileAccess.Read);

                // Detect filesystem
                //if (this._driveInfo.DriveFormat.ToUpper() == "NTFS")
                //    this._partInfo = new NTFS(this);
                //else
                    PartInfo = new FAT(this);

                retVal = true;
            }

            return retVal;
        }

        /// <summary>
        /// Gets drive bitmap
        /// </summary>
        /// <returns></returns>
        public bool GetBitmap()
        {
            Int64 StartingLCN = 0;
            uint BytesReturned = 0;

            GCHandle handle = GCHandle.Alloc(StartingLCN, GCHandleType.Pinned);
            var StartingLCNPtr = handle.AddrOfPinnedObject();

            //BitmapSize = (uint)Marshal.SizeOf(typeof(PInvoke.VOLUME_BITMAP_BUFFER)) + 4;
            uint BitmapSize = 28;

            var pDest = Marshal.AllocHGlobal((int)BitmapSize);

            var Result = PInvoke.DeviceIoControl(
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

            var Bitmap = new PInvoke.VOLUME_BITMAP_BUFFER(pDest, false);

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
/*
            BitmapSize = (uint)Marshal.SizeOf(typeof(PInvoke.VOLUME_BITMAP_BUFFER)) + ((uint)Bitmap.BitmapSize.QuadPart / 8) + 1;
*/

            PartInfo.ClusterCount = Bitmap.BitmapSize.QuadPart;

            BitmapDetail = Bitmap.Buffer;

            Marshal.FreeHGlobal(pDest);

            return true;
        }

        public bool IsClusterUsed (ulong Cluster)
        {
            int[] BitShift = { 1, 2, 4, 8, 16, 32, 64, 128 };
            int IsUsed = (BitmapDetail[Cluster / 8] & BitShift[Cluster % 8]);
            return (IsUsed > 0);
        }

        public void SetCluster(ulong Cluster, bool Used)
        {
            if (Used)
                BitmapDetail[Cluster / 8] = 0xFF; // 0xFF is always greater than 0 when AND with  1, 2, 4, 8, 16, 32, 64, and 128
            else
                BitmapDetail[Cluster / 8] = 0;
        }

        public bool BuildFileList (Defragment defrag)
        {
            Files.Clear();
            Directories.Clear();
            Directories.Add(RootPath);

            BuildDBInfo Info = new BuildDBInfo(defrag, this, (PartInfo.TotalBytes - PartInfo.FreeBytes) / PartInfo.ClusterSize);

            ScanDirectory (RootPath, BuildDBCallback, Info);

            if (defrag.PleaseStop)
            {
                Directories.Clear();
                Files.Clear();
            }

            return (true);
        }

        bool BuildDBCallback(ref FileInfo Info, ref SafeFileHandle FileHandle, ref BuildDBInfo DBInfo)
        {
            DriveVolume Vol = DBInfo.Volume;

            Vol.Files.Add (Info);

            if (DBInfo.QuitMonitor)
                return (false);

            DBInfo.ClusterProgress += Info.Clusters;
            DBInfo.Percent = (DBInfo.ClusterProgress / (double)DBInfo.ClusterCount) * 100.0f;

            return (true);
        }

        public delegate bool ScanCallback(ref FileInfo Info, ref SafeFileHandle FileHandle, ref BuildDBInfo DBInfo);

        public bool ScanDirectory(string DirPrefix, ScanCallback Callback, BuildDBInfo UserData)
        {
            PInvoke.WIN32_FIND_DATA FindData;

            var DirIndice = (uint)Directories.Count - 1;

            var SearchString = DirPrefix + "*";

            var FindHandle = PInvoke.FindFirstFile(SearchString, out FindData);

            if (FindHandle == PInvoke.INVALID_HANDLE_VALUE)
                return false;

            do
            {
                UInt64 FileSize = ((FindData.nFileSizeHigh << 32) | FindData.nFileSizeLow);

                FileInfo Info = new FileInfo(FindData.cFileName, DirIndice, FileSize, FindData.dwFileAttributes);
                SafeFileHandle Handle = null;

                // DonLL't ever include '.L' and '..'
                if (Info.Name == "."  || Info.Name == "..")
                    continue;

                //Info.FullName = DirPrefix + Info.Name;

                Info.Clusters = 0;
                if (GetClusterInfo(Info, ref Handle))
                {
                    UInt64 TotalClusters = Info.Fragments.Aggregate<Extent, ulong>(0, (current, ext) => current + ext.Length);

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
                var CallbackResult = Callback(ref Info, ref Handle, ref UserData);

                if (!Handle.IsInvalid && !Handle.IsClosed)
                    Handle.Close();

                if (!CallbackResult)
                    break;

                // If directory, perform recursion
                if (Info.Attributes.Directory)
                {
                    var Dir = GetDBDir (Info.DirIndice);
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
            return Directories[(int)index];
        }

        public FileInfo GetDBFile(uint index)
        {
            return Files[(int)index];
        }

        private bool ShouldProcess (FileAttr Attr)
        {
            if (Attr.Offline || Attr.Reparse || Attr.Temporary)
                return false;

            return (true);
        }

        private bool GetClusterInfo(FileInfo Info, ref SafeFileHandle Handle)
        {
            Info.Fragments.Clear();

            string FullName = GetDBDir(Info.DirIndice) + Info.Name;
            PInvoke.BY_HANDLE_FILE_INFORMATION FileInfo;

            if ((Handle == null) || Handle.IsClosed || Handle.IsInvalid)
            {
                Handle = PInvoke.CreateFile(
                    FullName,
                    PInvoke.GENERIC_READ,
                    PInvoke.FILE_SHARE_READ | PInvoke.FILE_SHARE_WRITE,
                    IntPtr.Zero,
                    PInvoke.OPEN_EXISTING,
                    (Info.Attributes.Directory) ? PInvoke.FILE_FLAG_BACKUP_SEMANTICS : 0,
                    IntPtr.Zero
                );
            }

            if (Handle.IsInvalid || Handle.IsClosed)
            {
                Debug.WriteLine("Error #{0} occurred trying to open file '{1}'", Marshal.GetLastWin32Error(), FullName);

                Info.Attributes.AccessDenied = true;
                return false;
            }

            var Result = PInvoke.GetFileInformationByHandle (Handle, out FileInfo);

            if (!Result)
            {
                Info.Attributes.AccessDenied = true;
                Debug.WriteLine("GetFileInformationByHandle ('{0}') failed\n", FullName);

                Handle.Close();
                return false;
            }

            // Get cluster allocation information
            PInvoke.LARGE_INTEGER StartingVCN = new PInvoke.LARGE_INTEGER();
            IntPtr pDest = IntPtr.Zero;
            uint BytesReturned = 0;

            // Grab info one extent at a time, until it's done grabbing all the extent data
            // Yeah, well it doesn't give us a way to ask L"how many extents?" that I know of ...
            // btw, the Extents variable tends to only reflect memory usage, so when we have
            // all the extents we look at the structure Win32 gives us for the REAL count!
            uint Extents = 10;

            const uint RETRIEVAL_POINTERS_BUFFER_SIZE = 28;

            StartingVCN.QuadPart = 0;

            GCHandle handle = GCHandle.Alloc(StartingVCN, GCHandleType.Pinned);
            IntPtr StartingVCNPtr = handle.AddrOfPinnedObject();

            do
            {
                Extents *= 2;
                var RetSize = RETRIEVAL_POINTERS_BUFFER_SIZE + (uint)((Extents - 1) * Marshal.SizeOf(typeof(PInvoke.LARGE_INTEGER)) * 2);

                pDest = pDest != IntPtr.Zero ? Marshal.ReAllocHGlobal(pDest, (IntPtr)RetSize) : Marshal.AllocHGlobal((int)RetSize);

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
                        Handle.Close();
                        Marshal.FreeHGlobal(pDest);

                        return false;
                    }

                    Extents++;
                }
            } while (!Result);

            var Retrieval = new PInvoke.RETRIEVAL_POINTERS_BUFFER(pDest);

            // Readjust extents, as it only reflects how much memory was allocated and may not
            // be accurate
            Extents = (uint)Retrieval.ExtentCount;

            // Ok, we have the info. Now translate it. hrmrmr

            Info.Fragments.Clear();
            for (int i = 0; i < Extents; i++)
            {
                Extent Add;

                Add.StartLCN = Retrieval.Extents[i].Lcn.QuadPart;
                if (i != 0)
                    Add.Length = Retrieval.Extents[i].NextVcn.QuadPart - Retrieval.Extents[i - 1].NextVcn.QuadPart;
                else
                    Add.Length = Retrieval.Extents[i].NextVcn.QuadPart - Retrieval.StartingVcn.QuadPart;

                Info.Fragments.Add(Add);
            }

            Marshal.FreeHGlobal(pDest);

            return true;
        }

        public bool FindFreeRange (ulong StartLCN, ulong ReqLength, out ulong LCNResult)
        {
            ulong Max;
            ulong i;

            LCNResult = 0;

            for (i = StartLCN; i < PartInfo.ClusterCount; i++)
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
                {
                    ulong j;
                    for (j = (i + 1); j < (i + ReqLength - 2); j++)
                    {
                        if (IsClusterUsed(j))
                        {
                            Found = false;
                            break;
                        }
                    }
                }

                if (!Found)
                    continue;
                LCNResult = i;
                return true;
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
            bool ReturnVal;
            PInvoke.MoveFileData MoveData = new PInvoke.MoveFileData();

            // NewLCN can't be less than 0
            if (NewLCN == 0)
                return false;

            // Set up variables
            var Info = GetDBFile ((uint)FileIndice);
            var FullName = GetDBDir (Info.DirIndice);
            FullName += Info.Name;

            var CurrentLCN = NewLCN;
            ulong CurrentVCN = 0;

            /*
            if (Info.Attributes.Directory == 1)
            {
                //
            }
            */

            // Open file
            var FileHandle = PInvoke.CreateFile
                (
                    FullName,
                    PInvoke.GENERIC_READ,
                    PInvoke.FILE_SHARE_READ,
                    IntPtr.Zero,
                    PInvoke.OPEN_EXISTING,
                    (Info.Attributes.Directory) ? PInvoke.FILE_FLAG_BACKUP_SEMANTICS : 0,
                    IntPtr.Zero
                );

            if (FileHandle.IsClosed || FileHandle.IsInvalid)
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
                        uint BytesReturned = 0;

                        //wprintf (L"%3u", i);

                        MoveData.ClusterCount = (uint)Info.Fragments[i].Length;
                        MoveData.StartingLCN.QuadPart = CurrentLCN;
                        MoveData.StartingVCN.QuadPart = CurrentVCN;

                        MoveData.hFile = FileHandle.DangerousGetHandle();

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

                        var Result = PInvoke.DeviceIoControl
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

                        CurrentLCN += Info.Fragments[i].Length;
                        CurrentVCN += Info.Fragments[i].Length;
                    }
                }
                catch
                {
                    // ignored
                }
                finally
                {
                    // Update file info either way
                    FileInfo fileInfo = Files[FileIndice];
                    GetClusterInfo(fileInfo, ref FileHandle);

                    FileHandle.Close();
                }

            }

            return ReturnVal;
        }
    }
}
