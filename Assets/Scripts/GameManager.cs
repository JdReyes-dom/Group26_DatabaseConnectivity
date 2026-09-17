using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    // Singleton
    public static GameManager Instance;

    // References
    public GameObject allyUnitPrefab;
    public GameObject enemyUnitPrefab;
    public Transform[] lanes; // 6 lane positions
    public Slider climateBar;
    public TextMeshProUGUI oxygenText;
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI climateText;
    public GameObject[] unitButtons;
    public TextMeshProUGUI[] unitCostTexts;
    public TextMeshProUGUI announcementText; // Announcement text
    public float announcementDuration = 2.5f; // How long to show announcement

    // Game State
    public int oxygenPoints = 5;
    public int maxOxygenPoints = 5;
    public int waveStartingOxygen = 5;
    public int currentWave = 1;
    public int maxWaves = 3;
    public int climateState = 0; // 0-6
    public int selectedUnitType = 1; // 1, 2, 3, 4, 5
    public static string CurrentPlayerName = "Anonymous";
    private int enemiesDefeatedTotal = 0;
    private bool scoreSaved = false;
    public bool isGameOver = false;

    // Lane tracking
    private Dictionary<int, GameObject> allyInLane = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> enemyInLane = new Dictionary<int, GameObject>();

    // Oxygen tracking
    private int turnsInCurrentWave = 0;

    // Turn result tracking
    private int enemiesDefeatedThisTurn = 0;
    private int alliesLostThisTurn = 0;
    private int climateDamageThisTurn = 0;
    private int oxygenPenaltyApplied = 0;

    // Unit costs (index 0 = unit type 1, etc.)
    private int[] unitCosts = { 1, 2, 3, 4, 5 };

    // Enemy spawn configuration per wave
    private int[] enemiesPerWave = { 2, 4, 6 };
    private int[] enemyPowerPerWave = { 1, 2, 3 };
    private int[] oxygenPerWave = { 5, 7, 10 };

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        ResetScoreTracking();

        isGameOver = false;
        turnsInCurrentWave = 0;
        waveStartingOxygen = oxygenPerWave[0];
        maxOxygenPoints = waveStartingOxygen;
        oxygenPoints = maxOxygenPoints;

        if (announcementText != null)
            announcementText.gameObject.SetActive(false);

        UpdateUI();
        StartWave(currentWave);
    }

    // ============================================================
    // DATABASE / SCORE TRACKING HELPERS
    // ============================================================

    public void RegisterEnemyDefeated()
    {
        enemiesDefeatedTotal++;
    }

    private int ComputeScore()
    {
        int waveBonus = currentWave * 100;
        int enemyBonus = enemiesDefeatedTotal * 10;
        int winBonus = (isGameOver && climateState < 6 && currentWave >= maxWaves) ? 500 : 0;
        return waveBonus + enemyBonus + winBonus;
    }

    private void SaveCurrentRun(string result)
    {
        if (scoreSaved) return;
        scoreSaved = true;

        if (DatabaseManager.Instance == null)
        {
            Debug.LogWarning("[GameManager] DatabaseManager not found! Score not saved.");
            return;
        }

        int finalScore = ComputeScore();
        DatabaseManager.Instance.SaveScore(
            CurrentPlayerName,
            finalScore,
            currentWave,
            Mathf.Clamp(climateState, 0, 6),
            oxygenPoints,
            result
        );
        Debug.Log($"[GameManager] Run saved. Score={finalScore}, Result={result}");
    }

    public void ResetScoreTracking()
    {
        enemiesDefeatedTotal = 0;
        scoreSaved = false;
    }

    // ============================================================
    // ANNOUNCEMENTS
    // ============================================================

    void ShowAnnouncement(string message, Color color)
    {
        if (announcementText == null) return;

        announcementText.text = message;
        announcementText.color = color;
        announcementText.gameObject.SetActive(true);

        // Hide after duration
        CancelInvoke("HideAnnouncement");
        Invoke("HideAnnouncement", announcementDuration);
    }

    void HideAnnouncement()
    {
        if (announcementText != null)
            announcementText.gameObject.SetActive(false);
    }

    // ============================================================
    // PLAYER ACTIONS
    // ============================================================

    public void SelectUnit(int unitType)
    {
        selectedUnitType = unitType;
        Debug.Log("Selected Unit Type: " + unitType + " (Cost: " + GetUnitCost(unitType) + ")");
    }

    public void DeployAlly(int laneIndex)
    {
        if (isGameOver)
        {
            Debug.Log("Game is over! Cannot deploy.");
            return;
        }

        if (allyInLane.ContainsKey(laneIndex))
        {
            Debug.Log("Lane already has an ally!");
            return;
        }

        int cost = GetUnitCost(selectedUnitType);
        if (oxygenPoints < cost)
        {
            Debug.Log($"Not enough oxygen! Need: {cost}, Have: {oxygenPoints}");
            ShowAnnouncement($"❌ Not enough oxygen! Need: {cost}", Color.red);
            return;
        }

        oxygenPoints -= cost;

        Vector3 spawnPos = lanes[laneIndex].position + new Vector3(-3f, 0, 0);
        GameObject newAlly = Instantiate(allyUnitPrefab, spawnPos, Quaternion.identity);
        newAlly.GetComponent<AllyUnit>().powerLevel = selectedUnitType;
        newAlly.GetComponent<AllyUnit>().laneIndex = laneIndex;

        allyInLane[laneIndex] = newAlly;

        Debug.Log($"Deployed Unit {selectedUnitType} (Cost: {cost}). Oxygen now: {oxygenPoints}");
        ShowAnnouncement($"✅ Deployed Unit {selectedUnitType} (Cost: {cost})", Color.green);
        UpdateUI();
    }

    // ============================================================
    // HELPERS
    // ============================================================

    int GetUnitCost(int unitType)
    {
        return unitCosts[unitType - 1];
    }

    int GetTurnPenalty(int turn)
    {
        if (turn <= 1)
            return 0;
        if (turn == 2)
            return 1;
        return Mathf.RoundToInt(Mathf.Pow(2, turn - 2));
    }

    void UpdateOxygenDisplay()
    {
        int currentTurn = turnsInCurrentWave + 1;
        int penalty = GetTurnPenalty(currentTurn);
        int maxOxygenForThisTurn = waveStartingOxygen - penalty;
        oxygenPoints = Mathf.Max(1, maxOxygenForThisTurn);

        Debug.Log($"=== OXYGEN UPDATE ===");
        Debug.Log($"Wave Starting: {waveStartingOxygen}, Turn: {currentTurn}, Penalty: {penalty}");
        Debug.Log($"Oxygen Points: {oxygenPoints}");
    }

    // ============================================================
    // WAVE MANAGEMENT
    // ============================================================

    void StartWave(int waveNumber)
    {
        turnsInCurrentWave = 0;
        Debug.Log($"=== WAVE {waveNumber} STARTED! ===");

        foreach (var enemy in FindObjectsOfType<EnemyUnit>())
        {
            Destroy(enemy.gameObject);
        }
        enemyInLane.Clear();

        waveStartingOxygen = oxygenPerWave[waveNumber - 1];
        maxOxygenPoints = waveStartingOxygen;

        UpdateOxygenDisplay();

        int enemyCount = enemiesPerWave[waveNumber - 1];
        int enemyPower = enemyPowerPerWave[waveNumber - 1];

        List<int> availableLanes = new List<int> { 0, 1, 2, 3, 4, 5 };

        for (int i = 0; i < enemyCount; i++)
        {
            int laneIndex = availableLanes[Random.Range(0, availableLanes.Count)];
            availableLanes.Remove(laneIndex);

            Vector3 spawnPos = lanes[laneIndex].position + new Vector3(3f, 0, 0);
            GameObject newEnemy = Instantiate(enemyUnitPrefab, spawnPos, Quaternion.identity);
            newEnemy.GetComponent<EnemyUnit>().powerLevel = enemyPower;
            newEnemy.GetComponent<EnemyUnit>().laneIndex = laneIndex;
            newEnemy.GetComponent<EnemyUnit>().waveNumber = waveNumber;

            enemyInLane[laneIndex] = newEnemy;
            Debug.Log("Spawned Enemy in Lane " + laneIndex);
        }

        waveText.text = "Wave: " + waveNumber + "/" + maxWaves;
        ShowAnnouncement($"🌊 WAVE {waveNumber} STARTED! Enemies: {enemyCount}", Color.cyan);
        UpdateUI();
    }

    // ============================================================
    // TURN RESOLUTION
    // ============================================================

    public void EndTurn()
    {
        if (isGameOver)
        {
            Debug.Log("Game is already over!");
            return;
        }

        // Reset turn tracking
        enemiesDefeatedThisTurn = 0;
        alliesLostThisTurn = 0;
        climateDamageThisTurn = 0;
        oxygenPenaltyApplied = 0;

        // Increment turn counter
        turnsInCurrentWave++;
        int currentTurn = turnsInCurrentWave;
        Debug.Log($"=== END TURN {currentTurn} (Wave {currentWave}) ===");

        // 1. Check empty lanes - enemies attack climate bar
        List<int> enemyLanes = new List<int>(enemyInLane.Keys);
        foreach (int laneIndex in enemyLanes)
        {
            if (!allyInLane.ContainsKey(laneIndex))
            {
                int damage = 1;
                climateState += damage;
                climateDamageThisTurn += damage;
                Debug.Log($"Climate attacked! Lane {laneIndex} has no defender. Climate: {climateState}");

                UpdateUI();

                if (climateState >= 6)
                {
                    isGameOver = true;
                    climateState = 6;
                    UpdateUI();
                    ShowAnnouncement("💀 GAME OVER! Climate destroyed!", Color.red);
                    waveText.text = "GAME OVER!\nClimate destroyed!";
                    SaveCurrentRun("LOSE");
                    return;
                }
            }
        }

        // 2. Resolve combat
        bool combatHappened = true;
        int maxIterations = 30;
        int iterations = 0;

        while (combatHappened && iterations < maxIterations && !isGameOver)
        {
            combatHappened = false;
            iterations++;

            List<int> lanesToProcess = new List<int>(allyInLane.Keys);
            foreach (int laneIndex in lanesToProcess)
            {
                if (allyInLane.ContainsKey(laneIndex) && enemyInLane.ContainsKey(laneIndex))
                {
                    GameObject allyObj = allyInLane[laneIndex];
                    GameObject enemyObj = enemyInLane[laneIndex];

                    if (allyObj == null || enemyObj == null) continue;

                    AllyUnit ally = allyObj.GetComponent<AllyUnit>();
                    EnemyUnit enemy = enemyObj.GetComponent<EnemyUnit>();

                    if (ally == null || enemy == null) continue;

                    int allyDamage = GetDamage(ally.powerLevel, true);
                    int enemyDamage = GetDamage(enemy.powerLevel, false);

                    // Check if enemy will die from this hit
                    if (enemy.currentHP - allyDamage <= 0)
                    {
                        enemiesDefeatedThisTurn++;
                        enemiesDefeatedTotal++;
                    }

                    // Check if ally will die from this hit
                    if (ally.currentHP - enemyDamage <= 0)
                        alliesLostThisTurn++;

                    ally.TakeDamage(enemyDamage);
                    enemy.TakeDamage(allyDamage);

                    combatHappened = true;
                    Debug.Log($"Lane {laneIndex}: Ally deals {allyDamage} dmg, Enemy deals {enemyDamage} dmg");
                }
            }

            UpdateUI();

            if (climateState >= 6)
            {
                isGameOver = true;
                climateState = 6;
                UpdateUI();
                ShowAnnouncement("💀 GAME OVER! Climate destroyed!", Color.red);
                waveText.text = "GAME OVER!";
                SaveCurrentRun("LOSE");
                return;
            }
        }

        // 3. Check if wave is complete
        Debug.Log($"Enemies remaining: {enemyInLane.Count}");

        if (enemyInLane.Count == 0)
        {
            Debug.Log($"Wave {currentWave} is complete!");

            if (currentWave >= maxWaves)
            {
                isGameOver = true;

                foreach (var enemy in FindObjectsOfType<EnemyUnit>())
                {
                    Destroy(enemy.gameObject);
                }
                enemyInLane.Clear();

                ShowAnnouncement("🏆 YOU WIN! All waves cleared!", Color.yellow);
                waveText.text = "YOU WIN!";
                UpdateUI();
                SaveCurrentRun("WIN");
                return;
            }

            currentWave++;
            StartWave(currentWave);
            return;
        }

        // 4. Apply oxygen penalty
        int nextTurn = turnsInCurrentWave + 1;
        int penalty = GetTurnPenalty(nextTurn);
        oxygenPenaltyApplied = penalty;
        UpdateOxygenDisplay();
        UpdateUI();

        // 5. Build announcement message
        string announcementMsg = "";
        bool hasEvents = false;

        if (enemiesDefeatedThisTurn > 0)
        {
            announcementMsg += $"⚔️ {enemiesDefeatedThisTurn} enemies defeated! ";
            hasEvents = true;
        }

        if (alliesLostThisTurn > 0)
        {
            announcementMsg += $"💔 {alliesLostThisTurn} allies lost! ";
            hasEvents = true;
        }

        if (climateDamageThisTurn > 0)
        {
            announcementMsg += $"🔥 Climate +{climateDamageThisTurn}! ";
            hasEvents = true;
        }

        if (oxygenPenaltyApplied > 0)
        {
            announcementMsg += $"💨 Oxygen -{oxygenPenaltyApplied} (Penalty) ";
            hasEvents = true;
        }

        // Always show turn number
        announcementMsg = $"📊 Turn {currentTurn} ended. " + announcementMsg;

        // Add remaining enemies
        announcementMsg += $"\n👾 {enemyInLane.Count} enemies remaining";

        // Add oxygen status
        if (oxygenPoints <= 3)
        {
            announcementMsg += $"\n⚠️ Oxygen: {oxygenPoints} - CRITICAL!";
        }

        if (!hasEvents && oxygenPenaltyApplied == 0)
        {
            announcementMsg += "\n⏳ No significant events this turn.";
        }

        // Show announcement with color based on severity
        Color announceColor = oxygenPoints <= 3 ? Color.red : Color.white;
        if (alliesLostThisTurn > enemiesDefeatedThisTurn)
            announceColor = new Color(1f, 0.5f, 0f); // Orange - bad turn

        ShowAnnouncement(announcementMsg, announceColor);

        // 6. Check if oxygen reached minimum
        if (oxygenPoints <= 1)
        {
            Debug.LogWarning("Oxygen at minimum! Player must clear wave to refresh.");
        }

        // 7. Check climate
        if (climateState >= 6)
        {
            isGameOver = true;
            climateState = 6;
            UpdateUI();
            ShowAnnouncement("💀 GAME OVER! Climate destroyed!", Color.red);
            waveText.text = "GAME OVER!";
            SaveCurrentRun("LOSE");
            return;
        }
    }

    int GetDamage(int powerLevel, bool isAlly)
    {
        if (isAlly)
        {
            switch (powerLevel)
            {
                case 1: return 2;
                case 2: return 4;
                case 3: return 6;
                case 4: return 8;
                case 5: return 10;
                default: return 2;
            }
        }
        else
        {
            switch (powerLevel)
            {
                case 1: return 3;
                case 2: return 5;
                case 3: return 7;
                default: return 3;
            }
        }
    }

    public void UpdateUI()
    {
        if (oxygenText != null)
            oxygenText.text = "Oxygen: " + oxygenPoints + "/" + maxOxygenPoints;

        if (climateBar != null)
        {
            climateBar.maxValue = 6;
            climateBar.value = Mathf.Clamp(climateState, 0, 6);
        }

        if (climateText != null)
            climateText.text = "Climate: " + Mathf.Clamp(climateState, 0, 6) + "/6";

        for (int i = 0; i < unitButtons.Length; i++)
        {
            if (unitButtons[i] != null)
            {
                int unitType = i + 1;
                int cost = GetUnitCost(unitType);

                if (unitCostTexts != null && i < unitCostTexts.Length && unitCostTexts[i] != null)
                {
                    unitCostTexts[i].text = "Unit " + unitType + " (Cost: " + cost + ")";
                }

                Button btn = unitButtons[i].GetComponent<Button>();
                if (btn != null)
                {
                    btn.interactable = oxygenPoints >= cost && !isGameOver;
                }
            }
        }
    }

    public void RemoveAllyFromLane(int laneIndex)
    {
        if (allyInLane.ContainsKey(laneIndex))
        {
            allyInLane.Remove(laneIndex);
            Debug.Log($"Ally removed from Lane {laneIndex}. Allies remaining: {allyInLane.Count}");
        }
    }

    public void RemoveEnemyFromLane(int laneIndex)
    {
        if (enemyInLane.ContainsKey(laneIndex))
        {
            enemyInLane.Remove(laneIndex);
            Debug.Log($"Enemy removed from Lane {laneIndex}. Enemies remaining: {enemyInLane.Count}");
            UpdateUI();
        }
    }

    public void RestartGame()
    {
        isGameOver = false;
        ResetScoreTracking();

        climateState = 0;
        currentWave = 1;
        turnsInCurrentWave = 0;
        alliesLostThisTurn = 0;
        enemiesDefeatedThisTurn = 0;
        climateDamageThisTurn = 0;
        oxygenPenaltyApplied = 0;
        allyInLane.Clear();
        enemyInLane.Clear();

        foreach (var ally in FindObjectsOfType<AllyUnit>())
        {
            Destroy(ally.gameObject);
        }
        foreach (var enemy in FindObjectsOfType<EnemyUnit>())
        {
            Destroy(enemy.gameObject);
        }

        waveStartingOxygen = oxygenPerWave[0];
        maxOxygenPoints = waveStartingOxygen;
        oxygenPoints = maxOxygenPoints;
        waveText.text = "Wave: 1/" + maxWaves;
        HideAnnouncement();
        StartWave(currentWave);
        UpdateUI();
    }
}