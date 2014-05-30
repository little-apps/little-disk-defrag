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
    public class BuildDBInfo
    {
        private readonly Defragment _defrag;
        private readonly DriveVolume _volume;
        private readonly UInt64 _clusterCount;
        private UInt64 _clusterProgress = 0;

        public Defragment Defrag
        {
            get { return this._defrag; }
        }

        public DriveVolume Volume
        {
            get { return this._volume; }
        }

        public double Percent
        {
            get { return this.Defrag.StatusPercent; }
            set { this.Defrag.StatusPercent = value; }
        }
        public bool QuitMonitor
        {
            get { return this.Defrag.PleaseStop; }
            set { this.Defrag.PleaseStop = value; }
        }

        public UInt64 ClusterCount
        {
            get { return this._clusterCount; }
        }

        public UInt64 ClusterProgress
        {
            get { return this._clusterProgress; }
            set { this._clusterProgress = value; }
        }

        public BuildDBInfo(Defragment defrag, DriveVolume volume, UInt64 clusterCount)
        {
            this._defrag = defrag;
            this._volume = volume;
            this._clusterCount = clusterCount;
        }
    }
}
