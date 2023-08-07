namespace ColorPicker.Controls;

public class ColorWheel : ColorPickerViewBase
{
    private readonly ColorCircle _colorCircle = new();
    private readonly AlphaSlider _alphaSlider = new();
    private readonly LuminositySlider _luminositySlider = new();

    protected const double LuminositySliderRowHeight = 12;
    protected const double AlphaSliderRowHeight = 12;

    public static readonly BindableProperty ShowLuminosityWheelProperty
                         = BindableProperty.Create(nameof(ShowLuminosityWheel),
                                                    typeof(bool),
                                                    typeof(ColorWheel),
                                                    true,
                                                    propertyChanged: HandleShowLuminosity);

    public static readonly BindableProperty ShowLuminositySliderProperty
                         = BindableProperty.Create(nameof(ShowLuminositySlider),
                                                    typeof(bool),
                                                    typeof(ColorWheel),
                                                    false,
                                                    propertyChanged: HandleShowLuminositySlider);

    public static readonly BindableProperty ShowAlphaSliderProperty
                         = BindableProperty.Create(nameof(ShowAlphaSlider),
                                                    typeof(bool),
                                                    typeof(ColorWheel),
                                                    false,
                                                    propertyChanged: HandleShowAlphaSlider);

    public static readonly BindableProperty WheelBackgroundColorProperty
                         = BindableProperty.Create(nameof(WheelBackgroundColor),
                                                    typeof(Color),
                                                    typeof(ColorWheel),
                                                    Colors.Transparent,
                                                    propertyChanged: HandleWheelBackgroundColor);

    public static readonly BindableProperty PickerRadiusScaleProperty
                         = BindableProperty.Create(nameof(PickerRadiusScale),
                                                    typeof(float),
                                                    typeof(ColorWheel),
                                                    0.05F,
                                                    propertyChanged: HandlePickerRadiusScale);

    public static readonly BindableProperty VerticalProperty
                         = BindableProperty.Create(nameof(Vertical),
                                                    typeof(bool),
                                                    typeof(ColorWheel),
                                                    false,
                                                    propertyChanged: HandleVertical);

    public bool ShowLuminosityWheel
    {
        get => (bool)GetValue(ShowLuminosityWheelProperty);
        set => SetValue(ShowLuminosityWheelProperty, value);
    }
    private static void HandleShowLuminosity(BindableObject bindable, object oldValue, object newValue)
            => ((ColorWheel)bindable)._colorCircle.ShowLuminosityWheel = (bool)newValue;


    public bool ShowLuminositySlider
    {
        get => (bool)GetValue(ShowLuminositySliderProperty);
        set => SetValue(ShowLuminositySliderProperty, value);
    }

    private static void HandleShowLuminositySlider(BindableObject bindable, object oldValue, object newValue)
            => ((ColorWheel)bindable).UpdateLuminositySlider((bool)newValue);


    public bool ShowAlphaSlider
    {
        get => (bool)GetValue(ShowAlphaSliderProperty);
        set => SetValue(ShowAlphaSliderProperty, value);
    }
    private static void HandleShowAlphaSlider(BindableObject bindable, object oldValue, object newValue)
            => ((ColorWheel)bindable).UpdateAlphaSlider((bool)newValue);


    public Color WheelBackgroundColor
    {
        get => (Color)GetValue(WheelBackgroundColorProperty);
        set => SetValue(WheelBackgroundColorProperty, value);
    }
    private static void HandleWheelBackgroundColor(BindableObject bindable, object oldValue, object newValue)
    {
        if (newValue != oldValue)
            ((ColorWheel)bindable)._colorCircle.WheelBackgroundColor = (Color)newValue;
    }

    public float PickerRadiusScale
    {
        get => (float)GetValue(PickerRadiusScaleProperty);
        set => SetValue(PickerRadiusScaleProperty, value);
    }

    private static void HandlePickerRadiusScale(BindableObject bindable, object oldValue, object newValue)
    {
        if (newValue == oldValue) return;
        ((ColorWheel)bindable)._colorCircle.PickerRadiusScale = (float)newValue;
        ((ColorWheel)bindable)._alphaSlider.PickerRadiusScale = (float)newValue;
        ((ColorWheel)bindable)._luminositySlider.PickerRadiusScale = (float)newValue;
    }

    public bool Vertical
    {
        get => (bool)GetValue(VerticalProperty);
        set => SetValue(VerticalProperty, value);
    }

    private static void HandleVertical(BindableObject bindable, object oldValue, object newValue)
    {
        if (newValue == oldValue) return;
        ((ColorWheel)bindable)._alphaSlider.Vertical = (bool)newValue;
        ((ColorWheel)bindable)._luminositySlider.Vertical = (bool)newValue;
    }


    /// <summary>
    /// Constructor
    /// </summary>
    public ColorWheel()
    {
        _colorCircle.AttachedColorPicker = this;

        HorizontalOptions = LayoutOptions.Center;
        VerticalOptions = LayoutOptions.Center;

        Children.Add(_colorCircle);

        _alphaSlider.AttachedColorPicker = this;
        _luminositySlider.AttachedColorPicker = this;

        UpdateAlphaSlider(ShowAlphaSlider);
        UpdateLuminositySlider(ShowLuminositySlider);
    }

    protected override void OnSelectedColorChanging(Color color) { }

    protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
    {
        if (double.IsPositiveInfinity(widthConstraint) &&
             double.IsPositiveInfinity(heightConstraint))
        {
            widthConstraint = 200;
            heightConstraint = 200;
        }

        var aspectRatio = 1.0;

        if (ShowAlphaSlider)
            aspectRatio -= 0.1;

        if (ShowLuminositySlider)
            aspectRatio -= 0.1;

        double minWidth;
        double minHeight;

        if (Vertical)
        {
            minHeight = Math.Min(heightConstraint, aspectRatio * widthConstraint);
            minWidth = minHeight / aspectRatio;
        }
        else
        {
            minWidth = Math.Min(widthConstraint, aspectRatio * heightConstraint);
            minHeight = minWidth / aspectRatio;
        }

        return new SizeRequest(new Size(minWidth, minHeight));
    }

    protected override void LayoutChildren(double x, double y, double width, double height)
    {
        var circleSize = Vertical ? height : width;

        _colorCircle.Layout(new Rect(x, y, circleSize, circleSize));

        var bottom = Vertical ? x + circleSize
                              : y + width;

        var sliderHeight = _colorCircle.GetPickerRadiusPixels(new SKSize((float)width, (float)height)) * 2.4F;

        if (ShowLuminositySlider)
        {
            _luminositySlider.Layout(Vertical
                ? new Rect(bottom, x, sliderHeight, circleSize)
                : new Rect(x, bottom, circleSize, sliderHeight));

            bottom += sliderHeight;
        }

        if (ShowAlphaSlider)
        {
            _alphaSlider.Layout(Vertical
                ? new Rect(bottom, x, sliderHeight, circleSize)
                : new Rect(x, bottom, circleSize, sliderHeight));
        }
    }

    private void BoundColorPicker_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedColor))
            SelectedColor = ((IColorPicker)sender).SelectedColor;
    }

    private void UpdateAlphaSlider(bool show)
    {
        if (show)
            Children.Add(_alphaSlider);
        else
            Children.Remove(_alphaSlider);
    }

    private void UpdateLuminositySlider(bool show)
    {
        if (show)
            Children.Add(_luminositySlider);
        else
            Children.Remove(_luminositySlider);
    }
}
