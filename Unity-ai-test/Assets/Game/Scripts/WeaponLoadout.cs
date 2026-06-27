using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Player weapon arsenal. Holds several weapon definitions, configures the single
/// <see cref="Weapon"/> component when switching, preserves each gun's ammo, and
/// reads switch/reload input via the new Input System.
/// Keys: 1/2/3/4 select, Q/wheel cycle, R reload. Gamepad: shoulders cycle, X reload.
/// </summary>
public class WeaponLoadout : MonoBehaviour
{
    [System.Serializable]
    public class Spec
    {
        public string name;
        public string fireSfx;
        public float damage;
        public float range;
        public float fireRate;
        public int pellets = 1;
        public float spread = 0f;
        public int magazineSize;
        public int reloadTime_ms;
        public int startReserve;
        public Color tracer;
    }

    public Weapon weapon;

    readonly List<Spec> _specs = new();
    int[] _mag;
    int[] _reserve;
    int _index;

    public int Index => _index;
    public int Count => _specs.Count;
    public string CurrentName => weapon != null ? weapon.weaponName : "";

    void Awake()
    {
        if (weapon == null) weapon = GetComponentInChildren<Weapon>();

        _specs.Add(new Spec { name = "Pistol",  fireSfx = "shot_pistol",  damage = 26f, range = 42f, fireRate = 5f,  pellets = 1, spread = 1.5f, magazineSize = 12, reloadTime_ms = 1100, startReserve = 96,  tracer = new Color(1f, 0.4f, 0.85f) });
        _specs.Add(new Spec { name = "SMG",     fireSfx = "shot_smg",     damage = 14f, range = 38f, fireRate = 13f, pellets = 1, spread = 4.5f, magazineSize = 30, reloadTime_ms = 1500, startReserve = 240, tracer = new Color(0.4f, 1f, 0.9f) });
        _specs.Add(new Spec { name = "Shotgun", fireSfx = "shot_shotgun", damage = 12f, range = 26f, fireRate = 1.4f, pellets = 8, spread = 11f, magazineSize = 6,  reloadTime_ms = 2000, startReserve = 36,  tracer = new Color(1f, 0.75f, 0.25f) });
        _specs.Add(new Spec { name = "Laser",   fireSfx = "shot_laser",   damage = 40f, range = 55f, fireRate = 3f,  pellets = 1, spread = 0f,   magazineSize = 8,  reloadTime_ms = 1300, startReserve = 64,  tracer = new Color(0.7f, 0.6f, 1f) });

        _mag = new int[_specs.Count];
        _reserve = new int[_specs.Count];
        for (int i = 0; i < _specs.Count; i++)
        {
            _mag[i] = _specs[i].magazineSize;
            _reserve[i] = _specs[i].startReserve;
        }
    }

    void Start()
    {
        Apply(0, silent: true);
    }

    void Update()
    {
        if (weapon == null) return;
        var kb = Keyboard.current;
        var gp = Gamepad.current;

        if (kb != null)
        {
            if (kb.digit1Key.wasPressedThisFrame) Apply(0);
            else if (kb.digit2Key.wasPressedThisFrame) Apply(1);
            else if (kb.digit3Key.wasPressedThisFrame) Apply(2);
            else if (kb.digit4Key.wasPressedThisFrame) Apply(3);
            else if (kb.qKey.wasPressedThisFrame) Cycle(-1);
            if (kb.rKey.wasPressedThisFrame) weapon.StartReload();
        }

        var mouse = Mouse.current;
        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll > 0.5f) Cycle(1);
            else if (scroll < -0.5f) Cycle(-1);
        }

        if (gp != null)
        {
            if (gp.rightShoulder.wasPressedThisFrame) Cycle(1);
            else if (gp.leftShoulder.wasPressedThisFrame) Cycle(-1);
            if (gp.buttonWest.wasPressedThisFrame) weapon.StartReload();
        }
    }

    void Cycle(int dir)
    {
        Apply((_index + dir + _specs.Count) % _specs.Count);
    }

    void Apply(int i, bool silent = false)
    {
        if (i < 0 || i >= _specs.Count || weapon == null) return;

        // Save the outgoing weapon's live ammo.
        _mag[_index] = weapon.Magazine;
        _reserve[_index] = weapon.Reserve;

        _index = i;
        var s = _specs[i];
        weapon.weaponName = s.name;
        weapon.fireSfx = s.fireSfx;
        weapon.damage = s.damage;
        weapon.range = s.range;
        weapon.fireRate = s.fireRate;
        weapon.pellets = s.pellets;
        weapon.spread = s.spread;
        weapon.magazineSize = s.magazineSize;
        weapon.reloadTime = s.reloadTime_ms / 1000f;
        weapon.tracerColor = s.tracer;
        weapon.infiniteAmmo = false;
        weapon.autoReload = true;
        weapon.SetAmmo(_mag[i], _reserve[i]);

        if (!silent)
        {
            SfxManager.Play("switch", 0.6f);
            GameManager.Instance?.ShowBanner(s.name.ToUpper(), 0.8f);
        }
    }
}
