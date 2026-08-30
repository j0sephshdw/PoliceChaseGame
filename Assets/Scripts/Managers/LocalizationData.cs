using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewLocalizationData", menuName = "Oyun Verileri/Dil Verisi")]
public class LocalizationData : ScriptableObject
{
    [System.Serializable]
    public class LocalizedEntry
    {
        public string label; // Sadece senin Inspector'da hangi satırın ne olduğunu anlaman için, kod bunu kullanmıyor

        // Uzun metinlerde (örn. Nasıl Oynanır açıklaması) alt alta satır yazabilmek için
        // çok satırlı kutu kullanıyoruz; tek satırlık alanda Enter'a basmak mümkün olmuyor.
        [TextArea(2, 8)] public string turkish;
        [TextArea(2, 8)] public string english;
    }

    public List<LocalizedEntry> entries;
}