using IsntGwent.Scripts.Diagnostics;
using IsntGwent.Scripts.Tutorial.Client;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace IsntGwent.Scripts.Tutorial.UI
{
    public class TutorialDebugHud : MonoBehaviour
    {
        private const string GameSceneName = "GameScene";
        private const float Width = 230f;
        private const float Margin = 10f;

        [InjectOptional] private readonly TutorialService _tutorial;

        private bool _expanded;

        private void OnGUI()
        {
            if (_tutorial == null || AppRole.IsServer) return;

            var area = new Rect(Margin, Margin, Width, Screen.height - Margin * 2f);

            GUILayout.BeginArea(area);

            if (GUILayout.Button(_expanded ? "TUTORIAL ▲" : "TUTORIAL ▼"))
                _expanded = !_expanded;

            if (_expanded)
                DrawBody();

            GUILayout.EndArea();
        }

        private void DrawBody()
        {
            GUILayout.Label("State: " + _tutorial.State.Value);

            if (SceneManager.GetActiveScene().name == GameSceneName)
                GUILayout.Label("Start it from the menu");
            else if (GUILayout.Button("Start tutorial"))
                RestartTutorial();

            if (GUILayout.Button("Menu steps"))
                _tutorial.BeginMenuPhase();

            if (GUILayout.Button("Mark done"))
                _tutorial.MarkDone();
        }

        private void RestartTutorial()
        {
            Log.Info(LogTag.Tutorial, "TutorialDebugHud: сбрасываем прогресс");

            _tutorial.Restart();
        }
    }
}
