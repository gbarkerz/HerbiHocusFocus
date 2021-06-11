using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using interop.UIAutomationCore;

namespace HerbiHocusFocus
{
    public partial class HighlightForm : Form
    {
        private bool _fGotCaretEvent = false;

        private Pen _pen;
        private Brush _brushCaret;
        private Color _colorCaret;
        private Rectangle _rectBeingHighlighted;
        private int _margin = 0;
        private int _marginCaret = 0;
        private int _marginUnderline = 0;
        private Point[] _pts = new Point[2];
        private Rectangle _rectCaret;
        private bool _highlightFocus = true;
        private bool _highlightCaretAbove = true;
        private bool _highlightCaretBelow = true;
        private bool _highlightTextLine = false;
        private int _heightUnderline = 0;
        private int _transparencyCaret = 0;
        private int _sizeCaret = 30;

        private static HerbiHocusFocusForm s_mainForm;

        public static IntPtr s_winEventHook = IntPtr.Zero;
        public static Win32Interop.WinEventDelegate s_winEventDelegate;
        public static HerbiHocusFocusForm.WinEventHandlerDelegate s_winEventHandlerDelegate;
        public static HerbiHocusFocusForm.CaretWinEventHandlerDelegate s_caretWinEventHandlerDelegate;

        public Form _formTextLine;

        public HighlightForm(
            HerbiHocusFocusForm mainForm,
            HerbiHocusFocusForm.WinEventHandlerDelegate winEventHandlerDelegate,
            HerbiHocusFocusForm.CaretWinEventHandlerDelegate caretWinEventHandlerDelegate)
        {
            InitializeComponent();

            s_mainForm = mainForm;

            s_winEventHandlerDelegate = winEventHandlerDelegate;
            s_caretWinEventHandlerDelegate = caretWinEventHandlerDelegate;

            Color colTransparent = Color.FromArgb(1, 1, 1);
            this.TransparencyKey = colTransparent;
            this.BackColor = colTransparent;

            this._pts[0] = new Point();
            this._pts[1] = new Point();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style -= 0x00C00000; // WS_CAPTION
                return cp;
            }
        }

        public void Initialize(
            bool highlightFocus,
            bool highlightCaretAbove,
            bool highlightCaretBelow,
            Color color,
            Color colorCaret,
            Color colorUnderline, 
            int thickness, 
            int margin,
            int marginCaret,
            int marginUnderline,
            int sizeCaret,
            int sizeUnderline, 
            DashStyle style)
        {
            this._highlightFocus = highlightFocus;
            this._highlightCaretAbove = highlightCaretAbove;
            this._highlightCaretBelow = highlightCaretBelow;

            this._pen = new Pen(color, thickness);
            this._pen.DashStyle = style;

            this._brushCaret = new SolidBrush(colorCaret);
            this._colorCaret = colorCaret;

            this._formTextLine = new TextLineForm();

            this._formTextLine.BackColor = colorUnderline;
            this._formTextLine.Height = sizeUnderline;
            this._marginUnderline = marginUnderline;

            this._margin = margin;
            this._marginCaret = marginCaret;
            this._sizeCaret = sizeCaret;
            
            s_winEventDelegate = new Win32Interop.WinEventDelegate(EventCallback);

            s_winEventHook =
                Win32Interop.SetWinEventHook(
                Win32Interop.EVENT_SYSTEM_MENUSTART,
                Win32Interop.EVENT_SYSTEM_MENUPOPUPSTART,
                IntPtr.Zero,
                s_winEventDelegate,
                0,
                0,
                Win32Interop.WINEVENT_OUTOFCONTEXT);
        }

