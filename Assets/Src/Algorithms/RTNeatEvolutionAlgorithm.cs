using System;
using System.Collections;
using System.Collections.Generic;
using SharpNeat;
using SharpNeat.Core;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Utility;
using UnityEngine;

namespace Src.Algorithms
{
    public class RTNeatEvolutionAlgorithm<TGenome> : AbstractGenerationalAlgorithm<TGenome>
        where TGenome : class, IGenome<TGenome>
    {
        NeatEvolutionAlgorithmParameters _eaParams;
        readonly NeatEvolutionAlgorithmParameters _eaParamsComplexifying;
        readonly NeatEvolutionAlgorithmParameters _eaParamsSimplifying;

        readonly ISpeciationStrategy<TGenome> _speciationStrategy;
        IList<Specie<TGenome>> _specieList;
        /// <summary>Index of the specie that contains _currentBestGenome.</summary>
        [SerializeField] private int _bestSpecieIdx;
        readonly FastRandom _rng = new FastRandom();
        readonly NeatAlgorithmStats _stats;

        ComplexityRegulationMode _complexityRegulationMode;
        readonly IComplexityRegulationStrategy _complexityRegulationStrategy;

        #region Properties

        /// <summary>
        /// Gets a list of all current genomes. The current population of genomes. These genomes
        /// are also divided into the species available through the SpeciesList property.
        /// </summary>
        public IList<TGenome> GenomeList
        {
            get { return _genomeList; }
        }

        /// <summary>
        /// Gets a list of all current species. The genomes contained within the species are the same genomes
        /// available through the GenomeList property.
        /// </summary>
        public IList<Specie<TGenome>> SpecieList
        {
            get { return _specieList; }
        }

        /// <summary>
        /// Gets the algorithm statistics object.
        /// </summary>
        public NeatAlgorithmStats Statistics
        {
            get { return _stats; }
        }

        /// <summary>
        /// Gets the current complexity regulation mode.
        /// </summary>
        public ComplexityRegulationMode ComplexityRegulationMode
        {
            get { return _complexityRegulationMode; }
        }

        #endregion
        
                #region Public Methods [Initialization]

        /// <summary>
        /// Initializes the evolution algorithm with the provided IGenomeListEvaluator, IGenomeFactory
        /// and an initial population of genomes.
        /// </summary>
        /// <param name="genomeListEvaluator">The genome evaluation scheme for the evolution algorithm.</param>
        /// <param name="genomeFactory">The factory that was used to create the genomeList and which is therefore referenced by the genomes.</param>
        /// <param name="genomeList">An initial genome population.</param>
        public override void Initialize(IGenomeListEvaluator<TGenome> genomeListEvaluator,
                                        IGenomeFactory<TGenome> genomeFactory,
                                        List<TGenome> genomeList)
        {
            base.Initialize(genomeListEvaluator, genomeFactory, genomeList);
            Initialize();
        }

        /// <summary>
        /// Initializes the evolution algorithm with the provided IGenomeListEvaluator
        /// and an IGenomeFactory that can be used to create an initial population of genomes.
        /// </summary>
        /// <param name="genomeListEvaluator">The genome evaluation scheme for the evolution algorithm.</param>
        /// <param name="genomeFactory">The factory that was used to create the genomeList and which is therefore referenced by the genomes.</param>
        /// <param name="populationSize">The number of genomes to create for the initial population.</param>
        public override void Initialize(IGenomeListEvaluator<TGenome> genomeListEvaluator,
                                        IGenomeFactory<TGenome> genomeFactory,
                                        int populationSize)
        {
            base.Initialize(genomeListEvaluator, genomeFactory, populationSize);
            Initialize();
        }

        /// <summary>
        /// Code common to both public Initialize methods.
        /// </summary>
        private void Initialize()
        {
            // Evaluate the genomes.
            _genomeListEvaluator.Evaluate(_genomeList);

            // Speciate the genomes.
            _specieList = _speciationStrategy.InitializeSpeciation(_genomeList, _eaParams.SpecieCount);
            //Debug.Assert(!TestForEmptySpecies(_specieList), "Speciation resulted in one or more empty species.");
            
            // Sort the genomes in each specie fittest first, secondary sort youngest first.
            SortSpecieGenomes();

            // Store ref to best genome.
            UpdateBestGenome();
        }

        #endregion

        
        /// <summary>
        /// Constructs with the provided NeatEvolutionAlgorithmParameters and ISpeciationStrategy.
        /// </summary>
        public RTNeatEvolutionAlgorithm(NeatEvolutionAlgorithmParameters eaParams,
            ISpeciationStrategy<TGenome> speciationStrategy,
            IComplexityRegulationStrategy complexityRegulationStrategy)
        {
            _eaParams = eaParams;
            _eaParamsComplexifying = _eaParams;
            _eaParamsSimplifying = _eaParams.CreateSimplifyingParameters();
            _stats = new NeatAlgorithmStats(_eaParams);
            _speciationStrategy = speciationStrategy;

            _complexityRegulationMode = ComplexityRegulationMode.Complexifying;
            _complexityRegulationStrategy = complexityRegulationStrategy;
        }
        
        
        
        protected override IEnumerator PerformOneGeneration()
        {
            // 1. Calcualte adjusted fitness of all indivuals in the population
            // adjusted fitness = fitness / species size

            
            double smallestAdjustedFitness = double.MaxValue;
            int smallestIndex = -1;
            for(int i = _genomeList.Count - 1; i >= 0; i--)
            {
                _genomeList[i].GenomeAge++;
                    
                _genomeList[i].EvaluationInfo.FitnessAdjusted = _genomeList[i].EvaluationInfo.Fitness / _specieList[_genomeList[i].SpecieIdx].GenomeList.Count;
                if(_genomeList[i].EvaluationInfo.FitnessAdjusted < smallestAdjustedFitness)
                {
                    smallestAdjustedFitness = _genomeList[i].EvaluationInfo.FitnessAdjusted;
                    smallestIndex = i;
                }
            }

            TGenome oldGenome = null;
            TGenome offspring = null;
            int offspringCount;
            
            // 2. Remove agent with worst adjusted fitness if age is greater than minimum age
            if(_genomeList[smallestIndex].GenomeAge > _eaParams.MinGenomeAge)
            {
                oldGenome = _genomeList[smallestIndex];
                _genomeList.RemoveAt(smallestIndex);
                _specieList[oldGenome.SpecieIdx].GenomeList.Remove(oldGenome);
                
                SpecieStats[] specieStatsArr = CalcSpecieStats(out offspringCount);
            
                // 4 .Choose a parent species to create the new offspring based on the their average fitness.
                // probability of choosing a given species is proportional to the average fitness of that species.
                // a single new offspring is created by combining two offspring from this species

                offspring = this.CreateOffspring(specieStatsArr);
                _genomeList.Add(offspring);
            }
            
            yield return _genomeListEvaluator.EvaluateRtNeat(_genomeList, oldGenome, offspring);

            // 5. reassign all agents to the new species.
            // this doesnt need to be done every step
            foreach(Specie<TGenome> specie in _specieList) {
                specie.GenomeList.Clear();
            }

            // Speciate genomeList.
            _speciationStrategy.SpeciateGenomes(_genomeList, _specieList);

            // Sort the genomes in each specie. Fittest first (secondary sort - youngest first).
            SortSpecieGenomes();
             
            // Update stats and store reference to best genome.
            UpdateBestGenome();
            UpdateStats();
            
            // Determine the complexity regulation mode and switch over to the appropriate set of evolution
            // algorithm parameters. Also notify the genome factory to allow it to modify how it creates genomes
            // (e.g. reduce or disable additive mutations).
            _complexityRegulationMode = _complexityRegulationStrategy.DetermineMode(_stats);
            _genomeFactory.SearchMode = (int)_complexityRegulationMode;
            switch(_complexityRegulationMode)
            {
                case ComplexityRegulationMode.Complexifying:
                    _eaParams = _eaParamsComplexifying;
                    break;
                case ComplexityRegulationMode.Simplifying:
                    _eaParams = _eaParamsSimplifying;
                    break;
            }
        }

        private TGenome CreateOffspring(SpecieStats[] specieStatsArr)
        {
            // Select the species that will produce the offspring.
            double[] speciesProbabilities = new double[specieStatsArr.Length];
            
            for(int i = 0; i < specieStatsArr.Length; i++)
            {
                // TODO:: Check if this is correct
                speciesProbabilities[i] = specieStatsArr[i]._meanFitness;
            }
            
            RouletteWheelLayout rouletteWheelLayout = new RouletteWheelLayout(speciesProbabilities);
            int specieIdx = RouletteWheel.SingleThrow(rouletteWheelLayout, _rng);
            
            // Create new roulette wheel layout for the chosen species' genomes.
            List<TGenome> genomeList = _specieList[specieIdx].GenomeList;

            // Add case for when there is only one member in the species.
            // Note: Early Return
            if (genomeList.Count == 1)
            {
                return genomeList[0].CreateOffspring(_currentGeneration);
            }
            
            double[] genomeProbabilities = new double[genomeList.Count];
            for(int i = 0; i < genomeList.Count; i++)
            {
                genomeProbabilities[i] = genomeList[i].EvaluationInfo.Fitness;
            }
            
            RouletteWheelLayout genomeRouletteWheelLayout = new RouletteWheelLayout(genomeProbabilities);
            int genomeIdx1 = RouletteWheel.SingleThrow(genomeRouletteWheelLayout, _rng);
            RouletteWheelLayout rwlTmp = genomeRouletteWheelLayout.RemoveOutcome(genomeIdx1);
            int genomeIdx2 = RouletteWheel.SingleThrow(rwlTmp, _rng);
            
            // Create offspring.
            TGenome offspring = _genomeList[genomeIdx1].CreateOffspring(_genomeList[genomeIdx2], _currentGeneration);

            return offspring;
        }

        // TODO:: CLEAN THIS UP AS IT IS COPIED AND I COULDNT BE ASKED TO REFACTOR IT PROPERLY
        /// <summary>
        /// Calculate statistics for each specie. This method is at the heart of the evolutionary algorithm,
        /// the key things that are achieved in this method are - for each specie we calculate:
        ///  1) The target size based on fitness of the specie's member genomes.
        ///  2) The elite size based on the current size. Potentially this could be higher than the target 
        ///     size, so a target size is taken to be a hard limit.
        ///  3) Following (1) and (2) we can calculate the total number offspring that need to be generated 
        ///     for the current generation.
        /// </summary>
        private SpecieStats[] CalcSpecieStats(out int offspringCount)
        {
            double totalMeanFitness = 0.0;

            // Build stats array and get the mean fitness of each specie.
            int specieCount = _specieList.Count;
            SpecieStats[] specieStatsArr = new SpecieStats[specieCount];
            for(int i=0; i<specieCount; i++)
            {   
                SpecieStats inst = new SpecieStats();
                specieStatsArr[i] = inst;
                if (_specieList[i].GenomeList.Count == 0)
                {
                    inst._meanFitness = 0;
                }
                else
                {
                    inst._meanFitness = _specieList[i].CalcMeanFitness();
                }

                totalMeanFitness += inst._meanFitness;
            }

            // Calculate the new target size of each specie using fitness sharing. 
            // Keep a total of all allocated target sizes, typically this will vary slightly from the
            // overall target population size due to rounding of each real/fractional target size.
            int totalTargetSizeInt = 0;

            if(0.0 == totalMeanFitness)
            {   // Handle specific case where all genomes/species have a zero fitness. 
                // Assign all species an equal targetSize.
                double targetSizeReal = (double)_populationSize / (double)specieCount;

                for(int i=0; i<specieCount; i++) 
                {
                    SpecieStats inst = specieStatsArr[i];
                    inst._targetSizeReal = targetSizeReal;

                    // Stochastic rounding will result in equal allocation if targetSizeReal is a whole
                    // number, otherwise it will help to distribute allocations evenly.
                    inst._targetSizeInt = (int)Utilities.ProbabilisticRound(targetSizeReal, _rng);

                    // Total up discretized target sizes.
                    totalTargetSizeInt += inst._targetSizeInt;
                }
            }
            else
            {
                // The size of each specie is based on its fitness relative to the other species.
                for(int i=0; i<specieCount; i++)
                {
                    SpecieStats inst = specieStatsArr[i];
                    inst._targetSizeReal = (inst._meanFitness / totalMeanFitness) * (double)_populationSize;

                    // Discretize targetSize (stochastic rounding).
                    inst._targetSizeInt = (int)Utilities.ProbabilisticRound(inst._targetSizeReal, _rng);

                    // Total up discretized target sizes.
                    totalTargetSizeInt += inst._targetSizeInt;
                }
            }

            // Discretized target sizes may total up to a value that is not equal to the required overall population
            // size. Here we check this and if there is a difference then we adjust the specie's targetSizeInt values
            // to compensate for the difference.
            //
            // E.g. If we are short of the required populationSize then we add the required additional allocation to
            // selected species based on the difference between each specie's targetSizeReal and targetSizeInt values.
            // What we're effectively doing here is assigning the additional required target allocation to species based
            // on their real target size in relation to their actual (integer) target size.
            // Those species that have an actual allocation below there real allocation (the difference will often 
            // be a fractional amount) will be assigned extra allocation probabilistically, where the probability is
            // based on the differences between real and actual target values.
            //
            // Where the actual target allocation is higher than the required target (due to rounding up), we use the same
            // method but we adjust specie target sizes down rather than up.
            int targetSizeDeltaInt = totalTargetSizeInt - _populationSize;

            if(targetSizeDeltaInt < 0)
            {
                // Check for special case. If we are short by just 1 then increment targetSizeInt for the specie containing
                // the best genome. We always ensure that this specie has a minimum target size of 1 with a final test (below),
                // by incrementing here we avoid the probabilistic allocation below followed by a further correction if
                // the champ specie ended up with a zero target size.
                if(-1 == targetSizeDeltaInt)
                {
                    specieStatsArr[_bestSpecieIdx]._targetSizeInt++;
                }
                else
                {
                    // We are short of the required populationSize. Add the required additional allocations.
                    // Determine each specie's relative probability of receiving additional allocation.
                    double[] probabilities = new double[specieCount];
                    for(int i=0; i<specieCount; i++) 
                    {
                        SpecieStats inst = specieStatsArr[i];
                        probabilities[i] = Math.Max(0.0, inst._targetSizeReal - (double)inst._targetSizeInt);
                    }

                    // Use a built in class for choosing an item based on a list of relative probabilities.
                    RouletteWheelLayout rwl = new RouletteWheelLayout(probabilities);

                    // Probabilistically assign the required number of additional allocations.
                    // ENHANCEMENT: We can improve the allocation fairness by updating the RouletteWheelLayout 
                    // after each allocation (to reflect that allocation).
                    // targetSizeDeltaInt is negative, so flip the sign for code clarity.
                    targetSizeDeltaInt *= -1;
                    for(int i=0; i<targetSizeDeltaInt; i++)
                    {
                        int specieIdx = RouletteWheel.SingleThrow(rwl, _rng);
                        specieStatsArr[specieIdx]._targetSizeInt++;
                    }
                }
            }
            else if(targetSizeDeltaInt > 0)
            {
                // We have overshot the required populationSize. Adjust target sizes down to compensate.
                // Determine each specie's relative probability of target size downward adjustment.
                double[] probabilities = new double[specieCount];
                for(int i=0; i<specieCount; i++)
                {
                    SpecieStats inst = specieStatsArr[i];
                    probabilities[i] = Math.Max(0.0, (double)inst._targetSizeInt - inst._targetSizeReal);
                }

                // Use a built in class for choosing an item based on a list of relative probabilities.
                RouletteWheelLayout rwl = new RouletteWheelLayout(probabilities);

                // Probabilistically decrement specie target sizes.
                // ENHANCEMENT: We can improve the selection fairness by updating the RouletteWheelLayout 
                // after each decrement (to reflect that decrement).
                for(int i=0; i<targetSizeDeltaInt;)
                {
                    int specieIdx = RouletteWheel.SingleThrow(rwl, _rng);

                    // Skip empty species. This can happen because the same species can be selected more than once.
                    if(0 != specieStatsArr[specieIdx]._targetSizeInt) {   
                        specieStatsArr[specieIdx]._targetSizeInt--;
                        i++;
                    }
                }
            }

            // We now have Sum(_targetSizeInt) == _populationSize. 

            // TODO: Better way of ensuring champ species has non-zero target size?
            // However we need to check that the specie with the best genome has a non-zero targetSizeInt in order
            // to ensure that the best genome is preserved. A zero size may have been allocated in some pathological cases.
            if(0 == specieStatsArr[_bestSpecieIdx]._targetSizeInt)
            {
                specieStatsArr[_bestSpecieIdx]._targetSizeInt++;

                // Adjust down the target size of one of the other species to compensate.
                // Pick a specie at random (but not the champ specie). Note that this may result in a specie with a zero 
                // target size, this is OK at this stage. We handle allocations of zero in PerformOneGeneration().
                int idx = RouletteWheel.SingleThrowEven(specieCount-1, _rng);
                idx = idx==_bestSpecieIdx ? idx+1 : idx;

                if(specieStatsArr[idx]._targetSizeInt > 0) {
                    specieStatsArr[idx]._targetSizeInt--;
                }
                else 
                {   // Scan forward from this specie to find a suitable one.
                    bool done = false;
                    idx++;
                    for(; idx<specieCount; idx++)
                    {
                        if(idx != _bestSpecieIdx && specieStatsArr[idx]._targetSizeInt > 0) {
                            specieStatsArr[idx]._targetSizeInt--;
                            done = true;
                            break;
                        }
                    }

                    // Scan forward from start of species list.
                    if(!done)
                    {
                        for(int i=0; i<specieCount; i++)
                        {
                            if(i != _bestSpecieIdx && specieStatsArr[i]._targetSizeInt > 0) {
                                specieStatsArr[i]._targetSizeInt--;
                                done = true;
                                break;
                            }
                        }
                        if(!done) {
                            throw new SharpNeatException("CalcSpecieStats(). Error adjusting target population size down. Is the population size less than or equal to the number of species?");
                        }
                    }
                }
            }

            // Now determine the eliteSize for each specie. This is the number of genomes that will remain in a 
            // specie from the current generation and is a proportion of the specie's current size.
            // Also here we calculate the total number of offspring that will need to be generated.
            offspringCount = 0;
            for(int i=0; i<specieCount; i++)
            {
                // Special case - zero target size.
                if(0 == specieStatsArr[i]._targetSizeInt) {
                    specieStatsArr[i]._eliteSizeInt = 0;
                    continue;
                }

                // Discretize the real size with a probabilistic handling of the fractional part.
                double eliteSizeReal = _specieList[i].GenomeList.Count * _eaParams.ElitismProportion;
                int eliteSizeInt = (int)Utilities.ProbabilisticRound(eliteSizeReal, _rng);

                // Ensure eliteSizeInt is no larger than the current target size (remember it was calculated 
                // against the current size of the specie not its new target size).
                SpecieStats inst = specieStatsArr[i];
                inst._eliteSizeInt = Math.Min(eliteSizeInt, inst._targetSizeInt);

                // Ensure the champ specie preserves the champ genome. We do this even if the targetsize is just 1
                // - which means the champ genome will remain and no offspring will be produced from it, apart from 
                // the (usually small) chance of a cross-species mating.
                if(i == _bestSpecieIdx && inst._eliteSizeInt==0)
                {
                    //Debug.Assert(inst._targetSizeInt !=0, "Zero target size assigned to champ specie.");
                    inst._eliteSizeInt = 1;
                }

                // Now we can determine how many offspring to produce for the specie.
                inst._offspringCount = inst._targetSizeInt - inst._eliteSizeInt;
                offspringCount += inst._offspringCount;

                // While we're here we determine the split between asexual and sexual reproduction. Again using 
                // some probabilistic logic to compensate for any rounding bias.
                double offspringAsexualCountReal = (double)inst._offspringCount * _eaParams.OffspringAsexualProportion;
                inst._offspringAsexualCount = (int)Utilities.ProbabilisticRound(offspringAsexualCountReal, _rng);
                inst._offspringSexualCount = inst._offspringCount - inst._offspringAsexualCount;

                // Also while we're here we calculate the selectionSize. The number of the specie's fittest genomes
                // that are selected from to create offspring. This should always be at least 1.
                double selectionSizeReal = _specieList[i].GenomeList.Count * _eaParams.SelectionProportion;
                inst._selectionSizeInt = Math.Max(1, (int)Utilities.ProbabilisticRound(selectionSizeReal, _rng));
            }

            return specieStatsArr;
        }
        
        /// <summary>
        /// Sorts the genomes within each species fittest first, secondary sorts on age.
        /// </summary>
        private void SortSpecieGenomes()
        {
            int minSize = _specieList[0].GenomeList.Count;
            int maxSize = minSize;
            int specieCount = _specieList.Count;

            for(int i=0; i<specieCount; i++)
            {
                // Shuffle the genomes; this ensures that genomes with equal fitness are randomly distributed amongst themselves, and therefore
                // that the top N genomes chosen for selection and elitism isn't biased to an arbitrary set that happen to be at the front of a genome list.
                // N.B. In github/colgreen/SharpNEAT this is done using SortUtils.SortUnstable() for improved performance.
                Utilities.Shuffle(_specieList[i].GenomeList, _rng);
                _specieList[i].GenomeList.Sort(GenomeFitnessComparer<TGenome>.Singleton);
                minSize = Math.Min(minSize, _specieList[i].GenomeList.Count);
                maxSize = Math.Max(maxSize, _specieList[i].GenomeList.Count);
            }

            // Update stats.
            _stats._minSpecieSize = minSize;
            _stats._maxSpecieSize = maxSize;
        }
        
        /// <summary>
        /// Updates _currentBestGenome and _bestSpecieIdx, these are the fittest genome and index of the specie
        /// containing the fittest genome respectively.
        /// 
        /// This method assumes that all specie genomes are sorted fittest first and can therefore save much work
        /// by not having to scan all genomes.
        /// Note. We may have several genomes with equal best fitness, we just select one of them in that case.
        /// </summary>
        protected void UpdateBestGenome()
        {
            // If all genomes have the same fitness (including zero) then we simply return the first genome.
            TGenome bestGenome = null;
            double bestFitness = -1.0;
            int bestSpecieIdx = -1;

            int count = _specieList.Count;
            for(int i=0; i<count; i++)
            {
                // Get the specie's first genome. Genomes are sorted, therefore this is also the fittest 
                // genome in the specie.
                TGenome genome = _specieList[i].GenomeList[0];
                if(genome.EvaluationInfo.Fitness > bestFitness)
                {
                    bestGenome = genome;
                    bestFitness = genome.EvaluationInfo.Fitness;
                    bestSpecieIdx = i;
                }
            }

            _currentBestGenome = bestGenome;
            _bestSpecieIdx = bestSpecieIdx;
        }
        
        /// <summary>
        /// Updates the NeatAlgorithmStats object.
        /// </summary>
        private void UpdateStats()
        {
            _stats._generation = _currentGeneration;
            _stats._totalEvaluationCount = _genomeListEvaluator.EvaluationCount;

            // Evaluation per second.
            DateTime now = DateTime.Now;
            TimeSpan duration = now - _stats._evalsPerSecLastSampleTime;  
          
            // To smooth out the evals per sec statistic we only update if at least 1 second has elapsed 
            // since it was last updated.
            if(duration.Ticks > 9999)
            {
                long evalsSinceLastUpdate = (long)(_genomeListEvaluator.EvaluationCount - _stats._evalsCountAtLastUpdate);
                _stats._evaluationsPerSec = (int)((evalsSinceLastUpdate*1e7) / duration.Ticks);

                // Reset working variables.
                _stats._evalsCountAtLastUpdate = _genomeListEvaluator.EvaluationCount;
                _stats._evalsPerSecLastSampleTime = now;
            }

            // Fitness and complexity stats.
            double totalFitness = _genomeList[0].EvaluationInfo.Fitness;
            double totalComplexity = _genomeList[0].Complexity;
            double maxComplexity = totalComplexity;

            int count = _genomeList.Count;
            for(int i=1; i<count; i++) {
                totalFitness += _genomeList[i].EvaluationInfo.Fitness;
                totalComplexity += _genomeList[i].Complexity;
                maxComplexity = Math.Max(maxComplexity, _genomeList[i].Complexity);
            }

            _stats._maxFitness = _currentBestGenome.EvaluationInfo.Fitness;
            _stats._meanFitness = totalFitness / count;

            _stats._maxComplexity = maxComplexity;
            _stats._meanComplexity = totalComplexity / count;

            // Specie champs mean fitness.
            double totalSpecieChampFitness = _specieList[0].GenomeList[0].EvaluationInfo.Fitness;
            int specieCount = _specieList.Count;
            for(int i=1; i<specieCount; i++) {
                totalSpecieChampFitness += _specieList[i].GenomeList[0].EvaluationInfo.Fitness;
            }
            _stats._meanSpecieChampFitness = totalSpecieChampFitness / specieCount;

            // Moving averages.
            _stats._prevBestFitnessMA = _stats._bestFitnessMA.Mean;
            _stats._bestFitnessMA.Enqueue(_stats._maxFitness);

            _stats._prevMeanSpecieChampFitnessMA = _stats._meanSpecieChampFitnessMA.Mean;
            _stats._meanSpecieChampFitnessMA.Enqueue(_stats._meanSpecieChampFitness);

            _stats._prevComplexityMA = _stats._complexityMA.Mean;
            _stats._complexityMA.Enqueue(_stats._meanComplexity);
        }

    }
}