using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum ActionType { Attack, Slash, EpicStrike, Shot, Heal }

[System.Serializable]
public class Fighter
{
    public string name;
    public int maxHP;
    public int currentHP;
    public int maxDP;
    public int currentDP;
    public int sp;
    public int switchInTurnBuff;
    public Transform fighterTransform;
    public bool isEnemy;
    public Vector3 origPos;

    public void OnSwitchOut()
    {
        // Hook point for future passive effects.
    }

    public void OnSwitchIn()
    {
        // Hook point for future passive effects.
        switchInTurnBuff = 1;
    }
}

public class BattleManager : MonoBehaviour
{
    [Header("Fighters Stats")]
    public List<Fighter> allies = new List<Fighter>();
    public List<Fighter> enemies = new List<Fighter>();

    public Fighter[] frontLine = new Fighter[3];
    public Fighter[] backLine = new Fighter[3];
    private PartyManager partyManager;

    [Header("Overdrive")]
    public int currentOverdrive = 0;
    public int maxOverdrive = 300; // 3 bars


    [Header("Camera & Positions")]
    public CameraFollow cameraFollow;
    public Transform battleCameraPoint;
    [Header("Camera Options")]
    public bool useCinematicCamera = true;

    private AudioSource combatAudioSource;
    public BattleUI battleUI;
    public GameManager gameManager;

    // Camera state for battle
    private Vector3 defaultCamPos;
    private Quaternion defaultCamRot;
    private bool isBattleActive = false;

    void Awake()
    {
        combatAudioSource = gameObject.AddComponent<AudioSource>();
        combatAudioSource.spatialBlend = 0f;

        // Auto-bind to the persistent GameManager singleton if available
        if (gameManager == null && GameManager.Instance != null)
        {
            gameManager = GameManager.Instance;
        }
    }

    public void Initialize(GameManager manager, BattleUI ui)
    {
        gameManager = manager;
        battleUI = ui;
    }

    /// <summary>
    /// Full state reset for clean restart.
    /// </summary>
    public void ResetState()
    {
        StopAllCoroutines();
        allies.Clear();
        enemies.Clear();
        partyManager = null;
        System.Array.Clear(frontLine, 0, frontLine.Length);
        System.Array.Clear(backLine, 0, backLine.Length);
        currentOverdrive = 0;
        isBattleActive = false;
    }

    public void StartBattle(List<Transform> allyTransforms, List<Transform> enemyTransforms)
    {
        // Full reset before starting
        ResetState();

        // Initialize 6 Allies
        for (int i = 0; i < allyTransforms.Count; i++)
        {
            if (allyTransforms[i] == null) continue;

            Fighter f = new Fighter();
            f.name = i == 0 ? "主人公" : "戦友NPC " + i;
            f.maxHP = i == 0 ? 100 : 80;
            f.currentHP = f.maxHP;
            f.maxDP = i == 0 ? 150 : 120;
            f.currentDP = f.maxDP;
            f.sp = 3; // Initial SP
            f.switchInTurnBuff = 0;
            f.isEnemy = false;
            f.fighterTransform = allyTransforms[i];
            f.origPos = allyTransforms[i].position;
            allies.Add(f);
        }
        partyManager = new PartyManager(allies);
        frontLine = partyManager.frontLine;
        backLine = partyManager.backLine;

        // Initialize 2 Enemies
        for (int i = 0; i < enemyTransforms.Count; i++)
        {
            if (enemyTransforms[i] == null) continue;

            Fighter e = new Fighter();
            e.name = "キャンサー " + (i + 1);
            e.maxHP = 600;
            e.currentHP = e.maxHP;
            e.maxDP = 300;
            e.currentDP = e.maxDP;
            e.sp = 0;
            e.isEnemy = true;
            e.fighterTransform = enemyTransforms[i];
            e.origPos = enemyTransforms[i].position;
            enemies.Add(e);
        }

        // Keep Battle scene camera position as the standard viewpoint.
        // This makes the opening UI position and battle position consistent.
        if (Camera.main != null)
        {
            defaultCamPos = Camera.main.transform.position;
            defaultCamRot = Camera.main.transform.rotation;
        }

        if (cameraFollow != null)
        {
            cameraFollow.target = null;
            cameraFollow.isTalking = false;
        }

        isBattleActive = true;
        battleUI.Show(this);
        ArrangeLinePositions();
        Debug.Log("HBR Style Battle Started! (6 vs 2)");
    }

