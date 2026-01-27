using UnityEngine;

[DisallowMultipleComponent]
public class EnemyRouteFollower : MonoBehaviour
{
    [SerializeField] private EnemyRouteSO route;

    private Vector3 basePos;
    private int index;
    private float pauseTimer;
    private bool finished;
    private bool paused;
    private Camera cam;

    public bool HasRoute => route != null && route.IsValid && !finished;

    public void SetPaused(bool value) => paused = value;

    public void RebaseToCurrentPosition()
    {
        basePos = transform.position;
    }

    private void Awake()
    {
        cam = Camera.main;
    }

    public void ApplyRoute(EnemyRouteSO newRoute)
    {
        route = newRoute;
        finished = false;
        pauseTimer = 0f;
        basePos = transform.position;

        if (route != null && route.IsValid)
        {
            transform.position = ResolvePoint(route.nodes[0]);
            index = 1;
        }
        else
        {
            index = 0;
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
        if (paused) return;
        if (!HasRoute) return;

        var fm = GetComponent<EnemyFormationMember>();
        if (fm != null && (fm.isFlyingIn || fm.inFormation)) return;

        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.deltaTime;
            return;
        }

        if (index < 0 || index >= route.nodes.Count)
        {
            finished = true;
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
                            ClearRoute();
                            break;
                    }
                }
            }
        }
    }

    private Vector3 ResolvePoint(EnemyRouteSO.RouteNode node)
    {
        if (cam == null) cam = Camera.main;
        return route.ResolvePoint(node, basePos, transform.position.z, cam);
    }
}
