﻿using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GraphAnalysis
{
    internal class ZoomBorder : Border
    {
        private UIElement child = null;
        private Point origin;
        private Point start;

        internal bool Flag_Select;


        private TranslateTransform GetTranslateTransform(UIElement element)
        {
            return (TranslateTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is TranslateTransform);
        }

        private ScaleTransform GetScaleTransform(UIElement element)
        {
            return (ScaleTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is ScaleTransform);
        }

        public override UIElement Child
        {
            get { return base.Child; }
            set
            {
                if (value != null && value != this.Child)
                {
                    this.Initialize(value);
                }

                base.Child = value;
            }
        }

        public void Initialize(UIElement element)
        {
            child = element;
            if (child != null)
            {
                TransformGroup group = new TransformGroup();
                ScaleTransform st = new ScaleTransform();
                group.Children.Add(st);
                TranslateTransform tt = new TranslateTransform();
                group.Children.Add(tt);
                child.RenderTransform = group;
                child.RenderTransformOrigin = new Point(0.0, 0.0);
                MouseWheel += child_MouseWheel;
                MouseLeftButtonDown += child_MouseLeftButtonDown;
                MouseLeftButtonUp += child_MouseLeftButtonUp;
                MouseMove += child_MouseMove;
                KeyDown += PushKeyR;

                Flag_Select = false;
                KeyDown += PushLeftCtrl;
                KeyUp += UpLeftCtrl;
            }
        }

        public void Reset()
        {
            if (child != null)
            {
                // reset zoom
                var st = GetScaleTransform(child);
                st.ScaleX = 1.0;
                st.ScaleY = 1.0;

                // reset pan
                var tt = GetTranslateTransform(child);
                tt.X = 0.0;
                tt.Y = 0.0;
            }
        }

        #region Child Zoom Events

        public void child_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Flag_Select == false)
            {
                if (child != null)
                {
                    var st = GetScaleTransform(child);
                    var tt = GetTranslateTransform(child);

                    double zoom = e.Delta > 0 ? .2 : -.2;
                    if (!(e.Delta > 0) && (st.ScaleX < .4 || st.ScaleY < .4))
                        return;

                    Point relative = e.GetPosition(child);
                    double absoluteX;
                    double absoluteY;

                    absoluteX = relative.X * st.ScaleX + tt.X;
                    absoluteY = relative.Y * st.ScaleY + tt.Y;

                    st.ScaleX += zoom;
                    st.ScaleY += zoom;

                    tt.X = absoluteX - relative.X * st.ScaleX;
                    tt.Y = absoluteY - relative.Y * st.ScaleY;
                }
            }
        }

        public void child_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Flag_Select == false)
            {
                if (child != null)
                {
                    var tt = GetTranslateTransform(child);
                    start = e.GetPosition(this);
                    origin = new Point(tt.X, tt.Y);
                    this.Cursor = Cursors.Hand;
                    child.CaptureMouse();
                }
            }
        }

        public void child_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (Flag_Select == false)
            {
                if (child != null)
                {
                    child.ReleaseMouseCapture();
                    this.Cursor = Cursors.Arrow;
                }
            }
        }

        public void child_MouseMove(object sender, MouseEventArgs e)
        {
            if (Flag_Select == false)
            {
                if (child != null)
                {
                    if (child.IsMouseCaptured)
                    {
                        var tt = GetTranslateTransform(child);
                        Vector v = start - e.GetPosition(this);
                        tt.X = origin.X - v.X;
                        tt.Y = origin.Y - v.Y;
                    }
                }
            }
        }

        public void PushKeyR(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.R)
            {
                if (Flag_Select == false && e.Source is ZoomBorder)
                {
                    Reset();
                }
            }
        }
        #endregion

        #region Selected Event

        public void PushLeftCtrl(object sender, KeyEventArgs e)
        {
            if (e.Source is ZoomBorder)
            {
                if (e.Key == Key.LeftCtrl)
                {
                    Flag_Select = true;
                }
            }
        }

        public void UpLeftCtrl(object sender, KeyEventArgs e)
        {
            if (e.Source is ZoomBorder)
            {
                if (e.Key == Key.LeftCtrl)
                {
                    Flag_Select = false;
                }
            }
        }

        #endregion
    }
}
