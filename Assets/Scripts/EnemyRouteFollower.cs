using System;
using UnityEngine;

public sealed class EnemyRouteFollower : MonoBehaviour
{
    [Header("Route (runtime)")]
    [SerializeField] EnemyRouteSO route;
    [SerializeField] bool snapToFirstNode = true;

    [Header("Debug")]
    [SerializeField] bool drawGizmos = false;
    [SerializeField] Color gizmoColor = Color.cyan;

    public event Action<int, string> OnNodeReached;
    public event Action OnRouteFinished;

    Vector3 basePos;
    int index;
    float pauseTimer;
    bool hasRoute;

    public bool HasRoute => hasRoute && route != null && route.IsValid;

    public void ApplyRoute(EnemyRouteSO newRoute)
    {
        route = newRoute;
        if (route == null || !route.IsValid)
        {
            ClearRoute();
            return;
        }

        hasRoute = true;
        basePos = transform.position;
        index = 0;
        pauseTimer = 0f;

        if (snapToFirstNode)
        {
            transform.position = ResolveWorld(route.nodes[0], basePos, transform.position.z);
        }

        // Consider node 0 reached immediately.
        var n0 = route.nodes[0];
        pauseTimer = Mathf.Max(0f, n0.pause);
        if (!string.IsNullOrEmpty(n0.actionId))
            OnNodeReached?.Invoke(0, n0.actionId);

        index = 1;
    }

    public void ClearRoute()
    {
        route = null;
        hasRoute = false;
        index = 0;
        pauseTimer = 0f;
    }

    void Update()
    {
        if (!HasRoute) return;

        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.deltaTime;
            return;
        }

        if (index >= route.nodes.Count)
        {
            HandleRouteEnd();
            return;
        }

        var node = route.nodes[index];
        Vector3 target = ResolveWorld(node, basePos, transform.position.z);
        float speed = Mathf.Max(0.01f, node.speed);

        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if ((transform.position - target).sqrMagnitude <= 0.000001f)
        {
            if (!string.IsNullOrEmpty(node.actionId))
                OnNodeReached?.Invoke(index, node.actionId);

            pauseTimer = Mathf.Max(0f, node.pause);
            index++;
        }
    }

    void HandleRouteEnd()
    {
        if (route == null)
        {
            ClearRoute();
            return;
        }

        if (route.loop)
        {
            index = 0;

            var n0 = route.nodes[0];
            if (!string.IsNullOrEmpty(n0.actionId))
                OnNodeReached?.Invoke(0, n0.actionId);

            pauseTimer = Mathf.Max(0f, n0.pause);
            index = 1;
            return;
        }

        var mode = route.endMode;
        OnRouteFinished?.Invoke();
        ClearRoute();

        if (mode == EnemyRouteSO.EndMode.Despawn)
            gameObject.SetActive(false);
    }

    Vector3 ResolveWorld(EnemyRouteSO.RouteNode node, Vector3 spawnPos, float z)
    {
        if (route != null && route.useViewportPoints)
        {
            var cam = Camera.main;
            if (cam == null)
                return new Vector3(node.offset.x, node.offset.y, z);

            float depth = Mathf.Abs(z - cam.transform.position.z);
            var wp = cam.ViewportToWorldPoint(new Vector3(node.offset.x, node.offset.y, depth));
            wp.z = z;
            return wp;
        }

        if (route != null && route.offsetsRelativeToSpawn)
            return spawnPos + (Vector3)node.offset;

        return new Vector3(node.offset.x, node.offset.y, z);
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        if (route == null || !route.IsValid) return;

        Gizmos.color = gizmoColor;

        Vector3 sp = Application.isPlaying ? basePos : transform.position;
        float z = transform.position.z;

        Vector3 prev = ResolveWorld(route.nodes[0], sp, z);
        Gizmos.DrawSphere(prev, 0.15f);

        for (int i = 1; i < route.nodes.Count; i++)
        {
            Vector3 p = ResolveWorld(route.nodes[i], sp, z);
            Gizmos.DrawLine(prev, p);
            Gizmos.DrawSphere(p, 0.15f);
            prev = p;
        }
    }
}
