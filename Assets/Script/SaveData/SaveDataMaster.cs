using UnityEngine;

namespace Base
{
    public sealed class SaveDataMaster
    {
        private static SaveDataMaster _instance;
        private readonly SaveData _saveData;

        public static SaveDataMaster Instance => _instance ??= new SaveDataMaster();

        public SaveDataContents Data => _saveData.Data;
        public LanguageData LanguageData => _saveData.LanguageData;
        public string SaveFilePath => _saveData.SaveFilePath;

        public string SaveDirectory
        {
            get => _saveData.SaveDirectory;
            set => _saveData.SaveDirectory = value;
        }

        private SaveDataMaster()
        {
            _saveData = new SaveData();
            _saveData.Load();
        }

        public bool Save() => _saveData.Save();
        public bool Load() => _saveData.Load();

        // ドメイン再読み込みを無効にした場合も前回の再生状態を持ち越しません。
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance()
        {
            _instance?._saveData.Dispose();
            _instance = null;
        }
    }
}
