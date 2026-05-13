// AutoSceneSetup.cs — Assets/Editor/
// Creates MainScene.unity automatically when Unity opens the project.
// Runs ONCE on first compile; skips if scene already exists.
#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using TMPro;

/// <summary>
/// Builds the entire MainScene programmatically so zero manual
/// Inspector work is needed. Triggered on first project open.
/// </summary>
[InitializeOnLoad]
public static class AutoSceneSetup
{
    static AutoSceneSetup()
    {
        // delayCall fires after Unity finishes importing/compiling
        EditorApplication.delayCall += TryCreateScene;
    }

    private static void TryCreateScene()
    {
        const string scenePath = "Assets/Scenes/MainScene.unity";
        if (File.Exists(scenePath))
        {
            Debug.Log("[AutoSceneSetup] MainScene already exists — skipping.");
            return;
        }

        Debug.Log("[AutoSceneSetup] Creating MainScene.unity ...");

        // Ensure dirs exist
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/Prefabs");

        var scene = EditorSceneManager.NewScene(
            NewSceneSetup.DefaultGameObjects,
            NewSceneMode.Single);

        // Remove default camera — XR Rig provides its own
        var defaultCam = GameObject.Find("Main Camera");
        if (defaultCam != null) Object.DestroyImmediate(defaultCam);
        var defaultLight = GameObject.Find("Directional Light");
        if (defaultLight != null) Object.DestroyImmediate(defaultLight);

        // ── 1. System GameObjects ─────────────────────────────────────────────
        var xrRig   = BuildXrRig();
        var aiGo    = BuildAiRoot();
        var qaGo    = BuildQaRoot();
        var xrTgt   = BuildXrTargeting();

        // ── 2. UI Canvas ──────────────────────────────────────────────────────
        BuildCanvas(out var captureUiComp);

        // ── 3. EventSystem ────────────────────────────────────────────────────
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // ── 4. Prefabs ────────────────────────────────────────────────────────
        BuildDeviceListItemPrefab(captureUiComp);
        BuildReticlePrefab(xrTgt);

        // ── 5. Save scene ─────────────────────────────────────────────────────
        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.Refresh();

        Debug.Log("[AutoSceneSetup] ✓ MainScene.unity created and saved!");
        Debug.Log("[AutoSceneSetup] ✓ All components added. SceneBootstrapper wires refs at runtime.");
        Debug.Log("[AutoSceneSetup] Next: File → Build Settings → Add Open Scenes → Build And Run");
    }

    // ── XR Rig ────────────────────────────────────────────────────────────────

    private static GameObject BuildXrRig()
    {
        var go = new GameObject("XR Rig");

        // Camera child (becomes Camera.main)
        var camGo = new GameObject("Main Camera");
        camGo.transform.SetParent(go.transform);
        var cam = camGo.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane  = 1000f;
        camGo.AddComponent<AudioListener>();

        // Add all core MonoBehaviours via reflection to survive AOT/rename
        go.AddComponent<SceneBootstrapper>();
        AddScriptByName(go, "SiteOwlXR.Core.CaptureController");
        AddScriptByName(go, "SiteOwlXR.Core.CalibrationManager");
        AddScriptByName(go, "SiteOwlXR.Core.CsvManager");
        AddScriptByName(go, "SiteOwlXR.Core.PhotoCapture");
        AddScriptByName(go, "SiteOwlXR.Core.GitAutoPush");
        AddScriptByName(go, "SiteOwlXR.Core.RealGpsManager");
        AddScriptByName(go, "SiteOwlXR.Core.PermissionsManager");

        return go;
    }

    private static GameObject BuildAiRoot()
    {
        var go = new GameObject("AI");
        AddScriptByName(go, "SiteOwlXR.AI.DeviceRecognizer");
        AddScriptByName(go, "SiteOwlXR.AI.DeviceLibraryLoader");
        AddScriptByName(go, "SiteOwlXR.AI.ExternalDataConnector");
        AddScriptByName(go, "SiteOwlXR.AI.AiCopilotUI");
        return go;
    }

    private static GameObject BuildQaRoot()
    {
        var go = new GameObject("QA");
        AddScriptByName(go, "SiteOwlXR.QA.QualityAssurance");
        AddScriptByName(go, "SiteOwlXR.QA.MetadataManager");
        return go;
    }

    private static SiteOwlXR.XR.XrTargeting BuildXrTargeting()
    {
        var go = new GameObject("XR Targeting");
        var lr = go.AddComponent<LineRenderer>();

        lr.positionCount = 2;
        lr.startWidth    = 0.005f;
        lr.endWidth      = 0.002f;
        lr.useWorldSpace = true;

        // Simple white material for the beam
        lr.material = new Material(Shader.Find("Sprites/Default"));

        var xrTgt = AddScriptByName(go, "SiteOwlXR.XR.XrTargeting") as SiteOwlXR.XR.XrTargeting;
        return xrTgt;
    }

    // ── Canvas + all UI panels ────────────────────────────────────────────────

