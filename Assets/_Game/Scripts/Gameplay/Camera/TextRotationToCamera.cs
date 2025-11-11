using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class TextRotationToCamera : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshPro m_Text;
    public TMPro.TextMeshPro Text => m_Text;

    [SerializeField] private Transform m_Transform;
    public Transform Transform => m_Transform;

    [SerializeField] private bool m_XRotation = true;
    public bool XRotation => m_XRotation;

    private void Awake()
    {
        Assert.IsTrue(m_Text != null || m_Transform != null);

        if (m_Transform == null)
        {
            m_Transform = m_Text.transform;
        }

        TextRotationToCameraManager.Instance.AddText(this);
    }
}
