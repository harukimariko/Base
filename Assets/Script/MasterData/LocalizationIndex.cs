using System;
using System.Collections.Generic;
using UnityEngine;

namespace Base
{
    [CreateAssetMenu(menuName = "Master Data/Localization Index", fileName = "LocalizationIndex")]
    public class LocalizationIndex : MasterDataTable<LocalizationIndex.LocalizationData>
    {
        [Serializable]
        public class LocalizationData
        {
            public string LocalizationID;
            public string Japanese;
            public string English;
        }

        public override void ValidateRows(Array rows)
        {
            base.ValidateRows(rows);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in (LocalizationData[])rows)
            {
                if (row == null || string.IsNullOrWhiteSpace(row.LocalizationID))
                    throw new ArgumentException("LocalizationID が空の行があります。");
                if (!ids.Add(row.LocalizationID))
                    throw new ArgumentException($"LocalizationID が重複しています: {row.LocalizationID}");
            }
        }

        public bool TryGetText(string id, LanguageData.LanguageType language, out string text)
        {
            text = null;
            if (string.IsNullOrEmpty(id))
                return false;

            foreach (var entry in Rows)
            {
                if (entry == null || !string.Equals(entry.LocalizationID, id, StringComparison.Ordinal))
                    continue;

                text = language == LanguageData.LanguageType.English ? entry.English : entry.Japanese;
                if (string.IsNullOrEmpty(text))
                    text = entry.Japanese ?? "";
                return true;
            }
            return false;
        }
    }
}
