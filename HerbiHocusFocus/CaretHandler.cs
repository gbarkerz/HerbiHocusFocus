using Accessibility;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace HerbiHocusFocus
{
    class CaretHandler
    {
        public static IntPtr s_caretWinEventHook = IntPtr.Zero;
        public static Win32Interop.WinEventDelegate s_caretWinEventDelegate;
        public static HerbiHocusFocusForm.CaretWinEventHandlerDelegate s_caretWinEventHandlerDelegate;

        private static HerbiHocusFocusForm s_mainForm;

        public static int MaxCaretWidth = 20;

        public CaretHandler(
            HerbiHocusFocusForm mainForm, 
            HerbiHocusFocusForm.CaretWinEventHandlerDelegate caretWinEventHandlerDelegate)
        {
            s_mainForm = mainForm;

            s_caretWinEventHandlerDelegate = caretWinEventHandlerDelegate;
        }

        public void Initialize()
        {
            s_caretWinEventDelegate = new Win32Interop.WinEventDelegate(WinEventCallback);

            s_caretWinEventHook =
                Win32Interop.SetWinEventHook(
                Win32Interop.EVENT_OBJECT_LOCATIONCHANGE,
                Win32Interop.EVENT_OBJECT_TEXTSELECTIONCHANGED,
                IntPtr.Zero,
                s_caretWinEventDelegate,
                0,
                0,
                Win32Interop.WINEVENT_OUTOFCONTEXT);
        }

        public void Uninitialize()
        {
            if (s_caretWinEventHook != IntPtr.Zero)
            {
                Win32Interop.UnhookWinEvent(s_caretWinEventHook);
            }
        }

        static public bool gotCaretWinEvent = false;

        public static void WinEventCallback(
            IntPtr hWinEventHook,
            uint eventType,
            IntPtr hwnd,
            int idObject,
            int idChild,
            uint dwEventThread,
            uint dwmsEventTime)
        {
            if ((eventType == Win32Interop.EVENT_OBJECT_LOCATIONCHANGE) && (idObject == -8)) // OBJID_CARET
            {
                Debug.WriteLine("Got caret location change.");

                gotCaretWinEvent = true;

                // Assume all the work below to get the caret rect is performant enough
                // to do in the winevent hook.

                var guiThreadInfo = new Win32Interop.GUITHREADINFO();
                guiThreadInfo.cbSize = Marshal.SizeOf(guiThreadInfo);

                if (Win32Interop.GetGUIThreadInfo(dwEventThread, ref guiThreadInfo))
                {
                    // guiThreadInfo.hwndCaret is good for:
                    // RUn, Notepad, WordPad, Word 2013, cmd window.

                    IntPtr hwndCaret = guiThreadInfo.hwndCaret;
                    if (hwndCaret == IntPtr.Zero)
                    {
                        // guiThreadInfo.hwndFocus needed for VS 2015.
                        hwndCaret = guiThreadInfo.hwndFocus;
                    }

                    // Future: If we still don't have a window, consider looking at gti.rcCaret.

                    if (hwndCaret != IntPtr.Zero)
                    {
                        Guid guid = new Guid("{618736E0-3C3D-11CF-810C-00AA00389B71}");
                        object obj = null;

                        int retVal = Win32Interop.AccessibleObjectFromWindow(
                            hwndCaret, 0xFFFFFFF8 /* CARET */, ref guid, ref obj);

                        IAccessible accessible = (IAccessible)obj;
                        if (accessible != null)
                        {
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
                                // Barker todo: Determine why an exception gets thrown here when interacting with the Spotify app.
                                Debug.WriteLine("WinEventCallback: " + ex.Message);
                            }

                            Debug.WriteLine("x, y = " + x + ", " + y + ", cx, cy = " + cx + ", " + cy);

                            if (cx > CaretHandler.MaxCaretWidth)
                            {
                                cx = 1;
                            }

                            Rectangle rectBounds = new Rectangle(x, y, cx, cy);

                            try
                            {
                                s_mainForm.BeginInvoke(s_caretWinEventHandlerDelegate, rectBounds);
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }
            else if (eventType == Win32Interop.EVENT_OBJECT_TEXTSELECTIONCHANGED)
            {
                if (gotCaretWinEvent)
                {
                    return;
                }

                Debug.WriteLine("Got EVENT_OBJECT_TEXTSELECTIONCHANGED");

                Rectangle rectBounds = new Rectangle();

                try
                {
                    s_mainForm.BeginInvoke(s_caretWinEventHandlerDelegate, rectBounds);
                }
                catch
                {
                }
            }
        }
    }
}
