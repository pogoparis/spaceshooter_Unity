using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FormationSpawner : MonoBehaviour
{
    public Transform enemyContainer;
    public Camera gameplayCamera;

    public List<GameObject> SpawnFormation(FormationData formation)
    {
        var spawned = new List<GameObject>();

        if (formation == null || formation.enemyPrefab == null || formation.pathTemplate == null)
            return spawned;

        for (int i = 0; i < formation.enemyCount; i++)
        {
            // 1️ Calcul de l’offset de formation (UNE FOIS)
            Vector2 offset = ComputeOffset(formation, i);

            // 2️ Instanciation
            GameObject enemy = Instantiate(formation.enemyPrefab);
            enemy.transform.SetParent(enemyContainer, worldPositionStays: true);

            // 3️ Application de l’offset UNE SEULE FOIS
            enemy.transform.position += (Vector3)offset;

            // 4️ Désactivation temporaire (spawn delay)
            enemy.SetActive(false);

            // 5️ Configuration du PathFollower (SANS offset)
            PathFollower follower = enemy.GetComponent<PathFollower>();
            if (follower == null)
                follower = enemy.AddComponent<PathFollower>();

            follower.Configure(
                formation.pathTemplate,
                formation,
                Vector2.zero,   //  plus jamais d’offset ici
                i,
                gameplayCamera
            );

            spawned.Add(enemy);


            float delay = formation.pathTemplate.spawnDelayBetweenEnemies * i;
            StartCoroutine(EnableAfterDelay(enemy, delay));


        }

        return spawned;
    }

    private IEnumerator EnableAfterDelay(GameObject go, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (go != null)
            go.SetActive(true);
    }

    private static Vector2 ComputeOffset(FormationData f, int i)
    {
        switch (f.layoutType)
        {
            case FormationData.LayoutType.CustomOffsets:
                if (f.customOffsets != null && i < f.customOffsets.Count)
                    return f.customOffsets[i];
                return Vector2.zero;

            case FormationData.LayoutType.Line:
                return new Vector2(
                    (i - (f.enemyCount - 1) * 0.5f) * f.lineSpacing,
                    0f
                );

            case FormationData.LayoutType.Grid:
                {
                    int cols = Mathf.Max(1, f.gridCols);
                    int row = i / cols;
                    int col = i % cols;

                    float x = (col - (cols - 1) * 0.5f) * f.gridColSpacing;
                    float y = -(row * f.gridRowSpacing);
                    return new Vector2(x, y);
                }

            case FormationData.LayoutType.Circle:
                {
                    if (f.enemyCount <= 1)
                        return Vector2.zero;

                    float angle = (i / (float)f.enemyCount) * Mathf.PI * 2f;
                    return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * f.circleRadius;
                }

            default:
                return Vector2.zero;
        }
    }
}
