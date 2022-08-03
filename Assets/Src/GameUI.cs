using System;
using System.Collections;
using System.Collections.Generic;
using SharpNeat.Decoders;
using SharpNeat.Domains;
using Src.Algorithms;
using Src.Algorithms.AlgorithmControllers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnitySharpNEAT;
using Button = UnityEngine.UIElements.Button;

public class GameUI : MonoBehaviour
{
    private const float RIGHTBUFFER = 10;
    private const float TOPBUFFER = 10;
    private const float BOTTOMBUFFER = 15;
    private const float SCREENPERCENT = 0.3f;
    
    [SerializeField] 
    private NeatSupervisor _neatSupervisor;
    
    [SerializeField] 
    private RTNeatController _rtNeatController;

    [SerializeField] private bool useRtNeat = false;
    
    [SerializeField]
    private GameObject _newExperimentPopup;
    
    [Header("Popup Input Fields")]
    
    [SerializeField]
    private TMP_InputField nameInput;

    [SerializeField]
    private TMP_InputField descriptionInput;

    [SerializeField]
    private TMP_InputField popSizeInput;

    [SerializeField]
    private TMP_InputField specieCountInput;

    [SerializeField]
    private TMP_Dropdown activationOptions;
    
    [SerializeField]
    private TMP_InputField complexityStrategyInput;
    
    [SerializeField]
    private TMP_InputField complexityThresholdInput;

    private IAlgorithmController AlgorithmController { get; set; }

    private void Start()
    {
        try
        {
            if (useRtNeat)
            {
                AlgorithmController = _rtNeatController;
            }
            else
            {
                AlgorithmController = _neatSupervisor;
            }
        }
        catch (Exception e)
        {
            Debug.Log($"Cannot cast to IAlgorithmController, {e.Message}");;
        }
    }

    private void OnGUI()
    {
        float xPos = (Screen.width - (Screen.width * SCREENPERCENT)) - RIGHTBUFFER;
        // Model Options
        GUI.Box(new Rect(xPos,  ((Screen.height/3) * 0) + TOPBUFFER, Screen.width * SCREENPERCENT, Screen.height/3 - BOTTOMBUFFER), "Model Options");
        
        if (GUI.Button(new Rect(xPos + 20f, 35, 110, 40), "New Experiment"))
        {
            this._newExperimentPopup.SetActive(true);
        }
        if (GUI.Button(new Rect(xPos + 20f, 80, 110, 40), "Save Experiment"))
        {
            AlgorithmController.SaveExperiment();
        }
        if (GUI.Button(new Rect(xPos + 20f, 125, 110, 40), "Load Experiment"))
        {
            AlgorithmController.LoadExperiment();
        }
        if (GUI.Button(new Rect(xPos + 20f, 170, 110, 40), "Start Training"))
        {
            AlgorithmController.StartEvolution();
        }
        if (GUI.Button(new Rect(xPos + 160f, 35, 110, 40), "Pause Training"))
        {
            AlgorithmController.StopEvolution();
        }
        if (GUI.Button(new Rect(xPos + 160f, 80, 110, 40), "Run Best"))
        {
            AlgorithmController.RunBest();
        }
        if (GUI.Button(new Rect(xPos + 160f, 125, 110, 40), "Delete Experiment"))
        {
            ExperimentIO.DeleteAllSaveFiles(AlgorithmController.Experiment);
        }
        
        // Agent Options
        GUI.Box(new Rect(xPos, ((Screen.height/3) * 1) + TOPBUFFER, Screen.width * SCREENPERCENT, Screen.height/3 - BOTTOMBUFFER), "Agent Options");
        
        // Fitness Goals
        GUI.Box(new Rect(xPos, ((Screen.height/3) * 2) + TOPBUFFER, Screen.width * SCREENPERCENT, Screen.height/3 - BOTTOMBUFFER), "Fitness Goals");

        GUI.Button(new Rect(10, Screen.height - 70, 110, 60), string.Format("Generation: {0}\nFitness: {1:0.00}\nSpecies Count:{2}", AlgorithmController.CurrentGeneration, AlgorithmController.CurrentBestFitness, AlgorithmController.SpeciesCount));
    }
    
    public void OnSaveClick()
    {
        Experiment experiment = new Experiment();
        NetworkActivationScheme networkActivationScheme =
            ExperimentUtils.CreateActivationScheme(activationOptions.options[activationOptions.value].text, String.Empty);
        // TODO:: Fix input output count being hard coded at the end
        experiment.Initialize(nameInput.text, Int32.Parse(popSizeInput.text), Int32.Parse(specieCountInput.text), networkActivationScheme, complexityStrategyInput.text, Int32.Parse(complexityThresholdInput.text), descriptionInput.text, AlgorithmController, 2, 2);
        AlgorithmController.SaveExperiment(experiment);
        AlgorithmController.LoadExperiment(experiment);
    }

    public void OnCancelClick()
    {
        this._newExperimentPopup.SetActive(false);
    }
    
}
