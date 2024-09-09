using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player;
    public Vector3 offset;

    /* Make camera follow the player. */
    private void LateUpdate()
    {
        if (player)
        {
            transform.position = player.position + offset;
        }
    }
}
