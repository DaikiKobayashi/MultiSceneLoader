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

            // インポートしたVisualTreeAssetを反映
            visualTree.CloneTree(rootVisualElement);
            rootVisualElement.styleSheets.Add(uss);

            // 各アイテムを取得
            sceneListView = rootVisualElement.Query<ScrollView>("SceneListView").First();
            sceneGroupNameField = rootVisualElement.Query<TextField>("SceneGroupPushName").First();
            pushSceneList = rootVisualElement.Query<ScrollView>("PushSceneList").First();
            dropSceneMessageLabel = rootVisualElement.Query<Label>("DropSceneMessageLabel").First();

            // 各アイテムイベントを登録
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


            // データをロード
            DataLoad();
        }

        private void OnDisable()
        {
            DataSave();
        }

        private void DataLoad(int loopCount = 0)
        {
            var data = AssetDatabase.LoadAssetAtPath(k_DataPath, typeof(SceneLoadDataSO)) as SceneLoadDataSO;
            
            // データが存在するか
            if (data == null)
            {
                // ディレクトリ階層が無ければ作成
                FileEx.SafeCreateDirectory(k_DataPath);

                // ファイルを作成
                var createData = ScriptableObject.CreateInstance(typeof(SceneLoadDataSO));

                // ファイルを保存
                AssetDatabase.CreateAsset(createData, k_DataPath);
                Debug.Log("Multi scene loader".Coloring("cyan") + " " + "Create new data!");

                // 再帰
                DataLoad(loopCount++);
            }

            loadSceneList = data;

            // データを読み込む
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
                    // タイトルを取得
                    string title = x.Query<Label>("GroupTitle").First().text;
                    
                    // シーンパスを取得
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
        /// シーングループをリストビューに追加
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
                // シーンを保存するか?
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
        /// シーンリストをプッシュ
        /// </summary>
        void PushSceneGroup()
        {
            // シーンリストから要素を取得
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
        /// プッシュシーンリストに追加
        /// </summary>
        /// <param name="list">ドラッグされたオブジェクト</param>
        void AddPushSceneList(List<Object> list)
        {
            // 要素数が変化したか
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
        /// 追加シーンリストの要素変更イベント
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
        /// PushSceneListの取得
        /// </summary>
        private UQueryBuilder<VisualElement> PushSceneLabels => pushSceneList.Query<VisualElement>(null, "PushSceneLabel");
    }

    public static class FileEx
    {
        /// <summary>
        /// 指定したパスにディレクトリが存在しない場合
        /// すべてのディレクトリとサブディレクトリを作成します
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