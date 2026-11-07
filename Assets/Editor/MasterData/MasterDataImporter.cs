using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Base.Editor
{
    public static class MasterDataImporter
    {
        public const string SourceDirectory = "Assets/Master/Excel";
        public const string JsonDirectory = "Assets/Master/Json";
        public const string AssetDirectory = "Assets/Resources/MasterData";

        [MenuItem("Tools/マスターデータ/すべて変換")]
        public static void ImportAll()
        {
            if (!Directory.Exists(SourceDirectory))
                return;
            var count = 0;
            foreach (var path in Directory.GetFiles(SourceDirectory, "*.xlsx", SearchOption.AllDirectories).OrderBy(p => p))
            {
                if (IsSource(path) && TryImport(path))
                    count++;
            }
            Debug.Log($"マスターデータを {count} 件変換しました。");
        }

        [MenuItem("Assets/マスターデータに変換", false, 2000)]
        private static void ImportSelected() => TryImport(AssetDatabase.GetAssetPath(Selection.activeObject));

        [MenuItem("Assets/マスターデータに変換", true)]
        private static bool CanImportSelected() => IsSource(AssetDatabase.GetAssetPath(Selection.activeObject));

        public static bool IsSource(string path)
        {
            path = path.Replace('\\', '/');
            return path.StartsWith(SourceDirectory + "/", StringComparison.Ordinal) &&
                   path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) &&
                   !Path.GetFileName(path).StartsWith("~$", StringComparison.Ordinal);
        }

        public static bool TryImport(string path)
        {
            try
            {
                ImportWorkbook(path);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"マスターデータの変換に失敗しました: {path}\n{exception.Message}");
                return false;
            }
        }

        public static MasterDataAsset ImportWorkbook(string path)
        {
            path = path.Replace('\\', '/');
            if (!IsSource(path))
                throw new ArgumentException($"{SourceDirectory} 内の .xlsx を指定してください。");
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("再生を停止してからマスターデータを変換してください。");

            var name = Path.GetFileNameWithoutExtension(path);
            var matches = TypeCache.GetTypesDerivedFrom<MasterDataAsset>()
                .Where(t => !t.IsAbstract && !t.ContainsGenericParameters && t.Name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidDataException($"'{name}' と同名の MasterDataTable<T> 実装を1つ用意してください（検出: {matches.Length} 件）。");
            var duplicates = Directory.GetFiles(SourceDirectory, "*.xlsx", SearchOption.AllDirectories)
                .Where(IsSource).Count(p => Path.GetFileNameWithoutExtension(p) == name);
            if (duplicates != 1)
                throw new InvalidDataException($"同名の Excel が複数あります: {name}");

            var candidate = (MasterDataAsset)ScriptableObject.CreateInstance(matches[0]);
            try
            {
                var json = ExcelMasterReader.Read(path, candidate.RowType).ToString(Formatting.Indented);
                // JSON を読み直した値を ScriptableObject へ入力します。
                var rows = (Array)JObject.Parse(json)["Rows"].ToObject(candidate.RowType.MakeArrayType());
                candidate.ImportRows(rows);

                var assetPath = $"{AssetDirectory}/{name}.asset";
                var jsonPath = $"{JsonDirectory}/{name}.json";
                var existing = AssetDatabase.LoadMainAssetAtPath(assetPath);
                if (existing != null && existing.GetType() != matches[0])
                    throw new InvalidDataException($"{assetPath} に異なる型のアセットがあります。");

                EnsureFolder(JsonDirectory);
                EnsureFolder(AssetDirectory);
                if (!File.Exists(jsonPath) || File.ReadAllText(jsonPath) != json)
                    File.WriteAllText(jsonPath, json, new UTF8Encoding(false));
                AssetDatabase.ImportAsset(jsonPath);

                MasterDataAsset result;
                if (existing == null)
                {
                    result = candidate;
                    AssetDatabase.CreateAsset(result, assetPath);
                    candidate = null;
                }
                else
                {
                    result = (MasterDataAsset)existing;
                    Undo.RecordObject(result, "Import master data");
                    result.ImportRows(rows);
                    EditorUtility.SetDirty(result);
                }
                AssetDatabase.SaveAssetIfDirty(result);
                return result;
            }
            finally
            {
                if (candidate != null)
                    UnityEngine.Object.DestroyImmediate(candidate);
            }
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var parent = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var current = parent + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(current))
                    AssetDatabase.CreateFolder(parent, parts[i]);
                parent = current;
            }
        }
    }

    public class MasterDataExcelPostprocessor : AssetPostprocessor
    {
        private static readonly HashSet<string> Pending = new HashSet<string>(StringComparer.Ordinal);
        private static bool _scheduled;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            foreach (var path in importedAssets.Concat(movedAssets).Where(MasterDataImporter.IsSource))
                Pending.Add(path);

            if (didDomainReload && Directory.Exists(MasterDataImporter.SourceDirectory))
            {
                foreach (var path in Directory.GetFiles(MasterDataImporter.SourceDirectory, "*.xlsx", SearchOption.AllDirectories)
                             .Where(MasterDataImporter.IsSource))
                {
                    if (!File.Exists($"{MasterDataImporter.AssetDirectory}/{Path.GetFileNameWithoutExtension(path)}.asset"))
                        Pending.Add(path.Replace('\\', '/'));
                }
            }
            Schedule();
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                Schedule();
        }

        private static void Schedule()
        {
            if (_scheduled || Pending.Count == 0 || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            _scheduled = true;
            EditorApplication.delayCall += Process;
        }

        private static void Process()
        {
            _scheduled = false;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            var paths = Pending.ToArray();
            Pending.Clear();
            foreach (var path in paths.Where(File.Exists))
                MasterDataImporter.TryImport(path);
        }
    }
}
