using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GeneticSharp;
using Godot;
using UnityEngine.ExternalScripts.Particle.Genetics;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class Genetics
{
    private readonly Vector2I _size;
    private readonly GeneticAlgorithm _ga;
    private int _genFinished = 0;

    private ParticleSimulatorFitness comparison;
    private ParticleSimulatorFitness viewerEvaluator;

    private IProblem _problem;

    private object _lock = new();
    private readonly GAParams _gaParams;
    private readonly GeneticLooper _viewer;
    public event Action<Threshold> OnThresholdReached;
    private Threshold[] _thresholds;

    private Stopwatch _sw;

    public record struct Threshold(float value, int index, bool firstReach = true);

    private BitWiseEvaluator proportional;
    private ExactMatchEvaluator exact;
    private AveragedEvaluators average;

    public Genetics(int nbParticles, Vector2I size, GAParams param, List<GeneticLooper> loopers,
        GeneticLooper viewer,
        IEnumerable<AGate> gatesTemplate, IEnumerable<float> thresholds)
    {
        _sw = new Stopwatch();
        _sw.Start();
        _gaParams = param;
        _viewer = viewer;
        GatesTypesToInt.OverrideReflection(new EmptyGate(), gatesTemplate);
        _size = size;
        _ga = CreateGA(nbParticles, loopers, out proportional, out exact, out average);
        _ga.CrossoverProbability = _gaParams.CrossoverProb;
        _ga.MutationProbability = _gaParams.MutationProb;
        //_ga.Reinsertion = new PureReinsertion();
        _ga.Termination = CreateTermination();
        _ga.TaskExecutor = new ParallelTaskExecutor();
        UnityEngine.Debug.Log("GA created : " + _sw.Elapsed);
        _thresholds = thresholds.OrderBy(t => t).Select((t, i) => new Threshold(t, i, true)).ToArray();
        _ga.GenerationRan += (s, a) =>
        {
            for (var index = 0; index < _thresholds.Length; index++)
            {
                var t = _thresholds[index];
                if (_ga.BestChromosome.Fitness >= t.value)
                {
                    OnThresholdReached?.Invoke(t);
                    t.firstReach = false;
                    _thresholds[index] = t;
                }
                else
                    break; //Assuming they are sorted
            }
        };
        _ga.GenerationRan += async (s, a) => await GenerationFinished();
        _ga.GenerationRan += (s, a) => UpdateWeights((float)_ga.Population.BestChromosome.Fitness.Value);
        _ga.TerminationReached += (sender, args) =>
            UnityEngine.Debug.Log($"GA Termination Reached at generation {_genFinished} with best fitness: " +
                                  _ga.BestChromosome.Fitness);

        //On start, even before our first evaluation is completed (i.e we don't yet have a best cromosome, we show a random) one in the viewer so we still have something on screen, all later generations will be showing the best one
        RunViewer(new Chromosome(_size.X * _size.Y));
    }

    public Task StartAsync()
    {
        return Task.Run(() => _ga.Start());
    }

    private void UpdateWeights(float maxFit)
    {
        float proportionalWeight = _gaParams.ProportionalWeight(maxFit);
        float exactWeight = _gaParams.ExactWeight(maxFit);
        int nbEvaluationsPerIndividual = _gaParams.NbEvaluationsPerIndividual(maxFit);
        UnityEngine.Debug.Log(
            $" Updating weights with max fit {maxFit} => prop {proportionalWeight}, exact {exactWeight} and nb eval {nbEvaluationsPerIndividual}");
        comparison.UpdateWeight(proportional, proportionalWeight);
        comparison.UpdateWeight(exact, exactWeight);
        average.NumberEvaluations = nbEvaluationsPerIndividual;
    }

    private async Task GenerationFinished()
    {
        var best = _ga.BestChromosome;
        var averageFitness = _ga.Population.CurrentGeneration.Chromosomes.Average(c => c.Fitness.Value);
        UnityEngine.Debug.Log($"--------------Gen finished {_genFinished}, best fitness: " + best.Fitness +
                              " showing it on the view, avg fitness : " + averageFitness + " among " +
                              _ga.Population.CurrentGeneration.Chromosomes.Count + " in " + _sw.Elapsed);
        _sw.Restart();
        _genFinished++;
        RunViewer(best);
    }

    private void RunViewer(IChromosome target)
    {
        Task.Run(() =>
        {
            var inputs = comparison.Inputs(target).ToArray();
            if (_gaParams.RandomizeViewerInputs)
                inputs = Enumerable.Range(0, inputs.Length).Select(i => _problem.CreateNewInput()).ToArray();
            UnityEngine.Debug.Log(
                $"Running viewer on best with {(_gaParams.RandomizeViewerInputs ? "random " : "last")} inputs " +
                string.Join(",", inputs));
            //For some reasons running the viewer in another nested task makes it not block the viewing thread
            Task.Run(async () =>
            {
                var result = await viewerEvaluator.Evaluate(target, inputs, false);
                Debug.Log(
                    "Best of last gen, reran in viewer got sum of fitness " + result.Sum() + " / " + result.Length);
            });
        });
        //Task.Run(async () =>
        //{
        //    while (_viewer.Busy) //This call will wait until the viewer has been freed
        //        await Task.Delay(100);
//
        //    for (var i = 0; i < inputs.Length; i++)
        //    {
        //        var input = inputs[i];
        //        if (input == 0)
        //            continue;
        //        _viewer.Start(target, comparison.Input(target));
        //        while (!_viewer.ResultAvailable)
        //            await Task.Delay(100);
        //        _ = _viewer.GetResult(i == inputs.Length - 1); //Will free for the next call to restart it
        //    }
        //});
    }

    private ITermination CreateTermination()
    {
        ITermination term = new FitnessThresholdTermination(1f /*w[1] / w.Sum()*/);
        if (_gaParams.MaxGen > 0)
            term = new OrTermination(term, new GenerationNumberTermination(_gaParams.MaxGen));
        term = new OrTermination(term, new FitnessStagnationTermination(1000));
        return term;
    }

    private GeneticAlgorithm CreateGA(int nbParticles, List<GeneticLooper> loopers, out BitWiseEvaluator proportional,
        out ExactMatchEvaluator exact,
        out AveragedEvaluators average)
    {
        var selection = new TournamentSelection(3);
        var crossover = new BlockCrossover(_size);
        IMutation mutation = new Mutation(_gaParams.MutateToEmpty, _gaParams.MutateBlock, _size);
        float[] w = [0.1f, 9f];
        var max = (int)Mathf.Pow(2, nbParticles) - 1;
        _problem = new Operation(max);
        proportional = new BitWiseEvaluator(nbParticles);
        exact = new ExactMatchEvaluator();
        comparison = new ParticleSimulatorFitness(nbParticles, max, loopers, _problem,
            (proportional, _gaParams.ProportionalWeight(0)), (exact, _gaParams.ExactWeight(0)));
        viewerEvaluator = new ParticleSimulatorFitness(nbParticles, max, [_viewer], _problem, (exact, 1f));
        average = new AveragedEvaluators(comparison, _gaParams.NbEvaluationsPerIndividual(0));
        IFitness fitness = new CombinedFitness((new MostNullGates(), w[0]), (average, w[1]));
        var chromosome = new Chromosome(_size.X * _size.Y);
        var population = new Population(_gaParams.PopSize, _gaParams.PopSize * 4, chromosome);
        return new GeneticAlgorithm(population, fitness, selection, crossover, mutation);
    }
}