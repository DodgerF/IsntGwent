using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace IsntGwent.Scripts.Decks.UI
{
    [RequireComponent(typeof(Button))]
    public class NewDeckTileView : MonoBehaviour
    {
        private Button _button;

        public IObservable<Unit> Clicked => Button.OnClickAsObservable();

        private Button Button => _button != null ? _button : _button = GetComponent<Button>();
    }
}
