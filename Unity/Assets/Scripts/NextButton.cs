using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class NextButton : MonoBehaviour
{
    public Sprinkler.Components.TMProPlayer Player;
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(() =>
        {
            Player.NextPage();
        });
    }

    private void Update()
    {
        _button.interactable = Player.IsWaiting;
    }
}
