using UnityEngine;

// ============================================================
// IABILITY — Oyun Döngüsü ve UI (Bedirhan) sorumluluğunda.
// Kişi 1'in (Yusuf) yazacağı yetenek sınıflarının (ShieldAbility,
// ShockwaveAbility, SpeedBoostAbility vb.) uyması gereken sözleşme.
// Kart Seçim Ekranı, hangi somut ability olduğunu hiç bilmeden
// sadece bu arayüz üzerinden isim/açıklama/ikon gösterip Activate()
// çağıracak.
// ============================================================
public interface IAbility
{
    string AbilityName { get; }  // Kartta gösterilecek isim (örn. "Kalkan")
    string Description { get; }  // Kartta gösterilecek açıklama metni
    Sprite Icon { get; }         // Kartta gösterilecek ikon
    int MaxLevel { get; }

    // "target", yeteneği tetikleyen oyuncu objesi (örn. PlayerCar).
    // İçeride ne olacağına (kendi canını mı etkiler, çevredeki
    // düşmanları mı etkiler vb.) her ability kendi karar verir.
    void Activate(GameObject target, int currentLevel);
    string GetValueAtLevel(int level);
}