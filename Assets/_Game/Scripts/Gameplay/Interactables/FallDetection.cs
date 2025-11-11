using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallDetection : MonoBehaviour
{
    [SerializeField] private Transform m_TeleportPoint;

    private void OnTriggerEnter(Collider Other)
    {
        if (Other.gameObject == GameManager.Instance.Player.gameObject)
        {
            GameManager.Instance.Player.transform.position = m_TeleportPoint.transform.position;
        }
    }
}
