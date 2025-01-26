using Newtonsoft.Json;
using System;
using static ML.NeuralNetwork;

namespace ML;

/// <summary>
/// Cache for biases and weights, so we can re-run the same neural network, with the same weights and biases.
/// </summary>
internal static class BiasWeightingCache
{
    /// <summary>
    /// NN Biases. Either improves or lowers the chance of this neuron fully firing.
    /// [layer][neuron]
    /// </summary>
    private static readonly Dictionary<int /*layer-hash*/, Dictionary<InitType /*random/xavier/gaussian*/, double[][]>> _sameBiases = [];

    /// <summary>
    /// NN weights. Reduces or amplifies the output for the relationship between neurons in each layer
    /// [layer][neuron][neuron]
    /// </summary>
    private static readonly Dictionary<int /*layer-hash*/, Dictionary<InitType /*random/xavier/gaussian*/, double[][][]>> _sameWeights = [];

    /// <summary>
    /// Ensure we can create ALL neural networks with the same weights and biases (allowing for
    /// when they use Xavier or Gaussian initialisation).
    /// </summary>
    internal static void SameWeightsBiases(int[] Layers, InitType weightInitType)
    {
        int hashOfLayers = GetArrayHashCode(Layers);
#if false
        if (_sameBiases.ContainsKey(hashOfLayers) && _sameBiases[hashOfLayers].ContainsKey(weightInitType))
        {
            return; // we've already initialized this set of biases and weights.
        }
#endif
        CacheBiases(Layers, weightInitType, hashOfLayers);
        CacheWeights(Layers, weightInitType, hashOfLayers);
    }

    /// <summary>
    /// Cache the weights for a given neural network.
    /// </summary>
    /// <param name="Layers"></param>
    /// <param name="weightInitType"></param>
    /// <param name="hashOfLayers"></param>
    private static void CacheWeights(int[] Layers, InitType weightInitType, int hashOfLayers)
    {
        // standard weights for all neurons.

        List<double[][]> weightsList = []; // used to construct weights, as dynamic arrays aren't supported

        for (int layer = 1; layer < Layers.Length; layer++)
        {
            List<double[]> layerWeightsList = [];

            int neuronsInPreviousLayer = Layers[layer - 1];

            for (int neuronIndexInLayer = 0; neuronIndexInLayer < Layers[layer]; neuronIndexInLayer++)
            {
                double[] neuronWeights = new double[neuronsInPreviousLayer];

                double variance = 1.0f / neuronsInPreviousLayer;
                double weightScale;

                if (weightInitType == InitType.Xavier)
                {
                    int neuronsInCurrentLayer = Layers[layer];

                    // Xavier initialization
                    weightScale = Math.Sqrt(6.0 / (neuronsInPreviousLayer + neuronsInCurrentLayer));
                }
                else
                {
                    weightScale = 1.0f;
                }

                for (int neuronIndexInPreviousLayer = 0; neuronIndexInPreviousLayer < neuronsInPreviousLayer; neuronIndexInPreviousLayer++)
                {
                    if (weightInitType == InitType.Gaussian)
                    {
                        neuronWeights[neuronIndexInPreviousLayer] = NextGaussian(0, Math.Sqrt(variance)) * weightScale;
                    }
                    else
                    {
                        neuronWeights[neuronIndexInPreviousLayer] = ReproduceablePseudoRandomNumberGenerator.GetRandomdoubleBetweenMinusHalfToPlusHalf() * weightScale;
                    }
                }

                layerWeightsList.Add(neuronWeights);
            }

            weightsList.Add([.. layerWeightsList]);

            if (!_sameWeights.ContainsKey(hashOfLayers)) _sameWeights.Add(hashOfLayers, []);

            _sameWeights[hashOfLayers][weightInitType] = [.. weightsList];
        }
    }

