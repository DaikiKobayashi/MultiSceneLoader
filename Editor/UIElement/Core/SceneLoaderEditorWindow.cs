using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;
using System.IO;
using UnityEditor.SceneManagement;

namespace MultiSceneLoader
{
    public class SceneLoaderEditorWindow : EditorWindow
    {
        public const string k_DataPath = "Assets/MultiSceneLoader/Data/LoaderSceneListData.asset";
        private const string k_SceneGroupStylePath = "Packages/com.daikikobayashi1910.multi_scene_loader/Editor/UIElement/Core/MultipleSceneGroup.uxml";


        private static SceneLoadDataSO loadSceneList;
        private VisualTreeAsset sceneGroupTree;

        private ScrollView sceneListView;
        private TextField sceneGroupNameField;
        private ScrollView pushSceneList;
        private Label dropSceneMessageLabel;

        [MenuItem("Window/UI Toolkit/SceneLoaderEditorWindow")]
        public static void ShowExample()
        {
            SceneLoaderEditorWindow wnd = GetWindow<SceneLoaderEditorWindow>();
            wnd.titleContent = new GUIContent("SceneLoaderEditorWindow");
        }


        public void OnEnable()
        {
            // Import UXML
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.daikikobayashi1910.multi_scene_loader/Editor/UIElement/Core/SceneLoaderEditorWindow.uxml");
            var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.daikikobayashi1910.multi_scene_loader/Editor/UIElement/Core/SceneLoaderEditorWindow.uss");
            sceneGroupTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_SceneGroupStylePath);

            // �C���|�[�g����VisualTreeAsset�𔽉f
            visualTree.CloneTree(rootVisualElement);
            rootVisualElement.styleSheets.Add(uss);

            // �e�A�C�e�����擾
            sceneListView = rootVisualElement.Query<ScrollView>("SceneListView").First();
            sceneGroupNameField = rootVisualElement.Query<TextField>("SceneGroupPushName").First();
            pushSceneList = rootVisualElement.Query<ScrollView>("PushSceneList").First();
            dropSceneMessageLabel = rootVisualElement.Query<Label>("DropSceneMessageLabel").First();

            // �e�A�C�e���C�x���g��o�^
            rootVisualElement.Query<Button>("SceneGroupPushButton").First().clicked += () =>
            {
                PushSceneGroup();
            };

