using Cysharp.Threading.Tasks;
using R3;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


namespace Base
{
    public class TitleManager : MonoBehaviour
    {
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private string _loadGameSceneName = "Game";

        private void Awake()
        {
            _startButton.OnClickAsObservable().Subscribe(_ =>
            {
                StartGame().Forget();
            }).AddTo(this);
            _quitButton.OnClickAsObservable().Subscribe(_ =>
            {
                QuitGame();
            }).AddTo(this);
        }

        private async UniTask StartGame()
        {
            await Entry();
            if (this != null)
            {
                SceneManager.LoadScene(_loadGameSceneName);
            }
        }
        private UniTask Entry()
        {
            return UniTask.CompletedTask;
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        }
    }
}