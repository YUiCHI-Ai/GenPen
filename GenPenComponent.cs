using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Special;
using Rhino.Geometry;

namespace GenPen
{
    public class GenPenComponent : GH_Component
    {
        /// <summary>
        /// コンストラクタ：新しいインスタンスを初期化します（テスト用コメント変更）
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
                
                // モデル選択用のValue Listを作成
                try
                {
                    CreateModelValueList(document);
                }
                catch (Exception ex)
                {
                    // Value List作成に失敗した場合でもコンポーネントは正常に動作するようにする
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                        $"モデル選択用のValue List作成に失敗しました: {ex.Message}");
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                        "モデル選択はコンポーネントの「モデル」入力に直接テキストを接続することでも可能です。");
                }
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    $"APIキー設定中にエラーが発生しました: {ex.Message}");
            }
        }
        
        /// <summary>
        /// モデル選択用のValue Listを作成します
        /// </summary>
        private void CreateModelValueList(GH_Document document)
        {
            try
            {
                // ドキュメントがnullでないことを確認
                if (document == null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "ドキュメントがnullです。Value Listを作成できません。");
                    return;
                }
                
                // 利用可能なAIモデルのリスト
                List<string> availableModels = new List<string>
                {
                    "gpt-3.5-turbo",
                    "gpt-4",
                    "gpt-4o",
                    "gpt-4o-mini",
                    "o1",
                    "o1-pro-mode",
                    "o1-mini",
                    "o3-mini",
                    "o3-mini-high",
                    "gpt-4.5"
                };
                
                // Value Listコンポーネントを作成
                GH_ValueList valueList = new GH_ValueList();
                valueList.CreateAttributes();
                
                // Value Listの位置を設定（GenPenコンポーネントの左側）
                try
                {
                    valueList.Attributes.Pivot = new System.Drawing.PointF(
                        this.Attributes.Pivot.X - 150, // コンポーネントの左側に配置
                        this.Attributes.Pivot.Y);      // 同じ高さに配置
                }
                catch (Exception posEx)
                {
                    // 位置設定に失敗した場合はデフォルト位置を使用
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                        $"Value Listの位置設定に失敗しました: {posEx.Message}");
                }
                
                // Value Listの名前と説明を設定
                valueList.NickName = "AIモデル";
                valueList.Description = "使用するAIモデルを選択";
                
                // Value Listをクリアして新しい値を追加
                valueList.ListItems.Clear();
                
                // 各モデルをValue Listに追加
                foreach (string model in availableModels)
                {
                    valueList.ListItems.Add(new GH_ValueListItem(model, $"\"{model}\""));
                }
                
                // デフォルト値を設定
                if (valueList.ListItems.Count > 0)
                {
                    valueList.ListItems[0].Selected = true;
                }
                
                // Value Listをドキュメントに追加
                document.AddObject(valueList, false);
                
                // ユーザーに接続を促すメッセージを表示
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    "AIモデル選択用のValue Listが作成されました。Value ListをGenPenコンポーネントの「モデル」入力に接続してください。");
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    $"Value List作成中にエラーが発生しました: {ex.Message}");
                
                if (ex.InnerException != null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                        $"内部エラー: {ex.InnerException.Message}");
                }
            }
        }

        /// <summary>
        /// コンポーネントの公開GUIDを提供します
        /// </summary>
        public override Guid ComponentGuid => new Guid("a1b2c3d4-e5f6-4a5b-9c8d-7e6f5a4b3c2d");

        // OpenAIServiceのインスタンス
        private dynamic _openAIService;

        /// <summary>
        /// 入力パラメータの登録
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("プロンプト", "P", "AIに送信するプロンプト", GH_ParamAccess.item);
            pManager.AddTextParameter("モデル", "M", "使用するOpenAIモデル", GH_ParamAccess.item, "gpt-3.5-turbo");
            pManager.AddNumberParameter("温度", "T", "生成の多様性（0.0～1.0）", GH_ParamAccess.item, 0.7);
        }

        /// <summary>
        /// 出力パラメータの登録
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("レスポンス", "R", "AIからのレスポンス", GH_ParamAccess.item);
            pManager.AddTextParameter("モデル", "M", "使用されたモデル", GH_ParamAccess.item);
            pManager.AddIntegerParameter("トークン数", "T", "使用されたトークンの総数", GH_ParamAccess.item);
            pManager.AddTextParameter("ステータス", "S", "API通信のステータス", GH_ParamAccess.item);
        }

        /// <summary>
        /// アルゴリズムの実行
        /// </summary>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 入力パラメータの取得
            string prompt = string.Empty;
            string model = "gpt-3.5-turbo";
            double temperature = 0.7;

            if (!DA.GetData(0, ref prompt))
            {
                return;
            }
            DA.GetData(1, ref model);
            DA.GetData(2, ref temperature);

            // ステータスメッセージの初期化
            string statusMessage = "準備完了";

            // APIキーが設定されているか確認
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
            catch (Exception)
            {
                // 反射に失敗した場合は、APIキーが設定されていないと仮定
                isApiKeySet = false;
            }

            if (!isApiKeySet)
            {
                statusMessage = "エラー: APIキーが設定されていません。";
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, statusMessage);
                DA.SetData(3, statusMessage);
                return;
            }

            try
            {
                // OpenAIServiceのインスタンスがなければ作成
                if (_openAIService == null)
                {
                    // OpenAIServiceクラスのインスタンスを作成
                    var openAIServiceType = Type.GetType("GenPen.OpenAIService, GenPen");
                    if (openAIServiceType != null)
                    {
                        _openAIService = Activator.CreateInstance(openAIServiceType) as dynamic;
                        statusMessage = "OpenAIServiceを初期化しました。";
                    }
                    else
                    {
                        statusMessage = "エラー: OpenAIServiceクラスが見つかりません。";
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, statusMessage);
                        DA.SetData(3, statusMessage);
                        return;
                    }
                }

                // リクエスト送信前のステータス更新
                statusMessage = "APIリクエスト送信中...";
                DA.SetData(3, statusMessage);

                // 非同期処理を同期的に実行（Grasshopperの制約）
                try
                {
                    // キャンセレーショントークンを作成（30秒でタイムアウト）
                    var cancellationTokenSource = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30));
                    
                    // 同期メソッドを使用（デッドロック回避のため）
                    System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
                    stopwatch.Start();
                    
                    // 同期メソッドを使用
                    dynamic response = _openAIService.SendRequest(
                        prompt,
                        model,
                        temperature
                    );
                    
                    stopwatch.Stop();

                    // 出力パラメータの設定
                    DA.SetData(0, response.GetContent());
                    DA.SetData(1, response.Model);
                    DA.SetData(2, response.Usage.TotalTokens);
                    
                    // 成功ステータスの設定
                    statusMessage = $"成功: {response.Usage.TotalTokens}トークンを使用しました。";
                    DA.SetData(3, statusMessage);
                }
                catch (System.Threading.Tasks.TaskCanceledException)
                {
                    // タイムアウトエラーの処理
                    statusMessage = "エラー: APIリクエストがタイムアウトしました。";
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, statusMessage);
                    DA.SetData(3, statusMessage);
                }
                catch (System.AggregateException ex)
                {
                    // 複数の例外が集約されている場合
                    statusMessage = $"エラー: {ex.InnerException?.Message ?? ex.Message}";
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, statusMessage);
                    DA.SetData(3, statusMessage);
                }
            }
            catch (Exception ex)
            {
                // エラーメッセージを表示
                statusMessage = $"エラー: {ex.Message}";
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, statusMessage);
                DA.SetData(3, statusMessage);
                
                if (ex.InnerException != null)
                {
                    string innerErrorMessage = $"内部エラー: {ex.InnerException.Message}";
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, innerErrorMessage);
                }
            }
        }
    }
}