    /// <summary>
    /// Swap front and back within the same column (legacy).
    /// </summary>
    public void SwapFrontBack(int colIndex)
    {
        TrySwapFrontAndBack(colIndex, colIndex);
    }

    /// <summary>
    /// Flexible swap: swap any front slot with any back slot (for drag-and-drop).
    /// frontCol: 0-2 index within frontLine
    /// backCol: 0-2 index within backLine
    /// </summary>
    public void SwapFrontAndBack(int frontCol, int backCol)
    {
        TrySwapFrontAndBack(frontCol, backCol);
    }

    public bool TrySwapFrontAndBack(int frontCol, int backCol)
    {
        if (frontCol < 0 || frontCol >= 3 || backCol < 0 || backCol >= 3) return false;
        if (partyManager == null) return false;
        if (!partyManager.SwapSingle(frontCol, backCol)) return false;

        ArrangeLinePositions();
        battleUI.UpdateUI();

        PlaySynthSound(ActionType.Heal, 800f, 0.1f);
        return true;
    }

    public bool TrySwapAllFrontBack()
    {
        if (partyManager == null) return false;
        if (!partyManager.SwapAll()) return false;

        ArrangeLinePositions();
        battleUI.UpdateUI();
        PlaySynthSound(ActionType.Heal, 800f, 0.1f);
        return true;
    }

    public bool CanCardBeSwitched(int cardIndex)
    {
        if (cardIndex < 0 || cardIndex >= 6) return false;
        Fighter f = cardIndex < 3 ? frontLine[cardIndex] : backLine[cardIndex - 3];
        return partyManager != null && partyManager.CanSwitch(f);
    }

    private void ArrangeLinePositions()
    {
        // Smoothly move models to reflect front/back line
        // Front line is closer to center, back line is further back
        for (int i = 0; i < 3; i++)
        {
            if (frontLine[i] != null && frontLine[i].fighterTransform != null)
            {
                frontLine[i].fighterTransform.gameObject.SetActive(true);
                Vector3 target = frontLine[i].origPos;
                frontLine[i].fighterTransform.position = target;
            }
            if (backLine[i] != null && backLine[i].fighterTransform != null)
            {
                backLine[i].fighterTransform.gameObject.SetActive(false);
            }

        }
    }

