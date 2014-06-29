using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Little_Disk_Defrag.Helpers
{
    public class MFTEntry
    {
        public string Longname;
        public ulong filesize;
        public ulong fragid;
        public ulong MFTid;
        public ulong fragments;
        public ulong Lcn;
        public ulong Len;
        public ulong Vcn;
        public bool fragmented;
        public bool locked;

        public MFTEntry()
        {

        }
    }
}
