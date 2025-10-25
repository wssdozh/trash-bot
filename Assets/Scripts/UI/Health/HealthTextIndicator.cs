using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthTextIndicator : StatIndicatorBase<Health>
{
    [SerializeField] private TextMeshProUGUI _text;

    protected override void Display()
    {
        if (_text == null)
            return;

        _text.text = _text.text = ((int)Stat.Value).ToString() + " / " + ((int)Stat.MaxValue).ToString();
    }
}
