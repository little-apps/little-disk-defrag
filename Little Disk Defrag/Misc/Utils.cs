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

using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace Little_Disk_Defrag.Misc
{
    public class Utils
    {
        internal static ulong Min (ulong a, ulong b) {
            return (a < b ? a : b);
        }

        internal static ulong Max(ulong a, ulong b) {
            return (a > b ? a : b);
        }

        internal static void CloseHandle(IntPtr handle)
        {
            if (handle.ToInt32() == -1 || handle == IntPtr.Zero)
                return;

            try
            {
                PInvoke.CloseHandle(handle);
            }
            catch (SEHException ex)
            {
                Debug.WriteLine("The following error occurred: {0}\nIs the handle trying to be closed twice?", ex.Message);
            }
            
            handle = IntPtr.Zero;
        }

        internal static long? FileSeek(SafeFileHandle handle, long offset, SeekOrigin origin)
        {
            uint moveMethod = 0;

            switch (origin)
            {
                case SeekOrigin.Begin:
                    moveMethod = 0;
                    break;

                case SeekOrigin.Current:
                    moveMethod = 1;
                    break;

                case SeekOrigin.End:
                    moveMethod = 2;
                    break;
            }

            int lo = (int)(offset & 0xffffffff);
            int hi = (int)(offset >> 32);

            lo = PInvoke.SetFilePointer(handle, lo, out hi, moveMethod);

            if (lo == -1)
            {
                if (Marshal.GetLastWin32Error() != 0)
                {
                    return null;
                }
            }

            return (((long)hi << 32) | (uint)lo);
        }

        internal static Color HexToColor(uint argb)
        {
            Color col = Color.FromRgb((byte)((argb & 0xff0000) >> 0x10), (byte)((argb & 0xff00) >> 0x08), (byte)(argb & 0xff));

            return col;
        }

        internal static uint LighterVal(uint col, int val)
        {
            uint r, g, b;

            b = ((col >> 16) & 0xFF) + (uint)((float)val / 255 * 256);
            g = ((col >> 8) & 0xFF) + (uint)((float)val / 255 * 256);
            r = ((col & 0xFF) + (uint)((float)val / 255 * 256));

            if (r > 255)
                r = 255;

            if (g > 255) 
                g = 255;

            if (b > 255)
                b = 255;

            col = (b << 16) | (g << 8) | r;

            return col;
        }

        internal static uint ColorToUInt(Color col)
        {
            return (uint)(((col.A << 24) | (col.R << 16) | (col.G << 8) | col.B) & 0xffffffffL);
        }

        internal static string FitName (string path, string filename, int totalWidth)
        {
	        int pathLen=0;
	        int fnLen=0;
	        int halfTotLen=0;
	        int len4fn=0;     /* number of chars remaining for filename after path is applied */
	        int len4path=0;   /* number of chars for path before filename is applied          */

	        pathLen = path.Length;
	        fnLen = filename.Length;
	        if ((totalWidth % 2) <= 0)
		        halfTotLen=totalWidth / 2;
	        else
		        halfTotLen=(totalWidth-1) / 2;  /* -1 because otherwise (halfTotLen*2) == (totalWidth+1) which wouldn't be good */

	        /* determine how much width the path and filename each get */
	        if ( (pathLen >= halfTotLen) && (fnLen < halfTotLen) )
	        {
		        len4fn = fnLen;
		        len4path = (totalWidth - len4fn);
	        }

	        if ( (pathLen < halfTotLen) && (fnLen < halfTotLen) )
	        {
		        len4fn = fnLen;
		        len4path = pathLen;
	        }

	        if ( (pathLen >= halfTotLen) && (fnLen >= halfTotLen) )
	        {
		        len4fn = halfTotLen;
		        len4path = halfTotLen;
	        }

	        if ( (pathLen < halfTotLen) && (fnLen >= halfTotLen) )
	        {
		        len4path = pathLen;
		        len4fn = (totalWidth - len4path);
	        }
	        /*
		        if halfTotLen was adjusted above to avoid a rounding error, give the
		        extra wchar_t to the filename
	        */
	        if (halfTotLen < (totalWidth/2)) len4path++;

            if (pathLen > len4path)
                path = path.Substring(0, len4path - 4) + "...\\";

            if (fnLen > len4fn)
                filename = filename.Substring(0, len4fn-3) + "...";

            return string.Copy(path + filename);
        }

        internal static string ProductName
        {
            get
            {
                return App.ResourceAssembly.GetName().Name;
            }
        }
    }
}
