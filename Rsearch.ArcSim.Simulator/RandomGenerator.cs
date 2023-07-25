using System;
namespace Rsearch.ArcSim.Simulator
{
    public class RandomGenerator
    {
        private Random random;

        public RandomGenerator()
        {
            random = new Random();
        }

        // Generate 'n' random non-negative integers with a given average and variance
        public int[] GenerateRandomNonNegativeIntegers(int n, double desiredAverage, double desiredVariance)
        {
            int[] randomIntegers = new int[n];

            // Calculate the standard deviation from the variance
            double standardDeviation = Math.Sqrt(desiredVariance);

            // Generate 'n' random numbers with mean 0 and variance 1
            double[] randomNumbers = new double[n];
            for (int i = 0; i < n; i++)
            {
                double u1 = random.NextDouble();
                double u2 = random.NextDouble();
                double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
                randomNumbers[i] = randStdNormal;
            }

            // Convert to desired mean and variance
            for (int i = 0; i < n; i++)
            {
                double scaledNumber = desiredAverage + randomNumbers[i] * standardDeviation;
                randomIntegers[i] = Math.Max(0, (int)Math.Round(scaledNumber));
            }

            return randomIntegers;
        }
    }

}

