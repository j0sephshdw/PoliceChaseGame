using System;
using UnityEngine;

public static class Localization
{
    public enum Language { Turkish, English }

    private const string LanguageKey = "SelectedLanguage";
    public static event Action OnLanguageChanged;

    public static Language CurrentLanguage
    {
        get { return (Language)PlayerPrefs.GetInt(LanguageKey, 0); } // 0 = Turkish varsayılan
        set
        {
            PlayerPrefs.SetInt(LanguageKey, (int)value);
            PlayerPrefs.Save();
            OnLanguageChanged?.Invoke();
        }
    }
}