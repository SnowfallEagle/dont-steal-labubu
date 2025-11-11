using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Assertions;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MirraGames.SDK;
using MirraGames.SDK.Common;

public class LocalizationManager : SingletonPersistent<LocalizationManager>
{
    public event System.Action OnRefresh;

    private Dictionary<string, string> m_Translations = new Dictionary<string, string>();
    [SerializeField] private LanguageType m_CurrentLanguage = LanguageType.English;
    public LanguageType CurrentLanguage => m_CurrentLanguage;

#if UNITY_EDITOR
    public LanguageType DebugLanguage = LanguageType.Russian;
#endif

    private void Awake()
    {
        LoadLanguage(LanguageType.English);
    }

    public void LoadLanguage(LanguageType Lang)
    {
        Debug.Log($"{nameof(LocalizationManager)}.{nameof(LoadLanguage)}() called with {Lang}");

        // Support only RU / EN
        if (Lang != LanguageType.Russian && Lang != LanguageType.English)
        {
            if (Lang == LanguageType.Ukrainian)
            {
                Lang = LanguageType.Russian;
            }
            else
            {
                Lang = LanguageType.English;
            }
        }

        m_CurrentLanguage = Lang;
#if UNITY_EDITOR
        m_CurrentLanguage = DebugLanguage;
#endif
        m_Translations.Clear();

        var StrPath = $"Localization/{(m_CurrentLanguage == LanguageType.Russian ? "ru" : "en")}";
        var Json = Resources.Load<TextAsset>(StrPath);

        if (Json == null)
        {
            Debug.LogError($"Language file not found: {StrPath}");
        }

        string JsonContent = Json.text;

        // Remove the outer curly braces and split by commas
        JsonContent = JsonContent.Trim().Trim('{', '}').Trim();
        string[] Pairs = JsonContent.Split(',');

        foreach (string Pair in Pairs)
        {
            int IndexColon = Pair.IndexOf(':');
            if (IndexColon < 0)
            {
                continue;
            }

            string Key = Pair.Substring(0, IndexColon).Trim().Trim('"');
            string Value = Pair.Substring(IndexColon + 1).Trim().Trim('"');

            m_Translations[Key] = Value;
        }

        Refresh();
    }

    public string GetTranslation(string Key)
    {
        if (m_Translations.TryGetValue(Key, out string Translation))
        {
            return Translation;
        }
        return Key; // Return the key if translation is not found
    }

    public void SetLanguage(LanguageType Lang)
    {
        if (m_CurrentLanguage != Lang)
        {
            LoadLanguage(Lang);
        }
    }

    public void Refresh()
    {
        LocalizedText[] LocalizedTexts = FindObjectsByType<LocalizedText>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (LocalizedText Text in LocalizedTexts)
        {
            Text.RefreshText();
        }

        OnRefresh?.Invoke();
    }
} 