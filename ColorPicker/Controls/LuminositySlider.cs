namespace ColorPicker.Controls;

public class LuminositySlider : SliderPicker
{
    protected override IEnumerable<SliderBase> GetSliders()
        => new SliderBase[]
            {
                new Slider( SliderFunctionsHsl.NewValueL,
                            SliderFunctionsHsl.GetNewColorL,
                            SliderFunctionsHsl.GetPaintL )
            };
}
