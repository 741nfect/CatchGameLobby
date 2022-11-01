namespace LobbyRelaySample
{
    /// <summary>
    /// Holds a LocalPlayer value and notifies all subscribers when it has been changed.
    /// Check the GameManager in the mainScene for the list of observers being used in the project.
    /// </summary>
    public class LobbyUserObserver : ObserverBehaviour<LocalPlayer> { }
}