    private static void BuildCanvas(out SiteOwlXR.UI.CaptureUI captureUi)
    {
        var canvasGo = new GameObject("UI Canvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        var rt = canvasGo.GetComponent<RectTransform>();
        rt.sizeDelta    = new Vector2(1920, 1080);
        rt.localScale   = Vector3.one * 0.001f;
        rt.localPosition = new Vector3(0, 0, 2f);

        // Build panels
        var calPanel    = MakePanel(canvasGo, "CalibrationPanel",  new Vector2(-600, 0));
        var listPanel   = MakePanel(canvasGo, "DeviceListPanel",   new Vector2(-600, 0));
        var capPanel    = MakePanel(canvasGo, "CapturePanel",      new Vector2(-600, 0));
        var statusPanel = MakePanel(canvasGo, "StatusPanel",       new Vector2(600, -400));

        // Calibration UI
        var calX   = MakeInputField(calPanel, "CalibrationXInput",  new Vector2(-120, 150), "SiteOwl X");
        var calY   = MakeInputField(calPanel, "CalibrationYInput",  new Vector2(120,  150), "SiteOwl Y");
        var calBtn = MakeButton(calPanel,     "CalibrateButton",    new Vector2(0, 80),  "Set Anchor");
        var scaleBtn = MakeButton(calPanel,   "ScaleCalButton",     new Vector2(0, 20),  "Begin Scale Cal");
        var calStatus= MakeLabel(calPanel,    "CalibrationStatusText", new Vector2(0, -40), "Not calibrated");

        // Device list UI
        var searchIn   = MakeInputField(listPanel, "SearchInput",       new Vector2(-200, 400), "Search...");
        var searchBtn  = MakeButton(listPanel,     "SearchButton",      new Vector2(100,  400), "Search");
        var missingBtn = MakeButton(listPanel,     "ShowMissingButton", new Vector2(280,  400), "Missing");
        var countTxt   = MakeLabel(listPanel,      "DeviceCountText",   new Vector2(0, 340),    "0/0 captured");
        var container  = MakeScrollList(listPanel, "DeviceListContainer");

        // Capture panel UI
        var devName  = MakeLabel(capPanel, "SelectedDeviceName", new Vector2(0,  200), "No device selected");
        var devInfo  = MakeLabel(capPanel, "SelectedDeviceInfo", new Vector2(0,  100), "");
        var capBtn   = MakeButton(capPanel,"CaptureButton",      new Vector2(-100, -200), "Capture");
        var cancelBtn= MakeButton(capPanel,"CancelButton",       new Vector2(100, -200), "Cancel");
        var capInd   = MakeLabel(capPanel, "CapturingIndicator", new Vector2(0, -280), "Capturing...");

        // Status bar
        var statusTxt = MakeLabel(statusPanel,"StatusText",     new Vector2(0, 60),  "Ready");
        var posTxt    = MakeLabel(statusPanel,"PositionText",   new Vector2(0, 20),  "Position: --");
        var confTxt   = MakeLabel(statusPanel,"ConfidenceText", new Vector2(0, -20), "Confidence: --");

        // ── Wire CaptureUI ────────────────────────────────────────────────────
        captureUi = canvasGo.AddComponent<SiteOwlXR.UI.CaptureUI>();

        captureUi.CalibrationPanel   = calPanel;
        captureUi.DeviceListPanel    = listPanel;
        captureUi.CapturePanel       = capPanel;
        captureUi.StatusPanel        = statusPanel;

        captureUi.CalibrationXInput  = calX;
        captureUi.CalibrationYInput  = calY;
        captureUi.CalibrateButton    = calBtn;
        captureUi.CalibrationStatusText = calStatus.GetComponent<TextMeshProUGUI>();

        captureUi.SearchInput        = searchIn;
        captureUi.SearchButton       = searchBtn;
        captureUi.ShowMissingButton  = missingBtn;
        captureUi.DeviceCountText    = countTxt.GetComponent<TextMeshProUGUI>();
        captureUi.DeviceListContainer= container.transform;

        captureUi.SelectedDeviceName = devName.GetComponent<TextMeshProUGUI>();
        captureUi.SelectedDeviceInfo = devInfo.GetComponent<TextMeshProUGUI>();
        captureUi.CaptureButton      = capBtn;
        captureUi.CancelButton       = cancelBtn;
        captureUi.CapturingIndicator = capInd;

        captureUi.StatusText         = statusTxt.GetComponent<TextMeshProUGUI>();
        captureUi.PositionText       = posTxt.GetComponent<TextMeshProUGUI>();
        captureUi.ConfidenceText     = confTxt.GetComponent<TextMeshProUGUI>();

        // Start with calibration panel visible
        listPanel.SetActive(false);
        capPanel.SetActive(false);
    }

    // ── Prefabs ───────────────────────────────────────────────────────────────

    private static void BuildDeviceListItemPrefab(SiteOwlXR.UI.CaptureUI captureUi)
    {
        var go     = new GameObject("DeviceListItem");
        var img    = go.AddComponent<Image>();
        img.color  = new Color(0.2f, 0.2f, 0.2f);

        var rt     = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(880, 70);

        var itemUi  = go.AddComponent<SiteOwlXR.UI.DeviceListItemUI>();

        var nameGo  = MakeLabel(go, "DeviceNameText",  new Vector2(-200, 15), "Device Name");
        var typeGo  = MakeLabel(go, "DeviceTypeText",  new Vector2(0,  -10), "Type | System");
        var statGo  = MakeLabel(go, "StatusText",      new Vector2(300, 0),  "NEEDS CAPTURE");
        var btnGo   = new GameObject("SelectButton");
        btnGo.transform.SetParent(go.transform);
        var btn     = btnGo.AddComponent<Button>();
        var btnImg  = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.1f, 0.4f, 0.8f);
        var btnRt   = btnGo.GetComponent<RectTransform>();
        btnRt.sizeDelta   = new Vector2(100, 50);
        btnRt.anchoredPosition = new Vector2(390, 0);

        itemUi.DeviceNameText  = nameGo.GetComponent<TextMeshProUGUI>();
        itemUi.DeviceTypeText  = typeGo.GetComponent<TextMeshProUGUI>();
        itemUi.StatusText      = statGo.GetComponent<TextMeshProUGUI>();
        itemUi.SelectButton    = btn;
        itemUi.BackgroundImage = img;

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/DeviceListItem.prefab");
        Object.DestroyImmediate(go);

        if (captureUi != null)
            captureUi.DeviceListItemPrefab = prefab;
    }

