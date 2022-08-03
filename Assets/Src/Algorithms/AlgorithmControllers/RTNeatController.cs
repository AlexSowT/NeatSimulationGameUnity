using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using SharpNeat.Core;
using SharpNeat.DistanceMetrics;
using SharpNeat.Domains;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using SharpNeat.SpeciationStrategies;
using UnityEngine;
using UnitySharpNEAT;
using Random = UnityEngine.Random;

namespace Src.Algorithms.AlgorithmControllers
{
    public class RTNeatController : MonoBehaviour, IAlgorithmController
    {
        #region FIELDS
        [Header("Experiment Settings")]

        [SerializeField]
        private string _experimentConfigFileName = "experiment.config";

        [SerializeField]
        private int _networkInputCount = 5;

        [SerializeField]
        private int _networkOutputCount = 2;

        [Header("Evaluation Settings")]

        [Tooltip("How many times per generation the generation gets evaluated.")]
        public int _trails = 1;

        [Tooltip("How many seconds pass between each evaluation (the duration gets scaled by the global timescale).")]
        public float _trailDuration = 20;

        [Tooltip("Stop the simulation as soon as a Unit reaches this fitness level.")]
        public float _stoppingFitness = 15;


        [Header("Unit Management")]

        [SerializeField, Tooltip("The Unit Prefab, which inherits from UnitController, that should be evaluated and spawned.")]
        private UnitController _unitControllerPrefab = default;

        [SerializeField, Tooltip("The parent transform which will hold the instantiated Units.")]
        private Transform _spawnParent = default;

        private Color[] _speciesColors = default;
        
        [SerializeField]
        private UnitPool _unitPool = default;
        
        [Header("Debug")]

        [SerializeField]
        private bool _enableDebugLogging = false;
        
        // Object pooling and Unit management
        private Dictionary<IBlackBox, UnitController> _blackBoxMap = new Dictionary<IBlackBox, UnitController>();

        private HashSet<UnitController> _unusedUnitsPool = new HashSet<UnitController>();

        private HashSet<UnitController> _usedUnitsPool = new HashSet<UnitController>();

        private DateTime _startTime;
        #endregion

        #region PROPERTIES
        public int NetworkInputCount { get => _networkInputCount; }

        public int NetworkOutputCount { get => _networkOutputCount; }

        public uint CurrentGeneration { get;  set; }

        public double CurrentBestFitness { get;  set; }
        //public int SpeciesCount { get; set; }

        public RTNeatEvolutionAlgorithm<NeatGenome> EvolutionAlgorithm { get; private set; }

        public int SpeciesCount
        {
            get { 
                if (EvolutionAlgorithm != null) 
                    return EvolutionAlgorithm.SpecieList.Count;
                return -1;
            }
            set { }
        }

        public Experiment Experiment { get; set; }
        
        public UnitPool UnitPool
        {
            get { return _unitPool; }
        }

        public int Trials
        {
            get { return this._trails; }
            set { this._trails = value; }
        }

        public float TrialDuration
        {
            get { return this._trailDuration; } 
            set { this._trailDuration = value; }
        }

        public float StoppingFitness
        {
            get { return this._stoppingFitness; } 
            set { this._stoppingFitness = value; }
        }

        private FileManager FileManager { get; set; }
        
        #endregion
        
        public void Start()
        {
            Utility.DebugLog = _enableDebugLogging;

            // load experiment config file and use it to create an Experiment
            XmlDocument xmlConfig = new XmlDocument();
            TextAsset textAsset = (TextAsset)Resources.Load(_experimentConfigFileName);

            if (textAsset == null)
            {
                Debug.LogError("The experiment config file named '" + _experimentConfigFileName + ".xml' could not be found in any Resources folder!");
                return;
            }

            xmlConfig.LoadXml(textAsset.text);

            Experiment = new Experiment();
            Experiment.Initialize(xmlConfig.DocumentElement, this, _networkInputCount, _networkOutputCount);

            ExperimentIO.DebugPrintSavePaths(Experiment);

            this.FileManager = new FileManager();
        }

        public void StartEvolution()
        {
            if (EvolutionAlgorithm != null && EvolutionAlgorithm.RunState == SharpNeat.Core.RunState.Running)
                return;

            DeactivateAllUnits();

            Utility.Log("Starting Experiment.");
            _startTime = DateTime.Now;

            this.InitEvolutionAlgorithm();
        }

        private void DeactivateAllUnits()
        {
            if (EvolutionAlgorithm != null && EvolutionAlgorithm.RunState == SharpNeat.Core.RunState.Running)
            {
                EvolutionAlgorithm.Stop();
            }
        }

