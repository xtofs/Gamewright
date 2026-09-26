namespace Gamewright.Graphics.Utilities;

public sealed class ExponentialMovingAverage(int equivalentWindowSize)
{
    private readonly float _smoothingFactor = 2f / (equivalentWindowSize + 1f);
    private bool _hasValue;

    public float Add(float value)
    {
        if (_hasValue)
        {
            Average += _smoothingFactor * (value - Average);
        }
        else
        {
            Average = value;
            _hasValue = true;
        }

        return Average;
    }

    public float Average { get; private set; }
}
