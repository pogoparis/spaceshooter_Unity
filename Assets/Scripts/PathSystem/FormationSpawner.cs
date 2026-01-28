using System.Collections;
using UnityEngine;

public class FormationSpawner : MonoBehaviour
{
    [Header("Refs")]
    public Transform enemyContainer;
    public Camera mainCamera;

    void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    // === APPELÉ PAR WaveManager ===
    public void SpawnFormation(
        FormationData formation,
        int repeatCount,
        float repeatDelay
    )
    {
        StartCoroutine(SpawnFormationRoutine(
            formation,
            repeatCount,
            repeatDelay
        ));
    }

    // === GÈRE LES RÉPÉTITIONS (PDF) ===
    private IEnumerator SpawnFormationRoutine(
        FormationData formation,
        int repeatCount,
        float repeatDelay
    )
    {
        for (int i = 0; i < repeatCount; i++)
        {
            SpawnFormationOnce(formation);

            if (i < repeatCount - 1 && repeatDelay > 0f)
                yield return new WaitForSeconds(repeatDelay);
        }
    }

    // === SPAWN RÉEL DES ENNEMIS ===
    private void SpawnFormationOnce(FormationData formation)
    {
        if (formation.enemyPrefab == null ||
            formation.pathTemplate == null)
            return;

        int count = formation.enemyCount;

        for (int i = 0; i < count; i++)
        {
            Vector2 offset = formation.GetOffset(i);

            GameObject enemy = Instantiate(
                formation.enemyPrefab,
                Vector3.zero,
                Quaternion.identity,
                enemyContainer
            );

            PathFollower pf = enemy.GetComponent<PathFollower>();
            if (pf != null)
            {
                pf.Configure(
                    formation.pathTemplate,
                    formation,
                    offset,
                    i,
                    mainCamera
                );
            }
        }
    }
}