    // ============================================================
    // Camera cinematic for player attack phase
    // ============================================================
    private IEnumerator CameraToAttackAngle()
    {
        if (Camera.main == null) yield break;
        Transform cam = Camera.main.transform;

        // Calculate ally center (frontline only)
        Vector3 allyCenter = Vector3.zero;
        int allyCount = 0;
        for (int i = 0; i < 3; i++)
        {
            if (frontLine[i] != null && frontLine[i].fighterTransform != null && frontLine[i].currentHP > 0)
            {
                allyCenter += frontLine[i].fighterTransform.position;
                allyCount++;
            }
        }
        if (allyCount > 0) allyCenter /= allyCount;

        // Calculate enemy center
        Vector3 enemyCenter = Vector3.zero;
        int enemyCount = 0;
        foreach (var e in enemies)
        {
            if (e != null && e.fighterTransform != null && e.currentHP > 0)
            {
                enemyCenter += e.fighterTransform.position;
                enemyCount++;
            }
        }
        if (enemyCount > 0) enemyCenter /= enemyCount;

        if (allyCount == 0 || enemyCount == 0) yield break;

        // Camera position: behind and slightly above allies, looking toward enemies
        Vector3 dirToEnemy = (enemyCenter - allyCenter).normalized;
        Vector3 perpendicular = Vector3.Cross(dirToEnemy, Vector3.up).normalized;
        Vector3 camPos = allyCenter - dirToEnemy * 4f + Vector3.up * 2.5f + perpendicular * 1.5f;
        
        // Look at a point between allies and enemies, slightly biased toward enemies
        Vector3 lookTarget = Vector3.Lerp(allyCenter, enemyCenter, 0.4f) + Vector3.up * 0.5f;
        Quaternion camRot = Quaternion.LookRotation(lookTarget - camPos);

        float elapsed = 0f;
        float duration = 0.6f;
        Vector3 startPos = cam.position;
        Quaternion startRot = cam.rotation;

        while (elapsed < duration)
        {
            if (Camera.main == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smoothT = t * t * (3f - 2f * t); // Smoothstep

            cam.position = Vector3.Lerp(startPos, camPos, smoothT);
            cam.rotation = Quaternion.Slerp(startRot, camRot, smoothT);
            yield return null;
        }

        cam.position = camPos;
        cam.rotation = camRot;
    }

    private IEnumerator CameraToDefaultAngle()
    {
        if (Camera.main == null) yield break;
        Transform cam = Camera.main.transform;

        float elapsed = 0f;
        float duration = 0.5f;
        Vector3 startPos = cam.position;
        Quaternion startRot = cam.rotation;

        while (elapsed < duration)
        {
            if (Camera.main == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smoothT = t * t * (3f - 2f * t);

            cam.position = Vector3.Lerp(startPos, defaultCamPos, smoothT);
            cam.rotation = Quaternion.Slerp(startRot, defaultCamRot, smoothT);
            yield return null;
        }

        cam.position = defaultCamPos;
        cam.rotation = defaultCamRot;
    }

    public void ExecuteTurn(ActionType[] actions)
    {
        if (!isBattleActive) return;
        StartCoroutine(TurnSequence(actions));
    }

    private IEnumerator TurnSequence(ActionType[] actions)
    {
        battleUI.SetButtonsEnabled(false);

        // Keep editor-authored camera transform unless cinematic camera is explicitly enabled.
        if (useCinematicCamera)
        {
            yield return StartCoroutine(CameraToAttackAngle());
        }

        // --- 1. HERO ACTION PHASE ---
        for (int i = 0; i < 3; i++)
        {
            if (!isBattleActive) yield break;

            Fighter attacker = frontLine[i];
            if (attacker == null || attacker.currentHP <= 0) continue;

            // Pick a random alive enemy target
            Fighter target = GetRandomAliveEnemy();
            if (target == null) break;

            yield return StartCoroutine(AnimateAction(attacker, target, actions[i]));
            
            if (CheckWinCondition())
            {
                EndBattle(true);
                yield break;
            }
            yield return new WaitForSeconds(0.3f);
        }

        if (useCinematicCamera)
        {
            yield return StartCoroutine(CameraToDefaultAngle());
        }

        yield return new WaitForSeconds(0.4f);

        // --- 2. ENEMY ACTION PHASE ---
        for (int i = 0; i < enemies.Count; i++)
        {
            if (!isBattleActive) yield break;
            if (enemies[i].currentHP <= 0) continue;

            yield return StartCoroutine(AnimateEnemyAction(enemies[i]));

            if (CheckLoseCondition())
            {
                EndBattle(false);
                yield break;
            }
            yield return new WaitForSeconds(0.4f);
        }

        // --- 3. TURN END PREPARATION ---
        // SP recovery for ALL alive allies
        foreach (var ally in allies)
        {
            if (ally.currentHP > 0)
            {
                ally.sp = Mathf.Min(ally.sp + 2, 20); // gain 2 SP, max 20
            }

        }

        battleUI.UpdateUI();
        battleUI.SetButtonsEnabled(true);
    }

    private Fighter GetRandomAliveEnemy()
    {
        List<Fighter> aliveEnemies = new List<Fighter>();
        foreach (var e in enemies) if (e.currentHP > 0) aliveEnemies.Add(e);
        if (aliveEnemies.Count == 0) return null;
        return aliveEnemies[Random.Range(0, aliveEnemies.Count)];
    }

    private Fighter GetRandomAliveFrontLine()
    {
        List<Fighter> aliveFront = new List<Fighter>();
        foreach (var f in frontLine) if (f != null && f.currentHP > 0) aliveFront.Add(f);
        if (aliveFront.Count == 0) return null;
        return aliveFront[Random.Range(0, aliveFront.Count)];
    }

    private bool CheckWinCondition()
    {
        foreach (var e in enemies) if (e.currentHP > 0) return false;
        return true; // All enemies defeated
    }

    private bool CheckLoseCondition()
    {
        foreach (var a in allies) if (a.currentHP <= 0) return true; // Anyone dies -> Game Over
        return false;
    }

    private IEnumerator AnimateAction(Fighter attacker, Fighter defender, ActionType action)
    {
        if (attacker.fighterTransform == null || defender.fighterTransform == null) yield break;

        Transform attackerTrans = attacker.fighterTransform;
        Vector3 currentPos = attackerTrans.position;
        Vector3 targetAttackPos = defender.fighterTransform.position + (attacker.isEnemy ? Vector3.right * 1.8f : Vector3.left * 1.8f);

        bool isRanged = action == ActionType.Shot || action == ActionType.Heal;

        if (!isRanged)
        {
            float elapsed = 0f;
            float duration = 0.2f;
            while (elapsed < duration)
            {
                if (attackerTrans == null) yield break;
                attackerTrans.position = Vector3.Lerp(currentPos, targetAttackPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (attackerTrans != null) attackerTrans.position = targetAttackPos;
        }
        else
        {
            if (attackerTrans != null)
                attackerTrans.position = currentPos + (attacker.isEnemy ? Vector3.left * 0.4f : Vector3.right * 0.4f);
            yield return new WaitForSeconds(0.1f);
        }

        CalculateActionImpact(attacker, defender, action);
        StartCoroutine(ScreenShake(0.18f, 0.15f));

        float returnElapsed = 0f;
        float returnDuration = 0.25f;
        while (returnElapsed < returnDuration)
        {
            if (attackerTrans == null) yield break;
            attackerTrans.position = Vector3.Lerp(attackerTrans.position, currentPos, returnElapsed / returnDuration);
            returnElapsed += Time.deltaTime;
            yield return null;
        }
        if (attackerTrans != null) attackerTrans.position = currentPos;
    }

    private IEnumerator AnimateEnemyAction(Fighter enemy)
    {
        Fighter target = GetRandomAliveFrontLine();
        if (target == null) yield break; // Shouldn't happen unless Game Over
        if (enemy.fighterTransform == null) yield break;

        Transform enemyTrans = enemy.fighterTransform;
        Vector3 currentPos = enemyTrans.position;
        Vector3 targetPos = currentPos + Vector3.left * 1.5f;

        float elapsed = 0f;
        while (elapsed < 0.25f)
        {
            if (enemyTrans == null) yield break;
            enemyTrans.position = Vector3.Lerp(currentPos, targetPos, elapsed / 0.25f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        PlaySynthSound(ActionType.EpicStrike, 180f, 0.6f);

        // Deal damage
        DealDamageToAlly(target, Random.Range(35, 60));

        StartCoroutine(ScreenShake(0.35f, 0.25f));
        yield return new WaitForSeconds(0.3f);

        float returnElapsed = 0f;
        while (returnElapsed < 0.3f)
        {
            if (enemyTrans == null) yield break;
            enemyTrans.position = Vector3.Lerp(enemyTrans.position, currentPos, returnElapsed / 0.3f);
            returnElapsed += Time.deltaTime;
            yield return null;
        }
        if (enemyTrans != null) enemyTrans.position = currentPos;
    }

    private void CalculateActionImpact(Fighter attacker, Fighter defender, ActionType action)
    {
        int dpDmg = 0;
        int hpDmg = 0;
        bool isHeal = action == ActionType.Heal;

        switch (action)
        {
            case ActionType.Attack:
                attacker.sp = Mathf.Max(attacker.sp - 0, 0);
                dpDmg = Random.Range(35, 45);
                hpDmg = Random.Range(20, 30);
                PlaySynthSound(ActionType.Attack, 600f, 0.15f);
                break;
            case ActionType.Slash:
                attacker.sp = Mathf.Max(attacker.sp - 2, 0);
                dpDmg = Random.Range(55, 70);
                hpDmg = Random.Range(35, 45);
                PlaySynthSound(ActionType.Slash, 900f, 0.2f);
                break;
            case ActionType.EpicStrike:
                attacker.sp = Mathf.Max(attacker.sp - 6, 0);
                dpDmg = Random.Range(180, 220); // 2.5x normal DP damage
                hpDmg = Random.Range(60, 80);
                PlaySynthSound(ActionType.EpicStrike, 1500f, 0.4f);
                break;
            case ActionType.Shot:
                attacker.sp = Mathf.Max(attacker.sp - 2, 0);
                dpDmg = Random.Range(45, 55);
                hpDmg = Random.Range(40, 50);
                PlaySynthSound(ActionType.Shot, 400f, 0.15f);
                break;
            case ActionType.Heal:
                attacker.sp = Mathf.Max(attacker.sp - 4, 0);
                // DP HEAL all front line
                foreach (var f in frontLine)
                {
                    if (f != null && f.currentHP > 0 && f.fighterTransform != null)
                    {
                        f.currentDP = Mathf.Min(f.currentDP + 60, f.maxDP);
                        SpawnDamageText(f.fighterTransform.position + Vector3.up * 1.5f, "+60 DP", Color.cyan);
                    }
                }
                PlaySynthSound(ActionType.Heal, 1200f, 0.35f);
                break;
        }

        if (!isHeal && defender.currentHP > 0 && defender.fighterTransform != null)
        {
            Vector3 textSpawnPos = defender.fighterTransform.position + Vector3.up * 1.5f;

            if (defender.currentDP > 0)
            {
                int prevDP = defender.currentDP;
                defender.currentDP = Mathf.Max(defender.currentDP - dpDmg, 0);
                SpawnDamageText(textSpawnPos, dpDmg + " DP", Color.white);

                if (defender.currentDP == 0 && prevDP > 0)
                {
                    SpawnDamageText(textSpawnPos + Vector3.up * 0.8f, "DP BREAK!", Color.red, true);
                    Debug.Log("Enemy DP Shield Broken!");
                }
            }
            else
            {
                defender.currentHP = Mathf.Max(defender.currentHP - hpDmg, 0);
                SpawnDamageText(textSpawnPos, hpDmg + " HP", new Color(1f, 0.3f, 0.3f));
            }

            if (!attacker.isEnemy)
            {
                // Generate overdrive on successful hit
                currentOverdrive = Mathf.Min(currentOverdrive + 15, maxOverdrive);
            }

            battleUI.UpdateUI();
        }
    }

    public void ActivateOverdrive()
    {
        if (currentOverdrive >= 100)
        {
            int levels = currentOverdrive / 100;
            currentOverdrive -= levels * 100;
            
            // Give bonus SP and maybe extra stats
            foreach (var ally in allies)
            {
                if (ally.currentHP > 0) ally.sp = Mathf.Min(ally.sp + levels * 2, 20);
            }
            
            if (frontLine[1] != null && frontLine[1].fighterTransform != null)
                SpawnDamageText(frontLine[1].fighterTransform.position + Vector3.up * 2.5f, "OVERDRIVE LV" + levels + "!", Color.magenta, true);
                
            battleUI.UpdateUI();
        }
    }


    private void DealDamageToAlly(Fighter ally, int dmg)
    {
        if (ally.fighterTransform == null) return;
        Vector3 textSpawnPos = ally.fighterTransform.position + Vector3.up * 1.5f;
        
        if (ally.currentDP > 0)
        {
            int prevDP = ally.currentDP;
            ally.currentDP = Mathf.Max(ally.currentDP - dmg, 0);
            SpawnDamageText(textSpawnPos, dmg + " DP", Color.white);
            
            if (ally.currentDP == 0 && prevDP > 0)
            {
                SpawnDamageText(textSpawnPos + Vector3.up * 0.8f, "DP BREAK!", Color.red, true);
            }
        }
        else
        {
            ally.currentHP = Mathf.Max(ally.currentHP - dmg, 0);
            SpawnDamageText(textSpawnPos, dmg + " HP", new Color(1f, 0.2f, 0.2f));
        }

        battleUI.UpdateUI();
    }

    private void SpawnDamageText(Vector3 pos, string text, Color color, bool isGiant = false)
    {
        GameObject textObj = new GameObject("DamagePopUp");
        textObj.transform.position = pos + new Vector3(Random.Range(-0.4f, 0.4f), 0f, -0.5f);

        TextMeshPro tm = textObj.AddComponent<TextMeshPro>();
        if (gameManager != null && gameManager.customFont != null) tm.font = gameManager.customFont;
        tm.text = text;
        tm.color = color;
        tm.fontSize = isGiant ? 6f : 4.5f;
        tm.fontStyle = isGiant ? FontStyles.Bold : FontStyles.Normal;
        tm.alignment = TextAlignmentOptions.Center;
        textObj.transform.rotation = Quaternion.Euler(15f, 0f, 0f);

        StartCoroutine(FloatAndFadeText(textObj, tm));
    }

    private IEnumerator FloatAndFadeText(GameObject obj, TextMeshPro tm)
    {
        float elapsed = 0f;
        float duration = 0.8f;
        Vector3 startPos = obj.transform.position;
        Vector3 targetPos = startPos + Vector3.up * 1.2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (obj == null) yield break;
            obj.transform.position = Vector3.Lerp(startPos, targetPos, t);
            tm.color = new Color(tm.color.r, tm.color.g, tm.color.b, 1f - t);
            yield return null;
        }
        if (obj != null) Destroy(obj);
    }

    private IEnumerator ScreenShake(float duration, float magnitude)
    {
        if (Camera.main == null) yield break;
        Vector3 camOrigPos = Camera.main.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (Camera.main == null) yield break;
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            Camera.main.transform.position = new Vector3(camOrigPos.x + x, camOrigPos.y + y, camOrigPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (Camera.main != null) Camera.main.transform.position = camOrigPos;
    }

    private void EndBattle(bool isPlayerVictory)
    {
        isBattleActive = false;
        battleUI.Hide();

        // Restore original positions
        foreach(var f in allies)
        {
            if (f != null && f.fighterTransform != null)
                f.fighterTransform.position = f.origPos;
        }

        WeaponSummoner summoner = FindObjectOfType<WeaponSummoner>();
        if (summoner != null) summoner.ClearWeapons();

        gameManager.EndBattle(isPlayerVictory);
    }

    private void PlaySynthSound(ActionType type, float freq, float duration)
    {
        GameObject synthObj = new GameObject("CombatSynthSound");
        AudioSource src = synthObj.AddComponent<AudioSource>();
        src.volume = 0.4f;

        CombatSoundSynth synth = synthObj.AddComponent<CombatSoundSynth>();
        synth.Setup(type, freq, duration);

        src.Play();
        Destroy(synthObj, duration + 0.3f);
    }
}

public class CombatSoundSynth : MonoBehaviour
{
    private ActionType actionType;
    private float baseFreq;
    private float duration;
    private double phase = 0.0;
    private double sampling_frequency = 48000;
    private long sampleCount = 0;

    public void Setup(ActionType type, float freq, float dur)
    {
        actionType = type;
        baseFreq = freq;
        duration = dur;
    }

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

            double pitch = baseFreq;
            float sample = 0f;

            if (actionType == ActionType.Attack || actionType == ActionType.Slash)
            {
                pitch = Mathf.Lerp(baseFreq, baseFreq * 0.1f, t / duration);
                phase += 2.0 * Mathf.PI * pitch / doubleSampleRate;
                float sine = (float)Mathf.Sin((float)phase);
                float noise = (float)(new System.Random().NextDouble() * 2.0 - 1.0);
                sample = sine * 0.5f + noise * 0.5f;
            }
            else if (actionType == ActionType.Shot)
            {
                float noise = (float)(new System.Random().NextDouble() * 2.0 - 1.0);
                sample = noise * 0.8f;
            }
            else if (actionType == ActionType.Heal)
            {
                pitch = Mathf.Lerp(baseFreq * 0.5f, baseFreq * 2.2f, t / duration);
                phase += 2.0 * Mathf.PI * pitch / doubleSampleRate;
                sample = (float)Mathf.Sin((float)phase) * 0.5f + (float)Mathf.Sin((float)(phase * 1.5)) * 0.5f;
            }
            else if (actionType == ActionType.EpicStrike)
            {
                pitch = Mathf.Lerp(baseFreq, baseFreq * 0.2f, t / duration);
                phase += 2.0 * Mathf.PI * pitch / doubleSampleRate;
                float noise = (float)(new System.Random().NextDouble() * 2.0 - 1.0);
                sample = (float)Mathf.Sin((float)phase) * 0.3f + noise * 0.7f;
            }

            float env = Mathf.Clamp01(1f - t / duration);

            for (int c = 0; c < channels; c++)
            {
                data[i + c] = sample * env * 0.3f;
            }

            if (phase > 2.0 * Mathf.PI * 100.0)
            {
                phase -= 2.0 * Mathf.PI * 100.0;
            }
            sampleCount++;
        }
    }
}
