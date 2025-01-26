namespace ML;

/// <summary>
/// Provides pseudo-random numbers, so we can run the same neural network, with the same weights and biases.
/// </summary>
internal static class ReproduceablePseudoRandomNumberGenerator
{
    /// <summary>
    /// Seed for the pseudo-random number generator.
    /// </summary>
    private const int c_seedForPseudoRandomNumberGenerator = 3114733; // 14733 , 

    /// <summary>
    /// Random number generator, that is predictable.
    /// </summary>
    private static readonly Random s_random = new(c_seedForPseudoRandomNumberGenerator);

    /// <summary>
    /// Returns a non-negative random integer.
    /// </summary>
    /// <returns></returns>
    internal static double GetNextRandomDouble()
    {
        return s_random.NextDouble();
    }

    /// <summary>
    /// Returns a non-negative random integer.
    /// </summary>
    /// <returns></returns>
    internal static int GetNextRandomInt()
    {
        return s_random.Next();
    }

    /// <summary>
    /// Generate a pseudo random number between -0.5...+0.5.
    /// </summary>
    /// <returns></returns>
    internal static double GetRandomdoubleBetweenMinusHalfToPlusHalf()
    {
        return GetNextRandomDouble() - 0.5f;
    }

    /// <summary>
    /// Generate a pseudo random number between -1...+1.
    /// </summary>
    /// <returns></returns>
    internal static double GetRandomdoubleBetweenPlusOrMinus1()
    {
        return GetNextRandomDouble() * 2 - 1f;
    }

    /// <summary>
    /// Generate a random number for shuffling etc.
    /// </summary>
    /// <returns></returns>
    internal static int Next()
    {
        return s_random.Next();
    }
}