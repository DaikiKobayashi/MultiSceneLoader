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

        // 追加シーンパスキャッシュ
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
                // データが存在する
                loadSceneList = data;
                return;
            }
            else
            {
                // データが存在しない
                // ディレクトリを作成
                SafeCreateDirectory(k_DataPath);

                // ファイルを作成
                var createData = ScriptableObject.CreateInstance(typeof(SceneLoadDataSO));

                // ファイルを保存
                AssetDatabase.CreateAsset(createData, k_DataPath);
                Debug.Log("Multi scene loader".Coloring("cyan") + " " + "Create new data!");

                // 再帰
                LoadData(loopCount);
            }
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            // メニュー追加
            menu.AddItem(new GUIContent("Open Script"), false, () =>
            {
                string filePath = GetSourceFilePath();
                filePath = filePath.Replace(Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("/") + 1), "");
                UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(filePath, typeof(UnityEngine.Object));
                if (obj != null)
                {
                    Selection.activeObject = obj;
                    // 第２引数は行数
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
                        // シーンロード
                        if (GUILayout.Button("O", GUILayout.Height(EditorGUIUtility.singleLineHeight * 2), GUILayout.Width(EditorGUIUtility.singleLineHeight * 2)))
                        {
                            // シーンを保存するか?
                            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                            {
                                for (int i = 0; i < data.sceneList.Length; i++)
                                {
                                    // 最初に読み込むシーンはシングル読み込み
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

                            // リストから削除
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

                // ドラッグ&ドロップ領域
                Rect dropAreaRect = new Rect(7.5F, position.height - dropAreaHeight - 7.5F, position.width - 15F, dropAreaHeight);
                var list = CreateDragAndDropGUI(dropAreaRect);

                // ドラッグされたオブジェクトを取得
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] is SceneAsset)
                    {
                        var path = AssetDatabase.GetAssetPath(list[i]);

                        // 存在チェック
                        if (!addElementNameList.Contains(path))
                            addElementNameList.Add(AssetDatabase.GetAssetPath(list[i]));
                    }
                }

                // 追加名リスト表示
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

            //D&D出来る場所を描画
            var color = GUI.color;
            GUI.color = Color.red;
            GUI.Box(rect, "");
            GUI.color = color;

            //マウスの位置がD&Dの範囲になければスルー
            if (!rect.Contains(Event.current.mousePosition))
            {
                return list;
            }

            //現在のイベントを取得
            EventType eventType = Event.current.type;

            //ドラッグ＆ドロップで操作が 更新されたとき or 実行したとき
            if (eventType == EventType.DragUpdated || eventType == EventType.DragPerform)
            {
                //カーソルに+のアイコンを表示
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                //ドロップされたオブジェクトをリストに登録
                if (eventType == EventType.DragPerform)
                {
                    list = new List<Object>(DragAndDrop.objectReferences);

                    //ドラッグを受け付ける(ドラッグしてカーソルにくっ付いてたオブジェクトが戻らなくなる)
                    DragAndDrop.AcceptDrag();
                }

                //イベントを使用済みにする
                Event.current.Use();
            }

            return list;
        }

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

        private string GetSourceFilePath([CallerFilePath] string sourceFilePath = "")
        {
            return sourceFilePath.Replace("\\", "/");
        }



        /// <summary>
        /// セッティングポップアップウィンドウ
        /// </summary>
        public class MulltiSceneLoaderPopupContent : PopupWindowContent
        {
            private const int k_ElementCount = 5;
            /// <summary>
            /// サイズを取得する
            /// </summary>
            public override Vector2 GetWindowSize()
            {
                return new Vector2(100, (EditorGUIUtility.singleLineHeight + 3) * k_ElementCount);
            }

            /// <summary>
            /// GUI描画
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

                        // "isLockedプロパティがロックされているか（ウィンドウの鍵マークに対応している）
                        var isLocked = inspectorType.GetProperty("isLocked", BindingFlags.Instance | BindingFlags.Public);

                        // 作製したインスペクタウィンドウのisLockedをtrueに設定する
                        isLocked.GetSetMethod().Invoke(inspectorInstance, new object[] { true });
                    }
                }
            }

            /// <summary>
            /// 開いたときの処理
            /// </summary>
            public override void OnOpen()
            {
            }

            /// <summary>
            /// 閉じたときの処理
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