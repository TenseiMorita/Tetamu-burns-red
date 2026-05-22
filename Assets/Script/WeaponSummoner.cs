using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSummoner : MonoBehaviour
{
    [Header("Visual Materials (Auto-created if null)")]
    public Material neonBladeMaterial;
    public Material gunMetalMaterial;
    public Material hiltMetalMaterial;
    public Material gunNeonMaterial;

    private List<GameObject> weaponContainers = new List<GameObject>();
    private bool isSummoning = false;

    void Start()
    {
        InitMaterials();
    }

    public void SummonWeapons(List<Transform> allies, System.Action onComplete)
    {
        if (isSummoning) return;
        StartCoroutine(SummonSequence(allies, onComplete));
    }

    private IEnumerator SummonSequence(List<Transform> allies, System.Action onComplete)
    {
        isSummoning = true;

        // 1. Destroy any existing weapons
        ClearWeapons();

        // 2. Generate the 3D Weapon models procedurally high in the sky!
        float startY = 15f;
        float duration = 1.6f;

        List<GameObject> portals = new List<GameObject>();
        List<Vector3> targetPositions = new List<Vector3>();

        for (int i = 0; i < allies.Count; i++)
        {
            Transform ally = allies[i];
            bool isPlayer = (i == 0); // Player is first
            Color portalColor = isPlayer ? new Color(0f, 0.8f, 1f) : Color.green;

            GameObject portal = CreateWarpPortal(new Vector3(ally.position.x, startY, ally.position.z), portalColor);
            portals.Add(portal);

            PortalSpinner spinner = portal.AddComponent<PortalSpinner>();
            spinner.beamMat = portal.transform.Find("LightBeam").GetComponent<Renderer>().sharedMaterial;
            spinner.duration = duration;

            GameObject container = new GameObject("WeaponContainer_" + i);
            container.transform.position = new Vector3(ally.position.x, startY, ally.position.z);
            weaponContainers.Add(container);

            if (i == 0)
            {
                GameObject leftBlade = CreateDualBlade(true);
                GameObject rightBlade = CreateDualBlade(false);
                leftBlade.transform.parent = container.transform;
                rightBlade.transform.parent = container.transform;
                leftBlade.transform.localPosition = new Vector3(-0.4f, 0f, 0f);
                rightBlade.transform.localPosition = new Vector3(0.4f, 0f, 0f);
                targetPositions.Add(ally.position + new Vector3(0f, 1f, 0.5f));
            }
            else
            {
                GameObject weapon = null;
                switch (i)
                {
                    case 1: weapon = CreateCyberRifle(); break;
                    case 2: weapon = CreateScythe(); break;
                    case 3: weapon = CreateStaff(); break;
                    case 4: weapon = CreateShield(); break;
                    case 5: weapon = CreateBroadsword(); break;
                    default: weapon = CreateCyberRifle(); break;
                }
                weapon.transform.parent = container.transform;
                weapon.transform.localPosition = Vector3.zero;
                targetPositions.Add(ally.position + new Vector3(0.5f, 1f, 0.2f));
            }

        }

        // 3. Fall and Spin Animation
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float tSlam = t * t * (3f - 2f * t);

            for (int i = 0; i < allies.Count; i++)
            {
                Transform ally = allies[i];
                GameObject container = weaponContainers[i];
                Vector3 targetPos = targetPositions[i];
                bool isPlayer = (i == 0);

                container.transform.position = Vector3.Lerp(new Vector3(ally.position.x, startY, ally.position.z), targetPos, tSlam);

                if (isPlayer)
                {
                    container.transform.rotation = Quaternion.Euler(t * 720f, t * 1080f, t * 360f);
                }
                else
                {
                    container.transform.rotation = Quaternion.Euler(t * 540f, t * 720f, t * 1440f);
                }
            }

            yield return null;
        }

        // 4. Impact Flash & Audio feedback!
        for (int i = 0; i < allies.Count; i++)
        {
            PlaySummonImpactEffect(targetPositions[i]);

            Transform ally = allies[i];
            GameObject container = weaponContainers[i];
            bool isPlayer = (i == 0);

            container.transform.parent = ally;
            if (isPlayer)
            {
                container.transform.localPosition = new Vector3(0f, 1f, 0.5f);
                container.transform.localRotation = Quaternion.Euler(15f, -90f, 45f);
            }
            else
            {
                container.transform.localPosition = new Vector3(0.5f, 0.9f, 0.2f);
                container.transform.localRotation = Quaternion.Euler(0f, 90f, -10f);
            }

            StartCoroutine(DestroyPortalGracefully(portals[i]));
        }

        isSummoning = false;
        
        if (onComplete != null)
        {
            onComplete.Invoke();
        }
    }

    private GameObject CreateWarpPortal(Vector3 pos, Color portalColor)
    {
        GameObject portalParent = new GameObject("WarpPortal");
        portalParent.transform.position = pos;

        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "CentralDisc";
        disc.transform.SetParent(portalParent.transform, false);
        disc.transform.localPosition = Vector3.zero;
        disc.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        disc.transform.localScale = new Vector3(4f, 0.02f, 4f);
        Destroy(disc.GetComponent<Collider>());

        Renderer discRend = disc.GetComponent<Renderer>();
        Material discMat = new Material(Shader.Find("Standard"));
        discMat.SetFloat("_Mode", 3f); // Transparent
        discMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        discMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        discMat.SetInt("_ZWrite", 0);
        discMat.DisableKeyword("_ALPHATEST_ON");
        discMat.EnableKeyword("_ALPHABLEND_ON");
        discMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        discMat.renderQueue = 3000;
        discMat.color = new Color(portalColor.r, portalColor.g, portalColor.b, 0.35f);
        discMat.EnableKeyword("_EMISSION");
        discMat.SetColor("_EmissionColor", portalColor * 2f);
        discRend.sharedMaterial = discMat;

        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "OuterRing";
        ring.transform.SetParent(portalParent.transform, false);
        ring.transform.localPosition = new Vector3(0f, 0.01f, 0f);
        ring.transform.localScale = new Vector3(4.5f, 0.01f, 4.5f);
        Destroy(ring.GetComponent<Collider>());

        Renderer ringRend = ring.GetComponent<Renderer>();
        Material ringMat = new Material(Shader.Find("Standard"));
        ringMat.color = new Color(1f, 1f, 1f, 0.8f);
        ringMat.EnableKeyword("_EMISSION");
        ringMat.SetColor("_EmissionColor", new Color(1.5f, 1.5f, 1.5f));
        ringRend.sharedMaterial = ringMat;

        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 2f / 8f;
            GameObject rune = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rune.name = "Rune_" + i;
            rune.transform.SetParent(portalParent.transform, false);
            rune.transform.localPosition = new Vector3(Mathf.Cos(angle) * 2.2f, 0.05f, Mathf.Sin(angle) * 2.2f);
            rune.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            Destroy(rune.GetComponent<Collider>());

            Renderer runeRend = rune.GetComponent<Renderer>();
            Material runeMat = new Material(Shader.Find("Standard"));
            runeMat.color = portalColor;
            runeMat.EnableKeyword("_EMISSION");
            runeMat.SetColor("_EmissionColor", portalColor * 3f);
            runeRend.sharedMaterial = runeMat;
        }

        GameObject spotLightObj = new GameObject("PortalSpotlight");
        spotLightObj.transform.SetParent(portalParent.transform, false);
        spotLightObj.transform.localPosition = new Vector3(0f, -0.5f, 0f);
        spotLightObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        Light light = spotLightObj.AddComponent<Light>();
        light.type = LightType.Spot;
        light.range = 25f;
        light.spotAngle = 35f;
        light.color = portalColor;
        light.intensity = 8f;
        light.shadows = LightShadows.None;

        GameObject lightBeam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lightBeam.name = "LightBeam";
        lightBeam.transform.SetParent(portalParent.transform, false);
        lightBeam.transform.localPosition = new Vector3(0f, -7.5f, 0f);
        lightBeam.transform.localScale = new Vector3(3f, 7.5f, 3f);
        Destroy(lightBeam.GetComponent<Collider>());

        Renderer beamRend = lightBeam.GetComponent<Renderer>();
        Material beamMat = new Material(Shader.Find("Sprites/Default"));
        beamMat.color = new Color(portalColor.r, portalColor.g, portalColor.b, 0.12f);
        beamRend.sharedMaterial = beamMat;

        portalParent.transform.localScale = Vector3.zero;

        return portalParent;
    }

    private IEnumerator DestroyPortalGracefully(GameObject portal)
    {
        if (portal == null) yield break;
        PortalSpinner spinner = portal.GetComponent<PortalSpinner>();
        if (spinner != null) spinner.enabled = false;

        float elapsed = 0f;
        float fadeDuration = 0.3f;
        Vector3 startScale = portal.transform.localScale;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            if (portal != null)
            {
                portal.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            }
            yield return null;
        }

        if (portal != null) Destroy(portal);
    }

    public void ClearWeapons()
    {
        foreach (var c in weaponContainers)
        {
            if (c != null) Destroy(c);
        }
        weaponContainers.Clear();
    }

    private GameObject CreateDualBlade(bool isLeftHand)
    {
        GameObject bladeObj = new GameObject("EnergyBlade");

        GameObject hilt = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hilt.name = "Hilt";
        hilt.transform.parent = bladeObj.transform;
        hilt.transform.localPosition = Vector3.zero;
        hilt.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        hilt.transform.localScale = new Vector3(0.08f, 0.25f, 0.08f);
        if (hiltMetalMaterial != null) hilt.GetComponent<Renderer>().sharedMaterial = hiltMetalMaterial;
        else hilt.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard")) { color = new Color(0.3f, 0.33f, 0.35f) };

        GameObject guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        guard.name = "Crossguard";
        guard.transform.parent = hilt.transform;
        guard.transform.localPosition = new Vector3(0f, 1f, 0f);
        guard.transform.localScale = new Vector3(3f, 0.2f, 0.8f);
        guard.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard")) { color = Color.black };

        GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blade.name = "NeonBlade";
        blade.transform.parent = hilt.transform;
        blade.transform.localPosition = new Vector3(0f, 4f, 0f);
        blade.transform.localScale = new Vector3(1.2f, 6f, 0.3f);
        if (neonBladeMaterial != null) blade.GetComponent<Renderer>().sharedMaterial = neonBladeMaterial;
        else
        {
            Material bladeMat = new Material(Shader.Find("Standard"));
            bladeMat.color = new Color(0f, 0.8f, 1f, 0.8f);
            bladeMat.EnableKeyword("_EMISSION");
            bladeMat.SetColor("_EmissionColor", new Color(0f, 1.8f, 2.5f));
            blade.GetComponent<Renderer>().sharedMaterial = bladeMat;
        }

        bladeObj.transform.localScale = Vector3.one;
        return bladeObj;
    }

    private GameObject CreateScythe()
    {
        GameObject scythe = new GameObject("Scythe");
        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        handle.transform.parent = scythe.transform;
        handle.transform.localPosition = Vector3.zero;
        handle.transform.localScale = new Vector3(0.1f, 1.5f, 0.1f);
        if (hiltMetalMaterial != null) handle.GetComponent<Renderer>().sharedMaterial = hiltMetalMaterial;
        
        GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blade.transform.parent = scythe.transform;
        blade.transform.localPosition = new Vector3(0.8f, 1.4f, 0f);
        blade.transform.localScale = new Vector3(1.8f, 0.1f, 0.3f);
        if (neonBladeMaterial != null) blade.GetComponent<Renderer>().sharedMaterial = neonBladeMaterial;
        
        return scythe;
    }

    private GameObject CreateStaff()
    {
        GameObject staff = new GameObject("Staff");
        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        handle.transform.parent = staff.transform;
        handle.transform.localPosition = Vector3.zero;
        handle.transform.localScale = new Vector3(0.08f, 1.2f, 0.08f);
        if (hiltMetalMaterial != null) handle.GetComponent<Renderer>().sharedMaterial = hiltMetalMaterial;
        
        GameObject gem = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        gem.transform.parent = staff.transform;
        gem.transform.localPosition = new Vector3(0f, 1.3f, 0f);
        gem.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        if (neonBladeMaterial != null) gem.GetComponent<Renderer>().sharedMaterial = neonBladeMaterial;
        
        return staff;
    }

    private GameObject CreateShield()
    {
        GameObject shield = new GameObject("Shield");
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.transform.parent = shield.transform;
        body.transform.localPosition = Vector3.zero;
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        body.transform.localScale = new Vector3(1.5f, 0.1f, 1.5f);
        if (gunMetalMaterial != null) body.GetComponent<Renderer>().sharedMaterial = gunMetalMaterial;
        
        GameObject cross = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cross.transform.parent = shield.transform;
        cross.transform.localPosition = new Vector3(0f, 0f, -0.1f);
        cross.transform.localScale = new Vector3(0.4f, 1.6f, 0.15f);
        if (gunNeonMaterial != null) cross.GetComponent<Renderer>().sharedMaterial = gunNeonMaterial;
        
        return shield;
    }

    private GameObject CreateBroadsword()
    {
        GameObject sword = new GameObject("Broadsword");
        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        handle.transform.parent = sword.transform;
        handle.transform.localPosition = Vector3.zero;
        handle.transform.localScale = new Vector3(0.12f, 0.4f, 0.12f);
        if (hiltMetalMaterial != null) handle.GetComponent<Renderer>().sharedMaterial = hiltMetalMaterial;
        
        GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blade.transform.parent = sword.transform;
        blade.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        blade.transform.localScale = new Vector3(0.6f, 2f, 0.1f);
        if (gunMetalMaterial != null) blade.GetComponent<Renderer>().sharedMaterial = gunMetalMaterial;
        
        return sword;
    }

    private GameObject CreateCyberRifle()
    {
        GameObject rifleObj = new GameObject("CyberRifle");

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Receiver";
        body.transform.parent = rifleObj.transform;
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(0.8f, 0.22f, 0.16f);
        if (gunMetalMaterial != null) body.GetComponent<Renderer>().sharedMaterial = gunMetalMaterial;
        else body.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard")) { color = new Color(0.12f, 0.13f, 0.15f) };

        GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrel.name = "Barrel";
        barrel.transform.parent = body.transform;
        barrel.transform.localPosition = new Vector3(0.9f, 0.15f, 0f);
        barrel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        barrel.transform.localScale = new Vector3(0.12f, 1.2f, 0.12f);
        barrel.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard")) { color = new Color(0.25f, 0.27f, 0.3f) };

        GameObject stock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stock.name = "ButtStock";
        stock.transform.parent = body.transform;
        stock.transform.localPosition = new Vector3(-0.8f, -0.1f, 0f);
        stock.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
        stock.transform.localScale = new Vector3(0.8f, 0.3f, 0.8f);
        stock.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard")) { color = Color.black };

        GameObject clip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        clip.name = "AmmoMagazine";
        clip.transform.parent = body.transform;
        clip.transform.localPosition = new Vector3(0.2f, -0.4f, 0f);
        clip.transform.localRotation = Quaternion.Euler(0f, 0f, 15f);
        clip.transform.localScale = new Vector3(0.25f, 0.6f, 0.9f);
        clip.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard")) { color = new Color(0.08f, 0.08f, 0.08f) };

        GameObject scope = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        scope.name = "TargetScope";
        scope.transform.parent = body.transform;
        scope.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        scope.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        scope.transform.localScale = new Vector3(0.15f, 0.4f, 0.15f);
        scope.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard")) { color = Color.black };

        GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lens.name = "Lens";
        lens.transform.parent = scope.transform;
        lens.transform.localPosition = new Vector3(0f, 0.52f, 0f);
        lens.transform.localScale = new Vector3(0.9f, 0.1f, 0.9f);
        Material lensMat = new Material(Shader.Find("Standard"));
        lensMat.color = Color.green;
        lensMat.EnableKeyword("_EMISSION");
        lensMat.SetColor("_EmissionColor", new Color(0f, 2f, 0f));
        lens.GetComponent<Renderer>().sharedMaterial = lensMat;

        GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stripe.name = "NeonStripe";
        stripe.transform.parent = body.transform;
        stripe.transform.localPosition = new Vector3(0f, 0f, 0.52f);
        stripe.transform.localScale = new Vector3(0.85f, 0.08f, 0.08f);
        if (gunNeonMaterial != null) stripe.GetComponent<Renderer>().sharedMaterial = gunNeonMaterial;
        else
        {
            Material stripeMat = new Material(Shader.Find("Standard"));
            stripeMat.color = Color.green;
            stripeMat.EnableKeyword("_EMISSION");
            stripeMat.SetColor("_EmissionColor", new Color(0f, 1.5f, 0f));
            stripe.GetComponent<Renderer>().sharedMaterial = stripeMat;
        }

        rifleObj.transform.localScale = Vector3.one;
        return rifleObj;
    }

    private void PlaySummonImpactEffect(Vector3 pos)
    {
        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.name = "SummonFlash";
        flash.transform.position = pos;
        flash.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        
        Renderer rend = flash.GetComponent<Renderer>();
        Material flashMat = new Material(Shader.Find("Standard"));
        flashMat.color = Color.white;
        flashMat.EnableKeyword("_EMISSION");
        flashMat.SetColor("_EmissionColor", new Color(3f, 3f, 2f));
        rend.sharedMaterial = flashMat;
        
        Destroy(flash.GetComponent<Collider>());

        GameObject impactLightObj = new GameObject("ImpactLight");
        impactLightObj.transform.position = pos;
        Light light = impactLightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.7f, 0.9f, 1f);
        light.range = 8f;
        light.intensity = 4f;

        StartCoroutine(AnimateFlash(flash, light));

        GameObject synthAudio = new GameObject("SummonSound");
        synthAudio.transform.position = pos;
        AudioSource source = synthAudio.AddComponent<AudioSource>();
        source.spatialBlend = 1f;
        source.volume = 0.5f;

        LaserSynth synth = synthAudio.AddComponent<LaserSynth>();
        source.Play();
        Destroy(synthAudio, 1.8f);
    }

    private IEnumerator AnimateFlash(GameObject flash, Light light)
    {
        float elapsed = 0f;
        float duration = 0.4f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (flash != null)
            {
                flash.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 4f, t);
                // Animate alpha via sharedMaterial color - this is safe at runtime
                Color c = flash.GetComponent<Renderer>().sharedMaterial.color;
                flash.GetComponent<Renderer>().sharedMaterial.color = new Color(c.r, c.g, c.b, 1f - t);
            }
            if (light != null)
            {
                light.intensity = Mathf.Lerp(4f, 0f, t);
            }
            yield return null;
        }

        if (flash != null) Destroy(flash);
        if (light != null) Destroy(light.gameObject);
    }

    private void InitMaterials()
    {
        if (neonBladeMaterial == null)
        {
            neonBladeMaterial = new Material(Shader.Find("Standard"));
            neonBladeMaterial.color = new Color(0f, 0.8f, 1f, 0.8f);
            neonBladeMaterial.EnableKeyword("_EMISSION");
            neonBladeMaterial.SetColor("_EmissionColor", new Color(0f, 1.8f, 2.5f));
        }
        if (gunMetalMaterial == null)
        {
            gunMetalMaterial = new Material(Shader.Find("Standard"));
            gunMetalMaterial.color = new Color(0.1f, 0.11f, 0.13f);
            gunMetalMaterial.SetFloat("_Metallic", 0.8f);
            gunMetalMaterial.SetFloat("_Glossiness", 0.6f);
        }
        if (hiltMetalMaterial == null)
        {
            hiltMetalMaterial = new Material(Shader.Find("Standard"));
            hiltMetalMaterial.color = new Color(0.4f, 0.42f, 0.44f);
            hiltMetalMaterial.SetFloat("_Metallic", 0.9f);
            hiltMetalMaterial.SetFloat("_Glossiness", 0.7f);
        }
        if (gunNeonMaterial == null)
        {
            gunNeonMaterial = new Material(Shader.Find("Standard"));
            gunNeonMaterial.color = Color.green;
            gunNeonMaterial.EnableKeyword("_EMISSION");
            gunNeonMaterial.SetColor("_EmissionColor", new Color(0f, 1.8f, 0f));
        }
    }
}

