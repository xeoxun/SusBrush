using UnityEngine;

namespace SusBrush.UI
{
    public sealed class ButtonSound : MonoBehaviour
    {
        [SerializeField] private bool mainButton;
        // Connected in Button.onClick, including dynamically instantiated room-list prefabs.
        public void Play() { if (SceneAudio.Instance != null) SceneAudio.Instance.PlayButton(mainButton); }
    }
}
