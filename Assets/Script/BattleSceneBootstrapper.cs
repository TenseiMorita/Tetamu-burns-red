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
        bool createdNewCamera = false;
        if (cam == null)
        {
            GameObject camObj = new GameObject("MainCamera");
            camObj.tag = "MainCamera";
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
            createdNewCamera = true;
        }

        // 背景を HBR 風のダーク Navy に
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.05f, 0.10f, 1f);
        cam.fieldOfView = 60f;

        // CameraFollow がなければ追加
        CameraFollow cf = cam.GetComponent<CameraFollow>();
        if (cf == null) cf = cam.gameObject.AddComponent<CameraFollow>();

        // Keep editor-authored transform for existing cameras.
        // Only assign fallback transform when camera was created at runtime.
        if (createdNewCamera)
        {
            cam.transform.position = new Vector3(0f, 5f, -10f);
            cam.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
        }
    }

    private void EnsureGround()
    {
        // Destroy any procedurally generated BattleGround if it exists
        GameObject oldBattleGround = GameObject.Find("BattleGround");
        if (oldBattleGround != null)
        {
            Destroy(oldBattleGround);
        }

        // Enable the kandaiComplete school stage model placed in the Battle scene hierarchy
        GameObject kandaiStage = GameObject.Find("kandaiComplete");
        if (kandaiStage != null)
        {
            kandaiStage.SetActive(true);
            Debug.Log("BattleSceneBootstrapper: kandaiComplete stage enabled (keeping scene transform).");
        }
        else
        {
            Debug.LogWarning("BattleSceneBootstrapper: kandaiComplete not found! Falling back to simple ground.");
            // Fallback: create a simple ground plane
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "BattleGround";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(10f, 1f, 10f);
            Material grMat = new Material(Shader.Find("Standard"));
            grMat.color = new Color(0.48f, 0.42f, 0.35f, 1f);
            ground.GetComponent<Renderer>().sharedMaterial = grMat;
        }
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

        Camera cam = Camera.main;
        float battleY = 0.5f;

        // Viewport anchors chosen so all allies stay in-screen on the left side.
        Vector3 frontLeft  = GetWorldPointOnYPlane(cam, 0.30f, 0.35f, battleY);
        Vector3 frontMid   = GetWorldPointOnYPlane(cam, 0.38f, 0.35f, battleY);
        Vector3 frontRight = GetWorldPointOnYPlane(cam, 0.46f, 0.35f, battleY);
        Vector3 backLeft   = GetWorldPointOnYPlane(cam, 0.30f, 0.27f, battleY);
        Vector3 backMid    = GetWorldPointOnYPlane(cam, 0.38f, 0.27f, battleY);
        Vector3 backRight  = GetWorldPointOnYPlane(cam, 0.46f, 0.27f, battleY);

        Vector3[] allyPositions =
        {
            frontLeft, frontMid, frontRight,
            backLeft,  backMid,  backRight
        };

        string[] names = { "主人公", "戦友NPC 1", "戦友NPC 2", "戦友NPC 3", "戦友NPC 4", "戦友NPC 5" };

        for (int i = 0; i < 6; i++)
        {
            GameObject ally = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ally.name = "BattleAlly_" + i;
            ally.transform.position = allyPositions[i];

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

        Camera cam = Camera.main;
        float battleY = 0.0f;
        Vector3 enemyPosA = GetWorldPointOnYPlane(cam, 0.62f, 0.33f, battleY);
        Vector3 enemyPosB = GetWorldPointOnYPlane(cam, 0.72f, 0.33f, battleY);

        for (int i = 0; i < 2; i++)
        {
            GameObject enemy = new GameObject("BattleEnemy_" + i);
            enemy.transform.position = (i == 0) ? enemyPosA : enemyPosB;
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Model";
            capsule.transform.SetParent(enemy.transform, false);
            capsule.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            capsule.transform.localScale = new Vector3(1.2f, 1.6f, 1.2f);
            Destroy(capsule.GetComponent<Collider>());

            Material enemyMat = new Material(Shader.Find("Standard"));
            enemyMat.color = new Color(0.55f, 0.20f, 0.90f, 1f);
            enemyMat.SetFloat("_Metallic", 0.25f);
            enemyMat.SetFloat("_Glossiness", 0.8f);
            enemyMat.EnableKeyword("_EMISSION");
            enemyMat.SetColor("_EmissionColor", new Color(0.30f, 0.05f, 0.45f));
            capsule.GetComponent<Renderer>().sharedMaterial = enemyMat;

            enemy.tag = "Respawn";
            enemyTransforms.Add(enemy.transform);
        }
    }

    private Vector3 GetWorldPointOnYPlane(Camera cam, float viewportX, float viewportY, float yPlane)
    {
        if (cam == null)
        {
            return new Vector3(0f, yPlane, 0f);
        }

        Ray ray = cam.ViewportPointToRay(new Vector3(viewportX, viewportY, 0f));
        Plane plane = new Plane(Vector3.up, new Vector3(0f, yPlane, 0f));
        if (plane.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }

        // Safe fallback if ray/plane do not intersect (extreme camera angle).
        return new Vector3(0f, yPlane, 0f);
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
