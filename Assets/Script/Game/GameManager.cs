using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private string loadStartSceneName = "FirstStep";

    private async void Awake()
    {
        SceneManager.LoadScene(loadStartSceneName, LoadSceneMode.Additive);
        await Entry();
    }

    private UniTask Entry()
    {
        return UniTask.CompletedTask;
    }
}
