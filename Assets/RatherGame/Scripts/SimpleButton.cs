
using UdonSharp;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class SimpleButton : UdonSharpBehaviour
{
    public UdonBehaviour targetBehavior;
    public string eventName = "Interact";
    public bool masterOnly = false;

    public override void Interact()
    {
        if (targetBehavior)
        {
            if (!masterOnly || Networking.LocalPlayer.isMaster)
            {
                targetBehavior.SendCustomEvent(eventName);
            }
        }
    }
}
