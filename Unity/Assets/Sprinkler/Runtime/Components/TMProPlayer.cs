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
            Empty, // なんもない
            Playing, // 再生中
            Paused, // 再生停止中
            Waiting, // 待機中
            Finished, // 再生が終わった
        }

        public float Wait = 0.025f;

        private TMProPlus _plus;
        private State _state;
        private float _defaultWait;
        private float _waitScale;
        private float _wait;
        private float _time;
        private int _cursor;
        private int _pageIndex;

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

        private void Update()
        {
            if (_state == State.Playing) StreamUpdate();
        }

        private void StreamUpdate()
        {
            if (_cursor >= _plus.Commands.Length)
            {
                if ((_pageIndex + 1) < _plus.PageCount)
                {
                    _state = State.Waiting;
                }
                else
                {
                    _state = State.Finished;
                }
                return;
            }

            if (_time < _wait * _waitScale)
            {
                _time += Time.deltaTime;
                return;
            }
            _time = 0.0f;

            var cmd = _plus.Commands[_cursor++];

            switch (cmd.Type)
            {
            case TextProcessor.CommandType.Put:
                _plus.Text.maxVisibleCharacters += cmd.Put.Count;
                _wait = _defaultWait;
                break;
            case TextProcessor.CommandType.Wait:
                _wait = cmd.Wait.Second;
                break;
            case TextProcessor.CommandType.Speed:
                _waitScale = cmd.Speed.Scale;
                break;
            }
        }

        public bool IsStreaming => (IsPlaying || IsPaused) && !IsFinished;
        public bool IsPlaying => _state == State.Playing;
        public bool IsPaused => _state == State.Paused;
        public bool IsWaiting => _state == State.Waiting;
        public bool IsFinished => _state == State.Finished;

        public void SetText(string text, bool autoPlay=false)
        {
            Clear();
            _plus.TaggedText = text;
            _plus.SetText(text, _pageIndex);
            _state = State.Paused;
            if (autoPlay) Play();
        }

        public void Play()
        {
            if (_state == State.Paused) _state = State.Playing;
        }

        public void Pause()
        {
            if (_state == State.Playing) _state = State.Paused;
        }

        public void Clear()
        {
            _cursor = 0;
            _defaultWait = Wait;
            _waitScale = 1.0f;
            _wait = _defaultWait;
            _time = 0.0f;
            _plus.Text.maxVisibleCharacters = 0;
            _state = State.Empty;
            _pageIndex = 0;
        }

        public void SkipAll()
        {
            _plus.Text.maxVisibleCharacters = _plus.Info.characterCount;
            //if (IsPlaying || IsPaused) _state = State.Finished;
            _cursor = _plus.Commands.Length;
        }

        public void NextPage()
        {
            Assert.IsTrue(IsWaiting);

            var next = _pageIndex + 1;
            Clear();
            _pageIndex = next;
            _state = State.Playing;
            _plus.SetPageText(_pageIndex);
        }
    }
}
