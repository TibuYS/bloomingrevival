using UnityEngine;

public class BloodPool : MonoBehaviour
{
    public float thickness = 0.0001f;
    public float maxSize = 2f;
    public float growSpeed = 1f;
    public float lifetime = 300f;

    float currentSize = 0.1f;
    float timeAlive = 0f;

    Renderer rend;
    Material mat;

    Color freshColor = new Color(0.6f, 0f, 0f, 1f);
    Color dryColor = new Color(0.2f, 0f, 0f, 1f);

    public bool IsActive { get; private set; }

    void Awake()
    {
        rend = GetComponent<Renderer>();

        if (rend != null)
            mat = rend.material;
        else
            Debug.LogError("missing renderer");

        IsActive = false;
    }


    public void Activate(Vector3 pos, Quaternion rot)
    {
        if (rend == null)
            rend = GetComponentInChildren<Renderer>();

        if (mat == null && rend != null)
            mat = rend.material;

        transform.position = pos;
        transform.rotation = rot;

        currentSize = Random.Range(0.05f, 0.15f);
        timeAlive = 0f;

        transform.localScale = new Vector3(currentSize, thickness, currentSize);
        if (mat != null)
            mat.color = freshColor;

        IsActive = true;
        gameObject.SetActive(true);
    }

    void Update()
    {
        if (!IsActive) return;

        timeAlive += Time.deltaTime;

        Grow();
        Darken();

        if (timeAlive > lifetime)
            Deactivate();
    }

    void Grow()
    {
        if (currentSize >= maxSize) return;

        currentSize += growSpeed * Time.deltaTime;
        transform.localScale = new Vector3(currentSize, thickness, currentSize);
    }

    void Darken()
    {
        mat.color = Color.Lerp(freshColor, dryColor, timeAlive / lifetime);
    }

    public void Clean(float strength)
    {
        currentSize -= strength * Time.deltaTime;

        if (currentSize <= 0.05f)
            Deactivate();
        else
            transform.localScale = new Vector3(currentSize, 1f, currentSize);
    }

    public void Deactivate()
    {
        IsActive = false;
        gameObject.SetActive(false);
    }
}
