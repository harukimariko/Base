# Excel マスターデータ

## 変換

- 入力: `Assets/Master/Excel/<クラス名>.xlsx`
- JSON: `Assets/Master/Json/<クラス名>.json`
- ScriptableObject: `Assets/Resources/MasterData/<クラス名>.asset`
- Excel を保存・再インポートすると自動変換します。
- 手動変換: `Tools > マスターデータ > すべて変換`、または Excel を右クリックして `マスターデータに変換`。
- 既存アセットの GUID を維持して更新します。変換エラー時は既存の JSON / アセットを更新しません。

## Excel の形式

- 最初の表示ワークシートを使用します。
- **4 行目: C# の public フィールド名。5 行目: 型名。6 行目以降: データ。**
- 列は名前で照合するため、順番を変更できます。
- `string`, `int`, `long`, `float`, `double`, `decimal`, `bool`、enum の型名を使用できます。
- 配列や複合型の列は型名を `json` にして、セルに JSON を記述します。
- 文字列以外の数値は英語形式（小数点 `.`）で入力します。bool は `true` / `false` / `1` / `0`。
- 空行、最初の値が `#` で始まる行、変数名が `#` で始まる列は無視します。
- 型名の不一致、値の変換失敗、未知の列・不足した列はエラーになります。
- 数式は Excel が保存した計算結果を読みます。Excel で再計算・保存してください。
- ID は Excel でも文字列として入力してください。書式によるゼロ埋めは適用しません。

## マスターの追加例

`ItemIndex.xlsx` の4行目を `ID`, `Price`, `Weight`、5行目を `string`, `int`, `float` にします。

```csharp
using System;
using UnityEngine;

namespace Base
{
    [CreateAssetMenu(menuName = "Master Data/Item Index")]
    public class ItemIndex : MasterDataTable<ItemIndex.ItemRow>
    {
        [Serializable]
        public class ItemRow
        {
            public string ID;
            public int Price;
            public float Weight;
        }
    }
}
```

マスター固有の検証は `ValidateRows(Array rows)` を override して追加できます。

## ローカライズ

TMP_Text と同じ GameObject に `LocalizationText` を追加します。対象の TMP_Text は自動取得します。
TMP_Text の初期テキストを `ui_title_startbutton` などの LocalizationID にします。
Index は `Assets/Resources/MasterData/LocalizationIndex.asset` から自動取得します。アタッチは不要です。
ID の個別入力欄はなく、TMP_Text の最初の text を照合に使います。翻訳後や再有効化時も最初の ID を維持します。

```csharp
SaveDataMaster.Instance.LanguageData.Language.Value = LanguageData.LanguageType.English;
SaveDataMaster.Instance.Save(); // 言語の永続化は明示的に実行
```

言語変更は有効な LocalizationText に即時反映されます。無効化中は購読を解除し、再有効化時に現在の言語で更新します。
未登録 ID は元の表示を維持して警告します。英語が空欄なら日本語へフォールバックします。

日本語フォントは `Assets/Fonts/NotoSansJP/NotoSansCJKjp-Regular SDF.asset` を使用します。
TMP Settings のフォールバックにも登録済みです。Dynamic / Multi Atlas のため、追加した日本語も字形を生成できます。
タイトルのラベルには Auto Size を設定し、言語による文字サイズ・行高の違いを枠内に収めています。
