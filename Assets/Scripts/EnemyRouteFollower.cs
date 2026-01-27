using UnityEngine;

[DisallowMultipleComponent]
public class EnemyRouteFollower : MonoBehaviour
{
    [SerializeField] private EnemyRouteSO route;

    private Vector3 basePos;
    private int index;
    private float pauseTimer;
    private bool finished;

    public bool HasRoute => route != null && route.IsValid && !finished;

    private bool paused;
    public void SetPaused(bool value) => paused = value;

    public void ApplyRoute(EnemyRouteSO newRoute)
    {
        route = newRoute;
        finished = false;
        pauseTimer = 0f;
        index = 0;
        basePos = transform.position;

        if (route != null && route.IsValid)
        {
            // Snap au premier point (si offset = 0,0 -> pas de saut)
            transform.position = ResolvePoint(route.nodes[0]);
            index = 1;
        }
    }

    public void ClearRoute()
    {
        route = null;
        finished = false;
        pauseTimer = 0f;
        index = 0;
    }

    private void Update()
    {
        if (!HasRoute) return;

        // Si formation fly-in / inFormation : on ne suit pas la route
        var fm = GetComponent<EnemyFormationMember>();
        if (fm != null && (fm.isFlyingIn || fm.inFormation)) return;

        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.deltaTime;
            return;
        }

        Vector3 target = ResolvePoint(route.nodes[index]);
        float spd = Mathf.Max(0.01f, route.nodes[index].speed);

        transform.position = Vector3.MoveTowards(transform.position, target, spd * Time.deltaTime);

        if ((transform.position - target).sqrMagnitude <= 0.0001f)
        {
            pauseTimer = Mathf.Max(0f, route.nodes[index].pause);
            index++;

            if (index >= route.nodes.Count)
            {
                if (route.loop)
                {
                    index = 0;
                }
                else
                {
                    switch (route.endMode)
                    {
                        case EnemyRouteSO.EndMode.StopHere:
                            finished = true;
                            break;

                        case EnemyRouteSO.EndMode.Despawn:
                            gameObject.SetActive(false);
                            break;

                        default:
                            // reprend la descente normale (Enemy.Update)
                            ClearRoute();
                            break;
                    }
                }
            }
        }
    }

    private Vector3 ResolvePoint(EnemyRouteSO.RouteNode node)
    {
        // basePos est déjà ton spawn world pos (tu le set dans ApplyRoute)
        return route.ResolvePoint(node, basePos, transform.position.z, Camera.main);
    }
}
