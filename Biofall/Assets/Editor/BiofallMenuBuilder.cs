using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Biofall.UI;

namespace Biofall.EditorTools
{
    // Regenerates the main menu from code, the way Office regenerates its boot scene and player
    // prefab. Hand-tweaking the result is fine; re-running this replaces it.
    public static class BiofallMenuBuilder
    {
        private static readonly Color Red = new(1f, 0.16f, 0.16f, 1f);
        private static readonly Color Idle = new(0.72f, 0.72f, 0.74f, 1f);
        private static readonly Color Dim = new(0.55f, 0.56f, 0.58f, 1f);

        private const float RowHeight = 68f;
        private const float FirstRowY = -430f;
        private const float LeftMargin = 110f;

        [MenuItem("Biofall/Setup/Build Main Menu")]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);

            // Retire the old menu instead of deleting it: it stays in the scene, switched off.
            foreach (var old in Object.FindObjectsByType<MainMenuUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                old.transform.root.gameObject.SetActive(false);

            foreach (var prev in Object.FindObjectsByType<MainMenuScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(prev.transform.root.gameObject);

            var canvasGo = new GameObject("[MainMenu]", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            Transform root = canvasGo.transform;

            var bg = NewImage("Background", root);
            bg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Images/IMG_Background.png");
            bg.color = Color.white;
            Stretch(bg.rectTransform);

            var scrim = NewImage("Scrim", root);
            scrim.color = new Color(0f, 0f, 0f, 0.22f);
            scrim.raycastTarget = false;
            Stretch(scrim.rectTransform);

            var screen = canvasGo.AddComponent<MainMenuScreen>();
            var so = new SerializedObject(screen);

            BuildRootPanel(root, so);
            BuildCoopPanel(root, so);
            BuildLobbyPanel(root, so);

            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[Biofall] Main menu rebuilt.");
        }

        private static void BuildRootPanel(Transform root, SerializedObject so)
        {
            Transform panel = NewPanel("RootPanel", root);
            so.FindProperty("rootPanel").objectReferenceValue = panel.gameObject;

            string[] labels = { "SINGLE PLAYER", "COOP", "WAVE MODE", "SETTINGS", "CREDITS", "EXIT" };
            string[] fields = { "singlePlayerButton", "coopButton", "waveModeButton",
                                "settingsButton", "creditsButton", "exitButton" };

            for (int i = 0; i < labels.Length; i++)
                so.FindProperty(fields[i]).objectReferenceValue = MenuRow(panel, labels[i], i);
        }

        private static void BuildCoopPanel(Transform root, SerializedObject so)
        {
            Transform panel = NewPanel("CoopPanel", root);
            panel.gameObject.SetActive(false);
            so.FindProperty("coopPanel").objectReferenceValue = panel.gameObject;

            Heading(panel, "CO-OP");

            so.FindProperty("hostButton").objectReferenceValue = MenuRow(panel, "HOST SQUAD", 0);
            so.FindProperty("joinButton").objectReferenceValue = MenuRow(panel, "JOIN BY CODE", 1);

            var input = NewInput("JoinCodeField", panel, 2);
            so.FindProperty("joinCodeField").objectReferenceValue = input;

            so.FindProperty("coopBackButton").objectReferenceValue = MenuRow(panel, "BACK", 3);

            var status = NewText("Status", panel, string.Empty, 20f);
            status.color = Red;
            status.alignment = TextAlignmentOptions.MidlineLeft;
            PlaceRow(status.rectTransform, 4, 700f);
            so.FindProperty("statusLabel").objectReferenceValue = status;
        }

        private static void BuildLobbyPanel(Transform root, SerializedObject so)
        {
            Transform panel = NewPanel("LobbyPanel", root);
            panel.gameObject.SetActive(false);
            so.FindProperty("lobbyPanel").objectReferenceValue = panel.gameObject;

            Heading(panel, "SQUAD");

            var code = NewText("JoinCode", panel, "CODE —", 24f);
            code.color = Red;
            code.alignment = TextAlignmentOptions.MidlineLeft;
            code.characterSpacing = 8f;
            PlaceRow(code.rectTransform, -1, 700f);
            so.FindProperty("joinCodeLabel").objectReferenceValue = code;

            var slots = so.FindProperty("slotLabels");
            slots.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                var slot = NewText("Slot" + i, panel, "— EMPTY —", 22f);
                slot.color = Dim;
                slot.alignment = TextAlignmentOptions.MidlineLeft;
                slot.characterSpacing = 6f;
                PlaceRow(slot.rectTransform, i, 700f);
                slots.GetArrayElementAtIndex(i).objectReferenceValue = slot;
            }

            Button ready = MenuRow(panel, "READY", 5);
            so.FindProperty("readyButton").objectReferenceValue = ready;
            so.FindProperty("readyButtonLabel").objectReferenceValue =
                ready.transform.Find("Label").GetComponent<TMP_Text>();

            so.FindProperty("startButton").objectReferenceValue = MenuRow(panel, "START RUN", 6);
            so.FindProperty("leaveButton").objectReferenceValue = MenuRow(panel, "LEAVE", 7);
        }

        private static void Heading(Transform parent, string text)
        {
            var heading = NewText("Heading", parent, text, 44f);
            heading.color = Red;
            heading.alignment = TextAlignmentOptions.MidlineLeft;
            heading.characterSpacing = 16f;
            PlaceRow(heading.rectTransform, -2, 700f);
        }

        private static Button MenuRow(Transform parent, string text, int index)
        {
            var go = new GameObject(text.Replace(" ", "") + "Row",
                typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            PlaceRow((RectTransform)go.transform, index, 430f);

            var hit = go.GetComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0.01f);   // invisible, still raycastable

            var frame = NewImage("Frame", go.transform);
            frame.color = new Color(1f, 0.16f, 0.16f, 0.10f);
            frame.raycastTarget = false;
            frame.enabled = false;
            Stretch(frame.rectTransform);
            var outline = frame.gameObject.AddComponent<Outline>();
            outline.effectColor = Red;
            outline.effectDistance = new Vector2(1.5f, 1.5f);

            var tick = NewImage("Tick", go.transform);
            tick.color = new Color(1f, 1f, 1f, 0.16f);
            tick.raycastTarget = false;
            var trt = tick.rectTransform;
            trt.anchorMin = new Vector2(0f, 0.18f);
            trt.anchorMax = new Vector2(0f, 0.82f);
            trt.pivot = new Vector2(0f, 0.5f);
            trt.anchoredPosition = Vector2.zero;
            trt.sizeDelta = new Vector2(2f, 0f);

            var marker = NewText("Marker", go.transform, "▶", 20f);
            marker.color = Red;
            marker.alignment = TextAlignmentOptions.MidlineLeft;
            var mrt = marker.rectTransform;
            mrt.anchorMin = new Vector2(0f, 0f);
            mrt.anchorMax = new Vector2(0f, 1f);
            mrt.pivot = new Vector2(0f, 0.5f);
            mrt.sizeDelta = new Vector2(40f, 0f);
            mrt.anchoredPosition = new Vector2(16f, 0f);
            marker.enabled = false;

            var label = NewText("Label", go.transform, text, 30f);
            label.color = Idle;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.characterSpacing = 12f;
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(52f, 0f);
            label.rectTransform.offsetMax = new Vector2(-12f, 0f);

            var visual = go.AddComponent<MenuItemVisual>();
            var vso = new SerializedObject(visual);
            vso.FindProperty("label").objectReferenceValue = label;
            vso.FindProperty("frame").objectReferenceValue = frame;
            vso.FindProperty("marker").objectReferenceValue = marker;
            vso.ApplyModifiedPropertiesWithoutUndo();

            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            return button;
        }

        private static TMP_InputField NewInput(string name, Transform parent, int index)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            PlaceRow((RectTransform)go.transform, index, 430f);

            var back = go.GetComponent<Image>();
            back.color = new Color(1f, 1f, 1f, 0.06f);

            var text = NewText("Text", go.transform, string.Empty, 26f);
            text.color = Red;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.characterSpacing = 10f;
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(18f, 0f);
            text.rectTransform.offsetMax = new Vector2(-12f, 0f);

            var placeholder = NewText("Placeholder", go.transform, "JOIN CODE", 24f);
            placeholder.color = Dim;
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            placeholder.characterSpacing = 10f;
            Stretch(placeholder.rectTransform);
            placeholder.rectTransform.offsetMin = new Vector2(18f, 0f);
            placeholder.rectTransform.offsetMax = new Vector2(-12f, 0f);

            var input = go.AddComponent<TMP_InputField>();
            input.textViewport = (RectTransform)text.transform.parent;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.characterLimit = 12;
            input.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
            return input;
        }

        private static void PlaceRow(RectTransform rt, int index, float width)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(width, 64f);
            rt.anchoredPosition = new Vector2(LeftMargin, FirstRowY - index * RowHeight);
        }

        private static Transform NewPanel(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform);
            return go.transform;
        }

        private static Image NewImage(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            return go.GetComponent<Image>();
        }

        private static TMP_Text NewText(string name, Transform parent, string content, float size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = content;
            t.fontSize = size;
            t.raycastTarget = false;
            return t;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
