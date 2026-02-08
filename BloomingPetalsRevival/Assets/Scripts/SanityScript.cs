using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class SanityScript : MonoBehaviour
{

    public int CurrentSanity = 100;
    public TMP_Text SanityLabel;

    public static SanityScript instance;

    private void Start()
    {
        instance = this;
    }

    private void Update()
    {
        Mathf.Clamp(CurrentSanity, 0, 100);
    }
    public void SetSanity(int targetValue, float duration)
    {
        DOTween.To(
            () => CurrentSanity,
            x =>
            {
                CurrentSanity = x;
                SanityLabel.text = $"Sanity: {CurrentSanity.ToString()}%";
            },
            targetValue,
            duration
        );
    }
}
