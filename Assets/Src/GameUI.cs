using System;
using System.Collections;
using System.Collections.Generic;
using SharpNeat.Decoders;
using SharpNeat.Domains;
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
            _neatSupervisor.SaveExperiment();
        }
        if (GUI.Button(new Rect(xPos + 20f, 125, 110, 40), "Load Experiment"))
        {
            _neatSupervisor.LoadExperiment();
        }
        if (GUI.Button(new Rect(xPos + 20f, 170, 110, 40), "Start Training"))
        {
            _neatSupervisor.StartEvolution();
        }
        if (GUI.Button(new Rect(xPos + 160f, 35, 110, 40), "Pause Training"))
        {
            _neatSupervisor.StopEvolution();
        }
        if (GUI.Button(new Rect(xPos + 160f, 80, 110, 40), "Run Best"))
        {
            _neatSupervisor.RunBest();
        }
        if (GUI.Button(new Rect(xPos + 160f, 125, 110, 40), "Delete Experiment"))
        {
            ExperimentIO.DeleteAllSaveFiles(_neatSupervisor.Experiment);
        }
        
        // Agent Options
        GUI.Box(new Rect(xPos, ((Screen.height/3) * 1) + TOPBUFFER, Screen.width * SCREENPERCENT, Screen.height/3 - BOTTOMBUFFER), "Agent Options");
        
        // Fitness Goals
        GUI.Box(new Rect(xPos, ((Screen.height/3) * 2) + TOPBUFFER, Screen.width * SCREENPERCENT, Screen.height/3 - BOTTOMBUFFER), "Fitness Goals");

        GUI.Button(new Rect(10, Screen.height - 70, 110, 60), string.Format("Generation: {0}\nFitness: {1:0.00}\nSpecies Count:{2}", _neatSupervisor.CurrentGeneration, _neatSupervisor.CurrentBestFitness, _neatSupervisor.SpeciesCount));
    }
    
    public void OnSaveClick()
    {
        Experiment experiment = new Experiment();
        NetworkActivationScheme networkActivationScheme =
            ExperimentUtils.CreateActivationScheme(activationOptions.options[activationOptions.value].text, String.Empty);
        // TODO:: Fix input output count being hard coded at the end
        experiment.Initialize(nameInput.text, Int32.Parse(popSizeInput.text), Int32.Parse(specieCountInput.text), networkActivationScheme, complexityStrategyInput.text, Int32.Parse(complexityThresholdInput.text), descriptionInput.text, _neatSupervisor, 2, 2);
        _neatSupervisor.SaveExperiment(experiment);
        _neatSupervisor.LoadExperiment(experiment);
    }

    public void OnCancelClick()
    {
        this._newExperimentPopup.SetActive(false);
    }
    
}
