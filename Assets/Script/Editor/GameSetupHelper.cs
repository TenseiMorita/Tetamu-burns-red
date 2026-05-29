using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;

[InitializeOnLoad]
public class GameSetupHelper : EditorWindow
{
    static GameSetupHelper()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            // Execute absolute dynamic auto-setup seamlessly right before the scene runs!
            AutoSetupInternal(true);
        }
    }

    [MenuItem("Tools/Auto-Setup HBR Scenario")]
    public static void AutoSetup()
    {
        AutoSetupInternal(false);
    }

    public static void AutoSetupInternal(bool isSilent)
    {
        if (isSilent)
        {
            Debug.Log("GameSetupHelper: Performing silent pre-flight scene setup...");
        }
        else
        {
            Debug.Log("Starting Deep Clean Auto-Setup for Heaven Burns Red scenario...");
        }

        // AUTO-REGISTER SCENES IN BUILD SETTINGS
        EnsureScenesInBuildSettings();


        // Register Undo for scene modification
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("HBR Scenario Deep Setup");

        // 1. Create/Setup Player GameObject
        PlayerMove player = FindFirstObjectByType<PlayerMove>();
        GameObject playerObj = null;
        if (player != null)
        {
            playerObj = player.gameObject;
        }
        else
        {
            playerObj = GameObject.Find("Player");
        }

        if (playerObj == null)
        {
            playerObj = new GameObject("Player");
            player = playerObj.AddComponent<PlayerMove>();
            Undo.RegisterCreatedObjectUndo(playerObj, "Create Player");
            
            // Add visual Capsule child
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "VisualCapsule";
            visual.transform.parent = playerObj.transform;
            visual.transform.localPosition = new Vector3(0f, 1f, 0f);
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            DestroyImmediate(visual.GetComponent<Collider>());
            Debug.Log("Created new Player GameObject with visual capsule.");
        }
        else
        {
            player = GetOrAddComponent<PlayerMove>(playerObj);
            Undo.RecordObject(playerObj, "Setup Player");
            Debug.Log("Found existing Player GameObject.");
        }

        playerObj.name = "Player";
        playerObj.tag = "Player";
        CharacterController charCtrl = GetOrAddComponent<CharacterController>(playerObj);
        charCtrl.height = 2.0f;
        charCtrl.radius = 0.5f;
        charCtrl.center = new Vector3(0f, 1.0f, 0f);

        // Remove conflicting colliders on player root GameObject
        foreach (var c in playerObj.GetComponents<Collider>())
        {
            if (c != charCtrl)
            {
                DestroyImmediate(c);
                Debug.Log("Removed conflicting Collider component from Player root.");
            }
        }
        Rigidbody rb = playerObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            DestroyImmediate(rb);
            Debug.Log("Removed conflicting Rigidbody component from Player root.");
        }

        // Set player position safely above the floor
        playerObj.transform.position = new Vector3(0f, 0.5f, 2.0f);
        playerObj.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // face right

        // 2. Create/Setup Comrade NPC GameObject
        NPCDialogue npc = FindFirstObjectByType<NPCDialogue>();
        GameObject npcObj = null;
        if (npc != null)
        {
            npcObj = npc.gameObject;
        }
        else
        {
            npcObj = GameObject.Find("NPC");
        }

        if (npcObj == null)
        {
            npcObj = new GameObject("NPC");
            npc = npcObj.AddComponent<NPCDialogue>();
            Undo.RegisterCreatedObjectUndo(npcObj, "Create NPC");
            
            // Add visual Capsule child (Red color to distinguish from player)
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "VisualCapsule";
            visual.transform.parent = npcObj.transform;
            visual.transform.localPosition = new Vector3(0f, 1f, 0f);
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            visual.GetComponent<Renderer>().sharedMaterial.color = Color.red;
            DestroyImmediate(visual.GetComponent<Collider>());
            Debug.Log("Created new NPC GameObject with visual capsule.");
        }
        else
        {
            npc = GetOrAddComponent<NPCDialogue>(npcObj);
            Undo.RecordObject(npcObj, "Setup NPC");
            Debug.Log("Found existing NPC GameObject.");
        }

        npcObj.name = "NPC";
        SphereCollider npcTrigger = GetOrAddComponent<SphereCollider>(npcObj);
        npcTrigger.isTrigger = true;
        npcTrigger.radius = 2.5f;
        npcTrigger.center = new Vector3(0f, 1.0f, 0f);

        // Remove conflicting colliders on NPC root GameObject (except our sphere trigger)
        foreach (var c in npcObj.GetComponents<Collider>())
        {
            if (c != npcTrigger)
            {
                DestroyImmediate(c);
                Debug.Log("Removed conflicting Collider component from NPC root.");
            }
        }
        Rigidbody npcRb = npcObj.GetComponent<Rigidbody>();
        if (npcRb != null)
        {
            DestroyImmediate(npcRb);
            Debug.Log("Removed conflicting Rigidbody component from NPC root.");
        }

        // Set NPC position safely above the floor
        npcObj.transform.position = new Vector3(3f, 0.5f, 2.0f);
        npcObj.transform.rotation = Quaternion.Euler(0f, -90f, 0f); // face left

        // 3. Create/Setup Camera
        CameraFollow camFollow = FindFirstObjectByType<CameraFollow>();
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("MainCamera");
            mainCam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            Undo.RegisterCreatedObjectUndo(camObj, "Create MainCamera");
        }
        camFollow = GetOrAddComponent<CameraFollow>(mainCam.gameObject);
        Undo.RecordObject(camFollow, "Setup CameraFollow");
        camFollow.target = player.transform;
        camFollow.normalOffset = new Vector3(0f, 2.5f, -5f);
        camFollow.talkOffset = new Vector3(1.5f, 1.8f, -2.5f);
        camFollow.isTalking = false;

        // 4. Create/Setup GameManager
        GameObject gmObj = GameObject.Find("GameManager");
        if (gmObj == null)
        {
            gmObj = new GameObject("GameManager");
            Undo.RegisterCreatedObjectUndo(gmObj, "Create GameManager");
            Debug.Log("Created new GameManager GameObject.");
        }
        else
        {
            Undo.RecordObject(gmObj, "Modify GameManager");
            Debug.Log("Found existing GameManager GameObject.");
        }

        // Align GameManager to zero
        gmObj.transform.position = Vector3.zero;
        gmObj.transform.rotation = Quaternion.identity;
        gmObj.transform.localScale = Vector3.one;

        GameManager gm = GetOrAddComponent<GameManager>(gmObj);
        EmergencyAlertSystem alert = GetOrAddComponent<EmergencyAlertSystem>(gmObj);
        WeaponSummoner summoner = GetOrAddComponent<WeaponSummoner>(gmObj);
        BattleManager battle = GetOrAddComponent<BattleManager>(gmObj);
        BattleUI ui = GetOrAddComponent<BattleUI>(gmObj);

        // Create/Find separate CorridorGenerator GameObject
        GameObject corrGenObj = GameObject.Find("CorridorGenerator");
        if (corrGenObj == null)
        {
            corrGenObj = new GameObject("CorridorGenerator");
            Undo.RegisterCreatedObjectUndo(corrGenObj, "Create CorridorGenerator");
        }
        corrGenObj.transform.position = Vector3.zero;
        corrGenObj.transform.rotation = Quaternion.identity;
        corrGenObj.transform.localScale = Vector3.one;
        CorridorGenerator corrGen = GetOrAddComponent<CorridorGenerator>(corrGenObj);

        // Create/Find separate SchoolyardGenerator GameObject
        GameObject yardGenObj = GameObject.Find("SchoolyardGenerator");
        if (yardGenObj == null)
        {
            yardGenObj = new GameObject("SchoolyardGenerator");
            Undo.RegisterCreatedObjectUndo(yardGenObj, "Create SchoolyardGenerator");
        }
        yardGenObj.transform.position = new Vector3(0f, 0f, -150f);
        yardGenObj.transform.rotation = Quaternion.identity;
        yardGenObj.transform.localScale = Vector3.one;
        SchoolyardGenerator yardGen = GetOrAddComponent<SchoolyardGenerator>(yardGenObj);

        // Remove any obsolete generators from GameManager GameObject if they exist
        CorridorGenerator oldCorrGen = gmObj.GetComponent<CorridorGenerator>();
        if (oldCorrGen != null) DestroyImmediate(oldCorrGen);
        SchoolyardGenerator oldYardGen = gmObj.GetComponent<SchoolyardGenerator>();
        if (oldYardGen != null) DestroyImmediate(oldYardGen);

        // Connect GameManager references
        Undo.RecordObject(gm, "Connect GameManager References");
        gm.playerMove = player;
        gm.cameraFollow = camFollow;
        gm.npcDialogue = npc;
        gm.corridorGenerator = corrGen;
        gm.schoolyardGenerator = yardGen;
        gm.alertSystem = alert;
        gm.weaponSummoner = summoner;
        gm.battleManager = battle;
        gm.battleUI = ui;

        // Configure generator properties
        corrGen.segmentCount = 8;
        corrGen.segmentWidth = 6f;
        corrGen.corridorHeight = 4f;
        corrGen.corridorDepth = 4f;

        // Connect Dialogue references
        Undo.RecordObject(npc, "Connect NPC Dialogue References");
        npc.gameManager = gm;
        npc.playerMove = player;
        npc.cameraFollow = camFollow;

        // Automatically find and assign dialogue panel (active or inactive in scene)
        if (npc.dialoguePanel == null)
        {
            var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj != null && (obj.name == "dialoguePanel" || obj.name == "DialoguePanel" || obj.name == "Dialogue Panel"))
                {
                    if (EditorUtility.IsPersistent(obj)) continue; // ignore project prefabs
                    npc.dialoguePanel = obj;
                    break;
                }
            }
        }

        // Deactivate dialogue panel at setup time so it starts hidden and doesn't display raw placeholders!
        if (npc.dialoguePanel != null)
        {
            Undo.RecordObject(npc.dialoguePanel, "Deactivate Dialogue Panel");
            npc.dialoguePanel.SetActive(false);
            Debug.Log("GameSetupHelper: Found and deactivated dialoguePanel to prevent startup placeholder clipping.");
        }

        // Connect BattleManager references
        Undo.RecordObject(battle, "Connect BattleManager References");
        battle.cameraFollow = camFollow;

        // Connect Font Asset - Use dynamic Japanese SDF font to guarantee full hiragana/katakana and kanji rendering
        TMPro.TMP_FontAsset customFont = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/SU3DJPFont/TextMeshProFont/Dynamic/mplus-1p-medium SDF Dynamic.asset");
        if (customFont != null)
        {
            gm.customFont = customFont;
            corrGen.customFont = customFont;
            Debug.Log("Assigned Japanese Dynamic SDF font.");

            // Update scene UI fonts
            var allSceneUI = FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None);
            foreach (var textElem in allSceneUI)
            {
                Undo.RecordObject(textElem, "Assign Custom UI Font");
                textElem.font = customFont;
                EditorUtility.SetDirty(textElem);
            }

            var allScene3D = FindObjectsByType<TMPro.TextMeshPro>(FindObjectsSortMode.None);
            foreach (var textElem in allScene3D)
            {
                Undo.RecordObject(textElem, "Assign Custom 3D Font");
                textElem.font = customFont;
                EditorUtility.SetDirty(textElem);
            }
        }
        else
        {
            Debug.LogWarning("Japanese Dynamic SDF Font asset not found.");
        }

        PopulateDefaultMaterials(corrGen, yardGen);

        // 5. Hide original scene environment objects right in the Editor!
        string[] originalObjNames = {
            "Ground",
            "Classroom corridor wall Right",
            "Classroom corridor wall Left",
            "Classroom corridor doar"
        };
        foreach (string name in originalObjNames)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                Undo.RecordObject(obj, "Deactivate scene plain obj");
                obj.SetActive(false);
                Debug.Log("Auto-Setup: Hidden conflicting plain scene object: " + name);
            }
        }

        // 6. Generate robust environments instantly in the Editor!
        corrGen.Generate();
        
        // Setup schoolyard offset and generate
        yardGen.transform.position = new Vector3(0f, 0f, -150f);
        yardGen.Generate();
        GameObject schoolyardObj = GameObject.Find("Generated_Procedural_Schoolyard");
        if (schoolyardObj != null)
        {
            schoolyardObj.SetActive(false); // Hide the schoolyard initially
        }

        // Force save changes and repaint editor
        EditorUtility.SetDirty(gm);
        EditorUtility.SetDirty(npc);
        EditorUtility.SetDirty(battle);
        
        // Permanently mark the scene as dirty and save it!
        // This ensures the deleted old ground and new procedural generator colliders are locked in
        // and Unity won't discard them when you click 'Play'!
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("★ Clean Auto-Setup Completed Successfully and Scene Saved! ★");
        if (!isSilent)
        {
            EditorUtility.DisplayDialog("HBR Auto-Setup Success", 
                "Heaven Burns Red scenario auto-setup completed successfully!\n\n" +
                "A clean, robust, and correctly configured Player and NPC have been built. Conflicting Rigidbodies/Colliders have been cleaned up.\n\n" +
                "SampleScene and Battle are registered in Build Settings.\n\n" +
                "The procedurally generated corridor is now fully visible in your scene! Click 'Play' to test the full gameplay flow!", 
                "Awesome!");
        }
    }

    private static void EnsureScenesInBuildSettings()
    {
        string[] requiredScenes = {
            "Assets/Scenes/SampleScene.unity",
            "Assets/Scenes/Battle.unity"
        };

        var existing = EditorBuildSettings.scenes.ToList();
        bool changed = false;

        foreach (string scenePath in requiredScenes)
        {
            bool alreadyIncluded = existing.Any(s => s.path == scenePath);
            if (!alreadyIncluded)
            {
                // Check the file actually exists
                if (System.IO.File.Exists(scenePath))
                {
                    existing.Add(new EditorBuildSettingsScene(scenePath, true));
                    Debug.Log("GameSetupHelper: Added '" + scenePath + "' to Build Settings.");
                    changed = true;
                }
                else
                {
                    Debug.LogWarning("GameSetupHelper: Scene file not found: " + scenePath);
                }
            }
            else
            {
                // Make sure it's enabled
                var scene = existing.First(s => s.path == scenePath);
                if (!scene.enabled)
                {
                    scene.enabled = true;
                    changed = true;
                    Debug.Log("GameSetupHelper: Re-enabled '" + scenePath + "' in Build Settings.");
                }
            }
        }

        if (changed)
        {
            EditorBuildSettings.scenes = existing.ToArray();
            Debug.Log("GameSetupHelper: Build Settings updated with required scenes.");
        }
        else
        {
            Debug.Log("GameSetupHelper: All required scenes already in Build Settings.");
        }
    }

    private static T GetOrAddComponent<T>(GameObject obj) where T : Component
    {
        T comp = obj.GetComponent<T>();
        if (comp == null)
        {
            comp = obj.AddComponent<T>();
            Undo.RegisterCreatedObjectUndo(comp, "Add Component " + typeof(T).Name);
            Debug.Log("Added component: " + typeof(T).Name);
        }
        return comp;
    }

    private static void PopulateDefaultMaterials(CorridorGenerator corrGen, SchoolyardGenerator yardGen)
    {
        Material floorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/FloorMat.mat");
        Material doorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/Classroom corridor doar.mat");
        Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Stylized Texture Pack/Stylized Texture Pack 2k .png/Wood_Surface_01/Stylized_Wood_Planks.mat");

        if (floorMat != null) corrGen.floorMaterial = floorMat;
        else if (woodMat != null) corrGen.floorMaterial = woodMat;

        if (doorMat != null) corrGen.doorMaterial = doorMat;

        Material groundMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Stylized Texture Pack/Stylized Texture Pack 2k .png/Ground_Tile_01/Ground_Tile.mat");
        if (groundMat != null) yardGen.groundMaterial = groundMat;
    }
}
