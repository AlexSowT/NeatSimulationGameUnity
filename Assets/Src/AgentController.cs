using System;
using System.Collections;
using System.Collections.Generic;
using SharpNeat.Phenomes;
using Src.Outputs;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnitySharpNEAT;

namespace Src
{
    [Flags]
    public enum InputTypes
    {
        PositionX = 1,
        PositionY = 2,
        Rotation = 4,
        Speed = 8
    }
    
    [Flags]
    public enum OutputTypes
    {
        Rotation = 1,
        Speed = 2
    }

    public class AgentController : UnitController
    {
        public float speed = 3.0f;
    
        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;
        
        private InputTypes inputTypes;
        private OutputTypes outputTypes;
        
        private float Fitness { get; set; }
    
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        protected override void UpdateBlackBoxInputs(ISignalArray inputSignalArray)
        {
            if (inputTypes.HasFlag(InputTypes.PositionX))
            {
                inputSignalArray[0] = transform.position.x;
            }
            
            if (inputTypes.HasFlag(InputTypes.PositionY))
            {
                inputSignalArray[1] = transform.position.y;
            }
            
            if (inputTypes.HasFlag(InputTypes.Rotation))
            {
                inputSignalArray[2] = transform.rotation.eulerAngles.z;
            }
            
            if (inputTypes.HasFlag(InputTypes.Speed))
            {
                inputSignalArray[3] = rb.velocity.magnitude;
            }
        }

        protected override void UseBlackBoxOutpts(ISignalArray outputSignalArray)
        {
            if (outputTypes.HasFlag(OutputTypes.Rotation))
            {
                float rotation = (float) outputSignalArray[0] * 360f;
                rb.SetRotation(rotation);
            }
            
            if (outputTypes.HasFlag(OutputTypes.Speed))
            {
                float speedModifer = (float) outputSignalArray[1]; 
                rb.velocity= rb.transform.right * speed * speedModifer;
            }
        }

        // Simple just get fitness for when you've entered the trigger goal
        /*public override float GetFitness()
    {
        return Fitness;
    }*/
        
        protected override void HandleIsActiveChanged(bool newIsActive)
        {
            if (this.SpawnLocation != null)
            {
                rb.transform.SetPositionAndRotation(new Vector3(this.SpawnLocation.x,this.SpawnLocation.y,0), Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f)));
            }
            else{
                rb.transform.SetPositionAndRotation(new Vector3(UnityEngine.Random.Range(10f, 14f),UnityEngine.Random.Range(10f, 14f),0), Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f)));
            }
        
            rb.velocity = new Vector2(0, 0);
        }
    
        public override float GetFitness()
        {
            GameObject[] fitnessGoals = GameObject.FindGameObjectsWithTag("FitnessGoal");
        
            // Find the closest goal
            GameObject closestGoal = null;
            float closestDistance = float.MaxValue;
            foreach (GameObject goal in fitnessGoals)
            {
                float distance = Vector3.Distance(goal.transform.position, transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestGoal = goal;
                }
            }
        
            if (closestGoal != null)
            {

                float value = Math.Max(0, closestDistance); // Scale value between itself and 0
                value = Math.Min(closestDistance, closestGoal.GetComponent<FitnessGoal>().fitness); // Scale value between itself and the fitness goal
                return closestGoal.GetComponent<FitnessGoal>().fitness - value; // Return the fitness goal minus the distance to the goal
            }
            else
            {
                return 0f;
            }
        }

        public void SetInputs(InputTypes inputTypes)
        {
            this.inputTypes = inputTypes;
        }

        public void SetOutputs(OutputTypes outputTypes)
        {
            this.outputTypes = outputTypes;
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("FitnessGoal"))
            {
                this.Fitness = other.GetComponent<FitnessGoal>().fitness;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("FitnessGoal"))
            {
                this.Fitness = 0;
            }
        }
    
        private void FixedUpdate()
        {
            base.FixedUpdate();
            this.spriteRenderer.color = UnitColor;
            gameObject.SetActive(this.enabled);
        }
    }
}