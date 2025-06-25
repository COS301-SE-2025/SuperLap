using System;
using System.Collections.Generic;

namespace RacelineOptimizer
{
    public class SavitzkyGolayFilter
    {
        private readonly int windowSize;
        private readonly int polynomialOrder;
        private readonly float[] coefficients;

        public SavitzkyGolayFilter(int windowSize, int polynomialOrder)
        {
            if (windowSize % 2 == 0 || windowSize < 3)
                throw new ArgumentException("Window size must be an odd number >= 3.");

            if (polynomialOrder >= windowSize)
                throw new ArgumentException("Polynomial order must be less than window size.");

            this.windowSize = windowSize;
            this.polynomialOrder = polynomialOrder;
            this.coefficients = GenerateCoefficients(windowSize, polynomialOrder);
        }

        public List<float> Smooth(List<float> data)
        {
            int half = windowSize / 2;
            int n = data.Count;
            List<float> result = new(data);

            for (int i = 0; i < n; i++)
            {
                float sum = 0f;
                for (int j = -half; j <= half; j++)
                {
                    int index = Math.Clamp(i + j, 0, n - 1);
                    sum += data[index] * coefficients[j + half];
                }
                result[i] = sum;
            }

            return result;
        }

        private float[] GenerateCoefficients(int m, int order)
        {
            int n = order + 1;
            double[,] A = new double[n, n];
            double[] b = new double[n];

            for (int i = 0; i < n; i++)
            {
                b[i] = i == 0 ? 1.0 : 0.0;
                for (int j = 0; j < n; j++)
                {
                    double sum = 0.0;
                    for (int k = -m / 2; k <= m / 2; k++)
                        sum += Math.Pow(k, i + j);
                    A[i, j] = sum;
                }
            }

            double[] solution = SolveLinearSystem(A, b);
            float[] result = new float[m];

            for (int k = -m / 2; k <= m / 2; k++)
            {
                double sum = 0.0;
                for (int i = 0; i < n; i++)
                    sum += solution[i] * Math.Pow(k, i);
                result[k + m / 2] = (float)sum;
            }

            return result;
        }

        private double[] SolveLinearSystem(double[,] A, double[] b)
        {
            int n = b.Length;
            double[,] mat = new double[n, n + 1];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                    mat[i, j] = A[i, j];
                mat[i, n] = b[i];
            }

            for (int i = 0; i < n; i++)
            {
                int max = i;
                for (int j = i + 1; j < n; j++)
                    if (Math.Abs(mat[j, i]) > Math.Abs(mat[max, i]))
                        max = j;

                for (int k = 0; k <= n; k++)
                {
                    double temp = mat[i, k];
                    mat[i, k] = mat[max, k];
                    mat[max, k] = temp;
                }

                for (int j = i + 1; j < n; j++)
                {
                    double f = mat[j, i] / mat[i, i];
                    for (int k = i; k <= n; k++)
                        mat[j, k] -= mat[i, k] * f;
                }
            }

            double[] x = new double[n];
            for (int i = n - 1; i >= 0; i--)
            {
                x[i] = mat[i, n] / mat[i, i];
                for (int j = 0; j < i; j++)
                    mat[j, n] -= mat[j, i] * x[i];
            }

            return x;
        }
    }
}
