using UnityEngine;
using UnityEngine.Serialization;

// her kit (Tommy, 2026-11-27). attack: she flicks her wand at an insect for instant grass damage, and the
// drained life flows back to her as motes, healing her a little. passive: that heal, and Rejuvenating
// Burgeon on herself, fill her golden sap; with sap she channels into a hurt plant, healing it over time
// and spending the sap. skill: revives a fallen plant (unchanged)
public class Rhodiola : Shooter
{
    // the mote of life energy (RhodiolaProjectile) her drain sends back to her and her channel sends out
    [FormerlySerializedAs("healProjectilePrefab")]
    [SerializeField] private GameObject lifeMotePrefab;

    private RhodiolaData RData => data as RhodiolaData;

    // her passive bar is her golden sap, gold, directly under her health bar
    protected override bool GetPassiveBarVisible() => true;
    protected override float GetPassiveBarFill() => SapCap > 0f ? Mathf.Clamp01(_storedSap / SapCap) : 0f;
    protected override Color? PassiveBarColor => PlantWidget.Style != null ? PlantWidget.Style.sapBarColor : SapColor;
    protected override bool PassiveBarFirst => true;

    // skill tree node unlock ids
    public const string RevivalAoeUnlock      = "rhodiola_revival_aoe";
    public const string RevivalEmpowerUnlock  = "rhodiola_revival_empower";
    public const string InstantSkillUnlock    = "rhodiola_instant_skill";
    // nodes of her old healing seed (a burst every 8th seed, a seed that bounced on): they do nothing in
    // this kit until they're redesigned
    public const string OvergrowthUnlock      = "rhodiola_overgrowth";
    public const string WidespreadBloomUnlock = "rhodiola_widespread_bloom";

    // heals and shields she gives grow with her grass damage
    public float GrassConversion => RData?.baseGrassConversion ?? 0.5f;

    // ---------- attack: drain ----------
    public float DrainHeal => attackDamage * (RData?.drainHealShare ?? 0.25f);
    private float DrainMoteSpeed => RData?.drainMoteSpeed ?? 5f;

    // ---------- passive: golden sap and the channel ----------
    public float SapCap        => (RData?.goldenSapCap ?? 200f) + (RData?.path2GoldenSapCapPerLevel ?? 40f) * effectivePath2Level;
    public float SapConversion => (RData?.sapConversion ?? 1f) + (RData?.path2SapConversionPerLevel ?? 0.5f) * effectivePath2Level;
    public float SapToChannel  => RData?.sapToChannel ?? 0.1f;
    public float LowHealthThreshold => RData?.lowHealthThreshold ?? 0.5f;
    public float ChannelHealMP => (RData?.channelHealMPScaling ?? 0.1f) * magicPower;
    public float ChannelHealPerSecond => (RData?.channelHealPerSecond ?? 30f) + ChannelHealMP;
    private float ChannelTickInterval => Mathf.Max(0.05f, RData?.channelTickInterval ?? 0.25f);
    private float ChannelMoteSpeed => RData?.channelMoteSpeed ?? 5f;

    private float _storedSap;
    public float StoredSap => _storedSap;
    private SpriteRenderer _sapGlow;
    private static readonly Color SapColor = new Color32(0xFF, 0xD7, 0x00, 0xFF);

    // Symbiosis max level bonus: full health favors more Grass Damage (see UpdateStats), otherwise
    // a portion of the healing her channel and Rejuvenating Burgeon give returns to her
    public float MaxLevelGrassDamageBonus => RData?.maxLevelGrassDamageBonus ?? 0.25f;
    public float MaxLevelHealingReturn    => RData?.maxLevelHealingReturn    ?? 0.33f;

    // Rejuvenating Burgeon: her attack's heal puts it on her, and while she has it her channel passes it on
    public float BurgeonHealPerSecond => (RData?.burgeonHealPerSecond ?? 12f) + (RData?.path2BurgeonHealPerLevel ?? 2f) * effectivePath2Level;
    public float BurgeonDuration      => RData?.baseBurgeonDuration  ?? 4f;
    public float BurgeonTickInterval  => RData?.burgeonTickInterval   ?? 0.5f;
    // burgeon armor scales straight with her grass damage: +100% grass damage doubles it
    public float BurgeonArmorBase     => (RData?.burgeonBaseArmor ?? 16f) + (RData?.path2BurgeonArmorPerLevel ?? 8f) * effectivePath2Level;
    public float BurgeonArmor         => BurgeonArmorBase * (1f + grassDamage);

