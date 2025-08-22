using System;

public class GameEventManager
{
    //game manager event
    public static EventHandler OnGameStateChanged;
    public static Action<object, int> OnComboChanged;

    //tray
    public static EventHandler OnTrayFinished;
    public static EventHandler OnTrayGoesOut;


    public static event EventHandler<OnProgressChangedEventArgs> OnProgressChanged;
    public class OnProgressChangedEventArgs : EventArgs
    {
        public float progressNormalized;
        public int moveCount;
    }

    public static void VisualProgressChanged(object sender, float progressNormalized, int moveCount)
    {
        OnProgressChanged?.Invoke(sender, new OnProgressChangedEventArgs
        {
            progressNormalized = progressNormalized,
            moveCount = moveCount
        });
    }

}
