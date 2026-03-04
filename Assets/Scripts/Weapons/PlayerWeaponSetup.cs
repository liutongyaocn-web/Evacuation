using UnityEngine;
using JUTPS;
using JUTPS.CharacterBrain;
using JUTPS.WeaponSystem;
using JUTPS.ItemSystem;
using JUTPS.InventorySystem;

/// <summary>
/// Initializes weapons on the player character at runtime.
/// Attach this to the Player GameObject. It creates weapon GameObjects
/// under the hand bones and configures them for the JU TPS Controller system.
/// Must run before JUInventory.SetupItems() discovers items.
///
/// KEY ORIENTATION NOTES (from analysis of JU TPS demo + Synty models):
/// - Synty PolygonMilitary weapons have barrel along LOCAL +Z natively
/// - Our character's Hand_R bone has fingers extending along local +X
/// - Weapon root needs (0, 90, -90) euler: Y=90 maps barrel +Z → finger +X,
///   Z=-90 rolls the weapon so top faces UP (hand -Z) instead of LEFT (hand +Y)
/// - Mesh child needs NO rotation (Synty barrel is already +Z)
/// - CameraAimingPosition uses weapon.transform.right/up for offsets
/// - localPosition offsets weapon from Hand_R bone (wrist) to grip center
/// </summary>
[DefaultExecutionOrder(-50)]
[AddComponentMenu("Evacuation/Player Weapon Setup")]
public class PlayerWeaponSetup : MonoBehaviour
{
    public enum WeaponCategory { Gun, Melee, Grenade }

    [System.Serializable]
    public class WeaponEntry
    {
        [Header("Basic")]
        public string weaponName = "Weapon";
        public GameObject meshPrefab;
        public WeaponCategory category = WeaponCategory.Gun;
        public bool startUnlocked = true;
        [Tooltip("Weapon icon texture for HUD display (converted to Sprite at runtime)")]
        public Texture2D itemIconTexture;

        [Header("Weapon Root Transform (under Hand_R)")]
        [Tooltip("Local position offset from hand bone")]
        public Vector3 localPosition = new Vector3(0.1f, 0, 0.005f);
        [Tooltip("Local euler rotation to orient weapon in hand")]
        public Vector3 localRotation = new Vector3(-10, 90, -90);

        [Header("Mesh Adjustments")]
        [Tooltip("Position offset for the mesh child under weapon root")]
        public Vector3 meshOffset = Vector3.zero;
        [Tooltip("Euler rotation for mesh child (Synty models: barrel is +Z, usually no rotation needed)")]
        public Vector3 meshRotation = Vector3.zero;
        [Tooltip("Uniform scale multiplier for the mesh")]
        public float meshScale = 1f;

        [Header("Holding Style")]
        public JUHoldableItem.ItemHoldingPose holdPose = JUHoldableItem.ItemHoldingPose.Rifle;
        public JUHoldableItem.ItemSwitchPosition pushFrom = JUHoldableItem.ItemSwitchPosition.Back;
        public int wieldPositionID = 1;

        [Header("Gun Settings")]
        public Weapon.WeaponFireMode fireMode = Weapon.WeaponFireMode.Auto;
        public Weapon.WeaponAimMode aimMode = Weapon.WeaponAimMode.CameraApproach;
        public int bulletsPerMagazine = 30;
        public int totalBullets = 120;
        public float fireRate = 0.1f;
        [Range(0.1f, 50f)] public float precision = 0.5f;
        [Range(0.01f, 1f)] public float lossOfAccuracy = 0.1f;
        [Range(0f, 0.3f)] public float recoilForce = 0.05f;
        [Range(-100, 100)] public float recoilRotation = 15f;
        public float cameraFOV = 60f;
        public Vector3 cameraAimPosition = new Vector3(0, 0.1f, -0.2f);
        public Vector3 shootPositionOffset = new Vector3(0, 0.02f, 0.4f);
        public int shotgunPellets = 12;

        [Header("Melee Settings")]
        public float meleeDamage = 30f;
        public string meleeAttackParam = "OneHandMeleeAttack";

        [Header("Grenade Settings")]
        public float throwForce = 15f;
        public float throwUpForce = 8f;
        public float timeToExplode = 3f;
        public int startQuantity = 3;
    }

