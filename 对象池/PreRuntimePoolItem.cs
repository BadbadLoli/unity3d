using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PathologicalGames
{
    [AddComponentMenu("Path-o-logical/PoolManager/Pre-Runtime Pool Item")]
    public class PreRuntimePoolItem : MonoBehaviour
    {
        #region Public Properties
        public string poolName = "";
        public string prefabName = "";
        public bool despawnOnStart = true;
        public bool doNotReparent = false;
        #endregion Public Properties

        private void Start()
        {
            SpawnPool pool;
            if (!PoolManager.Pools.TryGetValue(this.poolName, out pool))
            {

                string msg = "PreRuntimePoolItem Error ('{0}'): " +
                        "No pool with the name '{1}' exists! Create one using the " +
                        "PoolManager Inspector interface or PoolManager.CreatePool()." +
                        "See the online docs for more information at " +
                        "http://docs.poolmanager.path-o-logical.com";

                Debug.LogError(string.Format(msg, this.name, this.poolName));
                return;
            }
            pool.Add(this.transform, this.prefabName, this.despawnOnStart, !this.doNotReparent);
        }
    }
}

