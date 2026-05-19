using UniRx;
using UnityEngine.SceneManagement;
using Zenject;

namespace IsntGwent.Scripts
{
    public class SceneController : IInitializable
    {
        [Inject] private SessionService _service;
        public void Initialize()
        {
            _service.OnJoinedLobby
                .Subscribe(_ =>
                {
                    SceneManager.LoadScene("DeckSelection");
                });
        }
    }
}