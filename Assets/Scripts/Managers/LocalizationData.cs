using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewLocalizationData", menuName = "Oyun Verileri/Dil Verisi")]
public class LocalizationData : ScriptableObject
{
    [System.Serializable]
    public class LocalizedEntry
    {
        public string label; // Sadece senin Inspector'da hangi satırın ne olduğunu anlaman için, kod bunu kullanmıyor
        public string turkish;
        public string english;
    }

    public List<LocalizedEntry> entries;
}