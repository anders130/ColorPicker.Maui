namespace ColorPicker.Controls;

public class ColorCircle : SkiaSharpPickerBase
{
    long? _locationHsProgressId = null;
    long? _locationLProgressId = null;

    SKPoint _locationHs = new();
    SKPoint _locationL = new();

    readonly SKColor[] _sweepGradientColors = new SKColor[256];

    public static readonly BindableProperty ShowLuminosityWheelProperty
                         = BindableProperty.Create(nameof(ShowLuminosityWheel),
                                                    typeof(bool),
                                                    typeof(SkiaSharpPickerBase),
                                                    true,
                                                    propertyChanged: HandleShowLuminosity);
    public bool ShowLuminosityWheel
    {
        get => (bool)GetValue(ShowLuminosityWheelProperty);
        set => SetValue(ShowLuminosityWheelProperty, value);
    }
    private static void HandleShowLuminosity(BindableObject bindable, object oldValue, object newValue)
    {
        if (newValue != oldValue)
            ((ColorCircle)bindable).InvalidateSurface();
    }

    public static readonly BindableProperty WheelBackgroundColorProperty
                         = BindableProperty.Create(nameof(WheelBackgroundColor),
                                                    typeof(Color),
                                                    typeof(IColorPicker),
                                                    Colors.Transparent,
                                                    propertyChanged: HandleWheelBackgroundColor);
    public Color WheelBackgroundColor
    {
        get => (Color)GetValue(WheelBackgroundColorProperty);
        set => SetValue(WheelBackgroundColorProperty, value);
    }
    private static void HandleWheelBackgroundColor(BindableObject bindable, object oldValue, object newValue)
    {
        if (newValue != oldValue)
        {
            ((ColorCircle)bindable).InvalidateSurface();
        }
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public ColorCircle()
    {
        for (var i = 128; i >= -127; i--)
            _sweepGradientColors[255 - (i + 127)] = Color.FromHsla((i < 0 ? 255 + i : i) / 255D, 1, 0.5).ToSKColor();
    }

    public override float GetPickerRadiusPixels() => GetPickerRadiusPixels(GetCanvasSize());
    public override float GetPickerRadiusPixels(SKSize canvasSize) => GetSize(canvasSize) * PickerRadiusScale;

    protected override void OnTouchActionPressed(ColorPickerTouchActionEventArgs args)
    {
        var canvasRadius = GetCanvasSize().Width / 2F;
        var point = ConvertToPixel(args.Location);

        if (_locationHsProgressId is null && IsInHsArea(point, canvasRadius))
        {
            _locationHsProgressId = args.Id;
            _locationHs = LimitToHsRadius(point, canvasRadius);
            UpdateColors(canvasRadius);
        }
        else if (_locationLProgressId is null && IsInLArea(point, canvasRadius))
        {
            _locationLProgressId = args.Id;
            _locationL = LimitToLRadius(point, canvasRadius);
            UpdateColors(canvasRadius);
        }
    }

    protected override void OnTouchActionMoved(ColorPickerTouchActionEventArgs args)
    {
        var canvasRadius = GetCanvasSize().Width / 2F;
        var point = ConvertToPixel(args.Location);

        if (_locationHsProgressId == args.Id)
        {
            _locationHs = LimitToHsRadius(point, canvasRadius);
            UpdateColors(canvasRadius);
        }
        else if (_locationLProgressId == args.Id)
        {
            _locationL = LimitToLRadius(point, canvasRadius);
            UpdateColors(canvasRadius);
        }
    }

    protected override void OnTouchActionReleased(ColorPickerTouchActionEventArgs args)
    {
        var canvasRadius = GetCanvasSize().Width / 2F;
        var point = ConvertToPixel(args.Location);

        if (_locationHsProgressId == args.Id)
        {
            _locationHsProgressId = null;
            _locationHs = LimitToHsRadius(point, canvasRadius);
            UpdateColors(canvasRadius);
        }
        else if (_locationLProgressId == args.Id)
        {
            _locationLProgressId = null;
            _locationL = LimitToLRadius(point, canvasRadius);
            UpdateColors(canvasRadius);
        }
    }

    protected override void OnTouchActionCancelled(ColorPickerTouchActionEventArgs args)
    {
        if (_locationHsProgressId == args.Id)
            _locationHsProgressId = null;
        else if (_locationLProgressId == args.Id)
            _locationLProgressId = null;
    }

    protected override void OnPaintSurface(SKCanvas canvas, int width, int height)
    {
        var canvasRadius = GetSize() / 2F;

        UpdateLocations(SelectedColor, canvasRadius);
        canvas.Clear();
        PaintBackground(canvas, canvasRadius);

        if (ShowLuminosityWheel)
        {
            PaintLGradient(canvas, canvasRadius);
            PaintPicker(canvas, _locationL);
        }

        PaintColorSweepGradient(canvas, canvasRadius);
        PaintGrayRadialGradient(canvas, canvasRadius);
        PaintPicker(canvas, _locationHs);
    }

    protected override void OnSelectedColorChanging(Color color)
            => InvalidateSurface();

    protected override SizeRequest GetMeasure(double widthConstraint, double heightConstraint)
    {
        if (double.IsPositiveInfinity(widthConstraint) &&
             double.IsPositiveInfinity(heightConstraint))
        {
            widthConstraint = 200;
            heightConstraint = 200;
        }

        var size = Math.Min(widthConstraint, heightConstraint);

        return new SizeRequest(new Size(size, size));
    }

    protected override float GetSize(SKSize canvasSize) => canvasSize.Width;
    protected override float GetSize() => GetSize(GetCanvasSize());

    private void UpdateLocations(Color color, float canvasRadius)
    {
        if (color.GetLuminosity() != 0 || !IsInHsArea(_locationHs, canvasRadius))
        {
            var angleHs = (0.5 - color.GetHue()) * (2 * Math.PI);
            var radiusHs = WheelHsRadius(canvasRadius) * color.GetSaturation();

            var resultHs = FromPolar(new PolarPoint(radiusHs, (float)angleHs));
            resultHs.X += canvasRadius;
            resultHs.Y += canvasRadius;
            _locationHs = resultHs;
        }

        var polarL = ToPolar(ToWheelLCoordinates(_locationL, canvasRadius));
        polarL.Angle -= (float)Math.PI / 2F;
        var signOld = polarL.Angle <= 0 ? 1 : -1;
        var angleL = color.GetLuminosity() * Math.PI * signOld;

        var resultL = FromPolar(new PolarPoint(WheelLRadius(canvasRadius), (float)(angleL - (Math.PI / 2))));
        resultL.X += canvasRadius;
        resultL.Y += canvasRadius;
        _locationL = resultL;
    }

    private void UpdateColors(float canvasRadius)
    {
        var wheelHsPoint = ToWheelHsCoordinates(_locationHs, canvasRadius);
        var wheelLPoint = ToWheelLCoordinates(_locationL, canvasRadius);

        var newColor = WheelPointToColor(wheelHsPoint, wheelLPoint);
        SelectedColor = newColor;
    }

    private bool IsInHsArea(SKPoint point, float canvasRadius)
    {
        var polar = ToPolar(new SKPoint(point.X - canvasRadius, point.Y - canvasRadius));
        return polar.Radius <= WheelHsRadius(canvasRadius);
    }

    private bool IsInLArea(SKPoint point, float canvasRadius)
    {
        if (!ShowLuminosityWheel)
            return false;

        var polar = ToPolar(new SKPoint(point.X - canvasRadius, point.Y - canvasRadius));

        return polar.Radius <= WheelLRadius(canvasRadius) + (GetPickerRadiusPixels() / 2F)
            && polar.Radius >= WheelLRadius(canvasRadius) - (GetPickerRadiusPixels() / 2F);
    }

    private void PaintBackground(SKCanvas canvas, float canvasRadius)
    {
        var center = new SKPoint(canvasRadius, canvasRadius);

        var paint = new SKPaint
        {
            IsAntialias = true,
            Color = WheelBackgroundColor.ToSKColor()
        };

        canvas.DrawCircle(center, canvasRadius - GetPickerRadiusPixels(), paint);
    }

    private void PaintLGradient(SKCanvas canvas, float canvasRadius)
    {
        var center = new SKPoint(canvasRadius, canvasRadius);

        var colors = new List<SKColor>()
        {
            Color.FromHsla(SelectedColor.GetHue(), SelectedColor.GetSaturation(), 0.5).ToSKColor(),
            Color.FromHsla(SelectedColor.GetHue(), SelectedColor.GetSaturation(), 1.0).ToSKColor(),
            Color.FromHsla(SelectedColor.GetHue(), SelectedColor.GetSaturation(), 0.5).ToSKColor(),
            Color.FromHsla(SelectedColor.GetHue(), SelectedColor.GetSaturation(), 0.0).ToSKColor(),
            Color.FromHsla(SelectedColor.GetHue(), SelectedColor.GetSaturation(), 0.5).ToSKColor()
        };

        var shader = SKShader.CreateSweepGradient(center, colors.ToArray(), null);

        var paint = new SKPaint
        {
            IsAntialias = true,
            Shader = shader,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = GetPickerRadiusPixels()
        };
        canvas.DrawCircle(center, WheelLRadius(canvasRadius), paint);
    }

    private void PaintColorSweepGradient(SKCanvas canvas, float canvasRadius)
    {
        var center = new SKPoint(canvasRadius, canvasRadius);

        var shader = SKShader.CreateSweepGradient(center, _sweepGradientColors, null);

        var paint = new SKPaint
        {
            IsAntialias = true,
            Shader = shader,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawCircle(center, WheelHsRadius(canvasRadius), paint);
    }

    private void PaintGrayRadialGradient(SKCanvas canvas, float canvasRadius)
    {
        var center = new SKPoint(canvasRadius, canvasRadius);

        var colors = new[]
        {
            SKColors.Gray,
            SKColors.Transparent
        };

        var shader = SKShader.CreateRadialGradient(center, WheelHsRadius(canvasRadius), colors, null, SKShaderTileMode.Clamp);

        var paint = new SKPaint
        {
            IsAntialias = true,
            Shader = shader,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawPaint(paint);
    }

    private SKPoint ToWheelHsCoordinates(SKPoint point, float canvasRadius)
    {
        var result = new SKPoint(point.X, point.Y);

        result.X -= canvasRadius;
        result.Y -= canvasRadius;
        result.X /= WheelHsRadius(canvasRadius);
        result.Y /= WheelHsRadius(canvasRadius);

        return result;
    }

    private SKPoint ToWheelLCoordinates(SKPoint point, float canvasRadius)
    {
        var result = new SKPoint(point.X, point.Y);

        result.X -= canvasRadius;
        result.Y -= canvasRadius;
        result.X /= WheelLRadius(canvasRadius);
        result.Y /= WheelLRadius(canvasRadius);

        return result;
    }

    private Color WheelPointToColor(SKPoint pointHs, SKPoint pointL)
    {
        var polarHs = ToPolar(pointHs);
        var polarL = ToPolar(pointL);

        polarL.Angle += (float)Math.PI / 2F;
        polarL = ToPolar(FromPolar(polarL));

        var h = (Math.PI - polarHs.Angle) / (2 * Math.PI);
        var s = polarHs.Radius;
        var l = Math.Abs(polarL.Angle) / Math.PI;

        return Color.FromHsla(h, s, l, SelectedColor.Alpha);
    }

    private SKPoint LimitToHsRadius(SKPoint point, float canvasRadius)
    {
        var polar = ToPolar(new SKPoint(point.X - canvasRadius, point.Y - canvasRadius));
        polar.Radius = polar.Radius < WheelHsRadius(canvasRadius) ? polar.Radius : WheelHsRadius(canvasRadius);
        var result = FromPolar(polar);

        result.X += canvasRadius;
        result.Y += canvasRadius;

        return result;
    }

    private SKPoint LimitToLRadius(SKPoint point, float canvasRadius)
    {
        var polar = ToPolar(new SKPoint(point.X - canvasRadius, point.Y - canvasRadius));
        polar.Radius = WheelLRadius(canvasRadius);
        var result = FromPolar(polar);

        result.X += canvasRadius;
        result.Y += canvasRadius;

        return result;
    }

    private static PolarPoint ToPolar(SKPoint point)
    {
        var radius = (float)Math.Sqrt((point.X * point.X) + (point.Y * point.Y));
        var angle = (float)Math.Atan2(point.Y, point.X);

        return new PolarPoint(radius, angle);
    }

    private static SKPoint FromPolar(PolarPoint point)
    {
        var x = (float)(point.Radius * Math.Cos(point.Angle));
        var y = (float)(point.Radius * Math.Sin(point.Angle));

        return new SKPoint(x, y);
    }

    private float WheelHsRadius(float canvasRadius)
       => !ShowLuminosityWheel ? canvasRadius - GetPickerRadiusPixels()
                                : canvasRadius - (3 * GetPickerRadiusPixels()) - 2;

    private float WheelLRadius(float canvasRadius)
       => canvasRadius - GetPickerRadiusPixels();
}
