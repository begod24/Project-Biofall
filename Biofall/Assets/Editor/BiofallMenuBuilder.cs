using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Biofall.UI;

namespace Biofall.EditorTools
{
    // Regenerates the whole main menu from code, the way Office regenerates its boot scene and
    // player prefab. Hand-tweaking the result is fine; re-running this replaces it.
    public static class BiofallMenuBuilder
    {
        private static readonly Color Red = new(1f, 0.16f, 0.16f, 1f);
        private static readonly Color Idle = new(0.72f, 0.72f, 0.74f, 1f);
        private static readonly Color Dim = new(0.55f, 0.56f, 0.58f, 1f);
        private static readonly Color Faint = new(1f, 1f, 1f, 0.16f);

        private const float RowHeight = 68f;
        private const float FirstRowY = -430f;
        private const float LeftMargin = 110f;
        private const float RowWidth = 430f;

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

            BuildRoot(root, so);
            BuildSingle(root, so);
            BuildCoop(root, so);
            BuildConnect(root, so);
            BuildOperatives(root, so);
            BuildLobby(root, so);
            BuildSettings(root, so);
            BuildCredits(root, so);
            BuildStatus(root, so);

            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[Biofall] Main menu rebuilt: 8 panels.");
        }

        // ---- panels ---------------------------------------------------------------------------

        private static void BuildRoot(Transform root, SerializedObject so)
        {
            Transform panel = NewPanel("RootPanel", root, so, "rootPanel", active: true);

            string[] labels = { "SINGLE PLAYER", "COOP", "WAVE MODE", "SETTINGS", "CREDITS", "EXIT" };
            string[] fields = { "singlePlayerButton", "coopButton", "waveModeButton",
                                "settingsButton", "creditsButton", "exitButton" };

            for (int i = 0; i < labels.Length; i++)
                so.FindProperty(fields[i]).objectReferenceValue = Row(panel, labels[i], i);
        }

        private static void BuildSingle(Transform root, SerializedObject so)
        {
            Transform panel = NewPanel("SinglePanel", root, so, "singlePanel", active: false);
            Heading(panel, "SINGLE PLAYER");

            so.FindProperty("newGameButton").objectReferenceValue = Row(panel, "NEW GAME", 0);
            so.FindProperty("continueButton").objectReferenceValue = Row(panel, "CONTINUE", 1);
            so.FindProperty("singleBackButton").objectReferenceValue = Row(panel, "BACK", 3);
        }

        private static void BuildCoop(Transform root, SerializedObject so)
        {
            Transform panel = NewPanel("CoopPanel", root, so, "coopPanel", active: false);
            Heading(panel, "CO-OP");

            so.FindProperty("hostGameButton").objectReferenceValue = Row(panel, "HOST A GAME", 0);
            so.FindProperty("coopContinueButton").objectReferenceValue = Row(panel, "CONTINUE", 1);
            so.FindProperty("connectButton").objectReferenceValue = Row(panel, "CONNECT", 2);
            so.FindProperty("coopBackButton").objectReferenceValue = Row(panel, "BACK", 4);
        }

        private static void BuildConnect(Transform root, SerializedObject so)
        {
            Transform panel = NewPanel("ConnectPanel", root, so, "connectPanel", active: false);
            Heading(panel, "CONNECT");

            Caption(panel, "ENTER THE CODE THE HOST GAVE YOU", -1);

            so.FindProperty("joinCodeField").objectReferenceValue = CodeField(panel, 0);
            so.FindProperty("joinButton").objectReferenceValue = Row(panel, "JOIN", 1);
            so.FindProperty("connectBackButton").objectReferenceValue = Row(panel, "BACK", 3);
        }

        private static void BuildOperatives(Transform root, SerializedObject so)
        {
            Transform panel = NewPanel("OperativePanel", root, so, "operativePanel", active: false);
            Heading(panel, "SELECT OPERATIVE");

            var buttons = so.FindProperty("operativeButtons");
            var names = so.FindProperty("operativeNames");
            var descs = so.FindProperty("operativeDescriptions");
            var frames = so.FindProperty("operativeFrames");

            buttons.arraySize = names.arraySize = descs.arraySize = frames.arraySize = 4;

            for (int i = 0; i < 4; i++)
            {
                Button card = OperativeCard(panel, i, out TMP_Text name, out TMP_Text desc, out Graphic frame);

                buttons.GetArrayElementAtIndex(i).objectReferenceValue = card;
                names.GetArrayElementAtIndex(i).objectReferenceValue = name;
                descs.GetArrayElementAtIndex(i).objectReferenceValue = desc;
                frames.GetArrayElementAtIndex(i).objectReferenceValue = frame;
            }

            so.FindProperty("deployButton").objectReferenceValue = Row(panel, "DEPLOY", 4);
            so.FindProperty("operativeBackButton").objectReferenceValue = Row(panel, "BACK", 5);
        }

