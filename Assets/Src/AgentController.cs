using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        Speed = 8,
        GoalPositionX = 16,
        GoalPositionY = 32,
    }
    
    [Flags]
    public enum OutputTypes
    {
        Rotation = 1,
        Speed = 2,
        XVelocity = 4,
        YVelocity = 8,
    }

    public class AgentController : UnitController
    {
        public float speed = 3.0f;

        public float fitness;
        public double age;
        public double ageMultiplyer = 3000;
        public int speciesId;
        
        
        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;
        
        private InputTypes inputTypes;
        private OutputTypes outputTypes;
        
        private float Fitness { get; set; }

        
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            age = 0;
            speciesId = -1;
        }

        private void Update()
        {
            fitness = this.GetFitness();
            age += 1;
            speciesId = SpeciesId;
        }


        protected override void UpdateBlackBoxInputs(ISignalArray inputSignalArray)
        {
            int usedInputCount = 0;
            
            
            if (inputTypes.HasFlag(InputTypes.PositionX))
            {
                float inputValue = transform.position.x.Map(12, 42, 0, 1);
                inputSignalArray[usedInputCount] = inputValue;
                usedInputCount++;
            }
            
            if (inputTypes.HasFlag(InputTypes.PositionY))
            {
                float inputValue = transform.position.y.Map(5, 35, 0, 1);
                inputSignalArray[usedInputCount] = inputValue;
                usedInputCount++;
            }
            
            if (inputTypes.HasFlag(InputTypes.Rotation))
            {
                float inputValue = transform.rotation.eulerAngles.z;
                inputSignalArray[usedInputCount] = inputValue;
                usedInputCount++;
            }
            
            if (inputTypes.HasFlag(InputTypes.Speed))
            {
                float inputValue = rb.velocity.magnitude.Map(0, speed, 0, 1);
                inputSignalArray[usedInputCount] = inputValue;
                usedInputCount++;
            }
            
            
            if (inputTypes.HasFlag(InputTypes.GoalPositionX))
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
                
                inputSignalArray[usedInputCount] = closestGoal.transform.position.x.Map(12, 42, 0, 1);
                usedInputCount++;
            }
            
            if (inputTypes.HasFlag(InputTypes.GoalPositionY))
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
                
                inputSignalArray[usedInputCount] = closestGoal.transform.position.y.Map(5, 35, 0, 1);
                usedInputCount++;
            }
        }

        protected override void UseBlackBoxOutpts(ISignalArray outputSignalArray)
        {
            int usedOutputCount = 0;


            if (outputTypes.HasFlag(OutputTypes.Rotation))
            {
                float rotation = (float) outputSignalArray[usedOutputCount] * 360f;
                rb.SetRotation(rotation);
                usedOutputCount++;
            }

            if (outputTypes.HasFlag(OutputTypes.XVelocity))
            {
                float xVelocity = (float) outputSignalArray[usedOutputCount];
                rb.velocity = new Vector2(xVelocity.Map(0, 1, -1, 1), rb.velocity.y);
                usedOutputCount++;
            }
            
            if (outputTypes.HasFlag(OutputTypes.YVelocity))
            {
                float yVelocity = (float) outputSignalArray[usedOutputCount];
                yVelocity = yVelocity.Map(0, 1, -1, 1);
                rb.velocity= new Vector2(rb.velocity.x, yVelocity.Map(0, 1, -1, 1));
                usedOutputCount++;
            }
            
            if (outputTypes.HasFlag(OutputTypes.Speed))
            {
                float speedModifer = (float) outputSignalArray[usedOutputCount];
                rb.velocity = rb.velocity * speed * speedModifer;
                usedOutputCount++;
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
            age = 1;
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
                // Using linear distance as fitness
                //return closestDistance.Map(0, 40, closestGoal.GetComponent<FitnessGoal>().fitness, 0) * (float)(1/Math.Ceiling(age / ageMultiplyer));
                
                // Using inverse square as fitness
                return 1 / (closestDistance * closestDistance); //* (float)(1/Math.Ceiling(age / ageMultiplyer));
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