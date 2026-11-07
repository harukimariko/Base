using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace Base.Editor
{
    public static class ExcelMasterReader
    {
        private static readonly XNamespace Spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly XNamespace Relationship = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        public static JObject Read(string path, Type rowType)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            var shared = archive.GetEntry("xl/sharedStrings.xml") == null
                ? Array.Empty<string>()
                : ReadXml(archive, "xl/sharedStrings.xml").Root.Elements(Spreadsheet + "si").Select(ReadText).ToArray();
            var workbook = ReadXml(archive, "xl/workbook.xml");
            var sheets = workbook.Root.Element(Spreadsheet + "sheets").Elements(Spreadsheet + "sheet")
                .Where(s => (string)s.Attribute("state") != "hidden" && (string)s.Attribute("state") != "veryHidden").ToArray();
            if (sheets.Length == 0)
                throw new InvalidDataException("表示されているワークシートがありません。");

            var relationId = (string)sheets[0].Attribute(Relationship + "id");
            var relations = ReadXml(archive, "xl/_rels/workbook.xml.rels");
            var relation = relations.Root.Elements().Single(r => (string)r.Attribute("Id") == relationId);
            if ((string)relation.Attribute("TargetMode") == "External")
                throw new InvalidDataException("外部ワークシートは使用できません。");
            var target = (string)relation.Attribute("Target");
            var sheetPath = new Uri(new Uri("http://xlsx/xl/workbook.xml"), target).AbsolutePath.TrimStart('/');
            var rows = ReadXml(archive, sheetPath).Descendants(Spreadsheet + "row")
                .ToDictionary(r => (int)r.Attribute("r"), r => ReadCells(r, shared));
            if (!rows.TryGetValue(4, out var headers) || !rows.TryGetValue(5, out var types))
                throw new InvalidDataException("4行目に変数名、5行目に型名を設定してください。");

            var fields = rowType.GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Where(f => !f.IsDefined(typeof(NonSerializedAttribute), true)).ToDictionary(f => f.Name, StringComparer.Ordinal);
            var columns = new Dictionary<int, FieldInfo>();
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var cell in headers.OrderBy(c => c.Key))
            {
                var name = cell.Value.Trim();
                if (name.Length == 0 || name.StartsWith("#", StringComparison.Ordinal))
                    continue;
                if (!fields.TryGetValue(name, out var field))
                    throw CellError(cell.Key, 4, $"{rowType.Name} に変数 '{name}' がありません。");
                if (!names.Add(name))
                    throw CellError(cell.Key, 4, $"変数名 '{name}' が重複しています。");
                if (!types.TryGetValue(cell.Key, out var typeName) || !MatchesType(typeName.Trim(), field.FieldType))
                    throw CellError(cell.Key, 5, $"'{name}' の型名は {GetTypeName(field.FieldType)} にしてください。");
                columns.Add(cell.Key, field);
            }

            if (columns.Count == 0)
                throw new InvalidDataException("4行目に有効な変数名がありません。");
            var missing = fields.Keys.Where(name => !names.Contains(name)).ToArray();
            if (missing.Length != 0)
                throw new InvalidDataException("4行目に必要な変数名がありません: " + string.Join(", ", missing));

            var records = new JArray();
            foreach (var row in rows.Where(r => r.Key >= 6).OrderBy(r => r.Key))
            {
                if (row.Value.Values.All(string.IsNullOrWhiteSpace))
                    continue;
                var first = row.Value.OrderBy(c => c.Key).FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.Value)).Value;
                if (first != null && first.StartsWith("#", StringComparison.Ordinal))
                    continue;

                var record = new JObject();
                foreach (var column in columns)
                {
                    row.Value.TryGetValue(column.Key, out var value);
                    try
                    {
                        record[column.Value.Name] = ParseValue(value ?? "", column.Value.FieldType);
                    }
                    catch (Exception exception)
                    {
                        throw CellError(column.Key, row.Key, $"'{column.Value.Name}' を {GetTypeName(column.Value.FieldType)} に変換できません: {exception.Message}");
                    }
                }
                records.Add(record);
            }
            return new JObject { ["Rows"] = records };
        }

        private static Dictionary<int, string> ReadCells(XElement row, string[] shared)
        {
            var cells = new Dictionary<int, string>();
            foreach (var cell in row.Elements(Spreadsheet + "c"))
            {
                var address = (string)cell.Attribute("r");
                var column = 0;
                foreach (var character in address.TakeWhile(char.IsLetter))
                    column = checked(column * 26 + char.ToUpperInvariant(character) - 'A' + 1);
                var type = (string)cell.Attribute("t");
                var value = cell.Element(Spreadsheet + "v");
                if (type == "e")
                    throw new InvalidDataException($"{address}: Excel の数式エラーがあります: {value?.Value}");
                if (cell.Element(Spreadsheet + "f") != null && value == null)
                    throw new InvalidDataException($"{address}: 数式の計算結果がありません。Excel で再計算して保存してください。");
                cells[column] = type == "s" ? shared[int.Parse(value.Value, CultureInfo.InvariantCulture)]
                    : type == "inlineStr" ? ReadText(cell.Element(Spreadsheet + "is"))
                    : value?.Value ?? "";
            }
            return cells;
        }

        // rPh 内のふりがなは翻訳文字列に混ぜません。
        private static string ReadText(XElement element) => element == null ? "" : string.Concat(
            element.Descendants(Spreadsheet + "t").Where(t => !t.Ancestors(Spreadsheet + "rPh").Any()).Select(t => t.Value));

        private static JToken ParseValue(string value, Type type)
        {
            if (type == typeof(string))
                return new JValue(value);
            var nullable = Nullable.GetUnderlyingType(type);
            if (nullable != null && string.IsNullOrWhiteSpace(value))
                return JValue.CreateNull();
            type = nullable ?? type;
            if (type == typeof(bool))
                return new JValue(value == "1" || (value != "0" && bool.Parse(value)));
            if (type.IsEnum)
            {
                var parsed = Enum.Parse(type, value, false);
                if (!Enum.IsDefined(type, parsed))
                    throw new FormatException("定義されていない enum 値です。");
                return JToken.FromObject(parsed);
            }
            if (type.IsPrimitive || type == typeof(decimal))
            {
                var parsed = Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
                if (parsed is float f && (float.IsNaN(f) || float.IsInfinity(f)) ||
                    parsed is double d && (double.IsNaN(d) || double.IsInfinity(d)))
                    throw new FormatException("有限の数値を指定してください。");
                return JToken.FromObject(parsed);
            }
            var token = JToken.Parse(value);
            token.ToObject(type);
            return token;
        }

        private static bool MatchesType(string name, Type type) =>
            string.Equals(name, GetTypeName(type), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, type.Name, StringComparison.OrdinalIgnoreCase) ||
            name == "json" && !type.IsPrimitive && type != typeof(string) && !type.IsEnum;

        private static string GetTypeName(Type type)
        {
            var nullable = Nullable.GetUnderlyingType(type);
            if (nullable != null) return GetTypeName(nullable) + "?";
            if (type == typeof(string)) return "string";
            if (type == typeof(int)) return "int";
            if (type == typeof(long)) return "long";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(decimal)) return "decimal";
            if (type == typeof(bool)) return "bool";
            return type.Name;
        }

        private static XDocument ReadXml(ZipArchive archive, string path)
        {
            var entry = archive.GetEntry(path) ?? throw new InvalidDataException($"Excel 内に {path} がありません。");
            using var stream = entry.Open();
            using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
            return XDocument.Load(reader);
        }

        private static Exception CellError(int column, int row, string message)
        {
            var letters = "";
            while (column > 0) { column--; letters = (char)('A' + column % 26) + letters; column /= 26; }
            return new InvalidDataException($"{letters}{row}: {message}");
        }
    }
}
