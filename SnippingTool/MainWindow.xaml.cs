using System;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Path = System.Windows.Shapes.Path;
using Point = System.Windows.Point;

namespace SnippingTool
{
    public partial class MainWindow
    {
        private readonly string _imagePath;
        private readonly Key _closeKey;

        private Point _dragPoint;
        private Point _cursorPoint;
        private Point _cursorMovePoint;

        private bool _canMove;
        private bool _canCapture;

        private Bitmap _backgroundBitmap;

        public MainWindow(string imagePath, Key closeKey)
        {
            InitializeComponent();

            PositionInfo.Effect = new DropShadowEffect
            {
                RenderingBias = RenderingBias.Quality,
                Color = Colors.Black,
                BlurRadius = 15.0,
                ShadowDepth = 0.0,
                Opacity = 0.9
            };

            _imagePath = imagePath;
            _closeKey = closeKey;
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            var size = GetAllScreenBounds();

            var screenLeft = size.Left;
            var screenTop = size.Top;
            var screenWidth = size.Width;
            var screenHeight = size.Height;

            // Create new bitmap with same size as our screens
            _backgroundBitmap = new Bitmap(screenWidth, screenHeight);

            using (var graphics = Graphics.FromImage(_backgroundBitmap))
            {
                graphics.CopyFromScreen(screenLeft, screenTop, 0, 0, _backgroundBitmap.Size, CopyPixelOperation.SourceCopy);
            }

            // Change the size of the control to fit size of all screens
            Left = screenLeft;
            Top = screenTop;
            Width = screenWidth;
            Height = screenHeight;

            // Setup geometry
            ScreenArea.Geometry1 = new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight));
            ScreenArea.Geometry2 = new RectangleGeometry(Rect.Empty);
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            _backgroundBitmap?.Dispose();
            _backgroundBitmap = null;
        }

        private void MainWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_backgroundBitmap == null)
            {
                return;
            }

            Background = new ImageBrush(_backgroundBitmap.GetBitmapSource());
        }

        private void MainWindow_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == _closeKey)
            {
                Close();
                return;
            }

            if (e.Key != Key.Space)
            {
                return;
            }

            if (_canMove)
            {
                return;
            }
            
            _dragPoint = Mouse.GetPosition(this);
            _canMove = true;

            UpdatePositionText();
        }

        private void MainWindow_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Space)
            {
                return;
            }

            if (!_canMove)
            {
                return;
            }

            _dragPoint = new Point(-1, -1);
            _canMove = false;

            UpdatePositionText();
        }

        private void UIElement_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is Path path))
            {
                return;
            }

            // Capture mouse movement when left click is pressed
            path.CaptureMouse();
            Cursor = Cursors.Cross;

            _canCapture = true;
            _cursorPoint = e.GetPosition(path);
        }

        private void UIElement_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is Path path))
            {
                return;
            }

            // Stop capture when left click is released
            _canCapture = false;
            Cursor = Cursors.Arrow;
            path.ReleaseMouseCapture();

            // Save screenshot
            TakeScreenShot(e.GetPosition(path));
        }

        private void UIElement_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_canCapture)
            {
                return;
            }

            if (!(sender is Path path))
            {
                return;
            }

            _cursorMovePoint = e.GetPosition(path);

            if (_canMove)
            {
                // Calculate the difference between old position and new position
                var difference = new Point
                {
                    X = _cursorMovePoint.X - _dragPoint.X,
                    Y = _cursorMovePoint.Y - _dragPoint.Y
                };

                // Store new value as old point
                _dragPoint.X = _cursorMovePoint.X;
                _dragPoint.Y = _cursorMovePoint.Y;

                // Update cursor point
                _cursorPoint.X += difference.X;
                _cursorPoint.Y += difference.Y;
            }

            UpdatePositionText();
            DrawGeometry();
        }

        private void TakeScreenShot(Point point)
        {
            var start = _cursorPoint;
            var end = point;

            // Normalize values
            if (start.X < 0)
            {
                start.X = 0;
            }

            if (start.Y < 0)
            {
                start.Y = 0;
            }

            if (start.X > ActualWidth)
            {
                start.X = (int)ActualWidth;
            }

            if (start.Y > ActualHeight)
            {
                start.Y = (int)ActualHeight;
            }

            // Calculate start position of area. This is necessary in case when user does the selection in opposite direction
            var x = start.X < end.X ? (int)start.X : (int)end.X;
            var y = start.Y < end.Y ? (int)start.Y : (int)end.Y;

            // Calculate width and height of area based on start and end position
            var width = (int)Math.Abs(end.X - start.X);
            var height = (int)Math.Abs(end.Y - start.Y);

            if (width == 0 || height == 0)
            {
                return;
            }

            using (var bitmap = _backgroundBitmap.CopyAreaToNewBitmap(new Rectangle(0, 0, width, height),
                new Rectangle(x, y, width, height)))
            {
                try
                {
                    bitmap.Save(_imagePath, System.Drawing.Imaging.ImageFormat.Png);
                }
                catch (System.Runtime.InteropServices.ExternalException)
                {
                    // We should think about this, maybe to log exception?
                    MessageBox.Show($"Saving image '{_imagePath}' failed.", "Error");
                }
            }

            DialogResult = true;
        }

        /// <summary>
        ///     Updates the info text which displays the area size or current coordinates.
        /// </summary>
        private void UpdatePositionText()
        {
            PositionInfo.Text = _canMove
                ? $"({_cursorMovePoint.X}, {_cursorMovePoint.Y})"
                : $"{Math.Abs(_cursorMovePoint.X - _cursorPoint.X)}x{Math.Abs(_cursorMovePoint.Y - _cursorPoint.Y)}";

            PositionInfo.Margin = _cursorMovePoint.Y <= _cursorPoint.Y
                ? new Thickness(_cursorMovePoint.X, _cursorMovePoint.Y - 30.0, 0.0, 0.0)
                : new Thickness(_cursorMovePoint.X, _cursorMovePoint.Y + 20.0, 0.0, 0.0);
        }

        /// <summary>
        ///     Updates the geometry to create selected area effect.
        /// </summary>
        private void DrawGeometry()
        {
            var x = Math.Min(_cursorPoint.X, _cursorMovePoint.X);
            var y = Math.Min(_cursorPoint.Y, _cursorMovePoint.Y);

            var width = Math.Abs(_cursorMovePoint.X - _cursorPoint.X);
            var height = Math.Abs(_cursorMovePoint.Y - _cursorPoint.Y); 

            ((RectangleGeometry) (ScreenArea.Geometry2)).Rect = new Rect(x, y, width, height);
        }

        /// <summary>
        ///     Gets the rectangle that represents the bounds of all screens together.
        /// </summary>
        /// <returns>New instance of <see cref="Rectangle"/>.</returns>
        private Rectangle GetAllScreenBounds()
        {
            var l = int.MaxValue;
            var t = int.MaxValue;
            var r = int.MinValue;
            var b = int.MinValue;

            // It would be nice to remove Windows Forms reference at some point
            foreach (var screen in System.Windows.Forms.Screen.AllScreens)
            {
                if (screen.Bounds.Left < l) l = screen.Bounds.Left;
                if (screen.Bounds.Top < t) t = screen.Bounds.Top;
                if (screen.Bounds.Right > r) r = screen.Bounds.Right;
                if (screen.Bounds.Bottom > b) b = screen.Bounds.Bottom;
            }

            return Rectangle.FromLTRB(l, t, r, b);
        }
    }
}
