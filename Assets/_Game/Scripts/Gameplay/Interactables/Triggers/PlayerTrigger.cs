using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Events;

public class PlayerTrigger : MonoBehaviour
{
    [SerializeField] private UnityEvent m_OnPlayerEntered;
    [SerializeField] private UnityEvent m_OnPlayerLeft;

    private void OnTriggerEnter(Collider Other)
    {
        if (Other.gameObject != GameManager.Instance.Player.gameObject)
        {
            return;
        }

        m_OnPlayerEntered?.Invoke();
    }

    private void OnTriggerExit(Collider Other)
    {
        if (Other.gameObject != GameManager.Instance.Player.gameObject)
        {
            return;
        }

        m_OnPlayerLeft?.Invoke();
    }
}
