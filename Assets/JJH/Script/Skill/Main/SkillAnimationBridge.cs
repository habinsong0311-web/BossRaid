using ExitGames.Client.Photon;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;
using SF = UnityEngine.SerializeField;

public class SkillAnimationBridge : MonoBehaviour
{
    [SF] private SkillSpawner spawner;

    public void ReceiveAnimEvent(string eventName)
    {
        if (spawner != null)
        {
            spawner.ReceiveAnimEvent(eventName);
        }
    }
}
