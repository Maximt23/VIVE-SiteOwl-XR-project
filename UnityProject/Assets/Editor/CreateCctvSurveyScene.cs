using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

namespace SiteOwlXR.Editor
{
    /// <summary>
    /// Editor tool to create a complete working CCTV survey scene
    /// Run: Tools → CCTV Survey → Create Working Scene
    /// </summary>
    public class CreateCctvSurveyScene : EditorWindow
    {
        [MenuItem("Tools/CCTV Survey/Create Working Scene")]
        public static void CreateScene()
        {
            // 1. Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            
            Debug.Log("[SceneCreator] Creating CCTV Survey scene...");
            
            // 2. Create XR Rig
            var xrRig = CreateXrRig();
            
            // 3. Create UI
            var uiCanvas = CreateUI();
            
            // 4. Create test objects (cubes representing cameras)
            CreateTestEnvironment();
            
            // 5. Add directional light
            var light = new GameObject("Directional Light");
            light.AddComponent<Light>().type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            
            // 6. Save scene
            string path = "Assets/Scenes/CctvSurvey.unity";
            EditorSceneManager.SaveScene(scene, path);
            
            Debug.Log($"[SceneCreator] Scene created: {path}");
            Debug.Log("[SceneCreator] Next steps:");
            Debug.Log("  1. Open the scene");
            Debug.Log("  2. Assign CSV file to SiteOwlCsvManager");
            Debug.Log("  3. Configure photo uploader settings");
            Debug.Log("  4. Build and test!");
            
            EditorUtility.DisplayDialog(
                "Scene Created!",
                "Working CCTV Survey scene created at:\n\n" +
                "Assets/Scenes/CctvSurvey.unity\n\n" +
                "Next steps:\n" +
                "1. Open the scene\n" +
                "2. Configure CSV path in SiteOwlCsvManager\n" +
                "3. Set photo storage provider\n" +
                "4. Build and run!",
                "Open Scene",
                "Later"
            );
            
            // Open the scene
            EditorSceneManager.OpenScene(path);
        }
        
        static GameObject CreateXrRig()
        {
            Debug.Log("[SceneCreator] Creating XR Rig...");
            
            // Create XR Origin (Action-based)
            var xrOriginGO = new GameObject("XR Cctv Rig");
            var xrOrigin = xrOriginGO.AddComponent<XROrigin>();
            
            // Create Camera Offset
            var cameraOffset = new GameObject("Camera Offset");
            cameraOffset.transform.SetParent(xrOriginGO.transform);
            xrOrigin.CameraFloorOffsetObject = cameraOffset;
            
            // Create Main Camera
            var cameraGO = new GameObject("Main Camera");
            cameraGO.transform.SetParent(cameraOffset.transform);
            var camera = cameraGO.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.Skybox;
            xrOrigin.Camera = camera;
            
            // Create Controllers
            var leftController = new GameObject("LeftHand Controller");
            leftController.transform.SetParent(xrOriginGO.transform);
            leftController.AddComponent<ActionBasedController>();
            
            var rightController = new GameObject("RightHand Controller");
            rightController.transform.SetParent(xrOriginGO.transform);
            rightController.AddComponent<ActionBasedController>();
            
            // Add all our scripts
            Debug.Log("[SceneCreator] Adding core components...");
            
            // GPS Manager
            var gpsManager = xrOriginGO.AddComponent<Core.RealGpsManager>();
            gpsManager.desiredAccuracy = 5f;
            gpsManager.updateDistance = 1f;
            
            // Calibration Manager
            var calManager = xrOriginGO.AddComponent<Core.CalibrationManager>();
            
            // CSV Manager
            var csvManager = xrOriginGO.AddComponent<Core.SiteOwlCsvManager>();
            csvManager.CsvFileName = "cameras.csv";
            
            // Photo Capture
            var photoCapture = xrOriginGO.AddComponent<Core.PhotoCapture>();
            photoCapture.photosFolder = "CapturedPhotos";
            
            // External Uploader
            var uploader = xrOriginGO.AddComponent<Core.ExternalPhotoUploader>();
            uploader.provider = Core.StorageProviderType.LocalNetwork;
            
            // AI Recognizer
            var aiRecognizer = xrOriginGO.AddComponent<AI.CctvCameraRecognizer>();
            aiRecognizer.confidenceThreshold = 0.6f;
            
            // The Orchestrator
            var controller = xrOriginGO.AddComponent<Core.CctvCaptureController>();
            controller.cameraTransform = camera.transform;
            
            // Memory Manager
            var memoryManager = xrOriginGO.AddComponent<Memory.SurveyMemoryManager>();
            
            // XR Initializer
            var xrInit = xrOriginGO.AddComponent<XrInitializer>();
            
            // Add Line Renderer for pointer
            var lineObj = new GameObject("Pointer Line");
            lineObj.transform.SetParent(cameraGO.transform);
            var lineRenderer = lineObj.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = 0.01f;
            lineRenderer.endWidth = 0.005f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.cyan;
            lineRenderer.endColor = Color.cyan;
            
            Debug.Log("[SceneCreator] XR Rig complete!");
            
            return xrOriginGO;
        }
        
