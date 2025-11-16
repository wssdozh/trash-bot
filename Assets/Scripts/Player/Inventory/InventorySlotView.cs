using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotView : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _amountText;
    [SerializeField] private InventorySlotHighlight _highlight;

    private InventorySlot _slot;

    public void SetSlot(InventorySlot slot)
    {
        _slot = slot;
        Refresh();
    }

    public void Refresh()
    {
        if (_slot.IsEmpty() == true)
        {
            _icon.enabled = false;
            _amountText.enabled = false;
            return;
        }

        _icon.enabled = true;
        _icon.sprite = _slot.Item.Icon;

        if (_slot.Item.IsStackable == true && _slot.Amount > 1)
        {
            _amountText.enabled = true;
            _amountText.text = _slot.Amount.ToString();
        }
        else
        {
            _amountText.enabled = false;
        }
    }

    public void SetActive(bool isActive)
    {
        if (_highlight == null)
        {
            return;
        }

        _highlight.SetActive(isActive);
    }
}
