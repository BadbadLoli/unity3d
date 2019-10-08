using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace PathologicalGames
{
    [AddComponentMenu("Path-o-logical/PoolManager/SpawnPool")]
    public sealed class SpawnPool : MonoBehaviour, IList<Transform>
    {
        #region Inspector Parameters
        public string poolName = "";
        public bool matchPoolScale = false;
        public bool matchPoolLayer = false;
        public bool dontReparent = false;
        public bool dontDestroyOnLoad
		{
			get
			{
				return this._dontDestroyOnLoad;
			}
			set
			{
				this._dontDestroyOnLoad = value;
				
				if (this.group != null)
					Object.DontDestroyOnLoad(this.group.gameObject);
			}
		}
        public bool _dontDestroyOnLoad = false;
        public bool logMessages = false;
        public List<PrefabPool> _perPrefabPoolOptions = new List<PrefabPool>();
        public Dictionary<object, bool> prefabsFoldOutStates = new Dictionary<object, bool>();
        #endregion Inspector Parameters
        #region Public Code-only Parameters
        public float maxParticleDespawnTime = 300;
        public Transform group { get; private set; }
        public PrefabsDict prefabs = new PrefabsDict();
        public Dictionary<object, bool> _editorListItemStates = new Dictionary<object, bool>();

        public Dictionary<string, PrefabPool> prefabPools
        {
            get
            {
                var dict = new Dictionary<string, PrefabPool>();

                for (int i = 0; i < this._prefabPools.Count; i++)
                    dict[this._prefabPools[i].prefabGO.name] = this._prefabPools[i];
                return dict;
            }
        }
        #endregion Public Code-only Parameters
        #region Private Properties
        private List<PrefabPool> _prefabPools = new List<PrefabPool>();
        internal List<Transform> _spawned = new List<Transform>();
        #endregion Private Properties
        #region Constructor and Init
        private void Awake()
        {
            if (this._dontDestroyOnLoad) Object.DontDestroyOnLoad(this.gameObject);

            this.group = this.transform;

            if (this.poolName == "")
            {
                this.poolName = this.group.name.Replace("Pool", "");
                this.poolName = this.poolName.Replace("(Clone)", "");
            }

            if (this.logMessages)
                Debug.Log(string.Format("SpawnPool {0}: Initializing..", this.poolName));

            for (int i = 0; i < this._perPrefabPoolOptions.Count; i++)
            {
                if (this._perPrefabPoolOptions[i].prefab == null)
                {
                    Debug.LogWarning(string.Format("Initialization Warning: Pool '{0}' " +
                              "contains a PrefabPool with no prefab reference. Skipping.",
                               this.poolName));
                    continue;
                }

                this._perPrefabPoolOptions[i].inspectorInstanceConstructor();
                this.CreatePrefabPool(this._perPrefabPoolOptions[i]);
            }

            PoolManager.Pools.Add(this);
        }

		public delegate GameObject InstantiateDelegate(GameObject prefab, Vector3 pos, Quaternion rot);
		public delegate void DestroyDelegate(GameObject instance);
		public InstantiateDelegate instantiateDelegates;
		public DestroyDelegate destroyDelegates;
		internal GameObject InstantiatePrefab(GameObject prefab, Vector3 pos, Quaternion rot)
		{
			if (this.instantiateDelegates != null)
			{
				return this.instantiateDelegates(prefab, pos, rot);
			}
			else
			{
				return InstanceHandler.InstantiatePrefab(prefab, pos, rot);
			}
		}
		
		internal void DestroyInstance(GameObject instance)
		{
			if (this.destroyDelegates != null)
			{
				this.destroyDelegates(instance);
			}
			else
			{
				InstanceHandler.DestroyInstance(instance);
			}
		}

        private void OnDestroy()
        {
            if (this.logMessages)
                Debug.Log(string.Format("SpawnPool {0}: Destroying...", this.poolName));

			if (PoolManager.Pools.ContainsValue(this))
				PoolManager.Pools.Remove(this);

            this.StopAllCoroutines();
            this._spawned.Clear();

            foreach (PrefabPool pool in this._prefabPools) 
			{
				pool.SelfDestruct();
			}

            this._prefabPools.Clear();
            this.prefabs._Clear();
        }

		public void CreatePrefabPool(PrefabPool prefabPool)
		{
			bool isAlreadyPool = this.GetPrefabPool(prefabPool.prefab) == null ? false : true;
			if (isAlreadyPool)
				throw new System.Exception(string.Format
            	(
					"Prefab '{0}' is already in  SpawnPool '{1}'. Prefabs can be in more than 1 SpawnPool but " +
					"cannot be in the same SpawnPool twice.",
					prefabPool.prefab, 
					this.poolName
				));

			prefabPool.spawnPool = this;
			this._prefabPools.Add(prefabPool);
			this.prefabs._Add(prefabPool.prefab.name, prefabPool.prefab);

			if (prefabPool.preloaded != true)
			{
				if (this.logMessages)
					Debug.Log(string.Format
					(
						"SpawnPool {0}: Preloading {1} {2}",
						this.poolName,
						prefabPool.preloadAmount,
						prefabPool.prefab.name
					));
				
				prefabPool.PreloadInstances();
			}
		}

        public void Add(Transform instance, string prefabName, bool despawn, bool parent)
        {
            for (int i = 0; i < this._prefabPools.Count; i++)
            {
                if (this._prefabPools[i].prefabGO == null)
                {
                    Debug.LogError("Unexpected Error: PrefabPool.prefabGO is null");
                    return;
                }

                if (this._prefabPools[i].prefabGO.name == prefabName)
                {
                    this._prefabPools[i].AddUnpooled(instance, despawn);

                    if (this.logMessages)
                        Debug.Log(string.Format(
                                "SpawnPool {0}: Adding previously unpooled instance {1}",
                                                this.poolName,
                                                instance.name));

                    if (parent) 
					{
						var worldPositionStays = !(instance is RectTransform);
						instance.SetParent(this.group, worldPositionStays);
					}

                    if (!despawn) this._spawned.Add(instance);
                    return;
                }
            }

            Debug.LogError(string.Format("SpawnPool {0}: PrefabPool {1} not found.", this.poolName, prefabName));

        }
        #endregion Constructor and Init
        #region List Overrides
        public void Add(Transform item)
        {
            string msg = "Use SpawnPool.Spawn() to properly add items to the pool.";
            throw new System.NotImplementedException(msg);
        }

        public void Remove(Transform item)
        {
            string msg = "Use Despawn() to properly manage items that should " +
                         "remain in the pool but be deactivated.";
            throw new System.NotImplementedException(msg);
        }

        #endregion List Overrides
        #region Pool Functionality
        public Transform Spawn(Transform prefab, Vector3 pos, Quaternion rot, Transform parent)
        {
            Transform inst;
			bool worldPositionStays;
            #region Use from Pool
            for (int i = 0; i < this._prefabPools.Count; i++)
            {
                if (this._prefabPools[i].prefabGO == prefab.gameObject)
                {
                    inst = this._prefabPools[i].SpawnInstance(pos, rot);
                    if (inst == null) return null;
					worldPositionStays = !(inst is RectTransform);
					if (parent != null)
					{
						inst.SetParent(parent, worldPositionStays);
					}
                    else if (!this.dontReparent && inst.parent != this.group)
					{
						inst.SetParent(this.group, worldPositionStays);
					}
                    this._spawned.Add(inst);
	                inst.gameObject.BroadcastMessage(
						"OnSpawned",
						this,
						SendMessageOptions.DontRequireReceiver
					);
                    return inst;
                }
            }
            #endregion Use from Pool
            #region New PrefabPool
            PrefabPool newPrefabPool = new PrefabPool(prefab);
            this.CreatePrefabPool(newPrefabPool);
            inst = newPrefabPool.SpawnInstance(pos, rot);
			worldPositionStays = !(inst is RectTransform);
			if (parent != null)
			{
				inst.SetParent(parent, worldPositionStays);
			}
			else if (!this.dontReparent && inst.parent != this.group)
			{
				inst.SetParent(this.group, worldPositionStays);
			}
            this._spawned.Add(inst);
            #endregion New PrefabPool
            inst.gameObject.BroadcastMessage(
				"OnSpawned",
				this,
				SendMessageOptions.DontRequireReceiver
			);
            return inst;
        }
        public Transform Spawn(Transform prefab, Vector3 pos, Quaternion rot)
        {
            Transform inst = this.Spawn(prefab, pos, rot, null);
            if (inst == null) return null;
            return inst;
        }
        public Transform Spawn(Transform prefab)
        {
            return this.Spawn(prefab, Vector3.zero, Quaternion.identity);
        }
        public Transform Spawn(Transform prefab, Transform parent)
        {
            return this.Spawn(prefab, Vector3.zero, Quaternion.identity, parent);
        }
		
		
		#region GameObject Overloads
		public Transform Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent)
		{
			return Spawn(prefab.transform, pos, rot, parent);
		}
		
		public Transform Spawn(GameObject prefab, Vector3 pos, Quaternion rot)
		{
			return Spawn(prefab.transform, pos, rot);
		}
		
		public Transform Spawn(GameObject prefab)
		{
			return Spawn(prefab.transform);
		}
		
		public Transform Spawn(GameObject prefab, Transform parent)
		{
			return Spawn(prefab.transform, parent);
		}
		#endregion GameObject Overloads
		
		
        /// <summary>
        /// See primary Spawn method for documentation.
        /// 
        /// Overload to take only a prefab name. The cached reference is pulled  
        /// from the SpawnPool.prefabs dictionary.
        /// </summary>
        public Transform Spawn(string prefabName)
        {
            Transform prefab = this.prefabs[prefabName];
            return this.Spawn(prefab);
        }


        /// <summary>
        /// See primary Spawn method for documentation.
        /// 
        /// Convienince overload to take only a prefab name and parent the new 
        /// instance under the given parent
        /// </summary>
        public Transform Spawn(string prefabName, Transform parent)
        {
            Transform prefab = this.prefabs[prefabName];
            return this.Spawn(prefab, parent);
        }


        /// <summary>
        /// See primary Spawn method for documentation.
        /// 
        /// Overload to take only a prefab name. The cached reference is pulled from 
        /// the SpawnPool.prefabs dictionary. An instance will be set to the passed 
        /// position and rotation.
        /// </summary>
        public Transform Spawn(string prefabName, Vector3 pos, Quaternion rot)
        {
            Transform prefab = this.prefabs[prefabName];
            return this.Spawn(prefab, pos, rot);
        }


        /// <summary>
        /// See primary Spawn method for documentation.
        /// 
        /// Convienince overload to take only a prefab name and parent the new 
        /// instance under the given parent. An instance will be set to the passed 
        /// position and rotation.
        /// </summary>
        public Transform Spawn(string prefabName, Vector3 pos, Quaternion rot, 
                               Transform parent)
        {
            Transform prefab = this.prefabs[prefabName];
            return this.Spawn(prefab, pos, rot, parent);
        }


        public AudioSource Spawn(AudioSource prefab,
                            Vector3 pos, Quaternion rot)
        {
            return this.Spawn(prefab, pos, rot, null);  // parent = null
        }


        public AudioSource Spawn(AudioSource prefab)
        {
            return this.Spawn
            (
                prefab, 
                Vector3.zero, Quaternion.identity,
                null  // parent = null
            );
        }
		
	 	
		public AudioSource Spawn(AudioSource prefab, Transform parent)
        {
            return this.Spawn
            (
                prefab, 
                Vector3.zero, 
				Quaternion.identity,
                parent
            );
        }
		
		
        public AudioSource Spawn(AudioSource prefab,
                            	 Vector3 pos, Quaternion rot,
                            	 Transform parent)
        {
            // Instance using the standard method before doing audio stuff
            Transform inst = Spawn(prefab.transform, pos, rot, parent);

            // Can happen if limit was used
            if (inst == null) return null;

            // Get the emitter and start it
            var src = inst.GetComponent<AudioSource>();
            src.Play();

            this.StartCoroutine(this.ListForAudioStop(src));

            return src;
        }


        /// <summary>
        ///	See docs for SpawnInstance(Transform prefab, Vector3 pos, Quaternion rot)
        ///	for basic functionalty information.
        ///		
        /// Pass a ParticleSystem component of a prefab to instantiate, trigger 
        /// emit, then listen for when all particles have died to "auto-destruct", 
        /// but instead of destroying the game object it will be deactivated and 
        /// added to the pool to be reused.
        /// 
        /// IMPORTANT: 
        ///     * You must pass a ParticleSystem next time as well, or the emitter
        ///       will be treated as a regular prefab and simply activate, but emit
        ///       will not be triggered!
        ///     * The listner that waits for the death of all particles will 
        ///       time-out after a set number of seconds and log a warning. 
        ///       This is done to keep the developer aware of any unexpected 
        ///       usage cases. Change the public property "maxParticleDespawnTime"
        ///       to adjust this length of time.
        /// 
        /// Broadcasts "OnSpawned" to the instance. Use this instead of Awake()
        ///		
        /// This function has the same initial signature as Unity's Instantiate() 
        /// that takes position and rotation. The return Type is different though.
        /// </summary>
        public ParticleSystem Spawn(ParticleSystem prefab,
                                    Vector3 pos, Quaternion rot)
        {
            return Spawn(prefab, pos, rot, null);  // parent = null

        }

        /// <summary>
        /// See primary Spawn ParticleSystem method for documentation.
        /// 
        /// Convienince overload to take only a prefab name and parent the new 
        /// instance under the given parent. An instance will be set to the passed 
        /// position and rotation.
        /// </summary>
        public ParticleSystem Spawn(ParticleSystem prefab,
                                    Vector3 pos, Quaternion rot,
                                    Transform parent)
        {
            // Instance using the standard method before doing particle stuff
            Transform inst = this.Spawn(prefab.transform, pos, rot, parent);

            // Can happen if limit was used
            if (inst == null) return null;

            // Get the emitter and start it
            var emitter = inst.GetComponent<ParticleSystem>();
            //emitter.Play(true);  // Seems to auto-play on activation so this may not be needed

            this.StartCoroutine(this.ListenForEmitDespawn(emitter));

            return emitter;
        }



        /// <summary>
        ///	If the passed object is managed by the SpawnPool, it will be 
        ///	deactivated and made available to be spawned again.
        ///		
        /// Despawned instances are removed from the primary list.
        /// </summary>
        /// <param name="item">The transform of the gameobject to process</param>
        public void Despawn(Transform instance)
        {
            // Find the item and despawn it
            bool despawned = false;
            for (int i = 0; i < this._prefabPools.Count; i++)
            {
                if (this._prefabPools[i]._spawned.Contains(instance))
                {
                    despawned = this._prefabPools[i].DespawnInstance(instance);
                    break;
                }  // Protection - Already despawned?
                else if (this._prefabPools[i]._despawned.Contains(instance))
                {
                    Debug.LogError(
                        string.Format("SpawnPool {0}: {1} has already been despawned. " +
                                       "You cannot despawn something more than once!",
                                        this.poolName,
                                        instance.name));
                    return;
                }
            }

            // If still false, then the instance wasn't found anywhere in the pool
            if (!despawned)
            {
                Debug.LogError(string.Format("SpawnPool {0}: {1} not found in SpawnPool",
                               this.poolName,
                               instance.name));
                return;
            }

            // Remove from the internal list. Only active instances are kept. 
            // 	 This isn't needed for Pool functionality. It is just done 
            //	 as a user-friendly feature which has been needed before.
            this._spawned.Remove(instance);
        }


        /// <summary>
        ///	See docs for Despawn(Transform instance) for basic functionalty information.
        ///		
        /// Convienince overload to provide the option to re-parent for the instance 
        /// just before despawn.
        /// </summary>
        public void Despawn(Transform instance, Transform parent)
        {
			// Spawn the new instance (Note: prefab already set in PrefabPool)
			bool worldPositionStays = !(instance is RectTransform);
			instance.SetParent(parent, worldPositionStays);
            this.Despawn(instance);
        }


        /// <description>
        /// See docs for Despawn(Transform instance). This expands that functionality.
        ///   If the passed object is managed by this SpawnPool, it will be 
        ///   deactivated and made available to be spawned again.
        /// </description>
        /// <param name="item">The transform of the instance to process</param>
        /// <param name="seconds">The time in seconds to wait before despawning</param>
        public void Despawn(Transform instance, float seconds)
        {
            this.StartCoroutine(this.DoDespawnAfterSeconds(instance, seconds, false, null));
        }


        /// <summary>
        ///	See docs for Despawn(Transform instance) for basic functionalty information.
        ///		
        /// Convienince overload to provide the option to re-parent for the instance 
        /// just before despawn.
        /// </summary>
        public void Despawn(Transform instance, float seconds, Transform parent)
        {
            this.StartCoroutine(this.DoDespawnAfterSeconds(instance, seconds, true, parent));
        }


        /// <summary>
        /// Waits X seconds before despawning. See the docs for DespawnAfterSeconds()
        /// the argument useParent is used because a null parent is valid in Unity. It will 
        /// make the scene root the parent
        /// </summary>
        private IEnumerator DoDespawnAfterSeconds(Transform instance, float seconds, bool useParent, Transform parent)
        {
            GameObject go = instance.gameObject;
            while (seconds > 0)
            {
                yield return null;

                // If the instance was deactivated while waiting here, just quit
                if (!go.activeInHierarchy)
                    yield break;
                
                seconds -= Time.deltaTime;
            }

            if (useParent)
                this.Despawn(instance, parent);
            else
                this.Despawn(instance);
        }


        /// <description>
        /// Despawns all active instances in this SpawnPool
        /// </description>
        public void DespawnAll()
        {
            var spawned = new List<Transform>(this._spawned);
            for (int i = 0; i < spawned.Count; i++)
                this.Despawn(spawned[i]);
        }


        /// <description>
        ///	Returns true if the passed transform is currently spawned.
        /// </description>
        /// <param name="item">The transform of the gameobject to test</param>
        public bool IsSpawned(Transform instance)
        {
            return this._spawned.Contains(instance);
        }

        #endregion Pool Functionality
        #region Utility Functions
        public PrefabPool GetPrefabPool(Transform prefab)
        {
            for (int i = 0; i < this._prefabPools.Count; i++)
            {
                if (this._prefabPools[i].prefabGO == null)
                    Debug.LogError(string.Format("SpawnPool {0}: PrefabPool.prefabGO is null", this.poolName));
                if (this._prefabPools[i].prefabGO == prefab.gameObject)
                    return this._prefabPools[i];
            }
            return null;
        }
        public PrefabPool GetPrefabPool(GameObject prefab)
        {
            for (int i = 0; i < this._prefabPools.Count; i++)
            {
                if (this._prefabPools[i].prefabGO == null)
                    Debug.LogError(string.Format("SpawnPool {0}: PrefabPool.prefabGO is null", this.poolName));

                if (this._prefabPools[i].prefabGO == prefab)
                    return this._prefabPools[i];
            }
            return null;
        }
        public Transform GetPrefab(Transform instance)
        {
            for (int i = 0; i < this._prefabPools.Count; i++)
                if (this._prefabPools[i].Contains(instance))
                    return this._prefabPools[i].prefab;
            return null;
        }
        public GameObject GetPrefab(GameObject instance)
        {
            for (int i = 0; i < this._prefabPools.Count; i++)
                if (this._prefabPools[i].Contains(instance.transform))
                    return this._prefabPools[i].prefabGO;
            return null;
        }
        private IEnumerator ListForAudioStop(AudioSource src)
        {
            yield return null;
			GameObject srcGameObject = src.gameObject;
            while (src.isPlaying)
			{
                yield return null;
			}
			if (!srcGameObject.activeInHierarchy)
			{
				src.Stop();
				yield break;
			}
            this.Despawn(src.transform);
        }
        private IEnumerator ListenForEmitDespawn(ParticleSystem emitter)
        {
			yield return new WaitForSeconds(emitter.main.startDelay.constantMax + 0.25f);
            float safetimer = 0;   // Just in case! See Spawn() for more info
			GameObject emitterGO = emitter.gameObject;
			while (emitter.IsAlive(true) && emitterGO.activeInHierarchy)
            {
                safetimer += Time.deltaTime;
                if (safetimer > this.maxParticleDespawnTime)
                    Debug.LogWarning
                    (
                        string.Format
                        (
                            "SpawnPool {0}: " +
                                "Timed out while listening for all particles to die. " +
                                "Waited for {1}sec.",
                            this.poolName,
                            this.maxParticleDespawnTime
                        )
                    );

                yield return null;
            }
			if (emitterGO.activeInHierarchy)
			{
				this.Despawn(emitter.transform);
				emitter.Clear(true);
			}
        }

        #endregion Utility Functions
        public override string ToString()
        {
            var name_list = new List<string>();
            foreach (Transform item in this._spawned)
                name_list.Add(item.name);
            return System.String.Join(", ", name_list.ToArray());
        }
        public Transform this[int index]
        {
            get { return this._spawned[index]; }
            set { throw new System.NotImplementedException("Read-only."); }
        }

        /// <summary>
        /// The name "Contains" is misleading so IsSpawned was implimented instead.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(Transform item)
        {
            string message = "Use IsSpawned(Transform instance) instead.";
            throw new System.NotImplementedException(message);
        }


        /// <summary>
        /// Used by OTHERList.AddRange()
        /// This adds this list to the passed list
        /// </summary>
        /// <param name="array">The list AddRange is being called on</param>
        /// <param name="arrayIndex">
        /// The starting index for the copy operation. AddRange seems to pass the last index.
        /// </param>
        public void CopyTo(Transform[] array, int arrayIndex)
        {
            this._spawned.CopyTo(array, arrayIndex);
        }


        /// <summary>
        /// Returns the number of items in this (the collection). Readonly.
        /// </summary>
        public int Count
        {
            get { return this._spawned.Count; }
        }


        /// <summary>
        /// Impliments the ability to use this list in a foreach loop
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Transform> GetEnumerator()
        {
            for (int i = 0; i < this._spawned.Count; i++)
                yield return this._spawned[i];
        }

        /// <summary>
        /// Impliments the ability to use this list in a foreach loop
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            for (int i = 0; i < this._spawned.Count; i++)
                yield return this._spawned[i];
        }

        // Not implemented
        public int IndexOf(Transform item) { throw new System.NotImplementedException(); }
        public void Insert(int index, Transform item) { throw new System.NotImplementedException(); }
        public void RemoveAt(int index) { throw new System.NotImplementedException(); }
        public void Clear() { throw new System.NotImplementedException(); }
        public bool IsReadOnly { get { throw new System.NotImplementedException(); } }
        bool ICollection<Transform>.Remove(Transform item) { throw new System.NotImplementedException(); }

    }

    [System.Serializable]
    public class PrefabPool
    {

        #region Public Properties Available in the Editor
        /// <summary>
        /// The prefab to preload
        /// </summary>
        public Transform prefab;

        /// <summary>
        /// A reference of the prefab's GameObject stored for performance reasons
        /// </summary>
        internal GameObject prefabGO;  // Hidden in inspector, but not Debug tab

        /// <summary>
        /// The number of instances to preload
        /// </summary>
        public int preloadAmount = 1;

        /// <summary>
        /// Displays the 'preload over time' options
        /// </summary>
        public bool preloadTime = false;

        /// <summary>
        /// The number of frames it will take to preload all requested instances
        /// </summary>
        public int preloadFrames = 2;

        /// <summary>
        /// The number of seconds to wait before preloading any instances
        /// </summary>
        public float preloadDelay = 0;

        /// <summary>
        /// Limits the number of instances allowed in the game. Turning this ON
        ///	means when 'Limit Amount' is hit, no more instances will be created.
        /// CALLS TO SpawnPool.Spawn() WILL BE IGNORED, and return null!
        ///
        /// This can be good for non-critical objects like bullets or explosion
        ///	Flares. You would never want to use this for enemies unless it makes
        ///	sense to begin ignoring enemy spawns in the context of your game.
        /// </summary>
        public bool limitInstances = false;

        /// <summary>
        /// This is the max number of instances allowed if 'limitInstances' is ON.
        /// </summary>
        public int limitAmount = 100;

        /// <summary>
        /// FIFO stands for "first-in-first-out". Normally, limiting instances will
        /// stop spawning and return null. If this is turned on (set to true) the
        /// first spawned instance will be despawned and reused instead, keeping the
        /// total spawned instances limited but still spawning new instances.
        /// </summary>
        public bool limitFIFO = false;  // Keep after limitAmount for auto-inspector

        /// <summary>
        /// Turn this ON to activate the culling feature for this Pool. 
        /// Use this feature to remove despawned (inactive) instances from the pool
        /// if the size of the pool grows too large. 
        ///	
        /// DO NOT USE THIS UNLESS YOU NEED TO MANAGE MEMORY ISSUES!
        /// This should only be used in extreme cases for memory management. 
        /// For most pools (or games for that matter), it is better to leave this 
        /// off as memory is more plentiful than performance. If you do need this
        /// you can fine tune how often this is triggered to target extreme events.
        /// 
        /// A good example of when to use this would be if you you are Pooling 
        /// projectiles and usually never need more than 10 at a time, but then
        /// there is a big one-off fire-fight where 50 projectiles are needed. 
        /// Rather than keep the extra 40 around in memory from then on, set the 
        /// 'Cull Above' property to 15 (well above the expected max) and the Pool 
        /// will Destroy() the extra instances from the game to free up the memory. 
        /// 
        /// This won't be done immediately, because you wouldn't want this culling 
        /// feature to be fighting the Pool and causing extra Instantiate() and 
        /// Destroy() calls while the fire-fight is still going on. See 
        /// "Cull Delay" for more information about how to fine tune this.
        /// </summary>
        public bool cullDespawned = false;

        /// <summary>
        /// The number of TOTAL (spawned + despawned) instances to keep. 
        /// </summary>
        public int cullAbove = 50;

        /// <summary>
        /// The amount of time, in seconds, to wait before culling. This is timed 
        /// from the moment when the Queue's TOTAL count (spawned + despawned) 
        /// becomes greater than 'Cull Above'. Once triggered, the timer is repeated 
        /// until the count falls below 'Cull Above'.
        /// </summary>
        public int cullDelay = 60;

        /// <summary>
        /// The maximum number of instances to destroy per this.cullDelay
        /// </summary>
        public int cullMaxPerPass = 5;

        /// <summary>
        /// Prints information during run-time to make debugging easier. This will 
        /// be set to true if the owner SpawnPool is true, otherwise the user's setting
        /// here will be used
        /// </summary>
        public bool _logMessages = false;  // Used by the inspector
        public bool logMessages            // Read-only
        {
            get
            {
                if (forceLoggingSilent) return false;

                if (this.spawnPool.logMessages)
                    return this.spawnPool.logMessages;
                else
                    return this._logMessages;
            }
        }

        // Forces logging to be silent regardless of user settings.
        private bool forceLoggingSilent = false;


        /// <summary>
        /// Used internally to reference back to the owner spawnPool for things like
        /// anchoring co-routines.
        /// </summary>
        public SpawnPool spawnPool;
        #endregion Public Properties Available in the Editor


        #region Constructor and Self-Destruction
        /// <description>
        ///	Constructor to require a prefab Transform
        /// </description>
        public PrefabPool(Transform prefab)
        {
            this.prefab = prefab;
            this.prefabGO = prefab.gameObject;
        }

        /// <description>
        ///	Constructor for Serializable inspector use only
        /// </description>
        public PrefabPool() { }

        /// <description>
        ///	A pseudo constructor to init stuff not init by the serialized inspector-created
        ///	instance of this class.
        /// </description>
        internal void inspectorInstanceConstructor()
        {
            this.prefabGO = this.prefab.gameObject;
            this._spawned = new List<Transform>();
            this._despawned = new List<Transform>();
        }


        /// <summary>
        /// Run by a SpawnPool when it is destroyed
        /// </summary>
        internal void SelfDestruct()
        {
			if (this.logMessages)
				Debug.Log(string.Format(
					"SpawnPool {0}: Cleaning up PrefabPool for {1}...", this.spawnPool.poolName, this.prefabGO.name
				));

            // Go through both lists and destroy everything
            foreach (Transform inst in this._despawned)
                if (inst != null && this.spawnPool != null)  // Tear-down-time protection
					this.spawnPool.DestroyInstance(inst.gameObject);

            foreach (Transform inst in this._spawned)
				if (inst != null && this.spawnPool != null)  // Tear-down-time protection
					this.spawnPool.DestroyInstance(inst.gameObject);

            this._spawned.Clear();
            this._despawned.Clear();

			// Probably overkill but no harm done
			this.prefab = null;
			this.prefabGO = null;
			this.spawnPool = null;
        }
        #endregion Constructor and Self-Destruction


        #region Pool Functionality
        /// <summary>
        /// Is set to true when the culling coroutine is started so another one
        /// won't be
        /// </summary>
        private bool cullingActive = false;


        /// <summary>
        /// The active instances associated with this prefab. This is the pool of
        /// instances created by this prefab.
        /// 
        /// Managed by a SpawnPool
        /// </summary>
        internal List<Transform> _spawned = new List<Transform>();
        public List<Transform> spawned { get { return new List<Transform>(this._spawned); } }

        /// <summary>
        /// The deactive instances associated with this prefab. This is the pool of
        /// instances created by this prefab.
        /// 
        /// Managed by a SpawnPool
        /// </summary>
        internal List<Transform> _despawned = new List<Transform>();
        public List<Transform> despawned { get { return new List<Transform>(this._despawned); } }


        /// <summary>
        /// Returns the total count of instances in the PrefabPool
        /// </summary>
        public int totalCount
        {
            get
            {
                // Add all the items in the pool to get the total count
                int count = 0;
                count += this._spawned.Count;
                count += this._despawned.Count;
                return count;
            }
        }


        /// <summary>
        /// Used to make PreloadInstances() a one-time event. Read-only.
        /// </summary>
        private bool _preloaded = false;
        internal bool preloaded
        {
            get { return this._preloaded; }
            private set { this._preloaded = value; }
        }


        /// <summary>
        /// Move an instance from despawned to spawned, set the position and 
        /// rotation, activate it and all children and return the transform
        /// </summary>
        /// <returns>
        /// True if successfull, false if xform isn't in the spawned list
        /// </returns>
        internal bool DespawnInstance(Transform xform)
        {
            return DespawnInstance(xform, true);
        }

        internal bool DespawnInstance(Transform xform, bool sendEventMessage)
        {
            if (this.logMessages)
                Debug.Log(string.Format("SpawnPool {0} ({1}): Despawning '{2}'",
                                       this.spawnPool.poolName,
                                       this.prefab.name,
                                       xform.name));

            // Switch to the despawned list
            this._spawned.Remove(xform);
            this._despawned.Add(xform);

            // Notify instance of event OnDespawned for custom code additions.
            //   This is done before handling the deactivate and enqueue incase 
            //   there the user introduces an unforseen issue.
            if (sendEventMessage)
                xform.gameObject.BroadcastMessage(
					"OnDespawned",
					this.spawnPool,
                    SendMessageOptions.DontRequireReceiver
				);

            // Deactivate the instance and all children
			xform.gameObject.SetActive(false);

            // Trigger culling if the feature is ON and the size  of the 
            //   overall pool is over the Cull Above threashold.
            //   This is triggered here because Despawn has to occur before
            //   it is worth culling anyway, and it is run fairly often.
            if (!this.cullingActive &&   // Cheap & Singleton. Only trigger once!
                this.cullDespawned &&    // Is the feature even on? Cheap too.
                this.totalCount > this.cullAbove)   // Criteria met?
            {
                this.cullingActive = true;
                this.spawnPool.StartCoroutine(CullDespawned());
            }
            return true;
        }



        /// <summary>
        /// Waits for 'cullDelay' in seconds and culls the 'despawned' list if 
        /// above 'cullingAbove' amount. 
        /// 
        /// Triggered by DespawnInstance()
        /// </summary>
        internal IEnumerator CullDespawned()
        {
            if (this.logMessages)
                Debug.Log(string.Format("SpawnPool {0} ({1}): CULLING TRIGGERED! " +
                                          "Waiting {2}sec to begin checking for despawns...",
                                        this.spawnPool.poolName,
                                        this.prefab.name,
                                        this.cullDelay));

            // First time always pause, then check to see if the condition is
            //   still true before attempting to cull.
            yield return new WaitForSeconds(this.cullDelay);

            while (this.totalCount > this.cullAbove)
            {
                // Attempt to delete an amount == this.cullMaxPerPass
                for (int i = 0; i < this.cullMaxPerPass; i++)
                {
                    // Break if this.cullMaxPerPass would go past this.cullAbove
                    if (this.totalCount <= this.cullAbove)
                        break;  // The while loop will stop as well independently

                    // Destroy the last item in the list
                    if (this._despawned.Count > 0)
                    {
                        Transform inst = this._despawned[0];
                        this._despawned.RemoveAt(0);
						this.spawnPool.DestroyInstance(inst.gameObject);

                        if (this.logMessages)
                            Debug.Log(string.Format("SpawnPool {0} ({1}): " +
                                                    "CULLING to {2} instances. Now at {3}.",
                                                this.spawnPool.poolName,
                                                this.prefab.name,
                                                this.cullAbove,
                                                this.totalCount));
                    }
                    else if (this.logMessages)
                    {
                        Debug.Log(string.Format("SpawnPool {0} ({1}): " +
                                                    "CULLING waiting for despawn. " +
                                                    "Checking again in {2}sec",
                                                this.spawnPool.poolName,
                                                this.prefab.name,
                                                this.cullDelay));

                        break;
                    }
                }

                // Check again later
                yield return new WaitForSeconds(this.cullDelay);
            }

            if (this.logMessages)
                Debug.Log(string.Format("SpawnPool {0} ({1}): CULLING FINISHED! Stopping",
                                        this.spawnPool.poolName,
                                        this.prefab.name));

            // Reset the singleton so the feature can be used again if needed.
            this.cullingActive = false;
            yield return null;
        }



        /// <summary>
        /// Move an instance from despawned to spawned, set the position and 
        /// rotation, activate it and all children and return the transform.
        /// 
        /// If there isn't an instance available, a new one is made.
        /// </summary>
        /// <returns>
        /// The new instance's Transform. 
        /// 
        /// If the Limit option was used for the PrefabPool associated with the
        /// passed prefab, then this method will return null if the limit is
        /// reached.
        /// </returns>    
        internal Transform SpawnInstance(Vector3 pos, Quaternion rot)
        {
            // Handle FIFO limiting if the limit was used and reached.
            //   If first-in-first-out, despawn item zero and continue on to respawn it
            if (this.limitInstances && this.limitFIFO &&
                this._spawned.Count >= this.limitAmount)
            {
                Transform firstIn = this._spawned[0];

                if (this.logMessages)
                {
                    Debug.Log(string.Format
                    (
                        "SpawnPool {0} ({1}): " +
                            "LIMIT REACHED! FIFO=True. Calling despawning for {2}...",
                        this.spawnPool.poolName,
                        this.prefab.name,
                        firstIn
                    ));
                }

                this.DespawnInstance(firstIn);

                // Because this is an internal despawn, we need to re-sync the SpawnPool's
                //  internal list to reflect this
                this.spawnPool._spawned.Remove(firstIn);
            }

            Transform inst;

            // If nothing is available, create a new instance
            if (this._despawned.Count == 0)
            {
                // This will also handle limiting the number of NEW instances
                inst = this.SpawnNew(pos, rot);
            }
            else
            {
                // Switch the instance we are using to the spawned list
                // Use the first item in the list for ease
                inst = this._despawned[0];
                this._despawned.RemoveAt(0);
                this._spawned.Add(inst);

                // This came up for a user so this was added to throw a user-friendly error
                if (inst == null)
                {
                    var msg = "Make sure you didn't delete a despawned instance directly.";
                    throw new MissingReferenceException(msg);
                }

                if (this.logMessages)
                    Debug.Log(string.Format("SpawnPool {0} ({1}): respawning '{2}'.",
                                            this.spawnPool.poolName,
                                            this.prefab.name,
                                            inst.name));

                // Get an instance and set position, rotation and then 
                //   Reactivate the instance and all children
                inst.position = pos;
                inst.rotation = rot;
				inst.gameObject.SetActive(true);

            }
			
			//
			// NOTE: OnSpawned message broadcast was moved to main Spawn() to ensure it runs last
			//
			
            return inst;
        }



        /// <summary>
        /// Spawns a NEW instance of this prefab and adds it to the spawned list.
        /// The new instance is placed at the passed position and rotation
        /// </summary>
        /// <param name="pos">Vector3</param>
        /// <param name="rot">Quaternion</param>
        /// <returns>
        /// The new instance's Transform. 
        /// 
        /// If the Limit option was used for the PrefabPool associated with the
        /// passed prefab, then this method will return null if the limit is
        /// reached.
        /// </returns>
        public Transform SpawnNew() { return this.SpawnNew(Vector3.zero, Quaternion.identity); }
        public Transform SpawnNew(Vector3 pos, Quaternion rot)
        {
            // Handle limiting if the limit was used and reached.
            if (this.limitInstances && this.totalCount >= this.limitAmount)
            {
                if (this.logMessages)
                {
                    Debug.Log(string.Format
                    (
                        "SpawnPool {0} ({1}): " +
                                "LIMIT REACHED! Not creating new instances! (Returning null)",
                            this.spawnPool.poolName,
                            this.prefab.name
                    ));
                }

                return null;
            }

            // Use the SpawnPool group as the default position and rotation
            if (pos == Vector3.zero) pos = this.spawnPool.group.position;
            if (rot == Quaternion.identity) rot = this.spawnPool.group.rotation;

			GameObject instGO = this.spawnPool.InstantiatePrefab(this.prefabGO, pos, rot);
			Transform inst = instGO.transform;

			this.nameInstance(inst);  // Adds the number to the end

            if (!this.spawnPool.dontReparent)
			{
				// The SpawnPool group is the parent by default
				// This will handle RectTransforms as well
				var worldPositionStays = !(inst is RectTransform);
				inst.SetParent(this.spawnPool.group, worldPositionStays);
			}

            if (this.spawnPool.matchPoolScale)
                inst.localScale = Vector3.one;

            if (this.spawnPool.matchPoolLayer)
                this.SetRecursively(inst, this.spawnPool.gameObject.layer);

            // Start tracking the new instance
            this._spawned.Add(inst);

            if (this.logMessages)
                Debug.Log(string.Format("SpawnPool {0} ({1}): Spawned new instance '{2}'.",
                                        this.spawnPool.poolName,
                                        this.prefab.name,
                                        inst.name));

            return inst;
        }


        /// <summary>
        /// Sets the layer of the passed transform and all of its children
        /// </summary>
        /// <param name="xform">The transform to process</param>
        /// <param name="layer">The new layer</param>
        private void SetRecursively(Transform xform, int layer)
        {
            xform.gameObject.layer = layer;
            foreach (Transform child in xform)
                SetRecursively(child, layer);
        }


        /// <summary>
        /// Used by a SpawnPool to add an existing instance to this PrefabPool.
        /// This is used during game start to pool objects which are not 
        /// instantiated at runtime
        /// </summary>
        /// <param name="inst">The instance to add</param>
        /// <param name="despawn">True to despawn on add</param>
        internal void AddUnpooled(Transform inst, bool despawn)
        {
            this.nameInstance(inst);   // Adds the number to the end

            if (despawn)
            {
                // Deactivate the instance and all children
				inst.gameObject.SetActive(false);

                // Start Tracking as despawned
                this._despawned.Add(inst);
            }
            else
                this._spawned.Add(inst);
        }


        /// <summary>
        /// Preload PrefabPool.preloadAmount instances if they don't already exist. In 
        /// otherwords, if there are 7 and 10 should be preloaded, this only creates 3.
        /// This is to allow asynchronous Spawn() usage in Awake() at game start
        /// </summary>
        /// <returns></returns>
        internal void PreloadInstances()
        {
            // If this has already been run for this PrefabPool, there is something
            //   wrong!
            if (this.preloaded)
            {
                Debug.Log(string.Format("SpawnPool {0} ({1}): " +
                                          "Already preloaded! You cannot preload twice. " +
                                          "If you are running this through code, make sure " +
                                          "it isn't also defined in the Inspector.",
                                        this.spawnPool.poolName,
                                        this.prefab.name));

                return;
            }

			this.preloaded = true;

            if (this.prefab == null)
            {
                Debug.LogError(string.Format("SpawnPool {0} ({1}): Prefab cannot be null.",
                                             this.spawnPool.poolName,
                                             this.prefab.name));

                return;
            }

            // Protect against preloading more than the limit amount setting
            //   This prevents an infinite loop on load if FIFO is used.
            if (this.limitInstances && this.preloadAmount > this.limitAmount)
            {
                Debug.LogWarning
                (
                    string.Format
                    (
                        "SpawnPool {0} ({1}): " +
                            "You turned ON 'Limit Instances' and entered a " +
                            "'Limit Amount' greater than the 'Preload Amount'! " +
                            "Setting preload amount to limit amount.",
                         this.spawnPool.poolName,
                         this.prefab.name
                    )
                );

                this.preloadAmount = this.limitAmount;
            }

            // Notify the user if they made a mistake using Culling
            //   (First check is cheap)
            if (this.cullDespawned && this.preloadAmount > this.cullAbove)
            {
                Debug.LogWarning(string.Format("SpawnPool {0} ({1}): " +
                    "You turned ON Culling and entered a 'Cull Above' threshold " +
                    "greater than the 'Preload Amount'! This will cause the " +
                    "culling feature to trigger immediatly, which is wrong " +
                    "conceptually. Only use culling for extreme situations. " +
                    "See the docs.",
                    this.spawnPool.poolName,
                    this.prefab.name
                ));
            }

            if (this.preloadTime)
            {
                if (this.preloadFrames > this.preloadAmount)
                {
                    Debug.LogWarning(string.Format("SpawnPool {0} ({1}): " +
                        "Preloading over-time is on but the frame duration is greater " +
                        "than the number of instances to preload. The minimum spawned " +
                        "per frame is 1, so the maximum time is the same as the number " +
                        "of instances. Changing the preloadFrames value...",
                        this.spawnPool.poolName,
                        this.prefab.name
                    ));

                    this.preloadFrames = this.preloadAmount;
                }

                this.spawnPool.StartCoroutine(this.PreloadOverTime());
            }
            else
            {
                // Reduce debug spam: Turn off this.logMessages then set it back when done.
                this.forceLoggingSilent = true;

                Transform inst;
                while (this.totalCount < this.preloadAmount) // Total count will update
                {
                    // Preload...
                    // This will parent, position and orient the instance
                    //   under the SpawnPool.group
                    inst = this.SpawnNew();
                    this.DespawnInstance(inst, false);
                }

                // Restore the previous setting
                this.forceLoggingSilent = false;
            }
        }

        private IEnumerator PreloadOverTime()
        {
            yield return new WaitForSeconds(this.preloadDelay);

            Transform inst;

            // subtract anything spawned by other scripts, just in case
            int amount = this.preloadAmount - this.totalCount;
            if (amount <= 0)
                yield break;

            // Doesn't work for Windows8...
            //  This does the division and sets the remainder as an out value.
            //int numPerFrame = System.Math.DivRem(amount, this.preloadFrames, out remainder);
            int remainder = amount % this.preloadFrames;
            int numPerFrame = amount / this.preloadFrames;

            // Reduce debug spam: Turn off this.logMessages then set it back when done.
            this.forceLoggingSilent = true;

            int numThisFrame;
            for (int i = 0; i < this.preloadFrames; i++)
            {
                // Add the remainder to the *last* frame
                numThisFrame = numPerFrame;
                if (i == this.preloadFrames - 1)
                {
                    numThisFrame += remainder;
                }

                for (int n = 0; n < numThisFrame; n++)
                {
                    // Preload...
                    // This will parent, position and orient the instance
                    //   under the SpawnPool.group
                    inst = this.SpawnNew();
                    if (inst != null)
                        this.DespawnInstance(inst, false);

                    yield return null;
                }

                // Safety check in case something else is making instances. 
                //   Quit early if done early
                if (this.totalCount > this.preloadAmount)
                    break;
            }

            // Restore the previous setting
            this.forceLoggingSilent = false;
        }

        #endregion Pool Functionality


        #region Utilities
        /// <summary>
        /// If this PrefabPool spawned or despawned lists contain the given 
        /// transform, true is returned. Othrewise, false is returned
        /// </summary>
        /// <param name="transform">A transform to test.</param>
        /// <returns>bool</returns>
        public bool Contains(Transform transform)
        {
            if (this.prefabGO == null)
                Debug.LogError(string.Format("SpawnPool {0}: PrefabPool.prefabGO is null",
                                             this.spawnPool.poolName));

            bool contains;

            contains = this.spawned.Contains(transform);
            if (contains)
                return true;

            contains = this.despawned.Contains(transform);
            if (contains)
                return true;

            return false;
        }
        
        /// <summary>
        /// Appends a number to the end of the passed transform. The number
        /// will be one more than the total objects in this PrefabPool, so 
        /// name the object BEFORE adding it to the spawn or depsawn lists.
        /// </summary>
        /// <param name="instance"></param>
        private void nameInstance(Transform instance)
        {
            // Rename by appending a number to make debugging easier
            //   ToString() used to pad the number to 3 digits. Hopefully
            //   no one has 1,000+ objects.
            instance.name += (this.totalCount + 1).ToString("#000");
        }
        #endregion Utilities

    }



    public class PrefabsDict : IDictionary<string, Transform>
    {
        #region Public Custom Memebers
        /// <summary>
        /// Returns a formatted string showing all the prefab names
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            // Get a string[] array of the keys for formatting with join()
            var keysArray = new string[this._prefabs.Count];
            this._prefabs.Keys.CopyTo(keysArray, 0);

            // Return a comma-sperated list inside square brackets (Pythonesque)
            return string.Format("[{0}]", System.String.Join(", ", keysArray));
        }
        #endregion Public Custom Memebers


        #region Internal Dict Functionality
        // Internal Add and Remove...
        internal void _Add(string prefabName, Transform prefab)
        {
            this._prefabs.Add(prefabName, prefab);
        }

        internal bool _Remove(string prefabName)
        {
            return this._prefabs.Remove(prefabName);
        }

        internal void _Clear()
        {
            this._prefabs.Clear();
        }
        #endregion Internal Dict Functionality


        #region Dict Functionality
        // Internal (wrapped) dictionary
        private Dictionary<string, Transform> _prefabs = new Dictionary<string, Transform>();

        /// <summary>
        /// Get the number of SpawnPools in PoolManager
        /// </summary>
        public int Count { get { return this._prefabs.Count; } }

        /// <summary>
        /// Returns true if a prefab exists with the passed prefab name.
        /// </summary>
        /// <param name="prefabName">The name to look for</param>
        /// <returns>True if the prefab exists, otherwise, false.</returns>
        public bool ContainsKey(string prefabName)
        {
            return this._prefabs.ContainsKey(prefabName);
        }

        /// <summary>
        /// Used to get a prefab when the user is not sure if the prefabName is used.
        /// This is faster than checking Contains(prefabName) and then accessing the dict
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(string prefabName, out Transform prefab)
        {
            return this._prefabs.TryGetValue(prefabName, out prefab);
        }

        #region Not Implimented

        public void Add(string key, Transform value)
        {
            throw new System.NotImplementedException("Read-Only");
        }

        public bool Remove(string prefabName)
        {
            throw new System.NotImplementedException("Read-Only");
        }

        public bool Contains(KeyValuePair<string, Transform> item)
        {
            string msg = "Use Contains(string prefabName) instead.";
            throw new System.NotImplementedException(msg);
        }

        public Transform this[string key]
        {
            get
            {
                Transform prefab;
                try
                {
                    prefab = this._prefabs[key];
                }
                catch (KeyNotFoundException)
                {
                    string msg = string.Format("A Prefab with the name '{0}' not found. " +
                                                "\nPrefabs={1}",
                                                key, this.ToString());
                    throw new KeyNotFoundException(msg);
                }

                return prefab;
            }
            set
            {
                throw new System.NotImplementedException("Read-only.");
            }
        }

        public ICollection<string> Keys
        {
            get
            {
                return this._prefabs.Keys;
            }
        }


        public ICollection<Transform> Values
        {
            get
            {
                return this._prefabs.Values;
            }
        }


        #region ICollection<KeyValuePair<string, Transform>> Members
        private bool IsReadOnly { get { return true; } }
        bool ICollection<KeyValuePair<string, Transform>>.IsReadOnly { get { return true; } }

        public void Add(KeyValuePair<string, Transform> item)
        {
            throw new System.NotImplementedException("Read-only");
        }

        public void Clear() { throw new System.NotImplementedException(); }

        private void CopyTo(KeyValuePair<string, Transform>[] array, int arrayIndex)
        {
            string msg = "Cannot be copied";
            throw new System.NotImplementedException(msg);
        }

        void ICollection<KeyValuePair<string, Transform>>.CopyTo(KeyValuePair<string, Transform>[] array, int arrayIndex)
        {
            string msg = "Cannot be copied";
            throw new System.NotImplementedException(msg);
        }

        public bool Remove(KeyValuePair<string, Transform> item)
        {
            throw new System.NotImplementedException("Read-only");
        }
        #endregion ICollection<KeyValuePair<string, Transform>> Members
        #endregion Not Implimented




        #region IEnumerable<KeyValuePair<string, Transform>> Members
        public IEnumerator<KeyValuePair<string, Transform>> GetEnumerator()
        {
            return this._prefabs.GetEnumerator();
        }
        #endregion



        #region IEnumerable Members
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this._prefabs.GetEnumerator();
        }
        #endregion

        #endregion Dict Functionality

    }

}


