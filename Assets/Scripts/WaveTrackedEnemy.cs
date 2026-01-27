using UnityEngine;

public class WaveTrackedEnemy : MonoBehaviour
{
    WaveManager manager;
    bool notified;

    public void Init(WaveManager m)
    {
        manager = m;
        notified = false;
    }

    void OnDisable()
    {
        NotifyGone();
    }

    void OnDestroy()
    {
        NotifyGone();
    }

    void NotifyGone()
    {
        if (notified) return;
        notified = true;

        if (manager != null)
            manager.NotifyEnemyGone(this);
    }
}
