using UnityEngine.Events;

// Static class for session management to receive commands from the computer through socket 
public static class SocketCommandsListener
{
    public static int ParticipantID { get; private set; } = 1;

    public static UnityEvent goIdleScene = new(); // used to terminate trial and go to empty scene forced
}