            pushSceneList.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            });

            pushSceneList.RegisterCallback<DragPerformEvent>(evt =>
            {
                var obj = new List<Object>(DragAndDrop.objectReferences);
                AddPushSceneList(obj);
            });


            // �f�[�^�����[�h
            DataLoad();
        }

        private void OnDisable()
        {
            DataSave();
        }

        private void DataLoad(int loopCount = 0)
        {
            var data = AssetDatabase.LoadAssetAtPath(k_DataPath, typeof(SceneLoadDataSO)) as SceneLoadDataSO;
            
            // �f�[�^�����݂��邩
            if (data == null)
            {
                // �f�B���N�g���K�w��������΍쐬
                FileEx.SafeCreateDirectory(k_DataPath);

                // �t�@�C�����쐬
                var createData = ScriptableObject.CreateInstance(typeof(SceneLoadDataSO));

                // �t�@�C����ۑ�
                AssetDatabase.CreateAsset(createData, k_DataPath);
                Debug.Log("Multi scene loader".Coloring("cyan") + " " + "Create new data!");

                // �ċA
                DataLoad(loopCount++);
            }

            loadSceneList = data;

            // �f�[�^��ǂݍ���
            for(int i = 0;i < loadSceneList.loadGroups.Count; i++)
            {
                var loadData = loadSceneList.loadGroups[i];

                AddSceneGroupElement(loadData.dataName, loadData.sceneList.ToList());
            }

            Debug.Log("SceneLoader".Bold().Coloring("cyan") + " => " + "Data Load!");
        }

        private void DataSave()
        {
            Debug.Log("SceneLoader".Bold().Coloring("cyan") + " => " +"Data Save!");

            var newSaveData = new List<LoadData>();

            rootVisualElement.Query<VisualElement>(null, "SceneGroupElement")
                .ForEach(x =>
                {
                    // �^�C�g�����擾
                    string title = x.Query<Label>("GroupTitle").First().text;
                    
                    // �V�[���p�X���擾
                    List<string> scenePath = new List<string>();
                    x.Query<Label>(null, "ScenePathLabel")
                    .ForEach(x =>
                    {
                        scenePath.Add(x.text);
                    });

                    newSaveData.Add(new LoadData(
                        title,
                        scenePath.ToArray()));
                });

            loadSceneList.loadGroups = newSaveData;
        }

        /// <summary>
        /// �V�[���O���[�v�����X�g�r���[�ɒǉ�
        /// </summary>
        /// <param name="title"></param>
        /// <param name="sceneNames"></param>
        private void AddSceneGroupElement(string title, IEnumerable<string> sceneNames)
        {
            var addTarget = sceneListView;

            VisualElement instance = sceneGroupTree.CloneTree();
            instance.AddToClassList("SceneGroupElement");

            var loadSceneListView = instance.Query<ScrollView>("LoadSceneList").First();
            instance.Query<Label>("GroupTitle").First().text = title;
            instance.Query<Button>("DeleteButton").First().clicked += () =>
            {
                addTarget.Remove(instance);
            };
            instance.Query<Button>("LoadButton").First().clicked += () =>
            {
                // �V�[����ۑ����邩?
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    var loadScenes = loadSceneListView.Query<Label>(null, "ScenePathLabel").ToList();
                    for (int i = 0; i < loadScenes.Count(); i++)
                    {
                        var scene = loadScenes[i].text;
                        if (i == 0)
                            EditorSceneManager.OpenScene(scene);
                        else
                            EditorSceneManager.OpenScene(scene, OpenSceneMode.Additive);
                    }
                }
            };


            foreach (var sceneName in sceneNames)
            {
                Label scenePathLabel = new Label(sceneName);
                scenePathLabel.AddToClassList("ScenePathLabel");
                loadSceneListView.Add(scenePathLabel);
            }

            addTarget.Add(instance);
        }

        /// <summary>
        /// �V�[�����X�g���v�b�V��
        /// </summary>
        void PushSceneGroup()
        {
            // �V�[�����X�g����v�f���擾
            var pushScenes = pushSceneList.Query<VisualElement>(null, "PushSceneLabel")
                .ToList()
                .Select(x =>
                {
                    return x.Query<Label>().First().text;
                });
            
            if (!pushScenes.Any())
                return;

            AddSceneGroupElement(sceneGroupNameField.text, pushScenes);
            
            SceneListElementChangeEvent();
        }

        /// <summary>
        /// �v�b�V���V�[�����X�g�ɒǉ�
        /// </summary>
        /// <param name="list">�h���b�O���ꂽ�I�u�W�F�N�g</param>
        void AddPushSceneList(List<Object> list)
        {
            // �v�f�����ω�������
            bool isElementChange = false;

            for (int i = 0; i < list.Count; i++)
            {
                var obj = list[i];
                if (obj is SceneAsset)
                {
                    if (pushSceneList.childCount > 0)
                    {
                        bool continueflag = false;
                        foreach (var child in pushSceneList.Children())
                        {
                            if (child.name == $"SceneElement-{obj.name}")
                            {
                                continueflag = true;
                                break;
                            }
                        }

                        if (continueflag)
                            continue;
                    }

                    var path = AssetDatabase.GetAssetPath(obj);
                    var root = new VisualElement();

                    root.name = $"SceneElement-{obj.name}";
                    root.AddToClassList("horizontal");
                    root.AddToClassList("PushSceneLabel");

                    var deleteButton = new Button();
                    deleteButton.text = "X";
                    deleteButton.clicked += () =>
                    {
                        pushSceneList.Remove(root);
                        SceneListElementChangeEvent();
                    };

                    root.Add(new Label(path));
                    root.Add(deleteButton);

                    pushSceneList.Add(root);
                    isElementChange = true;
                }
            }

            if (isElementChange)
                SceneListElementChangeEvent();
        }

        /// <summary>
        /// �ǉ��V�[�����X�g�̗v�f�ύX�C�x���g
        /// </summary>
        private void SceneListElementChangeEvent()
        {
            var elementCount = PushSceneLabels.ToList().Count;

            if (elementCount == 0)
            {
                dropSceneMessageLabel.visible = true;
            }
            else
            {
                dropSceneMessageLabel.visible = false;
            }
        }

        /// <summary>
        /// PushSceneList�̎擾
        /// </summary>
        private UQueryBuilder<VisualElement> PushSceneLabels => pushSceneList.Query<VisualElement>(null, "PushSceneLabel");
    }

    public static class FileEx
    {
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
    }
}