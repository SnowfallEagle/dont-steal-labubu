using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextRotationToCameraManager : Singleton<TextRotationToCameraManager>
{
    private List<TextRotationToCamera> m_Texts = new List<TextRotationToCamera>();

    private void Start()
    {
        TimerService.Instance.AddTimer(new TimerService.Handle(), this, () => { m_Texts.RemoveAll(Text => Text == null); }, 1f, true);
    }

    public void AddText(TextRotationToCamera Text)
    {
        if (Text != null && !m_Texts.Contains(Text))
        {
            m_Texts.Add(Text);
            UpdateText(Text);
        }
    }

    private void UpdateText(TextRotationToCamera Text)
    {
        if (Text == null)
        {
            return;
        }

        Vector3 Angles = Quaternion.LookRotation(Camera.main.transform.forward).eulerAngles;

        if (!Text.XRotation)
        {
            Angles.x = Text.transform.rotation.eulerAngles.x;
        }

        Text.Transform.rotation = Quaternion.Euler(Angles);
    }

    private void Update()
    {
        for (int i = 0; i < m_Texts.Count; i++)
        {
            UpdateText(m_Texts[i]);
        }
    }
}
