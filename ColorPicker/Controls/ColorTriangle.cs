namespace ColorPicker.Controls;

public class ColorTriangle : SkiaSharpPickerBase
{
    private double _lastHue = 0;
    private bool _zeroSl = false;
    private long? _locationSvProgressId = null;
    private long? _locationHProgressId = null;
    private SKPoint _locationSv = new();
    private SKPoint _locationH1 = new();
    private SKPoint _locationH2 = new();
    private SKPoint _locationMiddleH = new();

    private readonly SKColor[] _sweepGradientColors = new SKColor[256];

    public static readonly BindableProperty WheelBackgroundColorProperty
                         = BindableProperty.Create(nameof(WheelBackgroundColor),
                                                    typeof(Color),
                                                    typeof(IColorPicker),
                                                    Colors.Transparent,
                                                    propertyChanged: HandleWheelBackgroundColorSet);
    public Color WheelBackgroundColor
    {
        get => (Color)GetValue(WheelBackgroundColorProperty);
        set => SetValue(WheelBackgroundColorProperty, value);
    }

    private static void HandleWheelBackgroundColorSet(BindableObject bindable, object oldValue, object newValue)
    {
        if (newValue != oldValue)
        {
            ((ColorTriangle)bindable).InvalidateSurface();
        }
    }

    public static readonly BindableProperty RotateTriangleByHueProperty
                         = BindableProperty.Create(nameof(RotateTriangleByHue),
                                                    typeof(bool),
                                                    typeof(ColorTriangle),
                                                    true,
                                                    propertyChanged: HandleRotateTriangleByHueSet);
    public bool RotateTriangleByHue
    {
        get => (bool)GetValue(RotateTriangleByHueProperty);
        set => SetValue(RotateTriangleByHueProperty, value);
    }

