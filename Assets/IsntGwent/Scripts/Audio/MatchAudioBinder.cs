using System;
using System.Linq;
using IsntGwent.Scripts.Cards.Client;
using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Match.Client;
using IsntGwent.Scripts.Messages;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Audio
{
    public class MatchAudioBinder : IInitializable, IDisposable
    {
        private readonly MatchState _state;
        private readonly MatchClientHandler _handler;
        private readonly InputRouter _input;
        private readonly CardSelectionService _selection;
        private readonly AudioService _audio;
        private readonly CompositeDisposable _disposables = new();

        public MatchAudioBinder(MatchState state, MatchClientHandler handler, InputRouter input,
            CardSelectionService selection, AudioService audio)
        {
            _state = state;
            _handler = handler;
            _input = input;
            _selection = selection;
            _audio = audio;
        }

        public void Initialize()
        {
            _state.CardStaged
                .Subscribe(card =>
                {
                    var id = card?.Definition?.SoundId;
                    _audio.Play(string.IsNullOrEmpty(id) ? "card_play" : id);
                })
                .AddTo(_disposables);

            _state.IsWaitingImageActive
                .Where(waiting => !waiting)
                .Take(1)
                .Where(_ => !_state.IsConnectionLost.Value)
                .Subscribe(_ =>
                {
                    _audio.Play("sting_match_start");
                    _audio.PlayMusic("music_match");
                })
                .AddTo(_disposables);

            _state.CardDrawn
                .Subscribe(_ => _audio.Play("card_draw"))
                .AddTo(_disposables);

            _state.CardRedrawn
                .Subscribe(_ => _audio.Play("card_draw"))
                .AddTo(_disposables);

            _state.PendingPlayGranted
                .Subscribe(_ => _audio.Play("card_draw"))
                .AddTo(_disposables);

            _state.DamageDealt
                .Subscribe(PlayHitSounds)
                .AddTo(_disposables);

            _state.UnitsLinked
                .Subscribe(PlayLinkSounds)
                .AddTo(_disposables);

            _state.UnitsDied
                .Subscribe(count => _audio.PlayStack("unit_death", count))
                .AddTo(_disposables);

            _state.GraveyardPurged
                .Where(count => count > 0)
                .Subscribe(_ => _audio.Play("unit_devour"))
                .AddTo(_disposables);

            _state.TurnChanged
                .Where(_ => _state.IsMyTurn.Value && !_state.IsGameEnded.Value)
                .Subscribe(_ => _audio.Play("turn"))
                .AddTo(_disposables);

            _state.PassSent
                .Merge(_handler.OnEnemyPassed.AsUnitObservable())
                .Subscribe(_ => _audio.Play("pass"))
                .AddTo(_disposables);

            _state.HpLost
                .Subscribe(count => _audio.PlayStack("hp_loss", count))
                .AddTo(_disposables);

            _state.LastRoundResult.Skip(1)
                .Where(result => result != RoundResult.None)
                .Subscribe(_ => _audio.Play("round_end"))
                .AddTo(_disposables);

            _state.IsGameEnded
                .Where(ended => ended)
                .Take(1)
                .Subscribe(_ =>
                {
                    _audio.StopMusic();

                    if (_state.IsTie.Value) _audio.Play("tie");
                    else _audio.Play(_state.AmIWinner.Value ? "win" : "lose");
                })
                .AddTo(_disposables);

            _input.CardPressed.Subscribe(_ => _audio.Play("ui_click")).AddTo(_disposables);
            _input.BoardCardPressed.Subscribe(_ => _audio.Play("ui_click")).AddTo(_disposables);
            _input.CardHovered.Subscribe(_ => _audio.Play("ui_hover")).AddTo(_disposables);

            _selection.PlacementDenied.Subscribe(_ => _audio.Play("ui_denied")).AddTo(_disposables);
            _selection.DragBegan.Subscribe(_ => _audio.Play("card_move")).AddTo(_disposables);
            _selection.SlotHovered.Subscribe(_ => _audio.Play("ui_hover")).AddTo(_disposables);
            _selection.PlacementStaged.Subscribe(_ => _audio.Play("ui_click")).AddTo(_disposables);
            _selection.PlacementCancelled.Subscribe(_ => _audio.Play("ui_denied")).AddTo(_disposables);
        }

        private void PlayHitSounds(DamageInstance[] hits)
        {
            if (hits == null) return;

            var weather = hits.Count(hit => hit.Kind == DamageKind.Weather);

            if (weather > 0) _audio.PlayStack("weather_hit", weather);
            if (hits.Length > weather) _audio.PlayStack("hit", hits.Length - weather);
        }

        private void PlayLinkSounds(UnitLinkData[] links)
        {
            if (links == null) return;

            var devoured = links.Count(link => link.Kind == UnitLinkKind.Devour);
            if (devoured > 0)
                _audio.PlayStack("unit_devour", devoured);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
