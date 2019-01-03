using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using BDArmory.Bullets;
using BDArmory.Core;
using BDArmory.Core.Extension;
using BDArmory.Core.Utils;
using BDArmory.Events;
using BDArmory.FX;
using BDArmory.Misc;
using BDArmory.Targeting;
using BDArmory.UI;
using KSP.UI.Screens;
using UniLinq;
using UnityEngine;

namespace BDArmory.Modules
{
    public class ModuleWeapon : EngageableWeapon, IBDWeapon
    {
        #region Declarations

        public static ObjectPool bulletPool;
        public static ObjectPool shellPool;

        Coroutine startupRoutine;
        Coroutine shutdownRoutine;

        private bool _finalFire;
        public bool FinalFire
        {
            get { return _finalFire; }
            set
            {
                if (value != _finalFire)
                {
                    Dependencies.Get<FireEventService>().PublishFireEvent(this.part.vessel.id, this.part.flightID, this.part.craftID, value);
                }

                _finalFire = value;

            }
        }


        public int rippleIndex = 0;

        // WeaponTypes.Cannon is deprecated.  identical behavior is achieved with WeaponType.Ballistic and bulletInfo.explosive = true.
        public enum WeaponTypes
        {
            Ballistic,
            Cannon,
            Laser
        }
       
        public enum WeaponStates
        {
            Enabled,
            Disabled,
            PoweringUp,
            PoweringDown
        }

        public enum BulletDragTypes
        {
            None,
            AnalyticEstimate,
            NumericalIntegration
        }          
               
        public WeaponStates weaponState = WeaponStates.Disabled;
        
        //animations
        private float fireAnimSpeed = 1;
            //is set when setting up animation so it plays a full animation for each shot (animation speed depends on rate of fire)

        public float bulletBallisticCoefficient;     
        
        public WeaponTypes eWeaponType;
                
        public float heat;
        public bool isOverheated;
		private bool wasFiring;
            //used for knowing when to stop looped audio clip (when you're not shooting, but you were)

        AudioClip reloadCompleteAudioClip;
        AudioClip fireSound;
        AudioClip overheatSound;
        AudioClip chargeSound;
        AudioSource audioSource;
        AudioSource audioSource2;
        AudioLowPassFilter lowpassFilter;

        //AI
        public bool aiControlled = false;
        public bool autoFire;
        public float autoFireLength = 0;
        public float autoFireTimer = 0;

        //used by AI to lead moving targets
        private float targetDistance;
        private Vector3 targetPosition;
        private Vector3 targetVelocity;  // local frame velocity
        private Vector3 targetAcceleration; // local frame velocity
        private Vector3 targetVelocityPrevious; // for acceleration calculation
        private Vector3 relativeVelocity;
        Vector3 finalAimTarget;
        Vector3 lastFinalAimTarget;
        public Vessel legacyTargetVessel;
        bool targetAcquired;

        public Vector3? FiringSolutionVector => finalAimTarget.IsZero() ? (Vector3?)null : (finalAimTarget - fireTransforms[0].position).normalized;

        public bool recentlyFiring //used by guard to know if it should evaid this
        {
            get { return Time.time - timeFired < 1; }
        }
        
        //used to reduce volume of audio if multiple guns are being fired (needs to be improved/changed)
        //private int numberOfGuns = 0;

        //UI gauges(next to staging icon)
        private ProtoStageIconInfo heatGauge;

		//AI will fire gun if target is within this Cos(angle) of barrel
		public float maxAutoFireCosAngle = 0.9993908f; //corresponds to ~2 degrees

        //aimer textures
        Vector3 pointingAtPosition;
        Vector3 bulletPrediction;
        Vector3 fixedLeadOffset = Vector3.zero;
        float targetLeadDistance;
        
        //gapless particles
        List<BDAGaplessParticleEmitter> gaplessEmitters = new List<BDAGaplessParticleEmitter>();

        //muzzleflash emitters
        List<KSPParticleEmitter> muzzleFlashEmitters;
        
        //module references
        [KSPField] public int turretID = 0;
        public ModuleTurret turret;
        MissileFire mf;

        public MissileFire weaponManager
        {
            get
            {
                if (mf) return mf;
                List<MissileFire>.Enumerator wm = vessel.FindPartModulesImplementing<MissileFire>().GetEnumerator();
                while (wm.MoveNext())
                {
                    if (wm.Current == null) continue;
                    mf = wm.Current;
                    break;
                }
                wm.Dispose();
                return mf;
            }
        }

        LineRenderer[] laserRenderers;

        bool pointingAtSelf; //true if weapon is pointing at own vessel
        bool userFiring;
        Vector3 laserPoint;
        public bool slaved;

        public Transform turretBaseTransform
        {
            get
            {
                if (turret)
                {
                    return turret.yawTransform.parent;
                }
                else
                {
                    return fireTransforms[0];
                }
            }
        }

        public float maxPitch
        {
            get { return turret ? turret.maxPitch : 0; }
        }

        public float minPitch
        {
            get { return turret ? turret.minPitch : 0; }
        }

        public float yawRange
        {
            get { return turret ? turret.yawRange : 0; }
        }

        //weapon interface
        public WeaponClasses GetWeaponClass()
        {
            return WeaponClasses.Gun;
        }

		public Part GetPart()
        {
            return part;
        }

        public string GetSubLabel()
        {
            return string.Empty;
        }

        public string GetMissileType()
        {
            return string.Empty;
        }

        #if DEBUG
        Vector3 relVelAdj;
        Vector3 accAdj;
        Vector3 gravAdj;
        #endif

        #endregion

        #region KSPFields

		[KSPField(isPersistant = true, guiActive = true, guiName = "Weapon Name ", guiActiveEditor = true), UI_Label(affectSymCounterparts = UI_Scene.All, scene = UI_Scene.All)]
		public string WeaponName;

		[KSPField]
        public string fireTransformName = "fireTransform";
        public Transform[] fireTransforms;

        [KSPField]
        public string shellEjectTransformName = "shellEject";
        public Transform[] shellEjectTransforms;

        [KSPField]
        public bool hasDeployAnim = false;
        [KSPField]
        public string deployAnimName = "deployAnim";
        AnimationState deployState;
        [KSPField]
        public bool hasFireAnimation = false;
        [KSPField]
        public string fireAnimName = "fireAnim";
        private AnimationState fireState;
        [KSPField]
        public bool spinDownAnimation = false;
        private bool spinningDown;

        //weapon specifications
        [KSPField]
        public float maxTargetingRange = 2000; //max range for raycasting and sighting
        [KSPField]
        public float roundsPerMinute = 850; //rate of fire
        [KSPField]
        public float maxDeviation = 1; //inaccuracy two standard deviations in degrees (two because backwards compatibility :)
        [KSPField]
        public float maxEffectiveDistance = 2500; //used by AI to select appropriate weapon
        [KSPField]
        public float bulletMass = 0.3880f; //mass in KG - used for damage and recoil and drag
        [KSPField]
        public float caliber = 30; //caliber in mm, used for penetration calcs
        [KSPField]
        public float bulletDmgMult = 1; //Used for heat damage modifier for non-explosive bullets
        [KSPField]
        public float bulletVelocity = 1030; //velocity in meters/second
        [KSPField]
        public float ECPerShot = 0; //EC to use per shot for weapons like railguns

        [KSPField]
        public string bulletDragTypeName = "AnalyticEstimate";
        public BulletDragTypes bulletDragType;

        //drag area of the bullet in m^2; equal to Cd * A with A being the frontal area of the bullet; as a first approximation, take Cd to be 0.3
        //bullet mass / bullet drag area.  Used in analytic estimate to speed up code
        [KSPField]
        public float bulletDragArea = 1.209675e-5f;

        private BulletInfo bulletInfo;

        [KSPField]
        public string bulletType = "def";

        [KSPField]
        public string ammoName = "50CalAmmo"; //resource usage
        [KSPField]
        public float requestResourceAmount = 1; //amount of resource/ammo to deplete per shot
        [KSPField]
        public float shellScale = 0.66f; //scale of shell to eject
        [KSPField]
        public bool hasRecoil = true;
        [KSPField]
        public float recoilReduction = 1; //for reducing recoil on large guns with built in compensation

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Fire Limits"),
         UI_Toggle(disabledText = "None", enabledText = "In range")]
        public bool onlyFireInRange = true;
        //prevent firing when gun's turret is trying to exceed gimbal limits

        [KSPField]
        public bool bulletDrop = true; //projectiles are affected by gravity

        [KSPField]
        public string weaponType = "ballistic";
        //ballistic, cannon or laser

        [KSPField]
        public float laserDamage = 10000; //base damage/second of lasers

        //cannon shell specfications
        //TODO: deprectated, moved to bullet config
        [KSPField]
        public float cannonShellRadius = 30; //max radius of explosion forces/damage
        [KSPField]
        public float cannonShellPower = 8; //explosion's impulse force
        [KSPField]
        public float cannonShellHeat = -1; //if non-negative, heat damage


        //projectile graphics
        [KSPField]
        public string projectileColor = "255, 130, 0, 255"; //final color of projectile
        Color projectileColorC;
        [KSPField]
        public bool fadeColor = false;

        [KSPField]
        public string startColor = "255, 160, 0, 200";
        //if fade color is true, projectile starts at this color

        Color startColorC;
        [KSPField]
        public float tracerStartWidth = 0.25f;
        [KSPField]
        public float tracerEndWidth = 0.2f;

        [KSPField]
        public float tracerLength = 0;
        //if set to zero, tracer will be the length of the distance covered by the projectile in one physics timestep

        [KSPField]
        public float tracerDeltaFactor = 2.65f;
        [KSPField]
        public float nonTracerWidth = 0.01f;
        [KSPField]
        public int tracerInterval = 0;
        [KSPField]
        public float tracerLuminance = 1.75f;
        int tracerIntervalCounter;
        [KSPField]
        public string bulletTexturePath = "BDArmory/Textures/bullet";

        [KSPField]
        public bool oneShotWorldParticles = false;

        //heat
        [KSPField]
        public float maxHeat = 3600;
        [KSPField]
        public float heatPerShot = 75;
        [KSPField]
        public float heatLoss = 250;

        //canon explosion effects
        [KSPField]
        public string explModelPath = "BDArmory/Models/explosion/explosion";

        [KSPField]
        public string explSoundPath = "BDArmory/Sounds/explode1";

        //Used for scaling laser damage down based on distance.
        [KSPField]
        public float tanAngle = 0.0001f;
        //Angle of divergeance/2. Theoretical minimum value calculated using θ = (1.22 L/RL)/2, 
        //where L is laser's wavelength and RL is the radius of the mirror (=gun).


        //audioclip paths
        [KSPField]
        public string fireSoundPath = "BDArmory/Parts/50CalTurret/sounds/shot";
        [KSPField]
        public string overheatSoundPath = "BDArmory/Parts/50CalTurret/sounds/turretOverheat";
        [KSPField]
        public string chargeSoundPath = "BDArmory/Parts/laserTest/sounds/charge";

