using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TMPro;

namespace Sprinkler.Components
{
    [RequireComponent(typeof(TMP_Text))]
    [ExecuteInEditMode]
    public class TMProPlus : MonoBehaviour
    {
        public string TaggedText;

        private TMP_Text _text;
        private TMP_TextInfo _info;
        private TextProcessor _proc;
        private string _prevText;

        public static TMProPlus GetOrAdd(GameObject g)
        {
            var c = g.GetComponent<TMProPlus>();
            if (c == null) c = g.AddComponent<TMProPlus>();
            return c;
        }

        public TMP_Text Text => _text;
        public TMP_TextInfo Info => _info;
        public List<TextProcessor.Command> Commands => _proc.Commands;
        public ExpandableArray<TextProcessor.Attr> Attributes => _proc.Attributes;

        private void OnEnable()
        {
            _text = _text ?? (TMP_Text)GetComponent<TextMeshPro>() ?? (TMP_Text)GetComponent<TextMeshProUGUI>();
            _text.richText = true;
            _info = _info ?? _text.textInfo;
            _proc = _proc ?? new TextProcessor(_text);
        }

        private void Update()
        {
            if (_prevText != TaggedText)
            {
                SetText(TaggedText);
            }
        }

        private void OnDisable()
        {
        }

        private void OnDestroy()
        {
            _proc.Dispose();
        }

        internal void SetText(string text)
        {
            try {
                _proc.SetText(text);
            }
            catch (Exception e)
            {
                Debug.LogError($"{e}");
            }
            _text.SetCharArray(_proc.ToArray(), 0, _proc.Length);
            _text.ForceMeshUpdate();
            _prevText = text;
        }
    }
}
