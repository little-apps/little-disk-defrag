using Little_Disk_Defrag.Helpers;
using Little_Disk_Defrag.Misc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Little_Disk_Defrag
{

    /// <summary>
    /// Interaction logic for Drawing.xaml
    /// </summary>
    public partial class Drawing : ContentControl
    {
        #region Colors
        private static uint ColWhite = 0xFFFFFF;
        private static uint ColBG = 0xFFFFFF;

        private static uint ColDir = 0xCC0000;
        private static uint ColFile = 0x009900;
        private static uint ColFileFixed = 0x005500;
        private static uint ColFrag = 0x0000FF;
        private static uint ColFragFixed = 0x0000CC;
        private static uint ColMFT = 0xCC00CC;
        private static uint ColMFTFrag = 0x990099;
        private static uint ColComp = 0x00CCFF;
        private static uint ColCompFrag = 0x0066FF;
        private static uint ColLocked = 0x003399;
        private static uint ColMarks = 0x99CCCC;
        private static uint ColWrite = 0xFFCCCC;
        private static uint ColWriteDone = 0xFF6666;
        private static uint ColRead = 0x00FFFF;
        private static uint ColReadDone = 0x66DDDD;

        private static uint ColF1;
        private static uint ColF2;
        private static uint ColF3;
        private static uint ColF4;
        private static uint ColF5;
        #endregion

        int _blockSize;

        public int BlockSize
        {
            get { return this._blockSize; }
            set
            {
                if (this._blockSize == value)
                    return;

                this._blockSize = value;
            }
        }

        ulong colorSteps = 6;

        int blocksPerLine;
        int blockLines;

        ulong clustersPerLine;
        ulong clustersPerBlock;
        ulong blockCount;

        bool sizesCalculated = false;

        private static double ThreadSafeWidth = double.NaN;

        private DriveVolume _volume;

        private Draw draw;

        private Thread _drawThread;

        public Thread DrawThread
        {
            get
            {
                if (this._drawThread == null)
                    this._drawThread = new Thread(new ThreadStart(this.DrawBlocks));

                return this._drawThread;
            }
        }

        private object _drawLock = new object();

        private DispatcherTimer timer;

        private DriveVolume Volume
        {
            get { return this._volume; }
        }

        public Drawing()
        {
            InitializeComponent();

            this.draw = new Draw();

            this.Content = this.draw;

            this.BlockSize = 9;

            ColF1 = Utils.LighterVal(ColFile, 32);
            ColF2 = Utils.LighterVal(ColFile, 64);
            ColF3 = Utils.LighterVal(ColFile, 112);
            ColF4 = Utils.LighterVal(ColFile, 144);
            ColF5 = Utils.LighterVal(ColFile, 176);
        }

        public void SetDriveVolume(DriveVolume vol)
        {
            this._volume = vol;

            this.Redraw();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            this.Content = this.draw;

            this.timer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 750), IsEnabled = true };
            this.timer.Tick += RefreshDrawing;
        }

        public void RefreshDrawing(object o, EventArgs e)
        {
            if (this.DrawThread.IsAlive)
            {
                this.draw.InvalidateVisual();
            }
            
        }

        public void ChangeSize(double width, double height)
        {
            if (double.IsNaN(width) || double.IsNaN(height))
                return;

            if (width != this.Width)
                this.Width = width;

            if (height != this.Height)
                this.Height = height;
        }

        public void Redraw()
        {
            lock (this._drawLock)
            {
                if (this.DrawThread.IsAlive)
                    this.DrawThread.Abort();

                // Clear canvas
                this.draw.Clear();

                this.CalculateSizes(this.Width, this.Height);

                if (this.Volume != null && this.sizesCalculated)
                {
                    this._drawThread = new Thread(new ThreadStart(this.DrawBlocks));
                    this.DrawThread.Start();
                }
            }
        }

        private void CalculateSizes(double width, double height)
        {
            if (this.Volume == null)
                return;

            if (double.IsNaN(width) || double.IsNaN(height))
                return;

            this.blocksPerLine = (int)((width) / (this.BlockSize));
            this.blockLines = (int)((height - (30 + this.BlockSize)) / (this.BlockSize));

            if (this.blockLines < 1)
                this.blockLines = 1; // minimum size: 1 line
            if (this.blocksPerLine < 1)
                this.blocksPerLine = 1; // minimum size: 1 line

            this.clustersPerBlock = 0;
            while ((ulong)this.blocksPerLine * (ulong)this.blockLines * this.clustersPerBlock < this.Volume.VolInfo.ClusterCount)
                this.clustersPerBlock++;

            this.clustersPerLine = (ulong)this.blocksPerLine * (ulong)this.clustersPerBlock;
            this.blockCount = (ulong)this.blocksPerLine * (ulong)this.blockLines;

            if (!this.sizesCalculated)
                this.sizesCalculated = true;
        }

        public void DrawBlocks()
        {
            if (!this.Volume.BitmapLoaded)
                return;

            //if (Drawing.IsDrawing)
            //    return;

            // Wait until width is not NaN
            while (double.IsNaN(this.GetWidth()))
                continue;

            Drawing.ThreadSafeWidth = this.GetWidth();

            int currentX = 0;
            int currentY = 0;

            IntPtr CurrentLCNPtr;
            IntPtr pDest;

            uint BytesReturned = 0;

            uint BitmapSize = (65536 + 2 * sizeof(ulong));

            int err;

            ulong i;
            ulong currLcn = 0;
            ulong? lastLcn = 0;
            ulong lastCluster = 0;
            ulong? cluster = null;
            ulong? cluster2 = null;
            ulong clusterCount = 0;
            ulong clusterCount2 = 0;
            ulong numFree = 0;
            ulong numFree2 = 0;
            ulong numFree3 = 0;

            int[] BitShift = new int[] { 1, 2, 4, 8, 16, 32, 64, 128 };

            ulong Max = Utils.Min(this.Volume.VolInfo.ClusterCount, 8*65536);

            do
            {
                GCHandle handle = GCHandle.Alloc(currLcn, GCHandleType.Pinned);
                CurrentLCNPtr = handle.AddrOfPinnedObject();

                pDest = Marshal.AllocHGlobal((int)BitmapSize);

                PInvoke.DeviceIoControl(
                    this.Volume.Handle,
                    PInvoke.FSConstants.FSCTL_GET_VOLUME_BITMAP,
                    CurrentLCNPtr,
                    (uint)Marshal.SizeOf(currLcn),
                    pDest,
                    BitmapSize,
                    ref BytesReturned,
                    IntPtr.Zero);

                err = Marshal.GetLastWin32Error();

                if (err != PInvoke.ERROR_MORE_DATA && err != PInvoke.ERROR_HANDLE_EOF)
                    break;

                PInvoke.VOLUME_BITMAP_BUFFER BitmapBuffer = new PInvoke.VOLUME_BITMAP_BUFFER(pDest, (int)BitmapSize);

                for (i = 0; i < Max; i++)
                {
                    if ((BitmapBuffer.Buffer[i / 8] & BitShift[i % 8]) > 0)
                    {
                        // Cluster is used
                        if (cluster.HasValue)
                        {
                            if ((lastLcn.HasValue) && cluster.Value == lastLcn.Value)
                            {
                                lastLcn = null;
                            }
                            else if (cluster2.HasValue)
                            {
                                numFree2 += numFree;
                                //cFreeSpaceGaps++;
                                if (this.BlockSize == 1)
                                    DrawBlocks(cluster2.Value, cluster2.Value + numFree2, 0xFFFFFF);

                                lastCluster = cluster2.Value + numFree2;
                                lastLcn = cluster;
                                numFree = 0;
                                numFree2 = 0;
                                clusterCount2 = 0;
                                cluster = null;
                                cluster2 = null;
                            }
                        }
                    }
                    else
                    {
                        // Cluster is free
                        if (!cluster.HasValue)
                        {
                            cluster = currLcn + i;

                            if (this.BlockSize == 1)
                            {
                                this.DrawBlocks(lastCluster, cluster.Value, ColFile);
                            }

                            if (!cluster2.HasValue)
                                cluster2 = cluster;

                            numFree = 1;
                            numFree3++;
                        }
                        else
                        {
                            numFree++;
                            numFree3++;
                        }

                    }

                    clusterCount++;

                    if (clusterCount == this.clustersPerBlock)
                    {
                        clusterCount2 += this.clustersPerBlock;
                    }

                    // prevent drawing as one big block
                    if (clusterCount2 >= this.clustersPerLine && this.BlockSize == 1)
                    {
                        if (cluster2.HasValue && numFree > this.clustersPerLine)
                            DrawBlocks(cluster2.Value, cluster2.Value + numFree, ColWhite);
                        else if (numFree == 0)
                            DrawBlocks(currLcn + i - this.clustersPerLine, currLcn + i, ColFile);
                        clusterCount2 = 0;
                    }

                    // draw the shit
                    if (clusterCount >= this.clustersPerBlock)
                    {
                        if (this.BlockSize != 1)
                        {
                            DrawNoBlockBound(currentX, currentY); // just make sure all is new
                            if (numFree3 >= this.clustersPerBlock)
                                DrawBlockAt(currentX, currentY, ColWhite);
                            else if (numFree3 == 0)
                                DrawBlockAt(currentX, currentY, ColFile);
                            else if (numFree3 >= (this.clustersPerBlock / this.colorSteps) * 4)
                                DrawBlockAt(currentX, currentY, ColF5);
                            else if (numFree3 >= (this.clustersPerBlock / this.colorSteps) * 3)
                                DrawBlockAt(currentX, currentY, ColF4);
                            else if (numFree3 >= (this.clustersPerBlock / this.colorSteps) * 2)
                                DrawBlockAt(currentX, currentY, ColF3);
                            else if (numFree3 >= (this.clustersPerBlock / this.colorSteps))
                                DrawBlockAt(currentX, currentY, ColF2);
                            else
                                DrawBlockAt(currentX, currentY, ColF1);
                            /*
                            else
                                for (j=colorsteps-1; j>0; j--)
                                    if (numFree3 >= (int)(clustersPB/colorsteps)*j || j==1) {
                                        DrawBlockAt(x, y, LighterVal(ColFile, 256/(colorsteps-j+1)-(colorsteps-j)  ));
                                        break;
                                    }
                            */

                            currentX += this.BlockSize;
                            if (currentX > (int)(Drawing.ThreadSafeWidth - this.BlockSize))
                            {
                                // move to next y-line
                                currentX = 0;
                                currentY += this.BlockSize;
                            }
                            numFree3 = 0;
                        }

                        clusterCount = 0;
                    }
                } // for all clusters in this pair

                // Move to the next block
                currLcn = BitmapBuffer.StartingLcn.QuadPart + i;
            } while ((err == PInvoke.ERROR_MORE_DATA) && (currLcn < this.Volume.VolInfo.ClusterCount));

            if (clusterCount > 0)
            {
                //cFreeSpaceGaps++;

                // draw last cluster
                if (this.BlockSize > 1)
                {
                    DrawNoBlockBound(currentX, currentY);
                    if (numFree3 == clusterCount)
                    {
                        DrawBlockAt(currentX, currentY, ColWhite);
                    }
                    else
                    {
                        if (numFree3 > 0) {
                            uint col = Utils.LighterVal(ColFile, (int)(((float)((int)(this.clustersPerBlock - (this.clustersPerBlock - numFree3))) / (int)this.clustersPerBlock) * 255.0));
                            DrawBlockAt(currentX, currentY, col);
                        }
                        else
                            DrawBlockAt(currentX, currentY, ColFile);
                    }
                }
                else
                {
                    if (cluster2.HasValue)
                    {
                        if (numFree > 0)
                            DrawBlocks(cluster2.Value, cluster2.Value + numFree, ColWhite);
                        else
                            DrawBlocks(lastCluster, currLcn, ColFile);
                    }
                }
            }

            // Update window
            if (this.Dispatcher.Thread != Thread.CurrentThread)
            {
                this.Dispatcher.Invoke(new Action(() => this.draw.InvalidateVisual()));
            }
            else
            {
                this.draw.InvalidateVisual();
            }
        }

        private double GetWidth()
        {
            if (this.Dispatcher.Thread != Thread.CurrentThread)
            {
                return (double)this.Dispatcher.Invoke(new Func<double>(this.GetWidth));
            }

            return this.Width;
        }

        public void DrawClustersAt(int x1, int y1, int x2, int y2, uint color)
        {
            double width;
            double height;


            while (y1 < y2)
            {
                width = this.BlockSize - 1;
                height = y1 * this.BlockSize + this.BlockSize;

                if (this.BlockSize > 1)
                    height -= 1;

                this.AddBlock(x1, y1 * this.BlockSize, width, height, color);

                x1 = 0;
                y1++;
            }

            width = x2;
            height = y2 * (this.BlockSize * 2);

            if (this.BlockSize > 1) 
                height -= 1;

            this.AddBlock(x1, y1 * this.BlockSize, width, height, color);
        }

        public void DrawBlockAt(int x, int y, uint color)
        {
            this.AddBlock(x+1, y+1, this.BlockSize - 1, this.BlockSize, color);
        }

        private void DrawNoBlockBound(int x, int y)
        {
            if (this.BlockSize == 1)
                return;

            this.AddBlock(x, y, x + this.BlockSize - 1, y + this.BlockSize - 1, ColBG);
        }

        private void DrawBlockBound(int x, int y)
        {
            this.AddBlock(x, y, (x + this.BlockSize - 1) + 2, (y + this.BlockSize - 1) + 2, ColBG);
        }

        private void DrawBlockBounds(ulong clusterStart, ulong clusterEnd, bool clear)
        {
            ulong ui = 0;
            ulong x = 0;
            ulong y = 0;

            if (clusterStart > this.Volume.VolInfo.ClusterCount && clusterEnd > this.Volume.VolInfo.ClusterCount)
                return;
            if (clusterStart > this.Volume.VolInfo.ClusterCount) 
                return;

            if (clusterEnd > this.Volume.VolInfo.ClusterCount)
                clusterEnd = this.Volume.VolInfo.ClusterCount;

            while (y / (ulong)this.BlockSize * this.clustersPerLine <= clusterStart)
                y += (ulong)this.BlockSize;
            y -= (ulong)this.BlockSize;

            while (x / (ulong)this.BlockSize * this.clustersPerBlock + y / (ulong)this.BlockSize * this.clustersPerLine <= clusterStart)
                x += (ulong)this.BlockSize;
            x -= (ulong)this.BlockSize;

            if (!clear) 
                DrawBlockBound((int)x, (int)y);
            else 
                DrawNoBlockBound((int)x, (int)y);

            clusterStart += this.clustersPerBlock;

            for (ui = clusterStart; ui <= clusterEnd; ui += this.clustersPerBlock)
            {
                x += (ulong)this.BlockSize;
                if (x > Drawing.ThreadSafeWidth - this.BlockSize) 
                { 
                    x = 0; 
                    y += (ulong)this.BlockSize; 
                }
                
                if (clear) 
                    DrawNoBlockBound((int)x, (int)y);
                else
                    DrawBlockBound((int)x, (int)y);
            }
        }

        public void DrawBlocks(ulong clusterStart, ulong clusterEnd, uint color)
        {
            ulong ui;
            int lastx1 = 0;
            int x1 = 0;
            int y1 = 0;
            int x2 = 0;
            int y2 = 0;

            if (this.Volume.VolInfo.ClusterCount == 0 || clusterEnd == 0) 
                return;

            if (clusterStart > this.Volume.VolInfo.ClusterCount) 
                return;

            if (clusterEnd > this.Volume.VolInfo.ClusterCount) 
                clusterEnd = this.Volume.VolInfo.ClusterCount;

            if (this.BlockSize == 1)
            {
                x1 = this.BlockSize * (int)((clusterStart - clusterStart / this.clustersPerLine * this.clustersPerLine) / this.clustersPerBlock);
                y1 = (this.BlockSize * (int)(clusterStart / this.clustersPerLine)) / this.BlockSize;
                x2 = this.BlockSize * (int)((clusterEnd - clusterEnd / this.clustersPerLine * this.clustersPerLine) / this.clustersPerBlock);
                y2 = (this.BlockSize * (int)(clusterEnd / this.clustersPerLine)) / this.BlockSize;

                if (x1 == lastx1)
                    //  && x2-x1>1 && y2-y1==0
                    x1 = x1 - 1;

                lastx1 = x2;

                if (x2 - x1 <= 1 && y2 - y1 == 0)
                    DrawClustersAt(x1, y1, x1 + 1, y2, color);
                else
                    DrawClustersAt(x1 + 1, y1, x2 + 1, y2, color);
            }
            else
            {
                // find the start coords
                x1 = (int)this.BlockSize * (int)((clusterStart - clusterStart / this.clustersPerLine * this.clustersPerLine) / this.clustersPerBlock);
                y1 = ((int)this.BlockSize * (int)(clusterStart / this.clustersPerLine));

                DrawBlockAt(x1, y1, color);
                for (ui = clusterStart; ui + (this.clustersPerBlock) <= clusterEnd; ui += this.clustersPerBlock)
                {
                    x1 += this.BlockSize;
                    if (x1 > this.Width - this.BlockSize)
                    {
                        x1 = 0;
                        y1 += (int)this.BlockSize;
                    }

                    DrawBlockAt(x1, y1, color);
                }
            }
            
        }

        private void DrawMarks(ulong clusterStart, ulong clusterEnd)
        {
            ulong ui;
            int x1 = 0;
            int y1 = 0;
            int x2 = 0;
            int y2 = 0;
            int h = (int)this.Height;
            int w = (int)this.Width;

            if (this.BlockSize < 5) 
                return;
            if (this.Volume.VolInfo.ClusterCount == 0 || clusterEnd == 0) 
                return;
            if (clusterStart > this.Volume.VolInfo.ClusterCount && clusterEnd > this.Volume.VolInfo.ClusterCount) 
                return;
            if (clusterStart > this.Volume.VolInfo.ClusterCount) 
                return;
            if (clusterEnd > this.Volume.VolInfo.ClusterCount) 
                clusterEnd = this.Volume.VolInfo.ClusterCount;

            if (this.BlockSize == 1)
            {
                x1 = this.BlockSize * (int)((clusterStart - clusterStart / this.clustersPerLine * this.clustersPerLine) / this.clustersPerBlock);
                y1 = (this.BlockSize * (int)(clusterStart / this.clustersPerLine)) / this.BlockSize;
                x2 = this.BlockSize * (int)((clusterEnd - clusterEnd / this.clustersPerLine * this.clustersPerLine) / this.clustersPerBlock);
                y2 = (this.BlockSize * (int)(clusterEnd / this.clustersPerLine)) / this.BlockSize;
                DrawClusterMarkAt(x1 + 1, y1, x2, y2, ColMarks);
            }
            else
            {
                while ((ulong)y1 / (ulong)this.BlockSize * this.clustersPerLine <= clusterStart)
                    y1 += this.BlockSize;
                y1 -= this.BlockSize;

                while ((ulong)x1 / (ulong)this.BlockSize * this.clustersPerBlock + (ulong)y1 / (ulong)this.BlockSize * this.clustersPerLine <= clusterStart)
                    x1 += this.BlockSize;
                x1 -= this.BlockSize;

                DrawMarkAt(x1, y1, false); // start mark
                clusterStart += this.clustersPerBlock;

                for (ui = clusterStart; ui + (this.clustersPerBlock) <= clusterEnd; ui += this.clustersPerBlock)
                {
                    x1 += this.BlockSize;
                    if (x1 > this.Width - this.BlockSize) 
                    { 
                        x1 = 0; 
                        y1 += this.BlockSize; 
                    }
                    DrawMarkLineAt(x1, y1);
                }

                x1 += this.BlockSize;

                if (x1 > this.Width - this.BlockSize) 
                {
                    x1 = 0; 
                    y1 += this.BlockSize; 
                }

                DrawMarkAt(x1, y1, true); // end mark
            }
        }

        private void DrawClusterMarkAt(int x1, int y1, int x2, int y2, uint col) 
        {
            // Perpendicular
            this.AddLine(x1, y1 * this.BlockSize, x1, y1 * this.BlockSize + this.BlockSize - 2, col);

            while (y1 < y2)
            {
                this.AddLine(x1, y1 * this.BlockSize + this.BlockSize / 2 - 1, this.Width - 1, y1 * this.BlockSize + this.BlockSize / 2 - 1, col);

                x1 = 0;
                y1++;
            }

            this.AddLine(x1, y2 * this.BlockSize + this.BlockSize / 2 - 1, x2, y2 * this.BlockSize + this.BlockSize / 2 - 1, col);

            // Perpendicular
            this.AddLine(x2, y2 * this.BlockSize + 1, x2, y2 * this.BlockSize + this.BlockSize - 2, col);
        }

        private void DrawMarkLineAt(int x, int y)
        {
            x++;
            y++;

            this.AddLine(x, y + this.BlockSize / 2 - 1, x + this.BlockSize - 1, y + this.BlockSize / 2 - 1, ColMarks);
            this.AddLine(x, y + this.BlockSize / 2 - 1, x + this.BlockSize - 1, y + this.BlockSize / 2 - 1, ColMarks);
        }

        private void DrawMarkAt(int x, int y, bool start)
        {
            x++;
            y++;

            // vertically centered
            this.AddLine(x + this.BlockSize / 2 - 1, y + 1, x + this.BlockSize / 2 - 1, y + this.BlockSize - 2, ColMarks);
            // vertically centered
            this.AddLine(x + this.BlockSize / 2 - 1, y + 1, x + this.BlockSize / 2 - 1, y + this.BlockSize - 2, ColMarks);

            if (start)
            {
                // horizontally center to right |-
                this.AddLine(x, y + this.BlockSize / 2 - 1, x + this.BlockSize / 2 - 1, y + this.BlockSize / 2 - 1, ColMarks);
                // horizontally center to right |-
                this.AddLine(x, y + this.BlockSize / 2 - 1, x + this.BlockSize / 2 - 1, y + this.BlockSize / 2 - 1, ColMarks);
            }
            else
            {
                // horizontally left to center -|
                this.AddLine(x + this.BlockSize / 2 - 1, y + this.BlockSize / 2 - 1, x + this.BlockSize - 1, y + this.BlockSize / 2 - 1, ColMarks);
                // horizontally left to center -|
                this.AddLine(x + this.BlockSize / 2 - 1, y + this.BlockSize / 2 - 1, x + this.BlockSize - 1, y + this.BlockSize / 2 - 1, ColMarks);
            }
        }

        private void AddLine(double startX, double startY, double endX, double endY, uint color)
        {
            if (this.Dispatcher.Thread != Thread.CurrentThread)
            {
                this.Dispatcher.Invoke(new Action<double, double, double, double, uint>(this.AddBlock), startX, startY, endX, endY, color);
                return;
            }

            this.draw.DrawLine(startX, startY, endX, endY, color);
        }

        private void AddBlock(double left, double top, double width, double height, uint color)
        {
            if (this.Dispatcher.Thread != Thread.CurrentThread)
            {
                this.Dispatcher.Invoke(new Action<double, double, double, double, uint>(this.AddBlock), left, top, width, height, color);
                return;
            }

            this.draw.DrawBlock(left, top, width, height, color);
        }

    }
}
