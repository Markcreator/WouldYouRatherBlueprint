
using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class RatherData : UdonSharpBehaviour
{
    [Range(0, 1), Tooltip("0 = Yes/No\n1 = Would you rather")] public int defaultGamemode = 0;
    [Range(0, 1), Tooltip("0 = Texture Prompts\n1 = Text Prompts")] public int promptMode = 0;
    public Texture2D[] texturePrompts;
    public string[] textPrompts;
}