    public float RevivalBaseHeal     => RData?.revivalBaseHeal     ?? 40f;
    public float RevivalHealPerLevel => RData?.revivalHealPerLevel ?? 20f;
    public float RevivalHealFlat     => RevivalBaseHeal + RevivalHealPerLevel * effectivePath3Level;
    public float RevivalMPHeal       => (RData?.skillHealMPScaling ?? 0.30f) * magicPower;
    public float RevivalHeal         => RevivalHealFlat + RevivalMPHeal;

    protected override void Awake()
    {
        base.Awake();
        LoadData();

        // free skill readiness on placement - deliberately bypasses UnlockPath3() (which spends
        // sun and adds to totalSunSpent) so this can't be abused for an inflated uproot refund
        if (SkillTreeManager.HasUnlock(this, InstantSkillUnlock))
        {
            path3Unlocked = true;
            OnPath3Unlock();
        }
    }

    protected override void Update()
    {
        // the channel is decided before the attack, so a plant dropping low stops her next flick
        if (IsAlive) UpdateChannel();
        base.Update();
        if (!IsAlive) return;
        UpdateSapGlow();
    }

    private void OnDisable()
    {
        _channelTarget = null;
        _releaseRoutine = null;
        _castTarget = null;
    }

    // a soft gold glow over her root (Resources/RhodiolaSapGlow), drawn just above her own sprite
    // with the same sorting and material, fading in as the sap fills and pulsing gently when full
    private void UpdateSapGlow()
    {
        if (_sapGlow == null)
        {
            Sprite glowSprite = Resources.Load<Sprite>("RhodiolaSapGlow");
            SpriteRenderer body = GetMainRenderer();
            if (glowSprite == null || body == null) return;
            GameObject glow = new GameObject("SapGlow");
            glow.transform.SetParent(transform, false);
            glow.transform.localPosition = new Vector3(0f, -0.27f, 0f);
            _sapGlow = glow.AddComponent<SpriteRenderer>();
            _sapGlow.sprite = glowSprite;
            _sapGlow.sharedMaterial = body.sharedMaterial;
            _sapGlow.sortingLayerID = body.sortingLayerID;
            _sapGlow.sortingOrder = body.sortingOrder + 1;
        }

        float fill = SapCap > 0f ? Mathf.Clamp01(_storedSap / SapCap) : 0f;
        float alpha = fill * (RData?.sapGlowMaxAlpha ?? 0.6f);
        if (fill >= 1f) alpha *= 0.85f + 0.15f * Mathf.Sin(Time.time * 4f);
        _sapGlow.enabled = alpha > 0.01f;
        Color c = SapColor;
        c.a = alpha;
        _sapGlow.color = c;
    }

    // healing she gets from her attack or from her own Rejuvenating Burgeon fills the sap by the
    // conversion, counted in full even when she's already at full health
    public void AddSap(float healing)
    {
        if (healing <= 0f) return;
        _storedSap = Mathf.Min(SapCap, _storedSap + healing * SapConversion);
    }

    // Symbiosis max level bonus: while not fully healthy, a portion of the healing her channel and her
    // Rejuvenating Burgeon give others returns to her. sourceless, so it never fills the sap and never
    // triggers another return
    public void ReturnMaxLevelHealing(float amount)
    {
        if (!IsPath2Maxed || health >= maxHealth) return;
        Heal(amount * MaxLevelHealingReturn);
    }

    // her skill level takes a lot off the cooldown (and growth mulches and the skill tree add more
    // levels), so it never goes under this (Tommy, 2026-11-22)
    public const float MinSkillCooldown = 10f;

    public override void UpdateStats()
    {
        base.UpdateStats();
        skillCooldown = Mathf.Max(MinSkillCooldown, skillCooldown);

        // Symbiosis max level bonus: fully healthy favors more Grass Damage instead of the return above.
        // additive, like every other elemental damage bonus in the codebase
        if (IsPath2Maxed && health >= maxHealth)
            grassDamage += MaxLevelGrassDamageBonus;

        // heals and shields she gives are increased by a portion of grass damage
        healingBonus += grassDamage * GrassConversion;
    }

