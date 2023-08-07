namespace ColorPicker.Controls;

public class RgbSliders : SliderPickerWithAlpha
{
    protected override IEnumerable<SliderBase> GetSliders()
    {
        var result = new List<Slider>()
            {
                new(SliderFunctionsRgb.NewValueR,
                            SliderFunctionsRgb.GetNewColorR,
                            SliderFunctionsRgb.GetPaintR),

                new(SliderFunctionsRgb.NewValueG,
                            SliderFunctionsRgb.GetNewColorG,
                            SliderFunctionsRgb.GetPaintG),

                new(SliderFunctionsRgb.NewValueB,
                            SliderFunctionsRgb.GetNewColorB,
                            SliderFunctionsRgb.GetPaintB)
            };

        if (!ShowAlphaSlider) return result;
        var slider = new Slider(SliderFunctionsAlpha.NewValueAlpha,
            SliderFunctionsAlpha.GetNewColorAlpha,
            SliderFunctionsAlpha.GetPaintAlpha)
        {
            PaintChessPattern = true
        };
        result.Add(slider);

        return result;
    }
}
