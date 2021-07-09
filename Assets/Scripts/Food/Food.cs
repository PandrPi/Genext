using System.Collections;
using UnityEngine;

public class Food : MonoBehaviour
{
    public float size = 3;
    [SerializeField] private float energy = 20;

    private Transform myTransform;
    private SpriteRenderer sr;
    private CircleCollider2D circleCollider;

    private const float InitialEnergy = 500.0f;
    private const float ParameterRandomRange = 0.1f;
    private const float TimeToRegrowth = 30.0f;

    private void Start()
    {
        myTransform = transform;
        sr = GetComponent<SpriteRenderer>();
        circleCollider = GetComponent<CircleCollider2D>();
        Regrowth();
        size = sr.bounds.size.x;
    }

    private IEnumerator StartRegrowthTimer()
    {
        yield return new WaitForSeconds(TimeToRegrowth);
        
        Regrowth();
    }

    private void Regrowth()
    {
        myTransform.position = FoodManager.Manager.GetRandomWorldPosition();
        energy = InitialEnergy + MathHelper.RandomValue(InitialEnergy * ParameterRandomRange);
        sr.enabled = true;
        circleCollider.enabled = true;
    }

    public void SetFoodSpriteColor(Color color)
    {
        sr.color = color;
    }


    /// <summary>
    /// Use to determine the amount of eaten energy 
    /// </summary>
    /// <param name="energyPerBite">How many energy needs to subtract</param>
    /// <returns></returns>
    public float EatMe(float energyPerBite)
    {
        if (energy <= 0)
        {
            energy = 0;
            sr.enabled = false;
            circleCollider.enabled = false;

            StartCoroutine(StartRegrowthTimer());
            
            return 0;
        }

        if (energy < energyPerBite)
        {
            float temp = energy;
            energy = 0;
            return temp;
        }

        energy -= energyPerBite;
        return energyPerBite;
    }
}