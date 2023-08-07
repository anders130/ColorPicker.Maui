namespace ColorPicker.Classes;

public class SliderLocation
{
    public SliderLocation(SliderBase slider) => Slider = slider;

    public SliderBase Slider { get; }

    public long? LocationProgressId { get; set; }
    public float OffsetLocationMultiplier { get; set; }
    public SKPoint Location { get; set; } = new();

    public float GetSliderOffset(float pickerRadiusPixels) => pickerRadiusPixels * OffsetLocationMultiplier;
}