        private static void BuildLobby(Transform root, SerializedObject so)
        {
            Transform panel = NewPanel("LobbyPanel", root, so, "lobbyPanel", active: false);
            Heading(panel, "SQUAD");

            TMP_Text code = Label(panel, "JoinCode", "CODE  —", 26f, Red, -1, 700f);
            code.characterSpacing = 10f;
            so.FindProperty("joinCodeLabel").objectReferenceValue = code;

            var slots = so.FindProperty("slotLabels");
            slots.arraySize = 4;
            for (int i = 0; i < 4; i++)
                slots.GetArrayElementAtIndex(i).objectReferenceValue =
                    Label(panel, "Slot" + i, "— EMPTY —", 22f, Dim, i, 700f);

            Button ready = Row(panel, "READY", 5);
            so.FindProperty("readyButton").objectReferenceValue = ready;
            so.FindProperty("readyButtonLabel").objectReferenceValue =
                ready.transform.Find("Label").GetComponent<TMP_Text>();

            so.FindProperty("startButton").objectReferenceValue = Row(panel, "START RUN", 6);
            so.FindProperty("leaveButton").objectReferenceValue = Row(panel, "LEAVE", 7);
        }

        private static void BuildSettings(Transform root, SerializedObject so)
        {
            Transform panel = NewPanel("SettingsPanel", root, so, "settingsPanel", active: false);
            Heading(panel, "SETTINGS");

            so.FindProperty("masterVolumeSlider").objectReferenceValue = SliderRow(panel, "MASTER VOLUME", 0);
            so.FindProperty("musicVolumeSlider").objectReferenceValue = SliderRow(panel, "MUSIC VOLUME", 1);
            so.FindProperty("shakeSlider").objectReferenceValue = SliderRow(panel, "CAMERA SHAKE", 2);
            so.FindProperty("settingsBackButton").objectReferenceValue = Row(panel, "BACK", 4);
        }

        private static void BuildCredits(Transform root, SerializedObject so)
        {
            Transform panel = NewPanel("CreditsPanel", root, so, "creditsPanel", active: false);
            Heading(panel, "CREDITS");

            Caption(panel, "BIOFALL", 0);
            Caption(panel, "DESIGN, CODE AND ART", 1);
            TMP_Text author = Label(panel, "Author", "BEKBOLAT ALDIYAROV", 30f, Red, 2, 700f);
            author.characterSpacing = 10f;
            Caption(panel, "MADE WITH UNITY 6", 4);

            so.FindProperty("creditsBackButton").objectReferenceValue = Row(panel, "BACK", 6);
        }

        private static void BuildStatus(Transform root, SerializedObject so)
        {
            var status = NewText("Status", root, string.Empty, 20f);
            status.color = Red;
            status.alignment = TextAlignmentOptions.BottomLeft;
            status.characterSpacing = 6f;

            var rt = status.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(900f, 40f);
            rt.anchoredPosition = new Vector2(LeftMargin, 70f);

            so.FindProperty("statusLabel").objectReferenceValue = status;
        }

        // ---- pieces ---------------------------------------------------------------------------