        //audio
        [KSPField]
        public bool oneShotSound = true;
        //play audioclip on every shot, instead of playing looping audio while firing

        [KSPField]
        public float soundRepeatTime = 1;
        //looped audio will loop back to this time (used for not playing the opening bit, eg the ramp up in pitch of gatling guns)

        [KSPField]
        public string reloadAudioPath = string.Empty;
        AudioClip reloadAudioClip;
        [KSPField]
        public string reloadCompletePath = string.Empty;


        private ProtoStageIconInfo reloadBar;
        [KSPField]
        public bool showReloadMeter = false; //used for cannons or guns with extremely low rate of fire

        //Air Detonating Rounds
        [KSPField]
        public bool airDetonation = false;

        [KSPField]
        public bool proximityDetonation = false;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Default Detonation Range"),
         UI_FloatRange(minValue = 500, maxValue = 8000f, stepIncrement = 5f, scene = UI_Scene.All)]
        public float defaultDetonationRange = 3500;

        [KSPField]
        public float maxAirDetonationRange = 3500;
        float detonationRange = 10f;
        [KSPField]
        public bool airDetonationTiming = true;

        //auto proximity tracking
        [KSPField]
        public float autoProxyTrackRange = 0;
        bool atprAcquired;
        int aptrTicker;

        float timeFired;
        public float initialFireDelay = 0; //used to ripple fire multiple weapons of this type

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Barrage")]
        public bool
            useRippleFire = true;

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Toggle Barrage")]
        public void ToggleRipple()
        {
            List<Part>.Enumerator craftPart = EditorLogic.fetch.ship.parts.GetEnumerator();
            while (craftPart.MoveNext())
            {
                if (craftPart.Current == null) continue;
                if (craftPart.Current.name != part.name) continue;
                List<ModuleWeapon>.Enumerator weapon = craftPart.Current.FindModulesImplementing<ModuleWeapon>().GetEnumerator();
                while (weapon.MoveNext())
                {
                    if (weapon.Current == null) continue;
                    weapon.Current.useRippleFire = !weapon.Current.useRippleFire;
                }
                weapon.Dispose();
            }
            craftPart.Dispose();
        }

        IEnumerator IncrementRippleIndex(float delay)
        {
            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }
            weaponManager.gunRippleIndex = weaponManager.gunRippleIndex + 1;

            //Debug.Log("incrementing ripple index to: " + weaponManager.gunRippleIndex);
        }
        
        #endregion

        #region KSPActions

        [KSPAction("Toggle Weapon")]
        public void AGToggle(KSPActionParam param)
        {
            Toggle();
        }

        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Status")]
        public string guiStatusString =
            "Disabled";

        //PartWindow buttons
        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "Toggle")]
        public void Toggle()
        {
            if (weaponState == WeaponStates.Disabled || weaponState == WeaponStates.PoweringDown)
            {
                EnableWeapon();
            }
            else
            {
                DisableWeapon();
            }
        }

        bool agHoldFiring;

        [KSPAction("Fire (Toggle)")]
        public void AGFireToggle(KSPActionParam param)
        {
            agHoldFiring = (param.type == KSPActionType.Activate);
        }

        [KSPAction("Fire (Hold)")]
        public void AGFireHold(KSPActionParam param)
        {
            StartCoroutine(FireHoldRoutine(param.group));
        }

        IEnumerator FireHoldRoutine(KSPActionGroup group)
        {
            KeyBinding key = Misc.Misc.AGEnumToKeybinding(group);
            if (key == null)
            {
                yield break;
            }

            while (key.GetKey())
            {
                agHoldFiring = true;
                yield return null;
            }

            agHoldFiring = false;
            yield break;
        }

        #endregion

        #region KSP Events

        public override void OnAwake()
        {
            base.OnAwake();

            part.stagingIconAlwaysShown = true;
            this.part.stackIconGrouping = StackIconGrouping.SAME_TYPE;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            part.stagingIconAlwaysShown = true;
            this.part.stackIconGrouping = StackIconGrouping.SAME_TYPE;

            GameEvents.onVesselSwitching.Add(ReloadIconOnVesselSwitch);

			Events["HideUI"].active = false;
			Events["ShowUI"].active = true;
			ParseWeaponType();
            
            // extension for feature_engagementenvelope
            InitializeEngagementRange(0, maxEffectiveDistance);
			if (string.IsNullOrEmpty(GetShortName()))
			{
				shortName = part.partInfo.title;
			}
			WeaponName = shortName;
			IEnumerator<KSPParticleEmitter> emitter = part.FindModelComponents<KSPParticleEmitter>().AsEnumerable().GetEnumerator();
            while (emitter.MoveNext())
            {
                if (emitter.Current == null) continue;
                emitter.Current.emit = false;
                EffectBehaviour.AddParticleEmitter(emitter.Current);
            }
            emitter.Dispose();

            if (roundsPerMinute >= 1500)
            {
                Events["ToggleRipple"].guiActiveEditor = false;
                Fields["useRippleFire"].guiActiveEditor = false;
            }
            vessel.Velocity();
            if (airDetonation)
            {
                UI_FloatRange detRange = (UI_FloatRange)Fields["defaultDetonationRange"].uiControlEditor;
                detRange.maxValue = maxAirDetonationRange;
            }
            else
            {
                Fields["defaultDetonationRange"].guiActive = false;
                Fields["defaultDetonationRange"].guiActiveEditor = false;
            }
			muzzleFlashEmitters = new List<KSPParticleEmitter>();
            IEnumerator<Transform> mtf = part.FindModelTransforms("muzzleTransform").AsEnumerable().GetEnumerator();
            while (mtf.MoveNext())
            {
                if (mtf.Current == null) continue;
                KSPParticleEmitter kpe = mtf.Current.GetComponent<KSPParticleEmitter>();
                EffectBehaviour.AddParticleEmitter(kpe);
                muzzleFlashEmitters.Add(kpe);
                kpe.emit = false;
            }
            mtf.Dispose();

            if (HighLogic.LoadedSceneIsFlight)
            {
                if (eWeaponType != WeaponTypes.Laser)
                {
                    if (bulletPool == null)
                    {
                        SetupBulletPool();
                    }
                    if (shellPool == null)
                    {
                        SetupShellPool();
                    }
                }

                //setup transforms
                fireTransforms = part.FindModelTransforms(fireTransformName);
                shellEjectTransforms = part.FindModelTransforms(shellEjectTransformName);

                //setup emitters
                IEnumerator<KSPParticleEmitter> pe = part.FindModelComponents<KSPParticleEmitter>().AsEnumerable().GetEnumerator();
                while (pe.MoveNext())
                {
                    if (pe.Current == null) continue;
                    pe.Current.maxSize *= part.rescaleFactor;
                    pe.Current.minSize *= part.rescaleFactor;
                    pe.Current.shape3D *= part.rescaleFactor;
                    pe.Current.shape2D *= part.rescaleFactor;
                    pe.Current.shape1D *= part.rescaleFactor;

                    if (pe.Current.useWorldSpace && !oneShotWorldParticles)
                    {
                        BDAGaplessParticleEmitter gpe = pe.Current.gameObject.AddComponent<BDAGaplessParticleEmitter>();
                        gpe.part = part;
                        gaplessEmitters.Add(gpe);
                    }
                    else
                    {
                        EffectBehaviour.AddParticleEmitter(pe.Current);
                    }
                }
                pe.Dispose();

                //setup projectile colors
                projectileColorC = Misc.Misc.ParseColor255(projectileColor);
                startColorC = Misc.Misc.ParseColor255(startColor);

                //init and zero points
                targetPosition = Vector3.zero;
                pointingAtPosition = Vector3.zero;
                bulletPrediction = Vector3.zero;

                //setup audio
                SetupAudio();

                //laser setup
                if (eWeaponType == WeaponTypes.Laser)
                {
                    SetupLaserSpecifics();
                }
            }
            else if (HighLogic.LoadedSceneIsEditor)
            {
                fireTransforms = part.FindModelTransforms(fireTransformName);
   				WeaponNameWindow.OnActionGroupEditorOpened.Add(OnActionGroupEditorOpened);
				WeaponNameWindow.OnActionGroupEditorClosed.Add(OnActionGroupEditorClosed);
			}
			//turret setup
			List<ModuleTurret>.Enumerator turr = part.FindModulesImplementing<ModuleTurret>().GetEnumerator();
            while (turr.MoveNext())
            {
                if (turr.Current == null) continue;
                if (turr.Current.turretID != turretID) continue;
                turret = turr.Current;
                turret.SetReferenceTransform(fireTransforms[0]);
                break;
            }
            turr.Dispose();

            if (!turret)
            {
                Fields["onlyFireInRange"].guiActive = false;
                Fields["onlyFireInRange"].guiActiveEditor = false;
            }


            //setup animations
            if (hasDeployAnim)
            {
                deployState = Misc.Misc.SetUpSingleAnimation(deployAnimName, part);
                deployState.normalizedTime = 0;
                deployState.speed = 0;
                deployState.enabled = true;
            }
            if (hasFireAnimation)
            {
                fireState = Misc.Misc.SetUpSingleAnimation(fireAnimName, part);
                fireState.enabled = false;
            }

            SetupBullet();

            if (bulletInfo == null)
            {
                if(BDArmorySettings.DRAW_DEBUG_LABELS)
                    Debug.Log("[BDArmory]: Failed To load bullet : " + bulletType);
            }
            else
            {
                if(BDArmorySettings.DRAW_DEBUG_LABELS)
                    Debug.Log("[BDArmory]: BulletType Loaded : " + bulletType);
            }

            BDArmorySetup.OnVolumeChange += UpdateVolume;
        }

        private void ReloadIconOnVesselSwitch(Vessel data0, Vessel data1)
        {
            if (part == null) return;
            if (part.vessel == null) return;

            if (part.vessel.isActiveVessel)
            {
                part.stagingIconAlwaysShown = true;
                this.part.stackIconGrouping = StackIconGrouping.SAME_TYPE;
            }
        }

        void OnDestroy()
        {
            BDArmorySetup.OnVolumeChange -= UpdateVolume;
			WeaponNameWindow.OnActionGroupEditorOpened.Remove(OnActionGroupEditorOpened);
			WeaponNameWindow.OnActionGroupEditorClosed.Remove(OnActionGroupEditorClosed);
		}
		void Update()
        {
            if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready && !vessel.packed && vessel.IsControllable)
            {
                if (lowpassFilter)
                {
                    if (InternalCamera.Instance && InternalCamera.Instance.isActive)
                    {
                        lowpassFilter.enabled = true;
                    }
                    else
                    {
                        lowpassFilter.enabled = false;
                    }
                }

                if (weaponState == WeaponStates.Enabled &&
                    (TimeWarp.WarpMode != TimeWarp.Modes.HIGH || TimeWarp.CurrentRate == 1))
                {
                    userFiring = (BDInputUtils.GetKey(BDInputSettingsFields.WEAP_FIRE_KEY) &&
                                  (vessel.isActiveVessel || BDArmorySettings.REMOTE_SHOOTING) && !MapView.MapIsEnabled &&
                                  !aiControlled);
                    if ((userFiring || autoFire || agHoldFiring) &&
                        (yawRange == 0 || (maxPitch - minPitch) == 0 ||
                         turret.TargetInRange(finalAimTarget, 10, float.MaxValue)))
                    {
                        if (useRippleFire && (pointingAtSelf || isOverheated))
                        {
                            StartCoroutine(IncrementRippleIndex(0));
                            FinalFire = false;
                        }
                        else if (eWeaponType == WeaponTypes.Ballistic || eWeaponType == WeaponTypes.Cannon) //WeaponTypes.Cannon is deprecated
                        {
                            FinalFire = true;
                        }
                    }
                    else
                    {
                        if (spinDownAnimation) spinningDown = true;
                        if (eWeaponType == WeaponTypes.Laser) audioSource.Stop();
                        if (!oneShotSound && wasFiring)
                        {
                            audioSource.Stop();
                            wasFiring = false;
                            audioSource2.PlayOneShot(overheatSound);
                        }
                    }
                }
                else
                {
                    audioSource.Stop();
                    autoFire = false;
                }

                if (spinningDown && spinDownAnimation && hasFireAnimation)
                {
                    if (fireState.normalizedTime > 1) fireState.normalizedTime = 0;
                    fireState.speed = fireAnimSpeed;
                    fireAnimSpeed = Mathf.Lerp(fireAnimSpeed, 0, 0.04f);
                }
            }
        }

        void FixedUpdate()
        {
			if (HighLogic.LoadedSceneIsFlight && !vessel.packed)
            {
                if (MultiplayerFire)
                {
                    if (eWeaponType == WeaponTypes.Laser)
                    {
                        FireLaser();
                    }
                    else
                    {
                        Fire();
                    }
                }

                if (!vessel.IsControllable)
                {
                    if (weaponState != WeaponStates.PoweringDown || weaponState != WeaponStates.Disabled)
                    {
                        DisableWeapon();
                    }
                    return;
                }


                if (vessel.isActiveVessel)
                {
                    if (showReloadMeter)
                    {
                        UpdateReloadMeter();
                    }
                    else
                    {
                        UpdateHeatMeter();
                    }
                }
             
                
                UpdateHeat();
                if (weaponState == WeaponStates.Enabled &&
                    (TimeWarp.WarpMode != TimeWarp.Modes.HIGH || TimeWarp.CurrentRate == 1))
                {
                    //Aim();
                    if ((BDArmorySettings.MULTIPLAYER_ACTIVE && BDArmorySettings.MULTIPLAYER_VESSELS_OWNED.Contains(vessel.id)))
                    {
                        StartCoroutine(AimAndFireAtEndOfFrame()); 
                    }


                    if (eWeaponType == WeaponTypes.Laser)
                    {
                        if ((userFiring || autoFire || agHoldFiring) &&
                            (!turret || turret.TargetInRange(targetPosition, 10, float.MaxValue)))
                        {
                            FinalFire = true;
                        }
                        else
                        {
                            for (int i = 0; i < laserRenderers.Length; i++)
                            {
                                laserRenderers[i].enabled = false;
                            }
                            audioSource.Stop();
                        }
                    }
                }
                else if (eWeaponType == WeaponTypes.Laser)
                {
                    for (int i = 0; i < laserRenderers.Length; i++)
                    {
                        laserRenderers[i].enabled = false;
                    }
                    audioSource.Stop();
                }
            }
            lastFinalAimTarget = finalAimTarget;
        }
		private void UpdateMenus(bool visible)
		{
			Events["HideUI"].active = visible;
			Events["ShowUI"].active = !visible;
		}

		private void OnActionGroupEditorOpened()
		{
			Events["HideUI"].active = false;
			Events["ShowUI"].active = false;
		}

		private void OnActionGroupEditorClosed()
		{
			Events["HideUI"].active = false;
			Events["ShowUI"].active = true;
		}
		[KSPEvent(guiActiveEditor = true, guiName = "Hide Weapon Group UI", active = false)]
		public void HideUI()
		{
			WeaponGroupWindow.HideGUI();
			UpdateMenus(false);
		}

		[KSPEvent(guiActiveEditor = true, guiName = "Set Weapon Group UI", active = false)]
		public void ShowUI()
		{
			WeaponGroupWindow.ShowGUI(this);
			UpdateMenus(true);
		}
		void OnGUI()
        {
            if (weaponState == WeaponStates.Enabled && vessel && !vessel.packed && vessel.isActiveVessel &&
                BDArmorySettings.DRAW_AIMERS && !aiControlled && !MapView.MapIsEnabled && !pointingAtSelf)
            {
                float size = 30;

                Vector3 reticlePosition;
                if (BDArmorySettings.AIM_ASSIST)
                {
                    if (targetAcquired && (slaved || yawRange < 1 || maxPitch - minPitch < 1))
                    {
                        reticlePosition = pointingAtPosition + fixedLeadOffset;

                        if (!slaved)
                        {
                            BDGUIUtils.DrawLineBetweenWorldPositions(pointingAtPosition, reticlePosition, 2,
                                new Color(0, 1, 0, 0.6f));
                        }

                        BDGUIUtils.DrawTextureOnWorldPos(pointingAtPosition, BDArmorySetup.Instance.greenDotTexture,
                            new Vector2(6, 6), 0);

                        if (atprAcquired)
                        {
                            BDGUIUtils.DrawTextureOnWorldPos(targetPosition, BDArmorySetup.Instance.openGreenSquare,
                                new Vector2(20, 20), 0);
                        }
                    }
                    else
                    {
                        reticlePosition = bulletPrediction;
                    }
                }
                else
                {
                    reticlePosition = pointingAtPosition;
                }


                Texture2D texture;
                if (Vector3.Angle(pointingAtPosition - transform.position, finalAimTarget - transform.position) < 1f)
                {
                    texture = BDArmorySetup.Instance.greenSpikedPointCircleTexture;
                }
                else
                {
                    texture = BDArmorySetup.Instance.greenPointCircleTexture;
                }
                BDGUIUtils.DrawTextureOnWorldPos(reticlePosition, texture, new Vector2(size, size), 0);

                if (BDArmorySettings.DRAW_DEBUG_LINES)
                {
                    if (targetAcquired)
                    {
                        BDGUIUtils.DrawLineBetweenWorldPositions(fireTransforms[0].position, targetPosition, 2,
                            Color.blue);
                    }
                }
            }

            if (HighLogic.LoadedSceneIsEditor && BDArmorySetup.showWeaponAlignment)
            {
                DrawAlignmentIndicator();
            }

            #if DEBUG
            if (BDArmorySettings.DRAW_DEBUG_LINES && weaponState == WeaponStates.Enabled && vessel && !vessel.packed && !MapView.MapIsEnabled)
            {
                BDGUIUtils.DrawLineBetweenWorldPositions(targetPosition + transform.right * 3, targetPosition - transform.right * 3, 2, Color.cyan);
                BDGUIUtils.DrawLineBetweenWorldPositions(targetPosition + transform.up * 3, targetPosition - transform.up * 3, 2, Color.cyan);
                BDGUIUtils.DrawLineBetweenWorldPositions(targetPosition + transform.forward * 3, targetPosition - transform.forward * 3, 2, Color.cyan);

                BDGUIUtils.DrawLineBetweenWorldPositions(targetPosition, targetPosition + relVelAdj, 2, Color.green);
                BDGUIUtils.DrawLineBetweenWorldPositions(targetPosition + relVelAdj, targetPosition + relVelAdj + accAdj, 2, Color.magenta);
                BDGUIUtils.DrawLineBetweenWorldPositions(targetPosition + relVelAdj + accAdj, targetPosition + relVelAdj + accAdj + gravAdj, 2, Color.yellow);

                BDGUIUtils.DrawLineBetweenWorldPositions(finalAimTarget + transform.right * 4, finalAimTarget - transform.right * 4, 2, Color.cyan);
                BDGUIUtils.DrawLineBetweenWorldPositions(finalAimTarget + transform.up * 4, finalAimTarget - transform.up * 4, 2, Color.cyan);
                BDGUIUtils.DrawLineBetweenWorldPositions(finalAimTarget + transform.forward * 4, finalAimTarget - transform.forward * 4, 2, Color.cyan);
            }
            #endif
        }

        #endregion

        #region Fire

        private void Fire()
        {
            if (BDArmorySetup.GameIsPaused)
            {
                if (audioSource.isPlaying)
                {
                    audioSource.Stop();
                }
                return;
            }

            float timeGap = (60 / roundsPerMinute) * TimeWarp.CurrentRate;
            if (Time.time - timeFired > timeGap 
                && !isOverheated 
                && !pointingAtSelf 
                && (aiControlled || !Misc.Misc.CheckMouseIsOnGui())
                && WMgrAuthorized())
            {
                bool effectsShot = false;
                //Transform[] fireTransforms = part.FindModelTransforms("fireTransform");
                for (float iTime = Mathf.Min(Time.time - timeFired - timeGap, TimeWarp.fixedDeltaTime); iTime >= 0; iTime -= timeGap)
                for (int i = 0; i < fireTransforms.Length; i++)
                {
                    //if ((BDArmorySettings.INFINITE_AMMO || part.RequestResource(ammoName, requestResourceAmount) > 0))
                    if (CanFire())
                        {
                        Transform fireTransform = fireTransforms[i];
                        spinningDown = false;

                        //recoil
                        if (hasRecoil)
                        {
                            part.AddForceToPart((-fireTransform.forward) * (bulletVelocity * bulletMass / 1000 * BDArmorySettings.RECOIL_FACTOR * recoilReduction),
                                fireTransform.position, ForceMode.Impulse, false);
                        }

                        if (!effectsShot)
                        {
                            //sound
                            if (oneShotSound)
                            {
                                audioSource.Stop();
                                audioSource.PlayOneShot(fireSound);
                            }
                            else
                            {
                                wasFiring = true;
                                if (!audioSource.isPlaying)
                                {
                                    audioSource.clip = fireSound;
                                    audioSource.loop = false;
                                    audioSource.time = 0;
                                    audioSource.Play();
                                }
                                else
                                {
                                    if (audioSource.time >= fireSound.length)
                                    {
                                        audioSource.time = soundRepeatTime;
                                    }
                                }
                            }

                            //animation
                            if (hasFireAnimation)
                            {
                                float unclampedSpeed = (roundsPerMinute * fireState.length) / 60f;
                                float lowFramerateFix = 1;
                                if (roundsPerMinute > 500f)
                                {
                                    lowFramerateFix = (0.02f / Time.deltaTime);
                                }
                                fireAnimSpeed = Mathf.Clamp(unclampedSpeed, 1f * lowFramerateFix, 20f * lowFramerateFix);
                                fireState.enabled = true;
                                if (unclampedSpeed == fireAnimSpeed || fireState.normalizedTime > 1)
                                {
                                    fireState.normalizedTime = 0;
                                }
                                fireState.speed = fireAnimSpeed;
                                fireState.normalizedTime = Mathf.Repeat(fireState.normalizedTime, 1);

                                //Debug.Log("fireAnim time: " + fireState.normalizedTime + ", speed; " + fireState.speed);
                            }

                            //muzzle flash
                            List<KSPParticleEmitter>.Enumerator pEmitter = muzzleFlashEmitters.GetEnumerator();
                            while (pEmitter.MoveNext())
                            {
                                if (pEmitter.Current == null) continue;
                                //KSPParticleEmitter pEmitter = mtf.gameObject.GetComponent<KSPParticleEmitter>();
                                if (pEmitter.Current.useWorldSpace && !oneShotWorldParticles) continue;
                                if (pEmitter.Current.maxEnergy < 0.5f)
                                {
                                    float twoFrameTime = Mathf.Clamp(Time.deltaTime * 2f, 0.02f, 0.499f);
                                    pEmitter.Current.maxEnergy = twoFrameTime;
                                    pEmitter.Current.minEnergy = twoFrameTime / 3f;
                                }
                                pEmitter.Current.Emit();
                            }
                            pEmitter.Dispose();

                            List<BDAGaplessParticleEmitter>.Enumerator gpe = gaplessEmitters.GetEnumerator();
                            while (gpe.MoveNext())
                            {
                                if (gpe.Current == null) continue;
                                gpe.Current.EmitParticles();
                            }
                            gpe.Dispose();

                            //shell ejection
                            if (BDArmorySettings.EJECT_SHELLS)
                            {
                                IEnumerator<Transform> sTf = shellEjectTransforms.AsEnumerable().GetEnumerator();
                                while (sTf.MoveNext())
                                {
                                    if (sTf.Current == null) continue;                                    
                                    GameObject ejectedShell = shellPool.GetPooledObject();
                                    ejectedShell.transform.position = sTf.Current.position;
                                    //+(part.rb.velocity*TimeWarp.fixedDeltaTime);
                                    ejectedShell.transform.rotation = sTf.Current.rotation;
                                    ejectedShell.transform.localScale = Vector3.one * shellScale;
                                    ShellCasing shellComponent = ejectedShell.GetComponent<ShellCasing>();
                                    shellComponent.initialV = part.rb.velocity;
                                    ejectedShell.SetActive(true);
                                }
                                sTf.Dispose();
                            }
                            effectsShot = true;
                        }
                        
                        //firing bullet
                        GameObject firedBullet = bulletPool.GetPooledObject();
                        PooledBullet pBullet = firedBullet.GetComponent<PooledBullet>();

                        firedBullet.transform.position = fireTransform.position;

                        pBullet.OnlyVisual = this.MultiplayerFire;
                        pBullet.caliber = bulletInfo.caliber;
                        pBullet.bulletVelocity = bulletInfo.bulletVelocity;
                        pBullet.bulletMass = bulletInfo.bulletMass;
                        pBullet.explosive = bulletInfo.explosive;
                        pBullet.apBulletMod = bulletInfo.apBulletMod;                  
                        pBullet.bulletDmgMult = bulletDmgMult;

                        //A = π x (Ø / 2)^2
                        bulletDragArea = Mathf.PI * Mathf.Pow(caliber / 2f, 2f);

                        //Bc = m/Cd * A
                        bulletBallisticCoefficient = bulletMass / ((bulletDragArea / 1000000f) * 0.295f); // mm^2 to m^2
                        
                        //Bc = m/d^2 * i where i = 0.484
                        //bulletBallisticCoefficient = bulletMass / Mathf.Pow(caliber / 1000, 2f) * 0.484f;

                        pBullet.ballisticCoefficient = bulletBallisticCoefficient;

                        pBullet.flightTimeElapsed = iTime;
                        // measure bullet lifetime in time rather than in distance, because distances get very relative in orbit
                        pBullet.timeToLiveUntil = Mathf.Max(maxTargetingRange, maxEffectiveDistance) / bulletVelocity * 1.1f + Time.time;

                        timeFired = Time.time - iTime;
                        
                        Vector3 firedVelocity =
                            VectorUtils.GaussianDirectionDeviation(fireTransform.forward, maxDeviation / 2) * bulletVelocity;

                        pBullet.currentVelocity = (part.rb.velocity + Krakensbane.GetFrameVelocityV3f()) + firedVelocity; // use the real velocity, w/o offloading
                        firedBullet.transform.position += (part.rb.velocity + Krakensbane.GetFrameVelocityV3f()) * Time.fixedDeltaTime 
                                                            + pBullet.currentVelocity * iTime;

                        pBullet.sourceVessel = vessel;
                        pBullet.bulletTexturePath = bulletTexturePath;
                        pBullet.projectileColor = projectileColorC;
                        pBullet.startColor = startColorC;
                        pBullet.fadeColor = fadeColor;
                        tracerIntervalCounter++;
                        if (tracerIntervalCounter > tracerInterval)
                        {
                            tracerIntervalCounter = 0;
                            pBullet.tracerStartWidth = tracerStartWidth;
                            pBullet.tracerEndWidth = tracerEndWidth;
                        }
                        else
                        {
                            pBullet.tracerStartWidth = nonTracerWidth;
                            pBullet.tracerEndWidth = nonTracerWidth;
                            pBullet.startColor.a *= 0.5f;
                            pBullet.projectileColor.a *= 0.5f;
                        }
                        pBullet.tracerLength = tracerLength;
                        pBullet.tracerDeltaFactor = tracerDeltaFactor;
                        pBullet.tracerLuminance = tracerLuminance;
                        pBullet.bulletDrop = bulletDrop;

                        if ((eWeaponType == WeaponTypes.Ballistic && bulletInfo.explosive) || eWeaponType == WeaponTypes.Cannon) //WeaponTypes.Cannon is deprecated
                        {
                            if (bulletType == "def")
                            {
                                //legacy model, per weapon config
                                pBullet.bulletType = PooledBullet.PooledBulletTypes.Explosive;
                                pBullet.explModelPath = explModelPath;
                                pBullet.explSoundPath = explSoundPath;
                                pBullet.blastPower = cannonShellPower;
                                pBullet.blastHeat = cannonShellHeat;
                                pBullet.radius = cannonShellRadius;
                                pBullet.airDetonation = airDetonation;
                                pBullet.detonationRange = detonationRange;
                                pBullet.maxAirDetonationRange = maxAirDetonationRange;
                                pBullet.defaultDetonationRange = defaultDetonationRange;
                                pBullet.proximityDetonation = proximityDetonation;

                            }
                            else
                            {
                                //use values from bullets.cfg
                                pBullet.bulletType = PooledBullet.PooledBulletTypes.Explosive;                                
                                pBullet.explModelPath = explModelPath;
                                pBullet.explSoundPath = explSoundPath;

                                pBullet.tntMass = bulletInfo.tntMass;
                                pBullet.blastPower = bulletInfo.blastPower;
                                pBullet.blastHeat = bulletInfo.blastHeat;
                                pBullet.radius = bulletInfo.blastRadius;

                                pBullet.airDetonation = airDetonation;
                                pBullet.detonationRange = detonationRange;
                                pBullet.maxAirDetonationRange = maxAirDetonationRange;
                                pBullet.defaultDetonationRange = defaultDetonationRange;
                                pBullet.proximityDetonation = proximityDetonation;
                            }

                        }
                        else
                        {
                            pBullet.bulletType = PooledBullet.PooledBulletTypes.Standard;
                            pBullet.airDetonation = false;
                        }
                        switch (bulletDragType)
                        {
                            case BulletDragTypes.None:
                                pBullet.dragType = PooledBullet.BulletDragTypes.None;
                                break;
                            case BulletDragTypes.AnalyticEstimate:
                                pBullet.dragType = PooledBullet.BulletDragTypes.AnalyticEstimate;
                                break;
                            case BulletDragTypes.NumericalIntegration:
                                pBullet.dragType = PooledBullet.BulletDragTypes.NumericalIntegration;
                                break;
                        }

                        pBullet.bullet = BulletInfo.bullets[bulletType];
                        pBullet.gameObject.SetActive(true);


                        //heat
                        heat += heatPerShot;
                        //EC
                        DrainECPerShot();
                    }
                    else
                    {
                        spinningDown = true;
                        if (!oneShotSound && wasFiring)
                        {
                            audioSource.Stop();
                            wasFiring = false;
                            audioSource2.PlayOneShot(overheatSound);
                        }
                    }
                }

                if (useRippleFire)
                {
                    StartCoroutine(IncrementRippleIndex(initialFireDelay * TimeWarp.CurrentRate));
                }
            }
            else
            {
                spinningDown = true;
            }
        }

        private bool FireLaser()
        {
            float maxDistance = BDArmorySettings.PHYSICS_RANGE;
            if (BDArmorySettings.PHYSICS_RANGE == 0)
                maxDistance = 2500;

            float chargeAmount = requestResourceAmount * TimeWarp.fixedDeltaTime;

            if (!pointingAtSelf && !Misc.Misc.CheckMouseIsOnGui() && WMgrAuthorized() && !isOverheated &&
                (part.RequestResource(ammoName, chargeAmount) >= chargeAmount || BDArmorySettings.INFINITE_AMMO))
            {
                if (!audioSource.isPlaying)
                {
                    audioSource.PlayOneShot(chargeSound);
                    audioSource.Play();
                    audioSource.loop = true;
                }
                for (int i = 0; i < fireTransforms.Length; i++)
                {
                    Transform tf = fireTransforms[i];

                    LineRenderer lr = laserRenderers[i];                    

                    Vector3 rayDirection = tf.forward;

                    Vector3 targetDirection = Vector3.zero; //autoTrack enhancer
                    Vector3 targetDirectionLR = tf.forward;

                    if (((legacyTargetVessel != null && legacyTargetVessel.loaded) || slaved) 
                        && Vector3.Angle(rayDirection, targetDirection) < 1)
                    {
                        targetDirection = targetPosition + relativeVelocity * Time.fixedDeltaTime * 2 - tf.position;
                        rayDirection = targetDirection;
                        targetDirectionLR = targetDirection;
                    }

                    Ray ray = new Ray(tf.position, rayDirection);
                    lr.useWorldSpace = false;
                    lr.SetPosition(0, Vector3.zero);
                    RaycastHit hit;                    
                    
                    if (Physics.Raycast(ray, out hit, maxDistance, 9076737))
                    {
                        lr.useWorldSpace = true;
                        laserPoint = hit.point + targetVelocity * Time.fixedDeltaTime;
                        
                        lr.SetPosition(0, tf.position + (part.rb.velocity * Time.fixedDeltaTime));
                        lr.SetPosition(1, laserPoint);  

                        KerbalEVA eva = hit.collider.gameObject.GetComponentUpwards<KerbalEVA>();
                        Part p = eva ? eva.part : hit.collider.gameObject.GetComponentInParent<Part>();

                        if (p && p.vessel && p.vessel != vessel)
                        {
                            float distance = hit.distance;
                            //Scales down the damage based on the increased surface area of the area being hit by the laser. Think flashlight on a wall.
                            p.AddDamage(laserDamage / (1 + Mathf.PI * Mathf.Pow(tanAngle * distance, 2)) *
                                             TimeWarp.fixedDeltaTime
                                             * 0.425f);

                            if (BDArmorySettings.INSTAKILL) p.Destroy();
                        }


                        if (Time.time - timeFired > 6 / 120 && BDArmorySettings.BULLET_HITS)
                        {
                            BulletHitFX.CreateBulletHit(p,hit.point, hit, hit.normal, false,0,0);
                        }

                    }
                    else
                    {
                        laserPoint = lr.transform.InverseTransformPoint((targetDirectionLR * maxDistance) + tf.position);
                        lr.SetPosition(1, laserPoint);
                    }
                }
                heat += heatPerShot * TimeWarp.CurrentRate;
                return true;
            }
            else
            {
                return false;
            }
        }

        void SetupLaserSpecifics()
        {
            chargeSound = GameDatabase.Instance.GetAudioClip(chargeSoundPath);
            if (HighLogic.LoadedSceneIsFlight)
            {
                audioSource.clip = fireSound;
            }

            laserRenderers = new LineRenderer[fireTransforms.Length];

            for (int i = 0; i < fireTransforms.Length; i++)
            {
                Transform tf = fireTransforms[i];
                laserRenderers[i] = tf.gameObject.AddComponent<LineRenderer>();
                Color laserColor = Misc.Misc.ParseColor255(projectileColor);
                laserColor.a = laserColor.a / 2;
                laserRenderers[i].material = new Material(Shader.Find("KSP/Particles/Alpha Blended"));
                laserRenderers[i].material.SetColor("_TintColor", laserColor);
                laserRenderers[i].material.mainTexture = GameDatabase.Instance.GetTexture("BDArmory/Textures/laser", false);
                laserRenderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; //= false;
                laserRenderers[i].receiveShadows = false;
                laserRenderers[i].startWidth = tracerStartWidth;
                laserRenderers[i].endWidth = tracerEndWidth;
                laserRenderers[i].positionCount = 2;
                laserRenderers[i].SetPosition(0, Vector3.zero);
                laserRenderers[i].SetPosition(1, Vector3.zero);
                laserRenderers[i].useWorldSpace = false;
                laserRenderers[i].enabled = false;
            }
        }

        bool WMgrAuthorized()
        {
            MissileFire manager = BDArmorySetup.Instance.ActiveWeaponManager;
            if (manager != null && manager.vessel == vessel)
            {
                if (manager.hasSingleFired) return false;
                else return true;
            }
            else
            {
                return true;
            }
        }

        void CheckWeaponSafety()
        {
            pointingAtSelf = false;
            for (int i = 0; i < fireTransforms.Length; i++)
            {
                Ray ray = new Ray(fireTransforms[i].position, fireTransforms[i].forward);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, maxTargetingRange, 9076737))
                {
                    KerbalEVA eva = hit.collider.gameObject.GetComponentUpwards<KerbalEVA>();
                    Part p = eva ? eva.part : hit.collider.gameObject.GetComponentInParent<Part>();
                    if (p && p.vessel && p.vessel == vessel)
                    {
                        pointingAtSelf = true;
                        break;
                    }
                }
                else
                {
                    pointingAtSelf = false;
                }


                if (targetAcquired)
                {
                    pointingAtPosition = fireTransforms[i].transform.position + (ray.direction * targetLeadDistance);
                }
                else
                {
                    pointingAtPosition = fireTransforms[i].position + (ray.direction * (maxTargetingRange));
                }
            }
        }

        public void EnableWeapon()
        {
            if (weaponState == WeaponStates.Enabled || weaponState == WeaponStates.PoweringUp)
            {
                return;
            }

            StopShutdownStartupRoutines();

            startupRoutine = StartCoroutine(StartupRoutine());
        }

        public void DisableWeapon()
        {
            if (weaponState == WeaponStates.Disabled || weaponState == WeaponStates.PoweringDown)
            {
                return;
            }

            StopShutdownStartupRoutines();

            shutdownRoutine = StartCoroutine(ShutdownRoutine());
        }

        void ParseWeaponType()
        {
            weaponType = weaponType.ToLower();

            switch (weaponType)
            {
                case "ballistic":
                    eWeaponType = WeaponTypes.Ballistic;
                    break;

                case "cannon":
                    // Note:  this type is deprecated.  behavior is duplicated with Ballistic and bulletInfo.explosive = true
                    // Type remains for backward compatability for now.
                    eWeaponType = WeaponTypes.Cannon;
                    break;

                case "laser":
                    eWeaponType = WeaponTypes.Laser;
                    break;
            }
        }

        void DrainECPerShot()
        {
            if (ECPerShot == 0) return;
            //double drainAmount = ECPerShot * TimeWarp.fixedDeltaTime;
            double drainAmount = ECPerShot;
            double chargeAvailable = part.RequestResource("ElectricCharge", drainAmount, ResourceFlowMode.ALL_VESSEL);
        }

        bool CanFire()
        {

            if (ECPerShot != 0)
            {
                double chargeAvailable = part.RequestResource("ElectricCharge", ECPerShot, ResourceFlowMode.ALL_VESSEL);
                if (chargeAvailable < ECPerShot * 0.95f)
                {
                    ScreenMessages.PostScreenMessage("Weapon Requires EC", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    return false;
                }
            }

            if ((BDArmorySettings.INFINITE_AMMO || part.RequestResource(ammoName, requestResourceAmount) > 0))
            {
                return true;
            }
            
            return false;
        }

        #endregion

        #region Audio

        void UpdateVolume()
        {
            if (audioSource)
            {
                audioSource.volume = BDArmorySettings.BDARMORY_WEAPONS_VOLUME;
            }
            if (audioSource2)
            {
                audioSource2.volume = BDArmorySettings.BDARMORY_WEAPONS_VOLUME;
            }
            if (lowpassFilter)
            {
                lowpassFilter.cutoffFrequency = BDArmorySettings.IVA_LOWPASS_FREQ;
            }
        }

        void SetupAudio()
        {
            fireSound = GameDatabase.Instance.GetAudioClip(fireSoundPath);
            overheatSound = GameDatabase.Instance.GetAudioClip(overheatSoundPath);
            if (!audioSource)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.bypassListenerEffects = true;
                audioSource.minDistance = .3f;
                audioSource.maxDistance = 1000;
                audioSource.priority = 10;
                audioSource.dopplerLevel = 0;
                audioSource.spatialBlend = 1;
            }

            if (!audioSource2)
            {
                audioSource2 = gameObject.AddComponent<AudioSource>();
                audioSource2.bypassListenerEffects = true;
                audioSource2.minDistance = .3f;
                audioSource2.maxDistance = 1000;
                audioSource2.dopplerLevel = 0;
                audioSource2.priority = 10;
                audioSource2.spatialBlend = 1;
            }

            if (reloadAudioPath != string.Empty)
            {
                reloadAudioClip = (AudioClip)GameDatabase.Instance.GetAudioClip(reloadAudioPath);
            }
            if (reloadCompletePath != string.Empty)
            {
                reloadCompleteAudioClip = (AudioClip)GameDatabase.Instance.GetAudioClip(reloadCompletePath);
            }

            if (!lowpassFilter && gameObject.GetComponents<AudioLowPassFilter>().Length == 0)
            {
                lowpassFilter = gameObject.AddComponent<AudioLowPassFilter>();
                lowpassFilter.cutoffFrequency = BDArmorySettings.IVA_LOWPASS_FREQ;
                lowpassFilter.lowpassResonanceQ = 1f;
            }

            UpdateVolume();
        }
             

        #endregion

        #region Targeting

        void Aim()
        {
            //AI control
            if (aiControlled && !slaved)
            {
                if (!targetAcquired)
                {
                    autoFire = false;
                    return;
                }
            }


            if (!slaved && !aiControlled && (yawRange > 0 || maxPitch - minPitch > 0))
            {
                //MouseControl
                Vector3 mouseAim = new Vector3(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height,
                    0);
                Ray ray = FlightCamera.fetch.mainCamera.ViewportPointToRay(mouseAim);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, maxTargetingRange, 9076737))
                {
                    targetPosition = hit.point;

                    //aim through self vessel if occluding mouseray

                    KerbalEVA eva = hit.collider.gameObject.GetComponentUpwards<KerbalEVA>();
                    Part p = eva ? eva.part : hit.collider.gameObject.GetComponentInParent<Part>();

                    if (p && p.vessel && p.vessel == vessel)
                    {
                        targetPosition = ray.direction * maxTargetingRange +
                                         FlightCamera.fetch.mainCamera.transform.position;
                    }
                }
                else
                {
                    targetPosition = (ray.direction * (maxTargetingRange + (FlightCamera.fetch.Distance * 0.75f))) +
                                     FlightCamera.fetch.mainCamera.transform.position;
                    if (legacyTargetVessel != null && legacyTargetVessel.loaded)
                    {
                        targetPosition = ray.direction *
                                         Vector3.Distance(legacyTargetVessel.transform.position,
                                             FlightCamera.fetch.mainCamera.transform.position) +
                                         FlightCamera.fetch.mainCamera.transform.position;
                    }
                }
            }


            //aim assist
            Vector3 finalTarget = targetPosition;
            Vector3 originalTarget = targetPosition;
            targetDistance = Vector3.Distance(finalTarget, transform.position);
            targetLeadDistance = targetDistance;

            if ((BDArmorySettings.AIM_ASSIST || aiControlled) && eWeaponType != WeaponTypes.Laser)
            {
                float effectiveVelocity = bulletVelocity;

                int iterations = 4;
                while (--iterations >= 0)
                {
                    float time = targetDistance / effectiveVelocity;
                    finalTarget = targetPosition;
                    
                    if (targetAcquired)
                    {
                        float time2 = VectorUtils.CalculateLeadTime(finalTarget - fireTransforms[0].position,
                            relativeVelocity, effectiveVelocity);
                        if (time2 > 0) time = time2;
                        finalTarget += relativeVelocity * time;
                        #if DEBUG
                        relVelAdj = relativeVelocity * time;
                        var vc = finalTarget;
                        #endif
                        
                        //target vessel relative velocity compensation
                        if (weaponManager.currentTarget?.Vessel.InOrbit() == true)
                        {
                            var geeForceAtTarget = FlightGlobals.getGeeForceAtPosition(targetPosition);
                            var finalTargetGeeForce = FlightGlobals.getGeeForceAtPosition(finalTarget + 0.5f * (targetAcceleration
                                - (FlightGlobals.getGeeForceAtPosition(targetPosition) - FlightGlobals.getGeeForceAtPosition(finalTarget)) / 2)
                                * time * time);
                            var cosine = Vector3d.Dot(finalTargetGeeForce.normalized, geeForceAtTarget.normalized);
                            var avGeeForce = (finalTargetGeeForce + geeForceAtTarget) / 2 / (2 * cosine * cosine - 1);
                            finalTarget += 0.5f * (targetAcceleration - geeForceAtTarget + avGeeForce) * time * time;
                        }
                        else
                            finalTarget += 0.5f * targetAcceleration * time * time; //target acceleration

                        #if DEBUG
                        accAdj = (finalTarget - vc);
                        #endif
                    }
                    else if (vessel.altitude < 6000)
                    {
                        float time2 = VectorUtils.CalculateLeadTime(finalTarget - fireTransforms[0].position,
                            -(part.rb.velocity + Krakensbane.GetFrameVelocityV3f()), effectiveVelocity);
                        if (time2 > 0) time = time2;
                        finalTarget += (-(part.rb.velocity + Krakensbane.GetFrameVelocityV3f()) * time);
                        //this vessel velocity compensation against stationary
                    }
                    Vector3 up = (VectorUtils.GetUpDirection(finalTarget) + VectorUtils.GetUpDirection(fireTransforms[0].position)).normalized;
                    if (bulletDrop)
                    {
                        #if DEBUG
                        var vc = finalTarget;
                        #endif
                        float gAccel = ((float)FlightGlobals.getGeeForceAtPosition(finalTarget).magnitude
                        + (float)FlightGlobals.getGeeForceAtPosition(fireTransforms[0].position).magnitude) / 2;
                        Vector3 intermediateTarget = finalTarget + (0.5f * gAccel * time * time * up); //gravity compensation, -fixedDeltaTime is for fixedUpdate granularity

                        var avGrav = (FlightGlobals.getGeeForceAtPosition(finalTarget) + FlightGlobals.getGeeForceAtPosition(fireTransforms[0].position)) / 2;
                        effectiveVelocity = bulletVelocity
                            * (float)Vector3d.Dot((intermediateTarget - fireTransforms[0].position).normalized, (finalTarget - fireTransforms[0].position).normalized);
                        // effectiveVelocity += (float)Vector3d.Dot(avGrav, (finalTarget - fireTransforms[0].position).normalized) * time * time / 2;
                        finalTarget = intermediateTarget;

                        #if DEBUG
                        gravAdj = (finalTarget - vc);
                        #endif
                    }
                    else break;

                }

                targetLeadDistance = Vector3.Distance(finalTarget, fireTransforms[0].position);
                fixedLeadOffset = originalTarget - finalTarget; //for aiming fixed guns to moving target	

                //airdetonation
                if (airDetonation)
                {
                    if (targetAcquired && airDetonationTiming)
                    {                       
                        detonationRange = BlastPhysicsUtils.CalculateBlastRange(bulletInfo.tntMass);
                    }
                    else
                    {
                        //detonationRange = defaultDetonationRange;
                    }
                }
            }

            if (airDetonation)
            {
                detonationRange *= UnityEngine.Random.Range(0.96f, 1.04f);
            }

            finalAimTarget = finalTarget;

            //final turret aiming
            if (slaved && !targetAcquired) return;
            if (turret)
            {
                bool origSmooth = turret.smoothRotation;
                if (aiControlled || slaved)
                {
                    turret.smoothRotation = false;
                }
                turret.AimToTarget(finalTarget);
                turret.smoothRotation = origSmooth;
            }
        }

        void CheckAIAutofire()
        {
            //autofiring with AI
            if (targetAcquired && aiControlled)
            {
                Transform fireTransform = fireTransforms[0];
                Vector3 targetRelPos = (finalAimTarget) - fireTransform.position;
                Vector3 aimDirection = fireTransform.forward;
                float targetCosAngle = Vector3.Dot(aimDirection, targetRelPos.normalized);

                Vector3 targetDiffVec = finalAimTarget - lastFinalAimTarget;
                Vector3 projectedTargetPos = targetDiffVec;
                //projectedTargetPos /= TimeWarp.fixedDeltaTime;
                //projectedTargetPos *= TimeWarp.fixedDeltaTime;
                projectedTargetPos *= 2; //project where the target will be in 2 timesteps
                projectedTargetPos += finalAimTarget;

                targetDiffVec.Normalize();
                Vector3 lastTargetRelPos = (lastFinalAimTarget) - fireTransform.position;

                if (BDATargetManager.CheckSafeToFireGuns(weaponManager, aimDirection, 1000, 0.999848f) //~1 degree of unsafe angle
                    && targetCosAngle >= maxAutoFireCosAngle) //check if directly on target
                {
                    autoFire = true;
                }
                else
                {
                    autoFire = false;
                }
            }
            else
            {
                autoFire = false;
            }

            //disable autofire after burst length
            if (autoFire && Time.time - autoFireTimer > autoFireLength)
            {
                autoFire = false;
                legacyTargetVessel = null;
            }
        }

        IEnumerator AimAndFireAtEndOfFrame()
        {
            if (eWeaponType != WeaponTypes.Laser) yield return new WaitForEndOfFrame();

            UpdateTargetVessel();
            updateAcceleration(targetVelocity);
            relativeVelocity = targetVelocity - vessel.rb_velocity;

            RunTrajectorySimulation();
            Aim();
            CheckWeaponSafety();
            CheckAIAutofire();



            if (FinalFire)
            {
                if (eWeaponType == WeaponTypes.Laser)
                {
                    if (FireLaser())
                    {
                        Dependencies.Get<FireEventService>().PublishFireEvent(this.part.vessel.id, this.part.flightID, this.part.craftID, true);
                        for (int i = 0; i < laserRenderers.Length; i++)
                        {
                            laserRenderers[i].enabled = true;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < laserRenderers.Length; i++)
                        {
                            laserRenderers[i].enabled = false;
                        }
                        audioSource.Stop();
                    }
                }
                else
                {
                    if (useRippleFire && weaponManager.gunRippleIndex != rippleIndex)
                    {
                        //timeFired = Time.time + (initialFireDelay - (60f / roundsPerMinute)) * TimeWarp.CurrentRate;
                        FinalFire = false;
                    }
                    else
                    {
                        FinalFire = true;
                    }

                    if (FinalFire)
                    {
                        Dependencies.Get<FireEventService>().PublishFireEvent(this.part.vessel.id, this.part.flightID, this.part.craftID, true);
                        Fire();
                    }
                }

                FinalFire = false;
            }
            else
            {
                Dependencies.Get<FireEventService>().PublishFireEvent(this.part.vessel.id, this.part.flightID, this.part.craftID, false);
            }

            yield break;
        }

        public Vector3 GetLeadOffset()
        {
            return fixedLeadOffset;
        }

        void RunTrajectorySimulation()
        {
            //trajectory simulation
            if (BDArmorySettings.AIM_ASSIST && BDArmorySettings.DRAW_AIMERS)
            {
                Transform fireTransform = fireTransforms[0];

                if (eWeaponType == WeaponTypes.Laser)
                {
                    Ray ray = new Ray(fireTransform.position, fireTransform.forward);
                    RaycastHit rayHit;
                    if (Physics.Raycast(ray, out rayHit, maxTargetingRange, 9076737))
                    {
                        bulletPrediction = rayHit.point;
                    }
                    else
                    {
                        bulletPrediction = ray.GetPoint(maxTargetingRange);
                    }

                    pointingAtPosition = ray.GetPoint(maxTargetingRange);
                }
                else //ballistic/cannon weapons
                {
                    float simDeltaTime = 0.155f;

                    Vector3 simVelocity = part.rb.velocity + Krakensbane.GetFrameVelocityV3f() + (bulletVelocity * fireTransform.forward);
                    Vector3 simCurrPos = fireTransform.position + ((part.rb.velocity + Krakensbane.GetFrameVelocityV3f()) * Time.fixedDeltaTime);
                    Vector3 simPrevPos = simCurrPos;
                    Vector3 simStartPos = simCurrPos;
                    bool simulating = true;

                    List<Vector3> pointPositions = new List<Vector3>();
                    pointPositions.Add(simCurrPos);

                    while (simulating)
                    {
                        RaycastHit hit;
                        if (bulletDrop) simVelocity += FlightGlobals.getGeeForceAtPosition(simCurrPos) * simDeltaTime;
                        simCurrPos += simVelocity * simDeltaTime;
                        pointPositions.Add(simCurrPos);

                        if (Physics.Raycast(simPrevPos, simCurrPos - simPrevPos, out hit,
                            Vector3.Distance(simPrevPos, simCurrPos), 9076737))
                        {
                            Vessel hitVessel = null;
                            try
                            {
                                KerbalEVA eva = hit.collider.gameObject.GetComponentUpwards<KerbalEVA>();
                                hitVessel = (eva ? eva.part : hit.collider.gameObject.GetComponentInParent<Part>()).vessel;
                            }
                            catch (NullReferenceException)
                            {
                            }

                            if (hitVessel == null || (hitVessel != null && hitVessel != vessel))
                            {
                                bulletPrediction = hit.point;
                                simulating = false;
                            }
                        }

                        simPrevPos = simCurrPos;

                        if (legacyTargetVessel != null && legacyTargetVessel.loaded && !legacyTargetVessel.Landed &&
                            (simStartPos - simCurrPos).sqrMagnitude > targetLeadDistance*targetLeadDistance)
                        {
                            bulletPrediction = simStartPos + (simCurrPos - simStartPos).normalized * targetLeadDistance;
                            simulating = false;
                        }

                        if ((simStartPos - simCurrPos).sqrMagnitude > maxTargetingRange*maxTargetingRange)
                        {
                            bulletPrediction = simStartPos + ((simCurrPos - simStartPos).normalized * maxTargetingRange);
                            simulating = false;
                        }
                    }


                    if (BDArmorySettings.DRAW_DEBUG_LINES && BDArmorySettings.DRAW_AIMERS)
                    {
                        Vector3[] pointsArray = pointPositions.ToArray();
                        if (gameObject.GetComponent<LineRenderer>() == null)
                        {
                            LineRenderer lr = gameObject.AddComponent<LineRenderer>();
                            lr.startWidth = .1f;
                            lr.endWidth = .1f;
                            lr.positionCount = pointsArray.Length;
                            for (int i = 0; i < pointsArray.Length; i++)
                            {
                                lr.SetPosition(i, pointsArray[i]);
                            }
                        }
                        else
                        {
                            LineRenderer lr = gameObject.GetComponent<LineRenderer>();
                            lr.enabled = true;
                            lr.positionCount = pointsArray.Length;
                            for (int i = 0; i < pointsArray.Length; i++)
                            {
                                lr.SetPosition(i, pointsArray[i]);
                            }
                        }
                    }
                }
            }
        }

        void DrawAlignmentIndicator()
        {
            if (fireTransforms == null || fireTransforms[0] == null) return;

            Transform refTransform = EditorLogic.RootPart.GetReferenceTransform();

            if (!refTransform) return;

            Vector3 fwdPos = fireTransforms[0].position + (5 * fireTransforms[0].forward);
            BDGUIUtils.DrawLineBetweenWorldPositions(fireTransforms[0].position, fwdPos, 4, Color.green);

            Vector3 referenceDirection = refTransform.up;
            Vector3 refUp = -refTransform.forward;
            Vector3 refRight = refTransform.right;

            Vector3 refFwdPos = fireTransforms[0].position + (5 * referenceDirection);
            BDGUIUtils.DrawLineBetweenWorldPositions(fireTransforms[0].position, refFwdPos, 2, Color.white);

            BDGUIUtils.DrawLineBetweenWorldPositions(fwdPos, refFwdPos, 2, XKCDColors.Orange);

            Vector2 guiPos;
            if (BDGUIUtils.WorldToGUIPos(fwdPos, out guiPos))
            {
                Rect angleRect = new Rect(guiPos.x, guiPos.y, 100, 200);

                Vector3 pitchVector = (5 * Vector3.ProjectOnPlane(fireTransforms[0].forward, refRight));
                Vector3 yawVector = (5 * Vector3.ProjectOnPlane(fireTransforms[0].forward, refUp));

                BDGUIUtils.DrawLineBetweenWorldPositions(fireTransforms[0].position + pitchVector, fwdPos, 3,
                    Color.white);
                BDGUIUtils.DrawLineBetweenWorldPositions(fireTransforms[0].position + yawVector, fwdPos, 3, Color.white);

                float pitch = Vector3.Angle(pitchVector, referenceDirection);
                float yaw = Vector3.Angle(yawVector, referenceDirection);

                string convergeDistance;

                Vector3 projAxis = Vector3.Project(refTransform.position - fireTransforms[0].transform.position,
                    refRight);
                float xDist = projAxis.magnitude;
                float convergeAngle = 90 - Vector3.Angle(yawVector, refTransform.up);
                if (Vector3.Dot(fireTransforms[0].forward, projAxis) > 0)
                {
                    convergeDistance = "Converge: " +
                                       Mathf.Round((xDist * Mathf.Tan(convergeAngle * Mathf.Deg2Rad))).ToString() + "m";
                }
                else
                {
                    convergeDistance = "Diverging";
                }

                string xAngle = "X: " + Vector3.Angle(fireTransforms[0].forward, pitchVector).ToString("0.00");
                string yAngle = "Y: " + Vector3.Angle(fireTransforms[0].forward, yawVector).ToString("0.00");

                GUI.Label(angleRect, xAngle + "\n" + yAngle + "\n" + convergeDistance);
            }
        }


        #endregion

        #region Updates
        void UpdateHeat()
        {
            heat = Mathf.Clamp(heat - heatLoss * TimeWarp.fixedDeltaTime, 0, Mathf.Infinity);
            if (heat > maxHeat && !isOverheated)
            {
                isOverheated = true;
                autoFire = false;
                audioSource.Stop();
                wasFiring = false;
                audioSource2.PlayOneShot(overheatSound);
                weaponManager.ResetGuardInterval();
            }
            if (heat < maxHeat / 3 && isOverheated) //reset on cooldown
            {
                isOverheated = false;
                heat = 0;
            }
        }
		void UpdateHeatMeter()
        {
            //heat
            if (heat > maxHeat / 3)
            {
                if (heatGauge == null)
                {
                    heatGauge = InitHeatGauge();
                }

                heatGauge?.SetValue(heat, maxHeat / 3, maxHeat);    //null check
            }
            else if (heatGauge != null && heat < maxHeat / 4)
            {
                part.stackIcon.ClearInfoBoxes();
                heatGauge = null;
			}
		}

		void UpdateReloadMeter()
        {
            if (Time.time - timeFired < (60 / roundsPerMinute) && Time.time - timeFired > 0.1f)
            {
                if (reloadBar == null)
                {
                    reloadBar = InitReloadBar();
                    if (reloadAudioClip)
                    {
                        audioSource.PlayOneShot(reloadAudioClip);
                    }
                }
                reloadBar.SetValue(Time.time - timeFired, 0, 60 / roundsPerMinute);
            }
            else if (reloadBar != null)
            {
                part.stackIcon.ClearInfoBoxes();
                reloadBar = null;
                if (reloadCompleteAudioClip)
                {
                    audioSource.PlayOneShot(reloadCompleteAudioClip);
                }
            }
        }

        void UpdateTargetVessel()
        {
            targetAcquired = false;
            slaved = false;
            bool atprWasAcquired = atprAcquired;
            atprAcquired = false;

            //targetVessel = null;
            if (BDArmorySettings.ALLOW_LEGACY_TARGETING)
            {
                if (!aiControlled)
                {
                    if (vessel.targetObject != null && vessel.targetObject.GetVessel() != null)
                    {
                        legacyTargetVessel = vessel.targetObject.GetVessel();
                    }
                }
            }

            if (weaponManager)
            {
                //legacy or visual range guard targeting
                if (aiControlled && weaponManager && legacyTargetVessel &&
                    (BDArmorySettings.ALLOW_LEGACY_TARGETING ||
                     (legacyTargetVessel.transform.position - transform.position).sqrMagnitude < weaponManager.guardRange*weaponManager.guardRange))
                {
                    targetPosition = legacyTargetVessel.CoM;
                    targetVelocity = legacyTargetVessel.rb_velocity;
                    targetAcquired = true;
                    return;
                }

                if (weaponManager.slavingTurrets && turret)
                {
                    slaved = true;
                    targetPosition = weaponManager.slavedPosition;
                    targetVelocity = weaponManager.slavedTarget.vessel?.rb_velocity ?? (weaponManager.slavedVelocity - Krakensbane.GetFrameVelocityV3f());
                    targetAcquired = true;
                    return;
                }

                if (weaponManager.vesselRadarData && weaponManager.vesselRadarData.locked)
                {
                    TargetSignatureData targetData = weaponManager.vesselRadarData.lockedTargetData.targetData;
                    targetVelocity = targetData.velocity - Krakensbane.GetFrameVelocityV3f();
                    targetPosition = targetData.predictedPosition;
                    targetAcceleration = targetData.acceleration;
                    if (targetData.vessel)
                    {
                        targetVelocity = targetData.vessel?.rb_velocity ?? targetVelocity;
                        targetPosition = targetData.vessel.CoM;
                    }
                    targetAcquired = true;
                    return;
                }

                //auto proxy tracking
                if (vessel.isActiveVessel && autoProxyTrackRange > 0)
                {
                    if (aptrTicker < 20)
                    {
                        aptrTicker++;

                        if (atprWasAcquired)
                        {
                            targetAcquired = true;
                            atprAcquired = true;
                        }
                    }
                    else
                    {
                        aptrTicker = 0;
                        Vessel tgt = null;
                        float closestSqrDist = autoProxyTrackRange * autoProxyTrackRange;
                        List<Vessel>.Enumerator v = BDATargetManager.LoadedVessels.GetEnumerator();
                        while (v.MoveNext())
                        {
                            if (v.Current == null || !v.Current.loaded) continue;
                            if (!v.Current.IsControllable) continue;
                            if (v.Current == vessel) continue;
                            Vector3 targetVector = v.Current.transform.position - part.transform.position;
                            if (Vector3.Dot(targetVector, fireTransforms[0].forward) < 0) continue;
                            float sqrDist = (v.Current.transform.position - part.transform.position).sqrMagnitude;
                            if (sqrDist > closestSqrDist) continue;
                            if (Vector3.Angle(targetVector, fireTransforms[0].forward) > 20) continue;
                            tgt = v.Current;
                            closestSqrDist = sqrDist;
                        }
                        v.Dispose();

                        if (tgt == null) return;
                        targetAcquired = true;
                        atprAcquired = true;
                        targetPosition = tgt.CoM;
                        targetVelocity = tgt.rb_velocity;
                    }
                }
            }
        }

        void updateAcceleration(Vector3 target_rb_velocity)
        {
            targetAcceleration = (target_rb_velocity - Krakensbane.GetLastCorrection() - targetVelocityPrevious) / Time.fixedDeltaTime;
            targetVelocityPrevious = target_rb_velocity;
        }

        void UpdateGUIWeaponState()
        {
            guiStatusString = weaponState.ToString();
        }

        private ProtoStageIconInfo InitReloadBar()
        {
            ProtoStageIconInfo v = part.stackIcon.DisplayInfo();
            v.SetMsgBgColor(XKCDColors.DarkGrey);
            v.SetMsgTextColor(XKCDColors.White);
            v.SetMessage("Reloading");
            v.SetProgressBarBgColor(XKCDColors.DarkGrey);
            v.SetProgressBarColor(XKCDColors.Silver);

            return v;
        }

        private ProtoStageIconInfo InitHeatGauge() //thanks DYJ
        {
            ProtoStageIconInfo v = part.stackIcon.DisplayInfo();

            // fix nullref if no stackicon exists
            if (v != null)
            {
                v.SetMsgBgColor(XKCDColors.DarkRed);
                v.SetMsgTextColor(XKCDColors.Orange);
                v.SetMessage("Overheat");
                v.SetProgressBarBgColor(XKCDColors.DarkRed);
                v.SetProgressBarColor(XKCDColors.Orange);
            }
            return v;
        }
		IEnumerator StartupRoutine()
        {
            weaponState = WeaponStates.PoweringUp;
            UpdateGUIWeaponState();

            if (hasDeployAnim && deployState)
            {
                deployState.enabled = true;
                deployState.speed = 1;
                while (deployState.normalizedTime < 1) //wait for animation here
                {
                    yield return null;
                }
                deployState.normalizedTime = 1;
                deployState.speed = 0;
                deployState.enabled = false;
            }

            weaponState = WeaponStates.Enabled;
            UpdateGUIWeaponState();
            BDArmorySetup.Instance.UpdateCursorState();
        }

        IEnumerator ShutdownRoutine()
        {
            weaponState = WeaponStates.PoweringDown;
            UpdateGUIWeaponState();
            BDArmorySetup.Instance.UpdateCursorState();
            if (turret)
            {
                yield return new WaitForSeconds(0.2f);

                while (!turret.ReturnTurret()) //wait till turret has returned
                {
                    yield return new WaitForFixedUpdate();
                }
            }

            if (hasDeployAnim)
            {
                deployState.enabled = true;
                deployState.speed = -1;
                while (deployState.normalizedTime > 0)
                {
                    yield return null;
                }
                deployState.normalizedTime = 0;
                deployState.speed = 0;
                deployState.enabled = false;
            }

            weaponState = WeaponStates.Disabled;
            UpdateGUIWeaponState();
        }

        void StopShutdownStartupRoutines()
        {
            if (shutdownRoutine != null)
            {
                StopCoroutine(shutdownRoutine);
                shutdownRoutine = null;
            }

            if (startupRoutine != null)
            {
                StopCoroutine(startupRoutine);
                startupRoutine = null;
            }
        }

        #endregion

        #region Bullets

        void ParseBulletDragType()
        {
            bulletDragTypeName = bulletDragTypeName.ToLower();

            switch (bulletDragTypeName)
            {
                case "none":
                    bulletDragType = BulletDragTypes.None;
                    break;

                case "numericalintegration":
                    bulletDragType = BulletDragTypes.NumericalIntegration;
                    break;

                case "analyticestimate":
                    bulletDragType = BulletDragTypes.AnalyticEstimate;
                    break;
            }
        }
        
        void SetupBulletPool()
        {
            GameObject templateBullet = new GameObject("Bullet");                                 
            templateBullet.AddComponent<PooledBullet>();
            templateBullet.SetActive(false);
            bulletPool = ObjectPool.CreateObjectPool(templateBullet, 100, true, true);
        }

        void SetupShellPool()
        {
            GameObject templateShell =
                (GameObject)Instantiate(GameDatabase.Instance.GetModel("BDArmory/Models/shell/model"));
            templateShell.SetActive(false);
            templateShell.AddComponent<ShellCasing>();
            shellPool = ObjectPool.CreateObjectPool(templateShell, 50, true, true);
        }

        void SetupBullet()
        {
            bulletInfo = BulletInfo.bullets[bulletType];
            if (bulletType != "def")
            {
                //use values from bullets.cfg if not the Part Module defaults are used
                caliber = bulletInfo.caliber;
                bulletVelocity = bulletInfo.bulletVelocity;
                bulletMass = bulletInfo.bulletMass;
                bulletDragTypeName = bulletInfo.bulletDragTypeName;
                cannonShellHeat = bulletInfo.blastHeat;
                cannonShellPower = bulletInfo.blastPower;
                cannonShellRadius = bulletInfo.blastRadius;      
            }
            ParseBulletDragType();
        }
        #endregion

        #region RMB Info

        public override string GetInfo()
        {
            BulletInfo binfo = BulletInfo.bullets[bulletType];
            StringBuilder output = new StringBuilder();
            output.Append(Environment.NewLine);
            output.AppendLine($"Weapon Type: {weaponType}");

            if (weaponType == "laser")
            {
                output.AppendLine($"Laser damage: {laserDamage}");
            }
            else
            {
                output.AppendLine($"Rounds Per Minute: {roundsPerMinute * (fireTransforms?.Length ?? 1)}");
                output.AppendLine($"Ammunition: {ammoName}");
                output.AppendLine($"Bullet type: {bulletType}");
                output.AppendLine($"Bullet mass: {Math.Round(binfo.bulletMass,2)} kg");
                output.AppendLine($"Muzzle velocity: {Math.Round(binfo.bulletVelocity, 2)} m/s");
                output.AppendLine($"Max Range: {maxEffectiveDistance} m");
                if (weaponType == "cannon" || weaponType == "ballistic")
                {
                    output.AppendLine($"Explosive: {binfo.explosive}");
                    if (binfo.explosive)
                    {
                        output.AppendLine($"Blast:");
                        output.AppendLine($"- tnt mass:  {Math.Round((binfo.tntMass > 0 ? binfo.tntMass : binfo.blastPower),2)} kg");
                        output.AppendLine($"- radius:  {Math.Round(BlastPhysicsUtils.CalculateBlastRange(binfo.tntMass), 2)} m");
                        output.AppendLine($"Air detonation: {airDetonation}");
                        if (airDetonation)
                        {
                            output.AppendLine($"- auto timing: {airDetonationTiming}");
                            output.AppendLine($"- max range: {maxAirDetonationRange} m");
                        }
                    }
                }
            }
            return output.ToString();
        }

		#endregion
	}
	#region UI //borrowing code from ModularMissile GUI
	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class WeaponGroupWindow : MonoBehaviour
	{
		internal static EventVoid OnActionGroupEditorOpened = new EventVoid("OnActionGroupEditorOpened");
		internal static EventVoid OnActionGroupEditorClosed = new EventVoid("OnActionGroupEditorClosed");

		private static GUIStyle unchanged;
		private static GUIStyle changed;
		private static GUIStyle greyed;
		private static GUIStyle overfull;

		private static WeaponGroupWindow instance;
		private static Vector3 mousePos = Vector3.zero;

		private bool ActionGroupMode;

		private Rect guiWindowRect = new Rect(0, 0, 0, 0);

		private ModuleWeapon WPNmodule;

		[KSPField] public int offsetGUIPos = -1;

		private Vector2 scrollPos;

		[KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "Show Group Editor"), UI_Toggle(enabledText = "close Group GUI", disabledText = "open Group GUI")] [NonSerialized] public bool showRFGUI;

		private bool styleSetup;

		private string txtName = string.Empty;

		public static void HideGUI()
		{
			if (instance != null && instance.WPNmodule != null)
			{
				instance.WPNmodule.WeaponName = instance.WPNmodule.shortName;
				instance.WPNmodule = null;
				instance.UpdateGUIState();
			}
			EditorLogic editor = EditorLogic.fetch;
			if (editor != null)
				editor.Unlock("BD_MN_GUILock");
		}

		public static void ShowGUI(ModuleWeapon WPNmodule)
		{
			if (instance != null)
			{
				instance.WPNmodule = WPNmodule;
				instance.UpdateGUIState();
			}
		}

		private void UpdateGUIState()
		{
			enabled = WPNmodule != null;
			EditorLogic editor = EditorLogic.fetch;
			if (!enabled && editor != null)
				editor.Unlock("BD_MN_GUILock");
		}
		private IEnumerator<YieldInstruction> CheckActionGroupEditor()
		{
			while (EditorLogic.fetch == null)
			{
				yield return null;
			}
			EditorLogic editor = EditorLogic.fetch;
			while (EditorLogic.fetch != null)
			{
				if (editor.editorScreen == EditorScreen.Actions)
				{
					if (!ActionGroupMode)
					{
						HideGUI();
						OnActionGroupEditorOpened.Fire();
					}
					EditorActionGroups age = EditorActionGroups.Instance;
					if (WPNmodule && !age.GetSelectedParts().Contains(WPNmodule.part))
					{
						HideGUI();
					}
					ActionGroupMode = true;
				}
				else
				{
					if (ActionGroupMode)
					{
						HideGUI();
						OnActionGroupEditorClosed.Fire();
					}
					ActionGroupMode = false;
				}
				yield return null;
			}
		}
		private void Awake()
		{
			enabled = false;
			instance = this;
		}

		private void OnDestroy()
		{
			instance = null;
		}

		public void OnGUI()
		{
			if (!styleSetup)
			{
				styleSetup = true;
				Styles.InitStyles();
			}

			EditorLogic editor = EditorLogic.fetch;
			if (!HighLogic.LoadedSceneIsEditor || !editor)
			{
				return;
			}
			bool cursorInGUI = false; // nicked the locking code from Ferram
			mousePos = Input.mousePosition; //Mouse location; based on Kerbal Engineer Redux code
			mousePos.y = Screen.height - mousePos.y;

			int posMult = 0;
			if (offsetGUIPos != -1)
			{
				posMult = offsetGUIPos;
			}
			if (ActionGroupMode)
			{
				if (guiWindowRect.width == 0)
				{
					guiWindowRect = new Rect(430 * posMult, 365, 438, 50);
				}
				new Rect(guiWindowRect.xMin + 440, mousePos.y - 5, 300, 20);
			}
			else
			{
				if (guiWindowRect.width == 0)
				{
					//guiWindowRect = new Rect(Screen.width - 8 - 430 * (posMult + 1), 365, 438, (Screen.height - 365));
					guiWindowRect = new Rect(Screen.width - 8 - 430 * (posMult + 1), 365, 438, 50);
				}
				new Rect(guiWindowRect.xMin - (230 - 8), mousePos.y - 5, 220, 20);
			}
			cursorInGUI = guiWindowRect.Contains(mousePos);
			if (cursorInGUI)
			{
				editor.Lock(false, false, false, "BD_MN_GUILock");
				//if (EditorTooltip.Instance != null)
				//    EditorTooltip.Instance.HideToolTip();
			}
			else
			{
				editor.Unlock("BD_MN_GUILock");
			}
			guiWindowRect = GUILayout.Window(GetInstanceID(), guiWindowRect, GUIWindow, "Weapon Group GUI", Styles.styleEditorPanel);
		}

		public void GUIWindow(int windowID)
		{
			InitializeStyles();

			GUILayout.BeginVertical();
			GUILayout.Space(20);

			GUILayout.BeginHorizontal();

			GUILayout.Label("Add to Weapon Group: ");


			txtName = GUILayout.TextField(txtName);


			if (GUILayout.Button("Save & Close"))
			{
				WPNmodule.WeaponName = txtName;
				WPNmodule.shortName = txtName;  
				instance.WPNmodule.HideUI();
			}

			GUILayout.EndHorizontal();

			scrollPos = GUILayout.BeginScrollView(scrollPos);

			GUILayout.EndScrollView();

			GUILayout.EndVertical();

			GUI.DragWindow();
			BDGUIUtils.RepositionWindow(ref guiWindowRect);
		}

		private static void InitializeStyles()
		{
			if (unchanged == null)
			{
				if (GUI.skin == null)
				{
					unchanged = new GUIStyle();
					changed = new GUIStyle();
					greyed = new GUIStyle();
					overfull = new GUIStyle();
				}
				else
				{
					unchanged = new GUIStyle(GUI.skin.textField);
					changed = new GUIStyle(GUI.skin.textField);
					greyed = new GUIStyle(GUI.skin.textField);
					overfull = new GUIStyle(GUI.skin.label);
				}

				unchanged.normal.textColor = Color.white;
				unchanged.active.textColor = Color.white;
				unchanged.focused.textColor = Color.white;
				unchanged.hover.textColor = Color.white;

				changed.normal.textColor = Color.yellow;
				changed.active.textColor = Color.yellow;
				changed.focused.textColor = Color.yellow;
				changed.hover.textColor = Color.yellow;

				greyed.normal.textColor = Color.gray;

				overfull.normal.textColor = Color.red;
			}
		}
	}
	#endregion
}
