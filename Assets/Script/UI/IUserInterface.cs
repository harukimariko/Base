// MonoBehaviour では void IUserInterface.Start() / Update() と明示的に実装し、
// Unity の自動呼び出しと二重に実行されることを避けます。
public interface IUserInterface
{
    void Start();
    void Update();
}
