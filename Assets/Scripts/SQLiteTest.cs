using UnityEngine;
using SQLite;
public class SQLiteTest : MonoBehaviour
{
    void Start()
    {
        var db = new SQLiteConnection(Application.persistentDataPath + "/test.db");
        db.CreateTable<TestRow>();
        Debug.Log("[SQLite] OK — DB created at " + Application.persistentDataPath);
    }
    public class TestRow { [PrimaryKey, AutoIncrement] public int id { get; set; } }
}