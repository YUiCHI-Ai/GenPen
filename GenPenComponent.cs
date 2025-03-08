using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace GenPen
{
    public class GenPenComponent : GH_Component
    {
        /// <summary>
        /// コンストラクタ：新しいインスタンスを初期化します
        /// </summary>
        public GenPenComponent()
            : base("GenPen", "GP",
                "GenPenコンポーネントの説明",
                "GenPen", "Subcategory")
        {
            // 初回起動時の設定（確認ダイアログなし）
            InitializeSettings();
        }

        /// <summary>
        /// 起動時の設定を行います（確認ダイアログなし）
        /// </summary>
        private void InitializeSettings()
        {
            try
            {
                // APIキーが設定されているか確認（ダイアログは表示しない）
                // TokenManagerクラスを直接参照せず、APIキーが設定されているかどうかを確認する
                bool isApiKeySet = false;
                try
                {
                    // TokenManagerクラスのIsApiKeySetプロパティを反射を使用して取得
                    var tokenManagerType = Type.GetType("GenPen.TokenManager, GenPen");
                    if (tokenManagerType != null)
                    {
                        var isApiKeySetProperty = tokenManagerType.GetProperty("IsApiKeySet");
                        if (isApiKeySetProperty != null)
                        {
                            isApiKeySet = (bool)isApiKeySetProperty.GetValue(null);
                        }
                    }
                }
                catch
                {
                    // 反射に失敗した場合は、APIキーが設定されていないと仮定
                    isApiKeySet = false;
                }

                if (!isApiKeySet)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                        "APIキーが設定されていません。GenPenの機能を使用するにはAPIキーが必要です。");
                }

                // プロンプトテンプレートが存在するか確認
                try
                {
                    // PromptTemplateクラスのEnsurePromptTemplateExistsメソッドを反射を使用して呼び出し
                    var promptTemplateType = Type.GetType("GenPen.PromptTemplate, GenPen");
                    if (promptTemplateType != null)
                    {
                        var ensurePromptTemplateExistsMethod = promptTemplateType.GetMethod("EnsurePromptTemplateExists");
                        if (ensurePromptTemplateExistsMethod != null)
                        {
                            ensurePromptTemplateExistsMethod.Invoke(null, null);
                        }
                    }
                }
                catch
                {
                    // 反射に失敗した場合は何もしない
                }
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    $"初期化中にエラーが発生しました: {ex.Message}");
            }
        }

        /// <summary>
        /// コンポーネントがドキュメントに追加されたときに呼び出されます
        /// </summary>
        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            
            try
            {
                // コンポーネント配置時に毎回APIキー設定ダイアログを表示
                DialogResult result = DialogResult.Cancel;
                try
                {
                    // TokenManagerクラスのShowSettingsDialogメソッドを反射を使用して呼び出し
                    var tokenManagerType = Type.GetType("GenPen.TokenManager, GenPen");
                    if (tokenManagerType != null)
                    {
                        var showSettingsDialogMethod = tokenManagerType.GetMethod("ShowSettingsDialog");
                        if (showSettingsDialogMethod != null)
                        {
                            result = (DialogResult)showSettingsDialogMethod.Invoke(null, null);
                        }
                    }
                }
                catch
                {
                    // 反射に失敗した場合は、ダイアログ表示に失敗したと仮定
                    result = DialogResult.Cancel;
                }
                
                // APIキーが設定されているかどうかを確認
                bool isApiKeySet = false;
                try
                {
                    // TokenManagerクラスのIsApiKeySetプロパティを反射を使用して取得
                    var tokenManagerType = Type.GetType("GenPen.TokenManager, GenPen");
                    if (tokenManagerType != null)
                    {
                        var isApiKeySetProperty = tokenManagerType.GetProperty("IsApiKeySet");
                        if (isApiKeySetProperty != null)
                        {
                            isApiKeySet = (bool)isApiKeySetProperty.GetValue(null);
                        }
                    }
                }
                catch
                {
                    // 反射に失敗した場合は、APIキーが設定されていないと仮定
                    isApiKeySet = false;
                }
                
                if (result != DialogResult.OK && !isApiKeySet)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                        "APIキーが設定されていません。GenPenの機能を使用するにはAPIキーが必要です。");
                }
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    $"APIキー設定中にエラーが発生しました: {ex.Message}");
            }
        }

        /// <summary>
        /// コンポーネントの公開GUIDを提供します
        /// </summary>
        public override Guid ComponentGuid => new Guid("a1b2c3d4-e5f6-4a5b-9c8d-7e6f5a4b3c2d");

        /// <summary>
        /// 入力パラメータの登録
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // 入力パラメータを追加
            // 例: pManager.AddGeometryParameter("Geometry", "G", "Geometry to process", GH_ParamAccess.item);
        }

        /// <summary>
        /// 出力パラメータの登録
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // 出力パラメータを追加
            // 例: pManager.AddGeometryParameter("Result", "R", "Processed geometry", GH_ParamAccess.item);
        }

        /// <summary>
        /// アルゴリズムの実行
        /// </summary>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // コンポーネントのロジックをここに実装
        }
    }
}