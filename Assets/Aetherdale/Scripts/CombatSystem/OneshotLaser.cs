

/// <summary>
/// A laser instance that stays instantiated and waits for Fire() to be called before firing
/// </summary>
public class OneshotLaser : Laser
{
    public override void FixedUpdate()
    {
        UpdatePositions();
    }
}