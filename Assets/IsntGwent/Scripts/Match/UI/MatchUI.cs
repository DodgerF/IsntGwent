using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Match.Client;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Match.UI
{
    public class MatchUI : MonoBehaviour
    {
        public GameObject waitingImage;
        public GameObject ui;

        [Inject] private readonly MatchClientHandler _handler;
        [Inject] private readonly MatchState _matchState;
        [Inject] private readonly CardDatabase _cards;

        private void Start()
        {
            _matchState.IsWaitingImageActive
                .Subscribe(value =>
                {
                    waitingImage.SetActive(value);
                    ui.SetActive(!value);
                })
                .AddTo(this);

            // «Готов» уходит только с загруженной базой карт: по нему сервер начинает партию или шлёт
            // снапшот, и каждая карта в них разбирается по id. В вебе база едет из StreamingAssets
            // по файлу на карту, с CDN itch это дольше, чем загрузка сцены: без ожидания клиент ловил
            // KeyNotFoundException на стартовой раздаче, оставался с пустой рукой и нулевыми колодами
            // и чинился только реконнектом.
            _cards.OnLoaded
                .Where(loaded => loaded)
                .Take(1)
                .Subscribe(_ => _handler.SendReadyMessage())
                .AddTo(this);
        }
    }
}
