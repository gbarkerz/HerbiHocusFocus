using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Windows.Forms;

using interop.UIAutomationCore;
using Microsoft.Win32;

namespace HerbiHocusFocus
{
    public partial class HerbiHocusFocusForm : Form 
    {
        private FocusHandler _focusHandler;
        private CaretHandler _caretHandler;

        private static HighlightForm _highlightForm;
        private static SpeechSynthesizer _synth;

        private static bool _fClosing = false;
        private static bool _speakEnabled = false;

        public delegate void EventHandlerDelegate(string strName, Rectangle rectBounds);
        public delegate void CaretWinEventHandlerDelegate(Rectangle rectBounds);
        public delegate void WinEventHandlerDelegate();

        public static EventHandlerDelegate s_eventHandlerDelegate;
        public static WinEventHandlerDelegate s_winEventHandlerDelegate;
        public static CaretWinEventHandlerDelegate s_caretWinEventHandlerDelegate;

        public const uint msgLinkerKeyboardHook = (0x0400 /*WM_APP*/ + 0x0123 /*Some random number I made up.*/);
        private static Win32Interop.LowLevelKeyboardProc s_procKeyboardHook = KeyboardHookCallback;
        private static IntPtr s_keyboardHookID = IntPtr.Zero;
        private static IntPtr s_mainFormHandle = IntPtr.Zero;
        private static bool caretHighlightEnabled = true;
        private static int _caretHotkey = 0;
        float _dpiY;

        public const int VK_F1 = 0x70;

        // Barker: Make this private.
        public IUIAutomation _automation;

        private static HerbiHocusFocusForm s_mainForm;
        
        public HerbiHocusFocusForm()
        {
            s_mainForm = this;

            Win32Interop.SetProcessDPIAware();

            InitializeComponent();

            // For converting paragrpah data in point size later.
            Graphics g = this.CreateGraphics();
            this._dpiY = g.DpiY;
            g.Dispose();

            // Barker: Process.Start() not allowed in a Store app.
            // this.linkLabelHerbi.LinkClicked += LinkLabelHerbi_LinkClicked;

            this._automation = new CUIAutomation8();

            s_eventHandlerDelegate = new EventHandlerDelegate(HandleEventOnUIThread);
            s_winEventHandlerDelegate = new WinEventHandlerDelegate(HandleWinEventOnUIThread);

            s_caretWinEventHandlerDelegate = new CaretWinEventHandlerDelegate(HandleCaretWinEventOnUIThread);

            _focusHandler = new FocusHandler(this, s_eventHandlerDelegate);
            _focusHandler.Initialize();

            _caretHandler = new CaretHandler(this, s_caretWinEventHandlerDelegate);
            _caretHandler.Initialize();

            _highlightForm = new HighlightForm(
                this, s_winEventHandlerDelegate, s_caretWinEventHandlerDelegate);

            _highlightForm.Initialize(
                Settings1.Default.HighlightFocus,
                Settings1.Default.HighlightCaretAbove,
                Settings1.Default.HighlightCaretBelow,
                Settings1.Default.Color,
                Settings1.Default.ColorCaret,
                Settings1.Default.ColorUnderline,
                Settings1.Default.Thickness,
                Settings1.Default.Margin,
                Settings1.Default.MarginCaret,
                Settings1.Default.MarginUnderline,
                Settings1.Default.SizeCaret,
                Settings1.Default.SizeUnderline,
                Settings1.Default.Style);

            comboBoxThickness.SelectedIndex = Settings1.Default.Thickness - 1;
            comboBoxMargin.SelectedIndex = Settings1.Default.Margin;

            comboBoxTransparencyCaret.SelectedIndex = Settings1.Default.HighlightCaretTransparency;
            comboBoxMarginCaret.SelectedIndex = Settings1.Default.MarginCaret;

            comboBoxSizeCaret.SelectedIndex = (Settings1.Default.SizeCaret - 10) / 5;

            comboBoxSizeUnderline.SelectedIndex = (Settings1.Default.SizeUnderline - 10) / 5;
            comboBoxMarginUnderline.SelectedIndex = Settings1.Default.MarginUnderline;
            comboBoxTransparencyUnderline.SelectedIndex = Settings1.Default.UnderlineTransparency;

            int index = 0; 
            if (Settings1.Default.CaretHotkey > 0)
            {
                index = Settings1.Default.CaretHotkey - VK_F1 + 1;
            }

            comboBoxCaretHotkey.SelectedIndex = index;

            int selIndex = 0;

            switch (Settings1.Default.Style)
            {
                case DashStyle.Dot:
                    selIndex = 1;
                    break;
                case DashStyle.Dash:
                    selIndex = 2;
                    break;
                case DashStyle.DashDot:
                    selIndex = 3;
                    break;
                case DashStyle.DashDotDot:
                    selIndex = 4;
                    break;
            }

            checkBoxHighlightFocus.Checked = Settings1.Default.HighlightFocus;

            checkBoxCaretAbove.Checked = Settings1.Default.HighlightCaretAbove;
            checkBoxCaretBelow.Checked = Settings1.Default.HighlightCaretBelow;
            checkBoxHighlightTextLine.Checked = Settings1.Default.HighlightTextLine;

            comboBoxStyle.SelectedIndex = selIndex;

            checkBoxStartMinimized.Checked = Settings1.Default.StartMinimized;

            if (checkBoxStartMinimized.Checked)
            {
                this.WindowState = FormWindowState.Minimized;
            }

            checkBoxSpeak.Checked = Settings1.Default.SpeakOn;

            try
            {
                _synth = new SpeechSynthesizer();
            }
            catch
            {
                // Allow this sample to run even if speech is not available. 
                checkBoxSpeak.Enabled = false;

                _synth = null;
            }

            _highlightForm.Visible = true;

            // Create the keyboard hook.
            using (Process curProcess = Process.GetCurrentProcess())
            {
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    s_keyboardHookID = Win32Interop.SetWindowsHookEx(
                        Win32Interop.WH_KEYBOARD_LL,
                        s_procKeyboardHook,
                        Win32Interop.GetModuleHandle(curModule.ModuleName), 0);
                }
            }

