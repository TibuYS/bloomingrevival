using TMPro;
using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class SubtitleManager : MonoBehaviour
{
    [Header("Interface")]
    public TextMeshProUGUI subtitlePrefab;

    [Header("Transforms")]
    public Transform subtitlesParent;

    [Header("Values")]
    public float displayDuration = 3f;
    public float fadeOutDuration = 1f;
    public float yOffsetBetweenSubtitles = 30f;
    public float moveDuration = 0.3f;

    private readonly Queue<TextMeshProUGUI> _subtitleQueue = new Queue<TextMeshProUGUI>();

    public static SubtitleManager instance;

    private void Awake()
    {
        instance = this;
    }

    public void DisplaySubtitle(string text, Color color, float duration = -1f)
    {
        if (duration > 0f)
            displayDuration = duration;

        TextMeshProUGUI newSubtitle = Instantiate(subtitlePrefab, subtitlesParent);
        newSubtitle.text = text;
        newSubtitle.color = color;

        RectTransform rect = newSubtitle.rectTransform;
        rect.anchoredPosition = new Vector2(0, -450f);

        _subtitleQueue.Enqueue(newSubtitle);

        UpdateSubtitlePositions();

        newSubtitle.DOFade(0, fadeOutDuration)
            .SetDelay(displayDuration)
            .OnComplete(() =>
            {
                _subtitleQueue.Dequeue();
                Destroy(newSubtitle.gameObject);
                UpdateSubtitlePositions();
            });
    }

    private void UpdateSubtitlePositions()
    {
        float targetY = -450f;
        foreach (TextMeshProUGUI subtitle in _subtitleQueue)
        {
            RectTransform rect = subtitle.rectTransform;
            rect.DOAnchorPosY(targetY, moveDuration).SetEase(Ease.OutCubic);
            targetY += yOffsetBetweenSubtitles;
        }
    }
}
