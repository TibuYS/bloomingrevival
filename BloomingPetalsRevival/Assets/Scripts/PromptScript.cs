using UnityEngine;
using UnityEngine.Events;

public class PromptScript : MonoBehaviour
{

    [Header("Editor Values")]
    public UnityEvent OnPressed;
    [Space]

    public string Text = "None";
    public Transform Pivot;

    [Range(0, 4)]
    public int ButtonIndex;

    [Space]

    [Range(0, 2)]
    public float FillTimer = .5f;
    [Range(.5f, 5)]
    public float Distance = 1;

    [Space]

    [Range(0, 1)]
    public float OffsetX = 0;
    [Range(0, 1)]
    public float OffsetY = 0;

    [Space]

    public bool IgnoreWalk;

    [Header("Runtime Values")]

    [Space]

    [Range(0, 2)]
    public float RemainingTimer = 0;
    public Vector3 AdjustedPosition;

    [Space]

    public bool CanBePressed;
    public bool Nearby;
    public bool Pressed;

    private ProtagonistScript Player;
    private PromptManager Manager;
    private int PreviousIndex;

    private void Start()
    {
        if (!Pivot) Pivot = transform;

        Player = FindObjectOfType<ProtagonistScript>();
        Manager = FindObjectOfType<PromptManager>();
        UpdateData();
    }

    private void OnDisable()
    {

        Pressed = false;
        Nearby = false;

        for (int i = 0; i < Manager.buttons.Length; i++)
        {
            if (Manager.buttons[i].nearestPrompts.Contains(this))
            {
                Manager.buttons[i].nearestPrompts.Remove(this);
            }
        }
    }

    public void UpdateData()
    {

        for (int i = 0; i < Manager.buttons.Length; i++)
        {
            if (Manager.buttons[i].nearestPrompts.Contains(this))
            {
                Manager.buttons[i].nearestPrompts.Remove(this);
            }
        }

        if (!Pivot)
        {
            Pivot = transform;
        }

        Nearby = false;
        Pressed = false;

        RemainingTimer = FillTimer;

        PreviousIndex = ButtonIndex;
    }
    public void StopBeingPressed()
    {
        Pressed = false;
        RemainingTimer = FillTimer;
    }
    private void Update()
    {
        if (PreviousIndex != ButtonIndex)
        {
            UpdateData();
        }

        AdjustedPosition = Pivot.position;
        AdjustedPosition.x += OffsetX;
        AdjustedPosition.y += OffsetY;

        float dis = (Pivot.position - Player.transform.position).sqrMagnitude;

        CanBePressed = Player.CanMove || IgnoreWalk;
        Nearby = dis <= Distance * Distance && !Physics.Linecast(AdjustedPosition, Player.Hips.position, Manager.wallMask);



        if (Nearby && CanBePressed)
        {

            if (!Manager.buttons[ButtonIndex].nearestPrompts.Contains(this))
            {
                Manager.buttons[ButtonIndex].nearestPrompts.Add(this);
            }

        }

        else
        {
            Pressed = false;

            if (Manager.buttons[ButtonIndex].nearestPrompts.Contains(this))
            {
                Manager.buttons[ButtonIndex].nearestPrompts.Remove(this);
            }
        }

        PreviousIndex = ButtonIndex;
        if (Pressed)
        {
            OnPressed.Invoke();
            StopBeingPressed();
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (Pivot) Gizmos.DrawWireSphere(Pivot.position + new Vector3(OffsetX, OffsetY, 0), Distance);
        else Gizmos.DrawWireSphere(transform.position + new Vector3(OffsetX, OffsetY, 0), Distance);
    }
#endif
}