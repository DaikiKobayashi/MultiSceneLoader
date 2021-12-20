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
    public class MultiSceneLoaderWindow : EditorWindow, IHasCustomMenu
    {
        enum SelectWindowType
        {
            Main,
            SceneLoaderData,
        }

        public const string k_DataPath = "Assets/MultiSceneLoader/Data/LoaderSceneListData.asset";
        
        private static SceneLoadDataSO loadSceneList;
        private static SelectWindowType selectWindowType = SelectWindowType.Main;

        // �ǉ��V�[���p�X�L���b�V��
        private List<string> addElementNameList = new List<string>();

        [MenuItem("Tools/Multi scene loader")]
        static public void LoadWindow()
        {
            LoadData();
            MultiSceneLoaderWindow window = EditorWindow.GetWindow<MultiSceneLoaderWindow>("Scene Loader");
            window.Show();
        }

        static public void LoadData(int loopCount = 0)
        {
            var data = AssetDatabase.LoadAssetAtPath(k_DataPath, typeof(SceneLoadDataSO)) as SceneLoadDataSO;

            if (data != null)
            {
                // �f�[�^�����݂���
                loadSceneList = data;
                return;
            }
            else
            {
                // �f�[�^�����݂��Ȃ�
                // �f�B���N�g�����쐬
                SafeCreateDirectory(k_DataPath);

                // �t�@�C�����쐬
                var createData = ScriptableObject.CreateInstance(typeof(SceneLoadDataSO));

                // �t�@�C����ۑ�
                AssetDatabase.CreateAsset(createData, k_DataPath);
                Debug.Log("Multi scene loader".Coloring("cyan") + " " + "Create new data!");

                // �ċA
                LoadData(loopCount);
            }
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            // ���j���[�ǉ�
            menu.AddItem(new GUIContent("Open Script"), false, () =>
            {
                string filePath = GetSourceFilePath();
                filePath = filePath.Replace(Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("/") + 1), "");
                UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(filePath, typeof(UnityEngine.Object));
                if (obj != null)
                {
                    Selection.activeObject = obj;
                    // ��Q�����͍s��
                    AssetDatabase.OpenAsset(obj, 0);
                }
            });
        }

        private void OnGUI()
        {
            if (loadSceneList == null)
                LoadData();

            using (new GUILayout.VerticalScope())
            {
                DrawToolBar();
                switch (selectWindowType)
                {
                    case SelectWindowType.Main:
                        DrawElement();

                        GUILayout.FlexibleSpace();
                        DrawAddElementWindow();
                        break;

                    case SelectWindowType.SceneLoaderData:
                        DrawSceneLoaderData();
                        break;
                }
            }
        }

        private void DrawToolBar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.ExpandWidth(true)))
            {
                if (GUILayout.Button("ScneLoader", EditorStyles.toolbarButton))
                {
                    selectWindowType = SelectWindowType.Main;
                }

                if (GUILayout.Button(EditorGUIUtility.TrIconContent("d_SaveAs"), EditorStyles.toolbarButton, GUILayout.Width(25)))
                {
                    var wind = CreateInstance<SceneLoaderDataWindow>();
                    var windowPos = new Rect(position);
                    windowPos.x = position.x + position.width + 10; 

                    wind.SetUp(loadSceneList);
                    wind.titleContent = new GUIContent("Scene Loader Data");
                    
                    wind.Show();
                    wind.position = windowPos;
                }

                if (GUILayout.Button(EditorGUIUtility.TrIconContent("_Popup@2x"), EditorStyles.toolbarButton, GUILayout.Width(25)))
                {
                    var mouseRect = new Rect(Event.current.mousePosition, Vector2.one);
                    var content = new MulltiSceneLoaderPopupContent();
                    PopupWindow.Show(mouseRect, content);
                }
            }   
        }


        private Vector2 elementScrollPos;
        private void DrawElement()
        {
            List<LoadData> tempList = new List<LoadData>(loadSceneList.loadGroups);

            void Element(LoadData data)
            {
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    GUIStyle groupRavelStyle = new GUIStyle();
                    groupRavelStyle.fontSize = 15;
                    groupRavelStyle.normal.textColor = Color.white;
                    EditorGUILayout.LabelField(data.dataName, groupRavelStyle);

                    using (new GUILayout.HorizontalScope())
                    {
                        // �V�[�����[�h
                        if (GUILayout.Button("O", GUILayout.Height(EditorGUIUtility.singleLineHeight * 2), GUILayout.Width(EditorGUIUtility.singleLineHeight * 2)))
                        {
                            // �V�[����ۑ����邩?
                            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                            {
                                for (int i = 0; i < data.sceneList.Length; i++)
                                {
                                    // �ŏ��ɓǂݍ��ރV�[���̓V���O���ǂݍ���
                                    if (i == 0)
                                        EditorSceneManager.OpenScene(data.sceneList[i]);
                                    else
                                        EditorSceneManager.OpenScene(data.sceneList[i], OpenSceneMode.Additive);
                                }
                            }
                        }

                        using (new GUILayout.HorizontalScope())
                        {
                            using (new EditorGUILayout.VerticalScope())
                            {
                                foreach (string sceneName in data.sceneList)
                                    EditorGUILayout.LabelField(Path.GetFileNameWithoutExtension(sceneName));
                            }

                            GUILayout.FlexibleSpace();

                            // ���X�g����폜
                            if (GUILayout.Button("X", GUILayout.Width(EditorGUIUtility.singleLineHeight)))
                            {
                                loadSceneList.loadGroups.Remove(data);
                            }
                        }
                    }
                }
            }

            using (var scrollView = new EditorGUILayout.ScrollViewScope(elementScrollPos))
            {
                elementScrollPos = scrollView.scrollPosition;
                if (tempList.Count > 0)
                {
                    foreach (var value in tempList)
                    {
                        Element(value);
                    }
                }
            }
        }

        Vector2 addElementNameListScrollPos;
        string tempSceneGroupName;
        private void DrawAddElementWindow()
        {
            const float addButtonHeight = 25;
            const float dropAreaHeight = 75;

            GUIStyle style = new GUIStyle();
            style.fontSize = 18;
            style.normal.textColor = Color.white;
            EditorGUILayout.LabelField("Add load scenens group", style);

            using (var box = new GUILayout.VerticalScope("box"))
            {
                using (new GUILayout.HorizontalScope())
                {
                    tempSceneGroupName = EditorGUILayout.TextField(tempSceneGroupName);
                    if (GUILayout.Button("Add", GUILayout.Height(addButtonHeight)))
                    {
                        if (addElementNameList.Any())
                        {
                            loadSceneList.loadGroups.Add(new LoadData(tempSceneGroupName, addElementNameList.ToArray()));
                        }
                    }
                }

                // �h���b�O&�h���b�v�̈�
                Rect dropAreaRect = new Rect(7.5F, position.height - dropAreaHeight - 7.5F, position.width - 15F, dropAreaHeight);
                var list = CreateDragAndDropGUI(dropAreaRect);

                // �h���b�O���ꂽ�I�u�W�F�N�g���擾
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] is SceneAsset)
                    {
                        var path = AssetDatabase.GetAssetPath(list[i]);

                        // ���݃`�F�b�N
                        if (!addElementNameList.Contains(path))
                            addElementNameList.Add(AssetDatabase.GetAssetPath(list[i]));
                    }
                }

                // �ǉ������X�g�\��
                using (var scrollView = new GUILayout.ScrollViewScope(addElementNameListScrollPos, GUI.skin.window, GUILayout.Height(dropAreaHeight)))
                {
                    addElementNameListScrollPos = scrollView.scrollPosition;

                    using (new GUILayout.VerticalScope())
                    {
                        List<string> tempList = addElementNameList;

                        EditorGUILayout.BeginHorizontal();
                        for (int i = 0; i < tempList.Count; i++)
                        {
                            using (new GUILayout.HorizontalScope("box"))
                            {
                                EditorGUILayout.LabelField(Path.GetFileName(Path.GetFileNameWithoutExtension(addElementNameList[i])), GUILayout.Width(125));
                                if (GUILayout.Button("X", GUILayout.Width(EditorGUIUtility.singleLineHeight)))
                                {
                                    addElementNameList.Remove(tempList[i]);
                                }
                            }
                            if(i != 0 && i % 2 == 0)
                            {
                                EditorGUILayout.EndHorizontal();
                                EditorGUILayout.BeginHorizontal();
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
        }

        private void DrawSceneLoaderData()
        {
            var editor = Editor.CreateEditor(loadSceneList);
            
            if (editor == null)
                return;

            editor.OnInspectorGUI();
        }

        private List<Object> CreateDragAndDropGUI(Rect rect)
        {
            List<Object> list = new List<Object>();

            //D&D�o����ꏊ��`��
            var color = GUI.color;
            GUI.color = Color.red;
            GUI.Box(rect, "");
            GUI.color = color;

            //�}�E�X�̈ʒu��D&D�͈̔͂ɂȂ���΃X���[
            if (!rect.Contains(Event.current.mousePosition))
            {
                return list;
            }

            //���݂̃C�x���g���擾
            EventType eventType = Event.current.type;

            //�h���b�O���h���b�v�ő��삪 �X�V���ꂽ�Ƃ� or ���s�����Ƃ�
            if (eventType == EventType.DragUpdated || eventType == EventType.DragPerform)
            {
                //�J�[�\����+�̃A�C�R����\��
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                //�h���b�v���ꂽ�I�u�W�F�N�g�����X�g�ɓo�^
                if (eventType == EventType.DragPerform)
                {
                    list = new List<Object>(DragAndDrop.objectReferences);

                    //�h���b�O���󂯕t����(�h���b�O���ăJ�[�\���ɂ����t���Ă��I�u�W�F�N�g���߂�Ȃ��Ȃ�)
                    DragAndDrop.AcceptDrag();
                }

                //�C�x���g���g�p�ς݂ɂ���
                Event.current.Use();
            }

            return list;
        }

        /// <summary>
        /// �w�肵���p�X�Ƀf�B���N�g�������݂��Ȃ��ꍇ
        /// ���ׂẴf�B���N�g���ƃT�u�f�B���N�g�����쐬���܂�
        /// </summary>
        public static void SafeCreateDirectory(string path)
        {
            string distDir = Path.GetDirectoryName(path);
            if (!Directory.Exists(distDir))
            {
                Directory.CreateDirectory(path);
            }
        }

        private string GetSourceFilePath([CallerFilePath] string sourceFilePath = "")
        {
            return sourceFilePath.Replace("\\", "/");
        }



        /// <summary>
        /// �Z�b�e�B���O�|�b�v�A�b�v�E�B���h�E
        /// </summary>
        public class MulltiSceneLoaderPopupContent : PopupWindowContent
        {
            private const int k_ElementCount = 5;
            /// <summary>
            /// �T�C�Y���擾����
            /// </summary>
            public override Vector2 GetWindowSize()
            {
                return new Vector2(100, (EditorGUIUtility.singleLineHeight + 3) * k_ElementCount);
            }

            /// <summary>
            /// GUI�`��
            /// </summary>
            public override void OnGUI(Rect rect)
            {
                GUI.Box(rect,"");

                var button_Style = EditorStyles.miniButtonMid;
                button_Style.margin.top = 0;
                button_Style.margin.bottom = 0;

                if (GUILayout.Button("Select data file", button_Style))
                {
                    var obj = AssetDatabase.LoadAssetAtPath(MultiSceneLoaderWindow.k_DataPath, typeof(UnityEngine.Object));
                    if (obj != null)
                    {
                        EditorGUIUtility.PingObject(obj);
                    }
                }

                if (GUILayout.Button("Open data file", button_Style))
                {
                    var obj = AssetDatabase.LoadAssetAtPath(MultiSceneLoaderWindow.k_DataPath, typeof(UnityEngine.Object));
                    if (obj != null)
                    {
                        EditorGUIUtility.PingObject(obj);
                        Selection.activeObject = obj;

                        // Get Inspector Type
                        var inspectorType = typeof(EditorWindow).Assembly.GetType("UnityEditor.InspectorWindow");

                        // Create Inspector Instance
                        var inspectorInstance = ScriptableObject.CreateInstance(inspectorType) as EditorWindow;
                        inspectorInstance.Show();

                        // "isLocked�v���p�e�B�����b�N����Ă��邩�i�E�B���h�E�̌��}�[�N�ɑΉ����Ă���j
                        var isLocked = inspectorType.GetProperty("isLocked", BindingFlags.Instance | BindingFlags.Public);

                        // �쐻�����C���X�y�N�^�E�B���h�E��isLocked��true�ɐݒ肷��
                        isLocked.GetSetMethod().Invoke(inspectorInstance, new object[] { true });
                    }
                }
            }

            /// <summary>
            /// �J�����Ƃ��̏���
            /// </summary>
            public override void OnOpen()
            {
            }

            /// <summary>
            /// �����Ƃ��̏���
            /// </summary>
            public override void OnClose()
            {
            }
        }
    }

    public static class StringExtensions
    {
        public static string Coloring(this string str, string color)
        {
            return string.Format("<color={0}>{1}</color>", color, str);
        }
        public static string Red(this string str)
        {
            return str.Coloring("red");
        }
        public static string Green(this string str)
        {
            return str.Coloring("green");
        }
        public static string Blue(this string str)
        {
            return str.Coloring("blue");
        }
        public static string Resize(this string str, int size)
        {
            return string.Format("<size={0}>{1}</size>", size, str);
        }
        public static string Medium(this string str)
        {
            return str.Resize(11);
        }
        public static string Small(this string str)
        {
            return str.Resize(9);
        }
        public static string Large(this string str)
        {
            return str.Resize(16);
        }
        public static string Bold(this string str)
        {
            return string.Format("<b>{0}</b>", str);
        }
        public static string Italic(this string str)
        {
            return string.Format("<i>{0}</i>", str);
        }
    }
}