using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sprinkler.Components;

public class Example : MonoBehaviour
{
    private TMProPlayer _player;

    private void Start()
    {
        _player = GetComponent<TMProPlayer>();
        _player.SetText("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", true);
    }
}
