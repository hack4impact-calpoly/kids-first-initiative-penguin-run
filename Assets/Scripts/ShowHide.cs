using UnityEngine;

public class ShowHide : MonoBehaviour
{
    public GameObject targetObject;

    public void Show()
    {
        targetObject.SetActive(true);
    }

    public void Hide()
    {
        targetObject.SetActive(false);
    }
}
