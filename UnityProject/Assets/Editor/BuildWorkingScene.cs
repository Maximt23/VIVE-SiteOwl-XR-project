using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.OpenVR;
using TMPro;
using UnityEngine.UI;
using System.IO;

namespace SiteOwlXR.Editor
{
    /// <summary>
    /// Builds a complete working CCTV Survey scene
    /// Run: Tools → CCTV Survey → Build Working Scene
    /// </summary>
    public class BuildWorkingScene : EditorWindow
    {
        [MenuItem("Tools/CCTV Survey/🔨 Build Working Scene", false, 0)]
        public static void BuildScene()
        {
            if (EditorUtility.DisplayDialog(
                "Build Working Scene",
                "This will create a complete working CCTV Survey scene with:\n\n" +
                "✓ XR Rig with all components\n" +
                "✓ GPS Manager\n" +
                "✓ Calibration Manager\n" +
                "✓ CSV Manager\n" +
                "✓ Photo Capture\n" +
                "✓ AI Recognizer\n" +
                "✓ THE ORCHESTRATOR (Capture Controller)\n" +
                "✓ Complete UI with camera list\n" +
                "✓ Test camera objects\n" +
                "✓ Sample CSV data\n\n" +
                "Continue?",
                "Build Scene",
                "Cancel"))
            {
                ExecuteBuild();
            }
        }
        
        static void ExecuteBuild()
        {
            EditorUtility.DisplayProgressBar("Building Scene", "Creating scene asset...", 0f);
            
            try
            {
                // 1. Create scene
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                
                // 2. Create XR Rig
                EditorUtility.DisplayProgressBar("Building Scene", "Creating XR Rig...", 0.1f);
                var xrRig = CreateXrRig();
                
                // 3. Create UI
                EditorUtility.DisplayProgressBar("Building Scene", "Creating UI...", 0.3f);
                var ui = CreateCompleteUI();
                
                // 4. Create test environment
                EditorUtility.DisplayProgressBar("Building Scene", "Creating test environment...", 0.5f);
                CreateTestEnvironment();
                
                // 5. Create lighting
                EditorUtility.DisplayProgressBar("Building Scene", "Adding lighting...", 0.7f);
                CreateLighting();
                
                // 6. Wire up UI to controller
                EditorUtility.DisplayProgressBar("Building Scene", "Wiring components...", 0.8f);
                WireComponents(xrRig, ui);
                
                // 7. Create sample CSV
                EditorUtility.DisplayProgressBar("Building Scene", "Creating sample data...", 0.9f);
                CreateSampleCsv();
                
                // 8. Save scene
                string scenePath = "Assets/Scenes/CctvSurvey_Working.unity";
                Directory.CreateDirectory(Path.GetDirectoryName(scenePath));
                EditorSceneManager.SaveScene(scene, scenePath);
                
                EditorUtility.ClearProgressBar();
                
                EditorUtility.DisplayDialog(
                    "✅ Scene Built Successfully!",
                    "Working scene created at:\n\n" +
                    scenePath + "\n\n" +
                    "Next Steps:\n" +
                    "1. Open the scene\n" +
                    "2. Select 'XR Cctv Rig' to see all components\n" +
                    "3. Configure ExternalPhotoUploader for your storage\n" +
                    "4. Build and run on VIVE XR Elite\n\n" +
                    "Press Play in editor to test (mock GPS mode)",
                    "Open Scene Now"
                );
                
                // Open the scene
                EditorSceneManager.OpenScene(scenePath);
                
                // Select the XR Rig
                Selection.activeGameObject = xrRig;
                
                Debug.Log("<color=green>[BuildWorkingScene] ✅ Complete working scene built!</color>");
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"[BuildWorkingScene] Failed: {ex.Message}");
                EditorUtility.DisplayDialog("Build Failed", $"Error: {ex.Message}", "OK");
            }
        }
        
