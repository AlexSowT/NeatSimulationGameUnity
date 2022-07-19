using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FitnessGoal : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    
    // Start is called before the first frame update
    public float fitness;

    public void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void OnMouseEnter()
    {
        Color currentColor = spriteRenderer.color;
        spriteRenderer.color = new Color(currentColor.r, currentColor.g, currentColor.b, 1f);
    }

    public void OnMouseExit()
    {
        Color currentColor = spriteRenderer.color;
        spriteRenderer.color = new Color(currentColor.r, currentColor.g, currentColor.b, 0.35f);

    }

    public void OnMouseDrag()
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        transform.Translate(mousePosition);
    }
}
