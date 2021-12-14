using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MultiSceneLoader
{
    public class SceneLoadDataSO : ScriptableObject
    {
        [SerializeField] public List<LoadData> loadGroups = new List<LoadData>();
    }

    [System.Serializable]
    public class LoadData
    {
        [SerializeField] public string dataName = "";
        [SerializeField] public string[] sceneList = new string[0];

        public LoadData(string dataName, params string[] sceneNames)
        {
            this.dataName = dataName;
            this.sceneList = sceneNames;
        }
    }
}