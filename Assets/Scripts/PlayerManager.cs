using UnityEngine;
using Photon.Pun;

public class PlayerManager : MonoBehaviourPunCallbacks, IPunObservable
{
    private Vector3 realPosition = Vector3.zero;
    private Quaternion realRotation = Quaternion.identity;

    void Start()
    {
        if (photonView.IsMine)
        {
            // To jest nasz lokalny gracz, możesz dodać tutaj kod do sterowania
            GetComponent<MeshRenderer>().material.color = Color.blue; // Przykład: zmiana koloru lokalnego gracza
        }
        else
        {
            // To jest zdalny gracz
            GetComponent<MeshRenderer>().material.color = Color.red; // Przykład: zmiana koloru zdalnego gracza
        }
    }

    void Update()
    {
        if (!photonView.IsMine)
        {
            // Interpolacja pozycji i rotacji zdalnych graczy
            transform.position = Vector3.Lerp(transform.position, realPosition, Time.deltaTime * 10);
            transform.rotation = Quaternion.Lerp(transform.rotation, realRotation, Time.deltaTime * 10);
        }
        else
        {
            // Sterowanie lokalnym graczem (przykład)
            float move = Input.GetAxis("Vertical") * Time.deltaTime * 5.0f;
            float strafe = Input.GetAxis("Horizontal") * Time.deltaTime * 5.0f;
            transform.Translate(strafe, 0, move);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Dane, które wysyłamy do innych graczy
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            // Dane, które odbieramy od innych graczy
            realPosition = (Vector3)stream.ReceiveNext();
            realRotation = (Quaternion)stream.ReceiveNext();
        }
    }
}