        private void InitEvolutionAlgorithm()
        {
            List<NeatGenome> genomeList = Experiment.LoadPopulation();
            IGenomeFactory<NeatGenome> genomeFactory = Experiment.CreateGenomeFactory();
            
            IDistanceMetric distanceMetric = new ManhattanDistanceMetric(1.0, 0.0, 10.0);
            ISpeciationStrategy<NeatGenome> speciationStrategy = new KMeansClusteringStrategy<NeatGenome>(distanceMetric);

            IComplexityRegulationStrategy complexityRegulationStrategy = ExperimentUtils.CreateComplexityRegulationStrategy(Experiment._complexityRegulationStr, Experiment._complexityThreshold);

            RTNeatEvolutionAlgorithm<NeatGenome> ea = new RTNeatEvolutionAlgorithm<NeatGenome>(Experiment._eaParams, speciationStrategy, complexityRegulationStrategy);

            // Create black box evaluator       
            BlackBoxFitnessEvaluator evaluator = new BlackBoxFitnessEvaluator(this);

            IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder = Experiment.CreateGenomeDecoder();

            IGenomeListEvaluator<NeatGenome> innerEvaluator = new CoroutinedListEvaluator<NeatGenome, IBlackBox>(genomeDecoder, evaluator, this);

            IGenomeListEvaluator<NeatGenome> selectiveEvaluator = new SelectiveGenomeListEvaluator<NeatGenome>(innerEvaluator,
                SelectiveGenomeListEvaluator<NeatGenome>.CreatePredicate_OnceOnly());

            //ea.Initialize(selectiveEvaluator, genomeFactory, genomeList);
            ea.Initialize(innerEvaluator, genomeFactory, genomeList);

            EvolutionAlgorithm = ea;
            
            this.UnitPool.Init(SpeciesCount);

            EvolutionAlgorithm.UpdateEvent += new EventHandler(HandleUpdateEvent);
            EvolutionAlgorithm.PausedEvent += new EventHandler(HandlePauseEvent);
            
            EvolutionAlgorithm.StartContinue();
        }

        public void StopEvolution()
        {
            DeactivateAllUnits();

            if (EvolutionAlgorithm != null && EvolutionAlgorithm.RunState == SharpNeat.Core.RunState.Running)
            {
                EvolutionAlgorithm.Stop();
            }
        }

        public void RunBest()
        {
            StopEvolution();

            NeatGenome genome = Experiment.LoadChampion();
            if (genome == null)
                return;

            // Get a genome decoder that can convert genomes to phenomes.
            IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder = Experiment.CreateGenomeDecoder();
            // Decode the genome into a phenome (neural network, i.e. IBlackBox).
            IBlackBox phenome = genomeDecoder.Decode(genome);

            ActivateUnit(phenome, genome.SpecieIdx);
        }

        /// <summary>
        /// Event callback which gets called at the end of each generation.
        /// </summary>
        public void HandleUpdateEvent(object sender, EventArgs e)
        {
            Utility.Log(string.Format("Generation={0:N0} BestFitness={1:N6}", EvolutionAlgorithm.CurrentGeneration, EvolutionAlgorithm.Statistics._maxFitness));

            CurrentBestFitness = EvolutionAlgorithm.Statistics._maxFitness;
            CurrentGeneration = EvolutionAlgorithm.CurrentGeneration;
        }

        /// <summary>
        /// Event callback which gets called after the evolution got paused.
        /// </summary>
        public void HandlePauseEvent(object sender, EventArgs e)
        {
            Utility.Log("STOP - Save the Population and the current Champion");

            // Save genomes to xml file.    
            Experiment.SavePopulation(EvolutionAlgorithm.GenomeList);
            Experiment.SaveChampion(EvolutionAlgorithm.CurrentChampGenome);

            DateTime endTime = DateTime.Now;
            Utility.Log("Total time elapsed: " + (endTime - _startTime));
        }

        public void ActivateUnit(IBlackBox phenome, int genomeSpecieIdx)
        {
            this.UnitPool.ActivateUnit(phenome, genomeSpecieIdx);
        }

        public void DeactivateUnit(IBlackBox unit)
        {
            this.UnitPool.DeactivateUnit(unit);
        }

        #region EXPERIMENT MANAGEMENT

        public void LoadExperiment()
        {
            XmlDocument xmlConfig = this.FileManager.OpenFileBrowserForLoad();

            Experiment experiment = new Experiment();
            experiment.Initialize(xmlConfig.DocumentElement, this, _networkInputCount, _networkOutputCount);

            this.LoadExperiment(experiment);
        }
        
        public void LoadExperiment(Experiment experiment)
        {
            this.Experiment = experiment;

            _usedUnitsPool.Clear();

            // For each specieCount create a random number and add it to the SpecieColor list
            _speciesColors = new Color[this.Experiment.SpecieCount];
            for (int i = 0; i < Experiment.SpecieCount; i++)
            {
                _speciesColors[i] = (new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 0.2f), Random.Range(0.0f, 1.0f)));
            }
            
            this.InitEvolutionAlgorithm();
            
            ExperimentIO.DebugPrintSavePaths(Experiment);
        }
        
        public void SaveExperiment()
        {
            this.InitEvolutionAlgorithm();
            
            // Save the population and best network to unity data location
            Experiment.SavePopulation(EvolutionAlgorithm.GenomeList);
            Experiment.SaveChampion(EvolutionAlgorithm.CurrentChampGenome);

            // Serialize the experiment to a xml file.
            string path = FileManager.OpenFileBrowserForSave();
            
            using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate))
            {
                using (XmlWriter xmlWriter = new XmlTextWriter(fs, Encoding.Unicode))
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(Experiment.GetType());
                    xmlSerializer.Serialize(xmlWriter, Experiment);
                }
            }
        }
        
        public void SaveExperiment(Experiment experiment)
        {
            this.Experiment = experiment;
            this.SaveExperiment();
        }

        #endregion

        public float GetFitness(IBlackBox box)
        {
            return this.UnitPool.GetFitness(box);
        }
    }
}