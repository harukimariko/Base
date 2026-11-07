using System;
using System.Collections.Generic;
using UnityEngine;

namespace Base
{
    public abstract class MasterDataAsset : ScriptableObject
    {
        public abstract Type RowType { get; }
        public abstract int RowCount { get; }
        public abstract void ValidateRows(Array rows);
        public abstract void ImportRows(Array rows);
    }

    public abstract class MasterDataTable<TRow> : MasterDataAsset
    {
        [SerializeField] private TRow[] _rows = Array.Empty<TRow>();

        public IReadOnlyList<TRow> Rows => _rows;
        public override Type RowType => typeof(TRow);
        public override int RowCount => _rows.Length;

        public override void ValidateRows(Array rows)
        {
            if (!(rows is TRow[]))
                throw new ArgumentException($"{GetType().Name} requires {typeof(TRow).Name}[]");
        }

        public override void ImportRows(Array rows)
        {
            ValidateRows(rows);
            _rows = (TRow[])rows;
            OnDataChanged();
        }

        protected virtual void OnEnable() => OnDataChanged();
        protected virtual void OnValidate() => OnDataChanged();
        protected virtual void OnDataChanged() { }
    }
}
