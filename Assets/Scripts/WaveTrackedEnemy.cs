using UnityEngine;

public sealed class WaveTrackedEnemy : MonoBehaviour
{
    private WaveManager owner;
    private bool reported;

    public void Init(WaveManager waveManager) => owner = waveManager;

    private void OnEnable() => reported = false;

    private void OnDisable() => ReportGone();
    private void OnDestroy() => ReportGone();

    private void ReportGone()
    {
        if (reported) return;
        reported = true;
        owner?.NotifyEnemyGone(this);
    }
}
