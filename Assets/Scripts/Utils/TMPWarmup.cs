using TMPro;
using UnityEngine;

public class TMPWarmup : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset fontAsset;

    private void Awake()
    {
        GameObject hiddenObject = new GameObject("TMP Warmup");
        TMP_Text tmp = hiddenObject.AddComponent<TextMeshProUGUI>();
        tmp.font = fontAsset;
        tmp.text = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,!?";
        tmp.ForceMeshUpdate();
        hiddenObject.SetActive(false);
    }
}
