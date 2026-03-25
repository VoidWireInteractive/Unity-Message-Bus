using System.Collections.Generic;
using UnityEngine;

namespace VoidWireInteractive.Messaging.Samples
{
    public class SpawnFollowers : MonoBehaviour
    {
        public int spawnCount = 10;
        public float radius = 5f;
        public GameObject prefab;
        List<GameObject> followers = new List<GameObject>();
        public bool randomizePosition = true;
        private async void OnEnable()
        {
            await foreach (GameObject spawn in DoSpawns())
            {
                Debug.Log($"Spawned follower at {spawn.transform.position}");
                followers.Add(spawn);
                if (randomizePosition)
                {
                    Vector2 spos = Random.insideUnitCircle * radius / 2;
                    spawn.transform.position = new Vector3(spos.x, this.transform.position.y, spos.y);
                }
            }
        }
        private void OnDisable()
        {
            foreach (var f in followers)
            {
                Destroy(f);
            }
            followers.Clear();
        }
        private async IAsyncEnumerable<object> DoSpawns()
        {
            for (int i = 0; i < spawnCount; i++)
            {
                yield return Instantiate(prefab, this.transform);
            }
        }
    }
}
