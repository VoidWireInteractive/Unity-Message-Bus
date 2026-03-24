using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VoidWireInteractive.Messaging.Samples
{
    public class LoadSceneOnInit : MonoBehaviour
    {
        [SerializeField]
        string[] AdditiveScenesToLoad;

        private IEnumerator Start()
        {
            foreach (var scene in AdditiveScenesToLoad)
            {
                if (!SceneManager.GetSceneByName(scene).isLoaded)
                    SceneManager.LoadScene(scene, LoadSceneMode.Additive);
                yield return null;
            }
        }
    }
}