public class LaserSynth : MonoBehaviour
{
    private double sampling_frequency = 48000;
    private double phase = 0.0;
    private float duration = 1.0f;
    private long sampleCount = 0;

    void OnAudioFilterRead(float[] data, int channels)
    {
        double doubleSampleRate = sampling_frequency;

        for (int i = 0; i < data.Length; i += channels)
        {
            float t = (float)((double)sampleCount / doubleSampleRate);

            if (t > duration)
            {
                for (int c = 0; c < channels; c++) data[i + c] = 0f;
                sampleCount++;
                continue;
            }

            double pitch = Mathf.Lerp(3000f, 80f, t / 0.3f);
            if (t > 0.3f) pitch = Mathf.Lerp(80f, 20f, (t - 0.3f) / 0.7f);

            phase += 2.0 * Mathf.PI * pitch / doubleSampleRate;

            float sample;
            if (t <= 0.3f) sample = (float)Mathf.Sin((float)phase);
            else
            {
                float noise = (float)(new System.Random().NextDouble() * 2.0 - 1.0);
                sample = (float)Mathf.Sin((float)phase) * 0.4f + noise * 0.6f;
            }

            float amp = Mathf.Clamp01(1f - t / duration);

            for (int c = 0; c < channels; c++)
            {
                data[i + c] = sample * amp * 0.2f;
            }

            if (phase > 2.0 * Mathf.PI * 100.0) phase -= 2.0 * Mathf.PI * 100.0;
            sampleCount++;
        }
    }
}

public class PortalSpinner : MonoBehaviour
{
    public float spinSpeed = 90f;
    public float targetScale = 1.0f;
    private float currentScale = 0f;
    public Material beamMat;
    public float duration = 1.6f;
    private float elapsed = 0f;

    void Update()
    {
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime);

        if (currentScale < targetScale)
        {
            currentScale += Time.deltaTime * 3f;
            if (currentScale > targetScale) currentScale = targetScale;
            transform.localScale = Vector3.one * currentScale;
        }

        elapsed += Time.deltaTime;
        if (beamMat != null)
        {
            float alpha = Mathf.Clamp01(1f - (elapsed / duration)) * 0.12f;
            beamMat.color = new Color(beamMat.color.r, beamMat.color.g, beamMat.color.b, alpha);
        }
    }
}

