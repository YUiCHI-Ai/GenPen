using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
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
            pManager.AddBooleanParameter("テスト実行", "Test", "テストモードで実行する", GH_ParamAccess.item, false);
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
            // デバッグログ用のメソッド
            void LogDebug(string message)
            {
                try
                {
                    // OpenAIService.LogDebugメソッドを反射で呼び出す
                    var openAIServiceType = Type.GetType("GenPen.OpenAIService, GenPen");
                    if (openAIServiceType != null)
                    {
                        var logDebugMethod = openAIServiceType.GetMethod("LogDebug", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        if (logDebugMethod != null)
                        {
                            logDebugMethod.Invoke(null, new object[] { $"[GenPenComponent] {message}" });
                            return;
                        }
                    }
                    
                    // 反射に失敗した場合はデバッグ出力に直接書き込む
                    System.Diagnostics.Debug.WriteLine($"[GenPenComponent] {message}");
                }
                catch
                {
                    // ログ記録中のエラーは無視
                }
            }
            
            LogDebug("SolveInstance開始");
            
            // 入力パラメータの取得
            string prompt = string.Empty;
            string model = "gpt-3.5-turbo";
            double temperature = 0.7;
            bool isTestMode = false;

            if (!DA.GetData(0, ref prompt))
            {
                LogDebug("プロンプトが提供されていません");
                return;
            }
            DA.GetData(1, ref model);
            DA.GetData(2, ref temperature);
            DA.GetData(3, ref isTestMode);
            
            LogDebug($"入力パラメータ: モデル={model}, 温度={temperature}, テストモード={isTestMode}");
            LogDebug($"プロンプト長: {prompt.Length}文字");

            // ステータスメッセージの初期化
            string statusMessage = "準備完了";

            // APIキーが設定されているか確認
            bool isApiKeySet = false;
            try
            {
                LogDebug("TokenManagerのIsApiKeySetプロパティを取得中...");
                // TokenManagerクラスのIsApiKeySetプロパティを反射を使用して取得
                var tokenManagerType = Type.GetType("GenPen.TokenManager, GenPen");
                if (tokenManagerType != null)
                {
                    var isApiKeySetProperty = tokenManagerType.GetProperty("IsApiKeySet");
                    if (isApiKeySetProperty != null)
                    {
                        isApiKeySet = (bool)isApiKeySetProperty.GetValue(null);
                        LogDebug($"APIキー設定状態: {isApiKeySet}");
                    }
                    else
                    {
                        LogDebug("IsApiKeySetプロパティが見つかりません");
                    }
                }
                else
                {
                    LogDebug("TokenManagerクラスが見つかりません");
                }
            }
            catch (Exception ex)
            {
                // 反射に失敗した場合は、APIキーが設定されていないと仮定
                LogDebug($"APIキー設定状態の取得中にエラー: {ex.Message}");
                isApiKeySet = false;
            }

            if (!isApiKeySet)
            {
                statusMessage = "エラー: APIキーが設定されていません。";
                LogDebug(statusMessage);
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, statusMessage);
                DA.SetData(3, statusMessage);
                return;
            }

            try
            {
                // テストモードの場合はデフォルトのプロンプトを使用
                if (isTestMode && string.IsNullOrWhiteSpace(prompt))
                {
                    prompt = "こんにちは、あなたは誰ですか？";
                    statusMessage = "テストモード: デフォルトプロンプトを使用します。";
                    LogDebug(statusMessage);
                }

                // OpenAIServiceのインスタンスがなければ作成
                if (_openAIService == null)
                {
                    LogDebug("OpenAIServiceのインスタンスを作成中...");
                    // OpenAIServiceクラスのインスタンスを作成
                    var openAIServiceType = Type.GetType("GenPen.OpenAIService, GenPen");
                    if (openAIServiceType != null)
                    {
                        _openAIService = Activator.CreateInstance(openAIServiceType) as dynamic;
                        statusMessage = "OpenAIServiceを初期化しました。";
                        LogDebug("OpenAIServiceのインスタンス作成に成功しました");
                    }
                    else
                    {
                        statusMessage = "エラー: OpenAIServiceクラスが見つかりません。";
                        LogDebug(statusMessage);
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, statusMessage);
                        DA.SetData(3, statusMessage);
                        return;
                    }
                }
                else
                {
                    LogDebug("既存のOpenAIServiceインスタンスを使用します");
                }

                // リクエスト送信前のステータス更新
                statusMessage = "APIリクエスト送信中...";
                LogDebug(statusMessage);
                DA.SetData(3, statusMessage);

                // 非同期処理を同期的に実行（Grasshopperの制約）
                try
                {
                    // キャンセレーショントークンを作成（30秒でタイムアウト）
                    LogDebug("キャンセレーショントークンを作成中 (タイムアウト: 30秒)");
                    var cancellationTokenSource = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30));
                    
                    // 同期メソッドを使用（デッドロック回避のため）
                    LogDebug("SendRequest（同期メソッド）を呼び出し中...");
                    System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
                    stopwatch.Start();
                    
                    // 同期メソッドを使用
                    dynamic response = _openAIService.SendRequest(
                        prompt,
                        model,
                        temperature
                    );
                    
                    stopwatch.Stop();
                    LogDebug($"SendRequest（同期メソッド）完了: 所要時間={stopwatch.ElapsedMilliseconds}ms");

                    // 出力パラメータの設定
                    LogDebug("レスポンスを出力パラメータに設定中...");
                    DA.SetData(0, response.GetContent());
                    DA.SetData(1, response.Model);
                    DA.SetData(2, response.Usage.TotalTokens);
                    
                    // 成功ステータスの設定
                    statusMessage = $"成功: {response.Usage.TotalTokens}トークンを使用しました。";
                    LogDebug(statusMessage);
                    DA.SetData(3, statusMessage);

                    // テストモードの場合は詳細情報を表示
                    if (isTestMode)
                    {
                        string testMessage = $"テスト結果: プロンプト「{prompt}」に対するレスポンスを受信しました。";
                        LogDebug(testMessage);
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, testMessage);
                        
                        string modelMessage = $"モデル: {response.Model}, トークン数: {response.Usage.TotalTokens}";
                        LogDebug(modelMessage);
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, modelMessage);
                    }
                }
                catch (System.Threading.Tasks.TaskCanceledException ex)
                {
                    // タイムアウトエラーの処理
                    statusMessage = "エラー: APIリクエストがタイムアウトしました。";
                    LogDebug($"{statusMessage} 例外: {ex.Message}");
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, statusMessage);
                    DA.SetData(3, statusMessage);
                }
                catch (System.AggregateException ex)
                {
                    // 複数の例外が集約されている場合
                    statusMessage = $"エラー: {ex.InnerException?.Message ?? ex.Message}";
                    LogDebug($"集約例外: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        LogDebug($"内部例外: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                    }
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, statusMessage);
                    DA.SetData(3, statusMessage);
                }
            }
            catch (Exception ex)
            {
                // エラーメッセージを表示
                statusMessage = $"エラー: {ex.Message}";
                LogDebug($"例外が発生しました: {ex.GetType().Name}: {ex.Message}");
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, statusMessage);
                DA.SetData(3, statusMessage);
                
                if (ex.InnerException != null)
                {
                    string innerErrorMessage = $"内部エラー: {ex.InnerException.Message}";
                    LogDebug($"内部例外: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, innerErrorMessage);
                }
            }
            
            LogDebug("SolveInstance終了");
        }
    }
}