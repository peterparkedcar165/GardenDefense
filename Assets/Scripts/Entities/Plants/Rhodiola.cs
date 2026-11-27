using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// her kit (Tommy, 2026-11-27). attack: she flicks her wand at an insect for instant grass damage, and the
// drained life flows back to her as motes, healing her a little. passive: that heal, and Rejuvenating
// Burgeon on herself, fill her golden sap; with sap she channels into a hurt plant, healing it over time
// and spending the sap, shown by her heal ray (RhodiolaHealRay). skill: revives a fallen plant (unchanged)
public class Rhodiola : Shooter
{
    // the mote of life energy (RhodiolaProjectile): its trail goes on her drain's orb
    [FormerlySerializedAs("healProjectilePrefab")]
    [SerializeField] private GameObject lifeMotePrefab;

    private RhodiolaData RData => data as RhodiolaData;

    // her projectile's glint, the life mote's particle material: her skill's glitter wears it too, so the revive
    // beam's and the fallen plants' sparkles have her drain orb's colours (Tommy, 2026-11-27)
    private Material _glint;
    private bool _glintLooked;
    private Material Glint
    {
        get
        {
            if (!_glintLooked)
            {
                _glintLooked = true;
                ParticleSystemRenderer r = lifeMotePrefab != null ? lifeMotePrefab.GetComponentInChildren<ParticleSystemRenderer>(true) : null;
                _glint = r != null ? r.sharedMaterial : null;
            }
            return _glint;
        }
    }

    // her passive bar is her golden sap, gold, directly under her health bar
    protected override bool GetPassiveBarVisible() => true;
    protected override float GetPassiveBarFill() => SapCap > 0f ? Mathf.Clamp01(_storedSap / SapCap) : 0f;
    protected override Color? PassiveBarColor => PlantWidget.Style != null ? PlantWidget.Style.sapBarColor : SapColor;
    protected override bool PassiveBarFirst => true;

    // her Passive's tooltip shows her Golden Sap in a bar like the health bar, in the sap's gold (Tommy, 2026-11-27)
    public override bool TryGetPathBar(int path, out float value, out float max, out Color color)
    {
        value = _storedSap;
        max = SapCap;
        color = PassiveBarColor ?? SapColor;
        return path == 2;
    }

    // skill tree node unlock ids
    public const string RevivalAoeUnlock      = "rhodiola_revival_aoe";
    public const string RevivalEmpowerUnlock  = "rhodiola_revival_empower";
    public const string InstantSkillUnlock    = "rhodiola_instant_skill";
    // nodes of her old healing seed (a burst every 8th seed, a seed that bounced on): they do nothing in
    // this kit until they're redesigned
    public const string OvergrowthUnlock      = "rhodiola_overgrowth";
    public const string WidespreadBloomUnlock = "rhodiola_widespread_bloom";

    // Attack path max level bonus (Tommy, 2026-11-27): this share of her grass damage goes to the heals and shields she
    // gives (1: her healing benefits from her grass damage as her damage does)
    public float GrassConversion => RData?.baseGrassConversion ?? 1f;

    // ---------- attack: drain ----------
    // her drain heals her for this share of the damage the hit really dealt
    public float DrainHealShare => RData?.drainHealShare ?? 0.5f;
    private float DrainMoteSpeed => RData?.drainMoteSpeed ?? 5f;

    // ---------- passive: golden sap and the channel ----------
    public float SapCap        => (RData?.goldenSapCap ?? 200f) + (RData?.path2GoldenSapCapPerLevel ?? 20f) * effectivePath2Level;
    public float SapConversion => (RData?.sapConversion ?? 1f) + (RData?.path2SapConversionPerLevel ?? 0.1f) * effectivePath2Level;
    public float SapToChannel  => RData?.sapToChannel ?? 0.1f;
    public float LowHealthThreshold => RData?.lowHealthThreshold ?? 0.5f;
    public float ChannelHealMP => (RData?.channelHealMPScaling ?? 0.05f) * magicPower;
    public float ChannelHealPerSecond => (RData?.channelHealPerSecond ?? 40f) + ChannelHealMP;
    // healing to sap depletion ratio (Tommy, 2026-11-27): each Attack path level makes the channel heal more for each point
    // of sap it spends, 1 at level 0 (sap goes as fast as the healing)
    public float SapEfficiency => 1f + (RData?.path1SapEfficiencyPerLevel ?? 0.1f) * effectivePath1Level;
    private float ChannelTickInterval => Mathf.Max(0.05f, RData?.channelTickInterval ?? 0.25f);

