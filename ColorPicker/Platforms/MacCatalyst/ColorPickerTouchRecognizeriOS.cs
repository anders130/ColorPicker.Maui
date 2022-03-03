﻿using CoreGraphics;
using Foundation;
using UIKit;
using ColorPicker.Platform.IOS;

namespace ColorPicker.iOS.Effects
{
    class ColorPickerTouchRecognizer : UIGestureRecognizer
    {
        Element element;        // Forms element for firing events
        UIView view;            // iOS UIView 
        ColorPickerTouchRoutingEffect touchEffect;
        bool capture;

        static Dictionary<UIView, ColorPickerTouchRecognizer> viewDictionary =
            new Dictionary<UIView, ColorPickerTouchRecognizer>();

        static Dictionary<long, ColorPickerTouchRecognizer> idToTouchDictionary =
            new Dictionary<long, ColorPickerTouchRecognizer>();

        public ColorPickerTouchRecognizer(Element element, UIView view, ColorPickerTouchRoutingEffect touchEffect)
        {
            this.element = element;
            this.view = view;
            this.touchEffect = touchEffect;

            viewDictionary.Add(view, this);
        }

        public void Detach()
        {
            viewDictionary.Remove(view);
        }

        // touches = touches of interest; evt = all touches of type UITouch
        public override void TouchesBegan(NSSet touches, UIEvent evt)
        {
            base.TouchesBegan(touches, evt);

            foreach (UITouch touch in touches.Cast<UITouch>())
            {
                long id = touch.Handle.ToInt64();
                FireEvent(this, id, ColorPickerTouchActionType.Pressed, touch, true);

                if (!idToTouchDictionary.ContainsKey(id))
                {
                    idToTouchDictionary.Add(id, this);
                }
            }

            // Save the setting of the Capture property
            capture = touchEffect.Capture;
        }

        public override void TouchesMoved(NSSet touches, UIEvent evt)
        {
            base.TouchesMoved(touches, evt);

            foreach (UITouch touch in touches.Cast<UITouch>())
            {
                long id = touch.Handle.ToInt64();

                if (capture)
                {
                    FireEvent(this, id, ColorPickerTouchActionType.Moved, touch, true);
                }
                else
                {
                    CheckForBoundaryHop(touch);

                    if (idToTouchDictionary[id] is not null)
                    {
                        FireEvent(idToTouchDictionary[id], id, ColorPickerTouchActionType.Moved, touch, true);
                    }
                }
            }
        }

        public override void TouchesEnded(NSSet touches, UIEvent evt)
        {
            base.TouchesEnded(touches, evt);

            foreach (UITouch touch in touches.Cast<UITouch>())
            {
                long id = touch.Handle.ToInt64();

                if (capture)
                {
                    FireEvent(this, id, ColorPickerTouchActionType.Released, touch, false);
                }
                else
                {
                    CheckForBoundaryHop(touch);

                    if (idToTouchDictionary[id] is not null)
                    {
                        FireEvent(idToTouchDictionary[id], id, ColorPickerTouchActionType.Released, touch, false);
                    }
                }
                idToTouchDictionary.Remove(id);
            }
        }

        public override void TouchesCancelled(NSSet touches, UIEvent evt)
        {
            base.TouchesCancelled(touches, evt);

            foreach (UITouch touch in touches.Cast<UITouch>())
            {
                long id = touch.Handle.ToInt64();

                if (capture)
                {
                    FireEvent(this, id, ColorPickerTouchActionType.Cancelled, touch, false);
                }
                else if (idToTouchDictionary[id] is not null)
                {
                    FireEvent(idToTouchDictionary[id], id, ColorPickerTouchActionType.Cancelled, touch, false);
                }
                idToTouchDictionary.Remove(id);
            }
        }

        void CheckForBoundaryHop(UITouch touch)
        {
            long id = touch.Handle.ToInt64();

            // TODO: Might require converting to a List for multiple hits
            ColorPickerTouchRecognizer recognizerHit = null;

            foreach (UIView view in viewDictionary.Keys)
            {
                CGPoint location = touch.LocationInView(view);

                if (new CGRect(new CGPoint(), view.Frame.Size).Contains(location))
                {
                    recognizerHit = viewDictionary[view];
                }
            }
            if (recognizerHit != idToTouchDictionary[id])
            {
                if (idToTouchDictionary[id] is not null)
                {
                    FireEvent(idToTouchDictionary[id], id, ColorPickerTouchActionType.Exited, touch, true);
                }
                if (recognizerHit is not null)
                {
                    FireEvent(recognizerHit, id, ColorPickerTouchActionType.Entered, touch, true);
                }
                idToTouchDictionary[id] = recognizerHit;
            }
        }

        void FireEvent(ColorPickerTouchRecognizer recognizer, long id, ColorPickerTouchActionType actionType, UITouch touch, bool isInContact)
        {
            // Convert touch location to Xamarin.Forms Point value
            CGPoint cgPoint = touch.LocationInView(recognizer.View);
            Point xfPoint = new Point(cgPoint.X, cgPoint.Y);

            // Get the method to call for firing events
            Action<Element, ColorPickerTouchActionEventArgs> onTouchAction = recognizer.touchEffect.OnTouchAction;

            // Call that method
            onTouchAction(recognizer.element,
                new ColorPickerTouchActionEventArgs(id, actionType, xfPoint, isInContact));
        }
    }
}
