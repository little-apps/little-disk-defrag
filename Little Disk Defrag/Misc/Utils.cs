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
