using System;

namespace SharpSteer2.Helpers
{
    public class RandomHelpers
    {
        [ThreadStatic] private static Random _rng;

        private static Random rng
        {
            get
            {
                if (_rng == null)
                    _rng = new Random();
                return _rng;
            }
        }

        // ----------------------------------------------------------------------------
        // Random number utilities

        // Returns a float randomly distributed between 0 and 1

        public static float Random()
        {
            return (float)rng.NextDouble();
        }

        // Returns a float randomly distributed between lowerBound and upperBound

        public static float Random(float lowerBound, float upperBound)
        {
            return lowerBound + (Random() * (upperBound - lowerBound));
        }

        public static int RandomInt(int min, int max)
        {
            return (int)Random(min, max);
        }
    }
}
