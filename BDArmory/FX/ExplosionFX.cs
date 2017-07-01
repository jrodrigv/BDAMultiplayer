using System.Collections.Generic;
using System.Linq;
using BDArmory.Core;
using BDArmory.Core.Extension;
using BDArmory.Events;
using BDArmory.Misc;
using BDArmory.Parts;
using BDArmory.UI;
using UnityEngine;

namespace BDArmory.FX
{
    public class ExplosionFx : MonoBehaviour
    {
        KSPParticleEmitter[] _pEmitters;
        Light _lightFx;
        float _startTime;
        public AudioClip ExSound;
        public AudioSource AudioSource;
        float _maxTime;

        public float Range;

        void Start()
        {
            _startTime = Time.time;
            _pEmitters = gameObject.GetComponentsInChildren<KSPParticleEmitter>();
            List<KSPParticleEmitter>.Enumerator pe = _pEmitters.ToList().GetEnumerator();
            while (pe.MoveNext())
            {
                if (pe.Current == null) continue;
               EffectBehaviour.AddParticleEmitter(pe.Current);
                
                pe.Current.emit = true;

                if (pe.Current.maxEnergy > _maxTime)
                {
                    _maxTime = pe.Current.maxEnergy;
                }
            }
            pe.Dispose();

            _lightFx = gameObject.AddComponent<Light>();
            _lightFx.color = Misc.Misc.ParseColor255("255,238,184,255");
            _lightFx.intensity = 8;
            _lightFx.range = Range*3f;
            _lightFx.shadows = LightShadows.None;


            AudioSource.volume = BDArmorySettings.BDARMORY_WEAPONS_VOLUME;

            AudioSource.PlayOneShot(ExSound);
        }

        void FixedUpdate()
        {
            _lightFx.intensity -= 12*Time.fixedDeltaTime;
            if (Time.time - _startTime > 0.2f)
            {
                List<KSPParticleEmitter>.Enumerator pe = _pEmitters.ToList().GetEnumerator();
                while (pe.MoveNext())
                {
                    if (pe.Current == null) continue;
                    pe.Current.emit = false;
                }
                pe.Dispose();
            }

            if (Time.time - _startTime > _maxTime)
            {
                Destroy(gameObject);
            }
        }

        public static GameObject CreateExplosionAnimation(Vector3 position, float radius, float power, string explModelPath, string soundPath, bool shouldNotify = true)
        {
            if (shouldNotify)
            {
                Dependencies.Get<ExplosionEventService>().PublishExplosionEvent(position,radius,power,explModelPath,soundPath);
            }

            GameObject go = GameDatabase.Instance.GetModel(explModelPath);
            AudioClip soundClip = GameDatabase.Instance.GetAudioClip(soundPath);


            Quaternion rotation = Quaternion.LookRotation(VectorUtils.GetUpDirection(position));
            GameObject newExplosion = (GameObject)Instantiate(go, position, rotation);
            newExplosion.SetActive(true);
            ExplosionFx eFx = newExplosion.AddComponent<ExplosionFx>();
            eFx.ExSound = soundClip;
            eFx.AudioSource = newExplosion.AddComponent<AudioSource>();
            eFx.AudioSource.minDistance = 200;
            eFx.AudioSource.maxDistance = 5500;
            eFx.AudioSource.spatialBlend = 1;
            eFx.Range = radius;

            if (power <= 5)
            {
                eFx.AudioSource.minDistance = 4f;
                eFx.AudioSource.maxDistance = 3000;
                eFx.AudioSource.priority = 9999;
            }
            return newExplosion;
        }


        public static void CreateExplosion(Vector3 position, float radius, float power, float heat, Vessel sourceVessel
            , string explModelPath, string soundPath)
        {

            GameObject newExplosion = CreateExplosionAnimation(position, radius, power, explModelPath, soundPath);

            IEnumerator<KSPParticleEmitter> pe = newExplosion.GetComponentsInChildren<KSPParticleEmitter>().Cast<KSPParticleEmitter>()
                .GetEnumerator();
            while (pe.MoveNext())
            {
                if (pe.Current == null) continue;
                pe.Current.emit = true;
                
            }
            pe.Dispose();

            DoExplosionDamage(position, power, heat, radius, sourceVessel);
        }

        public static float ExplosionHeatMultiplier = 4200;
        public static float ExplosionImpulseMultiplier = 1.5f;