        static GameObject CreateXrRig()
        {
            Debug.Log("[BuildWorkingScene] Creating XR Cctv Rig...");
            
            // Create parent
            var xrRig = new GameObject("XR Cctv Rig");
            
            // Add XR Origin
            var xrOrigin = xrRig.AddComponent<XROrigin>();
            
            // Create camera offset
            var cameraOffset = new GameObject("Camera Offset");
            cameraOffset.transform.SetParent(xrRig.transform);
            xrOrigin.CameraFloorOffsetObject = cameraOffset;
            
            // Create main camera
            var cameraObj = new GameObject("Main Camera");
            cameraObj.transform.SetParent(cameraOffset.transform);
            var camera = cameraObj.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 60f;
            xrOrigin.Camera = camera;
            
            // Add all manager scripts
            Debug.Log("[BuildWorkingScene] Adding manager components...");
            
            // 1. XrInitializer
            var xrInit = xrRig.AddComponent<Managers.XrInitializer>();
            xrInit.timeoutSeconds = 10f;
            xrInit.skipXrInEditor = true; // For testing in editor
            
            // 2. RealGpsManager
            var gpsManager = xrRig.AddComponent<Managers.RealGpsManager>();
            gpsManager.desiredAccuracy = 5f;
            gpsManager.timeoutSeconds = 20f;
            gpsManager.useMockGpsInEditor = true;
            gpsManager.mockLatitude = 32.812345f;
            gpsManager.mockLongitude = -96.812345f;
            
            // 3. CalibrationManager
            var calManager = xrRig.AddComponent<Managers.CalibrationManager>();
            calManager.pixelsPerMeter = 10f;
            
            // 4. SiteOwlCsvManager
            var csvManager = xrRig.AddComponent<Managers.SiteOwlCsvManager>();
            csvManager.csvFileName = "cameras.csv";
            
            // 5. PhotoCapture
            var photoCapture = xrRig.AddComponent<Managers.PhotoCapture>();
            photoCapture.photoWidth = 1920;
            photoCapture.photoHeight = 1080;
            
            // 6. ExternalPhotoUploader
            var uploader = xrRig.AddComponent<Uploaders.ExternalPhotoUploader>();
            uploader.primaryProvider = Models.StorageProvider.LocalNetwork;
            uploader.fallbackToLocal = true;
            
            // 7. CctvCameraRecognizer
            var aiRecognizer = xrRig.AddComponent<AI.CctvCameraRecognizer>();
            aiRecognizer.confidenceThreshold = 0.6f;
            aiRecognizer.maxSuggestions = 3;
            
            // 8. THE ORCHESTRATOR
            var controller = xrRig.AddComponent<Controller.CctvCaptureController>();
            controller.XrCameraTransform = camera.transform;
            
            // 9. LineRenderer for pointer
            var lineObj = new GameObject("Pointer Line");
            lineObj.transform.SetParent(cameraObj.transform);
            var lineRenderer = lineObj.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = 0.01f;
            lineRenderer.endWidth = 0.005f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.cyan;
            lineRenderer.endColor = Color.cyan;
            lineRenderer.useWorldSpace = true;
            
            Debug.Log("<color=green>[BuildWorkingScene] XR Rig complete with 8 managers</color>");
            
            return xrRig;
        }
        
