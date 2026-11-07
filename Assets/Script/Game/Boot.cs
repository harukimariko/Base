using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace Base
{
    public class Boot : MonoBehaviour
    {
        [SerializeField] private string _loadTitleSceneName = "Title";

        private async void Awake()
        {
            await Booting();

            if (this != null)
            {
                SceneManager.LoadScene(_loadTitleSceneName);
            }
        }

        public UniTask Booting()
        {
            return UniTask.CompletedTask;
        }
    }
}