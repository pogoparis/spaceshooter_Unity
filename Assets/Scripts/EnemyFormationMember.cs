using System.Collections;
using UnityEngine;

public class EnemyFormationMember : MonoBehaviour
{
    public bool inFormation { get; private set; }
    public bool isFlyingIn { get; private set; }

    Coroutine moveCo;

    public void FlyIn(Vector3 start, Vector3 control, Vector3 target, float duration, Transform formationParent, Vector3 localSlot)
    {
        if (moveCo != null) StopCoroutine(moveCo);

        inFormation = false;
        isFlyingIn = true;

        moveCo = StartCoroutine(FlyInCo(start, control, target, duration, formationParent, localSlot));
    }

    IEnumerator FlyInCo(Vector3 start, Vector3 control, Vector3 target, float duration, Transform formationParent, Vector3 localSlot)
    {
        transform.SetParent(null);
        transform.position = start;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, duration);
            float tt = Mathf.Clamp01(t);
            transform.position = Bezier2(start, control, target, tt);
            yield return null;
        }

        transform.position = target;
        transform.SetParent(formationParent);
        transform.localPosition = localSlot;

        isFlyingIn = false;
        inFormation = true;
        moveCo = null;
    }

    static Vector3 Bezier2(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1f - t;
        return (u * u) * p0 + (2f * u * t) * p1 + (t * t) * p2;
    }
}
