using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class PromptManager : MonoBehaviour
{

    public ButtonType[] buttons;
    public LayerMask wallMask;
    public Camera mainCamera;

    ProtagonistScript player;

    private void Start()
    {
        player = FindObjectOfType<ProtagonistScript>();
    }

    private void Update()
    {

        for (int i = 0; i < buttons.Length; i++)
        {

            ButtonType button = buttons[i];

            if (buttons[i].nearestPrompts.Count > 0)
            {
                buttons[i].currentPrompt = GetNearestPrompt(buttons[i].nearestPrompts.ToArray());
                PromptScript currentPrompt = buttons[i].currentPrompt;


                if(mainCamera != null)
                {
                    Vector2 worldToScreen = mainCamera.WorldToScreenPoint(currentPrompt.AdjustedPosition);

                    button.promptParent.gameObject.SetActive(true);
                    button.promptParent.position = mainCamera.ScreenToWorldPoint(new Vector3(worldToScreen.x, worldToScreen.y, 1));
                }

                button.label.text = currentPrompt.Text;


                if (Input.GetKey(button.keyCode))
                {
                    if (currentPrompt.RemainingTimer <= 0f) currentPrompt.Pressed = true;
                    else currentPrompt.RemainingTimer -= Time.deltaTime;
                }
                else
                {

                    currentPrompt.Pressed = false;
                    currentPrompt.RemainingTimer = currentPrompt.FillTimer;
                }


                button.background.fillAmount = currentPrompt.RemainingTimer / currentPrompt.FillTimer;
            }
            else
            {
                button.promptParent.gameObject.SetActive(false);
                buttons[i].currentPrompt = null;
            }
        }
    }

    public PromptScript GetNearestPrompt(PromptScript[] Prompts)
    {
        PromptScript nearestPrompt = null;
        float minDist = Mathf.Infinity;
        Vector3 currentPos = player.transform.position;
        foreach (PromptScript prompt in Prompts)
        {
            if (prompt)
            {
                float dist = Vector3.Distance(prompt.transform.position, currentPos);
                if (dist < minDist)
                {
                    nearestPrompt = prompt;
                    minDist = dist;
                }
            }
        }
        return nearestPrompt;
    }
}

[System.Serializable]
public class ButtonType
{

    public KeyCode keyCode;
    public Transform promptParent;
    public Image background;
    public TextMeshProUGUI label;

    [HideInInspector]
    public PromptScript currentPrompt;
    [HideInInspector]
    public List<PromptScript> nearestPrompts;
}