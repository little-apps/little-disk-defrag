using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using Little_Disk_Defrag.Helpers;
using Little_Disk_Defrag.Misc;

namespace Little_Disk_Defrag
{

    /// <summary>
    /// Interaction logic for Drawing.xaml
    /// </summary>
    public partial class Drawing : ContentControl
    {
        #region Colors
        private static readonly uint ColWhite = 0xFFFFFF;
        private static readonly uint ColBG = 0xFFFFFF;

        private static uint ColDir = 0xCC0000;
        private static readonly uint ColFile = 0x009900;
        private static uint ColFileFixed = 0x005500;
        private static uint ColFrag = 0x0000FF;
        private static uint ColFragFixed = 0x0000CC;
        private static uint ColMFT = 0xCC00CC;
        private static uint ColMFTFrag = 0x990099;
        private static uint ColComp = 0x00CCFF;
        private static uint ColCompFrag = 0x0066FF;
        private static uint ColLocked = 0x003399;
        private static readonly uint ColMarks = 0x99CCCC;
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
            get { return _blockSize; }
            set
            {
                _blockSize = value;
            }
        }

        readonly ulong colorSteps = 6;

        int blocksPerLine;
        int blockLines;

        ulong clustersPerLine;
        ulong clustersPerBlock;
        ulong blockCount;

        bool sizesCalculated;

        private static double ThreadSafeWidth = double.NaN;

        private readonly Draw draw;

        private Task _drawTask;
        private CancellationTokenSource _cancellationTokenSource;