        static GameObject CreateCompleteUI()
        {
            Debug.Log("[BuildWorkingScene] Creating complete UI...");
            
            // Create Event System
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            
            // Create Canvas
            var canvasObj = new GameObject("Survey UI Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.AddComponent<CanvasRenderer>();
            
            // Position in front of user
            canvas.transform.position = new Vector3(0, 1.5f, 2f);
            canvas.transform.rotation = Quaternion.identity;
            canvas.transform.localScale = Vector3.one * 0.005f;
            
            var rectTransform = canvas.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(1000, 800);
            
            // Create Graphic Raycaster for interaction
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // === PANEL 1: Loading Panel ===
            var loadingPanel = CreatePanel(canvasObj, "LoadingPanel", Vector2.zero, new Vector2(800, 600), Color.black);
            loadingPanel.SetActive(false);
            
            var loadingText = CreateText(loadingPanel, "LoadingText", "Initializing XR...", Vector2.zero, new Vector2(700, 200), 48, TextAlignmentOptions.Center);
            var loadingSubText = CreateText(loadingPanel, "SubText", "Please wait while we initialize the headset", new Vector2(0, -100), new Vector2(700, 100), 24, TextAlignmentOptions.Center);
            loadingSubText.color = Color.gray;
            
            // === PANEL 2: Main Panel ===
            var mainPanel = CreatePanel(canvasObj, "MainPanel", Vector2.zero, new Vector2(1000, 800), new Color(0.1f, 0.1f, 0.1f, 0.95f));
            
            // Title
            CreateText(mainPanel, "TitleText", "📹 CCTV SURVEY", new Vector2(0, 350), new Vector2(900, 80), 56, TextAlignmentOptions.Center);
            
            // GPS Status
            var gpsText = CreateText(mainPanel, "GpsText", "🛰️ GPS: Searching...", new Vector2(0, 280), new Vector2(900, 60), 28, TextAlignmentOptions.Center);
            gpsText.color = new Color(0.8f, 0.8f, 0.8f);
            
            // Status message
            var statusText = CreateText(mainPanel, "StatusText", "Select a camera from the list below", new Vector2(0, 220), new Vector2(900, 60), 28, TextAlignmentOptions.Center);
            statusText.color = Color.yellow;
            
            // Camera List
            var listPanel = CreatePanel(mainPanel, "CameraListPanel", new Vector2(0, 20), new Vector2(900, 400), new Color(0.15f, 0.15f, 0.15f, 1f));
            
            // List header
            CreateText(listPanel, "ListHeader", "📋 CAMERA LIST", new Vector2(0, 170), new Vector2(800, 40), 24, TextAlignmentOptions.Left);
            
            // Search field
            var searchObj = new GameObject("SearchField");
            searchObj.transform.SetParent(listPanel.transform);
            var searchRect = searchObj.AddComponent<RectTransform>();
            searchRect.anchoredPosition = new Vector2(0, 130);
            searchRect.sizeDelta = new Vector2(800, 50);
            
            var searchInput = searchObj.AddComponent<TMP_InputField>();
            var searchTextObj = new GameObject("Text");
            searchTextObj.transform.SetParent(searchObj.transform);
            var searchText = searchTextObj.AddComponent<TextMeshProUGUI>();
            searchText.fontSize = 24;
            searchText.color = Color.white;
            searchInput.textComponent = searchText;
            searchInput.placeholder = searchText;
            
            // List container (ScrollView would be better, but simple for now)
            var listContainer = new GameObject("ListContainer");
            listContainer.transform.SetParent(listPanel.transform);
            var listContainerRect = listContainer.AddComponent<RectTransform>();
            listContainerRect.anchoredPosition = new Vector2(0, -50);
            listContainerRect.sizeDelta = new Vector2(850, 250);
            
            // Add CameraListUI component
            var cameraListUI = listContainer.AddComponent<UI.CameraListUI>();
            cameraListUI.ListContainer = listContainer.transform;
            cameraListUI.SearchField = searchInput;
            
            // === PANEL 3: Capture Panel ===
            var capturePanel = CreatePanel(mainPanel, "CapturePanel", new Vector2(0, -320), new Vector2(900, 150), new Color(0.2f, 0.2f, 0.2f, 1f));
            capturePanel.SetActive(false);
            
            var selectedText = CreateText(capturePanel, "SelectedCameraText", "Selected: None", new Vector2(0, 40), new Vector2(800, 50), 28, TextAlignmentOptions.Center);
            
            var progressText = CreateText(capturePanel, "ProgressText", "", new Vector2(0, 0), new Vector2(800, 40), 24, TextAlignmentOptions.Center);
            progressText.color = Color.cyan;
            
            // Capture Button
            var btnObj = new GameObject("CaptureButton");
            btnObj.transform.SetParent(capturePanel.transform);
            var btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchoredPosition = new Vector2(0, -40);
            btnRect.sizeDelta = new Vector2(400, 60);
            
            var btnImage = btnObj.AddComponent<Image>();
            btnImage.color = new Color(0.2f, 0.7f, 1f);
            
            var btn = btnObj.AddComponent<Button>();
            var btnTextObj = new GameObject("Text");
            btnTextObj.transform.SetParent(btnObj.transform);
            var btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
            btnText.text = "📸 CAPTURE";
            btnText.fontSize = 32;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.color = Color.white;
            
            // === PANEL 4: AI Suggestions Panel ===
            var aiPanel = CreatePanel(canvasObj, "AiSuggestionsPanel", Vector2.zero, new Vector2(900, 700), new Color(0.1f, 0.1f, 0.1f, 0.98f));
            aiPanel.SetActive(false);
            
            CreateText(aiPanel, "AiTitle", "🤖 AI SUGGESTIONS", new Vector2(0, 300), new Vector2(800, 80), 48, TextAlignmentOptions.Center);
            CreateText(aiPanel, "AiSubtitle", "Select the matching camera type", new Vector2(0, 240), new Vector2(800, 50), 24, TextAlignmentOptions.Center);
            
            var suggestionContainer = new GameObject("SuggestionContainer");
            suggestionContainer.transform.SetParent(aiPanel.transform);
            var suggRect = suggestionContainer.AddComponent<RectTransform>();
            suggRect.anchoredPosition = new Vector2(0, 0);
            suggRect.sizeDelta = new Vector2(850, 400);
            
            // Skip AI button
            var skipBtnObj = new GameObject("SkipButton");
            skipBtnObj.transform.SetParent(aiPanel.transform);
            var skipRect = skipBtnObj.AddComponent<RectTransform>();
            skipRect.anchoredPosition = new Vector2(0, -280);
            skipRect.sizeDelta = new Vector2(300, 60);
            
            var skipImage = skipBtnObj.AddComponent<Image>();
            skipImage.color = Color.gray;
            
            var skipBtn = skipBtnObj.AddComponent<Button>();
            var skipTextObj = new GameObject("Text");
            skipTextObj.transform.SetParent(skipBtnObj.transform);
            var skipText = skipTextObj.AddComponent<TextMeshProUGUI>();
            skipText.text = "Skip AI";
            skipText.fontSize = 28;
            skipText.alignment = TextAlignmentOptions.Center;
            skipText.color = Color.white;
            
            Debug.Log("<color=green>[BuildWorkingScene] UI complete with 4 panels</color>");
            
            return canvasObj;
        }
        
        static GameObject CreatePanel(GameObject parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent.transform);
            
            var rect = panel.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            var image = panel.AddComponent<Image>();
            image.color = color;
            
            return panel;
        }
        
