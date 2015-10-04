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
        private readonly string _name;
        private readonly uint _dirIndice;
        private readonly UInt64 _size;
        private UInt64 _clusters;
        private readonly FileAttr _attributes;
        private readonly List<Extent> _fragments;

        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Indice into directory list
        /// </summary>
        public uint DirIndice
        {
            get { return _dirIndice; }
        }

        public UInt64 Size
        {
            get { return _size; }
        }
        public UInt64 Clusters
        {
            get { return _clusters; }
            set { _clusters = value; }
        }
        public FileAttr Attributes
        {
            get { return _attributes; }
        }
        public List<Extent> Fragments
        {
            get { return _fragments; }
        }

        public FileInfo(string name, uint indice, UInt64 size, FileAttributes fileAttrs)
        {
            _name = name;
            _dirIndice = indice;
            _size = size;

            _fragments = new List<Extent>();
            _attributes = new FileAttr(fileAttrs);
        }
    }
}
