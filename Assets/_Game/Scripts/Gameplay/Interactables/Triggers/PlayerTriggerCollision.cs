using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Events;

public class PlayerTriggerCollision : MonoBehaviour
{
    [SerializeField] private UnityEvent m_OnPlayerEntered;
    [SerializeField] private UnityEvent m_OnPlayerLeft;

    private void OnCollisionEnter(Collision Other)
    {
        if (Other.gameObject != GameManager.Instance.Player.gameObject)
        {
            return;
        }

        m_OnPlayerEntered?.Invoke();
    }

    private void OnCollisionExit(Collision Other)
    {
        if (Other.gameObject != GameManager.Instance.Player.gameObject)
        {
            return;
        }

        m_OnPlayerLeft?.Invoke();
    }
}