        static GameObject CreateUI()
        {
            Debug.Log("[SceneCreator] Creating UI...");
            
            // Create Canvas
            var canvasGO = new GameObject("UI Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasRenderer>();
            
            // Position in front of user
            canvas.transform.position = new Vector3(0, 1.5f, 2f);
            canvas.transform.localScale = Vector3.one * 0.005f;
            
            // Create Event System
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            
            // Create Loading Panel
            var loadingPanel = CreatePanel(canvasGO, "LoadingPanel", Vector2.zero, new Vector2(800, 600));
            var loadingText = CreateText(loadingPanel, "LoadingText", "Initializing XR...", Vector2.zero, new Vector2(700, 200));
            loadingPanel.SetActive(false);
            
            // Create Main Panel
            var mainPanel = CreatePanel(canvasGO, "MainPanel", Vector2.zero, new Vector2(1000, 800));
            
            // Title
            CreateText(mainPanel, "TitleText", "CCTV SURVEY", new Vector2(0, 350), new Vector2(900, 100), 48);
            
            // Status
            CreateText(mainPanel, "StatusText", "Ready to capture", new Vector2(0, 250), new Vector2(900, 80));
            
            // GPS display
            CreateText(mainPanel, "GpsText", "GPS: Searching...", new Vector2(0, 150), new Vector2(900, 60));
            
            // Camera list area (placeholder)
            var cameraListArea = CreatePanel(mainPanel, "CameraListArea", new Vector2(0, -50), new Vector2(900, 400));
            CreateText(cameraListArea, "CameraListLabel", "Camera List", new Vector2(0, 170), new Vector2(800, 50));
            
            // Capture button
            var captureBtn = CreateButton(mainPanel, "CaptureButton", "CAPTURE", new Vector2(0, -300), new Vector2(400, 100));
            
            // AI Suggestions Panel (hidden by default)
            var aiPanel = CreatePanel(canvasGO, "AiSuggestionsPanel", new Vector2(0, 0), new Vector2(900, 700));
            CreateText(aiPanel, "AiTitle", "AI SUGGESTIONS", new Vector2(0, 300), new Vector2(800, 80), 36);
            aiPanel.SetActive(false);
            
            Debug.Log("[SceneCreator] UI complete!");
            
            return canvasGO;
        }
        
        static GameObject CreatePanel(GameObject parent, string name, Vector2 position, Vector2 size)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent.transform);
            
            var rect = panel.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            var image = panel.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            
            return panel;
        }
        
        static GameObject CreateText(GameObject parent, string name, string content, Vector2 position, Vector2 size, int fontSize = 24)
        {
            var textGO = new GameObject(name);
            textGO.transform.SetParent(parent.transform);
            
            var rect = textGO.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            
            return textGO;
        }
        
        static GameObject CreateButton(GameObject parent, string name, string label, Vector2 position, Vector2 size)
        {
            var btnGO = new GameObject(name);
            btnGO.transform.SetParent(parent.transform);
            
            var rect = btnGO.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            var image = btnGO.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.2f, 0.6f, 1f);
            
            var button = btnGO.AddComponent<UnityEngine.UI.Button>();
            
            // Add text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(btnGO.transform);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = size;
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 32;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            
            return btnGO;
        }
        
        static void CreateTestEnvironment()
        {
            Debug.Log("[SceneCreator] Creating test environment...");
            
            // Create some cubes representing cameras
            string[] cameraNames = { "CAM-001", "CAM-002", "CAM-003", "CAM-004", "CAM-005" };
            Vector3[] positions = {
                new Vector3(3, 1, 5),
                new Vector3(-2, 1.5f, 8),
                new Vector3(0, 2, 12),
                new Vector3(5, 1.2f, 7),
                new Vector3(-4, 1, 10)
            };
            
            for (int i = 0; i < cameraNames.Length; i++)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = cameraNames[i];
                cube.transform.position = positions[i];
                cube.transform.localScale = Vector3.one * 0.5f;
                
                // Add label
                var labelGO = new GameObject($"{cameraNames[i]}_Label");
                labelGO.transform.position = positions[i] + Vector3.up * 0.5f;
                
                Debug.Log($"[SceneCreator] Created test camera: {cameraNames[i]} at {positions[i]}");
            }
            
            Debug.Log("[SceneCreator] Test environment complete!");
        }
        
        [MenuItem("Tools/CCTV Survey/Create Test CSV")]
        public static void CreateTestCsv()
        {
            string csvContent = @"Site,Device Name,Device Type,System Type,Description,Barcode,X,Y,IP Address,VMS Zone,Manufacturer,Model,Photo URL,Photo Path,Capture Timestamp,Capture Method,GPS Accuracy,Review Status
2996,CAM-001,Dome Camera,CCTV,Main entrance dome camera,,,,192.168.1.101,Front_Entrance,Axis,P3245-LV,,,,,,PENDING
2996,CAM-002,Bullet Camera,CCTV,North parking lot view,,,,192.168.1.102,Parking_North,Hikvision,DS-2CD2T85G1-I5,,,,,,PENDING
2996,CAM-003,PTZ Camera,CCTV,Loading dock PTZ,,,,192.168.1.103,Loading_Dock,Axis,Q6155-E,,,,,,PENDING
2996,CAM-004,Turret Camera,CCTV,East warehouse exterior,,,,192.168.1.104,Warehouse_East,Dahua,IPC-HDW5841T-ZE,,,,,,PENDING
2996,CAM-005,Fisheye Camera,CCTV,Reception area 360 view,,,,192.168.1.105,Reception,Axis,M3057-PLVE,,,,,,PENDING";
            
            string path = "Assets/StreamingAssets/cameras.csv";
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            System.IO.File.WriteAllText(path, csvContent);
            
            Debug.Log($"[SceneCreator] Test CSV created: {path}");
            
            AssetDatabase.Refresh();
        }
    }
}