    // ---------- attack: drain ----------
    // her attack animation (rhodiola_attack.png, 17 frames; gold hair 2026-11-27): she raises her wand
    // and flicks it, and the hit lands on attackReleaseFrame. the clip plays at its own speed, only sped up
    // when her attacks come faster than it lasts, and the release frame keeps its place in it either way
    [Header("Attack Animation")]
    [SerializeField] private float attackClipFrameRate = 12f;
    [SerializeField] private int attackClipFrames = 17;
    [Tooltip("the frame (from 0) her wand's flick lands on: the hit and the drain happen then")]
    [SerializeField] private int attackReleaseFrame = 9;
    private const string BodyAttackTrigger = "Attack";

    // the attack sound plays when the flick lands, not when she starts raising the wand
    protected override bool PlaysOwnAttackSound => true;

    // the insect her targeting picked, and the one the flick in progress is for. a new flick lands the old
    // one at once; a channel starting cuts it off with no hit, and so does her being switched off
    private Insect _attackTarget;
    private Insect _castTarget;
    private Coroutine _releaseRoutine;

    // while channeling, and while she steps back into place after it, she never attacks
    protected override GameObject FindTarget()
    {
        if (_channelTarget != null || Time.time < _channelRecoverUntil) return null;
        GameObject target = base.FindTarget();
        _attackTarget = target != null ? target.GetComponent<Insect>() : null;
        return target;
    }

    protected override void Shoot(Vector3 aim)
    {
        if (_attackTarget == null) return;
        if (_releaseRoutine != null) { StopCoroutine(_releaseRoutine); _releaseRoutine = null; ReleaseFlick(); }
        _castTarget = _attackTarget;
        float frameRate = Mathf.Max(0.01f, attackClipFrameRate);
        float clipLength = Mathf.Max(1, attackClipFrames) / frameRate;
        float speed = Mathf.Max(1f, clipLength / Mathf.Max(0.01f, attackCooldown));
        PlayBodyAnimation(BodyAttackTrigger, clipLength, clipLength / speed);
        _releaseRoutine = StartCoroutine(ReleaseAfter(Mathf.Max(0, attackReleaseFrame) / (frameRate * speed)));
    }

    private System.Collections.IEnumerator ReleaseAfter(float seconds)
    {
        if (seconds > 0f) yield return new WaitForSeconds(seconds);
        _releaseRoutine = null;
        ReleaseFlick();
    }

    private void CancelFlick()
    {
        if (_releaseRoutine != null) { StopCoroutine(_releaseRoutine); _releaseRoutine = null; }
        _castTarget = null;
    }

    // the flick lands: instant grass damage on the insect it was for (or, if that one died meanwhile,
    // whoever her targeting picks now), then the drained life flies back from the insect to her
    private void ReleaseFlick()
    {
        Insect insect = _castTarget;
        _castTarget = null;
        if (!IsAlive || _channelTarget != null) return;
        if (insect == null || !insect.IsAlive)
        {
            GameObject again = base.FindTarget();
            insect = again != null ? again.GetComponent<Insect>() : null;
        }
        if (insect == null || !insect.IsAlive) return;
        Vector3 from = insect.GetAimPoint();
        insect.Damage(attackDamage, damageType, elementalType, this, true,
            new DamageTag[] { DamageTag.Attack, DamageTag.SingleTarget });
        if (data != null) SfxPlayer.Play(data.attackSound, GemWorldPosition());
        // the drained life: Tommy's orb, arcing from the insect to the tip of her staff (RhodiolaDrainOrb)
        RhodiolaDrainOrb.Launch(this, from, DrainMoteSpeed, RData?.drainArcPerUnit ?? 0.3f, RData?.drainArcMin ?? 0.4f,
            lifeMotePrefab, () => OnDrainReturned(this));
    }

    // the drain reaches her: it heals her, fills her sap and puts Rejuvenating Burgeon on her
    private void OnDrainReturned(Entity self)
    {
        if (!IsAlive) return;
        float heal = DrainHeal;
        Heal(heal, this);
        AddSap(heal);
        ApplyBurgeon(this);
    }

    private void ApplyBurgeon(Entity entity)
    {
        entity.ApplyEffect(new RejuvenatingBurgeonEffect(entity, BurgeonDuration, 1, this,
            BurgeonHealPerSecond * BurgeonTickInterval, BurgeonTickInterval, BurgeonArmor));
    }