        static TextMeshProUGUI CreateText(GameObject parent, string name, string content, Vector2 position, Vector2 size, int fontSize, TextAlignmentOptions alignment)
        {
            var textObj = new GameObject(name);
            textObj.transform.SetParent(parent.transform);
            
            var rect = textObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            
            return text;
        }
        
        static void CreateTestEnvironment()
        {
            Debug.Log("[BuildWorkingScene] Creating test environment...");
            
            // Create test cameras (cubes representing physical cameras)
            string[] cameraNames = { "CAM-001", "CAM-002", "CAM-003", "CAM-004", "CAM-005" };
            Vector3[] positions = {
                new Vector3(3, 1, 5),
                new Vector3(-2, 1.5f, 8),
                new Vector3(0, 2, 12),
                new Vector3(5, 1.2f, 7),
                new Vector3(-4, 1, 10)
            };
            
            Color[] colors = { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta };
            
            for (int i = 0; i < cameraNames.Length; i++)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = cameraNames[i];
                cube.transform.position = positions[i];
                cube.transform.localScale = Vector3.one * 0.3f;
                
                var renderer = cube.GetComponent<Renderer>();
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = colors[i];
                
                // Add label
                var labelObj = new GameObject($"{cameraNames[i]}_Label");
                labelObj.transform.position = positions[i] + Vector3.up * 0.5f;
                
                Debug.Log($"[BuildWorkingScene] Created test camera: {cameraNames[i]}");
            }
            
