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

namespace Little_Disk_Defrag.Helpers
{
    public struct Extent
    {
        public UInt64 StartLCN;
        public UInt64 Length;
    }

    public class FileInfo
    {
        public string Name { get; }

        /// <summary>
        /// Indice into directory list
        /// </summary>
        public uint DirIndice { get; }

        public UInt64 Size { get; }

        public UInt64 Clusters { get; set; }

        public FileAttr Attributes { get; }

        public List<Extent> Fragments { get; }

        public FileInfo(string name, uint indice, UInt64 size, FileAttributes fileAttrs)
        {
            Name = name;
            DirIndice = indice;
            Size = size;

            Fragments = new List<Extent>();
            Attributes = new FileAttr(fileAttrs);
        }
    }
}
