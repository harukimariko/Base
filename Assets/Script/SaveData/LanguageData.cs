using System;
using MessagePack;
using R3;

namespace Base
{
    [Serializable]
    [MessagePackObject]
    public partial class LanguageData : IDisposable
    {
        public enum LanguageType
        {
            Japanese = 0,
            English = 1,
        }

        [IgnoreMember]
        public ReactiveProperty<LanguageType> Language { get; } =
            new ReactiveProperty<LanguageType>(LanguageType.Japanese);

        // Key は以前と同じにし、ReactiveProperty 本体ではなく値だけ保存します。
        [Key(0)]
        public LanguageType SavedLanguage
        {
            get => Language.Value;
            set => Language.Value = value;
        }

        public void Dispose() => Language.Dispose();
    }
}
