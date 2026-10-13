using UnityEngine;

namespace RangeControl.Targets
{
    /// <summary>Shot grouping on a paper target.</summary>
    public static class GroupSize
    {
        /// <summary>
        /// The widest distance between any two of the first <paramref name="count"/> holes, in the same unit
        /// as the points. Zero for fewer than two holes.
        /// </summary>
        public static float Compute(Vector2[] holes, int count)
        {
            var widest = 0f;
            for (var i = 0; i < count - 1; i++)
            {
                for (var j = i + 1; j < count; j++)
                {
                    var squared = (holes[i] - holes[j]).sqrMagnitude;
                    if (squared > widest)
                        widest = squared;
                }
            }

            return Mathf.Sqrt(widest);
        }
    }
}
