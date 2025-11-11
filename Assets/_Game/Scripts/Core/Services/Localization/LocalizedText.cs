using UnityEngine;
using TMPro;

public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string m_TranslationKey;
    private TextMeshProUGUI m_TextComponentUI;
    private TextMeshPro m_TextComponent;

    private void Awake()
    {
        m_TextComponentUI = GetComponent<TextMeshProUGUI>();
        m_TextComponent = GetComponent<TextMeshPro>();
    }

    private void Start()
    {
        RefreshText();
    }

    private void OnEnable()
    {
        RefreshText();
    }

    public void RefreshText()
    {
        if (string.IsNullOrEmpty(m_TranslationKey))
        {
            return;
        }

        string Translation = LocalizationManager.Instance.GetTranslation(m_TranslationKey);

        if (m_TextComponentUI)
        {
            m_TextComponentUI.text = Translation;
        }
        if (m_TextComponent)
        {
            m_TextComponent.text = Translation;
        }
    }
} 