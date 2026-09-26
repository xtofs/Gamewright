float fillAlphaFromDistance(float distanceValue)
{
    float antialiasWidth = fwidth(distanceValue);
    return 1.0 - smoothstep(-antialiasWidth, antialiasWidth, distanceValue);
}

float strokeAlphaFromDistance(float distanceValue, float strokeWidth)
{
    float antialiasWidth = fwidth(distanceValue);
    float halfStrokeWidth = strokeWidth * 0.5;
    return 1.0 - smoothstep(
        halfStrokeWidth - antialiasWidth,
        halfStrokeWidth + antialiasWidth,
        abs(distanceValue));
}
