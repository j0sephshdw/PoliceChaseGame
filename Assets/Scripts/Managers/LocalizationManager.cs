using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class LocalizationManager : MonoBehaviour
{
    [System.Serializable]
    public class TextBinding
    {
        public TMP_Text targetText; // Hangi metin objesi güncellenecek
        public int entryIndex;      // MainMenu_Localization listesindeki hangi satır
    }

    [SerializeField] private LocalizationData sceneData;
    [SerializeField] private List<TextBinding> bindings;

    private void OnEnable()
    {
        Localization.OnLanguageChanged += UpdateAllTexts;
        UpdateAllTexts();
    }

    private void OnDisable()
    {
        Localization.OnLanguageChanged -= UpdateAllTexts;
    }

    private void UpdateAllTexts()
    {
        if (sceneData == null) return;

        foreach (TextBinding binding in bindings)
        {
            if (binding.targetText == null) continue;
            if (binding.entryIndex < 0 || binding.entryIndex >= sceneData.entries.Count) continue;

            LocalizationData.LocalizedEntry entry = sceneData.entries[binding.entryIndex];
            binding.targetText.text = Localization.CurrentLanguage == Localization.Language.Turkish
                ? entry.turkish
                : entry.english;
        }
    }
}