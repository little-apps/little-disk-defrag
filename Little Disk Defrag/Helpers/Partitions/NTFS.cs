using Little_Disk_Defrag.Helpers;
using Little_Disk_Defrag.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Little_Disk_Defrag.Helpers.Partitions
{
    public class NTFS : PartInfo
    {
        private readonly LinkedList<MFTEntry> _mftEntries;
        private ulong _mftStartLcn;
        private ulong _mft2StartLcn;
        private ulong _bytesPerMftRecord;
        private ulong _clustersPerIndexRecord;

        public LinkedList<MFTEntry> MFTEntries
        {
            get { return this._mftEntries; }
        }

        public NTFS(DriveVolume volume) 
            : base(volume)
        {
            this._mftEntries = new LinkedList<MFTEntry>();
        }

        public override bool GetPartitionDetails()
        {
            throw new NotImplementedException();
            return false;
        }
    }
}
