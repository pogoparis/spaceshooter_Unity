using UnityEngine;

public class InfiniteBackgroundScroller : MonoBehaviour
{
    public Transform bg1;
    public Transform bg2;
    public float scrollSpeed = 2.5f;

    float bgHeight;

    void Start()
    {
        if (bg1 == null || bg2 == null)
        {
            UnityEngine.Debug.LogError("Assign bg1 and bg2 in Inspector.");
            enabled = false;
            return;
        }

        var sr = bg1.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            UnityEngine.Debug.LogError("bg1 needs a SpriteRenderer.");
            enabled = false;
            return;
        }

        bgHeight = sr.bounds.size.y;

        // Force positions to avoid weird offsets
        bg1.localPosition = Vector3.zero;
        bg2.localPosition = Vector3.up * bgHeight;
    }

    void Update()
    {
        Vector3 move = Vector3.down * scrollSpeed * Time.deltaTime;
        bg1.localPosition += move;
        bg2.localPosition += move;

        // recycle based on local positions
        if (bg1.localPosition.y <= -bgHeight)
            bg1.localPosition = bg2.localPosition + Vector3.up * bgHeight;

        if (bg2.localPosition.y <= -bgHeight)
            bg2.localPosition = bg1.localPosition + Vector3.up * bgHeight;
    }
}