        private static Button Row(Transform parent, string text, int index)
        {
            var go = new GameObject(text.Replace(" ", "") + "Row",
                typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, index, RowWidth);

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
            tick.color = Faint;
            tick.raycastTarget = false;
            var trt = tick.rectTransform;
            trt.anchorMin = new Vector2(0f, 0.18f);
            trt.anchorMax = new Vector2(0f, 0.82f);
            trt.pivot = new Vector2(0f, 0.5f);
            trt.anchoredPosition = Vector2.zero;
            trt.sizeDelta = new Vector2(2f, 0f);

            // LiberationSans (the default TMP font) has no U+25B6, so it would draw as a box.
            // Swap this for "▶" once a font asset with the glyph is assigned.
            var marker = NewText("Marker", go.transform, ">", 24f);
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

        private static Button OperativeCard(Transform parent, int index,
            out TMP_Text name, out TMP_Text description, out Graphic frame)
        {
            var go = new GameObject("Operative" + index,
                typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(330f, 190f);
            rt.anchoredPosition = new Vector2(LeftMargin + index * 350f, FirstRowY);

            var back = go.GetComponent<Image>();
            back.color = new Color(1f, 1f, 1f, 0.05f);

            var selected = NewImage("SelectedFrame", go.transform);
            selected.color = new Color(1f, 0.16f, 0.16f, 0.10f);
            selected.raycastTarget = false;
            Stretch(selected.rectTransform);
            var outline = selected.gameObject.AddComponent<Outline>();
            outline.effectColor = Red;
            outline.effectDistance = new Vector2(2f, 2f);
            selected.enabled = false;
            frame = selected;

            var hoverFrame = NewImage("HoverFrame", go.transform);
            hoverFrame.color = new Color(1f, 1f, 1f, 0.05f);
            hoverFrame.raycastTarget = false;
            hoverFrame.enabled = false;
            Stretch(hoverFrame.rectTransform);

            name = NewText("Name", go.transform, "—", 28f);
            name.color = Idle;
            name.alignment = TextAlignmentOptions.TopLeft;
            name.characterSpacing = 10f;
            Stretch(name.rectTransform);
            name.rectTransform.offsetMin = new Vector2(20f, 120f);
            name.rectTransform.offsetMax = new Vector2(-16f, -20f);

            description = NewText("Description", go.transform, string.Empty, 17f);
            description.color = Dim;
            description.alignment = TextAlignmentOptions.TopLeft;
            Stretch(description.rectTransform);
            description.rectTransform.offsetMin = new Vector2(20f, 18f);
            description.rectTransform.offsetMax = new Vector2(-16f, -66f);

            var visual = go.AddComponent<MenuItemVisual>();
            var vso = new SerializedObject(visual);
            vso.FindProperty("label").objectReferenceValue = name;
            vso.FindProperty("frame").objectReferenceValue = hoverFrame;
            vso.ApplyModifiedPropertiesWithoutUndo();

            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            return button;
        }

        private static Slider SliderRow(Transform parent, string caption, int index)
        {
            Label(parent, caption + "Caption", caption, 20f, Dim, index, 430f)
                .rectTransform.anchoredPosition += new Vector2(0f, 22f);

            var go = new GameObject(caption.Replace(" ", "") + "Slider", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(430f, 12f);
            rt.anchoredPosition = new Vector2(LeftMargin, FirstRowY - index * RowHeight - 34f);

            var track = NewImage("Track", go.transform);
            track.color = new Color(1f, 1f, 1f, 0.12f);
            Stretch(track.rectTransform);

            var fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            Stretch((RectTransform)fillArea.transform);

            var fill = NewImage("Fill", fillArea.transform);
            fill.color = Red;
            var frt = fill.rectTransform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = new Vector2(1f, 1f);
            frt.sizeDelta = Vector2.zero;

            var slider = go.AddComponent<Slider>();
            slider.fillRect = frt;
            slider.targetGraphic = track;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.transition = Selectable.Transition.None;
            return slider;
        }

        private static TMP_InputField CodeField(Transform parent, int index)
        {
            var go = new GameObject("JoinCodeField", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, index, RowWidth);

            var back = go.GetComponent<Image>();
            back.color = new Color(1f, 1f, 1f, 0.07f);
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.16f, 0.16f, 0.5f);
            outline.effectDistance = new Vector2(1.5f, 1.5f);

            var viewport = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(go.transform, false);
            var vrt = (RectTransform)viewport.transform;
            Stretch(vrt);
            vrt.offsetMin = new Vector2(18f, 0f);
            vrt.offsetMax = new Vector2(-12f, 0f);

            var text = NewText("Text", viewport.transform, string.Empty, 28f);
            text.color = Red;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.characterSpacing = 14f;
            Stretch(text.rectTransform);

            var placeholder = NewText("Placeholder", viewport.transform, "JOIN CODE", 24f);
            placeholder.color = Dim;
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            placeholder.characterSpacing = 12f;
            Stretch(placeholder.rectTransform);

            var input = go.AddComponent<TMP_InputField>();
            input.textViewport = vrt;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.characterLimit = 12;
            input.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
            input.transition = Selectable.Transition.None;
            return input;
        }

        // ---- helpers --------------------------------------------------------------------------

        private static void Heading(Transform parent, string text)
        {
            TMP_Text heading = Label(parent, "Heading", text, 46f, Red, -2, 900f);
            heading.characterSpacing = 18f;
        }

        private static void Caption(Transform parent, string text, int index) =>
            Label(parent, "Caption" + index, text, 20f, Dim, index, 900f).characterSpacing = 8f;

        private static TMP_Text Label(Transform parent, string name, string text, float size,
            Color color, int index, float width)
        {
            TMP_Text t = NewText(name, parent, text, size);
            t.color = color;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            Place(t.rectTransform, index, width);
            return t;
        }

        private static void Place(RectTransform rt, int index, float width)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(width, 64f);
            rt.anchoredPosition = new Vector2(LeftMargin, FirstRowY - index * RowHeight);
        }

        private static Transform NewPanel(string name, Transform parent, SerializedObject so,
            string field, bool active)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform);
            go.SetActive(active);
            so.FindProperty(field).objectReferenceValue = go;
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
