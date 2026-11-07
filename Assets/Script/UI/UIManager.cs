using UnityEngine;


namespace Base
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField, Tooltip("IUserInterface を実装したコンポーネントを登録します。配列の順に Start を呼び出します。")]
        private MonoBehaviour[] _userInterfaces = new MonoBehaviour[0];

        private void Start()
        {
            foreach (var behaviour in _userInterfaces)
            {
                if (behaviour != null &&
                    behaviour.isActiveAndEnabled &&
                    behaviour is IUserInterface userInterface)
                {
                    userInterface.Start();
                }
            }
        }

        private void Update()
        {
            foreach (var behaviour in _userInterfaces)
            {
                if (behaviour != null &&
                    behaviour.isActiveAndEnabled &&
                    behaviour is IUserInterface userInterface)
                {
                    userInterface.Update();
                }
            }
        }
    }
}