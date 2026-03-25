using UnityEngine;

namespace VoidWireInteractive.Messaging.Samples
{
    public class DeleteTargetOnReached : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.transform.name.Contains("RequestReply_Customer"))
                Destroy(other.transform.gameObject);
        }
    }
}
