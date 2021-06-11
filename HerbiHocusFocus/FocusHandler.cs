using System;
using System.Diagnostics;
using System.Drawing;

using interop.UIAutomationCore;

namespace HerbiHocusFocus
{
    internal class FocusHandler : IUIAutomationFocusChangedEventHandler
    {
        private const int c_propertyIdBoundingRectangle = 30001;
        private const int c_propertyIdName = 30005;

        private bool _fAddedEventHandler = false;
        private HerbiHocusFocusForm _mainForm;

        private static HerbiHocusFocusForm.EventHandlerDelegate s_eventHandlerDelegate;

        public FocusHandler(HerbiHocusFocusForm mainForm, HerbiHocusFocusForm.EventHandlerDelegate eventHandlerDelegate)
        {
            this._mainForm = mainForm;

            s_eventHandlerDelegate = eventHandlerDelegate;
        }

        public void Initialize()
        {
            RegisterFocusChangedListener();
        }

        public void Uninitialize()
        {
            UnregisterFocusChangedListener();
        }

        private void RegisterFocusChangedListener()
        {
            IUIAutomationCacheRequest cacheRequest = _mainForm._automation.CreateCacheRequest();
            cacheRequest.AddProperty(c_propertyIdName);
            cacheRequest.AddProperty(c_propertyIdBoundingRectangle);

            // The above properties are all we'll need, so we have have no need for a reference 
            // to the source element when we receive the event. 
            cacheRequest.AutomationElementMode = AutomationElementMode.AutomationElementMode_None;

            _mainForm._automation.AddFocusChangedEventHandler(cacheRequest, this);

            this._fAddedEventHandler = true;
        }

        private void UnregisterFocusChangedListener()
        {
            if (this._fAddedEventHandler)
            {
                this._fAddedEventHandler = false;

                // Barker: Understand the exception here.
                try
                {
                    _mainForm._automation.RemoveFocusChangedEventHandler(this);
                }
                catch 
                {

                }
            }
        }

        public void HandleFocusChangedEvent(IUIAutomationElement sender)
        {
            if (!this._fAddedEventHandler)
            {
                return;
            }

            // Beware ElementNotAvialable for a sender.
            try
            {
                tagRECT rect = sender.CachedBoundingRectangle;
                Rectangle rectBounds = new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);

                string name = sender.CachedName;
                Debug.WriteLine("Focus now on: " + name);

                // Now have the main thread highlight the focused UI. This will return immediately. 
                this._mainForm.BeginInvoke(s_eventHandlerDelegate, name, rectBounds);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("HandleFocusChangedEvent: " + ex.Message);
            }
        }
    }
}
