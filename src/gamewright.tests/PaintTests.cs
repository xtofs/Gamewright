using Gamewright;

namespace Gamewright.Tests;

public class PaintTests
{
    private static readonly Color Red = new(255, 0, 0);
    private static readonly Color Gold = new(215, 183, 126);

    [Fact]
    public void FillAndStrokeAreSingleParts()
    {
        var fill = Paint.Fill(Red);
        var stroke = Paint.Stroke(Gold, 3);

        Assert.Equal(PaintMode.Fill, fill.Mode);
        Assert.True(fill.HasFill);
        Assert.False(fill.HasStroke);
        Assert.Equal(Red.Rgba, fill.FillRgba);

        Assert.Equal(PaintMode.Stroke, stroke.Mode);
        Assert.False(stroke.HasFill);
        Assert.True(stroke.HasStroke);
        Assert.Equal(Gold.Rgba, stroke.StrokeRgba);
        Assert.Equal(3f, stroke.StrokeWidth);
    }

    [Fact]
    public void TheOrderOfConstructionIsThePaintOrder()
    {
        var fillFirst = Paint.Fill(Red).WithStroke(Gold, 4);
        var strokeFirst = Paint.Stroke(Gold, 4).WithFill(Red);

        Assert.Equal(PaintMode.FillThenStroke, fillFirst.Mode);
        Assert.Equal(PaintMode.StrokeThenFill, strokeFirst.Mode);
        Assert.Equal(fillFirst.FillRgba, strokeFirst.FillRgba);
        Assert.Equal(fillFirst.StrokeRgba, strokeFirst.StrokeRgba);
        Assert.Equal(fillFirst.StrokeWidth, strokeFirst.StrokeWidth);
    }

    [Fact]
    public void ReplacingAPartKeepsTheOrder()
    {
        var blue = new Color(0, 0, 255);

        var fillFirst = Paint.Fill(Red).WithStroke(Gold, 4).WithFill(blue).WithStroke(Red, 1);
        var strokeFirst = Paint.Stroke(Gold, 4).WithFill(Red).WithStroke(blue, 2).WithFill(Gold);

        Assert.Equal(PaintMode.FillThenStroke, fillFirst.Mode);
        Assert.Equal(blue.Rgba, fillFirst.FillRgba);
        Assert.Equal(Red.Rgba, fillFirst.StrokeRgba);
        Assert.Equal(1f, fillFirst.StrokeWidth);
        Assert.Equal(PaintMode.StrokeThenFill, strokeFirst.Mode);
        Assert.Equal(Gold.Rgba, strokeFirst.FillRgba);
        Assert.Equal(blue.Rgba, strokeFirst.StrokeRgba);
    }

    [Fact]
    public void ReplacingTheOnlyPartDoesNotAddTheOther()
    {
        Assert.Equal(PaintMode.Fill, Paint.Fill(Red).WithFill(Gold).Mode);
        Assert.Equal(PaintMode.Stroke, Paint.Stroke(Red, 1).WithStroke(Gold, 2).Mode);
    }

    [Fact]
    public void ColorsAndStrokeStylesConvertImplicitly()
    {
        Paint fill = Red;
        Paint stroke = new StrokeStyle(Gold, 2);

        Assert.Equal(Paint.Fill(Red), fill);
        Assert.Equal(Paint.Stroke(Gold, 2), stroke);
    }

    [Fact]
    public void DefaultIsEmpty()
    {
        Assert.True(default(Paint).IsEmpty);
        Assert.Equal(PaintMode.None, default(Paint).Mode);
        Assert.False(default(Paint).HasFill);
        Assert.False(default(Paint).HasStroke);
        Assert.False(Paint.Fill(Red).IsEmpty);
    }

    [Fact]
    public void ExtendingTheDefaultGivesASinglePart()
    {
        Assert.Equal(PaintMode.Fill, default(Paint).WithFill(Red).Mode);
        Assert.Equal(PaintMode.Stroke, default(Paint).WithStroke(Red, 1).Mode);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void StrokeWidthMustBeFiniteAndPositive(float width)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Paint.Stroke(Red, width));
        Assert.Throws<ArgumentOutOfRangeException>(() => Paint.Fill(Red).WithStroke(Gold, width));
    }

    [Fact]
    public void EqualPaintsAreEqual()
    {
        Assert.Equal(Paint.Fill(Red).WithStroke(Gold, 2), Paint.Fill(Red).WithStroke(Gold, 2));
        Assert.NotEqual(Paint.Fill(Red).WithStroke(Gold, 2), Paint.Stroke(Gold, 2).WithFill(Red));
    }
}
