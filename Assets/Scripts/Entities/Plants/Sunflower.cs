using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class Sunflower : Shooter
{
    public float channelDuration = 1f;
    public int sunGenerated;
    public float skillAoERadius, sunrayDamagePerSecond;
    public float cooldownReductionPerHit;
    [SerializeField] private GameObject sunrayPrefab;

    [Header("Body")]
    // the Animator on the Visual child (her actual sprite) - separate from circleAnimator,
    // which only drives the charge-up orb. "Attack"/"Skill" triggers switch her body pose;
    // both transitions back to Idle via Has Exit Time, so nothing here needs to reset them
    [SerializeField] private Animator bodyAnimator;
    // the Attack clip's own authored Length in seconds, as shown in the Animation window - set
    // this to match whatever that value actually is, same convention as circleClipLength, so the
    // body's pose is stretched or compressed to fit exactly inside AttackChargeTime instead of
    // drifting out of sync with it as attack speed changes
    [SerializeField] private float bodyAttackClipLength = 0.5f;
    // same idea, but stretched/compressed to fit the skill's channel time instead (see
    // OnTargetConfirmed) - set to the SunflowerSkill clip's own authored Length
    [SerializeField] private float bodySkillClipLength = 0.5f;
    private const string BodyAttackTrigger = "Attack";
    private const string BodySkillTrigger = "Skill";

    // Animator.speed is a single global playback multiplier shared by every state on this
    // Animator, not per-state - so once Attack/Skill scales it to fit their own duration, Idle
    // would otherwise keep running at that same leftover speed forever after. this timer resets
    // it back to 1 exactly when the clip would have finished, and gets replaced (not stacked)
    // whenever a new body animation starts - e.g. Skill interrupting a still-charging Attack
    private Coroutine _bodySpeedResetRoutine;

    private void PlayBodyAnimation(string trigger, float clipLength, float targetDuration)
    {
        if (bodyAnimator == null) return;
        if (_bodySpeedResetRoutine != null) StopCoroutine(_bodySpeedResetRoutine);

        bodyAnimator.speed = targetDuration > 0f && clipLength > 0f
            ? Mathf.Max(0.01f, clipLength / targetDuration) : 1f;
        bodyAnimator.SetTrigger(trigger);
        _bodySpeedResetRoutine = StartCoroutine(ResetBodySpeedAfter(targetDuration));
    }

    private IEnumerator ResetBodySpeedAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (bodyAnimator != null) bodyAnimator.speed = 1f;
        _bodySpeedResetRoutine = null;
    }

    [Header("Grimoire (cosmetic)")]
    // the floating book - purely cosmetic, no gameplay effect. its unflipped art faces left, same
    // convention Plant.UpdateFacingFlip already uses for the body itself, so its own flip state
    // just mirrors whichever side of her it's currently sitting on
    [SerializeField] private Transform bookTransform;
    [SerializeField] private SpriteRenderer bookSpriteRenderer;
    [SerializeField] private float bookBobAmplitude = 0.05f;
    [SerializeField] private float bookBobSpeed = 2f;
    // how quickly the book catches up to its target side when she flips facing - a flat Lerp
    // factor per second, never an instant snap
    [SerializeField] private float bookXLerpSpeed = 4f;
    // the book's Animator has no idle state/clip at all - it normally just sits there as
    // whatever static sprite is on bookSpriteRenderer. the Animator component itself stays
    // disabled except for the moment it's actually playing the generate clip, so there's never a
    // risk of it idling mid-animation - see PlayBookGenerateAnimation
    [SerializeField] private Animator bookAnimator;
    // the clip's own authored Length in seconds, as shown in the Animation window - same
    // convention as circleClipLength/bodyAttackClipLength, set this once to match that real
    // value and leave it alone
    [SerializeField] private float bookGenerateAuthoredClipLength = 0.5f;
    // how long the generate animation should actually take to play out - freely tunable to
    // taste, independent of the clip's own real length. the Animator's speed is scaled so the
    // authored clip stretches or compresses to fit exactly this duration
    [SerializeField] private float bookGenerateClipLength = 0.5f;
    private Coroutine _bookAnimatorDisableRoutine;

    // tints from white (passive cooldown just reset) to this color (passive fully charged,
    // about to generate) as GetPassiveBarFill() rises - and back down the same way when the
    // timer resets, rather than snapping straight back to white the instant it does
    [SerializeField] private Color bookGeneratingColor = Color.yellow;
    [SerializeField] private float bookColorLerpSpeed = 3f;
    // real texture-brightness, not a scene light: this project's URP Volume (see
    // DefaultVolumeProfile.asset) already has Bloom active, so pushing the sprite's color channels
    // past 1.0 (HDR) genuinely overexposes and blooms, same as dragging a Brightness slider past
    // 100% in an art program - no separate light/shader needed, just the raw color value
    [SerializeField] private float bookGlowIntensity = 2f;
    // smoothed 0-1 "how charged" value, drives the sprite tint
    private float _bookCharge;

    private const float BookBaseHeight = -0.1f;
    private const float BookBaseXLeft = -0.35f;
    private const float BookBaseXRight = 0.35f;

    // called the instant the passive actually fires (see Update) - plays the one generate clip
    // once, then disables the Animator again so the book goes back to its plain static sprite
    // instead of holding on the clip's last frame
    private void PlayBookGenerateAnimation()
    {
        if (bookAnimator == null) return;
        if (_bookAnimatorDisableRoutine != null) StopCoroutine(_bookAnimatorDisableRoutine);

        bookAnimator.enabled = true;
        bookAnimator.speed = bookGenerateClipLength > 0f && bookGenerateAuthoredClipLength > 0f
            ? Mathf.Max(0.01f, bookGenerateAuthoredClipLength / bookGenerateClipLength) : 1f;
        bookAnimator.Play(0, 0, 0f);
        _bookAnimatorDisableRoutine = StartCoroutine(DisableBookAnimatorAfter(bookGenerateClipLength));
    }

    private IEnumerator DisableBookAnimatorAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (bookAnimator != null) bookAnimator.enabled = false;
        _bookAnimatorDisableRoutine = null;
    }

    private void UpdateBook()
    {
        if (bookTransform == null) return;

        SpriteRenderer mainRenderer = GetMainRenderer();
        bool facingRight = mainRenderer != null && mainRenderer.flipX;
        float targetX = facingRight ? BookBaseXRight : BookBaseXLeft;

        Vector3 local = bookTransform.localPosition;
        float newX = Mathf.Lerp(local.x, targetX, bookXLerpSpeed * Time.deltaTime);
        float bobY = BookBaseHeight + Mathf.Sin(Time.time * bookBobSpeed) * bookBobAmplitude;
        bookTransform.localPosition = new Vector3(newX, bobY, local.z);

        if (bookSpriteRenderer != null) bookSpriteRenderer.flipX = newX > 0f;
    }

    // GetPassiveBarFill() (Plant.cs) is already a smooth, continuously-rising ratio while the
    // timer counts down normally - the one moment it ISN'T smooth is the instant the passive
    // actually fires and passiveCooldownTimer hard-resets to full, snapping fill from 1 back to
    // 0 in a single frame. lerping the displayed color toward the fill-based target (rather than
    // just setting it directly) smooths over exactly that reset too, not just the rise
    // the glow only actually starts climbing once GetPassiveBarFill() reaches this fraction of
    // the way to full - fully charged still corresponds to fill = 1
    private const float BookGlowStartFill = 0.75f;

    private void UpdateBookColor()
    {
        if (bookSpriteRenderer == null) return;

        // remaps fill so 0..0.75 all reads as "not glowing yet" (0) and 0.75..1 stretches across
        // the full glow ramp (0 to 1) - InverseLerp already clamps, so fill below the threshold
        // (including right after a reset) correctly lands on 0, not a negative value
        float glowTarget = Mathf.InverseLerp(BookGlowStartFill, 1f, GetPassiveBarFill());
        _bookCharge = Mathf.Lerp(_bookCharge, glowTarget, bookColorLerpSpeed * Time.deltaTime);

        // white itself is left un-boosted (1,1,1,1) at zero charge - only the lerp TOWARD the
        // glow color pushes past 1.0, so the brightness ramp starts normal and overexposes more
        // as she gets closer to actually generating
        Color boostedGlow = bookGeneratingColor * bookGlowIntensity;
        boostedGlow.a = 1f;
        bookSpriteRenderer.color = Color.Lerp(Color.white, boostedGlow, _bookCharge);
    }

    [Header("Attack Charge Circle")]
    [SerializeField] private GameObject attackCircle;
    [SerializeField] private Animator circleAnimator;
    // the circle clip's own authored Length in seconds, as shown in the Animation window - set
    // this to match whatever that value actually is, so the whole clip is stretched or
    // compressed to fit exactly inside AttackChargeTime (see Plant.BeginAnimatedAttackCharge).
    // shared by the single circle above and the three max-level circles below, since they're all
    // built from the same animation
    [SerializeField] private float circleClipLength = 0.5f;

    [Header("Max Level Side Circles")]
    // attackCircle above doubles as the middle circle once Path1 is maxed - left and right are
    // two more pre-placed child objects (same SpriteRenderer + Animator setup), positioned by
    // hand in the Editor. firing order is always left, then middle, then right
    [SerializeField] private GameObject leftCircle;
    [SerializeField] private GameObject rightCircle;

    private const string AttackChargeTrigger = "AttackCharge";

    private SunflowerData SFData => data as SunflowerData;
    private float SunMarkDuration => passiveDuration;
    private float SunGenInterval  => passiveCooldown * (1f + sunGenerationCooldown);

    // skill tree node unlock ids
    public const string AmbientSunProcUnlock = "sunflower_ambient_sun_proc";
    public const string SunSkillCooldownUnlock = "sunflower_sun_skillcd";
    public const string ExtraProjectileUnlock = "sunflower_extra_projectile";
    public const string HomingSunrayUnlock    = "sunflower_homing_sunray";
    public const string SunrayRefreshUnlock   = "sunflower_sunray_refresh";
    public const string InstantSkillUnlock    = "sunflower_instant_skill";

    private const float AmbientSunProcReduction = 0.1f;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        basePassiveDuration  = 6f;
        passiveCooldownTimer = data.basePassiveCooldown;
        Entity.OnEntityHit += OnAnyEntityHit;

        // the Animator defaults to enabled the instant the component exists, which would
        // otherwise auto-play its one state (no idle to sit on) the moment she spawns - force it
        // off here so it only ever plays during PlayBookGenerateAnimation's own window
        if (bookAnimator != null) bookAnimator.enabled = false;

        // LoadData already applied any skill tree path1LevelAdder/path2LevelAdder/
        // path3LevelAdder ("+1 Effective X Point" nodes) and recomputed
        // effectivePath1/2/3Level from them, so re-running these three hooks here bakes that
        // virtual level straight into cooldownReductionPerHit/skillAoERadius/etc.
        OnPath1Upgrade(effectivePath1Level);
        OnPath2Upgrade(effectivePath2Level);
        OnPath3Upgrade(effectivePath3Level);

        // free skill readiness on placement - deliberately bypasses UnlockPath3() (which spends
        // sun and adds to totalSunSpent) so this can't be abused for an inflated uproot refund
        if (SkillTreeManager.HasUnlock(this, InstantSkillUnlock))
        {
            path3Unlocked = true;
            OnPath3Unlock();
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Entity.OnEntityHit -= OnAnyEntityHit;
    }

    private void OnAnyEntityHit(EntityEventData data)
    {
        if (data.source != this) return;
        if (data.elementalType != ElementalType.Fire) return;
        if (!IsPath2Maxed) return;
        if (data.target is Insect insect)
        {
            insect.ApplyEffect(new SunMarkEffect(insect, SunMarkDuration, 1, this));

            // skill tree node 5.2 - keyed off applying Sun Mark itself now, not the passive's own
            // sun-income tick (the two are unrelated systems; "Scorching Mark" is about the mark)
            if (SkillTreeManager.HasUnlock(this, SunSkillCooldownUnlock))
                ReduceSkillCooldown(3f);
        }
    }

    protected override void Update()
    {
        base.Update();

        UpdateBook();
        UpdateBookColor();

        int sunpl = SFData?.path2SunPerLevel ?? 3;
        sunGenerated = (SFData?.baseSunGenerated ?? 10) + sunpl * effectivePath2Level;

        if (passiveCooldownTimer <= 0)
        {
            int generatedTotal = GenerateSun(sunGenerated);
            if (bookTransform != null)
                SunGenerationParticleEffect.Spawn(bookTransform.position, generatedTotal, 0.25f);
            PlayBookGenerateAnimation();
            passiveCooldownTimer += passiveCooldown * (1f + sunGenerationCooldown);
        }
    }

    public override void UpdateStats()
    {
        attackDamageTotalMultiplier = IsPath1Maxed ? 0.5f : 1f;
        base.UpdateStats();
        float dpspl = SFData?.path3SunrayDPSPerLevel ?? 15f;
        sunrayDamagePerSecond = (SFData?.baseSunrayDPS ?? 0f) + dpspl * effectivePath3Level + skillDamageMultiplier * magicPower;
    }

    // triple shot at max Path 1 fires 3 projectiles from a single Shoot() call, so the attack
    // sound is played per projectile (in FireProjectile) instead of the usual once-per-Shoot()
    protected override bool PlaysOwnAttackSound => true;

    // caches whatever FindTarget() actually picked, so the charge sequence below can keep firing
    // at that exact insect later even if it has since walked outside attackRange - re-running
    // FindTarget() at fire time would otherwise silently retarget (or find nothing) the moment the
    // original target left range, breaking the lock
    private Insect _currentTarget;

    // true from the moment an attack commits (Shoot() called) until every bolt it's going to
    // fire actually has (see _pendingAttackShots) - lets a skill cast that comes in mid-attack
    // queue instead of cutting the attack animation off, matching the Animator (Attack can't be
    // interrupted by Skill there either)
    private bool _attackInProgress;
    private int _pendingAttackShots;
    private bool _skillQueued;
    private Vector3 _queuedSkillPosition;

    protected override GameObject FindTarget()
    {
        GameObject target = base.FindTarget();
        _currentTarget = target != null ? target.GetComponent<Insect>() : null;
        return target;
    }

    // the attack is committed to (cooldown reset, FindTarget already resolved) the moment
    // Shoot() is called, same as always - only the actual firing is deferred now, until an
    // Animation Event calls back into OnAttackChargeFireFrame(). below the Path1 max-level bonus,
    // the single attackCircle charges and the one bolt spawns from it; at max level, each of the
    // three circles STARTS its own charge staggered by boltDelay - left starts immediately,
    // middle (attackCircle) starts boltDelay later, right starts boltDelay after that - rather
    // than waiting for the previous one to actually finish firing first. since every circle takes
    // the same AttackChargeTime to charge, this still fires them boltDelay apart in the end, just
    // without any of them sitting frozen on a finished pose waiting for their turn. the target is
    // locked in right here (not re-resolved at fire time), so it keeps firing at the same insect
    // through the whole sequence regardless of range - and since all three charges share the same
    // captured lockedTarget/lastKnownPosition variables, whichever one first notices the target
    // died and finds a replacement updates it for whichever circles haven't fired yet too
    // the body's Attack pose starts the instant Shoot() commits (see PlayBodyAnimation above),
    // but the circle itself only appears this many seconds later - a small beat so the pose
    // visibly leads the charge-up orb instead of both popping in on the same frame
    private const float BallSpawnDelay = 0.1f;

    // safeguard for WaitForBodyReadyThenInvoke below - name of the body Animator's Skill state,
    // as it appears in the Animator window
    private const string BodySkillStateName = "SunflowerSkill";
    // never block the circle indefinitely if the body Animator is ever stuck/misconfigured -
    // this is purely a safety cap, not an expected wait
    private const float BodyReadyMaxExtraWait = 5f;

    protected override void Shoot(Vector3 target)
    {
        // attackCooldownTimer resets the instant an attack commits (see Shooter.Update()), not
        // when it actually finishes - so if one full charge-to-fire cycle ever takes longer than
        // the cooldown between shots (easily true right after a skill channel, where the timer
        // has been quietly building up the whole 1.5s), a second Shoot() can fire before the
        // first one's animation/circle has resolved. without this guard that retriggers the
        // Attack pose on top of the still-playing one instead of just skipping this tick
        if (_attackInProgress) return;

        PlayBodyAnimation(BodyAttackTrigger, bodyAttackClipLength, AttackChargeTime);
        Insect lockedTarget = _currentTarget;

        int shotCount = IsPath1Maxed ? (SkillTreeManager.HasUnlock(this, ExtraProjectileUnlock) ? 4 : 3) : 1;
        _attackInProgress = true;
        _pendingAttackShots = shotCount;

        if (IsPath1Maxed)
        {
            GameObject[] sequence = { leftCircle, attackCircle, rightCircle };
            Vector3 lastKnownPosition = lockedTarget != null ? lockedTarget.GetAimPoint() : transform.position;
            // 0.3s at base Attack Speed, scaling inversely with it - the stagger between each
            // circle's own charge starting, same 1/attackSpeed relationship attackCooldown uses
            float boltDelay = 0.3f * (data.baseAttackSpeed / attackSpeed);

            for (int shotIndex = 0; shotIndex < shotCount; shotIndex++)
            {
                int capturedIndex = shotIndex;
                System.Action startCircle = () =>
                {
                    GameObject circle = sequence.Length > 0 ? sequence[capturedIndex % sequence.Length] : null;
                    if (circle != null) circle.SetActive(true);
                    Animator anim = circle != null ? circle.GetComponent<Animator>() : null;

                    BeginAnimatedAttackCharge(anim, AttackChargeTrigger, circleClipLength, () =>
                    {
                        if (lockedTarget == null || !lockedTarget.IsAlive)
                        {
                            GameObject replacement = FindTarget();
                            Insect replacementInsect = replacement != null ? replacement.GetComponent<Insect>() : null;
                            if (replacementInsect != null) lockedTarget = replacementInsect;
                        }
                        if (lockedTarget != null && lockedTarget.IsAlive)
                            lastKnownPosition = lockedTarget.GetAimPoint();

                        Vector3 spawnPosition = circle != null ? circle.transform.position : transform.position;
                        FireProjectile(lockedTarget, spawnPosition, circle, lastKnownPosition);
                    });
                };

                float startDelay = BallSpawnDelay + capturedIndex * boltDelay;
                StartCoroutine(WaitForBodyReadyThenInvoke(startDelay, startCircle));
            }
        }
        else
        {
            Vector3 lastKnownPosition = lockedTarget != null ? lockedTarget.GetAimPoint() : transform.position;

            StartCoroutine(WaitForBodyReadyThenInvoke(BallSpawnDelay, () =>
            {
                if (attackCircle != null) attackCircle.SetActive(true);
                BeginAnimatedAttackCharge(circleAnimator, AttackChargeTrigger, circleClipLength, () =>
                {
                    if (lockedTarget == null || !lockedTarget.IsAlive)
                    {
                        GameObject replacement = FindTarget();
                        Insect replacementInsect = replacement != null ? replacement.GetComponent<Insect>() : null;
                        if (replacementInsect != null) lockedTarget = replacementInsect;
                    }
                    if (lockedTarget != null && lockedTarget.IsAlive)
                        lastKnownPosition = lockedTarget.GetAimPoint();

                    Vector3 spawnPosition = attackCircle != null ? attackCircle.transform.position : transform.position;
                    FireProjectile(lockedTarget, spawnPosition, attackCircle, lastKnownPosition);
                });
            }));
        }
    }

    // waits delay seconds, then also waits for the body Animator to actually have
    // left the Skill state before running action - the circle otherwise has no idea whether the
    // body is still mid-Skill-animation (its real playtime can run longer than channelDuration if
    // bodySkillClipLength doesn't exactly match the clip's authored Length), so without this a
    // slow Skill animation could let the circle visibly get ahead of the body's own attack pose
    private IEnumerator WaitForBodyReadyThenInvoke(float delay, System.Action action)
    {
        yield return new WaitForSeconds(delay);

        float elapsed = 0f;
        while (bodyAnimator != null && elapsed < BodyReadyMaxExtraWait
               && bodyAnimator.GetCurrentAnimatorStateInfo(0).IsName(BodySkillStateName))
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        action?.Invoke();
    }

    // sourceCircle (if any) is deactivated the instant its bolt actually fires. fires at
    // lockedTarget directly rather than re-resolving FindTarget() here, so it keeps homing on the
    // same insect regardless of range. both the single-shot and max-level sequence pass the
    // target's last known position as a fallback, so a target that died mid-charge still fires
    // there instead of the attack silently doing nothing
    private void FireProjectile(Insect lockedTarget, Vector3 spawnPosition, GameObject sourceCircle = null, Vector3? fallbackPosition = null)
    {
        if (sourceCircle != null) sourceCircle.SetActive(false);

        bool targetAlive = lockedTarget != null && lockedTarget.IsAlive;
        if (!targetAlive && fallbackPosition == null) { NotifyAttackShotFired(); return; }

        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        SunflowerProjectile petal = projectile.GetComponent<SunflowerProjectile>();
        if (petal != null)
        {
            Vector3 aimPoint = targetAlive ? lockedTarget.GetAimPoint() : fallbackPosition.Value;
            if (targetAlive) petal.SetTarget(lockedTarget.gameObject);
            petal.Initialize(aimPoint, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
        }
        if (data != null) SfxPlayer.Play(data.attackSound, spawnPosition);
        NotifyAttackShotFired();
    }

    // counts down the shots committed to back in Shoot() - once the last one has actually fired,
    // the attack is considered fully done, and any skill cast that came in mid-attack (see
    // OnTargetConfirmed) is free to start its channel now instead of staying queued
    private void NotifyAttackShotFired()
    {
        _pendingAttackShots--;
        if (_pendingAttackShots > 0) return;

        _attackInProgress = false;
        if (!_skillQueued) return;
        _skillQueued = false;
        StartSkillChannel(_queuedSkillPosition);
    }

    // deterministically shaves cooldownReductionPerHit seconds off the sun timer per attack hit -
    // used to be a chance to shave a full second; this is the RNG-free, expectation-preserving
    // equivalent
    public void ReduceSunTimer()
    {
        passiveCooldownTimer -= cooldownReductionPerHit;
    }

    // skill tree node 3.1: non-attack Fire damage (the Sunray's DoT ticks) scales by the same
    // rate as ReduceSunTimer, but for a much smaller reduction, since a beam can tick many
    // times a second across several targets
    public void TryReduceSunTimerSmall()
    {
        passiveCooldownTimer -= cooldownReductionPerHit * AmbientSunProcReduction;
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + level * (SFData?.path1AttackDamagePerLevel ?? 5f);
        baseAttackSpeed  = data.baseAttackSpeed  + level * (SFData?.path1AttackSpeedPerLevel  ?? 0.05f);
    }

    public override void OnPath2Upgrade(int level)
    {
        // sun generation interval is fixed now - Path 2 only grows the amount (see Update()),
        // not the cadence
        cooldownReductionPerHit = (SFData?.baseCooldownReductionPerHit ?? 0.35f) + level * (SFData?.path2CooldownReductionPerLevel ?? 0.03f);
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration + (SFData?.path3SkillDurationPerLevel ?? 0.5f) * level;
        skillAoERadius    = data.baseSkillRadius   + (SFData?.path3RadiusPerLevel        ?? 0.2f) * level;
    }

    public override void ActivateSkill()
    {
        SkillTargetingManager.instance.BeginTargeting(skillAoERadius, OnTargetConfirmed);
    }

    // cooldown starts the instant the player casts, even if the channel itself ends up queued
    // below - so re-casting is gated from this exact moment, not from whenever the attack
    // actually finishes and the channel gets to start
    private void OnTargetConfirmed(Vector3 position)
    {
        if (sunrayPrefab == null) return;
        skillCooldownTimer = skillCooldown;

        // matches the Animator (Skill can't interrupt Attack there either) - if an attack is
        // still mid-flight, hold the channel until NotifyAttackShotFired sees it finish
        if (_attackInProgress)
        {
            _skillQueued = true;
            _queuedSkillPosition = position;
            return;
        }

        StartSkillChannel(position);
    }

    private void StartSkillChannel(Vector3 position)
    {
        // matches channelDuration (the flat 1s field actually used by ChannelAndSpawn's wait
        // below) rather than data.channelDuration, which BeginChannel reads separately for the
        // attack lockout - kept in sync manually on the data asset, see BeginChannel
        PlayBodyAnimation(BodySkillTrigger, bodySkillClipLength, channelDuration);
        BeginChannel();   // can't attack until the sunray appears (set channelDuration in data)
        StartCoroutine(ChannelAndSpawn(position));
    }

    // extra beat after the channel itself ends before the sunray actually appears - attack
    // lockout and the skill animation both finish on their own schedule same as before, this
    // just delays the spawn on top of that
    private const float SunraySpawnExtraDelay = 1f;

    private IEnumerator ChannelAndSpawn(Vector3 position)
    {
        yield return new WaitForSeconds(channelDuration);
        yield return new WaitForSeconds(SunraySpawnExtraDelay);

        // sprite scales off its own faster growth rate, not the actual (slower-growing) hit
        // radius - see path3VisualRadiusPerLevel
        float visualRadius = data.baseSkillRadius + (SFData?.path3VisualRadiusPerLevel ?? 0.3f) * effectivePath3Level;
        float visualScale  = data.baseSkillRadius > 0f ? visualRadius / data.baseSkillRadius : 1f;

        StartCoroutine(SpawnSunray(position, visualScale));

        if (IsPath3Maxed)
        {
            List<Insect> valid = new List<Insect>();
            foreach (Insect insect in Insect.allInsects)
            {
                if (insect == null || !insect.IsAlive) continue;
                if (Vector3.Distance(position, insect.transform.position) <= skillAoERadius) continue;
                valid.Add(insect);
            }
            if (valid.Count > 0)
            {
                Insect target = valid[Random.Range(0, valid.Count)];
                StartCoroutine(SpawnSunray(target.transform.position, visualScale));
            }
        }
    }

    // plays the spawn sound sunraySpawnLeadTime seconds before the sunray itself is instantiated -
    // this lead-in is separate from (and not) the skill's own activation sound
    private IEnumerator SpawnSunray(Vector3 spawnPosition, float visualScale)
    {
        SfxPlayer.Play(SFData?.sunraySpawnSound, spawnPosition);
        yield return new WaitForSeconds(SFData?.sunraySpawnLeadTime ?? 0.1f);

        GameObject obj = Instantiate(sunrayPrefab, spawnPosition, Quaternion.identity);
        Sunray sunray = obj.GetComponent<Sunray>();
        if (sunray != null)
            sunray.Initialize(sunrayDamagePerSecond, skillAoERadius, skillDuration, this, visualScale,
                SFData?.sunrayLoopSound, SFData?.sunrayEndSound, SFData?.sunrayEndLeadTime ?? 0.2f);
    }

    public override string GetName() => "<b><color=orange>Sunflower</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} shoots her targets with sun bolts and generate precious <color=yellow>Sun</color> for the garden.";

    public override string GetAttackDescription() =>
        $"Briefly charges up a solar-powered energy orb then shoots it towards her target, dealing <color={PlantData.ElementalColor(elementalType)}><b>{attackDamage:F0}</b></color> {PlantData.DamageTypeLabel(damageType)}.";

    public override string GetSkillDesription()
    {
        float dpspl = SFData?.path3SunrayDPSPerLevel ?? 15f;
        return $"Gathers a large burst of energy from the sun, calling down a scorching beam from above that deals <color={PlantData.ElementalColor(elementalType)}><b>{(SFData?.baseSunrayDPS ?? 0f) + dpspl * effectivePath3Level:F0}</b></color> [<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F0}</b></color>] {PlantData.DamageTypeLabel(damageType)} per second to insects within the designated area for <color=green><b>{skillDuration}</b></color> seconds, with <color=green><b>10%</b></color> falloff per insect hit at once down to a <color=green><b>50%</b></color> minimum.";
    }

    public override string GetPassiveDescription() =>
        $"Passively generates <color=green><b>{sunGenerated}</b></color> <color=yellow>Sun</color> for the garden every <color=green><b>{SunGenInterval:F1}</b></color> seconds. Attacks reduce the cooldown by <color=green><b>{cooldownReductionPerHit:F2}</b></color> seconds.";

    public override string GetPath1Description(bool details = false)
    {
        float adpl = SFData?.path1AttackDamagePerLevel ?? 5f;
        float aspl = SFData?.path1AttackSpeedPerLevel  ?? 0.05f;
        int projectileCount = SkillTreeManager.HasUnlock(this, ExtraProjectileUnlock) ? 4 : 3;
        string desc = details
            ? $"Briefly charges up a solar-powered energy orb then shoots it towards her target, dealing <color={PlantData.ElementalColor(elementalType)}><b>[100% Attack Damage]</b></color> {PlantData.DamageTypeLabel(damageType)}."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Speed</b></color> by <color=green><b>{aspl:F2}</b></color> per level. [<color=green><b>+{aspl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"{Level5Section(path1Level, $"Reduce <color=green><b>Total Attack Damage</b></color> by <color=green><b>50%</b></color>. Attacks now shoot <color=green><b>{projectileCount}</b></color> projectiles to the target.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        int sunpl = SFData?.path2SunPerLevel ?? 3;
        float cdrpl = SFData?.path2CooldownReductionPerLevel ?? 0.03f;
        string passiveMaxBonus = $"Dealing <color=red><b>Fire Damage</b></color> inflicts <color=#FFD700><b>Sun Mark</b></color> for <color=green><b>{SunMarkDuration:F0}s</b></color>, increasing the <color=yellow><b>Sun</b></color> yield of the target by <color=green><b>27%</b></color>.";
        string desc = details
            ? $"Passively generates <color=green><b>[({SFData?.baseSunGenerated ?? 10}) + ({sunpl}/Lvl.)]</b></color> <color=yellow>Sun</color> for the garden every <color=green><b>{data.basePassiveCooldown:F0}</b></color> seconds. Attacks reduce the cooldown by <color=green><b>[({SFData?.baseCooldownReductionPerHit ?? 0.35f:F2}) + ({cdrpl:F2}/Lvl.)]</b></color> seconds."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase Sun Generated by <color=green><b>{sunpl}</b></color> per level. [<color=green><b>+{sunpl * effectivePath2Level}</b></color>]\n\n" +
               $"Increase <color=green><b>Cooldown Reduction per Hit</b></color> by <color=green><b>{cdrpl:F2}s</b></color> per level. [<color=green><b>+{cdrpl * effectivePath2Level:F2}s</b></color>]\n\n" +
               $"{Level5Section(path2Level, passiveMaxBonus)}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float dpspl    = SFData?.path3SunrayDPSPerLevel    ?? 15f;
        float durpl    = SFData?.path3SkillDurationPerLevel ?? 0.5f;
        float radiuspl = SFData?.path3RadiusPerLevel        ?? 0.2f;
        string desc = details
            ? $"Gathers a large burst of energy from the sun, calling down a scorching beam from above that deals <color=green><b>[({SFData?.baseSunrayDPS ?? 0f:F0}) + ({dpspl:F0}/Lvl.) + <color=#FFB6C1>{skillDamageMultiplier * 100f:F0}% Magic Power</color>]</b></color> {PlantData.DamageTypeLabel(damageType)} per second to insects within a <color=green><b>[({data.baseSkillRadius:F2}) + ({radiuspl:F2}/Lvl.)]</b></color> radius for <color=green><b>[({data.baseSkillDuration:F1}) + ({durpl:F1}/Lvl.)]</b></color> seconds, with <color=green><b>10%</b></color> falloff per insect hit at once down to a <color=green><b>50%</b></color> minimum."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase Sunray Damage by <color=green><b>{dpspl:F0}</b></color> per level. [<color=green><b>+{dpspl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase Sunray duration by <color=green><b>{durpl:F1}</b></color> second per level. [<color=green><b>+{durpl * effectivePath3Level:F1}</b></color>]\n\n" +
               $"Increase Sunray radius by <color=green><b>{radiuspl:F2}</b></color> per level. [<color=green><b>+{radiuspl * effectivePath3Level:F2}</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, "Unleashes an additional <color=orange><b>Sunray</b></color> at a random insect's location on the map.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