    private static void HandleRotateTriangleByHueSet(BindableObject bindable, object oldValue, object newValue)
    {
        if (newValue != oldValue)
        {
            ((ColorTriangle)bindable).InvalidateSurface();
        }
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public ColorTriangle() : base()
    {
        PickerRadiusScale = 0.035F;
        for (var i = 128; i >= -127; i--)
        {
            _sweepGradientColors[255 - (i + 127)] = Color.FromHsla((i < 0 ? 255 + i : i) / 255D, 1, 0.5).ToSKColor();
        }
    }

    public override float GetPickerRadiusPixels(SKSize canvasSize) => GetSize(canvasSize) * PickerRadiusScale;
    public override float GetPickerRadiusPixels() => GetPickerRadiusPixels(GetCanvasSize());

    protected override void OnTouchActionPressed(ColorPickerTouchActionEventArgs args)
    {
        var canvasRadius = GetCanvasSize().Width / 2F;
        var point = ConvertToPixel(args.Location);

        if (_locationSvProgressId is null && IsInSvArea(point, canvasRadius))
        {
            _locationSvProgressId = args.Id;
            _locationSv = LimitToSvTriangle(point, canvasRadius);
            UpdateColors(canvasRadius);
        }
        else if (_locationHProgressId is null && IsInHArea(point, canvasRadius))
        {
            _locationHProgressId = args.Id;
            LimitToHRadius(point, canvasRadius);
            UpdateColors(canvasRadius);
        }
    }

    protected override void OnTouchActionMoved(ColorPickerTouchActionEventArgs args)
    {
        var canvasRadius = GetCanvasSize().Width / 2F;
        var point = ConvertToPixel(args.Location);

        if (_locationSvProgressId == args.Id)
        {
            _locationSv = LimitToSvTriangle(point, canvasRadius);
            UpdateColors(canvasRadius);
        }
        else if (_locationHProgressId == args.Id)
        {
            LimitToHRadius(point, canvasRadius);
            UpdateColors(canvasRadius);
        }
    }

    protected override void OnTouchActionReleased(ColorPickerTouchActionEventArgs args)
    {
        var canvasRadius = GetCanvasSize().Width / 2F;
        var point = ConvertToPixel(args.Location);

        if (_locationSvProgressId == args.Id)
        {
            _locationSvProgressId = null;
            _locationSv = LimitToSvTriangle(point, canvasRadius);
            UpdateColors(canvasRadius);
        }
        else if (_locationHProgressId == args.Id)
        {
            _locationHProgressId = null;
            LimitToHRadius(point, canvasRadius);
            UpdateColors(canvasRadius);
        }
    }

    protected override void OnTouchActionCancelled(ColorPickerTouchActionEventArgs args)
    {
        if (_locationSvProgressId == args.Id)
            _locationSvProgressId = null;
        else if (_locationHProgressId == args.Id)
            _locationHProgressId = null;
    }

    protected override void OnPaintSurface(SKCanvas canvas, int width, int height)
    {
        var canvasRadius = GetSize() / 2F;

        UpdateLocations(SelectedColor, canvasRadius);
        canvas.Clear();

        PaintBackground(canvas, canvasRadius);
        PaintHGradient(canvas, canvasRadius);

        if (RotateTriangleByHue)
            PaintLinePicker(canvas);
        else
            PaintPicker(canvas, _locationMiddleH);

        PaintSvTriangle(canvas, canvasRadius);
        PaintPicker(canvas, _locationSv);
    }

    protected override void OnSelectedColorChanging(Color color)
    {
        if (color.GetSaturation() > 0.00390625D)
        {
            _lastHue = color.GetHue();
            _zeroSl = false;
        }
        else
        {
            _zeroSl = true;
        }

        InvalidateSurface();
    }

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
        ColorToHsv(color, out _, out var saturation, out var value);

        var luminosityX = -(float)((2 * TriangleSide * saturation) - TriangleSide);
        var luminosityY = TriangleHeight;

        var polarValue = ToPolar(new SKPoint(luminosityX, luminosityY));
        polarValue.Radius *= (float)value;

        _locationSv = FromPolar(polarValue);
        _locationSv.X = -_locationSv.X;
        _locationSv.Y -= 1;
        _locationSv.X *= WheelSvRadius(canvasRadius);
        _locationSv.Y *= WheelSvRadius(canvasRadius);

        polarValue = ToPolar(new SKPoint(_locationSv.X, _locationSv.Y));
        polarValue.Angle -= (float)(2 * Math.PI / 3);
        _locationSv = FromPolar(polarValue);

        _locationSv.X += canvasRadius;
        _locationSv.Y += canvasRadius;

        if (RotateTriangleByHue)
        {
            var rotationHue = SKMatrix.CreateRotation(-(float)((2D * Math.PI * _lastHue) + (Math.PI / 2D)), canvasRadius, canvasRadius);
            _locationSv = rotationHue.MapPoint(_locationSv);
        }

        var angleH = _lastHue * Math.PI * 2;

        _locationMiddleH = FromPolar(new PolarPoint(WheelHRadius(canvasRadius), (float)(Math.PI - angleH)));
        _locationMiddleH.X += canvasRadius;
        _locationMiddleH.Y += canvasRadius;

        _locationH1 = FromPolar(new PolarPoint(WheelHRadius(canvasRadius) + GetPickerRadiusPixels(), (float)(Math.PI - angleH)));
        _locationH1.X += canvasRadius;
        _locationH1.Y += canvasRadius;

        _locationH2 = FromPolar(new PolarPoint(WheelHRadius(canvasRadius) - GetPickerRadiusPixels(), (float)(Math.PI - angleH)));
        _locationH2.X += canvasRadius;
        _locationH2.Y += canvasRadius;
    }

    private void UpdateColors(float canvasRadius)
    {
        var wheelSvPoint = ToWheelSvCoordinates(_locationSv, canvasRadius);
        var wheelHPoint = ToWheelHCoordinates(_locationH1, canvasRadius);
        var newColor = WheelPointToColor(wheelSvPoint, wheelHPoint);

        if (_zeroSl && (newColor.GetSaturation() > 0))
        {
            newColor = Color.FromHsla(_lastHue, newColor.GetSaturation(), newColor.GetLuminosity(), newColor.Alpha);
        }

        SelectedColor = newColor;
    }

    private bool IsInSvArea(SKPoint point, float canvasRadius)
    {
        var polar = ToPolar(new SKPoint(point.X - canvasRadius, point.Y - canvasRadius));
        return polar.Radius <= WheelSvRadius(canvasRadius);
    }

    private bool IsInHArea(SKPoint point, float canvasRadius)
    {
        var polar = ToPolar(new SKPoint(point.X - canvasRadius, point.Y - canvasRadius));
        return polar.Radius <= WheelHRadius(canvasRadius) + GetPickerRadiusPixels() && polar.Radius >= WheelHRadius(canvasRadius) - GetPickerRadiusPixels();
    }

