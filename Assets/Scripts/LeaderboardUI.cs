using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardUI : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject leaderboardPanel;
    public Transform contentParent;
    public GameObject rowPrefab;
    public Button closeButton;
    public Button openButton;

    [Header("Row Layout (Forced)")]
    public float rowHeight = 50f;
    public float rowSpacing = 5f;
    public float rowFontSize = 24f;

    void Start()
    {
        Debug.Log("[Leaderboard] Start() — initializing");

        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(false);
            Debug.Log("[Leaderboard] leaderboardPanel hidden at Start");
        }
        else
        {
            Debug.LogError("[Leaderboard] leaderboardPanel reference is NULL in Inspector!");
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
            closeButton.onClick.AddListener(ClosePanel);
            Debug.Log("[Leaderboard] closeButton listener added");
        }
        else
        {
            Debug.LogWarning("[Leaderboard] closeButton reference is NULL");
        }

        if (openButton != null)
        {
            openButton.onClick.RemoveListener(OpenPanel);
            openButton.onClick.AddListener(OpenPanel);
            Debug.Log("[Leaderboard] openButton listener added");
        }
        else
        {
            Debug.LogWarning("[Leaderboard] openButton reference is NULL");
        }

        if (contentParent == null)
            Debug.LogError("[Leaderboard] contentParent reference is NULL in Inspector!");
        if (rowPrefab == null)
            Debug.LogError("[Leaderboard] rowPrefab reference is NULL in Inspector!");
    }

    public void OpenPanel()
    {
        Debug.Log("[Leaderboard] OpenPanel() called");

        if (leaderboardPanel == null)
        {
            Debug.LogError("[Leaderboard] Cannot open — leaderboardPanel is NULL!");
            return;
        }

        leaderboardPanel.SetActive(true);
        Debug.Log("[Leaderboard] Panel set active. Refreshing...");
        Refresh();
    }

    public void ClosePanel()
    {
        Debug.Log("[Leaderboard] ClosePanel() called");
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);
    }

    void Refresh()
    {
        Debug.Log("========== [Leaderboard] Refresh() START ==========");

        // --- 1. Validate contentParent ---
        if (contentParent == null)
        {
            Debug.LogError("[Leaderboard] contentParent is NULL. Assign it in the Inspector. ABORT.");
            return;
        }
        Debug.Log($"[Leaderboard] contentParent = '{contentParent.name}'");

        // --- 2. Clear old rows ---
        int cleared = 0;
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
            cleared++;
        }
        Debug.Log($"[Leaderboard] Cleared {cleared} old row(s)");

        // --- 3. Validate DatabaseManager ---
        if (DatabaseManager.Instance == null)
        {
            Debug.LogError("[Leaderboard] DatabaseManager.Instance is NULL! Ensure the DatabaseManager GameObject is in the scene.");
            CreateRow("⚠ Database not connected.");
            return;
        }
        Debug.Log("[Leaderboard] DatabaseManager.Instance OK");

        // --- 4. Retrieve records ---
        List<ScoreRecord> records = null;
        try
        {
            records = DatabaseManager.Instance.GetTopScores(10);
            Debug.Log($"[Leaderboard] Retrieved {records?.Count ?? -1} records from DB");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Leaderboard] GetTopScores threw: {e.Message}\n{e.StackTrace}");
            CreateRow("⚠ Error reading database.");
            return;
        }

        // --- 5. Handle empty ---
        if (records == null || records.Count == 0)
        {
            Debug.Log("[Leaderboard] No records found. Showing placeholder.");
            CreateRow("No records yet. Play a round!");
            Debug.Log("========== [Leaderboard] Refresh() END (empty) ==========");
            return;
        }

        // --- 6. Prepare content RectTransform ---
        RectTransform contentRT = contentParent as RectTransform;
        if (contentRT == null)
        {
            Debug.LogError("[Leaderboard] contentParent is NOT a RectTransform! Cannot position rows.");
            return;
        }

        // Force content anchors/position (top-stretch)
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;

        Debug.Log($"[Leaderboard] Content BEFORE layout: pos={contentRT.anchoredPosition}, " +
                  $"size={contentRT.sizeDelta}, scale={contentRT.lossyScale}, " +
                  $"parent='{contentRT.parent.name}'");

        // --- 7. Create rows manually, bypassing Layout Group / Content Size Fitter ---
        float yOffset = 0f;
        int created = 0;

        for (int i = 0; i < records.Count; i++)
        {
            var r = records[i];
            if (r == null)
            {
                Debug.LogWarning($"[Leaderboard] Record {i} is NULL — skipping");
                continue;
            }

            Debug.Log($"[Leaderboard] Record {i + 1}: id={r.player_id} | name={r.player_name} | " +
                      $"score={r.score} | wave={r.wave_reached} | climate={r.climate_state} | " +
                      $"oxygen={r.oxygen_remaining} | result={r.result} | at={r.created_at}");

            // Instantiate row
            GameObject row = Instantiate(rowPrefab, contentParent);
            if (row == null)
            {
                Debug.LogError($"[Leaderboard] Instantiate returned NULL for record {i}");
                continue;
            }

            // Force RectTransform layout
            RectTransform rt = row.GetComponent<RectTransform>();
            if (rt == null)
            {
                Debug.LogError("[Leaderboard] Row prefab has NO RectTransform!");
                continue;
            }

            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, rowHeight);       // Width auto, Height fixed
            rt.anchoredPosition = new Vector2(0f, -yOffset);
            yOffset += rowHeight + rowSpacing;

            // Force TextMeshProUGUI properties
            TextMeshProUGUI tmp = row.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
                tmp = row.GetComponentInChildren<TextMeshProUGUI>();

            if (tmp != null)
            {
                tmp.text = $"{i + 1}. {r.player_name} — {r.score} pts | Wave {r.wave_reached} | {r.result} | {r.created_at}";
                tmp.fontSize = rowFontSize;
                tmp.color = Color.white;
                tmp.alignment = TextAlignmentOptions.Left;
                tmp.enableWordWrapping = true;
                tmp.overflowMode = TextOverflowModes.Overflow;
                Debug.Log($"[Leaderboard] Row {i + 1} text set: \"{tmp.text}\"");
            }
            else
            {
                Debug.LogError("[Leaderboard] Row prefab has NO TextMeshProUGUI (root or children)!");
            }

            // Force row active
            row.SetActive(true);
            created++;
        }

        // --- 8. Resize content to fit rows ---
        contentRT.sizeDelta = new Vector2(0f, yOffset);
        Debug.Log($"[Leaderboard] Content AFTER layout: size={contentRT.sizeDelta}, " +
                  $"worldPos={contentRT.position}, childCount={contentRT.childCount}");

        Debug.Log($"[Leaderboard] Created {created} row(s) in UI");
        Debug.Log("========== [Leaderboard] Refresh() END ==========");

        // --- 9. DIAGNOSTIC DUMP ---
        DumpDiagnostics(contentRT);
    }

    void CreateRow(string text)
    {
        if (rowPrefab == null || contentParent == null) return;

        GameObject row = Instantiate(rowPrefab, contentParent);
        TextMeshProUGUI tmp = row.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = row.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = text;
            tmp.fontSize = rowFontSize;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Left;
        }
        row.SetActive(true);
        Debug.Log($"[Leaderboard] Placeholder row: \"{text}\"");
    }

    void DumpDiagnostics(RectTransform contentRT)
    {
        Debug.Log("---------- [DIAG] UI STATE DUMP ----------");

        Debug.Log($"[DIAG] Screen size: {Screen.width} x {Screen.height}");

        // Canvas
        Canvas canvas = contentRT.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Debug.Log($"[DIAG] Canvas: renderMode={canvas.renderMode}, sortingOrder={canvas.sortingOrder}, " +
                      $"scaleFactor={canvas.scaleFactor}");
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                Debug.Log($"[DIAG] CanvasScaler: mode={scaler.uiScaleMode}, " +
                          $"refRes={scaler.referenceResolution}, match={scaler.matchWidthOrHeight}");
            }
        }

        // Content
        Debug.Log($"[DIAG] Content: name='{contentRT.name}', pos={contentRT.anchoredPosition}, " +
                  $"size={contentRT.sizeDelta}, worldPos={contentRT.position}, lossyScale={contentRT.lossyScale}");
        Debug.Log($"[DIAG] Content hierarchy path: {GetPath(contentRT)}");

        // Viewport
        RectTransform viewportRT = contentRT.parent as RectTransform;
        if (viewportRT != null)
        {
            Debug.Log($"[DIAG] Viewport: name='{viewportRT.name}', pos={viewportRT.anchoredPosition}, " +
                      $"size={viewportRT.sizeDelta}, worldPos={viewportRT.position}, lossyScale={viewportRT.lossyScale}");
        }

        // ScrollView
        if (viewportRT != null && viewportRT.parent != null)
        {
            RectTransform scrollRT = viewportRT.parent as RectTransform;
            if (scrollRT != null)
            {
                Debug.Log($"[DIAG] ScrollView: name='{scrollRT.name}', size={scrollRT.sizeDelta}, " +
                          $"worldPos={scrollRT.position}");
            }
        }

        // First row
        if (contentRT.childCount > 0)
        {
            RectTransform firstRow = contentRT.GetChild(0) as RectTransform;
            if (firstRow != null)
            {
                Debug.Log($"[DIAG] Row[0]: active={firstRow.gameObject.activeInHierarchy}, " +
                          $"pos={firstRow.anchoredPosition}, size={firstRow.sizeDelta}, " +
                          $"worldPos={firstRow.position}, lossyScale={firstRow.lossyScale}");
            }
        }
        else
        {
            Debug.LogWarning("[DIAG] Content has NO children!");
        }

        // Parent visibility chain
        Transform t = contentRT;
        int depth = 0;
        while (t != null && depth < 10)
        {
            Debug.Log($"[DIAG] Chain[{depth}]: '{t.name}' active={t.gameObject.activeSelf}, " +
                      $"scale={t.localScale}");
            t = t.parent;
            depth++;
        }

        Debug.Log("---------- [DIAG] END DUMP ----------");
    }

    string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}