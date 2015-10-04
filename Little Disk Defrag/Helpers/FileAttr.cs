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

using System.IO;

namespace Little_Disk_Defrag.Helpers
{
    public class FileAttr
    {
        public bool Archive;
        public bool Compressed;
        public bool Directory;
        public bool Encrypted;
        public bool Hidden;
        public bool Normal;
        public bool Offline;
        public bool ReadOnly;
        public bool Reparse;
        public bool Sparse;
        public bool System;
        public bool Temporary;

        // For defragmenting purposes and other information
        public bool AccessDenied = false;  // could we not open it?
        public bool Unmovable = false;  // can we even touch it?
        public bool Process = true;  // should we process it?

        public FileAttr(FileAttributes fileAttrs)
        {
            Archive = ((fileAttrs & FileAttributes.Archive) != 0);
            Compressed = ((fileAttrs & FileAttributes.Compressed) != 0);
            Directory = ((fileAttrs & FileAttributes.Directory) != 0);
            Encrypted = ((fileAttrs & FileAttributes.Encrypted) != 0);
            Hidden = ((fileAttrs & FileAttributes.Hidden) != 0);
            Normal = ((fileAttrs & FileAttributes.Normal) != 0);
            Offline = ((fileAttrs & FileAttributes.Offline) != 0);
            ReadOnly = ((fileAttrs & FileAttributes.ReadOnly) != 0);
            Reparse = ((fileAttrs & FileAttributes.ReparsePoint) != 0);
            Sparse = ((fileAttrs & FileAttributes.SparseFile) != 0);
            System = ((fileAttrs & FileAttributes.System) != 0);
            Temporary = ((fileAttrs & FileAttributes.Temporary) != 0);
        }
    }
}
