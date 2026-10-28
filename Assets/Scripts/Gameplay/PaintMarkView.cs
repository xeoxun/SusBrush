using TMPro;
using UnityEngine;
namespace SusBrush.Gameplay
{
    public sealed class PaintMarkView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer fill;
        [SerializeField] private TMP_Text label;
        public void Show(Color color, string name) { fill.color = color; label.text = name.Replace("<", "＜").Replace(">", "＞") + "의 자국"; }
    }
}