    // ---------- passive: the channel ----------
    // with more than SapToChannel of her cap in sap she channels into an ally: at once for the lowest
    // plant in range under LowHealthThreshold (cutting off her attack), or, with no insects in her range,
    // for the lowest hurt plant, and only when no plant is hurt, the lowest hurt friendly insect (Tommy:
    // last priority). it heals every ChannelTickInterval, spending as much sap as it heals, until the ally
    // is healed, dies or leaves her range, the sap runs out, or she's stunned. a channel started with no
    // insects around gives way to her attack when insects come, unless its plant is under the line by then,
    // to any plant that drops under the line, and (one on a friendly insect) to any hurt plant
    //
    // her heal animation (Tommy, 2026-11-27, rhodiola_heal.png: the steps back sheet): she steps back
    // (Rhodiola_HealStart), holds the channel (Rhodiola_HealLoop) and steps forward into place
    // (Rhodiola_HealEnd), on the body animator's Channeling bool. the first heal goes out once she has
    // stepped back, and she attacks again only once she's back in place
    [Header("Heal Animation")]
    [SerializeField] private float healClipFrameRate = 8f;
    [Tooltip("frames of her step back, before the channel's first heal")]
    [SerializeField] private int healStartFrames = 12;
    [Tooltip("frames of her step forward after the channel, before she attacks again")]
    [SerializeField] private int healEndFrames = 9;
    private const string BodyChannelBool = "Channeling";
    private float HealStartSeconds => Mathf.Max(0, healStartFrames) / Mathf.Max(0.01f, healClipFrameRate);
    private float HealEndSeconds => Mathf.Max(0, healEndFrames) / Mathf.Max(0.01f, healClipFrameRate);

    private Entity _channelTarget;
    private bool _channelUrgent;
    private float _channelTickTimer;
    private float _channelCheckTimer;
    private float _channelStartedAt;
    private float _channelRecoverUntil;
    private const float ChannelCheckInterval = 0.1f;

    public bool IsChannelingHeal => _channelTarget != null;
    public Entity ChannelTarget => _channelTarget;

    // an ally counts as hurt once it's missing at least this much health, so a plant a fraction of a
    // point under full doesn't keep her channeling with nothing to show for it
    private const float MinMissingHealth = 1f;
    private static bool Hurt(Entity e) => e.maxHealth - e.health >= MinMissingHealth;
    private static bool Alive(Entity e) => e != null && e.health > 0f;
    private bool InRange(Entity e) => Vector2.Distance(transform.position, e.transform.position) <= attackRange;
    private static float HealthShare(Entity e) => e.maxHealth > 0f ? e.health / e.maxHealth : 1f;
    private bool InsectInRange() => base.FindTarget() != null;

    private void UpdateChannel()
    {
        if (_storedSap > SapCap) _storedSap = SapCap;

        if (_channelTarget != null)
        {
            if (KeepChanneling())
            {
                _channelTickTimer += Time.deltaTime;
                float interval = ChannelTickInterval;
                while (_channelTarget != null && _channelTickTimer >= interval)
                {
                    _channelTickTimer -= interval;
                    ChannelTick(interval);
                }
                return;
            }
            StopChannel();
        }

        _channelCheckTimer -= Time.deltaTime;
        if (_channelCheckTimer > 0f) return;
        _channelCheckTimer = ChannelCheckInterval;
        TryStartChannel();
    }

    private bool KeepChanneling()
    {
        Entity ally = _channelTarget;
        if (IsStunned || _storedSap <= 0f) return false;
        if (!Alive(ally) || !InRange(ally) || !Hurt(ally) || !CanSee(ally.transform.position)) return false;
        bool isPlant = ally is Plant;
        if (!_channelUrgent && (!isPlant || HealthShare(ally) >= LowHealthThreshold))
        {
            if (InsectInRange()) return false;
            Plant low = LowestPlant(true);
            if (low != null && low != ally) return false;
            if (!isPlant && LowestPlant(false) != null) return false;
        }
        return true;
    }

