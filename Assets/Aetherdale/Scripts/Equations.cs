using UnityEngine;

public abstract class Equation
{
    // ------- Game balance control panel ------------------------------------------------------
    public static readonly PolynomialEquation PLAYER_EXP_PER_LEVEL = new(1F, 1.1F, 500, 500); // Exp to get to next level
    public static readonly PolynomialEquation EXP_AWARDED_PER_EFFECTIVE_HEALTH = new(3F, 0.8F, 0.5F, 10);
    public static readonly PolynomialEquation ENTITY_HEALTH_SCALING = new(0.003F, 2.5F, 0.2F, 1.0F);
    public static readonly PolynomialEquation BOSS_HEALTH_SCALING = new(0.003F, 2.0F, 0.2F, 1.0F);

    public static readonly LinearEquation PLAYER_GLOBAL_DAMAGE_MULTIPLIER = new(0.1F, 1F);
    public static readonly PolynomialEquation ENEMY_GLOBAL_DAMAGE_MULTIPLIER = new(0.06F, 1.5F, 0, 1F);
    // ----------------------------------------------------------------------------------------


    public abstract float Calculate(float level);
    public abstract int ReverseCalculate(float value);
}

public class LinearEquation : Equation
{
    public readonly float leadingCoefficient;
    public readonly float baseValue;

    /// <returns>A new LinearEquation that passes through the two points</returns>
    public static LinearEquation FromTwoPoints(Vector2 p1, Vector2 p2)
    {
        float rise = p2.y - p1.y;
        float run = p2.x - p1.x;

        float slope;
        if (run == 0)
        {
            slope = p2.y > p1.y ? Mathf.Infinity : Mathf.NegativeInfinity;
        }
        else if (run == Mathf.Infinity)
        {
            slope = 0;
        }
        else
        {
            slope = rise / run;
        }

        // TODO there might be some logic errors here - infinity/zero not accounted for
        // y @ x=0 is <p1.y - (rise since 0)>
        float baseValue = p1.y - (slope * p1.x);

        return new LinearEquation(slope, baseValue);
    }


    public LinearEquation(float leadingCoefficient, float baseValue)
    {
        this.leadingCoefficient = leadingCoefficient;
        this.baseValue = baseValue;
    }

    public override float Calculate(float input)
    {
        return leadingCoefficient * input + baseValue;
    }

    public override int ReverseCalculate(float output)
    {
        return (int)((output - baseValue) / leadingCoefficient);
    }

}


/// <summary>
/// f(x) = Ax^B + Cx + D
/// </summary>
[System.Serializable]
public class PolynomialEquation : Equation
{
    readonly float leadingCoefficient1;
    readonly float degree;
    readonly float leadingCoefficient2;
    readonly float baseValue;

    public PolynomialEquation(float leadingCoefficient1, float degree, float leadingCoefficient2, float baseValue)
    {
        this.leadingCoefficient1 = leadingCoefficient1;
        this.degree = degree;
        this.leadingCoefficient2 = leadingCoefficient2;
        this.baseValue = baseValue;
    }


    public override float Calculate(float level)
    {
        return leadingCoefficient1 * Mathf.Pow(level, degree) + (leadingCoefficient2 * level) + baseValue;
    }

    public override int ReverseCalculate(float value)
    {
        return (int) (Mathf.Pow((value - 1) / degree, 1 / leadingCoefficient1) + baseValue);
    }

}

/// <summary>
/// f(x) = a + b^x
/// </summary>
[System.Serializable]
public class ExponentialEquation : Equation
{
    readonly float a;
    readonly float b;

    public ExponentialEquation(float a, float b)
    {
        this.a = a;
        this.b = b;
    }

    public override float Calculate(float level)
    {
        return a + Mathf.Pow(b, level);
    }

    public override int ReverseCalculate(float value)
    {
        return (int) Mathf.Log(value - a, b);
    }

}

public class ProjectileArc
{
    readonly Vector3 startPos;
    readonly Vector3 startVelocity;

    public ProjectileArc(Vector3 startPos, Vector3 startVelocity)
    {
        this.startPos = startPos;
        this.startVelocity = startVelocity;
    }

    public virtual Vector3 Calculate(float time)
    {
        Vector3 nonGravityPos = startPos + (time * startVelocity);

        float gravity = 0.5F * Physics.gravity.y * time * time;

        return nonGravityPos + new Vector3(0, gravity, 0);
    }
}
