using UnityEngine;

public class Anghost : Boss
{
    public const int LEVEL_UP_INTERVAL_SECONDS = 30;

    float lastLevelUp = 0;


    public override void Start()
    {
        base.Start();

        lastLevelUp = Time.time;
    }

    public override void Update()
    {
        base.Update();

        if (Time.time - lastLevelUp > LEVEL_UP_INTERVAL_SECONDS)
        {
            LevelUp();
        }
    }

    void LevelUp()
    {
        Player.SendEnvironmentChatMessage($"Anghost has become stronger! {GetCurrentHealth()}");
        SetLevel(GetLevel() + 1);
        lastLevelUp = Time.time;
    }

    public override string GetDisplayName()
    {
        return base.GetDisplayName() + $" - Level {GetLevel()}";
    }
}
