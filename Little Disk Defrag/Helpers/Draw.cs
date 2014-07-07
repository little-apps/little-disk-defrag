using Little_Disk_Defrag.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Little_Disk_Defrag.Helpers
{
    public class Draw : Control
    {
        public class MyRect
        {
            public Rect Rect;
            public System.Windows.Media.Brush Brush;
        }

        public class MyLine
        {
            public Point StartPoint;
            public Point EndPoint;
            public Pen Pen;
        }

        private object _lockObj = new object();

        private List<MyRect> rects = new List<MyRect>();
        private List<MyLine> lines = new List<MyLine>();

        public Draw() 
            : base()
        {
        }

        public void Clear()
        {
            lock (this._lockObj)
            {
                if (this.rects.Count > 0)
                    this.rects.Clear();

                if (this.lines.Count > 0)
                    this.lines.Clear();
            }
            
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (this.Dispatcher.Thread != Thread.CurrentThread)
            {
                this.Dispatcher.Invoke(new Action<DrawingContext>(this.OnRender), dc);
                return;
            }

            // This was the fastest way to draw multiple objects on WPF
            lock (this._lockObj)
            {
                foreach (MyRect mRect in rects)
                {
                    dc.DrawRectangle(mRect.Brush, null, mRect.Rect);
                }

                foreach (MyLine mLine in lines)
                {
                    dc.DrawLine(mLine.Pen, mLine.StartPoint, mLine.EndPoint);
                }
            }

            base.OnRender(dc);
        }

        public void DrawBlock(double left, double top, double width, double height, uint color)
        {
            lock (this._lockObj)
            {
                Rect rect = new Rect(left, top, width, height);
                System.Windows.Media.Brush brush = new SolidColorBrush(Utils.HexToColor(color));

                rects.Add(new MyRect() { Rect = rect, Brush = brush });
            }
        }

        public void DrawLine(double startX, double startY, double endX, double endY, uint color)
        {
            lock (this._lockObj)
            {
                Point startPoint = new Point(startX, startY);
                Point endPoint = new Point(endX, endY);
                Brush brush = new SolidColorBrush(Utils.HexToColor(color));
                Pen pen = new Pen(brush, 1);

                this.lines.Add(new MyLine() { Pen = pen, StartPoint = startPoint, EndPoint = endPoint });
            }
        }
    }
}