		public static void DoExplosionRay(Ray ray, float power, float heat, float maxDistance, ref List<Part> ignoreParts, ref List<DestructibleBuilding> ignoreBldgs, Vessel sourceVessel = null)
		{
			RaycastHit rayHit;
			if(Physics.Raycast(ray, out rayHit, maxDistance, 557057))
			{
				float sqrDist = (rayHit.point - ray.origin).sqrMagnitude;
				float sqrMaxDist = maxDistance * maxDistance;
				float distanceFactor = Mathf.Clamp01((sqrMaxDist - sqrDist) / sqrMaxDist);
				//parts
				Part part = rayHit.collider.GetComponentInParent<Part>();
				if(part)
				{
					Vessel missileSource = null;
					if(sourceVessel != null)
					{
                        MissileBase ml = part.FindModuleImplementing<MissileBase>();
						if(ml)
						{
							missileSource = ml.SourceVessel;
						}
					}


                    if (!ignoreParts.Contains(part) && part.physicalSignificance == Part.PhysicalSignificance.FULL &&
                        (!sourceVessel || sourceVessel != missileSource))
                    {
                        ignoreParts.Add(part);
                        Rigidbody rb = part.GetComponent<Rigidbody>();
                        if (rb)
                        {
                            rb.AddForceAtPosition(ray.direction*power*distanceFactor*ExplosionImpulseMultiplier,
                                rayHit.point, ForceMode.Impulse);
                        }
                        if (heat < 0)
                        {
                            heat = power;
                        }
                        float heatDamage = (BDArmorySettings.DMG_MULTIPLIER/100)*ExplosionHeatMultiplier*heat*
                                           distanceFactor/part.crashTolerance;
                        float excessHeat = Mathf.Max(0, (float) (part.temperature + heatDamage - part.maxTemp));
                        part.AddDamage(heatDamage);
                        if (BDArmorySettings.DRAW_DEBUG_LABELS)
                            Debug.Log("[BDArmory]:====== Explosion ray hit part! Damage: " + heatDamage);
                        if (excessHeat > 0 && part.parent)
                        {
                            part.parent.AddDamage(excessHeat);
                        }
                        return;
                    }
                }

                //buildings
                DestructibleBuilding building = rayHit.collider.GetComponentInParent<DestructibleBuilding>();
                if (building && !ignoreBldgs.Contains(building))
                {
                    ignoreBldgs.Add(building);
                    float damageToBuilding = (BDArmorySettings.DMG_MULTIPLIER/100)*ExplosionHeatMultiplier*0.00645f*
                                             power*distanceFactor;
                    if (damageToBuilding > building.impactMomentumThreshold/10) building.AddDamage(damageToBuilding);
                    if (building.Damage > building.impactMomentumThreshold) building.Demolish();
                    if (BDArmorySettings.DRAW_DEBUG_LABELS)
                        Debug.Log("[BDArmory]:== Explosion hit destructible building! Damage: " +
                                  (damageToBuilding).ToString("0.00") + ", total Damage: " + building.Damage);
                }
            }
        }

        public static List<Part> IgnoreParts = new List<Part>();
        public static List<DestructibleBuilding> IgnoreBuildings = new List<DestructibleBuilding>();

		public static void DoExplosionDamage(Vector3 position, float power, float heat, float maxDistance, Vessel sourceVessel)
		{
			if(BDArmorySettings.DRAW_DEBUG_LABELS) Debug.Log("[BDArmory]: ======= Doing explosion sphere =========");
			IgnoreParts.Clear();
			IgnoreBuildings.Clear();

            // Unity does not like linq.  changing to an enumeration to extract needed lists.
            #region Old code (For reference.  remove when satisfied new code works as expected)
            //var vesselsAffected =
            //    BDATargetManager.LoadedVessels.Where(
            //        v => v != null && v.loaded && !v.packed && (v.CoM - position).magnitude < maxDistance * 4);

            //var partsAffected =
            //    vesselsAffected.SelectMany(v => v.parts).Where(p => p != null && p && (p.transform.position - position).magnitude < maxDistance);

            //foreach (var part in partsAffected)
            //{
            //    DoExplosionRay(new Ray(position, part.transform.TransformPoint(part.CoMOffset) - position), power, heat, maxDistance, ref ignoreParts, ref ignoreBuildings, sourceVessel);
            //}

            //foreach (var bldg in BDATargetManager.LoadedBuildings)
            //{
            //    if (bldg == null) continue;
            //    if ((bldg.transform.position - position).magnitude < maxDistance * 1000)
            //    {
            //        DoExplosionRay(new Ray(position, bldg.transform.position - position), power, heat, maxDistance, ref ignoreParts, ref ignoreBuildings);
            //    }
            //}
            #endregion

            // this replaces 2 passes through the vessels list and 2 passes through the parts lists with a single pass, and eliminates boxing and unboxing performed by linq and foreach loops.  Should be faster, with better gc
            List<Vessel>.Enumerator v = BDATargetManager.LoadedVessels.GetEnumerator();
		    while (v.MoveNext())
		    {
		        if (v.Current == null) continue;
                if (!v.Current.loaded || v.Current.packed || (v.Current.CoM - position).magnitude >= maxDistance * 4) continue;
		        List<Part>.Enumerator p = v.Current.parts.GetEnumerator();
		        while (p.MoveNext())
		        {
		            if (p.Current == null) continue;
		            if ((p.Current.transform.position - position).magnitude >= maxDistance) continue;
		            DoExplosionRay(new Ray(position, p.Current.transform.TransformPoint(p.Current.CoMOffset) - position), power, heat, maxDistance, ref IgnoreParts, ref IgnoreBuildings, sourceVessel);
		        }
                p.Dispose();
		    }
            v.Dispose();

		    List<DestructibleBuilding>.Enumerator bldg = BDATargetManager.LoadedBuildings.GetEnumerator();
			while(bldg.MoveNext())
			{
				if(bldg.Current == null) continue;
				if((bldg.Current.transform.position - position).magnitude < maxDistance * 1000)
				{
					DoExplosionRay(new Ray(position, bldg.Current.transform.position - position), power, heat, maxDistance, ref IgnoreParts, ref IgnoreBuildings);
				}
			}
            bldg.Dispose();
		}
	}
}

