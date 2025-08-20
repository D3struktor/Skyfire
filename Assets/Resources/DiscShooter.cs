using UnityEngine;
using Photon.Pun;
using System.Collections;

[RequireComponent(typeof(PhotonView), typeof(AudioSource))]
public class DiscShooter : MonoBehaviourPunCallbacks
{
    [Header("Setup")]
    public GameObject discPrefab;
    public Transform shootingPoint;
    public float discSpeed = 60f;
    public float fireCooldown = 0.7f;
    public float weaponSwitchDelay = 1f;

    [Header("Ballistics")]
    [Range(0f, 1f)] public float inheritVelocityFactor = 0.5f; // część prędkości gracza dodawana do dysku

    [Header("Audio")]
    public AudioClip shootSound;

    [Header("UX")]
    public float discRotationSpeed = 360f; // obrót dekoracyjnego „attachedDisc”
    public GameObject attachedDisc;

    [Header("Safety windows")]
    public float ignoreCollisionTime = 0.25f; // lokalne ignorowanie z ownerem (spójne z Disc)

    [Header("Integration")]
    public bool isActiveWeapon = false;

    private float lastShotTime = 0f;
    private float lastWeaponSwitchTime = 0f;
    private AudioSource audioSource;

    private PlayerAmmoManager playerAmmoManager;
    private AmmoUI ammoUI;

    void Start()
    {
        if (!photonView.IsMine) return;

        audioSource = GetComponent<AudioSource>();

        Transform playerRoot = transform.root;
        playerAmmoManager = playerRoot.GetComponent<PlayerAmmoManager>();
        if (!playerAmmoManager) Debug.LogWarning("DiscShooter: brak PlayerAmmoManager na obiekcie gracza.");

        ammoUI = FindObjectOfType<AmmoUI>();
        if (!ammoUI) Debug.LogWarning("DiscShooter: brak AmmoUI w scenie.");

        if (!attachedDisc)
            attachedDisc = FindDiscInChildren(transform);

        UpdateAmmoUI();
    }

    private GameObject FindDiscInChildren(Transform parent)
    {
        foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
            if (t.name == "Disc") return t.gameObject;
        return null;
    }

    void Update()
    {
        if (!photonView.IsMine) return;
        if (!isActiveWeapon || Time.time < lastWeaponSwitchTime + weaponSwitchDelay) return;

        if (attachedDisc) attachedDisc.transform.Rotate(Vector3.forward * discRotationSpeed * Time.deltaTime);

        if (Input.GetButtonDown("Fire1") && Time.time >= lastShotTime + fireCooldown)
        {
            if (playerAmmoManager == null || playerAmmoManager.UseAmmo("DiscShooter"))
            {
                ShootDisc();
                lastShotTime = Time.time;
                UpdateAmmoUI();
            }
        }
    }

    public void SetActiveWeapon(bool active)
    {
        if (!photonView.IsMine) return;
        isActiveWeapon = active;
        if (active) lastWeaponSwitchTime = Time.time;
        UpdateAmmoUI();
    }

    public void SetLastShotTime(float time) => lastShotTime = time;

    private void ShootDisc()
    {
        if (!discPrefab || !shootingPoint)
        {
            Debug.LogError("DiscShooter: brak prefab'u dysku albo shootingPoint.");
            return;
        }

        // instancja sieciowa
        GameObject discGO = PhotonNetwork.Instantiate(discPrefab.name, shootingPoint.position, shootingPoint.rotation);
        var discRB = discGO.GetComponent<Rigidbody>();
        var disc = discGO.GetComponent<Disc>();

        if (!discRB || !disc)
        {
            Debug.LogError("DiscShooter: prefab dysku musi mieć Rigidbody i Disc.");
            return;
        }

        // dziedziczenie prędkości gracza
        var playerRb = transform.root.GetComponent<Rigidbody>();
        Vector3 inherited = (playerRb ? playerRb.velocity : Vector3.zero) * inheritVelocityFactor;

        // startowa prędkość: kierunek strzału + dziedziczona
        discRB.velocity = shootingPoint.forward * discSpeed + inherited;

        // krótkie ignorowanie kolizji z właścicielem po stronie local (dodatkowa asekuracja)
        StartCoroutine(IgnoreCollisionTemporarilyLocal(discGO));

        PlayShootSound();
    }

    private void PlayShootSound()
    {
        if (shootSound && audioSource)
            audioSource.PlayOneShot(shootSound);
    }

    private void UpdateAmmoUI()
    {
        if (ammoUI) ammoUI.SetCurrentWeapon("DiscShooter");
    }

    private IEnumerator IgnoreCollisionTemporarilyLocal(GameObject disc)
    {
        if (!disc) yield break;

        var discCol = disc.GetComponent<Collider>();
        if (!discCol) yield break;

        var playerCols = transform.root.GetComponentsInChildren<Collider>();
        foreach (var c in playerCols)
            if (c) Physics.IgnoreCollision(discCol, c, true);

        yield return new WaitForSeconds(ignoreCollisionTime);

        if (!disc) yield break;
        foreach (var c in playerCols)
            if (c) Physics.IgnoreCollision(discCol, c, false);
    }
}
