using System;
using System.Collections;
using System.Collections.Generic;
using SharpNeat.Phenomes;
using UnityEngine;
using UnitySharpNEAT;

public class AgentController : UnitController
{
    public float speed = 3.0f;
    
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    
    private float Fitness { get; set; }
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    protected override void UpdateBlackBoxInputs(ISignalArray inputSignalArray)
    {
        inputSignalArray[0] = rb.transform.position.x;
        inputSignalArray[1] = rb.transform.position.y;
    }

    protected override void UseBlackBoxOutpts(ISignalArray outputSignalArray)
    {
        //rb.velocity = new Vector2((float)outputSignalArray[0] * speed, (float)outputSignalArray[1] * speed);
        float rotation = (float) outputSignalArray[0] * 360f;
        float speedModifer = (float) outputSignalArray[1];
        
        rb.SetRotation(rotation);
        rb.velocity= rb.transform.right * speed * speedModifer;
    }

    public override float GetFitness()
    {
        return Fitness;
    }

    protected override void HandleIsActiveChanged(bool newIsActive)
    {
        rb.transform.SetPositionAndRotation(new Vector3(UnityEngine.Random.Range(10f, 14f),UnityEngine.Random.Range(10f, 14f),0), Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f)));
        rb.velocity = new Vector2(0, 0);
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
    }
}
