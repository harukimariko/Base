using System;
using System.IO;
using MessagePack;
using UnityEngine;

namespace Base
{
    public class SaveData : IDisposable
    {
        private string _saveDirectory;
        private readonly string _fileName;
        private readonly SaveDataContents _data = new SaveDataContents();

        private static readonly MessagePackSerializerOptions SerializerOptions =
            MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData);

        public SaveDataContents Data => _data;
        public LanguageData LanguageData => _data.LanguageData;

        public SaveData(string saveDirectory = null, string fileName = "save.msgpack")
        {
            _saveDirectory = saveDirectory ?? "";
            _fileName = fileName;
        }

        public string SaveDirectory
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_saveDirectory))
                    return Application.persistentDataPath;

                return Path.GetFullPath(Path.IsPathRooted(_saveDirectory)
                    ? _saveDirectory
                    : Path.Combine(Application.persistentDataPath, _saveDirectory));
            }
            set => _saveDirectory = value ?? "";
        }

        public string SaveFilePath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_fileName) ||
                    _fileName != Path.GetFileName(_fileName) ||
                    _fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    throw new ArgumentException("File Name にはフォルダーを含まない有効なファイル名を指定してください。");
                }

                return Path.Combine(SaveDirectory, _fileName);
            }
        }

        public bool Save()
        {
            string temporaryPath = null;
            try
            {
                Validate(_data);
                var bytes = MessagePackSerializer.Serialize(_data, SerializerOptions);
                var path = SaveFilePath;
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                // 書き込みが完了するまでは既存のセーブファイルを置き換えません。
                temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                File.WriteAllBytes(temporaryPath, bytes);
                if (File.Exists(path))
                    File.Replace(temporaryPath, path, null);
                else
                    File.Move(temporaryPath, path);

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"セーブに失敗しました: {exception.Message}");
                return false;
            }
            finally
            {
                if (temporaryPath != null)
                {
                    try
                    {
                        if (File.Exists(temporaryPath))
                            File.Delete(temporaryPath);
                    }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
        }

        public bool Load()
        {
            try
            {
                var path = SaveFilePath;
                if (!File.Exists(path))
                    return false;

                var loaded = MessagePackSerializer.Deserialize<SaveDataContents>(
                    File.ReadAllBytes(path), SerializerOptions);
                try
                {
                    Validate(loaded);
                    // 参照を置き換えずに更新し、外部の購読を維持します。
                    _data.Version = loaded.Version;
                    if (_data.LanguageData == null)
                        _data.LanguageData = new LanguageData();
                    _data.LanguageData.Language.Value = loaded.LanguageData.Language.Value;
                    return true;
                }
                finally
                {
                    loaded?.LanguageData?.Dispose();
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"ロードに失敗しました。現在のデータは維持します: {exception.Message}");
                return false;
            }
        }

        public void Dispose() => _data.LanguageData?.Dispose();

        private static void Validate(SaveDataContents data)
        {
            if (data == null || data.Version != 1)
                throw new InvalidDataException("対応していないセーブデータです。");

            if (data.LanguageData == null)
                data.LanguageData = new LanguageData();

            if (!Enum.IsDefined(typeof(LanguageData.LanguageType), data.LanguageData.Language.Value))
                throw new InvalidDataException("保存された言語の値が正しくありません。");
        }
    }

    [Serializable]
    [MessagePackObject]
    public partial class SaveDataContents
    {
        [Key(0)] public int Version = 1;
        [Key(1)] public LanguageData LanguageData = new LanguageData();
        // 今後のセーブ項目は新しい Key を付けてここに追加します。
    }
}