        public Task DrawTask
        {
            get
            {
                if (_drawTask == null)
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                    _drawTask = new Task(DrawBlocks, _cancellationTokenSource.Token);
                }
                    
                return _drawTask;
            }
        }

        private DispatcherTimer timer;

        private DriveVolume Volume { get; set; }

        public Drawing()
        {
            InitializeComponent();

            draw = new Draw();

            Content = draw;

            BlockSize = 9;

            ColF1 = Utils.LighterVal(ColFile, 32);
            ColF2 = Utils.LighterVal(ColFile, 64);
            ColF3 = Utils.LighterVal(ColFile, 112);
            ColF4 = Utils.LighterVal(ColFile, 144);
            ColF5 = Utils.LighterVal(ColFile, 176);
        }

        public void SetDriveVolume(DriveVolume vol)
        {
            Volume = vol;

            Redraw();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            Content = draw;

            timer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 250), IsEnabled = true };
            timer.Tick += RefreshDrawing;
        }

        public void RefreshDrawing(object o, EventArgs e)
        {
            if (DrawTask.Status == TaskStatus.Running)
            {
                draw.InvalidateVisual();
            }
            
        }

        public void ChangeSize(double width, double height)
        {
            if (double.IsNaN(width) || double.IsNaN(height))
                return;

            Width = width;

            Height = height;
        }

        public async void Redraw()
        {
            if (DrawTask.Status == TaskStatus.Running)
                _cancellationTokenSource?.Cancel();

            await Task.Run(() => { while (DrawTask.Status == TaskStatus.Running); });

            // Clear canvas
            draw.Clear();

            CalculateSizes(Width, Height);

            if (Volume != null && sizesCalculated)
            {
                //this._drawThread = new Thread(new ThreadStart(this.DrawBlocks));
                //this.DrawThread.Start();

                _cancellationTokenSource = new CancellationTokenSource();
                _drawTask = new Task(DrawBlocks, _cancellationTokenSource.Token);

                DrawTask.Start();

                await DrawTask;

                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void CalculateSizes(double width, double height)
        {
            if (Volume == null)
                return;

            if (double.IsNaN(width) || double.IsNaN(height))
                return;

            blocksPerLine = (int)((width) / (BlockSize));
            blockLines = (int)((height - (30 + BlockSize)) / (BlockSize));

            if (blockLines < 1)
                blockLines = 1; // minimum size: 1 line
            if (blocksPerLine < 1)
                blocksPerLine = 1; // minimum size: 1 line

            clustersPerBlock = 0;
            while ((ulong)blocksPerLine * (ulong)blockLines * clustersPerBlock < Volume.PartInfo.ClusterCount)
                clustersPerBlock++;

            clustersPerLine = (ulong)blocksPerLine * clustersPerBlock;
            blockCount = (ulong)blocksPerLine * (ulong)blockLines;

            if (!sizesCalculated)
                sizesCalculated = true;
        }

        public void DrawBlocks()
        {
            if (!Volume.BitmapLoaded)
                return;

            //if (Drawing.IsDrawing)
            //    return;

            // Wait until width is not NaN
            while (double.IsNaN(GetWidth()))
            {
                
            }

            ThreadSafeWidth = GetWidth();

            int currentX = 0;
            int currentY = 0;

            uint BytesReturned = 0;

            uint BitmapSize = (65536 + 2 * sizeof(ulong));

            int err;

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

            int[] BitShift = { 1, 2, 4, 8, 16, 32, 64, 128 };

            ulong Max = Utils.Min(Volume.PartInfo.ClusterCount, 8 * 65536);

            do
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                    break;

                GCHandle handle = GCHandle.Alloc(currLcn, GCHandleType.Pinned);
                var CurrentLCNPtr = handle.AddrOfPinnedObject();

                var pDest = Marshal.AllocHGlobal((int)BitmapSize);

                PInvoke.DeviceIoControl(
                    Volume.Handle,
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

                ulong i;
                for (i = 0; i < Max; i++)
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                        break;

                    if ((BitmapBuffer.Buffer[i / 8] & BitShift[i % 8]) > 0)
                    {
                        // Cluster is used
                        if (cluster.HasValue)
                        {
                            if ((lastLcn.HasValue) && cluster.Value == lastLcn.Value)
                            {
                                lastLcn = null;
                            }
                            else
                            {
                                numFree2 += numFree;
                                //cFreeSpaceGaps++;
                                if (BlockSize == 1)
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

                            if (BlockSize == 1)
                            {
                                DrawBlocks(lastCluster, cluster.Value, ColFile);
                            }

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

                    if (clusterCount == clustersPerBlock)
                    {
                        clusterCount2 += clustersPerBlock;
                    }

                    // prevent drawing as one big block
                    if (clusterCount2 >= clustersPerLine && BlockSize == 1)
                    {
                        if (cluster2.HasValue && numFree > clustersPerLine)
                            DrawBlocks(cluster2.Value, cluster2.Value + numFree, ColWhite);
                        else if (numFree == 0)
                            DrawBlocks(currLcn + i - clustersPerLine, currLcn + i, ColFile);
                        clusterCount2 = 0;
                    }

                    // draw the shit
                    if (clusterCount >= clustersPerBlock)
                    {
                        if (BlockSize != 1)
                        {
                            DrawNoBlockBound(currentX, currentY); // just make sure all is new
                            if (numFree3 >= clustersPerBlock)
                                DrawBlockAt(currentX, currentY, ColWhite);
                            else if (numFree3 == 0)
                                DrawBlockAt(currentX, currentY, ColFile);
                            else if (numFree3 >= (clustersPerBlock / colorSteps) * 4)
                                DrawBlockAt(currentX, currentY, ColF5);
                            else if (numFree3 >= (clustersPerBlock / colorSteps) * 3)
                                DrawBlockAt(currentX, currentY, ColF4);
                            else if (numFree3 >= (clustersPerBlock / colorSteps) * 2)
                                DrawBlockAt(currentX, currentY, ColF3);
                            else if (numFree3 >= (clustersPerBlock / colorSteps))
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

                            currentX += BlockSize;
                            if (currentX > (int)(ThreadSafeWidth - BlockSize))
                            {
                                // move to next y-line
                                currentX = 0;
                                currentY += BlockSize;
                            }
                            numFree3 = 0;
                        }

                        clusterCount = 0;
                    }
                } // for all clusters in this pair

                // Move to the next block
                currLcn = BitmapBuffer.StartingLcn.QuadPart + i;
            } while ((err == PInvoke.ERROR_MORE_DATA) && (currLcn < Volume.PartInfo.ClusterCount));

            if (clusterCount > 0)
            {
                //cFreeSpaceGaps++;

                // draw last cluster
                if (BlockSize > 1)
                {
                    DrawNoBlockBound(currentX, currentY);
                    if (numFree3 == clusterCount)
                    {
                        DrawBlockAt(currentX, currentY, ColWhite);
                    }
                    else
                    {
                        if (numFree3 > 0) {
                            uint col = Utils.LighterVal(ColFile, (int)(((float)((int)(clustersPerBlock - (clustersPerBlock - numFree3))) / (int)clustersPerBlock) * 255.0));
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
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.Invoke(() => draw.InvalidateVisual());
            }
            else
            {
                draw.InvalidateVisual();
            }
        }

        private double GetWidth()
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                return Dispatcher.Invoke(new Func<double>(GetWidth));
            }

            return Width;
        }

        public void DrawClustersAt(int x1, int y1, int x2, int y2, uint color)
        {
            double width;
            double height;


            while (y1 < y2)
            {
                width = BlockSize - 1;
                height = y1 * BlockSize + BlockSize;

                if (BlockSize > 1)
                    height -= 1;

                AddBlock(x1, y1 * BlockSize, width, height, color);

                x1 = 0;
                y1++;
            }

            width = x2;
            height = y2 * (BlockSize * 2);

            if (BlockSize > 1) 
                height -= 1;

            AddBlock(x1, y1 * BlockSize, width, height, color);
        }

        public void DrawBlockAt(int x, int y, uint color)
        {
            AddBlock(x+1, y+1, BlockSize - 1, BlockSize, color);
        }

        private void DrawNoBlockBound(int x, int y)
        {
            if (BlockSize == 1)
                return;

            AddBlock(x, y, x + BlockSize - 1, y + BlockSize - 1, ColBG);
        }

        private void DrawBlockBound(int x, int y)
        {
            AddBlock(x, y, (x + BlockSize - 1) + 2, (y + BlockSize - 1) + 2, ColBG);
        }

        private void DrawBlockBounds(ulong clusterStart, ulong clusterEnd, bool clear)
        {
            ulong ui;
            ulong x = 0;
            ulong y = 0;

            if (clusterStart > Volume.PartInfo.ClusterCount && clusterEnd > Volume.PartInfo.ClusterCount)
                return;
            if (clusterStart > Volume.PartInfo.ClusterCount) 
                return;

            if (clusterEnd > Volume.PartInfo.ClusterCount)
                clusterEnd = Volume.PartInfo.ClusterCount;

            while (y / (ulong)BlockSize * clustersPerLine <= clusterStart)
                y += (ulong)BlockSize;
            y -= (ulong)BlockSize;

            while (x / (ulong)BlockSize * clustersPerBlock + y / (ulong)BlockSize * clustersPerLine <= clusterStart)
                x += (ulong)BlockSize;
            x -= (ulong)BlockSize;

            if (!clear) 
                DrawBlockBound((int)x, (int)y);
            else 
                DrawNoBlockBound((int)x, (int)y);

            clusterStart += clustersPerBlock;

            for (ui = clusterStart; ui <= clusterEnd; ui += clustersPerBlock)
            {
                x += (ulong)BlockSize;
                if (x > ThreadSafeWidth - BlockSize) 
                { 
                    x = 0; 
                    y += (ulong)BlockSize; 
                }
                
                if (clear) 
                    DrawNoBlockBound((int)x, (int)y);
                else
                    DrawBlockBound((int)x, (int)y);
            }
        }

        public void DrawBlocks(ulong clusterStart, ulong clusterEnd, uint color)
        {
            int lastx1 = 0;
            int x1;
            int y1;

            if (Volume.PartInfo.ClusterCount == 0 || clusterEnd == 0) 
                return;

            if (clusterStart > Volume.PartInfo.ClusterCount) 
                return;

            if (clusterEnd > Volume.PartInfo.ClusterCount)
                clusterEnd = Volume.PartInfo.ClusterCount;

            if (BlockSize == 1)
            {
                x1 = BlockSize * (int)((clusterStart - clusterStart / clustersPerLine * clustersPerLine) / clustersPerBlock);
                y1 = (BlockSize * (int)(clusterStart / clustersPerLine)) / BlockSize;
                var x2 = BlockSize * (int)((clusterEnd - clusterEnd / clustersPerLine * clustersPerLine) / clustersPerBlock);
                var y2 = (BlockSize * (int)(clusterEnd / clustersPerLine)) / BlockSize;

                if (x1 == lastx1)
                    //  && x2-x1>1 && y2-y1==0
                    x1 = x1 - 1;

/*
                lastx1 = x2;
*/

                if (x2 - x1 <= 1 && y2 - y1 == 0)
                    DrawClustersAt(x1, y1, x1 + 1, y2, color);
                else
                    DrawClustersAt(x1 + 1, y1, x2 + 1, y2, color);
            }
            else
            {
                // find the start coords
                x1 = BlockSize * (int)((clusterStart - clusterStart / clustersPerLine * clustersPerLine) / clustersPerBlock);
                y1 = (BlockSize * (int)(clusterStart / clustersPerLine));

                DrawBlockAt(x1, y1, color);
                ulong ui;
                for (ui = clusterStart; ui + (clustersPerBlock) <= clusterEnd; ui += clustersPerBlock)
                {
                    x1 += BlockSize;
                    if (x1 > Width - BlockSize)
                    {
                        x1 = 0;
                        y1 += BlockSize;
                    }

                    DrawBlockAt(x1, y1, color);
                }
            }
            
        }

        private void DrawMarks(ulong clusterStart, ulong clusterEnd)
        {
            int x1 = 0;
            int y1 = 0;
            int h = (int)Height;
            int w = (int)Width;

            if (BlockSize < 5) 
                return;
            if (Volume.PartInfo.ClusterCount == 0 || clusterEnd == 0) 
                return;
            if (clusterStart > Volume.PartInfo.ClusterCount && clusterEnd > Volume.PartInfo.ClusterCount) 
                return;
            if (clusterStart > Volume.PartInfo.ClusterCount) 
                return;
            if (clusterEnd > Volume.PartInfo.ClusterCount)
                clusterEnd = Volume.PartInfo.ClusterCount;

            if (BlockSize == 1)
            {
                x1 = BlockSize * (int)((clusterStart - clusterStart / clustersPerLine * clustersPerLine) / clustersPerBlock);
                y1 = (BlockSize * (int)(clusterStart / clustersPerLine)) / BlockSize;
                var x2 = BlockSize * (int)((clusterEnd - clusterEnd / clustersPerLine * clustersPerLine) / clustersPerBlock);
                var y2 = (BlockSize * (int)(clusterEnd / clustersPerLine)) / BlockSize;
                DrawClusterMarkAt(x1 + 1, y1, x2, y2, ColMarks);
            }
            else
            {
                while ((ulong)y1 / (ulong)BlockSize * clustersPerLine <= clusterStart)
                    y1 += BlockSize;
                y1 -= BlockSize;

                while ((ulong)x1 / (ulong)BlockSize * clustersPerBlock + (ulong)y1 / (ulong)BlockSize * clustersPerLine <= clusterStart)
                    x1 += BlockSize;
                x1 -= BlockSize;

                DrawMarkAt(x1, y1, false); // start mark
                clusterStart += clustersPerBlock;

                ulong ui;
                for (ui = clusterStart; ui + (clustersPerBlock) <= clusterEnd; ui += clustersPerBlock)
                {
                    x1 += BlockSize;
                    if (x1 > Width - BlockSize) 
                    { 
                        x1 = 0; 
                        y1 += BlockSize; 
                    }
                    DrawMarkLineAt(x1, y1);
                }

                x1 += BlockSize;

                if (x1 > Width - BlockSize) 
                {
                    x1 = 0; 
                    y1 += BlockSize; 
                }

                DrawMarkAt(x1, y1, true); // end mark
            }
        }

        private void DrawClusterMarkAt(int x1, int y1, int x2, int y2, uint col) 
        {
            // Perpendicular
            AddLine(x1, y1 * BlockSize, x1, y1 * BlockSize + BlockSize - 2, col);

            while (y1 < y2)
            {
                AddLine(x1, y1 * BlockSize + BlockSize / 2 - 1, Width - 1, y1 * BlockSize + BlockSize / 2 - 1, col);

                x1 = 0;
                y1++;
            }

            AddLine(x1, y2 * BlockSize + BlockSize / 2 - 1, x2, y2 * BlockSize + BlockSize / 2 - 1, col);

            // Perpendicular
            AddLine(x2, y2 * BlockSize + 1, x2, y2 * BlockSize + BlockSize - 2, col);
        }

        private void DrawMarkLineAt(int x, int y)
        {
            x++;
            y++;

            AddLine(x, y + BlockSize / 2 - 1, x + BlockSize - 1, y + BlockSize / 2 - 1, ColMarks);
            AddLine(x, y + BlockSize / 2 - 1, x + BlockSize - 1, y + BlockSize / 2 - 1, ColMarks);
        }

        private void DrawMarkAt(int x, int y, bool start)
        {
            x++;
            y++;

            // vertically centered
            AddLine(x + BlockSize / 2 - 1, y + 1, x + BlockSize / 2 - 1, y + BlockSize - 2, ColMarks);
            // vertically centered
            AddLine(x + BlockSize / 2 - 1, y + 1, x + BlockSize / 2 - 1, y + BlockSize - 2, ColMarks);

            if (start)
            {
                // horizontally center to right |-
                AddLine(x, y + BlockSize / 2 - 1, x + BlockSize / 2 - 1, y + BlockSize / 2 - 1, ColMarks);
                // horizontally center to right |-
                AddLine(x, y + BlockSize / 2 - 1, x + BlockSize / 2 - 1, y + BlockSize / 2 - 1, ColMarks);
            }
            else
            {
                // horizontally left to center -|
                AddLine(x + BlockSize / 2 - 1, y + BlockSize / 2 - 1, x + BlockSize - 1, y + BlockSize / 2 - 1, ColMarks);
                // horizontally left to center -|
                AddLine(x + BlockSize / 2 - 1, y + BlockSize / 2 - 1, x + BlockSize - 1, y + BlockSize / 2 - 1, ColMarks);
            }
        }

        private void AddLine(double startX, double startY, double endX, double endY, uint color)
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.Invoke(new Action<double, double, double, double, uint>(AddBlock), startX, startY, endX, endY, color);
                return;
            }

            draw.DrawLine(startX, startY, endX, endY, color);
        }

        private void AddBlock(double left, double top, double width, double height, uint color)
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.Invoke(new Action<double, double, double, double, uint>(AddBlock), left, top, width, height, color);
                return;
            }

            draw.DrawBlock(left, top, width, height, color);
        }

    }
}
