using UnityEngine;
using UnityEngine.UI;

public sealed class ExitButton : MonoBehaviour
{
    [SerializeField] private Button _button;

    private void OnEnable()
    {
        _button.onClick.AddListener(ExitGame);
    }
    private void OnDisable()
    {
        _button.onClick.RemoveListener(ExitGame);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}