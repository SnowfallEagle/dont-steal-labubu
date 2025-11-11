using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GroundButton : MonoBehaviour
{
    [SerializeField] private UnityEvent m_OnPlayerEnteredEvent;

    private void OnTriggerEnter(Collider Other)
    {
        if (Other.gameObject != GameManager.Instance.Player.gameObject)
        {
            return;
        }

        OnPlayerEntered(Other);
    }

    protected virtual void OnPlayerEntered(Collider Collider)
    {
        // Debug.Log("GroundButton: Trigger Entered " + Collider.gameObject.name);
        m_OnPlayerEnteredEvent?.Invoke();
    }
}
