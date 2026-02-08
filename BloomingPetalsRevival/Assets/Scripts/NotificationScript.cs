using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;

public class NotificationScript : MonoBehaviour
{
    [Header("settings")]
    public GameObject NotificationPrefab;
    public Transform NotificationsParent;
    public float NotificationTime = 2f;
    public float NotificationSpacing = 50f;
    [Tooltip("ren increase this if you want")] public int MaxNotifications = 5;
    public float AnimationDuration = 0.3f;
    public float FirstNotificationY = -50f;

    public static NotificationScript instance;
    private List<GameObject> activeNotifications = new List<GameObject>();

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ShowNotification("this game sucks so bad ugh");
        }
    }

    public static void ShowNotificationStatic(string text)
    {
        if (instance != null)
            instance.ShowNotification(text);
    }

    public void ShowNotification(string text)
    {
        if (activeNotifications.Count >= MaxNotifications)
        {
            DestroyNotification(activeNotifications[0]);
        }

        GameObject notification = Instantiate(NotificationPrefab, NotificationsParent);
        notification.SetActive(true);

        RectTransform rt = notification.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(195, FirstNotificationY - activeNotifications.Count * NotificationSpacing);
        rt.localScale = Vector3.zero;


        TMP_Text textComponent = notification.transform.GetChild(0).GetComponent<TMP_Text>();
        textComponent.text = text;

        CanvasGroup canvasGroup = notification.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
        canvasGroup.DOFade(1f, AnimationDuration);
        notification.transform.DOScale(Vector3.one, AnimationDuration);
        activeNotifications.Add(notification);
        UpdateNotificationPositions();
        StartCoroutine(DestroyNotificationAfterTime(notification, NotificationTime));
    }

    private void UpdateNotificationPositions()
    {
        for (int i = 0; i < activeNotifications.Count; i++)
        {
            float targetY = FirstNotificationY - i * NotificationSpacing;
            RectTransform rt = activeNotifications[i].GetComponent<RectTransform>();
            Vector2 targetPos = new Vector2(195, targetY);
            rt.DOAnchorPos(targetPos, AnimationDuration);
        }
    }


    private System.Collections.IEnumerator DestroyNotificationAfterTime(GameObject notification, float time)
    {
        yield return new WaitForSeconds(time);
        DestroyNotification(notification);
    }

    private void DestroyNotification(GameObject notification)
    {
        if (!activeNotifications.Contains(notification)) return;

        activeNotifications.Remove(notification);

        CanvasGroup canvasGroup = notification.GetComponent<CanvasGroup>();
        notification.transform.DOScale(Vector3.zero, AnimationDuration);
        canvasGroup.DOFade(0f, AnimationDuration).OnComplete(() =>
        {
            Destroy(notification);
            UpdateNotificationPositions();
        });
    }
}
