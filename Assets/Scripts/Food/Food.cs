using System.Collections;
using UnityEngine;

public class Food : MonoBehaviour
{
    public float Size = 3;
    [SerializeField] private float Energy = 20;

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
        Size = sr.bounds.size.x;
    }

    // private void FixedUpdate()
    // {
    //     // if (Time.frameCount % FramesToSkip != 0) return;
    //     // if (isDead != true) return;
    //     //
    //     // RegrowthTimer += Time.fixedDeltaTime * FramesToSkip;
    //     // if (RegrowthTimer >= TimeToRegrowth)
    //     // {
    //     //     Regrowth();
    //     // }
    // }

    private IEnumerator StartRegrowthTimer()
    {
        yield return new WaitForSeconds(TimeToRegrowth);
        
        Regrowth();
    }

    private void Regrowth()
    {
        myTransform.position = FoodManager.Manager.GetRandomWorldPosition();
        Energy = InitialEnergy + MathHelper.RandomValue(InitialEnergy * ParameterRandomRange);
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
        if (Energy <= 0)
        {
            Energy = 0;
            sr.enabled = false;
            circleCollider.enabled = false;

            StartCoroutine(StartRegrowthTimer());
            
            return 0;
        }

        if (Energy < energyPerBite)
        {
            float temp = Energy;
            Energy = 0;
            return temp;
        }

        Energy -= energyPerBite;
        return energyPerBite;
    }
}