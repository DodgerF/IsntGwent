using UnityEngine.SceneManagement;

namespace IsntGwent.Scripts.Core
{
    public class SceneService
    {
        public void LoadGame()
        {
            SceneManager.LoadScene("GameScene");
        }

        public void LoadMenu()
        {
            SceneManager.LoadScene("Menu");
        }
    }
}