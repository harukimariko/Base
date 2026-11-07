using System;
using R3;
using TMPro;
using UnityEngine;

namespace Base
{
    [DisallowMultipleComponent]
    public class LocalizationText : MonoBehaviour
    {
        private TMP_Text _target;
        private LocalizationIndex _localizationIndex;

        private IDisposable _languageSubscription;
        private Action<LanguageData.LanguageType> _applyLanguage;
        private bool _missingIdReported;

        private void Reset() => _target = GetComponent<TMP_Text>();

        private void OnEnable()
        {
            if (!Prepare())
                return;

            _languageSubscription = SaveDataMaster.Instance.LanguageData.Language.Subscribe(_applyLanguage);
        }

        private void OnDisable()
        {
            _languageSubscription?.Dispose();
            _languageSubscription = null;
        }

        public void Refresh()
        {
            if (Prepare())
                _applyLanguage(SaveDataMaster.Instance.LanguageData.Language.Value);
        }

        private bool Prepare()
        {
            if (_target == null)
                _target = GetComponent<TMP_Text>();
            if (_localizationIndex == null)
                _localizationIndex = Resources.Load<LocalizationIndex>("MasterData/LocalizationIndex");

            if (_target == null || _localizationIndex == null)
            {
                Debug.LogWarning("TMP_Text、または Assets/Resources/MasterData/LocalizationIndex.asset が見つかりません。", this);
                return false;
            }

            if (_applyLanguage == null)
            {
                // 翻訳で text を上書きしても、最初の text をキーとして使い続けます。
                var id = _target.text;
                _applyLanguage = language => ApplyLanguage(id, language);
            }
            return true;
        }

        private void ApplyLanguage(string id, LanguageData.LanguageType language)
        {
            if (_target == null)
                return;

            if (_localizationIndex.TryGetText(id, language, out var text))
                _target.text = text;
            else if (!_missingIdReported)
            {
                Debug.LogWarning($"LocalizationID が見つかりません: {id}", this);
                _missingIdReported = true;
            }
        }
    }

}
