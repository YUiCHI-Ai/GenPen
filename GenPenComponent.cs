using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Grasshopper.Kernel;
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
            // 初回起動時の設定
            InitializeSettings();
        }

        /// <summary>
        /// 起動時の設定を行います
        /// </summary>
        private void InitializeSettings()
        {
            try
            {
                // APIキー設定ダイアログを表示
                DialogResult result = TokenManager.EnsureApiKeyIsSet();
                
                if (result != DialogResult.OK && !TokenManager.IsApiKeySet)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                        "APIキーが設定されていません。GenPenの機能を使用するにはAPIキーが必要です。");
                }

                // プロンプトテンプレートが存在するか確認
                PromptTemplate.EnsurePromptTemplateExists();
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    $"初期化中にエラーが発生しました: {ex.Message}");
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