using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Little_Disk_Defrag.Misc;

namespace Little_Disk_Defrag.Helpers
{
    public class Draw : Control
    {
        public class MyRect
        {
            public Rect Rect;
            public Brush Brush;
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

        public void Clear()
        {
            lock (_lockObj)
            {
                if (rects.Count > 0)
                    rects.Clear();

                if (lines.Count > 0)
                    lines.Clear();
            }
            
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.Invoke(new Action<DrawingContext>(OnRender), dc);
                return;
            }

            // This was the fastest way to draw multiple objects on WPF
            lock (_lockObj)
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
            lock (_lockObj)
            {
                Rect rect = new Rect(left, top, width, height);
                Brush brush = new SolidColorBrush(Utils.HexToColor(color));

                rects.Add(new MyRect { Rect = rect, Brush = brush });
            }
        }

        public void DrawLine(double startX, double startY, double endX, double endY, uint color)
        {
            lock (_lockObj)
            {
                Point startPoint = new Point(startX, startY);
                Point endPoint = new Point(endX, endY);
                Brush brush = new SolidColorBrush(Utils.HexToColor(color));
                Pen pen = new Pen(brush, 1);

                lines.Add(new MyLine { Pen = pen, StartPoint = startPoint, EndPoint = endPoint });
            }
        }
    }
}
