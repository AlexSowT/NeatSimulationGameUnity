using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnitySharpNEAT;

public class GameUI : MonoBehaviour
{
    private const float RIGHTBUFFER = 10;
    private const float TOPBUFFER = 10;
    private const float BOTTOMBUFFER = 15;
    private const float SCREENPERCENT = 0.3f;
    
    [SerializeField] 
    private NeatSupervisor _neatSupervisor;

    
    private void OnGUI()
    {
        float xPos = (Screen.width - (Screen.width * SCREENPERCENT)) - RIGHTBUFFER;
        // Model Options
        GUI.Box(new Rect(xPos,  ((Screen.height/3) * 0) + TOPBUFFER, Screen.width * SCREENPERCENT, Screen.height/3 - BOTTOMBUFFER), "Model Options");
        
        if (GUI.Button(new Rect(xPos + 20f, 35, 110, 40), "New Experiment"))
        {
        }
        if (GUI.Button(new Rect(xPos + 20f, 80, 110, 40), "Save Experiment"))
        {
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
}