    private float _storedSap;
    public float StoredSap => _storedSap;
    private SpriteRenderer _sapGlow;
    private static readonly Color SapColor = new Color32(0xFF, 0xD7, 0x00, 0xFF);

    // Symbiosis max level bonus (Tommy, 2026-11-27): her Rejuvenating Burgeon on any plant, herself included, fills her
    // golden sap as it heals (RejuvenatingBurgeonEffect). it was more grass damage at full health, else a share of the
    // healing returned to her
    public bool BurgeonFillsSap => IsPath2Maxed;

    // Rejuvenating Burgeon: her attack's heal puts it on her, and while she has it her channel passes it on
    public float BurgeonHealPerSecond => (RData?.burgeonHealPerSecond ?? 12f) + (RData?.path2BurgeonHealPerLevel ?? 2f) * effectivePath2Level;
    public float BurgeonDuration      => RData?.baseBurgeonDuration  ?? 4f;
    public float BurgeonTickInterval  => RData?.burgeonTickInterval   ?? 0.5f;
    // burgeon armor scales straight with her magic power: 100 magic power doubles it (Tommy, 2026-11-27: it was her grass damage)
    public float BurgeonArmorBase     => (RData?.burgeonBaseArmor ?? 16f) + (RData?.path2BurgeonArmorPerLevel ?? 8f) * effectivePath2Level;
    public float BurgeonArmorMP       => BurgeonArmorBase * magicPower / 100f;
    public float BurgeonArmor         => BurgeonArmorBase + BurgeonArmorMP;

