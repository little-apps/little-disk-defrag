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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Little_Disk_Defrag.Helpers
{
    public class DefragReport
    {
        readonly List<uint> _fraggedFiles;
        readonly List<uint> _unfraggedFiles;
        readonly List<uint> _unmovableFiles;

        public string RootPath;
        public string Label;
        public string Serial;
        public string FileSystem;
        public UInt64 FreeBytes;
        public UInt64 ClusterCount;
        public UInt32 ClusterSize;
        public UInt64 DiskSizeBytes;
        public UInt64 DirsCount;
        public UInt64 FilesCount;
        public UInt64 FilesSizeBytes;
        public UInt64 FilesSizeOnDisk;
        public UInt64 FilesSizeClusters;
        public UInt64 FilesSlackBytes;
        public uint FilesFragments;
        public double PercentFragged;
        public double PercentSlack;

        public double AverageFragments
        {
            get { return (double)FilesFragments / (double)FilesCount; }
        }

        public List<uint> FraggedFiles
        {
            get { return this._fraggedFiles; }
        }
        public List<uint> UnfraggedFiles
        {
            get { return this._unfraggedFiles; }
        }
        public List<uint> UnmovableFiles
        {
            get { return this._unmovableFiles; }
        }

        public DefragReport()
        {
            this._fraggedFiles = new List<uint>();
            this._unfraggedFiles = new List<uint>();
            this._unmovableFiles = new List<uint>();
        }
    }
}
