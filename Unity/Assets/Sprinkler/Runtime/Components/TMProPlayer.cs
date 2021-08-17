using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using Unity.Collections;
using TMPro;

namespace Sprinkler.Components
{
    [RequireComponent(typeof(TMProPlus))]
    public class TMProPlayer : MonoBehaviour
    {
        private enum State
        {
            Empty,
            Set,
            Playing,
            Pause,
            Waiting,
            Finished,
        }

        private TMProPlus _plus;
        private State _state;
        private float _defaultWait;
        private float _wait;
        private int _cursor;

        private Quaker _quaker = new Quaker();

        public static TMProPlayer GetOrAdd(GameObject g)
        {
            var mf = g.GetComponent<TMProPlayer>();
            if (mf == null) mf = g.AddComponent<TMProPlayer>();
            return mf;
        }

        private void Awake()
        {
            _plus = GetComponent<TMProPlus>();
        }

        private void OnEnable()
        {
            //SetText(_plus.TaggedText, true);
        }

        private void Update()
        {
            if (_state == State.Playing) StreamUpdate();
            _quaker.Update();
            if (_plus.Info.characterCount > 0) MeshUpdate();
        }

        private void OnDisable()
        {
        }

        private void OnDestroy()
        {
        }

        private void StreamUpdate()
        {
            if (_cursor >= _plus.Commands.Count)
            {
                _state = State.Waiting;
                return;
            }

            if (_wait > 0.0f)
            {
                _wait = Mathf.Max(0.0f, _wait - Time.deltaTime);
                return;
            }

            var cmd = _plus.Commands[_cursor++];

            switch (cmd.Type)
            {
            case TextProcessor.CommandType.Put:
                _plus.Text.maxVisibleCharacters += cmd.Count;
                _wait = _defaultWait;
                break;
            case TextProcessor.CommandType.Wait:
                _wait = cmd.Wait;
                break;
            }
        }

        private void MeshUpdate()
        {
            _plus.Text.ForceMeshUpdate();
            var attrs = _plus.Attributes;
            //if (!attrs.IsCreated) return;

            for (int i = 0; i < _plus.Info.characterCount; ++i)
            {
                var attr = attrs[i];
                var charInfo = _plus.Info.characterInfo[i];

                if (!charInfo.isVisible) continue;

                int materialIndex = charInfo.materialReferenceIndex;
                int vertexIndex = charInfo.vertexIndex;


                var vertices = _plus.Info.meshInfo[materialIndex].vertices;

                if ((attr.Flag & TextProcessor.AnimFlag.Quake) != 0)
                {
                    _quaker.Modify(i, charInfo, vertices, vertexIndex);
                }
            }

            for (int i = 0; i < _plus.Info.meshInfo.Length; ++i)
            {
                var meshInfo = _plus.Info.meshInfo[i];
                meshInfo.mesh.vertices = meshInfo.vertices;
                _plus.Text.UpdateGeometry(meshInfo.mesh, i);
            }
        }

        public bool IsStreaming => (IsPlaying || IsWaiting) && !IsFinished;
        public bool IsPlaying => _state == State.Set || _state == State.Playing || _state == State.Pause;
        public bool IsWaiting => _state == State.Waiting;
        public bool IsFinished => _state == State.Finished;

        public void SetText(string text, bool autoPlay=false)
        {
            Clear();
            _plus.SetText(text);
            _state = State.Set;
            if (autoPlay) Play();
        }

        public void Play()
        {
            if (_state == State.Set || _state == State.Pause) _state = State.Playing;
        }

        public void Pause()
        {
            if (_state == State.Set || _state == State.Playing) _state = State.Pause;
        }

        public void Clear()
        {
            _cursor = 0;
            _defaultWait = 0.05f;
            _wait = 0.0f;
            _plus.Text.maxVisibleCharacters = 0;
            _state = State.Empty;
        }

        public void SkipAll(bool nowait)
        {
            _plus.Text.maxVisibleCharacters = _plus.Info.characterCount;
            if (nowait)
            {
                _state = State.Finished;
            }
            else
            {
                _state = State.Waiting;
            }
        }

        public void GoAhead()
        {
            if (IsPlaying)
            {
                SkipAll(false);
            }
            else if (_state == State.Waiting)
            {
                _state = State.Finished;
            }
        }

        private interface IVertexModifier
        {
            void Update();
            void Modify(int idx, TMP_CharacterInfo info, Vector3[] vtx, int vtxtop);
        }

        private class Quaker : IVertexModifier
        {
            private float _time;
            private float _speed = 1.0f/0.7f;
            private float _horizontal;
            private float _vertical;
            private int _seed = 200;
            private const int A = 1664525;
            private const int C = 1013904223;
            private const int M = 0x7fffffff;

            public void Update()
            {
                _time += Mathf.PI * 2.0f * Time.deltaTime * _speed;

                if (_time >= Mathf.PI * 2.0f)
                {
                    _time -= Mathf.PI * 2.0f;
                }

                _horizontal = GetRand();
                _vertical = GetRand();
            }

            public void Modify(int idx, TMP_CharacterInfo info, Vector3[] vtx, int vtxtop)
            {
                var o = GetOffset(idx);
                var sz = info.pointSize * 0.15f;
                vtx[vtxtop + 0] += o * sz;
                vtx[vtxtop + 1] += o * sz;
                vtx[vtxtop + 2] += o * sz;
                vtx[vtxtop + 3] += o * sz;
            }


            private Vector3 GetOffset(int idx)
            {
                var w = Mathf.Sin(_time + idx * 1.0f) + 0.5f;

                if (w < 0.0f) return Vector3.zero;

                return new Vector3(w * _horizontal, w * _vertical, 0);
            }

            private float GetRand()
            {
                _seed = (_seed * A + C) & M;
                var r = _seed * (1.0f / (float)M);
                return (r - 0.5f) * 2.0f;
            }
        }
    }



}
