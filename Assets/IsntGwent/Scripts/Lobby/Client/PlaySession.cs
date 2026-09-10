using UniRx;
using UnityEngine;

namespace IsntGwent.Scripts.Lobby.Client
{
    public enum PlayPhase
    {
        None,
        Searching,
        WaitingForFriend,
        Starting
    }

    public class PlaySession
    {
        private const string CodeKey = "lobby.code";

        public readonly ReactiveProperty<string> JoinCode = new(PlayerPrefs.GetString(CodeKey, string.Empty));
        public readonly ReactiveProperty<PlayPhase> Phase = new(PlayPhase.None);

        public bool HasCode => !string.IsNullOrEmpty(JoinCode.Value);

        public void BeginSearch()
        {
            SetCode(string.Empty);
            Phase.Value = PlayPhase.Searching;
        }

        public void BeginRoom(string code)
        {
            SetCode(code);
            Phase.Value = PlayPhase.WaitingForFriend;
        }

        public void BeginMatch()
        {
            Phase.Value = PlayPhase.Starting;
        }

        public void Clear()
        {
            SetCode(string.Empty);
            Phase.Value = PlayPhase.None;
        }

        private void SetCode(string code)
        {
            JoinCode.Value = code ?? string.Empty;

            if (string.IsNullOrEmpty(JoinCode.Value))
                PlayerPrefs.DeleteKey(CodeKey);
            else
                PlayerPrefs.SetString(CodeKey, JoinCode.Value);

            PlayerPrefs.Save();
        }
    }
}
