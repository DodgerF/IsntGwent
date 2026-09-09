using TMPro;
using UnityEngine;

namespace IsntGwent.Scripts.UI
{
    [RequireComponent(typeof(TMP_InputField))]
    public class InputFieldEllipsis : MonoBehaviour
    {
        private TMP_InputField _input;

        private void Awake()
        {
            _input = GetComponent<TMP_InputField>();
        }

        private void OnEnable()
        {
            _input.onSelect.AddListener(OnSelect);
            _input.onDeselect.AddListener(OnDeselect);
            _input.onSubmit.AddListener(OnDeselect);
            Apply(_input.isFocused);
        }

        private void OnDisable()
        {
            _input.onSelect.RemoveListener(OnSelect);
            _input.onDeselect.RemoveListener(OnDeselect);
            _input.onSubmit.RemoveListener(OnDeselect);
        }

        private void OnSelect(string _) => Apply(true);

        private void OnDeselect(string _) => Apply(false);

        private void Apply(bool editing)
        {
            var text = _input.textComponent;
            if (text == null) return;

            text.overflowMode = editing ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;

            if (!editing)
                text.rectTransform.anchoredPosition = Vector2.zero;
        }
    }
}
