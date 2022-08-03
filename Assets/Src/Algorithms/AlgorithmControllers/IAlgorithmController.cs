using System;
using System.Collections.Generic;
using SharpNeat.Phenomes;
using UnitySharpNEAT;

namespace Src.Algorithms
{
    /// <summary>
    /// Handles the lifecycle and management of a evolutionary algorithm.
    /// </summary>
    public interface IAlgorithmController
    {
        Experiment Experiment { get; set; }
        
        UnitPool UnitPool { get; }
        
        public int Trials { get; set; }
        public float TrialDuration { get; set; }
        public float StoppingFitness { get; set; }
        uint CurrentGeneration { get; set; }
        double CurrentBestFitness { get; set; }
        int SpeciesCount { get; set; }

        #region UNITY FUNCTIONS
        
        void Start();
        
        #endregion

        public abstract void StartEvolution();

        void StopEvolution();

        void RunBest();

        void HandleUpdateEvent(object sender, EventArgs e);

        void HandlePauseEvent(object sender, EventArgs e);

        void ActivateUnit(IBlackBox phenome, int genomeSpecieIdx);
        
        float GetFitness(IBlackBox box);
        
        void DeactivateUnit(IBlackBox unit);
        void SaveExperiment();
        public void SaveExperiment(Experiment experiment);
        public void LoadExperiment(Experiment experiment);
        void LoadExperiment();
    }
}