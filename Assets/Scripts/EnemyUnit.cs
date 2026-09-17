using UnityEngine;
using UnityEngine.UI;

public class EnemyUnit : MonoBehaviour
{
    public int powerLevel = 1;
    public int laneIndex = 0;
    public int waveNumber = 1;
    public int maxHP = 10;
    public int currentHP;
    private bool isDead = false;

    void Start()
    {
        int waveBonus = (waveNumber - 1) * 3;
        switch (powerLevel)
        {
            case 1: maxHP = 8 + waveBonus; break;
            case 2: maxHP = 14 + waveBonus; break;
            case 3: maxHP = 20 + waveBonus; break;
            default: maxHP = 8 + waveBonus; break;
        }
        currentHP = maxHP;
        UpdateVisuals();
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHP -= damage;
        Debug.Log("Enemy took " + damage + " damage! HP: " + currentHP + "/" + maxHP);
        UpdateVisuals();

        if (currentHP <= 0)
        {
            Die();
        }
    }

    void UpdateVisuals()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        float hpPercent = (float)currentHP / maxHP;

        if (hpPercent > 0.66f)
            sr.color = Color.red;
        else if (hpPercent > 0.33f)
            sr.color = new Color(1f, 0.5f, 0f);
        else
            sr.color = Color.gray;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("Enemy in Lane " + laneIndex + " has been defeated!");

        // Remove from GameManager's tracking immediately
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RemoveEnemyFromLane(laneIndex);
            // Force UI update
            GameManager.Instance.UpdateUI();
        }

        // Destroy the GameObject immediately
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        // Safety net - but Die() should handle it first
        if (GameManager.Instance != null && !isDead)
        {
            GameManager.Instance.RemoveEnemyFromLane(laneIndex);
            GameManager.Instance.UpdateUI();
        }
    }
}