    private void PaintBackground(SKCanvas canvas, float canvasRadius)
    {
        var center = new SKPoint(canvasRadius, canvasRadius);
        var paint = new SKPaint
        {
            IsAntialias = true,
            Color = WheelBackgroundColor.ToSKColor()
        };

        canvas.DrawCircle(center, canvasRadius, paint);
    }

    private void PaintHGradient(SKCanvas canvas, float canvasRadius)
    {
        var center = new SKPoint(canvasRadius, canvasRadius);
        var shader = SKShader.CreateSweepGradient(center, _sweepGradientColors, null);

        var paint = new SKPaint
        {
            IsAntialias = true,
            Shader = shader,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = GetPickerRadiusPixels() * 2
        };
        canvas.DrawCircle(center, WheelHRadius(canvasRadius), paint);
    }

    private void PaintSvTriangle(SKCanvas canvas, float canvasRadius)
    {
        canvas.Save();

        var rotationHue = SKMatrix.CreateRotation(-(float)((2D * Math.PI * _lastHue) + (Math.PI / 2D)),
                                                   canvasRadius, canvasRadius);

        if (RotateTriangleByHue)
        {
            canvas.SetMatrix(rotationHue);
        }

        var point1 = new SKPoint(canvasRadius, canvasRadius - WheelSvRadius(canvasRadius));
        var point2 = new SKPoint(canvasRadius + (TriangleSide * WheelSvRadius(canvasRadius))
                , canvasRadius + (TriangleVerticalOffset * WheelSvRadius(canvasRadius)));

        var point3 = new SKPoint(canvasRadius - (TriangleSide * WheelSvRadius(canvasRadius))
                , canvasRadius + (TriangleVerticalOffset * WheelSvRadius(canvasRadius)));

        using (var pathTriangle = new SKPath())
        {
            pathTriangle.MoveTo(point1);
            pathTriangle.LineTo(point2);
            pathTriangle.LineTo(point3);

            canvas.ClipPath(pathTriangle, SKClipOperation.Intersect, true);
        }

        var matrix = SKMatrix.CreateRotation(-(float)Math.PI / 3F, point3.X, point3.Y);

        if (RotateTriangleByHue)
        {
            SKMatrix.Concat(ref matrix, rotationHue, matrix);
        }

        var shader = SKShader.CreateSweepGradient(point3,
                                                   new[]
                                                   {
                                                       Color.FromHsla(_lastHue, 1, 0.5).ToSKColor(),
                                                       Colors.White.ToSKColor(),
                                                       Color.FromHsla(_lastHue, 1, 0.5).ToSKColor()
                                                   },
                                                   new[]
                                                   {
                                                       0F, 0.16666666666666F, 1F
                                                   });

        var paint = new SKPaint
        {
            IsAntialias = true,
            Shader = shader,
            Style = SKPaintStyle.Fill
        };

        canvas.SetMatrix(matrix);
        canvas.DrawCircle(point3, WheelSvRadius(canvasRadius) * 2, paint);

        if (RotateTriangleByHue)
        {
            canvas.SetMatrix(rotationHue);
        }
        else
        {
            canvas.ResetMatrix();
        }

        var colors = new[]
        {
            SKColors.Black,
            SKColors.Transparent
        };

        PaintGradient(canvas, canvasRadius, colors, point3);

        canvas.ResetMatrix();
        canvas.Restore();
    }

    private void PaintGradient(SKCanvas canvas, float canvasRadius, SKColor[] colors, SKPoint centerGradient)
    {
        var center = new SKPoint(canvasRadius, canvasRadius);
        var polar = ToPolar(new SKPoint(center.X - centerGradient.X, center.Y - centerGradient.Y));

        polar.Radius *= TriangleHeight;

        var p2 = FromPolar(polar);
        p2.X += centerGradient.X;
        p2.Y += centerGradient.Y;

        var shader = SKShader.CreateLinearGradient(centerGradient, p2, colors, null, SKShaderTileMode.Clamp);

        var paint = new SKPaint
        {
            IsAntialias = true,
            Shader = shader,
            Style = SKPaintStyle.Fill
        };

        canvas.DrawCircle(center, WheelSvRadius(canvasRadius), paint);
    }

    private SKPoint ToWheelSvCoordinates(SKPoint point, float canvasRadius)
    {
        var result = new SKPoint(point.X, point.Y);

        result.X -= canvasRadius;
        result.Y -= canvasRadius;
        result.X /= WheelSvRadius(canvasRadius);
        result.Y /= WheelSvRadius(canvasRadius);

        return result;
    }

