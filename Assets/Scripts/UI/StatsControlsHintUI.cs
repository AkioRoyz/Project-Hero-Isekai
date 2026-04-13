using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class StatsControlsHintUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameInput gameInput;

    [Header("Key Texts")]
    [SerializeField] private TextMeshProUGUI selectKeyText;
    [SerializeField] private TextMeshProUGUI unequipKeyText;

    private void Start()
    {
        Refresh();
    }

    private void OnEnable()
    {
        if (gameInput != null)
        {
            gameInput.OnActiveDeviceGroupChanged += Refresh;
        }
    }

    private void OnDisable()
    {
        if (gameInput != null)
        {
            gameInput.OnActiveDeviceGroupChanged -= Refresh;
        }
    }

    public void Refresh()
    {
        if (gameInput == null)
            return;

        string currentGroup = gameInput.GetCurrentBindingGroupName();
        string currentLayout = gameInput.GetCurrentDeviceLayoutName();

        InputAction selectAction = gameInput.GetMenuAction_Select();
        InputAction unequipAction = gameInput.GetMenuAction_Unequip();

        if (selectKeyText != null)
        {
            selectKeyText.text = GetBestDisplayString(selectAction, currentGroup, currentLayout);
        }

        if (unequipKeyText != null)
        {
            unequipKeyText.text = GetBestDisplayString(unequipAction, currentGroup, currentLayout);
        }
    }

    private string GetBestDisplayString(InputAction action, string groupName, string currentDeviceLayout)
    {
        if (action == null || string.IsNullOrEmpty(groupName))
            return "?";

        int bestIndex = FindBestBindingIndex(action, groupName, currentDeviceLayout);

        if (bestIndex < 0)
        {
            Debug.LogWarning($"No suitable binding found for action '{action.name}' group '{groupName}' layout '{currentDeviceLayout}'.");
            return "?";
        }

        InputBinding binding = action.bindings[bestIndex];
        string bindingPath = string.IsNullOrEmpty(binding.effectivePath) ? binding.path : binding.effectivePath;

        // Для клавиатуры показываем английскую физическую клавишу
        if (groupName == "KeyboardMouse")
        {
            string keyboardDisplay = GetKeyboardPhysicalKeyDisplay(bindingPath);
            if (!string.IsNullOrEmpty(keyboardDisplay))
            {
                return keyboardDisplay;
            }
        }

        // Для геймпада и всего остального используем стандартный display string
        string displayString = action.GetBindingDisplayString(bestIndex);

        if (string.IsNullOrEmpty(displayString))
            return "?";

        return displayString;
    }

    private int FindBestBindingIndex(InputAction action, string groupName, string currentDeviceLayout)
    {
        int fallbackGroupIndex = -1;

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];

            if (binding.isComposite || binding.isPartOfComposite)
                continue;

            bool groupMatches = !string.IsNullOrEmpty(binding.groups) && binding.groups.Contains(groupName);
            if (!groupMatches)
                continue;

            if (fallbackGroupIndex < 0)
            {
                fallbackGroupIndex = i;
            }

            string bindingPath = string.IsNullOrEmpty(binding.effectivePath) ? binding.path : binding.effectivePath;
            if (string.IsNullOrEmpty(bindingPath))
                continue;

            string bindingDeviceLayout = InputControlPath.TryGetDeviceLayout(bindingPath);

            if (string.IsNullOrEmpty(bindingDeviceLayout))
                continue;

            if (groupName == "KeyboardMouse")
            {
                if (bindingDeviceLayout == "Keyboard" || bindingDeviceLayout == "Mouse")
                {
                    return i;
                }
            }
            else if (groupName == "Gamepad")
            {
                if (bindingDeviceLayout == currentDeviceLayout)
                {
                    return i;
                }

                if (InputSystem.IsFirstLayoutBasedOnSecond(currentDeviceLayout, bindingDeviceLayout))
                {
                    return i;
                }

                if (InputSystem.IsFirstLayoutBasedOnSecond(bindingDeviceLayout, currentDeviceLayout))
                {
                    return i;
                }
            }
        }

        return fallbackGroupIndex;
    }

    private string GetKeyboardPhysicalKeyDisplay(string bindingPath)
    {
        if (string.IsNullOrEmpty(bindingPath))
            return null;

        // Примеры:
        // <Keyboard>/q
        // <Keyboard>/e
        // <Keyboard>/digit1
        // <Keyboard>/leftShift

        const string keyboardPrefix = "<Keyboard>/";

        if (!bindingPath.StartsWith(keyboardPrefix))
            return null;

        string controlName = bindingPath.Substring(keyboardPrefix.Length);

        return controlName switch
        {
            "q" => "Q",
            "e" => "E",
            "r" => "R",
            "f" => "F",
            "digit1" => "1",
            "digit2" => "2",
            "digit3" => "3",
            "digit4" => "4",
            "digit5" => "5",
            "space" => "Space",
            "escape" => "Esc",
            "enter" => "Enter",
            "leftShift" => "LShift",
            "rightShift" => "RShift",
            "leftCtrl" => "LCtrl",
            "rightCtrl" => "RCtrl",
            "leftAlt" => "LAlt",
            "rightAlt" => "RAlt",
            "upArrow" => "↑",
            "downArrow" => "↓",
            "leftArrow" => "←",
            "rightArrow" => "→",
            _ => controlName.ToUpperInvariant()
        };
    }
}