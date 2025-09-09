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
    [Range(0f, 1f)] public float inheritVelocityFactor = 0.5f; // portion of player velocity added to the disc

    [Header("Audio")]
    public AudioClip shootSound;

    [Header("UX")]
    public float discRotationSpeed = 360f; // rotation of decorative attachedDisc
    public GameObject attachedDisc;

    [Header("Safety windows")]
    public float ignoreCollisionTime = 0.25f; // locally ignore the owner (same as Disc)

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
        if (!playerAmmoManager) Debug.LogWarning("DiscShooter: missing PlayerAmmoManager on the player object.");

        ammoUI = FindObjectOfType<AmmoUI>();
        if (!ammoUI) Debug.LogWarning("DiscShooter: no AmmoUI in the scene.");

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
            Debug.LogError("DiscShooter: missing disc prefab or shootingPoint.");
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

        // inherit player velocity
        var playerRb = transform.root.GetComponent<Rigidbody>();
        Vector3 inherited = (playerRb ? playerRb.velocity : Vector3.zero) * inheritVelocityFactor;

        // initial velocity: shoot direction + inherited
        discRB.velocity = shootingPoint.forward * discSpeed + inherited;

        // briefly ignore collisions with the owner on the local side (extra safety)
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
