﻿using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System;
using System.Runtime.InteropServices;

namespace RisCaptureLib
{
    internal class MaskWindow : Window, IDisposable
    {
        private MaskCanvas innerCanvas;
        private Bitmap screenSnapshot;
        private Timer timeOutTimmer;
        private readonly ScreenCaputre screenCaputreOwner;

        public MaskWindow(ScreenCaputre screenCaputreOwner, Rectangle rect, Window owner)
        {
            this.screenCaputreOwner = screenCaputreOwner;
            Ini(rect);
            this.Owner = owner;
        }

        private void Ini(Rectangle _rect)
        {

            //ini normal properties
            //Topmost = true;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;

            //set bounds to cover all screens
            Rectangle rect = _rect;
            Left = rect.X;
            Top = rect.Y;
            Width = rect.Width;
            Height = rect.Height;

            //set background 

            screenSnapshot = HelperMethods.GetScreenSnapshot(rect.X, rect.Y, rect.Width, rect.Height);
            if (screenSnapshot != null)
            {
                var bmp = screenSnapshot.ToBitmapSource();
                bmp.Freeze();
                Background = new ImageBrush(bmp);
            }

            //ini canvas
            innerCanvas = new MaskCanvas
            {
                MaskWindowOwner = this
            };
            Content = innerCanvas;

        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.RightButton == MouseButtonState.Pressed && e.ClickCount >= 2)
            {
                CancelCaputre();
            }
        }

        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (timeOutTimmer != null && timeOutTimmer.Enabled)
            {
                timeOutTimmer.Stop();
                timeOutTimmer.Start();
            }
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Escape)
            {
                CancelCaputre();
            }
        }

        private void CancelCaputre()
        {
            Close();
            screenCaputreOwner.OnScreenCaputreCancelled(null);
        }

        internal void OnShowMaskFinished(Rect maskRegion)
        {

        }

        internal void ClipSnapshot(Rect clipRegion)
        {
            Bitmap caputredBmp = CopyFromScreenSnapshotBMP(clipRegion);

            if (caputredBmp != null)
            {
                screenCaputreOwner.OnScreenCaputred(null, caputredBmp);
            }
            Dispose();
        }

        internal Bitmap CopyFromScreenSnapshotBMP(Rect region)
        {
            var sourceRect = region.ToRectangle();
            var destRect = new Rectangle(0, 0, sourceRect.Width, sourceRect.Height);

            if (screenSnapshot != null)
            {
                var bitmap = new Bitmap(sourceRect.Width, sourceRect.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.DrawImage(screenSnapshot, destRect, sourceRect, GraphicsUnit.Pixel);
                }

                return bitmap;
            }

            return null;
        }

        internal BitmapSource CopyFromScreenSnapshot(Rect region)
        {
            var sourceRect = region.ToRectangle();
            var destRect = new Rectangle(0, 0, sourceRect.Width, sourceRect.Height);

            if (screenSnapshot != null)
            {
                var bitmap = new Bitmap(sourceRect.Width, sourceRect.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.DrawImage(screenSnapshot, destRect, sourceRect, GraphicsUnit.Pixel);
                }

                return bitmap.ToBitmapSource();
            }

            return null;
        }

        public void Show(int timeOutSecond, System.Windows.Size? defaultSize)
        {
            if (timeOutSecond > 0)
            {
                if (timeOutTimmer == null)
                {
                    timeOutTimmer = new Timer();
                    timeOutTimmer.Tick += OnTimeOutTimmerTick;
                }
                timeOutTimmer.Interval = timeOutSecond * 1000;
                timeOutTimmer.Start();
            }

            if (innerCanvas != null)
            {
                innerCanvas.DefaultSize = defaultSize;
            }
            ShowDialog();
            Focus();

        }

        private void OnTimeOutTimmerTick(object sender, System.EventArgs e)
        {
            timeOutTimmer.Stop();
            CancelCaputre();
        }


        public void Dispose()
        {
            if (screenSnapshot != null)
            {
                //IntPtr pImg = screenSnapshot.GetHbitmap();
                //DeleteObject(pImg);
                screenSnapshot.Dispose();
                screenSnapshot = null;
                innerCanvas.Dispose();
                Content = null;
            }
            Background = null;
            Close();
        }

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
    }
}