    [Header("Effect Prefabs (from JU TPS Controller)")]
    [Tooltip("Bullet prefab with Bullet component")]
    public GameObject bulletPrefab;
    [Tooltip("Shotgun bullet prefab (optional)")]
    public GameObject shotgunBulletPrefab;
    [Tooltip("Muzzle flash particle prefab")]
    public GameObject muzzleFlashPrefab;
    [Tooltip("Explosion prefab for grenades")]
    public GameObject explosionPrefab;

    [Header("Weapon Sounds (shared)")]
    [Tooltip("Per-weapon shoot sounds (assign in order: Pistol, Rifle, Shotgun, Sniper)")]
    public AudioClip[] shootSounds;
    [Tooltip("Reload sound (shared by all guns)")]
    public AudioClip reloadSound;
    [Tooltip("Empty magazine click sound")]
    public AudioClip emptyMagSound;
    [Tooltip("Weapon equip sound")]
    public AudioClip equipSound;

    [Header("Weapons to Create")]
    public WeaponEntry[] weapons = new WeaponEntry[]
    {
        // [0] Pistol
        new WeaponEntry
        {
            weaponName = "Pistol",
            category = WeaponCategory.Gun,
            localPosition = new Vector3(0.1f, 0, 0.005f),
            localRotation = new Vector3(-10, 90, -90),
            meshOffset = new Vector3(0, 0.035f, 0),
            meshRotation = Vector3.zero,
            meshScale = 1f,
            holdPose = JUHoldableItem.ItemHoldingPose.PistolTwoHands,
            pushFrom = JUHoldableItem.ItemSwitchPosition.Hips,
            wieldPositionID = 0,
            fireMode = Weapon.WeaponFireMode.SemiAuto,
            aimMode = Weapon.WeaponAimMode.CameraApproach,
            bulletsPerMagazine = 15,
            totalBullets = 60,
            fireRate = 0.15f,
            precision = 0.3f,
            lossOfAccuracy = 0.08f,
            recoilForce = 0.03f,
            recoilRotation = 20f,
            cameraFOV = 55f,
            cameraAimPosition = new Vector3(-0.0004f, 0.063f, -0.3f),
            shootPositionOffset = new Vector3(0, 0.06f, 0.24f),
            startUnlocked = true
        },
        // [1] Assault Rifle
        new WeaponEntry
        {
            weaponName = "Assault Rifle",
            category = WeaponCategory.Gun,
            localPosition = new Vector3(0.1f, 0, 0.005f),
            localRotation = new Vector3(-10, 90, -90),
            meshOffset = new Vector3(0, 0.04f, -0.08f),
            meshRotation = Vector3.zero,
            meshScale = 1f,
            holdPose = JUHoldableItem.ItemHoldingPose.Rifle,
            pushFrom = JUHoldableItem.ItemSwitchPosition.Back,
            wieldPositionID = 1,
            fireMode = Weapon.WeaponFireMode.Auto,
            aimMode = Weapon.WeaponAimMode.CameraApproach,
            bulletsPerMagazine = 30,
            totalBullets = 120,
            fireRate = 0.1f,
            precision = 0.5f,
            lossOfAccuracy = 0.1f,
            recoilForce = 0.05f,
            recoilRotation = 15f,
            cameraFOV = 50f,
            cameraAimPosition = new Vector3(0, 0.11f, -0.2f),
            shootPositionOffset = new Vector3(0, 0.06f, 1.1f),
            startUnlocked = true
        },
        // [2] Shotgun
        new WeaponEntry
        {
            weaponName = "Shotgun",
            category = WeaponCategory.Gun,
            localPosition = new Vector3(0.1f, 0, 0.005f),
            localRotation = new Vector3(-10, 90, -90),
            meshOffset = new Vector3(0, 0.04f, -0.06f),
            meshRotation = Vector3.zero,
            meshScale = 1f,
            holdPose = JUHoldableItem.ItemHoldingPose.Rifle,
            pushFrom = JUHoldableItem.ItemSwitchPosition.Back,
            wieldPositionID = 1,
            fireMode = Weapon.WeaponFireMode.Shotgun,
            aimMode = Weapon.WeaponAimMode.CameraApproach,
            bulletsPerMagazine = 8,
            totalBullets = 32,
            fireRate = 0.8f,
            precision = 0.4f,
            lossOfAccuracy = 0.3f,
            recoilForce = 0.12f,
            recoilRotation = 30f,
            cameraFOV = 55f,
            cameraAimPosition = new Vector3(0, 0.12f, -0.2f),
            shootPositionOffset = new Vector3(0, 0.06f, 0.85f),
            shotgunPellets = 12,
            startUnlocked = true
        },
        // [3] Sniper
        new WeaponEntry
        {
            weaponName = "Sniper",
            category = WeaponCategory.Gun,
            localPosition = new Vector3(0.1f, 0, 0.005f),
            localRotation = new Vector3(-10, 90, -90),
            meshOffset = new Vector3(0, 0.04f, -0.1f),
            meshRotation = Vector3.zero,
            meshScale = 1f,
            holdPose = JUHoldableItem.ItemHoldingPose.Rifle,
            pushFrom = JUHoldableItem.ItemSwitchPosition.Back,
            wieldPositionID = 1,
            fireMode = Weapon.WeaponFireMode.BoltAction,
            aimMode = Weapon.WeaponAimMode.Scope,
            bulletsPerMagazine = 5,
            totalBullets = 25,
            fireRate = 1.5f,
            precision = 0.1f,
            lossOfAccuracy = 0.5f,
            recoilForce = 0.15f,
            recoilRotation = 25f,
            cameraFOV = 15f,
            cameraAimPosition = new Vector3(0, 0.15f, 1.05f),
            shootPositionOffset = new Vector3(0, 0.06f, 1.3f),
            startUnlocked = true
        },
        // [4] Knife
        new WeaponEntry
        {
            weaponName = "Knife",
            category = WeaponCategory.Melee,
            localPosition = new Vector3(0.1f, 0, 0.005f),
            localRotation = new Vector3(-10, 90, -90),
            meshOffset = new Vector3(0, 0.02f, 0),
            meshRotation = Vector3.zero,
            holdPose = JUHoldableItem.ItemHoldingPose.PistolOneHand,
            pushFrom = JUHoldableItem.ItemSwitchPosition.Hips,
            wieldPositionID = 0,
            meleeDamage = 40f,
            startUnlocked = true
        }
    };

