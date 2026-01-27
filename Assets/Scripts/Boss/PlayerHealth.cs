using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHp = 100;
    public int currentHp;

    void Awake()
    {
        currentHp = maxHp;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        currentHp -= amount;
        if (currentHp <= 0)
        {
            currentHp = 0;
            // For now: just disable player object
            gameObject.SetActive(false);
        }
    }
}
