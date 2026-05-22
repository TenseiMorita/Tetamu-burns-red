using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CorridorGenerator : MonoBehaviour
{
    [Header("Corridor Settings")]
    public int segmentCount = 10;
    public float segmentWidth = 6f;
    public float corridorHeight = 4f;
    public float corridorDepth = 4f;

    [Header("Materials (Optional - will auto-create if null)")]
    public Material floorMaterial;
    public Material wallMaterial;
    public Material pillarMaterial;
    public Material doorMaterial;
    public Material windowFrameMaterial;
    public Material glassMaterial;
    public Material ceilingMaterial;
    public Material lightFixtureMaterial;
    public Material noticeBoardMaterial;

    [Header("Prefabs (Optional)")]
    public GameObject doorPrefab;

    [Header("Custom Font")]
    public TMP_FontAsset customFont;

    private const string ParentName = "Generated_Procedural_Corridor";

    [ContextMenu("Generate Corridor")]
    public void Generate()
    {
        Clear();

        // Create main parent
        GameObject parentObj = new GameObject(ParentName);
        parentObj.transform.parent = this.transform;
        parentObj.transform.localPosition = Vector3.zero;
        parentObj.transform.localRotation = Quaternion.identity;

        // Initialize default materials if none are specified
        InitDefaultMaterials();

        float halfDepth = corridorDepth / 2f;
        float startX = -((segmentCount - 1) * segmentWidth) / 2f;

        // 1. Generate Floor (Thicker collider at Y = -0.5f to prevent clipping!)
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.parent = parentObj.transform;
        floor.transform.localPosition = new Vector3(0f, -0.5f, halfDepth);
        floor.transform.localScale = new Vector3(segmentCount * segmentWidth, 1.0f, corridorDepth);
        if (floorMaterial != null) floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;

        // 2. Generate Ceiling
        GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ceiling.name = "Ceiling";
        ceiling.transform.parent = parentObj.transform;
        ceiling.transform.localPosition = new Vector3(0f, corridorHeight + 0.05f, halfDepth);
        ceiling.transform.localScale = new Vector3(segmentCount * segmentWidth, 0.1f, corridorDepth);
        if (ceilingMaterial != null) ceiling.GetComponent<Renderer>().sharedMaterial = ceilingMaterial;

        // 3. Generate Segments
        for (int i = 0; i < segmentCount; i++)
        {
            float posX = startX + (i * segmentWidth);
            GameObject segmentParent = new GameObject("Segment_" + i);
            segmentParent.transform.parent = parentObj.transform;
            segmentParent.transform.localPosition = new Vector3(posX, 0f, 0f);

            // Back Wall (at Z = corridorDepth)
            GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backWall.name = "BackWall";
            backWall.transform.parent = segmentParent.transform;
            backWall.transform.localPosition = new Vector3(0f, corridorHeight / 2f, corridorDepth);
            backWall.transform.localScale = new Vector3(segmentWidth, corridorHeight, 0.1f);
            if (wallMaterial != null) backWall.GetComponent<Renderer>().sharedMaterial = wallMaterial;

            // Baseboard (蟾ｾ譛ｨ) - dark brown strip at the bottom of the wall
            GameObject baseboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseboard.name = "Baseboard";
            baseboard.transform.parent = segmentParent.transform;
            baseboard.transform.localPosition = new Vector3(0f, 0.15f, corridorDepth - 0.02f);
            baseboard.transform.localScale = new Vector3(segmentWidth, 0.3f, 0.05f);
            baseboard.GetComponent<Renderer>().sharedMaterial = CreateSafeMaterial(new Color(0.2f, 0.12f, 0.08f)); // Wood dark brown

            // Ceiling Molding (蝗槭ｊ邵・ - wood trim at the top of the wall
            GameObject molding = GameObject.CreatePrimitive(PrimitiveType.Cube);
            molding.name = "CeilingMolding";
            molding.transform.parent = segmentParent.transform;
            molding.transform.localPosition = new Vector3(0f, corridorHeight - 0.1f, corridorDepth - 0.02f);
            molding.transform.localScale = new Vector3(segmentWidth, 0.2f, 0.05f);
            molding.GetComponent<Renderer>().sharedMaterial = CreateSafeMaterial(new Color(0.2f, 0.12f, 0.08f));

            // Pillars (譟ｱ) - placed on the segment boundaries (left/right edge)
            // Left pillar for every segment to close the gaps, right pillar for the last one
            CreatePillar(segmentParent.transform, -segmentWidth / 2f, halfDepth);
            if (i == segmentCount - 1)
            {
                CreatePillar(segmentParent.transform, segmentWidth / 2f, halfDepth);
            }

            // Ceiling Light (陋榊・轣ｯ)
            CreateCeilingLight(segmentParent.transform, halfDepth);

            // Alternating elements (Door or Window or Notice Board)
            if (i % 3 == 1)
            {
                // Segment has a Classroom Door!
                CreateClassroomDoor(segmentParent.transform, i);
            }
            else if (i % 3 == 2)
            {
                // Segment has a Notice Board!
                CreateNoticeBoard(segmentParent.transform);
            }
            else
            {
                // Segment has a Window!
                CreateClassroomWindow(segmentParent.transform);
            }
        }

        // 4. Generate Side Walls to seal the ends of the corridor
        GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftWall.name = "LeftEndWall";
        leftWall.transform.parent = parentObj.transform;
        leftWall.transform.localPosition = new Vector3(startX - (segmentWidth / 2f), corridorHeight / 2f, halfDepth);
        leftWall.transform.localScale = new Vector3(0.1f, corridorHeight, corridorDepth);
        if (wallMaterial != null) leftWall.GetComponent<Renderer>().sharedMaterial = wallMaterial;

        GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightWall.name = "RightEndWall";
        rightWall.transform.parent = parentObj.transform;
        rightWall.transform.localPosition = new Vector3(-startX + (segmentWidth / 2f), corridorHeight / 2f, halfDepth);
        rightWall.transform.localScale = new Vector3(0.1f, corridorHeight, corridorDepth);
        if (wallMaterial != null) rightWall.GetComponent<Renderer>().sharedMaterial = wallMaterial;

        Debug.Log("Procedural School Corridor Generated Successfully!");
    }

    [ContextMenu("Clear Corridor")]
    public void Clear()
    {
        Transform existing = this.transform.Find(ParentName);
        if (existing != null)
        {
            DestroyImmediate(existing.gameObject);
            Debug.Log("Cleared existing corridor.");
        }
    }

    private void CreatePillar(Transform segment, float posX, float halfDepth)
    {
        GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pillar.name = "Pillar";
        pillar.transform.parent = segment;
        pillar.transform.localPosition = new Vector3(posX, corridorHeight / 2f, corridorDepth - 0.1f);
        pillar.transform.localScale = new Vector3(0.4f, corridorHeight, 0.3f);
        if (pillarMaterial != null) pillar.GetComponent<Renderer>().sharedMaterial = pillarMaterial;
    }

    private void CreateCeilingLight(Transform segment, float halfDepth)
    {
        // 1. White fluorescent fixture model
        GameObject lightFixture = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lightFixture.name = "FluorescentLightFixture";
        lightFixture.transform.parent = segment;
        lightFixture.transform.localPosition = new Vector3(0f, corridorHeight - 0.05f, halfDepth);
        lightFixture.transform.localScale = new Vector3(2f, 0.1f, 0.3f);
        if (lightFixtureMaterial != null)
        {
            lightFixture.GetComponent<Renderer>().sharedMaterial = lightFixtureMaterial;
        }
        else
        {
            Material lfMat = CreateSafeMaterial(Color.white);
            lfMat.EnableKeyword("_EMISSION");
            lfMat.SetColor("_EmissionColor", new Color(1.5f, 1.5f, 1.3f));
            lightFixture.GetComponent<Renderer>().sharedMaterial = lfMat;
        }

        // 2. Real Unity Point Light for gorgeous volumetric illumination and shadows!
        GameObject lightObj = new GameObject("PointLight");
        lightObj.transform.parent = lightFixture.transform;
        lightObj.transform.localPosition = new Vector3(0f, -0.2f, 0f);
        
        Light lightComp = lightObj.AddComponent<Light>();
        lightComp.type = LightType.Point;
        lightComp.range = 8f;
        lightComp.intensity = 1.2f;
        lightComp.color = new Color(1f, 0.95f, 0.85f); // Soft warm white
        lightComp.shadows = LightShadows.Soft; // Enable soft shadows for premium volumetric look!
    }

    private void CreateClassroomDoor(Transform segment, int segmentIndex)
    {
        // Door Frame
        GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frame.name = "DoorFrame";
        frame.transform.parent = segment;
        frame.transform.localPosition = new Vector3(0f, 1.4f, corridorDepth - 0.02f);
        frame.transform.localScale = new Vector3(1.8f, 2.8f, 0.15f);
        frame.GetComponent<Renderer>().sharedMaterial = CreateSafeMaterial(new Color(0.35f, 0.22f, 0.15f)); // Wood medium brown

        // Actual Door Panel (slightly recessed)
        GameObject door;
        if (doorPrefab != null)
        {
            door = Instantiate(doorPrefab, frame.transform);
            door.transform.localPosition = new Vector3(0f, 0f, -0.1f);
        }
        else
        {
            door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "DoorPanel";
            door.transform.parent = frame.transform;
            door.transform.localPosition = new Vector3(0f, 0f, -0.2f);
            door.transform.localScale = new Vector3(0.9f, 0.96f, 0.5f);
            if (doorMaterial != null) door.GetComponent<Renderer>().sharedMaterial = doorMaterial;

            // Small glass window in the door
            GameObject doorGlass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorGlass.name = "DoorGlass";
            doorGlass.transform.parent = door.transform;
            doorGlass.transform.localPosition = new Vector3(0f, 0.2f, -0.52f);
            doorGlass.transform.localScale = new Vector3(0.4f, 0.4f, 0.2f);
            if (glassMaterial != null) doorGlass.GetComponent<Renderer>().sharedMaterial = glassMaterial;

            // Brass Door Handle
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.name = "DoorHandle";
            handle.transform.parent = door.transform;
            handle.transform.localPosition = new Vector3(0.42f, -0.1f, -0.6f);
            handle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            handle.transform.localScale = new Vector3(0.08f, 0.15f, 0.08f);
            handle.GetComponent<Renderer>().sharedMaterial = CreateSafeMaterial(new Color(0.8f, 0.6f, 0.2f)); // Brass Gold
        }

        // Room Name Sign (e.g. "1-A", "2-B") using TextMesh Pro!
        GameObject signBoard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        signBoard.name = "ClassroomSignBoard";
        signBoard.transform.parent = segment;
        signBoard.transform.localPosition = new Vector3(-1.2f, 2.5f, corridorDepth - 0.05f);
        signBoard.transform.localScale = new Vector3(0.5f, 0.25f, 0.05f);
        signBoard.GetComponent<Renderer>().sharedMaterial = CreateSafeMaterial(Color.white);

        GameObject textObj = new GameObject("ClassroomText");
        textObj.transform.parent = signBoard.transform;
        textObj.transform.localPosition = new Vector3(0f, 0f, -0.55f);
        textObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // Text needs to face forward (since camera looks at wall)
        textObj.transform.localScale = new Vector3(0.2f, 0.4f, 1f);

        TextMeshPro textMesh = textObj.AddComponent<TextMeshPro>();
        if (customFont != null) textMesh.font = customFont;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.fontSize = 2.5f;
        textMesh.color = Color.black;
        
        string grade = (segmentIndex / 3 + 1).ToString();
        char classLetter = (char)('A' + (segmentIndex % 3));
        textMesh.text = grade + "-" + classLetter;
    }

    private void CreateClassroomWindow(Transform segment)
    {
        // Window Frame Outer
        GameObject outerFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        outerFrame.name = "WindowFrame";
        outerFrame.transform.parent = segment;
        outerFrame.transform.localPosition = new Vector3(0f, 2.2f, corridorDepth - 0.02f);
        outerFrame.transform.localScale = new Vector3(3.5f, 1.8f, 0.15f);
        if (windowFrameMaterial != null) outerFrame.GetComponent<Renderer>().sharedMaterial = windowFrameMaterial;
        else outerFrame.GetComponent<Renderer>().sharedMaterial = CreateSafeMaterial(new Color(0.35f, 0.22f, 0.15f));

        // Glass Pane (semi-transparent)
        GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
        glass.name = "WindowGlass";
        glass.transform.parent = outerFrame.transform;
        glass.transform.localPosition = new Vector3(0f, 0f, -0.2f);
        glass.transform.localScale = new Vector3(0.95f, 0.92f, 0.4f);
        if (glassMaterial != null) glass.GetComponent<Renderer>().sharedMaterial = glassMaterial;

        // Window sill (遯灘床)
        GameObject sill = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sill.name = "WindowSill";
        sill.transform.parent = segment;
        sill.transform.localPosition = new Vector3(0f, 1.25f, corridorDepth - 0.05f);
        sill.transform.localScale = new Vector3(3.7f, 0.1f, 0.25f);
        sill.GetComponent<Renderer>().sharedMaterial = CreateSafeMaterial(new Color(0.3f, 0.18f, 0.1f));
    }

    private void CreateNoticeBoard(Transform segment)
    {
        // Wooden frame
        GameObject boardFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boardFrame.name = "NoticeBoardFrame";
        boardFrame.transform.parent = segment;
        boardFrame.transform.localPosition = new Vector3(0f, 2.2f, corridorDepth - 0.02f);
        boardFrame.transform.localScale = new Vector3(3.2f, 1.6f, 0.08f);
        boardFrame.GetComponent<Renderer>().sharedMaterial = CreateSafeMaterial(new Color(0.3f, 0.18f, 0.1f));

        // Felt cork board center (Green or beige cork)
        GameObject boardCenter = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boardCenter.name = "NoticeBoardCenter";
        boardCenter.transform.parent = boardFrame.transform;
        boardCenter.transform.localPosition = new Vector3(0f, 0f, -0.2f);
        boardCenter.transform.localScale = new Vector3(0.96f, 0.92f, 0.5f);
        if (noticeBoardMaterial != null)
        {
            boardCenter.GetComponent<Renderer>().sharedMaterial = noticeBoardMaterial;
        }
        else
        {
            boardCenter.GetComponent<Renderer>().sharedMaterial = CreateSafeMaterial(new Color(0.15f, 0.35f, 0.2f)); // Deep green chalkboard/cork look
        }

        // Spawn some random colored papers pinned to the board!
        System.Random rand = new System.Random();
        int paperCount = rand.Next(3, 6);
        Color[] paperColors = { Color.white, new Color(0.95f, 0.95f, 0.7f), new Color(0.7f, 0.85f, 0.95f), new Color(0.95f, 0.75f, 0.75f) };

        for (int p = 0; p < paperCount; p++)
        {
            GameObject paper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            paper.name = "PinnedPaper_" + p;
            paper.transform.parent = boardCenter.transform;
            
            // Random offset within the notice board limits
            float randX = (float)(rand.NextDouble() * 0.7f - 0.35f);
            float randY = (float)(rand.NextDouble() * 0.6f - 0.3f);
            float randRot = (float)(rand.NextDouble() * 15f - 7.5f);

            paper.transform.localPosition = new Vector3(randX, randY, -0.55f);
            paper.transform.localRotation = Quaternion.Euler(0f, 0f, randRot);
            
            // Standard A4 paper proportions
            paper.transform.localScale = new Vector3(0.18f, 0.25f, 0.05f);
            paper.GetComponent<Renderer>().sharedMaterial = CreateSafeMaterial(paperColors[rand.Next(paperColors.Length)]);
        }
    }

    private Material CreateSafeMaterial(Color color, float glossiness = 0.5f, bool isGlass = false)
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
        
        if (isGlass)
        {
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }
        return mat;
    }

    private void InitDefaultMaterials()
    {
        // Procedurally generate solid default materials if none are selected to ensure instant, beautiful visual feedback!
        if (wallMaterial == null)
        {
            wallMaterial = CreateSafeMaterial(new Color(0.9f, 0.88f, 0.84f), 0.1f);
        }

        if (pillarMaterial == null)
        {
            pillarMaterial = CreateSafeMaterial(new Color(0.85f, 0.83f, 0.8f), 0.05f);
        }

        if (ceilingMaterial == null)
        {
            ceilingMaterial = CreateSafeMaterial(new Color(0.95f, 0.95f, 0.95f), 0.5f);
        }

        if (glassMaterial == null)
        {
            glassMaterial = CreateSafeMaterial(new Color(0.7f, 0.85f, 0.9f, 0.35f), 0.8f, true);
        }
    }
}

