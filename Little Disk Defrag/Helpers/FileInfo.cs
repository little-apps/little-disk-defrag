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
            get { return this._name; }
        }

        /// <summary>
        /// Indice into directory list
        /// </summary>
        public uint DirIndice
        {
            get { return this._dirIndice; }
        }

        public UInt64 Size
        {
            get { return this._size; }
        }
        public UInt64 Clusters
        {
            get { return this._clusters; }
            set { this._clusters = value; }
        }
        public FileAttr Attributes
        {
            get { return this._attributes; }
        }
        public List<Extent> Fragments
        {
            get { return this._fragments; }
        }

        public FileInfo(string name, uint indice, UInt64 size, System.IO.FileAttributes fileAttrs)
        {
            this._name = name;
            this._dirIndice = indice;
            this._size = size;

            this._fragments = new List<Extent>();
            this._attributes = new FileAttr(fileAttrs);
        }
    }
}
