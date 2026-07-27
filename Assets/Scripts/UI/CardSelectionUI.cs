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
        List<ScriptableObject> pool = new List<ScriptableObject>(allAbilities);

        for (int i = 0; i < currentOptions.Length; i++)
        {
            int randomIndex = Random.Range(0, pool.Count);
            currentOptions[i] = pool[randomIndex] as IAbility;
            pool.RemoveAt(randomIndex);
        }
    }

    private void PopulateCards()
    {
        for (int i = 0; i < currentOptions.Length; i++)
        {
            cards[i].icon.sprite = currentOptions[i].Icon;
            cards[i].nameText.text = currentOptions[i].AbilityName;
            cards[i].descriptionText.text = currentOptions[i].Description;
        }
    }

    private void SelectCard(int index)
    {
        currentOptions[index].Activate(playerObject);
        cardSelectionPanel.SetActive(false);
        GameManager.Instance.SetState(GameState.Playing);
    }
}