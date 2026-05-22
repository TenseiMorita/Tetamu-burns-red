using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SchoolyardGenerator : MonoBehaviour
{
    [Header("Schoolyard Settings")]
    public float yardWidth = 50f;
    public float yardDepth = 20f;

    [Header("Materials (Optional)")]
    public Material groundMaterial;
    public Material buildingMaterial;
    public Material fenceMaterial;
    public Material enemyCoreMaterial;
    public Material enemyMetalMaterial;
    public Material commanderMaterial;

    private const string ParentName = "Generated_Procedural_Schoolyard";

    [ContextMenu("Generate Schoolyard")]
    public void Generate()
    {
        Clear();

        // Create main parent
        GameObject parentObj = new GameObject(ParentName);
        parentObj.transform.parent = this.transform;
        parentObj.transform.localPosition = Vector3.zero;
        parentObj.transform.localRotation = Quaternion.identity;

        InitDefaultMaterials();

        // 1. Generate Battlefield Ground (Thicker collider at Y = -0.5f to prevent clipping!)
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.parent = parentObj.transform;
        ground.transform.localPosition = new Vector3(0f, -0.5f, 0f);
        ground.transform.localScale = new Vector3(yardWidth, 1.0f, yardDepth);
        if (groundMaterial != null) ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

        // 2. Generate School Building Background Wall (at Z = yardDepth / 2)
        float wallZ = yardDepth / 2f - 0.5f;
        GameObject schoolBackground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        schoolBackground.name = "SchoolBuildingWall";
        schoolBackground.transform.parent = parentObj.transform;
        schoolBackground.transform.localPosition = new Vector3(0f, 6f, wallZ);
        schoolBackground.transform.localScale = new Vector3(yardWidth, 12f, 1f);
        if (buildingMaterial != null) schoolBackground.GetComponent<Renderer>().sharedMaterial = buildingMaterial;

        // Generate some procedural windows on the school background to make it look like a real building!
        for (int x = -4; x <= 4; x++)
        {
            if (x == 0) continue;
            for (int y = 1; y <= 2; y++)
            {
                GameObject win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                win.name = "BackgroundWindow";
                win.transform.parent = schoolBackground.transform;
                win.transform.localPosition = new Vector3(x * 5f / (yardWidth/10f), y * 3f / 12f - 0.2f, -0.55f);
                win.transform.localScale = new Vector3(0.06f, 0.15f, 0.1f); // relative scale
                win.GetComponent<Renderer>().sharedMaterial = CreateSafeMaterial(new Color(0.2f, 0.4f, 0.6f)); // Glassy blue
            }
        }

        // 3. Generate Wire Fences at the sides
        GenerateFence(parentObj.transform, new Vector3(-yardWidth / 2f + 0.5f, 1f, 0f), new Vector3(0.2f, 2f, yardDepth));
        GenerateFence(parentObj.transform, new Vector3(yardWidth / 2f - 0.5f, 1f, 0f), new Vector3(0.2f, 2f, yardDepth));

        // 4. Generate Commander GameObject (at X = -8, Y = 0, Z = 0)
        GameObject commander = CreateCommander(parentObj.transform);
        commander.transform.localPosition = new Vector3(-7f, 1f, -1f);
        commander.transform.localRotation = Quaternion.Euler(0f, 90f, 0f); // Looking towards the enemy

        // 5. Generate Cancer Enemy (at X = 5, Y = 1.5, Z = 0)
        GameObject enemy = CreateCancerEnemy(parentObj.transform);
        enemy.transform.localPosition = new Vector3(5f, 1.8f, 0f);
        enemy.transform.localRotation = Quaternion.Euler(0f, -90f, 0f); // Looking towards the player

        Debug.Log("Procedural Schoolyard (Battlefield) Generated Successfully!");
    }

    [ContextMenu("Clear Schoolyard")]
    public void Clear()
    {
        Transform existing = this.transform.Find(ParentName);
        if (existing != null)
        {
            DestroyImmediate(existing.gameObject);
            Debug.Log("Cleared existing schoolyard.");
        }
    }

    private void GenerateFence(Transform parent, Vector3 pos, Vector3 scale)
    {
        GameObject fence = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fence.name = "Fence";
        fence.transform.parent = parent;
        fence.transform.localPosition = pos;
        fence.transform.localScale = scale;
        if (fenceMaterial != null) fence.GetComponent<Renderer>().sharedMaterial = fenceMaterial;
    }

    private GameObject CreateCommander(Transform parent)
    {
        GameObject commObj = new GameObject("Commander");
        commObj.transform.parent = parent;

        // Navy Uniform Body (Cylinder)
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "Body";
        body.transform.parent = commObj.transform;
        body.transform.localPosition = new Vector3(0f, 0f, 0f);
        body.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
        if (commanderMaterial != null) body.GetComponent<Renderer>().sharedMaterial = commanderMaterial;
        else body.GetComponent<Renderer>().sharedMaterial = CreateSafeMaterial(new Color(0.1f, 0.15f, 0.3f)); // Navy Blue Uniform

        // Head (Sphere)
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.parent = commObj.transform;
        head.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        head.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        head.GetComponent<Renderer>().sharedMaterial = CreateSafeMaterial(new Color(0.95f, 0.8f, 0.7f)); // Skin tone

        // Commander Military Cap (Cylinder + Box)
        GameObject capBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        capBase.name = "MilitaryCapBase";
        capBase.transform.parent = head.transform;
        capBase.transform.localPosition = new Vector3(0f, 0.45f, 0f);
        capBase.transform.localRotation = Quaternion.Euler(5f, 0f, 0f);
        capBase.transform.localScale = new Vector3(1.1f, 0.15f, 1.1f);
        capBase.GetComponent<Renderer>().sharedMaterial = CreateSafeMaterial(new Color(0.08f, 0.12f, 0.25f)); // Deep navy cap

        GameObject capVisor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        capVisor.name = "MilitaryCapVisor";
        capVisor.transform.parent = capBase.transform;
        capVisor.transform.localPosition = new Vector3(0f, -0.2f, 0.45f);
        capVisor.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
        capVisor.transform.localScale = new Vector3(0.8f, 0.1f, 0.5f);
        capVisor.GetComponent<Renderer>().sharedMaterial = CreateSafeMaterial(Color.black); // Black polished visor

        // Long Hair (Back coat/hair shape)
        GameObject hair = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hair.name = "Hair";
        hair.transform.parent = head.transform;
        hair.transform.localPosition = new Vector3(0f, -0.3f, -0.2f);
        hair.transform.localScale = new Vector3(0.9f, 0.8f, 0.4f);
        hair.GetComponent<Renderer>().sharedMaterial = CreateSafeMaterial(new Color(0.18f, 0.1f, 0.1f)); // Dark brown/auburn hair

        // Note: Do NOT set custom tags like 'NPC' since they are not registered and cause crash.
        // The game successfully finds the commander using standard references.

        return commObj;
    }

    private GameObject CreateCancerEnemy(Transform parent)
    {
        GameObject enemyObj = new GameObject("CancerBoss");
        enemyObj.transform.parent = parent;

        // 1. Central Core: Dark metallic sphere with a glowing neon red core!
        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "Core";
        core.transform.parent = enemyObj.transform;
        core.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        core.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
        if (enemyCoreMaterial != null) core.GetComponent<Renderer>().sharedMaterial = enemyCoreMaterial;
        else
        {
            Material coreMat = CreateSafeMaterial(new Color(0.1f, 0.05f, 0.15f)); // Deep obsidian
            coreMat.EnableKeyword("_EMISSION");
            coreMat.SetColor("_EmissionColor", new Color(0.4f, 0f, 0f)); // Creepy dim red glow
            core.GetComponent<Renderer>().sharedMaterial = coreMat;
        }

        // Inner glowing eye (small sphere)
        GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = "GlowingEye";
        eye.transform.parent = core.transform;
        eye.transform.localPosition = new Vector3(0f, 0f, -0.45f);
        eye.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
        Material eyeMat = CreateSafeMaterial(Color.red);
        eyeMat.EnableKeyword("_EMISSION");
        eyeMat.SetColor("_EmissionColor", new Color(2f, 0f, 0f)); // Bright glaring red eye!
        eye.GetComponent<Renderer>().sharedMaterial = eyeMat;

        // 2. Surrounding Outer Rings / Orbits (Geometric Shells)
        GameObject shellY = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shellY.name = "ShellY";
        shellY.transform.parent = core.transform;
        shellY.transform.localPosition = Vector3.zero;
        shellY.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        shellY.transform.localScale = new Vector3(1.2f, 0.1f, 1.2f);
        if (enemyMetalMaterial != null) shellY.GetComponent<Renderer>().sharedMaterial = enemyMetalMaterial;
        else shellY.GetComponent<Renderer>().sharedMaterial = CreateSafeMaterial(new Color(0.2f, 0.22f, 0.25f)); // Gunmetal

        // 3. Floating Creepy geometric claws/legs
        System.Random rand = new System.Random();
        for (int c = 0; c < 5; c++)
        {
            float angle = c * 72f * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            GameObject legParent = new GameObject("Leg_" + c);
            legParent.transform.parent = enemyObj.transform;
            legParent.transform.localPosition = new Vector3(cos * 0.9f, 0.5f, sin * 0.9f);

            // segment 1: extending outwards
            GameObject seg1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg1.transform.parent = legParent.transform;
            seg1.transform.localPosition = new Vector3(cos * 0.4f, 0.2f, sin * 0.4f);
            seg1.transform.localRotation = Quaternion.LookRotation(new Vector3(cos, 0.3f, sin));
            seg1.transform.localScale = new Vector3(0.18f, 0.18f, 0.8f);
            if (enemyMetalMaterial != null) seg1.GetComponent<Renderer>().sharedMaterial = enemyMetalMaterial;
            else seg1.GetComponent<Renderer>().sharedMaterial = CreateSafeMaterial(new Color(0.15f, 0.17f, 0.2f));

            // segment 2: pointing down to the ground
            GameObject seg2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg2.transform.parent = legParent.transform;
            seg2.transform.localPosition = new Vector3(cos * 0.8f, -0.6f, sin * 0.8f);
            seg2.transform.localRotation = Quaternion.LookRotation(new Vector3(0f, -1f, 0f));
            seg2.transform.localScale = new Vector3(0.12f, 0.12f, 1.2f);
            seg2.GetComponent<Renderer>().sharedMaterial = CreateSafeMaterial(new Color(0.15f, 0.17f, 0.2f));
            
            // Glowing tips on claws
            GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tip.transform.parent = seg2.transform;
            tip.transform.localPosition = new Vector3(0f, 0f, 0.6f);
            tip.transform.localScale = new Vector3(2f, 2f, 2f);
            Material tipMat = CreateSafeMaterial(Color.red);
            tipMat.EnableKeyword("_EMISSION");
            tipMat.SetColor("_EmissionColor", new Color(1.2f, 0f, 0f));
            tip.GetComponent<Renderer>().sharedMaterial = tipMat;
        }

        // Add a floating script or tag so it is identified as the enemy in Battle!
        enemyObj.tag = "Respawn"; // We can find it or identify it easily

        return enemyObj;
    }

    private Material CreateSafeMaterial(Color color, float glossiness = 0.5f, float metallic = 0f)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Diffuse");
        if (shader == null) shader = Shader.Find("Diffuse");
        
        Material mat;
        if (shader != null)
        {
            mat = new Material(shader);
        }
        else
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Material defaultMat = temp.GetComponent<Renderer>().sharedMaterial;
            mat = new Material(defaultMat);
            DestroyImmediate(temp);
        }
        
        mat.color = color;
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", glossiness);
        else if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", glossiness);
        
        if (metallic > 0f)
        {
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        }
        return mat;
    }

    private void InitDefaultMaterials()
    {
        if (groundMaterial == null)
        {
            groundMaterial = CreateSafeMaterial(new Color(0.48f, 0.42f, 0.35f), 0.05f);
        }

        if (buildingMaterial == null)
        {
            buildingMaterial = CreateSafeMaterial(new Color(0.85f, 0.82f, 0.78f), 0.5f);
        }

        if (fenceMaterial == null)
        {
            fenceMaterial = CreateSafeMaterial(new Color(0.5f, 0.55f, 0.6f), 0.3f, 0.6f);
        }
    }
}