            // Create ground plane
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0, 0, 5);
            ground.transform.localScale = new Vector3(3, 1, 3);
            ground.GetComponent<Renderer>().material.color = new Color(0.2f, 0.2f, 0.2f);
            
            Debug.Log("<color=green>[BuildWorkingScene] Test environment complete</color>");
        }
        
        static void CreateLighting()
        {
            // Directional light
            var lightObj = new GameObject("Directional Light");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.color = Color.white;
            lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
            
            // Ambient light
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.2f, 0.2f, 0.2f);
        }
        
        static void WireComponents(GameObject xrRig, GameObject ui)
        {
            Debug.Log("[BuildWorkingScene] Wiring components...");
            
            var controller = xrRig.GetComponent<Controller.CctvCaptureController>();
            if (controller != null)
            {
                // Find UI text components
                var canvas = ui.GetComponent<Canvas>();
                if (canvas != null)
                {
                    var mainPanel = canvas.transform.Find("MainPanel");
                    if (mainPanel != null)
                    {
                        controller.StatusText = mainPanel.Find("StatusText")?.GetComponent<TextMeshProUGUI>();
                        controller.GpsText = mainPanel.Find("GpsText")?.GetComponent<TextMeshProUGUI>();
                        
                        var capturePanel = mainPanel.Find("CapturePanel");
                        if (capturePanel != null)
                        {
                            controller.ProgressText = capturePanel.Find("ProgressText")?.GetComponent<TextMeshProUGUI>();
                            controller.CapturePanel = capturePanel.gameObject;
                            
                            var btn = capturePanel.Find("CaptureButton")?.GetComponent<Button>();
                            if (btn != null)
                            {
                                controller.CaptureButton = btn;
                            }
                        }
                    }
                }
                
                // Find loading panel
                var loadingPanel = ui.transform.Find("LoadingPanel")?.gameObject;
                if (loadingPanel != null)
                {
                    var xrInit = xrRig.GetComponent<Managers.XrInitializer>();
                    if (xrInit != null)
                    {
                        xrInit.loadingPanel = loadingPanel;
                        xrInit.loadingText = loadingPanel.transform.Find("LoadingText")?.GetComponent<TextMeshProUGUI>();
                    }
                }
            }
            
            Debug.Log("<color=green>[BuildWorkingScene] Components wired</color>");
        }
        
        static void CreateSampleCsv()
        {
            Debug.Log("[BuildWorkingScene] Creating sample CSV...");
            
            string csvContent = @"Site,Device Name,Device Type,System Type,Description,Barcode,X,Y,IP Address,VMS Zone,Manufacturer,Model,Photo URL,Photo Path,Capture Timestamp,Capture Method,Coordinate Confidence,Review Status
2996,CAM-001,Dome Camera,CCTV,Main entrance dome,,,,192.168.1.101,Front_Entrance,Axis,P3245-LV,,,,,,,PENDING
2996,CAM-002,Bullet Camera,CCTV,North parking lot view,,,,192.168.1.102,Parking_North,Hikvision,DS-2CD2T85G1-I5,,,,,,,PENDING
2996,CAM-003,PTZ Camera,CCTV,Loading dock PTZ with zoom,,,,192.168.1.103,Loading_Dock,Axis,Q6155-E,,,,,,,PENDING
2996,CAM-004,Fisheye Camera,CCTV,Lobby overview camera,,,,192.168.1.104,Lobby,Axis,M3058-PLVE,,,,,,,PENDING
2996,CAM-005,Bullet Camera,CCTV,Rear exit camera,,,,192.168.1.105,Rear_Exit,Bosch,FLEXIDOME,,,,,,,PENDING";
            
            string streamingAssetsPath = "Assets/StreamingAssets";
            Directory.CreateDirectory(streamingAssetsPath);
            
            string csvPath = Path.Combine(streamingAssetsPath, "cameras.csv");
            File.WriteAllText(csvPath, csvContent);
            
            AssetDatabase.Refresh();
            
            Debug.Log($"<color=green>[BuildWorkingScene] Sample CSV created: {csvPath}</color>");
        }
    }
}