    [Header("Settings")]
    [Tooltip("Index of the weapon to equip on start (-1 = fists)")]
    public int equipOnStartIndex = 0;

    private Animator _animator;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        if (_animator == null)
        {
            Debug.LogError("[PlayerWeaponSetup] No Animator found on " + gameObject.name);
            return;
        }

        // Ensure JUInventory exists on this GameObject
        if (!TryGetComponent<JUInventory>(out _))
        {
            gameObject.AddComponent<JUInventory>();
        }

        // Get hand bones
        Transform rightHand = _animator.GetBoneTransform(HumanBodyBones.RightHand);

        if (rightHand == null)
        {
            Debug.LogError("[PlayerWeaponSetup] RightHand bone not found in Animator");
            return;
        }

        // Create each weapon under the right hand bone
        for (int i = 0; i < weapons.Length; i++)
        {
            try
            {
                CreateWeapon(weapons[i], rightHand, i);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PlayerWeaponSetup] Failed to create weapon '{weapons[i].weaponName}': {e.Message}\n{e.StackTrace}");
            }
        }
    }

    void Start()
    {
        // Assign weapons to sequential slots (number keys 1-5) and equip starting weapon
        // Use a short delay so JUInventory.SetupItems() finishes first
        Invoke(nameof(PostInventorySetup), 0.1f);
    }

    void PostInventorySetup()
    {
        var inventory = GetComponent<JUInventory>();
        if (inventory == null || inventory.HoldableItensRightHand == null)
        {
            Debug.LogError("[PlayerWeaponSetup] JUInventory or HoldableItensRightHand is null!");
            return;
        }

        Debug.Log($"[PlayerWeaponSetup] Found {inventory.HoldableItensRightHand.Length} items in right hand inventory");

        // Map each weapon to a sequential slot (number keys 1-10)
        JUInventory.SequentialSlotsEnum[] slotEnums = new[]
        {
            JUInventory.SequentialSlotsEnum.first,
            JUInventory.SequentialSlotsEnum.second,
            JUInventory.SequentialSlotsEnum.third,
            JUInventory.SequentialSlotsEnum.fourth,
            JUInventory.SequentialSlotsEnum.fifth,
            JUInventory.SequentialSlotsEnum.sixth,
            JUInventory.SequentialSlotsEnum.seventh,
            JUInventory.SequentialSlotsEnum.eighth,
            JUInventory.SequentialSlotsEnum.ninth,
            JUInventory.SequentialSlotsEnum.tenth,
        };

        int count = Mathf.Min(inventory.HoldableItensRightHand.Length, slotEnums.Length);
        for (int i = 0; i < count; i++)
        {
            inventory.SetSequentialSlotItem(slotEnums[i], inventory.HoldableItensRightHand[i]);
        }

        // Equip starting weapon
        if (equipOnStartIndex >= 0)
        {
            Invoke(nameof(EquipStartingWeapon), 0.25f);
        }
    }

    void EquipStartingWeapon()
    {
        var brain = GetComponent<JUCharacterBrain>();
        if (brain != null && brain.Inventory != null)
        {
            brain.SwitchToItem(equipOnStartIndex, true);
        }
    }

    GameObject CreateWeapon(WeaponEntry config, Transform handBone, int weaponIndex)
    {
        // Create weapon root GameObject
        GameObject weaponGO = new GameObject(config.weaponName);
        weaponGO.transform.SetParent(handBone, false);
        weaponGO.transform.localPosition = config.localPosition;
        weaponGO.transform.localEulerAngles = config.localRotation;

        // Instantiate visual mesh prefab
        if (config.meshPrefab != null)
        {
            GameObject mesh = Instantiate(config.meshPrefab, weaponGO.transform);
            mesh.transform.localPosition = config.meshOffset;
            mesh.transform.localEulerAngles = config.meshRotation;
            mesh.transform.localScale = Vector3.one * config.meshScale;
            mesh.name = config.meshPrefab.name;

            // Remove colliders from Synty prefabs (interfere with camera and physics)
            foreach (var c in mesh.GetComponentsInChildren<Collider>(true))
                Destroy(c);
        }

        // Setup based on weapon category
        switch (config.category)
        {
            case WeaponCategory.Gun:
                SetupGun(weaponGO, config, weaponIndex);
                break;
            case WeaponCategory.Melee:
                SetupMelee(weaponGO, config);
                break;
            case WeaponCategory.Grenade:
                SetupGrenade(weaponGO, config);
                break;
        }

        return weaponGO;
    }

    void SetupGun(GameObject weaponGO, WeaponEntry config, int weaponIndex)
    {
        // AudioSource (required by Weapon)
        AudioSource audioSource = weaponGO.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        // Shoot position child
        GameObject shootPosGO = new GameObject("Shoot Position");
        shootPosGO.transform.SetParent(weaponGO.transform, false);
        shootPosGO.transform.localPosition = config.shootPositionOffset;

        // Add Weapon component
        Weapon weapon = weaponGO.AddComponent<Weapon>();

        // Initialize UnityEvent (not auto-initialized when added via code)
        weapon.OnShot = new UnityEngine.Events.UnityEvent();

        // Item settings
        weapon.ItemName = config.weaponName;
        weapon.Unlocked = config.startUnlocked;
        weapon.ItemQuantity = 1;
        weapon.MaxItemQuantity = 1;
        weapon.ItemIcon = TextureToSprite(config.itemIconTexture);

        // Holding settings
        weapon.HoldPose = config.holdPose;
        weapon.PushItemFrom = config.pushFrom;
        weapon.ItemWieldPositionID = config.wieldPositionID;

        // Gun settings
        weapon.BulletsPerMagazine = config.bulletsPerMagazine;
        weapon.TotalBullets = config.totalBullets;
        weapon.BulletsAmounts = config.bulletsPerMagazine;
        weapon.Fire_Rate = config.fireRate;
        weapon.Precision = config.precision;
        weapon.LossOfAccuracyPerShot = config.lossOfAccuracy;
        weapon.FireMode = config.fireMode;
        weapon.AimMode = config.aimMode;
        weapon.CameraAimingPosition = config.cameraAimPosition;
        weapon.CameraFOV = config.cameraFOV;
        weapon.NumberOfShotgunBulletsPerShot = config.shotgunPellets;

        // Sounds
        if (shootSounds != null && weaponIndex < shootSounds.Length && shootSounds[weaponIndex] != null)
            weapon.ShootAudio = shootSounds[weaponIndex];
        if (reloadSound != null) weapon.ReloadAudio = reloadSound;
        if (emptyMagSound != null) weapon.EmptyMagazineAudio = emptyMagSound;
        if (equipSound != null) weapon.WeaponEquipAudio = equipSound;

        // Procedural animation
        weapon.GenerateProceduralAnimation = true;
        weapon.RecoilForce = config.recoilForce;
        weapon.RecoilForceRotation = config.recoilRotation;
        weapon.WeaponPositionSpeed = 20f;
        weapon.WeaponRotationSpeed = 20f;
        weapon.CameraRecoilMultiplier = 1f;

        // Prefab references
        weapon.Shoot_Position = shootPosGO.transform;
        weapon.BulletPrefab = GetBulletForConfig(config);
        weapon.MuzzleFlashParticlePrefab = muzzleFlashPrefab;
    }

    void SetupMelee(GameObject weaponGO, WeaponEntry config)
    {
        // AudioSource
        AudioSource audioSource = weaponGO.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        // MeleeWeapon component
        MeleeWeapon melee = weaponGO.AddComponent<MeleeWeapon>();

        // Item settings
        melee.ItemName = config.weaponName;
        melee.Unlocked = config.startUnlocked;
        melee.ItemQuantity = 1;
        melee.MaxItemQuantity = 1;
        melee.ItemIcon = TextureToSprite(config.itemIconTexture);

        // Holding
        melee.HoldPose = config.holdPose;
        melee.PushItemFrom = config.pushFrom;
        melee.ItemWieldPositionID = config.wieldPositionID;

        // Note: MeleeWeapon has no audio fields

        // Attack parameter
        melee.AttackAnimatorParameterName = config.meleeAttackParam;

        // Create damager child
        GameObject damagerGO = new GameObject("Damager");
        damagerGO.transform.SetParent(weaponGO.transform, false);
        damagerGO.transform.localPosition = new Vector3(0, 0, 0.25f);
        damagerGO.SetActive(false);

        BoxCollider col = damagerGO.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(0.15f, 0.15f, 0.35f);

        Damager damager = damagerGO.AddComponent<Damager>();
        damager.Damage = config.meleeDamage;
        damager.DisableOnStart = true;

        melee.DamagerToEnable = damager;
    }

    void SetupGrenade(GameObject weaponGO, WeaponEntry config)
    {
        // AudioSource
        AudioSource audioSource = weaponGO.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        // Rigidbody (starts kinematic, enabled on throw)
        Rigidbody rb = weaponGO.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        // Collider (disabled, enabled on throw)
        SphereCollider col = weaponGO.AddComponent<SphereCollider>();
        col.radius = 0.05f;
        col.enabled = false;

        // Granade component
        Granade grenade = weaponGO.AddComponent<Granade>();

        // Item settings
        grenade.ItemName = config.weaponName;
        grenade.Unlocked = config.startUnlocked;
        grenade.ItemQuantity = config.startQuantity;
        grenade.MaxItemQuantity = config.startQuantity + 2;
        grenade.SingleUseItem = true;
        grenade.ItemIcon = TextureToSprite(config.itemIconTexture);

        // Holding
        grenade.HoldPose = config.holdPose;
        grenade.PushItemFrom = config.pushFrom;
        grenade.ItemWieldPositionID = config.wieldPositionID;

        // Throw settings
        grenade.ThrowForce = config.throwForce;
        grenade.ThrowUpForce = config.throwUpForce;
        grenade.TimeToExplode = config.timeToExplode;
        grenade.ExplosionPrefab = explosionPrefab;
    }

    GameObject GetBulletForConfig(WeaponEntry config)
    {
        if (config.fireMode == Weapon.WeaponFireMode.Shotgun && shotgunBulletPrefab != null)
            return shotgunBulletPrefab;
        return bulletPrefab;
    }

    Sprite TextureToSprite(Texture2D tex)
    {
        if (tex == null) return null;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }
}
