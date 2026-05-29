using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Battle シーンに BattleSceneBootstrapper をワンクリックで配置するエディターユーティリティ。
/// メニュー: Tools → Setup Battle Scene
/// </summary>
public class BattleSceneSetupHelper : EditorWindow
{
    [MenuItem("Tools/Setup Battle Scene")]
    public static void SetupBattleScene()
    {
        // Currently open scene path
        string currentScenePath = EditorSceneManager.GetActiveScene().path;

        // Open or save prompt
        if (EditorSceneManager.GetActiveScene().name != "Battle")
        {
            bool open = EditorUtility.DisplayDialog(
                "Setup Battle Scene",
                "現在「Battle」シーンが開いていません。\n" +
                "Battle.unity を開いてセットアップしますか？",
                "開く & セットアップ", "キャンセル");

            if (!open) return;

            // Save current scene
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            // Open Battle scene
            EditorSceneManager.OpenScene("Assets/Scenes/Battle.unity");
        }

        // Now we are in Battle scene - set it up
        SetupBattleSceneInternal();
    }

    private static void SetupBattleSceneInternal()
    {
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("HBR Battle Scene Setup");

        // 1. Create Bootstrapper object
        BattleSceneBootstrapper bootstrapper = FindFirstObjectByType<BattleSceneBootstrapper>();
        GameObject bsObj = null;
        if (bootstrapper == null)
        {
            bsObj = new GameObject("BattleSceneBootstrapper");
            bootstrapper = bsObj.AddComponent<BattleSceneBootstrapper>();
            Undo.RegisterCreatedObjectUndo(bsObj, "Create BattleSceneBootstrapper");
            Debug.Log("BattleSceneSetupHelper: Created BattleSceneBootstrapper.");
        }
        else
        {
            bsObj = bootstrapper.gameObject;
            Undo.RecordObject(bsObj, "Modify BattleSceneBootstrapper");
            Debug.Log("BattleSceneSetupHelper: Found existing BattleSceneBootstrapper.");
        }

        // 2. Create/find a simple directional light
        if (FindFirstObjectByType<Light>() == null)
        {
            GameObject lightObj = new GameObject("Directional Light");
            Light dl = lightObj.AddComponent<Light>();
            dl.type = LightType.Directional;
            dl.color = new Color(0.8f, 0.85f, 1f);
            dl.intensity = 1.1f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Undo.RegisterCreatedObjectUndo(lightObj, "Create Battle Light");
        }

        // 3. Ensure main camera
        Camera cam = Camera.main;

        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
            Undo.RegisterCreatedObjectUndo(camObj, "Create Main Camera");

        }

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.05f, 0.10f, 1f);
        cam.fieldOfView = 60f;
        // Set the camera to HBR diagonal over-the-shoulder perspective in the editor
        cam.transform.position = new Vector3(-6.5f, 4.5f, -11.0f);
        cam.transform.rotation = Quaternion.Euler(18f, 32f, 0f);

        // Add CameraFollow if missing
        if (cam.GetComponent<CameraFollow>() == null)
        {
            Undo.AddComponent<CameraFollow>(cam.gameObject);
        }

        // 4. NPC Dialogue (optional) for cutscene in Battle scene
        NPCDialogue npcDlg = FindFirstObjectByType<NPCDialogue>();
        if (npcDlg == null)
        {
            GameObject npcObj = new GameObject("BattleNPCDialogue");
            npcDlg = npcObj.AddComponent<NPCDialogue>();
            npcDlg.characterName = "司令官";
            Undo.RegisterCreatedObjectUndo(npcObj, "Create Battle NPC Dialogue");
            Debug.Log("BattleSceneSetupHelper: Created NPCDialogue for Battle scene cutscene.");
        }

        // Assign font if GameManager has it (GameManager might already be loaded if editor was in SampleScene)
        GameManager gm = FindFirstObjectByType<GameManager>();
        if (gm != null && gm.customFont != null)
        {
            // Pass font to NPCDialogue
            Undo.RecordObject(npcDlg, "Assign Font to BattleNPCDialogue");
        }

        // Mark scene dirty and save
        EditorUtility.SetDirty(bootstrapper);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "Battle Scene Setup Complete",
            "Battle シーンのセットアップが完了しました！\n\n" +
            "・BattleSceneBootstrapper がシーンに配置されました\n" +
            "・カメラ・照明が設定されました\n" +
            "・NPCDialogue（カットシーン用）が配置されました\n\n" +
            "Talkでプレイを開始し、会話を進めると自動的にBattleシーンに切り替わります。",
            "OK");
    }
}
