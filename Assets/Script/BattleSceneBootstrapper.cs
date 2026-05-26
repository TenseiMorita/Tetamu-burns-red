using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Battleシーンに配置する自動セットアップスクリプト。
/// GameManagerがDontDestroyOnLoadで持続するため、Battle専用の
/// カメラ・EventSystem・アライ/エネミーTransformをここで準備し、
/// GameManagerに渡す橋渡し役を担う。
/// </summary>
public class BattleSceneBootstrapper : MonoBehaviour
{
    [Header("Battle Setup")]
    public List<Transform> allyTransforms  = new List<Transform>();
    public List<Transform> enemyTransforms = new List<Transform>();

    [Header("Visual")]
    public Material groundMaterial;
    public Material skyboxMaterial;

    void Awake()
    {
        // 1. EventSystem が存在しない場合は自動生成（マウスクリック必須）
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
            Debug.Log("BattleSceneBootstrapper: Created EventSystem.");
        }

        // 2. カメラのセットアップ
        EnsureCamera();

        // 3. 地面の生成
        EnsureGround();

        // 4. アライ Transform の生成（戦闘キャラ用プレースホルダー）
        BuildAllyTransforms();

        // 5. エネミー Transform の生成
        BuildEnemyTransforms();
    }

    private void EnsureCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("MainCamera");
            camObj.tag = "MainCamera";
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }

        // 背景を HBR 風のダーク Navy に
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.05f, 0.10f, 1f);
        cam.fieldOfView = 60f;

        // CameraFollow がなければ追加
        CameraFollow cf = cam.GetComponent<CameraFollow>();
        if (cf == null) cf = cam.gameObject.AddComponent<CameraFollow>();

        // Battle シーン用の俯瞰アングル
        cam.transform.position = new Vector3(0f, 5f, -10f);
        cam.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
    }

    private void EnsureGround()
    {
        // シンプルな床プレーンの代わりに校庭（Schoolyard）の背景と地面を構築
        if (GameObject.Find("BattleGround") != null) return;

        GameObject schoolyardParent = new GameObject("BattleGround");
        schoolyardParent.transform.position = Vector3.zero;

        // 1. 校庭の地面 (Y = 0fにピッタリ設置するため、薄い板にするかPlaneを使用)
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.parent = schoolyardParent.transform;
        ground.transform.localPosition = new Vector3(0f, 0f, 0f);
        ground.transform.localScale = new Vector3(10f, 1f, 10f); // 100x100m
        
        Material grMat = new Material(Shader.Find("Standard"));
        grMat.color = new Color(0.48f, 0.42f, 0.35f, 1f); // 校庭の砂土色
        grMat.SetFloat("_Glossiness", 0.05f);
        ground.GetComponent<Renderer>().sharedMaterial = grMat;

        // 2. 校舎の背景壁 (十分に奥にずらす Z = 15f)
        GameObject schoolWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        schoolWall.name = "SchoolBuildingWall";
        schoolWall.transform.parent = schoolyardParent.transform;
        schoolWall.transform.localPosition = new Vector3(0f, 6f, 15f);
        schoolWall.transform.localScale = new Vector3(80f, 12f, 2f);
        
        Material wallMat = new Material(Shader.Find("Standard"));
        wallMat.color = new Color(0.85f, 0.82f, 0.78f, 1f);
        wallMat.SetFloat("_Glossiness", 0.5f);
        schoolWall.GetComponent<Renderer>().sharedMaterial = wallMat;

        // 窓の生成
        for (int x = -6; x <= 6; x++)
        {
            if (x == 0) continue;
            for (int y = 1; y <= 2; y++)
            {
                GameObject win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                win.name = "BackgroundWindow";
                win.transform.parent = schoolWall.transform;
                win.transform.localPosition = new Vector3(x * 5f, y * 3f - 6f, -1.05f);
                win.transform.localScale = new Vector3(2.5f, 1.8f, 0.1f);
                
                Material winMat = new Material(Shader.Find("Standard"));
                winMat.color = new Color(0.2f, 0.4f, 0.6f, 1f);
                win.GetComponent<Renderer>().sharedMaterial = winMat;
            }
        }

        // 3. 左右のフェンス (十分に外側に配置)
        GameObject fenceLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fenceLeft.name = "FenceLeft";
        fenceLeft.transform.parent = schoolyardParent.transform;
        fenceLeft.transform.localPosition = new Vector3(-35f, 1f, 0f);
        fenceLeft.transform.localScale = new Vector3(0.2f, 2f, 30f);
        Material fenceMat = new Material(Shader.Find("Standard"));
        fenceMat.color = new Color(0.5f, 0.55f, 0.6f, 1f);
        fenceLeft.GetComponent<Renderer>().sharedMaterial = fenceMat;

        GameObject fenceRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fenceRight.name = "FenceRight";
        fenceRight.transform.parent = schoolyardParent.transform;
        fenceRight.transform.localPosition = new Vector3(35f, 1f, 0f);
        fenceRight.transform.localScale = new Vector3(0.2f, 2f, 30f);
        fenceRight.GetComponent<Renderer>().sharedMaterial = fenceMat;
    }

    private void BuildGridLines()
    {
        GameObject linesParent = new GameObject("GridLines");

        for (int i = -4; i <= 4; i += 2)
        {
            CreateLine(linesParent.transform,
                new Vector3(i, 0.01f, -8f), new Vector3(i, 0.01f, 8f),
                new Color(0f, 0.6f, 1f, 0.3f));
        }
        for (int j = -7; j <= 7; j += 2)
        {
            CreateLine(linesParent.transform,
                new Vector3(-10f, 0.01f, j), new Vector3(10f, 0.01f, j),
                new Color(0f, 0.6f, 1f, 0.15f));
        }
    }

    private void CreateLine(Transform parent, Vector3 from, Vector3 to, Color color)
    {
        GameObject lineObj = new GameObject("Line");
        lineObj.transform.SetParent(parent, false);
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
        lr.startWidth = 0.04f;
        lr.endWidth   = 0.04f;
        lr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor   = color;
        lr.useWorldSpace = true;
    }

    private void BuildAllyTransforms()
    {
        if (allyTransforms.Count > 0) return; // Already set from Inspector

        string[] names = { "主人公", "戦友NPC 1", "戦友NPC 2", "戦友NPC 3", "戦友NPC 4", "戦友NPC 5" };

        for (int i = 0; i < 6; i++)
        {
            GameObject ally = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ally.name = "BattleAlly_" + i;
            ally.transform.position = new Vector3(-2f - (i % 3) * 1.5f, 0.5f, -1f + (i / 3) * 2f);

            // Assign distinct color material
            Renderer rend = ally.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Standard"));
            Color[] colors = { Color.cyan, Color.magenta, Color.yellow, Color.red, Color.blue, Color.green };
            mat.color = colors[i % colors.Length];
            mat.SetFloat("_Metallic", 0.4f);
            mat.SetFloat("_Glossiness", 0.6f);
            rend.sharedMaterial = mat;

            Destroy(ally.GetComponent<Collider>());

            allyTransforms.Add(ally.transform);
        }
    }

    private void BuildEnemyTransforms()
    {
        if (enemyTransforms.Count > 0) return; // Already set from Inspector

        for (int i = 0; i < 2; i++)
        {
            GameObject enemy = new GameObject("BattleEnemy_" + i);
            enemy.transform.position = new Vector3(3f + i * 2.5f, 0f, 0f);

            // Visual container
            GameObject visualParent = new GameObject("Visual");
            visualParent.transform.SetParent(enemy.transform, false);

            // 1. Central Core
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "Core";
            core.transform.SetParent(visualParent.transform, false);
            core.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            core.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
            Destroy(core.GetComponent<Collider>());

            Material coreMat = new Material(Shader.Find("Standard"));
            coreMat.color = new Color(0.1f, 0.05f, 0.15f, 1f); // Deep obsidian
            coreMat.SetFloat("_Glossiness", 0.8f);
            coreMat.EnableKeyword("_EMISSION");
            coreMat.SetColor("_EmissionColor", new Color(0.4f, 0f, 0f));
            core.GetComponent<Renderer>().sharedMaterial = coreMat;

            // Glowing eye
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = "GlowingEye";
            eye.transform.SetParent(core.transform, false);
            eye.transform.localPosition = new Vector3(0f, 0f, -0.45f);
            eye.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            Destroy(eye.GetComponent<Collider>());

            Material eyeMat = new Material(Shader.Find("Standard"));
            eyeMat.color = Color.red;
            eyeMat.EnableKeyword("_EMISSION");
            eyeMat.SetColor("_EmissionColor", new Color(2f, 0f, 0f));
            eye.GetComponent<Renderer>().sharedMaterial = eyeMat;

            // 2. Shell Ring
            GameObject shellY = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shellY.name = "ShellY";
            shellY.transform.SetParent(core.transform, false);
            shellY.transform.localPosition = Vector3.zero;
            shellY.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            shellY.transform.localScale = new Vector3(1.2f, 0.1f, 1.2f);
            Destroy(shellY.GetComponent<Collider>());

            Material shellMat = new Material(Shader.Find("Standard"));
            shellMat.color = new Color(0.2f, 0.22f, 0.25f, 1f);
            shellY.GetComponent<Renderer>().sharedMaterial = shellMat;

            // 3. Spooky Legs/Claws
            for (int c = 0; c < 5; c++)
            {
                float angle = c * 72f * Mathf.Deg2Rad;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                GameObject legParent = new GameObject("Leg_" + c);
                legParent.transform.SetParent(visualParent.transform, false);
                legParent.transform.localPosition = new Vector3(cos * 0.9f, 1.3f, sin * 0.9f);

                // Segment 1
                GameObject seg1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seg1.transform.SetParent(legParent.transform, false);
                seg1.transform.localPosition = new Vector3(cos * 0.4f, 0.2f, sin * 0.4f);
                seg1.transform.localRotation = Quaternion.LookRotation(new Vector3(cos, 0.3f, sin));
                seg1.transform.localScale = new Vector3(0.18f, 0.18f, 0.8f);
                Destroy(seg1.GetComponent<Collider>());
                seg1.GetComponent<Renderer>().sharedMaterial = shellMat;

                // Segment 2
                GameObject seg2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seg2.transform.SetParent(legParent.transform, false);
                seg2.transform.localPosition = new Vector3(cos * 0.8f, -0.6f, sin * 0.8f);
                seg2.transform.localRotation = Quaternion.LookRotation(new Vector3(0f, -1f, 0f));
                seg2.transform.localScale = new Vector3(0.12f, 0.12f, 1.2f);
                Destroy(seg2.GetComponent<Collider>());
                seg2.GetComponent<Renderer>().sharedMaterial = shellMat;

                // Tip
                GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                tip.transform.SetParent(seg2.transform, false);
                tip.transform.localPosition = new Vector3(0f, 0f, 0.6f);
                tip.transform.localScale = new Vector3(2f, 2f, 2f);
                Destroy(tip.GetComponent<Collider>());
                tip.GetComponent<Renderer>().sharedMaterial = eyeMat;
            }

            enemy.tag = "Respawn";
            enemyTransforms.Add(enemy.transform);
        }
    }

    // ============================================================
    // Lighting for Battle scene
    // ============================================================
    void Start()
    {
        SetupBattleLighting();
    }

    private void SetupBattleLighting()
    {
        // Main directional light
        Light existingLight = FindObjectOfType<Light>();
        if (existingLight == null)
        {
            GameObject lightObj = new GameObject("DirectionalLight");
            Light dl = lightObj.AddComponent<Light>();
            dl.type = LightType.Directional;
            dl.color = new Color(0.8f, 0.85f, 1f);
            dl.intensity = 1.1f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        // Ambient: cool dark blue atmosphere
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.07f, 0.09f, 0.14f);

        // Fog for depth
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.04f, 0.05f, 0.09f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 20f;
        RenderSettings.fogEndDistance = 60f;
    }
}
