using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;
using System.IO;
using UnityEditor.SceneManagement;
using System.Text.RegularExpressions;

namespace MultiSceneLoader
{
    public class SceneLoaderEditorWindow : EditorWindow
    {
        private const string WindowUxmlPath = "Packages/com.k_daiki1910.multi_scene_loader/Editor/Core/SceneLoaderEditorWindow.uxml";
        private const string WindowUssPath = "Packages/com.k_daiki1910.multi_scene_loader/Editor/Core/SceneLoaderEditorWindow.uss";
        private const string SceneGroupStylePath = "Packages/com.k_daiki1910.multi_scene_loader/Editor/Core/MultipleSceneGroup.uxml";

        private SceneLoadDataSO loadSceneData;
        private VisualTreeAsset sceneGroupTree;

        private ScrollView sceneListView;
        private TextField sceneGroupNameField;
        private ScrollView pushSceneList;
        private Label dropSceneMessageLabel;

        [MenuItem("Tools/Open SceneLoaderEditorWindow")]
        public static void ShowExample()
        {
            SceneLoaderEditorWindow wnd = GetWindow<SceneLoaderEditorWindow>();
            wnd.titleContent = new GUIContent("Scene Loader");
            wnd.Initlaize();
        }

        private void Initlaize()
        {
            // データが無ければ開かない
            loadSceneData = GetOrCreateSaveData();
            if (loadSceneData == null)
                Close();

            Debug.Log("[SceneLoader]".Bold().Coloring("cyan") + " => " + "Get save data!");

            ImportStyles();
            DataReflectedView();
        }

        private void ImportStyles()
        {
            // Import UXMLs
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(WindowUxmlPath);
            var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(WindowUssPath);
            sceneGroupTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SceneGroupStylePath);

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
        }

        private void DataReflectedView()
        {
            // データを読み込みビューへ反映する
            foreach(var item in loadSceneData.loadGroups)
            {
                AddSceneGroupElement(item.dataName, item.sceneList);
            }
        }

        private void OnDisable()
        {
            DataSave();
        }

        private void DataSave()
        {
            if (loadSceneData == null)
                return;

            Debug.Log("[SceneLoader]".Bold().Coloring("cyan") + " => " +"Data Save!");

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

            loadSceneData.loadGroups = newSaveData;
            loadSceneData = null;
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



        /// <summary>
        /// セーブデータを取得する。無ければ作成する
        /// </summary>
        /// <returns></returns>
        private static SceneLoadDataSO GetOrCreateSaveData()
        {
            var guids = AssetDatabase.FindAssets("t:SceneLoadDataSO");
            if (guids.Length == 0)
            {
                return CreateSaveData();
            }

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var obj = AssetDatabase.LoadAssetAtPath<SceneLoadDataSO>(path);
            
            return obj;
        }

        /// <summary>
        /// セーブデータの作成を行う
        /// </summary>
        /// <returns></returns>
        private static SceneLoadDataSO CreateSaveData()
        {
            var savePath = EditorUtility.SaveFilePanel("Save", "Assets", "SceneLoaderSaveData", "asset");

            // パスが入っていれば値が入っている
            if (!string.IsNullOrEmpty(savePath)) 
            {
                var temp = Regex.Split(savePath, "/Assets/");
                savePath = "Assets/" + temp[1];

                // ファイルデータを作成
                var createdSaveData = ScriptableObject.CreateInstance<SceneLoadDataSO>();
                
                // ファイルを指定した場所へ保存
                AssetDatabase.CreateAsset(createdSaveData, savePath);

                return createdSaveData;
            }

            return null;
        }
    }
}