    public float RevivalBaseHeal     => RData?.revivalBaseHeal     ?? 100f;
    public float RevivalHealPerLevel => RData?.revivalHealPerLevel ?? 20f;
    public float RevivalHealFlat     => RevivalBaseHeal + RevivalHealPerLevel * effectivePath3Level;
    public float RevivalMPHeal       => (RData?.skillHealMPScaling ?? 0.5f) * magicPower;
    // the revival heal past the plant's max health becomes Verdant Guard for this long (Tommy, 2026-11-27)
    public float VerdantGuardDuration => RData?.verdantGuardDuration ?? 20f;
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
        UpdateDeadPlantGlitter();
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
        _rayBegun = false;
        if (_healRay != null) _healRay.Hide();
        _switchTo = null;
        ClearDeadPlantGlitter();
    }

    // sparkles rising over every tile where a plant has fallen, while she's selected (Tommy, 2026-11-27): the plants
    // her skill can bring back (DeadPlantGlitter)
    private readonly Dictionary<Tile, DeadPlantGlitter> _deadGlitter = new Dictionary<Tile, DeadPlantGlitter>();
    private readonly List<Tile> _glitterGone = new List<Tile>();
    private float _deadGlitterCheck;

    private void UpdateDeadPlantGlitter()
    {
        if (!IsSelected || !IsAlive)
        {
            if (_deadGlitter.Count > 0) ClearDeadPlantGlitter();
            return;
        }
        _deadGlitterCheck -= Time.unscaledDeltaTime;
        if (_deadGlitterCheck > 0f) return;
        _deadGlitterCheck = 0.25f;
        foreach (var kvp in Tile.allTiles)
        {
            Tile t = kvp.Value;
            if (t == null || !t.HasDeadPlant || _deadGlitter.ContainsKey(t)) continue;
            _deadGlitter[t] = DeadPlantGlitter.Begin(t.transform.position, Glint);
        }
        // a tile whose plant came back (or whose record went) stops
        _glitterGone.Clear();
        foreach (var kvp in _deadGlitter)
            if (kvp.Key == null || !kvp.Key.HasDeadPlant) _glitterGone.Add(kvp.Key);
        foreach (Tile t in _glitterGone)
        {
            if (_deadGlitter[t] != null) _deadGlitter[t].Stop();
            _deadGlitter.Remove(t);
        }
    }

    private void ClearDeadPlantGlitter()
    {
        foreach (DeadPlantGlitter g in _deadGlitter.Values) if (g != null) g.Stop();
        _deadGlitter.Clear();
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

    // healing she gets from her attack fills the sap by the conversion (Tommy, 2026-11-27: only her attacks, her own
    // Rejuvenating Burgeon no longer does): the heal she'd get (with her heal bonuses and the healing she receives),
    // counted in full even when she's already at full health
    public void AddSap(float healing)
    {
        if (healing <= 0f) return;
        float wouldHeal = healing * (1f + healingReceived) * (1f + healingBonus);
        _storedSap = Mathf.Min(SapCap, _storedSap + wouldHeal * SapConversion);
    }

    // her skill level takes a lot off the cooldown (and growth mulches and the skill tree add more
    // levels), so it never goes under this (Tommy, 2026-11-22)
    public const float MinSkillCooldown = 10f;

    public override void UpdateStats()
    {
        base.UpdateStats();
        skillCooldown = Mathf.Max(MinSkillCooldown, skillCooldown);

        // Attack path max level bonus (Tommy): her healing also benefits from her grass damage (the heals and shields she gives)
        if (IsPath1Maxed) healingBonus += grassDamage * GrassConversion;
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

    // the flick lands: instant grass damage on the insect it was for (or, if that one died, left her range or
    // went out of her sight while she raised the wand, whoever her targeting picks now). the drained life flying
    // back to her is that hit's on hit effect (OnAttackHit)
    private void ReleaseFlick()
    {
        Insect insect = _castTarget;
        _castTarget = null;
        if (!IsAlive || _channelTarget != null) return;
        if (insect == null || !insect.IsAlive || !CanReachInsect(insect))
        {
            GameObject again = base.FindTarget();
            insect = again != null ? again.GetComponent<Insect>() : null;
        }
        if (insect == null || !insect.IsAlive) return;
        _attackLandingOn = insect;
        try
        {
            insect.Damage(attackDamage, damageType, elementalType, this, true,
                new DamageTag[] { DamageTag.Attack, DamageTag.SingleTarget });
        }
        finally { _attackLandingOn = null; }
        if (data != null) SfxPlayer.Play(data.attackSound, GemWorldPosition());
    }

    // her attack's on hit effect (Tommy, 2026-11-27), from Entity's on hit dispatcher: the drained life comes out of
    // the insect she hit, Tommy's orb arcing to the tip of her staff (RhodiolaDrainOrb). it carries a share of what the
    // hit really did, after the insect's armor, resistances and any crit. only her attack sends one, a hit that kills
    // still does, and an Echo Seed makes the same attack send a second (RepeatOnHitEffects)
    private Insect _attackLandingOn;
    private float _lastDrainHeal;

    public void OnAttackHit(Insect insect, float damageDealt)
    {
        if (insect == null || !ReferenceEquals(insect, _attackLandingOn)) return;
        _attackLandingOn = null;
        _lastDrainHeal = Mathf.Max(0f, damageDealt) * DrainHealShare;
        LaunchDrain(insect.GetAimPoint(), _lastDrainHeal);
    }

    // an Echo Seed repeating her attack: a second orb from where the insect was, carrying the same heal
    public override void RepeatOnHitEffects(Insect target, Vector3 groundPoint, Vector3 aimPoint) =>
        LaunchDrain(aimPoint, _lastDrainHeal);

    private void LaunchDrain(Vector3 from, float heal)
    {
        if (!IsAlive) return;
        RhodiolaDrainOrb.Launch(this, from, DrainMoteSpeed, RData?.drainArcPerUnit ?? 0.3f, RData?.drainArcMin ?? 0.4f,
            lifeMotePrefab, () => OnDrainReturned(heal));
    }

    // the orb reaches her staff: all at that moment, and all of it even when she's at full health, she's healed,
    // her sap fills by the heal she'd get, and she gains Rejuvenating Burgeon
    private void OnDrainReturned(float heal)
    {
        if (!IsAlive) return;
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
    // begun on a plant with her golden sap full: insects coming into range don't stop it (Tommy, 2026-11-27)
    private bool _channelOnFullSap;
    private bool SapFull => SapCap > 0f && _storedSap >= SapCap - 0.01f;
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
                // the heal ray starts at her staff just early enough for its beam to reach the ally as the first heal goes out
                if (!_rayBegun && Time.time >= _channelStartedAt + HealStartSeconds - RhodiolaHealRay.LeadSeconds)
                {
                    _rayBegun = true;
                    if (HealRay != null) HealRay.Begin(_channelTarget);
                }
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
        // another plant just dropped under 25%: she stops (steps forward) and starts over on it (steps back), the whole
        // animation every time (Tommy, 2026-11-27)
        Plant fell = NewlyCritical(ally);
        if (fell != null) { _switchTo = fell; return false; }
        bool isPlant = ally is Plant;
        if (!_channelUrgent && (!isPlant || HealthShare(ally) >= LowHealthThreshold))
        {
            if (!_channelOnFullSap && InsectInRange()) return false;
            Plant low = LowestPlant(true);
            if (low != null && low != ally) return false;
            if (!isPlant && LowestPlant(false) != null) return false;
        }
        return true;
    }

    private void TryStartChannel()
    {
        // kept up to date while she isn't healing too, so only a drop during a heal counts as one
        NewlyCritical(null);
        if (IsStunned || Time.time < _channelRecoverUntil || _storedSap < SapCap * SapToChannel) return;
        if (_switchTo != null)
        {
            Plant next = _switchTo;
            _switchTo = null;
            if (Alive(next) && Hurt(next) && InRange(next) && CanSee(next.transform.position)) { StartChannel(next, true); return; }
        }
        Plant low = LowestPlant(true);
        if (low != null) { StartChannel(low, true); return; }
        // with her golden sap full she heals any hurt plant in her range, insects about or not: healing is what
        // she's for (Tommy, 2026-11-27). she goes on until that plant is full or her sap runs out (still handing
        // over to a plant that drops under the line). hurt friendly insects still wait for no insects in range
        bool insectsNear = InsectInRange();
        if (insectsNear && !SapFull) return;
        Entity hurt = LowestPlant(false);
        if (hurt == null && !insectsNear) hurt = LowestFriendlyInsect();
        if (hurt != null) StartChannel(hurt, false, hurt is Plant && SapFull);
    }

    // a plant under this share of its health that wasn't at the last look makes her switch to it mid heal
    private const float CriticalHealthShare = 0.25f;
    private readonly HashSet<Plant> _critical = new HashSet<Plant>();
    private readonly List<Plant> _criticalNow = new List<Plant>();
    private Plant _switchTo;

    // the plants under 25% now; returns one in her range and sight, other than ally, that wasn't at the last look
    private Plant NewlyCritical(Entity ally)
    {
        Plant fell = null;
        _criticalNow.Clear();
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || plant == this || !plant.IsAlive || plant.maxHealth <= 0f) continue;
            if (HealthShare(plant) >= CriticalHealthShare) continue;
            _criticalNow.Add(plant);
            if (fell == null && !ReferenceEquals(plant, ally) && !_critical.Contains(plant) && InRange(plant) && CanSee(plant.transform.position))
                fell = plant;
        }
        _critical.Clear();
        foreach (Plant p in _criticalNow) _critical.Add(p);
        return fell;
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

    private void StartChannel(Entity ally, bool urgent, bool onFullSap = false)
    {
        CancelFlick();
        _channelTarget = ally;
        _rayBegun = false;
        _channelUrgent = urgent;
        _channelOnFullSap = onFullSap;
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
        if (_rayBegun && _healRay != null) _healRay.Stop();
        _rayBegun = false;
        _channelTarget = null;
        _channelCheckTimer = 0f;
    }

    // her heal ray (RhodiolaHealRay, Tommy 2026-11-27): a spiral at the point of the crystal on her staff and a beam
    // from there to the ally, while she channels. made the first time she needs it
    private RhodiolaHealRay _healRay;
    private bool _rayBegun;
    private RhodiolaHealRay HealRay
    {
        get
        {
            if (_healRay == null) _healRay = RhodiolaHealRay.Create(this, GetMainRenderer(), GemPointWorldPosition);
            return _healRay;
        }
    }

    // one heal of the channel, never more than the ally is missing, spending the heal divided by her healing to sap
    // depletion ratio (SapEfficiency). while she has Rejuvenating Burgeon herself, it passes the burgeon on. the heal
    // ray shows it (no motes any more)
    private void ChannelTick(float interval)
    {
        Entity ally = _channelTarget;
        float efficiency = Mathf.Max(0.01f, SapEfficiency);
        float amount = Mathf.Min(_storedSap * efficiency, ChannelHealPerSecond * interval, ally.maxHealth - ally.health);
        if (amount <= 0f) { StopChannel(); return; }
        _storedSap = Mathf.Max(0f, _storedSap - amount / efficiency);
        FaceAttackTarget(ally.gameObject);
        ally.Heal(amount, this);
        if (HasEffect<RejuvenatingBurgeonEffect>()) ApplyBurgeon(ally);
    }

    // where the crystal at the tip of her staff is right now: the tip in the frame she's showing
    // (RhodiolaStaffTips, read off every frame of her idle, attack and heal), or gemPixel for a frame not in it,
    // through her sprite's pivot and pixels per unit, mirrored when she faces right (flipX). her drain's orb
    // lands on it; her heal ray leaves from the crystal's point (GemPointWorldPosition)
    [Tooltip("the crystal's middle for a frame RhodiolaStaffTips doesn't know, art px from the frame's top left (her unflipped art faces left)")]
    [SerializeField] private Vector2 gemPixel = new Vector2(20f, 6f);

    public Vector3 StaffTipPosition => GemWorldPosition();
    public SpriteRenderer BodyRenderer => GetMainRenderer();

    private Vector3 GemWorldPosition() => StaffArtPoint(false);
    // the crystal's point, its far end along the staff (RhodiolaStaffTips.TryGetPoint), or its middle for a frame not in it
    private Vector3 GemPointWorldPosition() => StaffArtPoint(true);

    private Vector3 StaffArtPoint(bool crystalPoint)
    {
        SpriteRenderer body = GetMainRenderer();
        if (body == null) return transform.position;
        Sprite sprite = body.sprite;
        float ppu = sprite != null ? sprite.pixelsPerUnit : 64f;
        Vector2 pivot = sprite != null ? sprite.pivot : new Vector2(32f, 32f);
        float height = sprite != null ? sprite.rect.height : 64f;
        Vector2 tip = gemPixel;
        if (sprite != null)
        {
            if (crystalPoint && RhodiolaStaffTips.TryGetPoint(sprite.name, out Vector2 point)) tip = point;
            else if (RhodiolaStaffTips.TryGet(sprite.name, out Vector2 known)) tip = known;
        }
        Vector2 local = new Vector2((tip.x - pivot.x) / ppu, (height - tip.y - pivot.y) / ppu);
        if (body.flipX) local.x = -local.x;
        return body.transform.TransformPoint(local);
    }

    // ---------- skill: revival (unchanged) ----------

    public override void OnPath1Upgrade(int level)
    {
        // attack damage and attack range (back as her old path had it, Tommy 2026-11-27: no attack speed); its third
        // upgrade, the channel's healing to sap ratio, is SapEfficiency (the grass damage per level is gone, Tommy)
        baseAttackDamage    = data.baseAttackDamage + level * (RData?.path1AttackDamagePerLevel ?? 2f);
        baseAttackRange     = data.baseAttackRange  + level * (RData?.path1AttackRangePerLevel  ?? 0.3f);
        baseGrassDamage     = data.baseGrassDamage;
        baseAttackSpeed     = data.baseAttackSpeed;
        baseProjectileSpeed = data.baseProjectileSpeed;
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillCooldown = Mathf.Max(MinSkillCooldown, data.baseSkillCooldown - level * (RData?.path3CooldownReductionPerLevel ?? 10f));
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        bool anyDead = false;
        foreach (var kvp in Tile.allTiles)
            if (kvp.Value.HasDeadPlant) { anyDead = true; break; }
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
        RhodiolaReviveBeam.Play(tile.transform.position, Glint);
        HealRevived(revived);

        if (SkillTreeManager.HasUnlock(this, RevivalAoeUnlock))
            ReviveNeighbors(tile);

        if (SkillTreeManager.HasUnlock(this, RevivalEmpowerUnlock))
            revived.ApplyEffect(new VerdantEmpowermentEffect(revived, 8f, 1, this));

    }

    // her skill's heal on a plant it brought back (Tommy, 2026-11-27): past the plant's max health it becomes Verdant Guard
    // (OnHealOverflow), and with the skill at max level it fills her golden sap as her attack's heal does
    private Plant _revivalHealing;

    private void HealRevived(Plant revived)
    {
        _revivalHealing = revived;
        revived.Heal(RevivalHeal, this);
        _revivalHealing = null;
        if (IsPath3Maxed) AddSap(RevivalHeal);
    }

    public override void OnHealOverflow(Entity healed, float overflow)
    {
        if (healed == null || overflow <= 0f || !ReferenceEquals(healed, _revivalHealing)) return;
        // a shield grows with her heals bonus and the plant's healing received itself, and the overheal already has both
        float scale = (1f + healed.healingReceived) * (1f + healingBonus);
        healed.ApplyEffect(new VerdantGuardEffect(healed, VerdantGuardDuration, this, overflow / Mathf.Max(0.01f, scale)));
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
            if (!neighbor.HasDeadPlant) continue;
            Plant revivedNeighbor = Plant.RevivePlant(neighbor, true);
            if (revivedNeighbor == null) continue;
            RhodiolaReviveBeam.Play(neighbor.transform.position, Glint);
            HealRevived(revivedNeighbor);
        }
    }

    // ---------- descriptions ----------

    public override string GetName() => TitleName("green");

    public override string GetDescription() =>
        $"The {GetName()} drains life from insects and pours it into her allies.";

    private string GoldenSapLabel => "<color=#FFD700><b>Golden Sap</b></color>";
    private string BurgeonLabel => "<color=green><b>Rejuvenating Burgeon</b></color>";

    // Tommy's tooltips (2026-11-27)
    public override string GetAttackDescription() =>
        $"Absorbs the life essence of an insect, dealing <color=green><b>{attackDamage:F0}</b></color> {PlantData.DamageTypeLabel(damageType)}, and heals herself for <color=green><b>{DrainHealShare * 100f:F0}%</b></color> of the damage dealt.\n\n" +
        $"As long as the {GetName()} has at least <color=#FFD700><b>{SapCap * SapToChannel:F0}</b></color> {GoldenSapLabel} available, she may interrupt her attacks to heal a nearby plant below <color=green><b>{LowHealthThreshold * 100f:F0}%</b></color> <color=red><b>Health</b></color>. When her {GoldenSapLabel} is full, she also heals nearby plants below their <color=red><b>Max Health</b></color>, even with insects around.\n\n" +
        $"While targeting a plant for healing, she spends her {GoldenSapLabel} to heal a plant for <color=green><b>{ChannelHealPerSecond - ChannelHealMP:F0}</b></color> [<color=#FFB6C1><b>+{ChannelHealMP:F0}</b></color>] health per second. If another plant falls below <color=green><b>{CriticalHealthShare * 100f:F0}%</b></color> <color=red><b>Health</b></color>, she switches to it.";

    public override string GetPassiveDescription() =>
        $"Whenever the {GetName()} would get healed from her attacks, she fills up her {GoldenSapLabel} by <color=#FFD700><b>{SapConversion * 100f:F0}%</b></color> of the healing, and grants herself {BurgeonLabel}, regenerating <color=green><b>{BurgeonHealPerSecond:F0}</b></color> health per second for <color=green><b>{BurgeonDuration:F0}</b></color> seconds and granting <color=#00CED1><b>+{BurgeonArmorBase:F0}</b></color> [<color=#FFB6C1><b>+{BurgeonArmorMP:F0}</b></color>] <color=#00CED1><b>Armor</b></color>.\n\n" +
        $"If the {GetName()} heals a plant while she is blessed by {BurgeonLabel}, she applies the buff at equal strength onto that plant.";

    public override string GetSkillDesription() =>
        $"Target a tile where a plant has fallen to resurrect it, with the mulches it had. The plant is then healed for <color=green><b>{RevivalHealFlat:F0}</b></color> [<color=#FFB6C1><b>+{RevivalMPHeal:F0}</b></color>] Health.\n\n" +
        $"Healing the dead plant past its <color=red><b>Max Health</b></color> overheals it, granting <color=green><b>Verdant Guard</b></color> equal to the amount of overheal, for <color=green><b>{VerdantGuardDuration:F0}</b></color> seconds.";

    public override string GetPath1Name() => "Verdance";
    public override string GetPath2Name() => "Symbiosis";
    public override string GetPath3Name() => "Revival";

    public override string GetPath1Description(bool details = false)
    {
        float dmgpl   = RData?.path1AttackDamagePerLevel  ?? 2f;
        float rangepl = RData?.path1AttackRangePerLevel   ?? 0.3f;
        float effpl   = RData?.path1SapEfficiencyPerLevel ?? 0.1f;
        string desc = details
            ? $"Absorbs the life essence of an insect, dealing <color=green><b>[100% Attack Damage]</b></color> {PlantData.DamageTypeLabel(damageType)}, and heals herself for <color=green><b>[{DrainHealShare * 100f:F0}% of the damage dealt]</b></color>.\n\n" +
              $"As long as the {GetName()} has at least <color=#FFD700><b>{SapToChannel * 100f:F0}%</b></color> {GoldenSapLabel} available, she may interrupt her attacks to heal a nearby plant below <color=green><b>{LowHealthThreshold * 100f:F0}%</b></color> <color=red><b>Health</b></color>. When her {GoldenSapLabel} is full, she also heals nearby plants below their <color=red><b>Max Health</b></color>, even with insects around.\n\n" +
              $"While targeting a plant for healing, she spends her {GoldenSapLabel} to heal a plant for <color=green><b>[{RData?.channelHealPerSecond ?? 40f:F0}]</b></color> [<color=#FFB6C1><b>+{(RData?.channelHealMPScaling ?? 0.05f) * 100f:F0}% Magic Power</b></color>] health per second. If another plant falls below <color=green><b>{CriticalHealthShare * 100f:F0}%</b></color> <color=red><b>Health</b></color>, she switches to it."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n{UpgradesBreak}" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{dmgpl:F0}</b></color> per level. [<color=green><b>+{dmgpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rangepl:F1}</b></color> per level. [<color=green><b>+{rangepl * effectivePath1Level:F1}</b></color>]\n\n" +
               $"Increase the healing to {GoldenSapLabel} depletion ratio by <color=#FFD700><b>{effpl * 100f:F0}%</b></color> per level. [<color=#FFD700><b>+{effpl * effectivePath1Level * 100f:F0}%</b></color>]\n\n" +
               $"{Level5Section(path1Level, "<color=green><b>Healing</b></color> also benefits from <color=green><b>Grass Damage</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float cappl   = RData?.path2GoldenSapCapPerLevel  ?? 20f;
        float convpl  = RData?.path2SapConversionPerLevel ?? 0.1f;
        float burgpl  = RData?.path2BurgeonHealPerLevel   ?? 2f;
        float armorpl = RData?.path2BurgeonArmorPerLevel  ?? 8f;
        string desc = details
            ? $"Whenever the {GetName()} would get healed from her attacks, she fills up her {GoldenSapLabel} by <color=#FFD700><b>[({(RData?.sapConversion ?? 1f) * 100f:F0}%) + ({convpl * 100f:F0}%/Lvl.)]</b></color> of the healing, and grants herself {BurgeonLabel}, regenerating <color=green><b>[({RData?.burgeonHealPerSecond ?? 12f:F0}) + ({burgpl:F0}/Lvl.)]</b></color> health per second for <color=green><b>{BurgeonDuration:F0}</b></color> seconds and granting <color=#00CED1><b>[({RData?.burgeonBaseArmor ?? 16f:F0}) + ({armorpl:F0}/Lvl.)] Armor</b></color>, multiplied by <color=#FFB6C1><b>(100% + 1% per Magic Power)</b></color>.\n\n" +
              $"If the {GetName()} heals a plant while she is blessed by {BurgeonLabel}, she applies the buff at equal strength onto that plant."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n{UpgradesBreak}" +
               $"Increase {GoldenSapLabel} capacity by <color=#FFD700><b>{cappl:F0}</b></color> per level. [<color=#FFD700><b>+{cappl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Attack</b></color> to {GoldenSapLabel} conversion ratio by <color=#FFD700><b>{convpl * 100f:F0}%</b></color> per level. [<color=#FFD700><b>+{convpl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase {BurgeonLabel} healing by <color=green><b>{burgpl:F0}</b></color> per second per level. [<color=green><b>+{burgpl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"Increase {BurgeonLabel} armor by <color=#00CED1><b>{armorpl:F0}</b></color> per level. [<color=#00CED1><b>+{armorpl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"{Level5Section(path2Level, $"{BurgeonLabel} applied onto plants including herself also fills up {GoldenSapLabel} at the same rate (<color=#FFD700><b>{SapConversion * 100f:F0}%</b></color>).")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float cdrpl = RData?.path3CooldownReductionPerLevel ?? 10f;
        string desc = details
            ? $"Target a tile where a plant has fallen to resurrect it, with the mulches it had. The plant is then healed for <color=green><b>[({RevivalBaseHeal:F0}) + ({RevivalHealPerLevel:F0}/Lvl.) + <color=#FFB6C1><b>{(RData?.skillHealMPScaling ?? 0.30f) * 100f:F0}% Magic Power</b></color>]</b></color> Health.\n\n" +
              $"Healing the dead plant past its <color=red><b>Max Health</b></color> overheals it, granting <color=green><b>Verdant Guard</b></color> equal to the amount of overheal, for <color=green><b>{VerdantGuardDuration:F0}</b></color> seconds."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n{UpgradesBreak}" +
               $"Increase <color=green><b>Revival Heal</b></color> by <color=green><b>{RevivalHealPerLevel:F0}</b></color> per level. [<color=green><b>+{RevivalHealPerLevel * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Reduce <color=green><b>Base Skill Cooldown</b></color> by <color=green><b>{Mathf.RoundToInt(cdrpl)}s</b></color> per level, never below <color=green><b>{MinSkillCooldown:F0}s</b></color>. [<color=green><b>-{Mathf.RoundToInt(cdrpl * effectivePath3Level)}s</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, $"The revival heal also fills up her {GoldenSapLabel} at the same rate as her attack's heal (<color=#FFD700><b>{SapConversion * 100f:F0}%</b></color>).")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
