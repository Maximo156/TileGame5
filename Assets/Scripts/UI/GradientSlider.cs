using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class GradientSlider : Slider
{
    public Gradient Gradient;
    private Image img;
    protected override void Awake()
    {
        img = fillRect.GetComponent<Image>();
        base.Start();
    }
    public override float value 
    {   
        get => base.value; 
        set {
            img.color = Gradient.Evaluate(value);
            base.value = value;
        }
    }
}