    private SKPoint ToWheelHCoordinates(SKPoint point, float canvasRadius)
    {
        var result = new SKPoint(point.X, point.Y);

        result.X -= canvasRadius;
        result.Y -= canvasRadius;
        result.X /= WheelHRadius(canvasRadius);
        result.Y /= WheelHRadius(canvasRadius);

        return result;
    }

    private const float TriangleHeight = 1.5000001F;
    private const float TriangleSide = 0.8660244F;
    private const float TriangleVerticalOffset = 0.5000001F;

    private Color WheelPointToColor(SKPoint pointSv, SKPoint pointH)
    {
        if (RotateTriangleByHue)
        {
            var rotationHue = SKMatrix.CreateRotation((float)((2D * Math.PI * _lastHue) + (Math.PI / 2D)));
            pointSv = rotationHue.MapPoint(pointSv);
        }

        var polarH = ToPolar(pointH);
        var h = (-polarH.Angle + Math.PI) / (2 * Math.PI);

        pointSv.Y = -pointSv.Y + TriangleVerticalOffset;
        pointSv.X += TriangleSide;

        var x1 = TriangleSide;
        var y1 = TriangleHeight;
        var x2 = x1 * 2;
        var y2 = 0F;

        var vCurrent = ((pointSv.X * (y2 - y1)) - (pointSv.Y * (x2 - x1)) + (x2 * y1) - (y2 * x1)) / Math.Sqrt(Math.Pow(y2 - y1, 2) + Math.Pow(x2 - x1, 2));
        var v = (y1 - vCurrent) / y1;

        var sMax = x2 - (vCurrent / Math.Sin(Math.PI / 3));
        var sCurrent = pointSv.Y / Math.Sin(Math.PI / 3);
        var s = sCurrent / sMax;

        _lastHue = h;
        var result = ColorFromHsv(h, s, v, SelectedColor.Alpha);

        return result;
    }

    private SKPoint LimitToSvTriangle(SKPoint point, float canvasRadius)
    {
        var polar = ToPolar(new SKPoint(point.X - canvasRadius, point.Y - canvasRadius));
        polar.Radius = polar.Radius < WheelSvRadius(canvasRadius) ? polar.Radius : WheelSvRadius(canvasRadius);

        var result = FromPolar(polar);
        result.X += canvasRadius;
        result.Y += canvasRadius;
        return result;
    }

    private void LimitToHRadius(SKPoint point, float canvasRadius)
    {
        var point1 = ToPolar(new SKPoint(point.X - canvasRadius, point.Y - canvasRadius));
        point1.Radius = WheelHRadius(canvasRadius) + GetPickerRadiusPixels();

        var point2 = ToPolar(new SKPoint(point.X - canvasRadius, point.Y - canvasRadius));
        point1.Radius = WheelHRadius(canvasRadius) - GetPickerRadiusPixels();

        _locationH1 = FromPolar(point1);
        _locationH2 = FromPolar(point2);

        _locationH1.X += canvasRadius;
        _locationH1.Y += canvasRadius;
        _locationH2.X += canvasRadius;
        _locationH2.Y += canvasRadius;
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

    private float WheelSvRadius(float canvasRadius) => canvasRadius - (2 * GetPickerRadiusPixels()) - 2;
    private float WheelHRadius(float canvasRadius) => canvasRadius - GetPickerRadiusPixels();

    private void PaintLinePicker(SKCanvas canvas)
    {
        var paint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };

        paint.Color = Colors.Black.ToSKColor();
        paint.StrokeWidth = 4;

        using var pathTriangle = new SKPath();
        pathTriangle.MoveTo(_locationH1);
        pathTriangle.LineTo(_locationH2);

        canvas.DrawPath(pathTriangle, paint);
    }

    public static void ColorToHsv(Color color, out double hue, out double saturation, out double value)
    {
        var rgb = new Rgb { R = Math.Round(color.Red * 255F), G = Math.Round(color.Green * 255F), B = Math.Round(color.Blue * 255F) };
        var hsv = rgb.To<Hsv>();

        hue = color.GetHue();
        saturation = hsv.S;
        value = hsv.V;
    }

    public static Color ColorFromHsv(double hue, double saturation, double value, double a)
    {
        var result = Color.FromHsv((float)hue, (float)saturation, (float)value);
        return new Color(result.Red, result.Green, result.Blue, (float)a);
    }
}