    private void TryStartChannel()
    {
        if (IsStunned || Time.time < _channelRecoverUntil || _storedSap <= SapCap * SapToChannel) return;
        Plant low = LowestPlant(true);
        if (low != null) { StartChannel(low, true); return; }
        if (InsectInRange()) return;
        Entity hurt = LowestPlant(false);
        if (hurt == null) hurt = LowestFriendlyInsect();
        if (hurt != null) StartChannel(hurt, false);
    }

    // the lowest health hurt friendly insect (minions, hypnotized insects) in her range
    private Insect LowestFriendlyInsect()
    {
        Insect best = null;
        float bestShare = 1f;
        foreach (Insect ally in Insect.friendlyInsects)
        {
            if (!Alive(ally) || !Hurt(ally) || !InRange(ally) || !CanSee(ally.transform.position)) continue;
            float share = HealthShare(ally);
            if (share < bestShare) { bestShare = share; best = ally; }
        }
        return best;
    }

    // the lowest health plant in her range that she can see (line of sight), herself left out: under the low
    // health line, or just hurt
    private Plant LowestPlant(bool underLine)
    {
        Plant best = null;
        float bestShare = underLine ? LowHealthThreshold : 1f;
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || plant == this || !plant.IsAlive || plant.maxHealth <= 0f) continue;
            if (!Hurt(plant) || !InRange(plant) || !CanSee(plant.transform.position)) continue;
            float share = HealthShare(plant);
            if (share < bestShare) { bestShare = share; best = plant; }
        }
        return best;
    }

    private void StartChannel(Entity ally, bool urgent)
    {
        CancelFlick();
        _channelTarget = ally;
        _channelUrgent = urgent;
        _channelStartedAt = Time.time;
        // the first heal goes out once she has stepped back
        _channelTickTimer = ChannelTickInterval - HealStartSeconds;
        FaceAttackTarget(ally.gameObject);
        if (bodyAnimator != null) bodyAnimator.speed = 1f;
        SetBodyBool(BodyChannelBool, true);
    }

    // she steps forward into place (after finishing her step back, if it was still going), and only
    // attacks or channels again once she's there
    private void StopChannel()
    {
        if (_channelTarget != null)
        {
            SetBodyBool(BodyChannelBool, false);
            _channelRecoverUntil = Mathf.Max(Time.time, _channelStartedAt + HealStartSeconds) + HealEndSeconds;
        }
        _channelTarget = null;
        _channelCheckTimer = 0f;
    }

    // one heal of the channel: as much sap as it heals, never more than the ally is missing. while
    // she has Rejuvenating Burgeon herself, it passes the burgeon on
    private void ChannelTick(float interval)
    {
        Entity ally = _channelTarget;
        float amount = Mathf.Min(_storedSap, ChannelHealPerSecond * interval, ally.maxHealth - ally.health);
        if (amount <= 0f) { StopChannel(); return; }
        _storedSap -= amount;
        FaceAttackTarget(ally.gameObject);
        ally.Heal(amount, this);
        ReturnMaxLevelHealing(amount);
        if (HasEffect<RejuvenatingBurgeonEffect>()) ApplyBurgeon(ally);

        Vector3 gem = GemWorldPosition();
        SendMote(new Vector3(gem.x, transform.position.y, transform.position.z), gem.y, ally, ChannelMoteSpeed, null);
    }

    // a mote of life energy from ground (drawn launchY high) to target; onArrive runs when it gets there.
    // without the mote prefab the arrival happens at once
    private void SendMote(Vector3 ground, float launchY, Entity target, float speed, System.Action<Entity> onArrive)
    {
        RhodiolaProjectile mote = lifeMotePrefab != null
            ? Instantiate(lifeMotePrefab, ground, Quaternion.identity).GetComponent<RhodiolaProjectile>() : null;
        if (mote == null) { onArrive?.Invoke(target); return; }
        mote.Initialize(target, speed, onArrive);
        mote.SetLaunchHeight(launchY);
    }

    // where the crystal at the tip of her staff is right now: the tip in the frame she's showing
    // (RhodiolaStaffTips, read off every frame of her idle, attack and heal), or gemPixel for a frame not in it,
    // through her sprite's pivot and pixels per unit, mirrored when she faces right (flipX). her drain's orb
    // lands on it and her channel's life energy leaves from it
    [Tooltip("the crystal's middle for a frame RhodiolaStaffTips doesn't know, art px from the frame's top left (her unflipped art faces left)")]
    [SerializeField] private Vector2 gemPixel = new Vector2(20f, 6f);

    public Vector3 StaffTipPosition => GemWorldPosition();
    public SpriteRenderer BodyRenderer => GetMainRenderer();

    private Vector3 GemWorldPosition()
    {
        SpriteRenderer body = GetMainRenderer();
        if (body == null) return transform.position;
        Sprite sprite = body.sprite;
        float ppu = sprite != null ? sprite.pixelsPerUnit : 64f;
        Vector2 pivot = sprite != null ? sprite.pivot : new Vector2(32f, 32f);
        float height = sprite != null ? sprite.rect.height : 64f;
        Vector2 tip = sprite != null && RhodiolaStaffTips.TryGet(sprite.name, out Vector2 known) ? known : gemPixel;
        Vector2 local = new Vector2((tip.x - pivot.x) / ppu, (height - tip.y - pivot.y) / ppu);
        if (body.flipX) local.x = -local.x;
        return body.transform.TransformPoint(local);
    }

    // ---------- skill: revival (unchanged) ----------

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage    = data.baseAttackDamage + level * (RData?.path1AttackDamagePerLevel ?? 2f);
        baseAttackSpeed     = data.baseAttackSpeed  + level * (RData?.path1AttackSpeedPerLevel  ?? 0.04f);
        baseGrassDamage     = data.baseGrassDamage  + level * (RData?.path1GrassDamagePerLevel  ?? 0.05f);
        baseProjectileSpeed = data.baseProjectileSpeed;
        baseAttackRange     = data.baseAttackRange;
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillCooldown = Mathf.Max(MinSkillCooldown, data.baseSkillCooldown - level * (RData?.path3CooldownReductionPerLevel ?? 0f));
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        bool anyDead = false;
        foreach (var kvp in Tile.allTiles)
            if (kvp.Value.deadPlant != null) { anyDead = true; break; }
        if (!anyDead) return;
        SkillTargetingManager.instance.BeginDeadTileTargeting(OnTargetConfirmed);
    }

    private void OnTargetConfirmed(Tile tile)
    {
        // nothing to revive at the targeted tile - don't consume the skill (the player can
        // just re-target immediately, no cooldown/sun/cast wasted on an empty click)
        // a revived plant comes back with the mulches it had (Tommy, 2026-11-22; it took her skill's
        // max level until then)
        Plant revived = Plant.RevivePlant(tile, true);
        if (revived == null) return;

        skillCooldownTimer = skillCooldown;
        // the light coming down on it (Tommy, 2026-11-27)
        RhodiolaReviveBeam.Play(tile.transform.position);
        revived.Heal(RevivalHeal, this);

        if (SkillTreeManager.HasUnlock(this, RevivalAoeUnlock))
            ReviveNeighbors(tile);

        if (SkillTreeManager.HasUnlock(this, RevivalEmpowerUnlock))
            revived.ApplyEffect(new VerdantEmpowermentEffect(revived, 8f, 1, this));

        if (IsPath3Maxed)
        {
            float shield = RData?.verdantGuardianShield ?? 200f;
            float regen  = RData?.verdantGuardianRegen  ?? 20f;
            revived.ApplyEffect(new VerdantGuardianEffect(revived, skillDuration, this, shield, regen));
        }
    }

    // Verdant Resurgence: also revives any fallen plant on the 8 tiles surrounding the target
    private void ReviveNeighbors(Tile centerTile)
    {
        Vector3 center = centerTile.transform.position;
        Vector3[] offsets =
        {
            Vector3.right, Vector3.left, Vector3.up, Vector3.down,
            new Vector3(1, 1, 0), new Vector3(1, -1, 0), new Vector3(-1, 1, 0), new Vector3(-1, -1, 0)
        };
        foreach (Vector3 offset in offsets)
        {
            if (!Tile.allTiles.TryGetValue(Tile.TileKey(center + offset), out Tile neighbor)) continue;
            if (neighbor.deadPlant == null) continue;
            Plant revivedNeighbor = Plant.RevivePlant(neighbor, true);
            if (revivedNeighbor == null) continue;
            RhodiolaReviveBeam.Play(neighbor.transform.position);
            revivedNeighbor.Heal(RevivalHeal, this);
        }
    }

    // ---------- descriptions ----------

    public override string GetName() => TitleName("green");

    public override string GetDescription() =>
        $"The {GetName()} drains life from insects and pours it into her allies.";

    private string GoldenSapLabel => "<color=#FFD700><b>Golden Sap</b></color>";
    private string BurgeonLabel => "<color=green><b>Rejuvenating Burgeon</b></color>";

    public override string GetAttackDescription() =>
        $"Flicks her wand at an insect, dealing <color=green><b>{attackDamage:F0}</b></color> damage. The drained life flows back to her, healing her for <color=green><b>{DrainHeal:F0}</b></color> health.\n\n" +
        $"Increase <color=#FF6B81><b>Heals & Shields</b></color> given by <color=green><b>{GrassConversion * 100f:F0}%</b></color> of <color=green><b>Grass Damage</b></color>.";

    public override string GetPassiveDescription() =>
        $"Healing she gets from her attack and from {BurgeonLabel} on herself fills her {GoldenSapLabel} by <color=#FFD700><b>{SapConversion * 100f:F0}%</b></color> of the healing, even at full health, up to <color=#FFD700><b>{SapCap:F0}</b></color>.\n\n" +
        $"When a plant in range drops below <color=green><b>{LowHealthThreshold * 100f:F0}%</b></color> health, or no insects are in range and a plant is hurt, she channels {GoldenSapLabel} into it, healing <color=green><b>{ChannelHealPerSecond - ChannelHealMP:F0}</b></color> [<color=#FFB6C1><b>+{ChannelHealMP:F0}</b></color>] health per second until it's healed or the sap runs out. She needs over <color=#FFD700><b>{SapToChannel * 100f:F0}%</b></color> sap to start.\n\n" +
        $"Her attack's heal grants her {BurgeonLabel}, healing <color=green><b>{BurgeonHealPerSecond:F0}</b></color> health per second for <color=green><b>{BurgeonDuration:F0}s</b></color> and granting <color=#00CED1><b>+{BurgeonArmor:F0} Armor</b></color> (increased by her <color=green><b>{grassDamage * 100f:F0}%</b></color> <color=green><b>Grass Damage</b></color>). While she has it, her channel passes it on.";

    public override string GetSkillDesription() =>
        $"Target a tile where a plant has fallen to resurrect it, with the mulches it had. The plant is then healed for <color=green><b>{RevivalHealFlat:F0}</b></color> [<color=#FFB6C1><b>+{RevivalMPHeal:F0}</b></color>] Health.";

    public override string GetPath1Name() => "Verdance";
    public override string GetPath2Name() => "Symbiosis";
    public override string GetPath3Name() => "Revival";

    public override string GetPath1Description(bool details = false)
    {
        float dmgpl   = RData?.path1AttackDamagePerLevel ?? 2f;
        float speedpl = RData?.path1AttackSpeedPerLevel  ?? 0.04f;
        float grasspl = RData?.path1GrassDamagePerLevel  ?? 0.05f;
        string desc = details
            ? $"Flicks her wand at an insect, dealing <color=green><b>[100% Attack Damage]</b></color> damage. The drained life flows back to her, healing her for <color=green><b>[{(RData?.drainHealShare ?? 0.25f) * 100f:F0}% Attack Damage]</b></color> health.\n\n" +
              $"Increase <color=#FF6B81><b>Heals & Shields</b></color> given by <color=green><b>{GrassConversion * 100f:F0}%</b></color> of <color=green><b>Grass Damage</b></color>."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n{UpgradesBreak}" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{dmgpl:F0}</b></color> per level. [<color=green><b>+{dmgpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Speed</b></color> by <color=green><b>{speedpl:0.##}</b></color> per level. [<color=green><b>+{speedpl * effectivePath1Level:0.##}</b></color>]\n\n" +
               $"Increase <color=green><b>Grass Damage</b></color> by <color=green><b>{grasspl * 100f:F0}%</b></color> per level. [<color=green><b>+{grasspl * effectivePath1Level * 100f:F0}%</b></color>]\n\n" +
               $"{Level5Section(path1Level)}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float cappl   = RData?.path2GoldenSapCapPerLevel  ?? 40f;
        float convpl  = RData?.path2SapConversionPerLevel ?? 0.5f;
        float burgpl  = RData?.path2BurgeonHealPerLevel   ?? 2f;
        float armorpl = RData?.path2BurgeonArmorPerLevel  ?? 8f;
        string desc = details
            ? $"Healing she gets from her attack and from {BurgeonLabel} on herself fills her {GoldenSapLabel} by <color=#FFD700><b>[({(RData?.sapConversion ?? 1f) * 100f:F0}%) + ({convpl * 100f:F0}%/Lvl.)]</b></color> of the healing, even at full health, up to <color=#FFD700><b>[({RData?.goldenSapCap ?? 200f:F0}) + ({cappl:F0}/Lvl.)]</b></color>.\n\n" +
              $"When a plant in range drops below <color=green><b>{LowHealthThreshold * 100f:F0}%</b></color> health, or no insects are in range and a plant is hurt, she channels {GoldenSapLabel} into it, healing <color=green><b>[{RData?.channelHealPerSecond ?? 30f:F0}]</b></color> [<color=#FFB6C1><b>+{(RData?.channelHealMPScaling ?? 0.1f) * 100f:F0}% Magic Power</b></color>] health per second until it's healed or the sap runs out. She needs over <color=#FFD700><b>{SapToChannel * 100f:F0}%</b></color> sap to start.\n\n" +
              $"Her attack's heal grants her {BurgeonLabel}, healing <color=green><b>[({RData?.burgeonHealPerSecond ?? 12f:F0}) + ({burgpl:F0}/Lvl.)]</b></color> health per second for <color=green><b>{BurgeonDuration:F0}s</b></color> and granting <color=#00CED1><b>[({RData?.burgeonBaseArmor ?? 16f:F0}) + ({armorpl:F0}/Lvl.)] Armor</b></color>, multiplied by <color=green><b>(100% + Grass Damage)</b></color>. While she has it, her channel passes it on."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n{UpgradesBreak}" +
               $"Increase {GoldenSapLabel} capacity by <color=#FFD700><b>{cappl:F0}</b></color> per level. [<color=#FFD700><b>+{cappl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"Increase {GoldenSapLabel} conversion by <color=#FFD700><b>{convpl * 100f:F0}%</b></color> per level. [<color=#FFD700><b>+{convpl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase {BurgeonLabel} healing by <color=green><b>{burgpl:F0}</b></color> per second per level. [<color=green><b>+{burgpl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"Increase {BurgeonLabel} armor by <color=#00CED1><b>{armorpl:F0}</b></color> per level. [<color=#00CED1><b>+{armorpl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"{Level5Section(path2Level, $"If fully healthy, increase <color=green><b>Grass Damage</b></color> by <color=green><b>{MaxLevelGrassDamageBonus * 100f:F0}%</b></color>. Otherwise, <color=green><b>{MaxLevelHealingReturn * 100f:F0}%</b></color> of the healing her channel and <color=green><b>Rejuvenating Burgeon</b></color> give is returned to the {GetName()}.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float cdrpl = RData?.path3CooldownReductionPerLevel ?? 0.1f;
        string desc = details
            ? $"Target a tile where a plant has fallen to resurrect it, with the mulches it had. The plant is then healed for <color=green><b>[({RevivalBaseHeal:F0}) + ({RevivalHealPerLevel:F0}/Lvl.) + <color=#FFB6C1>{(RData?.skillHealMPScaling ?? 0.30f) * 100f:F0}% Magic Power</color>]</b></color> Health."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n{UpgradesBreak}" +
               $"Increase <color=green><b>Revival Heal</b></color> by <color=green><b>{RevivalHealPerLevel:F0}</b></color> per level. [<color=green><b>+{RevivalHealPerLevel * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Reduce <color=green><b>Base Skill Cooldown</b></color> by <color=green><b>{Mathf.RoundToInt(cdrpl)}s</b></color> per level, never below <color=green><b>{MinSkillCooldown:F0}s</b></color>. [<color=green><b>-{Mathf.RoundToInt(cdrpl * effectivePath3Level)}s</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, $"Upon reviving a plant, grant it <color=green><b>Verdant Guardian</b></color>, shielding it for <color=grey><b>{RData?.verdantGuardianShield ?? 200f:F0}</b></color> health and regenerating <color=green><b>{RData?.verdantGuardianRegen ?? 20f:F0}</b></color> health per second while the shield lasts, for <color=green><b>{skillDuration:F0}s</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