    /// <summary>
    /// Cache the biases for a given neural network.
    /// </summary>
    /// <param name="Layers"></param>
    /// <param name="weightInitType"></param>
    /// <param name="hashOfLayers"></param>
    private static void CacheBiases(int[] Layers, InitType weightInitType, int hashOfLayers)
    {
        // standard biases for all neurons.
        List<double[]> sameBiasesList = [];

        for (int layer = 1; layer < Layers.Length; layer++)
        {
            double[] bias = new double[Layers[layer]];

            for (int biasLayer = 0; biasLayer < Layers[layer]; biasLayer++)
            {
                bias[biasLayer] = ReproduceablePseudoRandomNumberGenerator.GetRandomdoubleBetweenMinusHalfToPlusHalf();
            }

            sameBiasesList.Add(bias);
        }

        if (!_sameBiases.ContainsKey(hashOfLayers)) _sameBiases.Add(hashOfLayers, []);
        _sameBiases[hashOfLayers][weightInitType] = [.. sameBiasesList];
    }

    /// <summary>
    /// Get the hash code for an array.
    /// </summary>
    /// <param name="array"></param>
    /// <returns></returns>
    public static int GetArrayHashCode(int[] array)
    {
        // a normal .GetHashCode() is not suitable for arrays. It will return a hash of the reference to the array, not the contents.
        if (array == null)
            return 0;

        int hash = 17;

        foreach (int element in array)
        {
            hash = hash * 31 + element.GetHashCode();
        }

        return hash;
    }

    /// <summary>
    /// Returns a Gaussian uniform random numbers, code from GitHub Copilot.
    /// </summary>
    /// <param name="mean"></param>
    /// <param name="stdDev"></param>
    /// <returns></returns>
    private static double NextGaussian(double mean, double stdDev)
    {
        double u1 = 1.0f - Math.Abs(ReproduceablePseudoRandomNumberGenerator.GetRandomdoubleBetweenPlusOrMinus1());
        double u2 = 1.0f - Math.Abs(ReproduceablePseudoRandomNumberGenerator.GetRandomdoubleBetweenPlusOrMinus1());
        double z = (Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));

        return mean + stdDev * z;
    }

    /// <summary>
    /// Retrieves precomputed biases for a given initialization type and layers.
    /// </summary>
    internal static double[][] GetBiases(InitType initType, int[] layers)
    {
        int hashOfLayers = GetArrayHashCode(layers);

        // Ensure _sameBiases is initialized
        if (_sameBiases == null || !_sameBiases.TryGetValue(hashOfLayers, out Dictionary<InitType, double[][]>? dictionaryOfBiasesPerInitType) || !dictionaryOfBiasesPerInitType.ContainsKey(initType))
        {
            throw new InvalidOperationException("_sameBiases is not initialized.");
        }

        // create a deep clone of _sameBiases into variable Biases via Json
        double[][]? biases = JsonConvert.DeserializeObject<double[][]>(JsonConvert.SerializeObject(dictionaryOfBiasesPerInitType[initType]));

        return biases ?? throw new InvalidOperationException("Biases is null.");
    }

    /// <summary>
    /// Retrieves precomputed weights for a given initialization type and layers.
    /// </summary>
    /// <param name="initType"></param>
    /// <param name="layers"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    internal static double[][][] GetWeights(InitType initType, int[] layers)
    {
        int hashOfLayers = GetArrayHashCode(layers);

        // Ensure _sameWeights is initialized
        if (_sameWeights == null || !_sameWeights.ContainsKey(hashOfLayers) || !_sameWeights[hashOfLayers].ContainsKey(initType))
        {
            throw new InvalidOperationException("_sameWeights is not initialized.");
        }

        // create a deep clone of _sameWeights into variable Weights via Json
        double[][][]? weights = JsonConvert.DeserializeObject<double[][][]>(JsonConvert.SerializeObject(_sameWeights[hashOfLayers][initType])) ?? throw new InvalidOperationException("Weights is null.");

        return weights;
    }
}