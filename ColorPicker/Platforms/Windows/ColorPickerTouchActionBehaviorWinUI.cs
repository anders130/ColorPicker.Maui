namespace ColorPicker.Platforms.WinUI;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

public class ColorPickerTouchActionBehaviorWinUi : Behavior<SkiaSharpPickerBase>
{
    private Action<Element, ColorPickerTouchActionEventArgs> _onTouchAction;
    private FrameworkElement _frameworkElement;
    private ColorPickerTouchBehavior _sharedBehavior;
    private Element _boundElement;

    public ColorPickerTouchActionBehaviorWinUi(ColorPickerTouchBehavior sharedBehavior)
    {
        ArgumentNullException.ThrowIfNull(sharedBehavior);

        _sharedBehavior = sharedBehavior;
    }

    protected override void OnAttachedTo(BindableObject sender)
    {
        if (sender is not SkiaSharpPickerBase bindable)
            return;

        bindable.HandlerChanged += OnHandlerChanged;
        base.OnAttachedTo(bindable);
    }

    private void OnHandlerChanged(object sender, EventArgs e)
    {
        if (sender is not SkiaSharpPickerBase bindable)
            return;

        // Get the Windows FrameworkElement corresponding to the Element that the Behavior is attached to
        _boundElement = bindable;
        var context = bindable.Handler?.MauiContext ?? bindable.Parent.Handler.MauiContext;
        _frameworkElement = bindable.ToPlatform(context);

        if (_sharedBehavior is null || _frameworkElement is null) return;
        // Save the method to call on touch events
        _onTouchAction = _sharedBehavior.OnTouchAction;

        // Set event handlers on FrameworkElement
        _frameworkElement.PointerEntered += OnPointerEntered;
        _frameworkElement.PointerPressed += OnPointerPressed;
        _frameworkElement.PointerMoved += OnPointerMoved;
        _frameworkElement.PointerReleased += OnPointerReleased;
        _frameworkElement.PointerExited += OnPointerExited;
        _frameworkElement.PointerCanceled += OnPointerCancelled;
    }

    protected override void OnDetachingFrom(SkiaSharpPickerBase bindable)
    {
        bindable.HandlerChanged -= OnHandlerChanged;

        if (_onTouchAction is not null)
        {
            // Release event handlers on FrameworkElement
            _frameworkElement.PointerEntered -= OnPointerEntered;
            _frameworkElement.PointerPressed -= OnPointerPressed;
            _frameworkElement.PointerMoved -= OnPointerMoved;
            _frameworkElement.PointerReleased -= OnPointerReleased;
            _frameworkElement.PointerExited -= OnPointerEntered;
            _frameworkElement.PointerCanceled -= OnPointerCancelled;
        }

        base.OnDetachingFrom(bindable);
    }

    private void OnPointerEntered(object sender, PointerRoutedEventArgs args)
            => CommonHandler(sender, ColorPickerTouchActionType.Entered, args);

    private void OnPointerMoved(object sender, PointerRoutedEventArgs args)
            => CommonHandler(sender, ColorPickerTouchActionType.Moved, args);

    private void OnPointerReleased(object sender, PointerRoutedEventArgs args)
            => CommonHandler(sender, ColorPickerTouchActionType.Released, args);

    private void OnPointerExited(object sender, PointerRoutedEventArgs args)
            => CommonHandler(sender, ColorPickerTouchActionType.Exited, args);

    private void OnPointerCancelled(object sender, PointerRoutedEventArgs args)
            => CommonHandler(sender, ColorPickerTouchActionType.Cancelled, args);

    private void OnPointerPressed(object sender, PointerRoutedEventArgs args)
    {
        CommonHandler(sender, ColorPickerTouchActionType.Pressed, args);

        // Check setting of Capture property
        if (_sharedBehavior.Capture)
            (sender as FrameworkElement)?.CapturePointer(args.Pointer);
    }

    private void CommonHandler(object sender, ColorPickerTouchActionType touchActionType, PointerRoutedEventArgs args)
    {
        var pointerPoint = args.GetCurrentPoint(sender as UIElement);
        var winPoint = pointerPoint.Position;

        _onTouchAction(_boundElement,
                        new ColorPickerTouchActionEventArgs(args.Pointer.PointerId,
                                                             touchActionType,
                                                             new Point(winPoint.X, winPoint.Y),
                                                             args.Pointer.IsInContact));
    }
}
