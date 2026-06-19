using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BulletTrail : MonoBehaviour
{
    public float lifetime = 0.15f;
    public float fadeSpeed = 12f;

    private LineRenderer lr;
    private float timer;
    private float startWidth;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        startWidth = lr.startWidth;
    }

    public void Init(Vector3 start, Vector3 end)
    {
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        timer = lifetime;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        float t = timer / lifetime;
        float width = Mathf.Lerp(0, startWidth, t * fadeSpeed);
        lr.startWidth = width;
        lr.endWidth = width;

        Color c = lr.startColor;
        c.a = t;
        lr.startColor = c;
        lr.endColor = c;

        if (timer <= 0) Destroy(gameObject);
    }
}
