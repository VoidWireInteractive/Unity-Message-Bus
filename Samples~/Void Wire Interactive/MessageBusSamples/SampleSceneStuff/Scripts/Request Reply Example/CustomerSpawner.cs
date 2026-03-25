using System.Collections;
using UnityEngine;

namespace VoidWireInteractive.Messaging.Samples
{
    public class CustomerSpawner : MonoBehaviour
    {
        public float spawnDelay= 1f;
        public GameObject prefab;
        private WaitForSeconds wfs;

        private void Start()
        {
            wfs = new WaitForSeconds(spawnDelay);
            StartCoroutine(SpawnOnDelay());
        }

        IEnumerator SpawnOnDelay()
        {
            while (true)
            {
                yield return wfs;
                Vector2 spos = UnityEngine.Random.insideUnitCircle * 2.5f;
                var go = Instantiate(prefab, new Vector3(spos.x, this.transform.position.y, spos.y),Quaternion.identity);
                go.transform.SetParent(this.transform);
            }
        }
    }
}
