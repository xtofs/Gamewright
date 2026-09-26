namespace Gamewright.Graphics.Utilities;

public static class EnumerableExtensions
{
    extension(Enumerable)
    {

        /// <summary>
        /// all elements of the Cartesian product of ranges 0..dim1 and 0..dim2
        /// </summary>
        /// <param name="dim1"></param>
        /// <param name="dim2"></param>
        /// <returns></returns>
        public static IEnumerable<(int, int)> Cartesian(int dim1, int dim2)
        {
            for (var i = 0; i < dim1; i++)
            {
                for (var j = 0; j < dim2; j++)
                {
                    yield return (i, j);
                }
            }
        }

        /// <summary>
        /// all elements of the Cartesian product of ranges 0..dim1 and 0..dim2
        /// </summary>
        /// <param name="dim1"></param>
        /// <param name="dim2"></param>
        /// <returns></returns>
        public static IEnumerable<(int, int, int)> Cartesian(int dim1, int dim2, int dim3)
        {
            for (var i = 0; i < dim1; i++)
            {
                for (var j = 0; j < dim2; j++)
                {
                    for (var k = 0; k < dim3; k++)
                    {
                        yield return (i, j, k);
                    }
                }
            }
        }

        // enumerate the cartesian product of the given ranges
        public static IEnumerable<int[]> Range(params (int, int)[] range)
        {
            var indices = new int[range.Length];
            while (true)
            {
                yield return (int[])indices.Clone();

                var i = range.Length - 1;
                while (i >= 0)
                {
                    indices[i]++;
                    if (indices[i] < range[i].Item2)
                    {
                        break;
                    }
                    indices[i] = range[i].Item1;
                    i--;
                }
                if (i < 0)
                {
                    yield break;
                }
            }
        }
    }
}
