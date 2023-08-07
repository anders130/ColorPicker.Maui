namespace ColorPicker.Controls;

public class HslSliders : SliderPickerWithAlpha
{
    protected override IEnumerable<SliderBase> GetSliders()
    {
        var result = new List<Slider>()
            {
                new(SliderFunctionsHsl.NewValueH,
                            SliderFunctionsHsl.GetNewColorH,
                            SliderFunctionsHsl.GetPaintH),

                new(SliderFunctionsHsl.NewValueS,
                            SliderFunctionsHsl.GetNewColorS,
                            SliderFunctionsHsl.GetPaintS),

                new(SliderFunctionsHsl.NewValueL,
                            SliderFunctionsHsl.GetNewColorL,
                            SliderFunctionsHsl.GetPaintL)
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
