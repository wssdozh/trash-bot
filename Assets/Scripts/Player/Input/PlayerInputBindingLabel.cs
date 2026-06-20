using System;
using UnityEngine.InputSystem;

public static class PlayerInputBindingLabel
{
    private const string KeyboardMouseGroupName = "Keyboard&Mouse";
    private const string LeftMousePath = "<Mouse>/leftButton";
    private const string RightMousePath = "<Mouse>/rightButton";
    private const string MiddleMousePath = "<Mouse>/middleButton";
    private const string LeftMouseLabel = "\u041b\u041a\u041c";
    private const string RightMouseLabel = "\u041f\u041a\u041c";
    private const string MiddleMouseLabel = "\u0421\u041a\u041c";

    public static string Get(PlayerInputActions inputs, string actionName, string bindingPartName)
    {
        if (inputs == null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        InputAction action = inputs.asset.FindAction(actionName, true);

        if (action == null)
        {
            throw new InvalidOperationException(actionName);
        }

        int bindingIndex = GetBindingIndex(action, bindingPartName);
        string bindingPath = action.bindings[bindingIndex].effectivePath;

        return GetHumanBindingLabel(bindingPath);
    }

    private static int GetBindingIndex(InputAction action, string bindingPartName)
    {
        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];

            if (IsTargetBinding(binding, bindingPartName))
            {
                return i;
            }
        }

        throw new InvalidOperationException(action.name);
    }

    private static bool IsTargetBinding(InputBinding binding, string bindingPartName)
    {
        if (string.IsNullOrEmpty(binding.groups))
        {
            return false;
        }

        if (binding.groups.Contains(KeyboardMouseGroupName) == false)
        {
            return false;
        }

        if (string.IsNullOrEmpty(bindingPartName))
        {
            return binding.isComposite == false && binding.isPartOfComposite == false;
        }

        if (binding.isPartOfComposite == false)
        {
            return false;
        }

        return binding.name == bindingPartName;
    }

    private static string GetHumanBindingLabel(string bindingPath)
    {
        if (bindingPath == LeftMousePath)
        {
            return LeftMouseLabel;
        }

        if (bindingPath == RightMousePath)
        {
            return RightMouseLabel;
        }

        if (bindingPath == MiddleMousePath)
        {
            return MiddleMouseLabel;
        }

        return InputControlPath.ToHumanReadableString(
            bindingPath,
            InputControlPath.HumanReadableStringOptions.OmitDevice);
    }
}
