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
    public class VolumeInfo
    {
        private string _name;
        private string _serial;
        private ulong _maxNameLen;
        private string _fileSystem;
        private ulong _clusterCount;
        private uint _clusterSize;
        private ulong _totalBytes;
        private ulong _freeBytes;

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

        public VolumeInfo()
        {

        }
    }
}
