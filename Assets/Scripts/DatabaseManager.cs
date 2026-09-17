using System;
using System.Collections.Generic;
using System.IO;
using SQLite;
using UnityEngine;

public class ScoreRecord
{
    [PrimaryKey, AutoIncrement]
    public int player_id { get; set; }
    public string player_name { get; set; }
    public int score { get; set; }
    public int wave_reached { get; set; }
    public int climate_state { get; set; }
    public int oxygen_remaining { get; set; }
    public string result { get; set; }
    public string created_at { get; set; }
}

public class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager Instance;

    private SQLiteConnection _db;
    private string _dbPath;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitDB();
    }

    void InitDB()
    {
        _dbPath = Path.Combine(Application.persistentDataPath, "group26_scores.db");
        _db = new SQLiteConnection(_dbPath);
        _db.CreateTable<ScoreRecord>();
        Debug.Log($"[DB] SQLite ready at: {_dbPath}");
    }

    public void SaveScore(string playerName, int score, int wave, int climate, int oxygen, string result)
    {
        var rec = new ScoreRecord
        {
            player_name = string.IsNullOrEmpty(playerName) ? "Anonymous" : playerName,
            score = score,
            wave_reached = wave,
            climate_state = climate,
            oxygen_remaining = oxygen,
            result = result,
            created_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
        _db.Insert(rec);
        Debug.Log($"[DB] Saved: {rec.player_name} | {rec.score} | {rec.result}");
    }

    public List<ScoreRecord> GetTopScores(int limit = 10)
    {
        return _db.Query<ScoreRecord>(
            "SELECT * FROM ScoreRecord ORDER BY score DESC, created_at DESC LIMIT ?", limit);
    }

    public void Close()
    {
        _db?.Close();
        _db = null;
    }

    void OnApplicationQuit() => Close();
}