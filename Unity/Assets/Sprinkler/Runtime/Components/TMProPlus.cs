using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TMPro;
using Sprinkler.TextEffects;

namespace Sprinkler.Components
{
    [RequireComponent(typeof(TMP_Text))]
    //[ExecuteInEditMode]
    public class TMProPlus : MonoBehaviour
    {
        public string TaggedText;
        public bool Rubyable = true;
        public int AdjustFontSizeForLine = 0;

        private RectTransform _transform;
        private TMP_Text _text;
        private TMP_TextInfo _info;
        private TextProcessor _proc;
        private string _prevText;
        private bool _rubyable;
        private int _adjustFontSizeForLine;
        private Quaker _quaker = new Quaker();
        private Shouter _shouter = new Shouter();

        public static TMProPlus GetOrAdd(GameObject g)
        {
            var c = g.GetComponent<TMProPlus>();
            if (c == null) c = g.AddComponent<TMProPlus>();
            return c;
        }

        public TMP_Text Text => _text;
        public TMP_TextInfo Info => _info;
        public List<TextProcessor.Command> Commands => _proc.Commands;
        //public ExpandableArray<TextProcessor.CharAttribute> Attributes => _proc.Attributes;

        private void OnEnable()
        {
            _transform = _transform ?? GetComponent<RectTransform>();
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

            if (_rubyable != Rubyable || _adjustFontSizeForLine != AdjustFontSizeForLine)
            {
                _rubyable = Rubyable;
                _adjustFontSizeForLine = AdjustFontSizeForLine;
                if (AdjustFontSizeForLine > 0) AdjustFontSize();
            }
            if (_info.characterCount > 0)
            {
                CharUpdate();
                MeshUpdate();
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
            try
            {
                _proc.SetText(text);

                int idx = 0;
                var prevAnimType = TextEffects.Type.Normal;
                for (var i = 0; i < _proc.Attributes.Length; ++i)
                {
                    var animType = _proc.Attributes[i].AnimType;
                    if (animType != prevAnimType)
                    {
                        idx = 0;
                        prevAnimType = animType;
                    }
                    switch (animType)
                    {
                    case TextEffects.Type.Quake:
                            _quaker.Setup(_proc.Attributes, idx++);
                        break;
                    case TextEffects.Type.Shout:
                            _shouter.Setup(_proc.Attributes, idx++);
                        break;
                    default:
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"{e}");
            }
            var bak = _text.enabled;
            if (bak) _text.enabled = false;
            _text.SetCharArray(_proc.ToArray(), 0, _proc.Length);
            _text.enabled = bak;
            _text.ForceMeshUpdate();
            _prevText = text;
        }

        private void AdjustFontSize()
        {
            var font = _text.font;
            var faceInfo = font.faceInfo;
            var rc = _transform.rect;
            var lineHeightScale = (float)faceInfo.pointSize / faceInfo.lineHeight;
            var lines = (Rubyable)? (float)AdjustFontSizeForLine * 1.5f : AdjustFontSizeForLine;
            _text.fontSize = (rc.height / lines) * lineHeightScale;
        }

        private void CharUpdate()
        {
            var sz = Mathf.Min(_text.maxVisibleCharacters, _info.characterCount);
            for (int i = 0; i < sz; ++i)
            {
                var charInfo = _info.characterInfo[i];
                if (!charInfo.isVisible) continue;

                switch (_proc.Attributes[i].AnimType)
                {
                case TextEffects.Type.Quake: _quaker.Update(_proc.Attributes, i); break;
                case TextEffects.Type.Shout: _shouter.Update(_proc.Attributes, i); break;
                default: break;
                }
            }
        }

        private void MeshUpdate()
        {
            _text.ForceMeshUpdate();

            for (int i = 0; i < _info.characterCount; ++i)
            {
                var charInfo = _info.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int materialIndex = charInfo.materialReferenceIndex;
                int vertexIndex = charInfo.vertexIndex;

                var vertices = _info.meshInfo[materialIndex].vertices;

                switch (_proc.Attributes[i].AnimType)
                {
                case TextEffects.Type.Quake: _quaker.Modify(_proc.Attributes, i, charInfo, vertices, vertexIndex); break;
                case TextEffects.Type.Shout: _shouter.Modify(_proc.Attributes, i, charInfo, vertices, vertexIndex); break;
                default: break;
                }
            }

            for (int i = 0; i < _info.meshInfo.Length; ++i)
            {
                var meshInfo = _info.meshInfo[i];
                meshInfo.mesh.vertices = meshInfo.vertices;
                _text.UpdateGeometry(meshInfo.mesh, i);
            }
        }
    }
}