    private static void BuildReticlePrefab(SiteOwlXR.XR.XrTargeting xrTgt)
    {
        var go  = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Reticle";
        go.transform.localScale = Vector3.one * 0.05f;
        Object.DestroyImmediate(go.GetComponent<SphereCollider>());

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ??
                               Shader.Find("Standard"));
        mat.color = Color.green;
        go.GetComponent<MeshRenderer>().material = mat;

        AssetDatabase.CreateAsset(mat, "Assets/Prefabs/ReticleMat.mat");

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Reticle.prefab");
        Object.DestroyImmediate(go);

        if (xrTgt != null)
            xrTgt.ReticlePrefab = prefab;
    }

    // ── UI helpers ────────────────────────────────────────────────────────────

    private static GameObject MakePanel(GameObject parent, string name, Vector2 pos)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        var rt  = go.GetComponent<RectTransform>();
        rt.sizeDelta      = new Vector2(900, 700);
        rt.anchoredPosition = pos;
        return go;
    }

    private static TMP_InputField MakeInputField(GameObject parent, string name,
        Vector2 pos, string placeholder)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var field = go.AddComponent<TMP_InputField>();
        var rt    = go.GetComponent<RectTransform>();
        rt.sizeDelta      = new Vector2(200, 40);
        rt.anchoredPosition = pos;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var text = textGo.AddComponent<TextMeshProUGUI>();
        text.fontSize = 18;
        text.color    = Color.white;
        field.textComponent = text;

        var phGo  = new GameObject("Placeholder");
        phGo.transform.SetParent(go.transform, false);
        var ph    = phGo.AddComponent<TextMeshProUGUI>();
        ph.text      = placeholder;
        ph.fontSize  = 18;
        ph.fontStyle = FontStyles.Italic;
        ph.color     = new Color(0.7f, 0.7f, 0.7f);
        field.placeholder = ph;

        go.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);
        return field;
    }

    private static Button MakeButton(GameObject parent, string name,
        Vector2 pos, string label)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var btn = go.AddComponent<Button>();
        var img = go.AddComponent<Image>();
        img.color = new Color(0f, 0.33f, 0.88f);  // Walmart blue
        var rt  = go.GetComponent<RectTransform>();
        rt.sizeDelta      = new Vector2(160, 40);
        rt.anchoredPosition = pos;

        var lblGo = new GameObject("Label");
        lblGo.transform.SetParent(go.transform, false);
        var txt   = lblGo.AddComponent<TextMeshProUGUI>();
        txt.text      = label;
        txt.fontSize  = 16;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color     = Color.white;

        return btn;
    }

    private static GameObject MakeLabel(GameObject parent, string name,
        Vector2 pos, string text)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text     = text;
        tmp.fontSize = 16;
        tmp.color    = Color.white;
        var rt  = go.GetComponent<RectTransform>();
        rt.sizeDelta      = new Vector2(500, 40);
        rt.anchoredPosition = pos;
        return go;
    }

    private static GameObject MakeScrollList(GameObject parent, string name)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt  = go.AddComponent<RectTransform>();
        rt.sizeDelta      = new Vector2(880, 550);
        rt.anchoredPosition = new Vector2(0, -80);
        go.AddComponent<VerticalLayoutGroup>().childForceExpandWidth = true;
        go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return go;
    }

    // ── Reflection helper (adds component by full type name, safe if not found) ──

    private static Component AddScriptByName(GameObject go, string fullTypeName)
    {
        var type = System.Type.GetType(fullTypeName + ", Assembly-CSharp") ??
                   System.Type.GetType(fullTypeName);
        if (type == null)
        {
            Debug.LogWarning($"[AutoSceneSetup] Type not found: {fullTypeName}");
            return null;
        }
        return go.AddComponent(type);
    }
}
#endif