        // Prevent any UIA hit-testing from hitting the highlight form.
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == Win32Interop.WM_NCHITTEST)
            {
                message.Result = (IntPtr)Win32Interop.HTTRANSPARENT;
            }
            else
            {
                base.WndProc(ref message);
            }
        }

        public static void EventCallback(
            IntPtr hWinEventHook, 
            uint eventType,
            IntPtr hwnd, 
            int idObject, 
            int idChild, 
            uint dwEventThread, 
            uint dwmsEventTime)
        {
            if ((eventType == Win32Interop.EVENT_SYSTEM_MENUSTART) ||
                (eventType == Win32Interop.EVENT_SYSTEM_MENUPOPUPSTART))
            {
                Debug.WriteLine("Got WinEvent " + eventType);

                try
                {
                    Debug.WriteLine("React to latest WinEvent");

                    s_mainForm.BeginInvoke(s_winEventHandlerDelegate);
                }
                catch
                {
                }
            }
        }

        public void Uninitialize()
        {
            if (s_winEventHook != IntPtr.Zero)
            {
                Win32Interop.UnhookWinEvent(s_winEventHook);
            }
        }

        private bool _ready = false;

        public bool Ready
        {
            get { return _ready; }
            set { _ready = value; }
        }

        public void SetColor(Color color)
        {
            this._pen.Color = color;

            this.Refresh();
        }

        public void SetColorCaret(Color color)
        {
            this._brushCaret = new SolidBrush(color);
            this._colorCaret = color;

            this.Refresh();
        }

        public void SetColorUnderline(Color color)
        {
            this._formTextLine.BackColor = color;
        }

        public void SetThickness(int thickness)
        {
            this._pen.Width = thickness;

            HighlightRect(_rectBeingHighlighted);
        }

        public void SetTransparencyCaret(int transparencyCaret)
        {
            this._transparencyCaret = (10 * transparencyCaret);

            this.Opacity = ((float)(100 - _transparencyCaret)) / 100.0;

            HighlightRect(_rectBeingHighlighted);
        }

        public void SetTransparencyUnderline(int transparencyUnderline)
        {
            this._formTextLine.Opacity = ((float)(100 - (10 * transparencyUnderline))) / 100.0;

            HighlightRect(_rectBeingHighlighted);
        }

        public void SetSizeCaret(int sizeCaret)
        {
            this._sizeCaret = sizeCaret;

            HighlightRect(_rectBeingHighlighted);
        }

        public void SetSizeUnderline(int sizeUnderline)
        {
            this._heightUnderline = sizeUnderline;

            HighlightRect(_rectBeingHighlighted);
        }

        public void SetHighlightFocus(bool highlightFocus)
        {
            this._highlightFocus = highlightFocus;

            HighlightRect(_rectBeingHighlighted);
        }

        public void SetHighlightCaretAbove(bool highlightCaretAbove)
        {
            this._highlightCaretAbove = highlightCaretAbove;

            HighlightRect(_rectBeingHighlighted);
        }

        public void SetHighlightCaretBelow(bool highlightCaretBelow)
        {
            this._highlightCaretBelow = highlightCaretBelow;

            HighlightRect(_rectBeingHighlighted);
        }

        public void SetHighlightTextLine(bool highlightTextLine)
        {
            this._highlightTextLine = highlightTextLine;
        }

        public void SetMargin(int margin)
        {
            this._margin = margin;

            HighlightRect(_rectBeingHighlighted);
        }

        public void SetMarginCaret(int marginCaret)
        {
            this._marginCaret = marginCaret;

            HighlightRect(_rectBeingHighlighted);
        }

        public void SetMarginUnderline(int marginUnderline)
        {
            this._marginUnderline = marginUnderline;

            HighlightRect(_rectBeingHighlighted);
        }

        public void SetStyle(DashStyle style)
        {
            this._pen.DashStyle = style;

            this.Refresh();
        }

        public void SetGotCaretEvent(bool fGotCaretEvent)
        {
            _fGotCaretEvent = fGotCaretEvent;
        }

        public void HighlightCurrentCaretPosition()
        {
            if (!this.Ready)
            {
                Debug.WriteLine("HighlightCurrentCaretPosition: Not ready");

                return;
            }

            Debug.WriteLine("HighlightCurrentCaretPosition: Highlight current caret position.");

            IntPtr hwndForeground = Win32Interop.GetForegroundWindow();

            uint threadId = Win32Interop.GetWindowThreadProcessId(hwndForeground, IntPtr.Zero);

            Debug.WriteLine("HighlightCurrentCaretPosition: hwndFocus " + hwndForeground);

            var guiThreadInfo = new Win32Interop.GUITHREADINFO();
            guiThreadInfo.cbSize = Marshal.SizeOf(guiThreadInfo);

            if (Win32Interop.GetGUIThreadInfo(threadId, ref guiThreadInfo))
            {
                IntPtr hwndCaret = guiThreadInfo.hwndCaret;
                if (hwndCaret == IntPtr.Zero)
                {
                    // guiThreadInfo.hwndFocus needed for VS 2015.
                    hwndCaret = guiThreadInfo.hwndFocus;
                }

                if (hwndCaret != IntPtr.Zero)
                {
                    Guid guid = new Guid("{618736E0-3C3D-11CF-810C-00AA00389B71}");
                    object obj = null;

                    int retVal = Win32Interop.AccessibleObjectFromWindow(
                        hwndCaret, 0xFFFFFFF8 /* CARET */, ref guid, ref obj);

                    Debug.WriteLine("HighlightCurrentCaretPosition: AccessibleObjectFromWindow - retVal " +
                        retVal + ", obj " + obj);

                    Rectangle rectCaret = new Rectangle();

                    IAccessible accessible = (IAccessible)obj;
                    if (accessible != null)
                    {
                        Debug.WriteLine("HighlightCurrentCaretPosition: Got IAccessible.");

                        int x = 0;
                        int y = 0;
                        int cx = 0;
                        int cy = 0;

                        try
                        {
                            accessible.accLocation(out x, out y, out cx, out cy);
                        }
                        catch (Exception ex)
                        {
                            // Barker todo: Determine why an exception was once thrown here.
                            Debug.WriteLine("HighlightCurrentCaretPosition: " + ex.Message);
                        }

                        Debug.WriteLine("HighlightCurrentCaretPosition: x, y = " + x + ", " + y + ", cx, cy = " + cx + ", " + cy);

                        if (cx > CaretHandler.MaxCaretWidth)
                        {
                            cx = 1;
                        }

                        rectCaret = new Rectangle(x, y, cx, cy);
                    }

                    // If we have no rect, try to use UIA.
                    if ((rectCaret.Size.Width == 0) && (rectCaret.Size.Height == 0))
                    {
                        Debug.WriteLine("HighlightCaret: No rect available");

                        s_mainForm.GetSelectionRect(ref rectCaret);
                    }

                    if ((rectCaret.Right != 0) && (rectCaret.Bottom != 0))
                    {
                        _fGotCaretEvent = true;

                        rectCaret.Offset(-this.Left, -this.Top);

                        _rectCaret = rectCaret;

                        Rectangle rectRefresh = new Rectangle();

                        // Redraw the highlight window to show the caret at its new position.
                        int xBuffer = _sizeCaret + 1;
                        int yBuffer = _marginCaret + _sizeCaret + 1;

                        rectRefresh.X = rectCaret.X - xBuffer;
                        rectRefresh.Y = rectCaret.Y - yBuffer;
                        rectRefresh.Width = rectCaret.Width + (2 * xBuffer);
                        rectRefresh.Height = rectCaret.Height + (2 * yBuffer);

                        this.Invalidate(rectRefresh);
                        this.Update();
                    }
                }
            }
        }

        public void HighlightCurrentTextLine(Rectangle rectLine)
        {
            if (!this.Ready)
            {
                Debug.WriteLine("HighlightCurrentTextLine: Not ready");

                return;
            }

            Debug.WriteLine("HighlightCurrentTextLine: " +
                "Start " + rectLine.Left + ", " + rectLine.Bottom + 
                ", Width " + rectLine.Width);

            if (rectLine.Width == 0)
            {
                this._formTextLine.Visible = false;
            }
            else
            {
                this._formTextLine.Width = rectLine.Width;
                this._formTextLine.Height = _heightUnderline;

                int yUnderline = rectLine.Bottom + this._marginUnderline;

                Point ptLine = new Point(rectLine.Left, yUnderline);
                this._formTextLine.Location = ptLine;

                this._formTextLine.Visible = true;
            }
        }

        public void HighlightCaret(Rectangle rect)
        {
            if (!this.Ready)
            {
                Debug.WriteLine("HighlightCaret: Not ready");

                return;
            }

            // Highlight the line of text as appropriate.
            s_mainForm.HighlightCurrentTextLineAsAppropriate(false /*Focus changed*/);

            // If we have no rect, try to use UIA.
            if ((rect.Size.Width == 0) && (rect.Size.Height == 0))
            {
                Debug.WriteLine("HighlightCaret: No rect available");

                s_mainForm.GetSelectionRect(ref rect);
            }

            if ((rect.Right != 0) && (rect.Bottom != 0))
            {
                Rectangle rectRefresh = new Rectangle();

                int xBuffer = _sizeCaret + 1;
                int yBuffer = _marginCaret + _sizeCaret + 1;

                rectRefresh.X = _rectCaret.X - xBuffer;
                rectRefresh.Y = _rectCaret.Y - yBuffer;
                rectRefresh.Width = _rectCaret.Width + (2 * xBuffer);
                rectRefresh.Height = _rectCaret.Height + (2 * yBuffer);

                // We will remove the current visuals for the caret.
                this.Invalidate(rectRefresh);

                // We have an valid updated caret position.
                _rectCaret = rect;

                // Make the cached caret rect relative to the highlight window.
                _rectCaret.Offset(-this.Left, -this.Top);

                _fGotCaretEvent = true;

                // Redraw the highlight window to show the caret at its new position.
                xBuffer = _sizeCaret + 1;
                yBuffer = _marginCaret + _sizeCaret + 1;

                rectRefresh.X = _rectCaret.X - xBuffer;
                rectRefresh.Y = _rectCaret.Y - yBuffer;
                rectRefresh.Width = _rectCaret.Width + (2 * xBuffer);
                rectRefresh.Height = _rectCaret.Height + (2 * yBuffer);

                this.Invalidate(rectRefresh);
                this.Update();
            }
        }

        // The rect is top/left corner and width/height.
        public void HighlightRect(Rectangle rect)
        {
            if (!this.Ready)
            {
                Debug.WriteLine("HighlightRect: Not ready");

                return;
            }

            // Don't highlight a zero-size rect.

            Debug.WriteLine("Highlight " +
                rect.Top + ", " +
                rect.Left + ", " +
                rect.Bottom + ", " +
                rect.Right);

            // The supplied rect is the bounding rect of the keyboard focus.
            if ((rect.Right != 0) && (rect.Bottom != 0))
            {
                _rectBeingHighlighted = rect;

                // Expand the window showing the highlight rect, to account for the margin applied for the focus
                // rect, and also for the margin applied to the caret feefback, and the caret feedback itself.

                int margin = this._margin + this._marginCaret + this._sizeCaret + (int)this._pen.Width;

                this.Location = new Point(
                    rect.Left - margin,
                    rect.Top - margin);

                this.Size = new Size(
                    rect.Right - rect.Left + (2 * margin),
                    rect.Bottom - rect.Top + (2 * margin));

                this.Refresh();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (!this.Ready)
            {
                Debug.WriteLine("OnPaint: Not ready");

                return;
            }

            if ((this._pen == null) || (this._brushCaret == null))
            {
                return;
            }

            Rectangle rect = this.ClientRectangle;

            int halfPenWidth = (int)((this._pen.Width + 0.5) / 2.0);

            int caretOffset = this._sizeCaret + this._marginCaret;

            bool highlightedCaret = false;

            // Do we know where the caret is since the previous focus changed event?
            if (_fGotCaretEvent && s_mainForm.IsCaretTrackingEnabled())
            {
                Debug.WriteLine("Repaint caret highlight now.");

                highlightedCaret = _highlightCaretAbove | _highlightCaretBelow;
                if (highlightedCaret)
                {
                    this.Opacity = ((float)(100 - _transparencyCaret)) / 100.0;
                }

                Point[] pathPoints = new Point[4];

                if (_highlightCaretAbove)
                {
                    pathPoints[0].X = _rectCaret.Left -
                                        (int)(((float)_sizeCaret + _rectCaret.Width + 0.5) / 2.0);
                    pathPoints[0].Y = _rectCaret.Top - _sizeCaret - _marginCaret;

                    pathPoints[1].X = pathPoints[0].X + _sizeCaret + _rectCaret.Width;
                    pathPoints[1].Y = pathPoints[0].Y;

                    pathPoints[2].X = (int)((((float)(pathPoints[0].X + pathPoints[1].X) + 0.5)) / 2.0);
                    pathPoints[2].Y = _rectCaret.Top - _marginCaret;

                    pathPoints[3].X = pathPoints[0].X;
                    pathPoints[3].Y = pathPoints[0].Y;

                    e.Graphics.FillPolygon(_brushCaret, pathPoints);
                }

                if (_highlightCaretBelow)
                {
                    pathPoints[0].X = _rectCaret.Left -
                                        (int)(((float)_sizeCaret + _rectCaret.Width + 0.5) / 2.0);
                    pathPoints[0].Y = _rectCaret.Bottom + this._sizeCaret + this._marginCaret;

                    pathPoints[1].X = pathPoints[0].X + _sizeCaret + _rectCaret.Width;
                    pathPoints[1].Y = pathPoints[0].Y;

                    pathPoints[2].X = (int)((((float)(pathPoints[0].X + pathPoints[1].X) + 0.5)) / 2.0);
                    pathPoints[2].Y = pathPoints[0].Y - this._sizeCaret;

                    pathPoints[3].X = pathPoints[0].X;
                    pathPoints[3].Y = pathPoints[0].Y;

                    e.Graphics.FillPolygon(this._brushCaret, pathPoints);
                }
            }

            if (_highlightFocus)
            {
                if (!highlightedCaret)
                {
                    this.Opacity = 1.0;
                }

                _pts[0].X = rect.Left + halfPenWidth + caretOffset;
                _pts[0].Y = rect.Top + halfPenWidth + caretOffset;

                _pts[1].X = rect.Width - 1 - caretOffset;
                _pts[1].Y = rect.Top + (int)((_pen.Width + 0.5) / 2.0) + caretOffset;

                e.Graphics.DrawLine(this._pen, _pts[0].X, _pts[0].Y, _pts[1].X, _pts[1].Y);

                _pts[0].X = rect.Width - halfPenWidth - 1 - caretOffset;
                _pts[0].Y = rect.Top + caretOffset;

                _pts[1].X = rect.Width - halfPenWidth - 1 - caretOffset;
                _pts[1].Y = rect.Height - 1 - caretOffset;

                e.Graphics.DrawLine(this._pen, _pts[0].X, _pts[0].Y, _pts[1].X, _pts[1].Y);

                _pts[0].X = rect.Width - 1 - caretOffset;
                _pts[0].Y = rect.Height - halfPenWidth - 1 - caretOffset;

                _pts[1].X = rect.Left + caretOffset;
                _pts[1].Y = rect.Height - halfPenWidth - 1 - caretOffset;

                e.Graphics.DrawLine(this._pen, _pts[0].X, _pts[0].Y, _pts[1].X, _pts[1].Y);

                _pts[0].X = rect.Left + halfPenWidth + caretOffset;
                _pts[0].Y = rect.Height - 1 - caretOffset;

                _pts[1].X = rect.Left + halfPenWidth + caretOffset;
                _pts[1].Y = rect.Top + caretOffset;

                e.Graphics.DrawLine(this._pen, _pts[0].X, _pts[0].Y, _pts[1].X, _pts[1].Y);
            }
        }
    }

    public class Win32Interop
    {
        public enum GWL
        {
            ExStyle = -20
        }

        public enum WS_EX
        {
            Transparent = 0x20,
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowThreadProcessId")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr processId);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        public static extern int GetWindowLong(IntPtr hWnd, GWL nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        public static extern int SetWindowLong(IntPtr hWnd, GWL nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetProcessDPIAware();

        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType,
             IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr
           hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess,
           uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr UnhookWinEvent(IntPtr hook);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);

        public const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        public const uint EVENT_SYSTEM_MENUSTART = 0x0004;
        public const uint EVENT_SYSTEM_MENUEND = 0x0005;
        public const uint EVENT_SYSTEM_MENUPOPUPSTART = 0x0006;
        public const uint EVENT_SYSTEM_MENUPOPUPEND = 0x0007;

        public const uint EVENT_OBJECT_SHOW = 0x8002;
        public const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
        public const uint EVENT_OBJECT_TEXTSELECTIONCHANGED = 0x8014;

        public const uint WINEVENT_OUTOFCONTEXT = 0x0000; // Events are ASYNC

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        static readonly IntPtr HWND_TOP = new IntPtr(0);
        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        public const int WM_NCHITTEST = 0x84;
        public const int HTTRANSPARENT = -1;

        public static class HWND
        {
            public static IntPtr
            NoTopMost = new IntPtr(-2),
            TopMost = new IntPtr(-1),
            Top = new IntPtr(0),
            Bottom = new IntPtr(1);
        }

        public static class SWP
        {
            public static readonly int
            NOSIZE = 0x0001,
            NOMOVE = 0x0002,
            NOZORDER = 0x0004,
            NOREDRAW = 0x0008,
            NOACTIVATE = 0x0010,
            DRAWFRAME = 0x0020,
            FRAMECHANGED = 0x0020,
            SHOWWINDOW = 0x0040,
            HIDEWINDOW = 0x0080,
            NOCOPYBITS = 0x0100,
            NOOWNERZORDER = 0x0200,
            NOREPOSITION = 0x0200,
            NOSENDCHANGING = 0x0400,
            DEFERERASE = 0x2000,
            ASYNCWINDOWPOS = 0x4000;
        }

        // Caret-related stuff.

        public struct GUITHREADINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hwndActive;
            public IntPtr hwndFocus;
            public IntPtr hwndCapture;
            public IntPtr hwndMenuOwner;
            public IntPtr hwndMoveSize;
            public IntPtr hwndCaret;
            public System.Drawing.Rectangle rcCaret;
        }

        [DllImport("user32.dll")]
        public static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

        [DllImport("oleacc.dll")]
        public static extern int AccessibleObjectFromWindow(
              IntPtr hwnd,
              uint id,
              ref Guid iid,
              [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object ppvObject);

        public const int WH_KEYBOARD_LL = 13;
        public const int WM_KEYDOWN = 0x0100;

        [StructLayout(LayoutKind.Sequential)]
        public class KbLLHookStruct
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }

        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

    }
}
