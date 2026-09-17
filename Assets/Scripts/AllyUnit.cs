using UnityEngine;
using UnityEngine.UI;

public class AllyUnit : MonoBehaviour
{
    public int powerLevel = 1;
    public int laneIndex = 0;
    public int maxHP = 10;
    public int currentHP;
    private bool isDead = false;

    void Start()
    {
        switch (powerLevel)
        {
            case 1: maxHP = 10; break;
            case 2: maxHP = 16; break;
            case 3: maxHP = 22; break;
            case 4: maxHP = 28; break;
            case 5: maxHP = 35; break;
            default: maxHP = 10; break;
        }
        currentHP = maxHP;
        UpdateVisuals();
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHP -= damage;
        Debug.Log("Ally took " + damage + " damage! HP: " + currentHP + "/" + maxHP);
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
            sr.color = Color.green;
        else if (hpPercent > 0.33f)
            sr.color = Color.yellow;
        else
            sr.color = Color.red;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("Ally in Lane " + laneIndex + " has been defeated!");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RemoveAllyFromLane(laneIndex);
            GameManager.Instance.UpdateUI();
        }

        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null && !isDead)
        {
            GameManager.Instance.RemoveAllyFromLane(laneIndex);
            GameManager.Instance.UpdateUI();
        }
    }
}