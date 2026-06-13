using IsntGwent.Scripts.Cards.UI;
using TMPro;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Match
{
    public class MatchUI : MonoBehaviour
    {
        public GameObject waitingImage;
        
        public TextMeshProUGUI enemyCardCounter;
        public TextMeshProUGUI ownCardCounter;

        public RowView hand;
        
        [Inject] private readonly MatchViewModel _vm;

        private void Start()
        {
            _vm.IsWaitingImageActive
                .Subscribe(value => waitingImage.SetActive(value))
                .AddTo(this);
            
            _vm.Ready();
        }
    }
}