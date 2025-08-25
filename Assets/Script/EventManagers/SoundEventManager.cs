using System;

public class SoundEventManager 
{
    //tray action
    public static EventHandler OnAnyTrayDropPlay;
    public static EventHandler OnAnyTrayPickupPlay;
    public static EventHandler OnAnyTraySpawner;
    public static EventHandler OnTryPickupBlocked;
    
    //food action
    public static EventHandler OnAnyFoodSwap;

    //UI action
    public static EventHandler OnAnyButtonClicked;

    //game action
    public static EventHandler OnGameResultShow;

}
