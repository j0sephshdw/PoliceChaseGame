using UnityEngine;
using UnityEngine.UI;

// ============================================================
// BUTTON CLICK SOUND — Oyun Döngüsü ve UI (Bedirhan) sorumluluğunda.
// Herhangi bir butona "Add Component" ile eklenen, o butona tıklanınca
// otomatik olarak genel tık sesini çalan küçük bileşen. Inspector'dan
// ayrıca OnClick() bağlamaya gerek yok, script kendi kendine bağlanıyor.
// ============================================================
[RequireComponent(typeof(Button))]
public class ButtonClickSound : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(UISoundPlayer.PlayClick);
    }
}