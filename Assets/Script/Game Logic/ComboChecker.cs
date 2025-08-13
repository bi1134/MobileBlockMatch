using System;

public class ComboChecker
{
    public float comboTimer;
    private readonly float comboTimeLimit;
    public int ComboCount { get; private set; }

    public ComboChecker(float comboTimeLimitSeconds)
    {
        comboTimeLimit = comboTimeLimitSeconds;
        GameEventManager.OnTrayFinished += HandleTrayFinished;
    }

    public void Update(float deltaTime)
    {
        if(ComboCount > 0)
        {
            comboTimer -= deltaTime;
            if(comboTimer <= 0f)
            {
                comboTimer = 0f;
                ResetCombo();
            }
        }
    }

    private void HandleTrayFinished(object sender, EventArgs e)
    {
        if (comboTimer > 0f)
        {
            ComboCount++;
        }
        else
        {
            ComboCount = 1; //first in streak
        }
        comboTimer = comboTimeLimit;
        GameEventManager.OnComboChanged?.Invoke(sender, ComboCount);
    }

    private void ResetCombo()
    {
        if (ComboCount > 0)
        {
            ComboCount = 0;
            GameEventManager.OnComboChanged?.Invoke(null, ComboCount);
        }
    }

    public void UnassignSignal()
    {
        GameEventManager.OnTrayFinished -= HandleTrayFinished;
    }
}
