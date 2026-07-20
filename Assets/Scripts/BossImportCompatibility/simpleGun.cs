using System;
using UnityEngine;

public class simpleGun : MonoBehaviour
{
    public static event Action<Vector3> OnGunshot;

    public static void BroadcastGunshot(Vector3 soundPos)
    {
        OnGunshot?.Invoke(soundPos);
    }
}
