using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float speed = 2.5f;
    public int hp = 1;

    float bottomY;

    void OnEnable()
    {
        var cam = Camera.main;
        bottomY = (cam != null) ? (cam.transform.position.y - cam.orthographicSize - 2f) : -9999f;
    }

    void Update()
    {
        transform.position += Vector3.down * speed * Time.deltaTime;

        if (transform.position.y < bottomY)
            gameObject.SetActive(false); // prêt pour pooling si on veut plus tard
    }

    public void TakeHit(int dmg)
    {
        hp -= dmg;
        if (hp <= 0)
            gameObject.SetActive(false);
    }
}
