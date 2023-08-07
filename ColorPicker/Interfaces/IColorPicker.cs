namespace ColorPicker.Interfaces;

public interface IColorPicker : INotifyPropertyChanged
{
    public Color SelectedColor { get; set; }
    public IColorPicker AttachedColorPicker { get; set; }
}
