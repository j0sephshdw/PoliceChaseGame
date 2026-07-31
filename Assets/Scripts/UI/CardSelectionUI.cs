using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class CardSelectionUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject cardSelectionPanel;

    [Header("Kullanılabilir Yetenekler (Tümü)")]
    [SerializeField] private List<ScriptableObject> allAbilities;

    [Header("Kartlar (3 adet)")]
    [SerializeField] private CardUI[] cards;

    private GameObject playerObject;
    private IAbility[] currentOptions = new IAbility[3];
    private Dictionary<IAbility, int> abilityLevels = new Dictionary<IAbility, int>();

    private void Start()
    {
        playerObject = FindAnyObjectByType<PlayerCarController>().gameObject;

        cardSelectionPanel.SetActive(false);
        ScoreManager.Instance.OnLevelUp += HandleLevelUp;

        for (int i = 0; i < cards.Length; i++)
        {
            int index = i;
            cards[i].button.onClick.AddListener(() => SelectCard(index));
        }
    }

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnLevelUp -= HandleLevelUp;
    }

    private void HandleLevelUp(int newLevel)
    {
        if (allAbilities.Count < currentOptions.Length)
        {
            Debug.LogWarning("Kart seçimi için yeterli yetenek yok, en az 3 ability eklenmeli.");
            return;
        }

        PickRandomAbilities();
        PopulateCards();
        cardSelectionPanel.SetActive(true);
        UISoundPlayer.PlayCardSelect();
        GameManager.Instance.SetState(GameState.CardSelection);
    }

    private void PickRandomAbilities()
    {
        List<ScriptableObject> pool = new List<ScriptableObject>();

        foreach (ScriptableObject abilityObj in allAbilities)
        {
            IAbility ability = abilityObj as IAbility;
            if (GetAbilityLevel(ability) < ability.MaxLevel)
            {
                pool.Add(abilityObj);
            }
        }

        for (int i = 0; i < currentOptions.Length; i++)
        {
            if (pool.Count == 0)
            {
                currentOptions[i] = null; // gösterilecek yetenek kalmadı (hepsi maksimum seviyede)
                continue;
            }

            int randomIndex = Random.Range(0, pool.Count);
            currentOptions[i] = pool[randomIndex] as IAbility;
            pool.RemoveAt(randomIndex);
        }
    }

    private void PopulateCards()
    {
        for (int i = 0; i < currentOptions.Length; i++)
        {
            IAbility ability = currentOptions[i];

            if (ability == null)
            {
                cards[i].nameText.text = "Maksimum Seviye";
                cards[i].descriptionText.text = "";
                continue;
            }

            int currentLevel = GetAbilityLevel(ability);
            int nextLevel = currentLevel + 1;

            cards[i].icon.sprite = ability.Icon;
            cards[i].nameText.text = ability.AbilityName + " (Seviye " + nextLevel + ")";
            cards[i].descriptionText.text = ability.Description + "\n" +
                ability.GetValueAtLevel(currentLevel) + " → " + ability.GetValueAtLevel(nextLevel);
        }
    }

    private void SelectCard(int index)
    {
        IAbility selected = currentOptions[index];
        if (selected == null) return; // "Maksimum Seviye" kartına tıklanırsa hiçbir şey yapma

        int currentLevel = GetAbilityLevel(selected);
        selected.Activate(playerObject, currentLevel);

        if (abilityLevels.ContainsKey(selected))
            abilityLevels[selected]++;
        else
            abilityLevels[selected] = 1;

        cardSelectionPanel.SetActive(false);
        GameManager.Instance.SetState(GameState.Playing);
    }

    private int GetAbilityLevel(IAbility ability)
    {
        return abilityLevels.ContainsKey(ability) ? abilityLevels[ability] : 0;
    }
}