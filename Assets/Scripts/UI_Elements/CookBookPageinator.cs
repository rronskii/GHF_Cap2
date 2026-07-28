using UnityEngine;

public class CookbookPaginator : MonoBehaviour
{
    [Header("Pages Setup")]
    [Tooltip("The empty GameObject parent that holds all your page panels/texts as children.")]
    public Transform pagesParent;

    private int currentPageIndex = 0;

    private void Start()
    {
        ShowPage(0);
    }

    private void OnEnable()
    {
        // Always reset to the first page when the cookbook is opened
        currentPageIndex = 0;
        ShowPage(currentPageIndex);
    }

    public void NextPage()
    {
        if (pagesParent == null) return;

        // Only go forward if we haven't reached the last page
        if (currentPageIndex < pagesParent.childCount - 1)
        {
            currentPageIndex++;
            ShowPage(currentPageIndex);
        }
    }

    public void PreviousPage()
    {
        if (pagesParent == null) return;

        // Only go back if we aren't on the first page
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            ShowPage(currentPageIndex);
        }
    }

    private void ShowPage(int index)
    {
        if (pagesParent == null) return;

        // Loop through every child and only enable the one that matches our current index
        for (int i = 0; i < pagesParent.childCount; i++)
        {
            pagesParent.GetChild(i).gameObject.SetActive(i == index);
        }
    }
}