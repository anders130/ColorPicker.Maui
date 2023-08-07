namespace ColorPicker.Classes;

public class PolarPoint
{
    private float _angle;
    public float Angle
    {
        get => _angle;
        set => _angle = (float)Math.Atan2(Math.Sin(value), Math.Cos(value));
    }

    public float Radius { get; set; }

    public PolarPoint(float radius, float angle)
    {
        Radius = radius;
        Angle = angle;
    }

    public override string ToString() => $"Radius: {Radius}; Angle: {Angle}";
}
