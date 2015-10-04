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

namespace Little_Disk_Defrag.Helpers
{
    public class BuildDBInfo
    {
        public Defragment Defrag { get; }

        public DriveVolume Volume { get; }

        public double Percent
        {
            get { return Defrag.StatusPercent; }
            set { Defrag.StatusPercent = value; }
        }
        public bool QuitMonitor
        {
            get { return Defrag.PleaseStop; }
            set { Defrag.PleaseStop = value; }
        }

        public UInt64 ClusterCount { get; }

        public UInt64 ClusterProgress { get; set; }

        public BuildDBInfo(Defragment defrag, DriveVolume volume, UInt64 clusterCount)
        {
            Defrag = defrag;
            Volume = volume;
            ClusterCount = clusterCount;
        }
    }
}
