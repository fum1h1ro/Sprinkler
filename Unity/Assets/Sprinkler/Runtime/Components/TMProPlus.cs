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
        //public string TaggedText;
        public bool Rubyable = true;
        public int AdjustFontSizeForLine = 0;

        private RectTransform _transform;
        private TMP_Text _text;
        private TMP_TextInfo _info;
        private TextProcessor _proc;
        private bool _rubyable;
        private int _adjustFontSizeForLine;
        private Dictionary<TextEffects.TypeFlag, (EffectorBase Effector, EffectorWork Work)> _effectors;
        private int _currentPageIndex;
        private ExpandableArray<CharAttribute>.Span _currentAttributes;
        private ExpandableArray<TextProcessor.Command>.Span _currentCommands;

        private class EffectorWork
        {
            public int Index;
        }

        public static TMProPlus GetOrAdd(GameObject g)
        {
            var c = g.GetComponent<TMProPlus>();
            if (c == null) c = g.AddComponent<TMProPlus>();
            return c;
        }

        public TMP_Text Text => _text;
        public TMP_TextInfo Info => _info;
        public ExpandableArray<TextProcessor.Command>.Span Commands => _currentCommands;
        public int PageCount => _proc.PageCount;

        private void OnEnable()
        {
            if (_effectors == null)
            {
                _effectors = new Dictionary<TextEffects.TypeFlag, (EffectorBase, EffectorWork)>();
                _effectors[TextEffects.TypeFlag.Quake] = (new Quaker(), new EffectorWork());
                _effectors[TextEffects.TypeFlag.Shout] = (new Shouter(), new EffectorWork());
                _effectors[TextEffects.TypeFlag.Fade] = (new Fader(), new EffectorWork());
            }

            _transform = _transform ?? GetComponent<RectTransform>();
            _text = _text ?? (TMP_Text)GetComponent<TextMeshPro>() ?? (TMP_Text)GetComponent<TextMeshProUGUI>();
            _text.richText = true;
            _info = _info ?? _text.textInfo;
            _proc = _proc ?? new TextProcessor(_text);
        }

        private void Update()
        {
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

        public int SetText(string text)
        {
            _proc.SetText(text);
            SetPageText(0);
            return _proc.PageCount;
        }

        public void SetPageText(int page=-1)
        {
            var bak = _text.enabled;
            if (bak) _text.enabled = false;
            if (page < 0)
            {
                _text.SetCharArray(_proc.ToArray(), 0, _proc.Length);
                _currentAttributes = _proc.Attributes.Slice(0, _proc.Attributes.Length);
                _currentCommands = _proc.Commands.Slice(0, _proc.Commands.Length);
            }
            else
            {
                var span = _proc.GetPageSpan(page);
                _text.SetCharArray(_proc.ToArray(), span.Start, span.Length);
                _currentAttributes = _proc.Attributes.Slice(span.AttrStart, span.AttrLength);
                _currentCommands = _proc.Commands.Slice(span.CommandStart, span.CommandLength);
            }

            var currentFlag = (TextEffects.TypeFlag)0;
            for (var i = 0; i < _currentAttributes.Length; ++i)
            {
                var charAnimFlag = _currentAttributes[i].AnimType;
                foreach (var effectorFlag in _effectors.Keys)
                {
                    var e = _effectors[effectorFlag];
                    // first
                    if ((charAnimFlag & effectorFlag) != 0 && (effectorFlag & currentFlag) == 0)
                    {
                        e.Work.Index = 0;
                        currentFlag |= charAnimFlag;
                    }
                    else if ((charAnimFlag & effectorFlag) == 0)
                    {
                        currentFlag &= ~charAnimFlag;
                    }
                    e.Effector.Setup(_currentAttributes, i, e.Work.Index++);
                }
            }
            _text.enabled = bak;
            _text.ForceMeshUpdate();
            _currentPageIndex = page;

            Assert.AreEqual(_info.characterCount, _currentAttributes.Length);
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
            var dt = Time.deltaTime;
            for (int i = 0; i < _info.characterCount; ++i)
            {
                var charInfo = _info.characterInfo[i];
                if (!charInfo.isVisible) continue;

                var p = _currentAttributes[i];
                p.Time += dt;
                _currentAttributes[i] = p;

                foreach (var effectorFlag in _effectors.Keys)
                {
                    var charAnimFlag = _currentAttributes[i].AnimType;
                    if ((charAnimFlag & effectorFlag) != 0)
                    {
                        var e = _effectors[effectorFlag];
                        e.Effector.Update(_currentAttributes, i);
                    }
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

                var meshInfo = _info.meshInfo[materialIndex];
                var vertices = meshInfo.vertices;
                var colors = meshInfo.colors32;

                foreach (var effectorFlag in _effectors.Keys)
                {
                    var charAnimFlag = _currentAttributes[i].AnimType;
                    if ((charAnimFlag & effectorFlag) != 0)
                    {
                        var e = _effectors[effectorFlag];
                        if (e.Effector is IVertexModifier v)
                        {
                            v.Modify(_currentAttributes[i], charInfo, vertices, vertexIndex);
                        }
                        if (e.Effector is IColorModifier c)
                        {
                            c.Modify(_currentAttributes[i], charInfo, colors, vertexIndex);
                        }
                    }
                }
            }

            for (int i = 0; i < _info.meshInfo.Length; ++i)
            {
                var meshInfo = _info.meshInfo[i];
                meshInfo.mesh.vertices = meshInfo.vertices;
                meshInfo.mesh.colors32 = meshInfo.colors32;
                _text.UpdateGeometry(meshInfo.mesh, i);
            }
        }
    }
}
