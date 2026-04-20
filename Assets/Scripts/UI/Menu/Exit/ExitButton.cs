using UnityEngine;
using UnityEngine.UI;

public sealed class ExitButton : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private string _sceneName;

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
        if (string.IsNullOrWhiteSpace(_sceneName) == false)
        {
            SceneLoadingScreen.LoadScene(_sceneName);

            return;
        }

        Application.Quit();
    }
}
