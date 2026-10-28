using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SusBrush.Editor
{
    public static class RoomEntryLoadingBuilder
    {
        [MenuItem("SusBrush/Build Room Entry Loading")]
        public static void Build()
        {
            var roles = new[] { "Mafia", "Citizen", "Guard", "Eccentric" };
            var sprites = new Sprite[4][];
            for (var i = 0; i < roles.Length; i++)
            {
                sprites[i] = new Sprite[3];
                for (var f = 0; f < 3; f++)
                {
                    var path = $"Assets/Sprites/UI/RoomEntryLoading/{roles[i]}_{f}.png";
                    AssetDatabase.ImportAsset(path);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 100;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = true;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                    sprites[i][f] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
            }

            var root = new GameObject("RoomEntryLoading", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            try
            {
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 32000;
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = .5f;
                var background = Image("Background", root.transform, new Vector2(1920, 1080), Vector2.zero,
                    new Color(1, .975f, .92f));
                background.rectTransform.anchorMin = Vector2.zero;
                background.rectTransform.anchorMax = Vector2.one;
                background.rectTransform.sizeDelta = Vector2.zero;
                background.raycastTarget = true;

                var group = root.GetComponent<CanvasGroup>();
                group.blocksRaycasts = true;
                group.interactable = false;
                var view = root.AddComponent<SusBrush.UI.RoomEntryLoading>();
                var so = new SerializedObject(view);
                so.FindProperty("group").objectReferenceValue = group;
                var walkers = so.FindProperty("walkers");
                walkers.arraySize = roles.Length;
                for (var i = 0; i < roles.Length; i++)
                {
                    var image = Image(roles[i], root.transform, new Vector2(270, 300),
                        new Vector2(-450 + i * 300, 90), Color.white);
                    image.sprite = sprites[i][0];
                    image.preserveAspect = true;
                    var walker = walkers.GetArrayElementAtIndex(i);
                    walker.FindPropertyRelative("image").objectReferenceValue = image;
                    var frames = walker.FindPropertyRelative("frames");
                    frames.arraySize = 3;
                    for (var f = 0; f < 3; f++) frames.GetArrayElementAtIndex(f).objectReferenceValue = sprites[i][f];
                }

                var labelGO = new GameObject("LoadingMessage", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelGO.transform.SetParent(root.transform, false);
                var label = labelGO.GetComponent<TextMeshProUGUI>();
                label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Maplestory Bold SDF.asset");
                label.text = "그림 그리러 가는 중..";
                label.fontSize = 48;
                label.color = new Color(.24f, .12f, .075f);
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;
                label.rectTransform.sizeDelta = new Vector2(1200, 90);
                label.rectTransform.anchoredPosition = new Vector2(0, -150);
                var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                var track = Image("ProgressTrack", root.transform, new Vector2(760, 32),
                    new Vector2(0, -250), new Color(.86f, .79f, .67f));
                track.sprite = sprite;
                track.type = UnityEngine.UI.Image.Type.Sliced;
                var outline = track.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(.24f, .12f, .075f);
                outline.effectDistance = new Vector2(3, -3);
                var fill = Image("ProgressFill", track.transform, new Vector2(748, 20), Vector2.zero,
                    new Color(1, .34f, .3f));
                fill.sprite = sprite;
                fill.type = UnityEngine.UI.Image.Type.Filled;
                fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
                fill.fillOrigin = 0;
                fill.fillAmount = 0;
                so.FindProperty("progressFill").objectReferenceValue = fill;
                so.ApplyModifiedPropertiesWithoutUndo();
                Directory.CreateDirectory("Assets/Resources/UI");
                PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/UI/RoomEntryLoading.prefab");
                AssetDatabase.SaveAssets();
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static UnityEngine.UI.Image Image(string name, Transform parent, Vector2 size, Vector2 position, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = false;
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(.5f, .5f);
            image.rectTransform.sizeDelta = size;
            image.rectTransform.anchoredPosition = position;
            return image;
        }
    }
}
