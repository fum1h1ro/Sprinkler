using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class TextSetter : MonoBehaviour
{
    public Sprinkler.Components.TMProPlus Plus;
    public Sprinkler.Components.TMProPlayer Player;
    public string Text;
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(() =>
        {
            if (Player != null)
            {
                Player.SetText(Text, true);
            }
            else if (Plus != null)
            {
                Plus.SetText(Text);
            }
        });
    }
}
