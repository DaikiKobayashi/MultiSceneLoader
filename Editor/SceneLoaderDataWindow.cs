using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using System.Runtime.CompilerServices;
using System.Reflection;

namespace MultiSceneLoader
{
    public class SceneLoaderDataWindow : EditorWindow
    {
        private SceneLoadDataSO loadSceneListData;

        public void SetUp(SceneLoadDataSO loadSceneListData)
        {
            this.loadSceneListData = loadSceneListData;
        }

        private void OnGUI()
        {
            var editor = Editor.CreateEditor(loadSceneListData);

            if (editor == null)
                return;

            editor.OnInspectorGUI();
        }
    }
}