            s_mainFormHandle = this.Handle;

            Win32Interop.SetWindowPos(
                _highlightForm.Handle,
                Win32Interop.HWND.TopMost,
                0, 0, 0, 0,
                Win32Interop.SWP.NOMOVE | Win32Interop.SWP.NOSIZE | Win32Interop.SWP.NOACTIVATE);

            AccountForHighContrastTheme();

            SystemEvents.UserPreferenceChanged += this.SystemEvents_UserPreferenceChanged;

            // If the app wanted to access the customer's current choices for the 
            // "Show notifications for" and "Play animations in Windows" settings 
            // at the Ease of Access Settings app, it could do so like this:

            //uint timeoutRequired;
            //NativeMethods.SystemParametersInfo(NativeMethods.SPI_GETMESSAGEDURATION, 0, out timeoutRequired, 0);
            //// Deliver the experience my customer needs with timeoutRequired.

            //uint animationsEnabled;
            //NativeMethods.SystemParametersInfo(NativeMethods.SPI_GETMENUANIMATION, 0, out animationsEnabled, 0);
            //// Deliver the experience my customer needs with animationsEnabled.
        }

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if ((e.Category == UserPreferenceCategory.Color) || (e.Category == UserPreferenceCategory.VisualStyle))
            {
                AccountForHighContrastTheme();
            }
        }

        private void AccountForHighContrastTheme()
        {
            string imagename = "HHF";

            if (SystemInformation.HighContrast)
            {
                imagename += "_HC" +
                    (SystemColors.Control.GetBrightness() < SystemColors.ControlText.GetBrightness() ? "B" : "W");
            }

            pictureBox1.Image = Properties.Resources.ResourceManager.GetObject(imagename) as Bitmap;
        }

        private void HerbiHocusFocusForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Debug.WriteLine("App closing Start");

            _fClosing = true;

            Settings1.Default.Save();

            Debug.WriteLine("App closing: Uninitialze Focus");

            if (_focusHandler != null)
            {
                _focusHandler.Uninitialize();
            }

            Debug.WriteLine("App closing: Uninitialze Caret");

            if (_caretHandler != null)
            {
                _caretHandler.Uninitialize();
            }

            Debug.WriteLine("App closing: Uninitialze Highlight Form");

            if (_highlightForm != null)
            {
                _highlightForm.Uninitialize();
            }

            Debug.WriteLine("App closing Done");
        }

        // Barker: Process.Start() is not allowed in a Store app.
        //private void LinkLabelHerbi_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        //{
        //    // Specify that the link was visited.
        //    this.linkLabelHerbi.LinkVisited = true;

        //    Process.Start("http://herbi.org/HerbiHocusFocus/HerbiHocusFocus.htm");
        //}

        protected override void OnClosing(CancelEventArgs e)
        {
            _fClosing = true;

            if ((_synth != null) && _speakEnabled)
            {
                Debug.WriteLine("Canceling in-progress speech on close.");
                _synth.SpeakAsyncCancelAll();
            }

            // Remove the keyboard hook if we created it earlier.
            if (s_keyboardHookID != IntPtr.Zero)
            {
                Win32Interop.UnhookWindowsHookEx(s_keyboardHookID);
            }

            base.OnClosing(e);
        }

        private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if ((nCode >= 0) && (wParam == (IntPtr)Win32Interop.WM_KEYDOWN))
            {
                Win32Interop.KbLLHookStruct kbData = (Win32Interop.KbLLHookStruct)Marshal.PtrToStructure(lParam, typeof(Win32Interop.KbLLHookStruct));

                bool fProcessed = false;

                if ((_caretHotkey != 0) && (kbData.vkCode == _caretHotkey))
                {
                    Win32Interop.PostMessage(
                        s_mainFormHandle,
                        msgLinkerKeyboardHook,
                        kbData.vkCode, 0);

                    fProcessed = true;
                }

                if (fProcessed)
                {
                    // Prevent the message being processed further. A shipping app would consider carefully whether
                    // the key press should be allowed to trigger additional action in the foreground app.
                    return (IntPtr)1;
                }
            }

            // Pass on the key message.
            return Win32Interop.CallNextHookEx(s_keyboardHookID, nCode, wParam, lParam);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == msgLinkerKeyboardHook)
            {
                ToggleCaretHighlight();
            }

            base.WndProc(ref m);
        }

        private void ToggleCaretHighlight()
        {
            caretHighlightEnabled = !caretHighlightEnabled;

            _highlightForm.Refresh();
        }

        public bool IsCaretTrackingEnabled()
        {
            return caretHighlightEnabled;
        }

        public void GetSelectionRect(ref Rectangle rect)
        {
            rect.X = 0;
            rect.Y = 0;
            rect.Width = 0;
            rect.Height = 0;

            int patternIdText = 10014; // UIA_TextPatternId
            IUIAutomationCacheRequest cacheRequestTextPattern =
                _automation.CreateCacheRequest();
            cacheRequestTextPattern.AddPattern(patternIdText);

            try
            {
                IUIAutomationElement element = _automation.GetFocusedElementBuildCache(cacheRequestTextPattern);
                if (element != null)
                {
                    // Does the element support the Text pattern?
                    IUIAutomationTextPattern textPattern =
                        element.GetCachedPattern(patternIdText);
                    if (textPattern != null)
                    {
                        IUIAutomationTextRangeArray array = textPattern.GetSelection();
                        if ((array != null) && (array.Length > 0))
                        {
                            IUIAutomationTextRange range = array.GetElement(0);
                            if (range != null)
                            {
                                // We either have a degenerative range where there is no current selection, or
                                // a selection. In the former case, we expand to include a single character.
                                // In the latter case we collpase to a single character. Either was we take
                                // the same action.

                                // Barker: Account for the caret being at the end of a selection. This action
                                // will always move the range to the start of the selection.

                                range.ExpandToEnclosingUnit(TextUnit.TextUnit_Character);

                                // Now try to get the bounding range of the range,
                                double[] rects = range.GetBoundingRectangles();
                                if ((rects == null) || (rects.Length < 4))
                                {
                                    // We still don't have bounds, so maybe the caret is at the send of line
                                    // and it can't be expanded forward. If so, try to expand ik backwards.

                                    range.MoveEndpointByUnit(TextPatternRangeEndpoint.TextPatternRangeEndpoint_Start,
                                        TextUnit.TextUnit_Character, -1);

                                    range.ExpandToEnclosingUnit(TextUnit.TextUnit_Character);

                                    rects = range.GetBoundingRectangles();

                                    // We know we moved backwards, so the X of interest is at the right edge of the rect.
                                    if ((rects != null) && (rects.Length > 3))
                                    {
                                        rects[0] += rects[2];
                                    }
                                }

                                // Now to we have some usable bounds?
                                if ((rects != null) && (rects.Length > 3))
                                {
                                    rect = new Rectangle((int)rects[0], (int)rects[1], (int)rects[2], (int)rects[3]);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + " " + ex.StackTrace);                
            }
        }

        // Cache the UIA element that currently has keyboard focus, so we don't need to retrieve
        // it every time the caret moves.
        private IUIAutomationElement _elementFocused;

        // Call this in response to every change in keyboard focus or caret position.
        public void HighlightCurrentTextLineAsAppropriate(bool focusChanged)
        {
            // Are we currently highlighting the line containing the caret? 
            if (!checkBoxHighlightTextLine.Checked)
            {
                // No, so make sure the window used for highlighting is invisible.
                _highlightForm._formTextLine.Visible = false;

                return;
            }

            // Using a managed wrapper around the Windows UIA API, (rather than the .NET UIA API),
            // I tend to hard-code UIA-related values picked up from UIAutomationClient.h.

            int propertyIdControlType = 30003; // UIA_ControlTypePropertyId
            int patternIdText = 10014; // UIA_TextPatternId

            // Are we here in response to a focus change?
            if (focusChanged)
            {
                // Hide the highlight until we know we can get the data we need.
                _highlightForm._formTextLine.Visible = false;

                // Create a cache request so that we access data we know we'll need,
                // in the fewest cross-proc calls as possible.
                IUIAutomationCacheRequest cacheRequest = _automation.CreateCacheRequest();
                cacheRequest.AddProperty(propertyIdControlType);
                cacheRequest.AddPattern(patternIdText);

                // Until I figure out the occassional interop exception, wrap this in a try/catch.
                try
                {
                    // Get the UIA element currently containing keybaord focus. (Note: if I rearrange some code
                    // I could avoid making this call, and instead use the element which originally supplied the
                    // UIA FocusChanged event.)
                    IUIAutomationElement elementFocusNew = _automation.GetFocusedElementBuildCache(cacheRequest);
                    if (elementFocusNew == null)
                    {
                        // If we failed to get the focused element, give up.
                        return;
                    }

                    // For this first version, only work with Document and Edit controls.
                    int CtrlType = elementFocusNew.CachedControlType;
                    if ((CtrlType != 50030) && // Document 
                        (CtrlType != 50004)) //Edit
                    {
                        Debug.WriteLine("Newly focused element is neither Document nor Edit, so reset.");

                        this._elementFocused = null;
                    }
                    else
                    {
                        Debug.WriteLine("Newly focused element is one of Document nor Edit, so use it.");

                        this._elementFocused = elementFocusNew;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message + " " + ex.StackTrace);
                }
            }

            try
            { 
                // Do we know which element contains keyboard focus?
                if (this._elementFocused != null)
                {
                    // Does the element support the Text pattern?
                    IUIAutomationTextPattern textPattern = this._elementFocused.GetCachedPattern(patternIdText);
                    if (textPattern != null)
                    {
                        // Get the current selection from the element. If there is no selection,
                        // then we'll get data back relating to where the caret currently is.
                        IUIAutomationTextRangeArray array = textPattern.GetSelection();
                        if ((array != null) && (array.Length > 0))
                        {
                            // For this version of the feature, only consider the first selection range 
                            // if there are multiple selections in the app.
                            IUIAutomationTextRange range = array.GetElement(0);
                            if (range != null)
                            {
                                // Expand the range to encompass the entire line containing the caret.
                                range.ExpandToEnclosingUnit(TextUnit.TextUnit_Line);

                                // Now get the bounding rect for that line.
                                double[] rects = range.GetBoundingRectangles();
                                if ((rects != null) && (rects.Length > 3))
                                {
                                    Rectangle rectLineText = new Rectangle(
                                        (int)rects[0],
                                        (int)rects[1],
                                        (int)rects[2],
                                        (int)rects[3]);

                                    // Is this the last line in the paragraph?
                                    IUIAutomationTextRange rangeParagraph = range.Clone();
                                    rangeParagraph.ExpandToEnclosingUnit(TextUnit.TextUnit_Paragraph);

                                    int compareResult = rangeParagraph.CompareEndpoints(
                                        TextPatternRangeEndpoint.TextPatternRangeEndpoint_End,
                                        range,
                                        TextPatternRangeEndpoint.TextPatternRangeEndpoint_End);

                                    Debug.WriteLine("Comparison result for line end point and paragraph end point: " + compareResult);

                                    if (compareResult == 0)
                                    {
                                        // Offset the line to account for paragraph after spacing;
                                        const int afterSpacingId = 40042; //  UIA_AfterParagraphSpacingAttributeId
                                        double? afterSpacing = range.GetAttributeValue(afterSpacingId) as double?;

                                        int underlineAfterSpacingPoints = (afterSpacing == null ? 0 : (int)afterSpacing.Value);

                                        Debug.WriteLine("Paragraph after spacing points : " + underlineAfterSpacingPoints);

                                        if (underlineAfterSpacingPoints != 0)
                                        {
                                            int underlineAfterSpacingPixels = (int)(underlineAfterSpacingPoints * this._dpiY) / 72;

                                            Debug.WriteLine("Paragraph after spacing pixels : " + underlineAfterSpacingPixels);

                                            rectLineText.Offset(0, -underlineAfterSpacingPixels);
                                        }
                                    }

                                    // We now know the bounding rect of interest. Move our highlight window
                                    // so that it appears at the bottom edge of the bounding rect.
                                    _highlightForm.HighlightCurrentTextLine(rectLineText);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + " " + ex.StackTrace);
            }
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // The rect is top/left corner and width/height.
        public static void HandleEventOnUIThread(string strName, Rectangle rectBounding)
        {
            if (_fClosing)
            {
                return;
            }

            Debug.WriteLine("Handle focus changed");

            // Stop highlighting the caret until we get another caret-related event.
            _highlightForm.SetGotCaretEvent(false);

            CaretHandler.gotCaretWinEvent = false;

            _highlightForm.Ready = true;

            _highlightForm.HighlightRect(rectBounding);

            if ((_synth != null) && _speakEnabled)
            {
                Debug.WriteLine("Canceling in-progress speech.");
                _synth.SpeakAsyncCancelAll();

                _synth.SpeakAsync(strName);
            }

            // Try to highlight the caret if appropriate.
            _highlightForm.HighlightCurrentCaretPosition();

            // Highlight the line of text as appropriate.
            s_mainForm.HighlightCurrentTextLineAsAppropriate(true /*Focus changed*/);
        }

        public static void HandleWinEventOnUIThread()
        {
            // Note: We move the highlight in response to ANY event here.

            // Don't get into a fight with other apps, so wait 100 ms since we last did this.
            // Actually, dont bother íf we're only looking at menus.
            //DateTime currentTime = DateTime.Now;
            //if (currentTime - this._timePrevious3MoveHighlightForm > new TimeSpan(0, 0, 0, 0, 250))

            Debug.WriteLine("Moving highlight to front now.");

            Win32Interop.SetWindowPos(
                _highlightForm.Handle,
                Win32Interop.HWND.TopMost,
                0, 0, 0, 0,
                Win32Interop.SWP.NOMOVE | Win32Interop.SWP.NOSIZE | Win32Interop.SWP.NOACTIVATE);
        }

        public static void HandleCaretWinEventOnUIThread(Rectangle rectBounding)
        {
            // Note: We move the highlight in response to ANY event here.

            // Don't get into a fight with other apps, so wait 100 ms since we last did this.
            // Actually, dont bother íf we're only looking at menus.
            //DateTime currentTime = DateTime.Now;
            //if (currentTime - this._timePrevious3MoveHighlightForm > new TimeSpan(0, 0, 0, 0, 250))

            if (_fClosing)
            {
                return;
            }

            Debug.WriteLine("Moving caret highlight now.");

            _highlightForm.Ready = true;

            _highlightForm.HighlightCaret(rectBounding);

            Win32Interop.SetWindowPos(
                _highlightForm.Handle,
                Win32Interop.HWND.TopMost,
                0, 0, 0, 0,
                Win32Interop.SWP.NOMOVE | Win32Interop.SWP.NOSIZE | Win32Interop.SWP.NOACTIVATE);
        }

        private void buttonColor_Click(object sender, EventArgs e)
        {
            var dlg = new ColorSelector(Settings1.Default.Color);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _highlightForm.SetColor(dlg.SelectedColor);

                Settings1.Default.Color = dlg.SelectedColor;
            }
        }

        private void comboBoxThickness_SelectedIndexChanged(object sender, EventArgs e)
        {
            int width = (sender as ComboBox).SelectedIndex + 1;

            _highlightForm.SetThickness(width);

            Settings1.Default.Thickness = width;
        }

        private void comboBoxMargin_SelectedIndexChanged(object sender, EventArgs e)
        {
            int margin = (sender as ComboBox).SelectedIndex;

            _highlightForm.SetMargin(margin);

            Settings1.Default.Margin = margin;
        }

        private void comboBoxStyle_SelectedIndexChanged(object sender, EventArgs e)
        {
            DashStyle style = DashStyle.Solid;

            int index = (sender as ComboBox).SelectedIndex;

            switch (index)
            {
                case 1:
                    style = DashStyle.Dot;
                    break;
                case 2:
                    style = DashStyle.Dash;
                    break;
                case 3:
                    style = DashStyle.DashDot;
                    break;
                case 4:
                    style = DashStyle.DashDotDot;
                    break;
            }

            _highlightForm.SetStyle(style);

            Settings1.Default.Style = style;
        }

        private void checkBoxSpeak_CheckedChanged(object sender, EventArgs e)
        {
            _speakEnabled = checkBoxSpeak.Checked;

            Settings1.Default.SpeakOn = _speakEnabled;
        }

        private void checkBoxStartMinimized_CheckedChanged(object sender, EventArgs e)
        {
            Settings1.Default.StartMinimized = checkBoxStartMinimized.Checked;
        }

        private void buttonColourCaret_Click(object sender, EventArgs e)
        {
            var dlg = new ColorSelector(Settings1.Default.ColorCaret);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _highlightForm.SetColorCaret(dlg.SelectedColor);

                Settings1.Default.ColorCaret = dlg.SelectedColor;
            }
        }


        private void buttonColourUnderline_Click(object sender, EventArgs e)
        {
            var dlg = new ColorSelector(Settings1.Default.ColorUnderline);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _highlightForm.SetColorUnderline(dlg.SelectedColor);

                Settings1.Default.ColorUnderline = dlg.SelectedColor;
            }
        }

        private void comboBoxMarginCaret_SelectedIndexChanged(object sender, EventArgs e)
        {
            int margin = (sender as ComboBox).SelectedIndex;

            _highlightForm.SetMarginCaret(margin);

            Settings1.Default.MarginCaret = margin;
        }


        private void comboBoxMarginUnderline_SelectedIndexChanged(object sender, EventArgs e)
        {
            int margin = (sender as ComboBox).SelectedIndex;

            _highlightForm.SetMarginUnderline(margin);

            Settings1.Default.MarginUnderline = margin;
        }

        private void comboBoxCaretHotkey_SelectedIndexChanged(object sender, EventArgs e)
        {
            int hotkey = (sender as ComboBox).SelectedIndex;

            if (hotkey != 0)
            {
                hotkey += VK_F1 - 1;
            }

            _caretHotkey = hotkey;

            Settings1.Default.CaretHotkey = hotkey;
        }

        private void comboBoxTransparencyCaret_SelectedIndexChanged(object sender, EventArgs e)
        {
            int transparency = (sender as ComboBox).SelectedIndex;

            _highlightForm.SetTransparencyCaret(transparency);

            Settings1.Default.HighlightCaretTransparency = transparency;
        }

        private void comboBoxTransparencyUnderline_SelectedIndexChanged(object sender, EventArgs e)
        {
            int transparency = (sender as ComboBox).SelectedIndex;

            _highlightForm.SetTransparencyUnderline(transparency);

            Settings1.Default.UnderlineTransparency = transparency;
        }

        private void checkBoxCaretAbove_CheckedChanged(object sender, EventArgs e)
        {
            bool highlightCaretAbove = (sender as CheckBox).Checked;

            _highlightForm.SetHighlightCaretAbove(highlightCaretAbove);

            Settings1.Default.HighlightCaretAbove = highlightCaretAbove;
        }

        private void checkBoxCaretBelow_CheckedChanged(object sender, EventArgs e)
        {
            bool highlightCaretBelow = (sender as CheckBox).Checked;

            _highlightForm.SetHighlightCaretBelow(highlightCaretBelow);

            Settings1.Default.HighlightCaretBelow = highlightCaretBelow;
        }

        private void comboBoxSizeCaret_SelectedIndexChanged(object sender, EventArgs e)
        {
            int size = 10 + ((sender as ComboBox).SelectedIndex * 5);

            _highlightForm.SetSizeCaret(size);

            Settings1.Default.SizeCaret = size;
        }

        private void comboBoxSizeUnderline_SelectedIndexChanged(object sender, EventArgs e)
        {
            int size = 10 + ((sender as ComboBox).SelectedIndex * 5);

            _highlightForm.SetSizeUnderline(size);

            Settings1.Default.SizeUnderline = size;
        }

        private void checkBoxHighlightFocus_CheckedChanged(object sender, EventArgs e)
        {
            bool highlightFocus = (sender as CheckBox).Checked;

            _highlightForm.SetHighlightFocus(highlightFocus);

            Settings1.Default.HighlightFocus = highlightFocus;
        }

        private void checkBoxHighlightTextLine_CheckedChanged(object sender, EventArgs e)
        {
            bool highlightTextLine = (sender as CheckBox).Checked;

            _highlightForm.SetHighlightTextLine(highlightTextLine);

            Settings1.Default.HighlightTextLine = highlightTextLine;
        }
    }

    public class NativeMethods
    {
        public const int SPI_GETMENUANIMATION = 0x1042;
        public const int SPI_GETMESSAGEDURATION = 0x2016;

        [DllImport("user32.dll", EntryPoint = "SystemParametersInfo")]
        public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, out uint pvParam, uint fWinIni);
    }
}
