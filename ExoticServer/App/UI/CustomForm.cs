using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ExoticServer.App.UI
{
    public class CustomForm : Form
    {
        #region Variables

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
        public class MONITORINFOEX
        {
            public int cbSize = Marshal.SizeOf(typeof(MONITORINFOEX));

            public RECT rcMonitor = default(RECT);

            public RECT rcWork = default(RECT);

            public int dwFlags = 0;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public char[] szDevice = new char[32];
        }

        public struct RECT
        {
            public int left;

            public int top;

            public int right;

            public int bottom;

            public int Width()
            {
                return right - left;
            }

            public int Height()
            {
                return bottom - top;
            }
        }

        private enum ResizeDirection
        {
            BottomLeft,
            Left,
            Right,
            BottomRight,
            Bottom,
            None
        }

        private enum ButtonState
        {
            XOver,
            MaxOver,
            MinOver,
            XDown,
            MaxDown,
            MinDown,
            None
        }

        public const int WM_NCLBUTTONDOWN = 161;

        public const int HT_CAPTION = 2;

        public const int WM_MOUSEMOVE = 512;

        public const int WM_LBUTTONDOWN = 513;

        public const int WM_LBUTTONUP = 514;

        public const int WM_LBUTTONDBLCLK = 515;

        public const int WM_RBUTTONDOWN = 516;

        private ResizeDirection resizeDir;

        private ButtonState buttonState = ButtonState.None;

        private readonly Dictionary<int, int> resizingLocationsToCmd = new Dictionary<int, int>
        {
            { 12, 3 },
            { 13, 4 },
            { 14, 5 },
            { 10, 1 },
            { 11, 2 },
            { 15, 6 },
            { 16, 7 },
            { 17, 8 }
        };

        private readonly Cursor[] resizeCursors = new Cursor[5]
        {
            Cursors.SizeNESW,
            Cursors.SizeWE,
            Cursors.SizeNWSE,
            Cursors.SizeWE,
            Cursors.SizeNS
        };

        private Rectangle minButtonBounds;

        private Rectangle maxButtonBounds;

        private Rectangle xButtonBounds;

        private Rectangle actionBarBounds;

        private Rectangle statusBarBounds;

        private bool Maximized;

        private Size previousSize;

        private Point previousLocation;

        private bool headerMouseDown;

        [Browsable(false)]
        public int Depth { get; set; }

        public new FormBorderStyle FormBorderStyle
        {
            get
            {
                return base.FormBorderStyle;
            }
            set
            {
                base.FormBorderStyle = value;
            }
        }

        public bool Sizable { get; set; }


        #region HeaderProperties

        private Color _headerBackColor = Color.White;

        [Browsable(true)]
        [Category("Custom")]
        [Description("Change The Back Color On The Header.")]
        public Color HeaderColor
        {
            get { return _headerBackColor; }
            set { _headerBackColor = value; }
        }

        private int _headerHeight = 20;

        [Category("Custom")]
        [Description("Change The Height On The Header.")]
        [Browsable(true)]
        public int HeaderHeight
        {
            get { return _headerHeight; }
            set { _headerHeight = value; }
        }
        #endregion


        private int _radius = 20;
        [Category("Custom")]
        [Description("Change The Height On The Header.")]
        [Browsable(true)]
        public int Radius
        {
            get { return _radius; }
            set { _radius = value; }
        }

        #endregion

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams createParams = base.CreateParams;
                createParams.Style = createParams.Style | 0x20000 | 0x80000;
                return createParams;
            }
        }

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern int TrackPopupMenuEx(IntPtr hmenu, uint fuFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

        [DllImport("user32.dll")]
        public static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetMonitorInfo(HandleRef hmonitor, [In][Out] MONITORINFOEX info);

        public CustomForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            Sizable = true;
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, value: true);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (base.DesignMode || base.IsDisposed)
            {
                return;
            }

            if (m.Msg == 515)
            {
                MaximizeWindow(!Maximized);
            }
            else if (m.Msg == 512 && Maximized && (statusBarBounds.Contains(PointToClient(Cursor.Position)) || actionBarBounds.Contains(PointToClient(Cursor.Position))) && !minButtonBounds.Contains(PointToClient(Cursor.Position)) && !maxButtonBounds.Contains(PointToClient(Cursor.Position)) && !xButtonBounds.Contains(PointToClient(Cursor.Position)))
            {
                if (headerMouseDown)
                {
                    Maximized = false;
                    headerMouseDown = false;
                    Point point = PointToClient(Cursor.Position);
                    if (point.X < base.Width / 2)
                    {
                        base.Location = ((point.X < previousSize.Width / 2) ? new Point(Cursor.Position.X - point.X, Cursor.Position.Y - point.Y) : new Point(Cursor.Position.X - previousSize.Width / 2, Cursor.Position.Y - point.Y));
                    }
                    else
                    {
                        base.Location = ((base.Width - point.X < previousSize.Width / 2) ? new Point(Cursor.Position.X - previousSize.Width + base.Width - point.X, Cursor.Position.Y - point.Y) : new Point(Cursor.Position.X - previousSize.Width / 2, Cursor.Position.Y - point.Y));
                    }

                    base.Size = previousSize;
                    ReleaseCapture();
                    SendMessage(base.Handle, 161, 2, 0);
                }
            }
            else if (m.Msg == 513 && (statusBarBounds.Contains(PointToClient(Cursor.Position)) || actionBarBounds.Contains(PointToClient(Cursor.Position))) && !minButtonBounds.Contains(PointToClient(Cursor.Position)) && !maxButtonBounds.Contains(PointToClient(Cursor.Position)) && !xButtonBounds.Contains(PointToClient(Cursor.Position)))
            {
                if (!Maximized)
                {
                    ReleaseCapture();
                    SendMessage(base.Handle, 161, 2, 0);
                }
                else
                {
                    headerMouseDown = true;
                }
            }
            else if (m.Msg == 516)
            {
                Point pt = PointToClient(Cursor.Position);
                if (statusBarBounds.Contains(pt) && !minButtonBounds.Contains(pt) && !maxButtonBounds.Contains(pt) && !xButtonBounds.Contains(pt))
                {
                    int wParam = TrackPopupMenuEx(GetSystemMenu(base.Handle, bRevert: false), 256u, Cursor.Position.X, Cursor.Position.Y, base.Handle, IntPtr.Zero);
                    SendMessage(base.Handle, 274, wParam, 0);
                }
            }
            else if (m.Msg == 161)
            {
                if (Sizable)
                {
                    byte b = 0;
                    if (resizingLocationsToCmd.ContainsKey((int)m.WParam))
                    {
                        b = (byte)resizingLocationsToCmd[(int)m.WParam];
                    }

                    if (b != 0)
                    {
                        SendMessage(base.Handle, 274, 0xF000 | b, (int)m.LParam);
                    }
                }
            }
            else if (m.Msg == 514)
            {
                headerMouseDown = false;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (!base.DesignMode)
            {
                UpdateButtons(e);

                if (e.Button == MouseButtons.Left && !Maximized)
                {
                    ResizeForm(resizeDir);
                }

                base.OnMouseDown(e);
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            if (!base.DesignMode)
            {
                buttonState = ButtonState.None;
                Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (base.DesignMode)
            {
                return;
            }

            if (Sizable)
            {
                bool flag = GetChildAtPoint(e.Location) != null;
                if (e.Location.X < 7 && e.Location.Y > base.Height - 7 && !flag && !Maximized)
                {
                    resizeDir = ResizeDirection.BottomLeft;
                    Cursor = Cursors.SizeNESW;
                }
                else if (e.Location.X < 7 && !flag && !Maximized)
                {
                    resizeDir = ResizeDirection.Left;
                    Cursor = Cursors.SizeWE;
                }
                else if (e.Location.X > base.Width - 7 && e.Location.Y > base.Height - 7 && !flag && !Maximized)
                {
                    resizeDir = ResizeDirection.BottomRight;
                    Cursor = Cursors.SizeNWSE;
                }
                else if (e.Location.X > base.Width - 7 && !flag && !Maximized)
                {
                    resizeDir = ResizeDirection.Right;
                    Cursor = Cursors.SizeWE;
                }
                else if (e.Location.Y > base.Height - 7 && !flag && !Maximized)
                {
                    resizeDir = ResizeDirection.Bottom;
                    Cursor = Cursors.SizeNS;
                }
                else
                {
                    resizeDir = ResizeDirection.None;
                    if (resizeCursors.Contains(Cursor))
                    {
                        Cursor = Cursors.Default;
                    }
                }
            }

            UpdateButtons(e);
        }

        protected void OnGlobalMouseMove(object sender, MouseEventArgs e)
        {
            if (!base.IsDisposed)
            {
                Point point = PointToClient(e.Location);
                MouseEventArgs e2 = new MouseEventArgs(MouseButtons.None, 0, point.X, point.Y, 0);
                OnMouseMove(e2);
            }
        }

        private void UpdateButtons(MouseEventArgs e, bool up = false)
        {
            if (base.DesignMode)
            {
                return;
            }

            ButtonState buttonState = this.buttonState;
            bool flag = base.MinimizeBox && base.ControlBox;
            bool flag2 = base.MaximizeBox && base.ControlBox;

            if (e.Button == MouseButtons.Left && !up)
            {
                if (flag && !flag2 && maxButtonBounds.Contains(e.Location))
                {
                    this.buttonState = ButtonState.MinDown;
                }
                else if (flag && flag2 && minButtonBounds.Contains(e.Location))
                {
                    this.buttonState = ButtonState.MinDown;
                }
                else if (flag2 && maxButtonBounds.Contains(e.Location))
                {
                    this.buttonState = ButtonState.MaxDown;
                }
                else if (base.ControlBox && xButtonBounds.Contains(e.Location))
                {
                    this.buttonState = ButtonState.XDown;
                }
                else
                {
                    this.buttonState = ButtonState.None;
                }
            }
            else if (flag && !flag2 && maxButtonBounds.Contains(e.Location))
            {
                this.buttonState = ButtonState.MinOver;
                if (buttonState == ButtonState.MinDown)
                {
                    base.WindowState = FormWindowState.Minimized;
                }
            }
            else if (flag && flag2 && minButtonBounds.Contains(e.Location))
            {
                this.buttonState = ButtonState.MinOver;
                if (buttonState == ButtonState.MinDown)
                {
                    base.WindowState = FormWindowState.Minimized;
                }
            }
            else if (base.MaximizeBox && base.ControlBox && maxButtonBounds.Contains(e.Location))
            {
                this.buttonState = ButtonState.MaxOver;
                if (buttonState == ButtonState.MaxDown)
                {
                    MaximizeWindow(!Maximized);
                }
            }
            else if (base.ControlBox && xButtonBounds.Contains(e.Location))
            {
                this.buttonState = ButtonState.XOver;
                if (buttonState == ButtonState.XDown)
                {
                    Close();
                }
            }
            else
            {
                this.buttonState = ButtonState.None;
            }

            if (buttonState != this.buttonState)
            {
                Invalidate();
            }
        }

        private void MaximizeWindow(bool maximize)
        {
            if (base.MaximizeBox && base.ControlBox)
            {
                Maximized = maximize;

                if (maximize)
                {
                    IntPtr handle = MonitorFromWindow(base.Handle, 2u);
                    MONITORINFOEX mONITORINFOEX = new MONITORINFOEX();
                    GetMonitorInfo(new HandleRef(null, handle), mONITORINFOEX);
                    previousSize = base.Size;
                    previousLocation = base.Location;
                    base.Size = new Size(mONITORINFOEX.rcWork.Width(), mONITORINFOEX.rcWork.Height());
                    base.Location = new Point(mONITORINFOEX.rcWork.left, mONITORINFOEX.rcWork.top);
                }
                else
                {
                    base.Size = previousSize;
                    base.Location = previousLocation;
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (!base.DesignMode)
            {
                UpdateButtons(e, up: true);
                base.OnMouseUp(e);
                ReleaseCapture();
            }
        }

        private void ResizeForm(ResizeDirection direction)
        {
            if (!base.DesignMode)
            {
                int num = -1;
                switch (direction)
                {
                    case ResizeDirection.BottomLeft:
                        num = 16;
                        break;
                    case ResizeDirection.Left:
                        num = 10;
                        break;
                    case ResizeDirection.Right:
                        num = 11;
                        break;
                    case ResizeDirection.BottomRight:
                        num = 17;
                        break;
                    case ResizeDirection.Bottom:
                        num = 15;
                        break;
                }

                ReleaseCapture();

                if (num != -1)
                {
                    SendMessage(base.Handle, 161, num, 0);
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            statusBarBounds = new Rectangle(0, 0, base.Width, 24);
            actionBarBounds = new Rectangle(0, 24, base.Width, 40);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            DrawHeadBackground(graphics);
        }

        private void DrawHeadBackground(Graphics graphics)
        {
            graphics.TextRenderingHint = TextRenderingHint.AntiAlias;

            // Create a rectangular path with rounded corners
            GraphicsPath path = new GraphicsPath();
            path.AddArc(0, 0, _radius * 2, _radius * 2, 180, 90);
            path.AddLine(_radius, 0, this.Width - _radius, 0);
            path.AddArc(this.Width - _radius * 2, 0, _radius * 2, _radius * 2, 270, 90);
            path.AddLine(this.Width, _radius, this.Width, this.Height - _radius);
            path.AddArc(this.Width - _radius * 2, this.Height - _radius * 2, _radius * 2, _radius * 2, 0, 90);
            path.AddLine(this.Width - _radius, this.Height, _radius, this.Height);
            path.AddArc(0, this.Height - _radius * 2, _radius * 2, _radius * 2, 90, 90);
            path.AddLine(0, this.Height - _radius, 0, _radius);
            path.CloseFigure();

            // Set the form region to the rectangular path with rounded corners
            this.Region = new Region(path);

            Brush brush = new SolidBrush(_headerBackColor);
            Rectangle headerRect = new Rectangle(0, 0, Width, _headerHeight);
            graphics.FillRectangle(brush, headerRect);
        }
    }
}
