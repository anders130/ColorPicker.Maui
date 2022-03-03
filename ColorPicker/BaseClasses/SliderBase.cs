﻿namespace ColorPicker.BaseClasses;

public abstract class SliderBase
{
    public bool             PaintChessPattern   { get; set; }

    public abstract float   NewValue( Color color );
    public abstract Color   GetNewColor( float newValue, Color oldColor );
    public abstract SKPaint GetPaint( Color color, SKPoint startPoint, SKPoint endPoint );
}