public class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue>
{
	private readonly IDictionary<TKey, TValue> _dictionary;
	
	public ReadOnlyDictionary(IDictionary<TKey, TValue> dictionary)
	{
		_dictionary = dictionary;
	}
	
	#region IDictionary<TKey,TValue> Members
	
	void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
	{
		throw ReadOnlyException();
	}
	
	public bool ContainsKey(TKey key)
	{
		return _dictionary.ContainsKey(key);
	}
	
	public ICollection<TKey> Keys
	{
		get { return _dictionary.Keys; }
	}
	
	bool IDictionary<TKey, TValue>.Remove(TKey key)
	{
		throw ReadOnlyException();
	}
	
	public bool TryGetValue(TKey key, out TValue value)
	{
		return _dictionary.TryGetValue(key, out value);
	}
	
	public ICollection<TValue> Values
	{
		get { return _dictionary.Values; }
	}
	
	public TValue this[TKey key]
	{
		get
		{
			return _dictionary[key];
		}
	}
	
	TValue IDictionary<TKey, TValue>.this[TKey key]
	{
		get
		{
			return this[key];
		}
		set
		{
			throw ReadOnlyException();
		}
	}
	
	#endregion
	
	#region ICollection<KeyValuePair<TKey,TValue>> Members
	
	void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
	{
		throw ReadOnlyException();
	}
	
	void ICollection<KeyValuePair<TKey, TValue>>.Clear()
	{
		throw ReadOnlyException();
	}
	
	public bool Contains(KeyValuePair<TKey, TValue> item)
	{
		return _dictionary.Contains(item);
	}
	
	public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
	{
		_dictionary.CopyTo(array, arrayIndex);
	}
	
	public int Count
	{
		get { return _dictionary.Count; }
	}
	
	public bool IsReadOnly
	{
		get { return true; }
	}
	
	bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
	{
		throw ReadOnlyException();
	}
	
	#endregion
	
	#region IEnumerable<KeyValuePair<TKey,TValue>> Members
	
	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
	{
		return _dictionary.GetEnumerator();
	}
	
	#endregion
	
	#region IEnumerable Members
	
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
	
	#endregion
	
	private static System.Exception ReadOnlyException()
	{
		return new System.NotSupportedException("This dictionary is read-only");
	}
}