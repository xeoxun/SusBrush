using System;
using UnityEngine;

namespace SusBrush.Drawing
{
    [CreateAssetMenu(menuName = "SusBrush/Pen Palette")]
    public sealed class PenPalette : ScriptableObject
    {
        [Serializable]
        private struct Pen
        {
            public string name;
            public LineRenderer strokePrefab;
        }

        [SerializeField] private Pen[] pens = new Pen[8];
        public int Count => pens.Length;

        public bool TryGetStroke(int index, out LineRenderer stroke)
        {
            stroke = index >= 0 && index < pens.Length ? pens[index].strokePrefab : null;
            return stroke != null;
        }
    }
}
