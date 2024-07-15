using UnityEngine;
using Photon.Pun;
using System.Collections;

public class Bullet : MonoBehaviourPunCallbacks
{
    public GameObject blackDotPrefab; // Prefabrykat czarnej kropki

    void OnCollisionEnter(Collision collision)
    {
        if (photonView.IsMine)
        {
            // Instantiate a black dot at the collision point
            ContactPoint contact = collision.contacts[0];
            Vector3 hitPoint = contact.point;
            Quaternion hitRotation = Quaternion.FromToRotation(Vector3.up, contact.normal);
            GameObject blackDot = PhotonNetwork.Instantiate(blackDotPrefab.name, hitPoint, hitRotation);

            // Destroy the black dot after 3 seconds
            StartCoroutine(DestroyAfterTime(blackDot, 3f));

            // Destroy the bullet
            PhotonNetwork.Destroy(gameObject);
        }
    }

    private IEnumerator DestroyAfterTime(GameObject obj, float time)
    {
        yield return new WaitForSeconds(time);
        PhotonNetwork.Destroy(obj);
    }
}
