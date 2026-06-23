namespace Ficep.QualityControl.Core.Registration;

/// <summary>
/// Cholesky solver for the small symmetric positive-definite normal equations the point-to-plane ICP
/// builds each iteration (a 6×6 system: three rotation, three translation unknowns). Returns false if
/// the matrix is not positive-definite, letting the caller stop instead of producing a bogus step.
/// </summary>
internal static class Cholesky
{
    public static bool Solve(double[,] a, double[] b, double[] x, int n)
    {
        var l = new double[n, n];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j <= i; j++)
            {
                double sum = a[i, j];
                for (int k = 0; k < j; k++)
                    sum -= l[i, k] * l[j, k];

                if (i == j)
                {
                    if (sum <= 0)
                        return false;
                    l[i, i] = Math.Sqrt(sum);
                }
                else
                {
                    l[i, j] = sum / l[j, j];
                }
            }
        }

        // Forward substitution: L y = b.
        var y = new double[n];
        for (int i = 0; i < n; i++)
        {
            double sum = b[i];
            for (int k = 0; k < i; k++)
                sum -= l[i, k] * y[k];
            y[i] = sum / l[i, i];
        }

        // Back substitution: Lᵀ x = y.
        for (int i = n - 1; i >= 0; i--)
        {
            double sum = y[i];
            for (int k = i + 1; k < n; k++)
                sum -= l[k, i] * x[k];
            x[i] = sum / l[i, i];
        }

        return true;
    }
}
