using TMPro;
using UnityEngine;

public sealed class GameRulesPopupController : MonoBehaviour
{
    [SerializeField] private GameObject[] pageObjects;
    [SerializeField] private TMP_Text pageText;
    [SerializeField] private UnityEngine.UI.Button previousButton;
    [SerializeField] private UnityEngine.UI.Button nextButton;
    private int pageIndex;

    private void OnEnable()
    {
        pageIndex = 0;
        RefreshPage();
    }

    public void Open()
    {
        pageIndex = 0;
        transform.SetAsLastSibling();
        SusBrush.UI.PopupTransition.SetVisible(gameObject, true);
        RefreshPage();
    }

    public void Close() => SusBrush.UI.PopupTransition.SetVisible(gameObject, false);
    public void Previous() { pageIndex--; RefreshPage(); }
    public void Next() { pageIndex++; RefreshPage(); }

    private void RefreshPage()
    {
        var count = pageObjects == null ? 0 : pageObjects.Length;
        pageIndex = Mathf.Clamp(pageIndex, 0, Mathf.Max(0, count - 1));
        for (var i = 0; i < count; i++)
            if (pageObjects[i] != null) pageObjects[i].SetActive(i == pageIndex);
        if (pageText != null) pageText.text = count == 0 ? "0 / 0" : $"{pageIndex + 1} / {count}";
        if (previousButton != null) previousButton.gameObject.SetActive(count > 0 && pageIndex > 0);
        if (nextButton != null) nextButton.gameObject.SetActive(pageIndex < count - 1);
    }
}
