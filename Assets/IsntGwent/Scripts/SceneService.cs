using UnityEngine.SceneManagement;

namespace IsntGwent.Scripts
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