using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Collections.Generic;
using System.Diagnostics;

namespace GenPen
{
    /// <summary>
    /// OpenAI APIとの通信を担当するクラス
    /// </summary>
    public class OpenAIService
    {
        private const string API_URL = "https://api.openai.com/v1/chat/completions";
        private readonly HttpClient _httpClient;
        
        // デバッグログ用のフラグ
        public static bool EnableDebugLogging { get; set; } = true;
        
        // デバッグログファイルのパス
        private static readonly string DebugLogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GenPen", "debug_log.txt");

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public OpenAIService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30); // 30秒のタイムアウトを設定
            
            // デバッグログの初期化
            if (EnableDebugLogging)
            {
                LogDebug("OpenAIService インスタンスを作成しました");
                LogDebug($"HTTPクライアントのタイムアウト: {_httpClient.Timeout.TotalSeconds}秒");
            }
        }
        
        /// <summary>
        /// デバッグログを記録します
        /// </summary>
        public static void LogDebug(string message)
        {
            if (!EnableDebugLogging) return;
            
            try
            {
                string logDirectory = Path.GetDirectoryName(DebugLogPath);
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
                
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logMessage = $"[{timestamp}] {message}";
                
                // ファイルに追記
                File.AppendAllText(DebugLogPath, logMessage + Environment.NewLine);
                
                // デバッグ出力にも表示
                Debug.WriteLine(logMessage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ログ記録中にエラーが発生しました: {ex.Message}");
            }
        }

        /// <summary>
        /// OpenAI APIにリクエストを送信し、レスポンスを取得します（同期バージョン）
        /// </summary>
        /// <param name="prompt">送信するプロンプト</param>
        /// <param name="model">使用するモデル（デフォルトはgpt-3.5-turbo）</param>
        /// <param name="temperature">温度パラメータ（デフォルトは0.7）</param>
        /// <returns>APIからのレスポンス</returns>
        public ChatCompletionResponse SendRequest(string prompt, string model = "gpt-3.5-turbo", double temperature = 0.7)
        {
            LogDebug("SendRequest（同期メソッド）開始");
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            try
            {
                // APIキーを取得
                LogDebug("APIキーを取得中...");
                string apiKey = TokenManager.ApiKey;
                if (string.IsNullOrEmpty(apiKey))
                {
                    LogDebug("エラー: APIキーが設定されていません");
                    throw new InvalidOperationException("APIキーが設定されていません。");
                }
                LogDebug("APIキーの取得に成功しました");

                // HTTPクライアントの設定
                LogDebug("HTTPクライアントを設定中...");
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                LogDebug("HTTPクライアントの設定が完了しました");

                // リクエストペイロードの作成
                LogDebug("リクエストペイロードを作成中...");
                var requestPayload = new ChatCompletionRequest
                {
                    Model = model,
                    Messages = new List<Message>
                    {
                        new Message { Role = "user", Content = prompt }
                    },
                    Temperature = temperature
                };
                LogDebug("リクエストペイロードの作成が完了しました");

                // JSONシリアライズ
                LogDebug("JSONシリアライズを実行中...");
                string jsonPayload = SerializeToJson(requestPayload);
                LogDebug($"JSONシリアライズが完了しました (長さ: {jsonPayload.Length}文字)");

                // リクエスト送信（同期的に実行）
                LogDebug($"APIリクエストを送信中... URL: {API_URL}");
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                
                // タイムアウト時間をログに記録
                LogDebug($"現在のHTTPクライアントタイムアウト: {_httpClient.Timeout.TotalSeconds}秒");
                
                // 同期的にHTTPリクエストを送信
                DateTime requestStartTime = DateTime.Now;
                LogDebug($"同期リクエスト開始時刻: {requestStartTime.ToString("HH:mm:ss.fff")}");
                
                // 同期的にHTTPリクエストを実行
                var response = _httpClient.PostAsync(API_URL, content).ConfigureAwait(false).GetAwaiter().GetResult();
                
                DateTime requestEndTime = DateTime.Now;
                TimeSpan requestDuration = requestEndTime - requestStartTime;
                LogDebug($"同期リクエスト完了時刻: {requestEndTime.ToString("HH:mm:ss.fff")} (所要時間: {requestDuration.TotalSeconds:F2}秒)");

                // レスポンスの確認
                LogDebug($"レスポンスステータスコード: {response.StatusCode}");
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    LogDebug($"APIリクエストエラー: ステータスコード={response.StatusCode}, エラー内容={errorContent}");
                    throw new HttpRequestException($"APIリクエストが失敗しました。ステータスコード: {response.StatusCode}, エラー: {errorContent}");
                }

                // レスポンスの取得と解析
                LogDebug("レスポンスの内容を読み込み中...");
                string jsonResponse = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                LogDebug($"レスポンスの読み込みが完了しました (長さ: {jsonResponse.Length}文字)");
                
                LogDebug("レスポンスをデシリアライズ中...");
                var result = DeserializeFromJson<ChatCompletionResponse>(jsonResponse);
                LogDebug($"デシリアライズが完了しました: モデル={result.Model}, トークン数={result.Usage?.TotalTokens ?? 0}");
                
                stopwatch.Stop();
                LogDebug($"SendRequest（同期メソッド）完了: 所要時間={stopwatch.ElapsedMilliseconds}ms");
                
                return result;
            }
            catch (TaskCanceledException ex)
            {
                stopwatch.Stop();
                LogDebug($"タスクキャンセル例外が発生しました: {ex.Message}, 所要時間={stopwatch.ElapsedMilliseconds}ms");
                throw new Exception($"OpenAI APIリクエストがタイムアウトまたはキャンセルされました: {ex.Message}", ex);
            }
            catch (OperationCanceledException ex)
            {
                stopwatch.Stop();
                LogDebug($"操作キャンセル例外が発生しました: {ex.Message}, 所要時間={stopwatch.ElapsedMilliseconds}ms");
                throw new Exception($"OpenAI APIリクエストがキャンセルされました: {ex.Message}", ex);
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                LogDebug($"HTTP例外が発生しました: {ex.Message}, 所要時間={stopwatch.ElapsedMilliseconds}ms");
                throw new Exception($"OpenAI APIリクエスト中にネットワークエラーが発生しました: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogDebug($"例外が発生しました: {ex.GetType().Name}: {ex.Message}, 所要時間={stopwatch.ElapsedMilliseconds}ms");
                if (ex.InnerException != null)
                {
                    LogDebug($"内部例外: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                }
                throw new Exception($"OpenAI APIリクエスト中にエラーが発生しました: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// OpenAI APIにリクエストを送信し、レスポンスを取得します（非同期バージョン）
        /// </summary>
        /// <param name="prompt">送信するプロンプト</param>
        /// <param name="model">使用するモデル（デフォルトはgpt-3.5-turbo）</param>
        /// <param name="temperature">温度パラメータ（デフォルトは0.7）</param>
        /// <param name="cancellationToken">キャンセレーショントークン（デフォルトはdefault）</param>
        /// <returns>APIからのレスポンス</returns>
        public async Task<ChatCompletionResponse> SendRequestAsync(string prompt, string model = "gpt-3.5-turbo", double temperature = 0.7, System.Threading.CancellationToken cancellationToken = default)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            LogDebug($"SendRequestAsync開始: モデル={model}, 温度={temperature}, プロンプト長={prompt?.Length ?? 0}文字");
            
            try
            {
                // APIキーを取得
                LogDebug("APIキーを取得中...");
                string apiKey = TokenManager.ApiKey;
                if (string.IsNullOrEmpty(apiKey))
                {
                    LogDebug("エラー: APIキーが設定されていません");
                    throw new InvalidOperationException("APIキーが設定されていません。");
                }
                LogDebug("APIキーの取得に成功しました");

                // HTTPクライアントの設定
                LogDebug("HTTPクライアントを設定中...");
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                LogDebug("HTTPクライアントの設定が完了しました");

                // リクエストペイロードの作成
                LogDebug("リクエストペイロードを作成中...");
                var requestPayload = new ChatCompletionRequest
                {
                    Model = model,
                    Messages = new List<Message>
                    {
                        new Message { Role = "user", Content = prompt }
                    },
                    Temperature = temperature
                };
                LogDebug("リクエストペイロードの作成が完了しました");

                // JSONシリアライズ
                LogDebug("JSONシリアライズを実行中...");
                string jsonPayload = SerializeToJson(requestPayload);
                LogDebug($"JSONシリアライズが完了しました (長さ: {jsonPayload.Length}文字)");

                // リクエスト送信
                LogDebug($"APIリクエストを送信中... URL: {API_URL}");
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                
                // タイムアウト時間をログに記録
                LogDebug($"現在のHTTPクライアントタイムアウト: {_httpClient.Timeout.TotalSeconds}秒");
                LogDebug($"キャンセレーショントークンのキャンセル状態: {cancellationToken.IsCancellationRequested}");
                
                // 非同期タスクの開始時間を記録
                DateTime requestStartTime = DateTime.Now;
                LogDebug($"PostAsync開始時刻: {requestStartTime.ToString("HH:mm:ss.fff")}");
                
                // リクエスト送信（この部分で固まる可能性がある）
                var response = await _httpClient.PostAsync(API_URL, content, cancellationToken);
                
                // リクエスト完了時間を記録
                DateTime requestEndTime = DateTime.Now;
                TimeSpan requestDuration = requestEndTime - requestStartTime;
                LogDebug($"PostAsync完了時刻: {requestEndTime.ToString("HH:mm:ss.fff")} (所要時間: {requestDuration.TotalSeconds:F2}秒)");

                // レスポンスの確認
                LogDebug($"レスポンスステータスコード: {response.StatusCode}");
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    LogDebug($"APIリクエストエラー: ステータスコード={response.StatusCode}, エラー内容={errorContent}");
                    throw new HttpRequestException($"APIリクエストが失敗しました。ステータスコード: {response.StatusCode}, エラー: {errorContent}");
                }

                // レスポンスの取得と解析
                LogDebug("レスポンスの内容を読み込み中...");
                string jsonResponse = await response.Content.ReadAsStringAsync();
                LogDebug($"レスポンスの読み込みが完了しました (長さ: {jsonResponse.Length}文字)");
                
                LogDebug("レスポンスをデシリアライズ中...");
                var result = DeserializeFromJson<ChatCompletionResponse>(jsonResponse);
                LogDebug($"デシリアライズが完了しました: モデル={result.Model}, トークン数={result.Usage?.TotalTokens ?? 0}");
                
                stopwatch.Stop();
                LogDebug($"SendRequestAsync完了: 所要時間={stopwatch.ElapsedMilliseconds}ms");
                
                return result;
            }
            catch (TaskCanceledException ex)
            {
                stopwatch.Stop();
                LogDebug($"タスクキャンセル例外が発生しました: {ex.Message}, 所要時間={stopwatch.ElapsedMilliseconds}ms");
                throw new Exception($"OpenAI APIリクエストがタイムアウトまたはキャンセルされました: {ex.Message}", ex);
            }
            catch (OperationCanceledException ex)
            {
                stopwatch.Stop();
                LogDebug($"操作キャンセル例外が発生しました: {ex.Message}, 所要時間={stopwatch.ElapsedMilliseconds}ms");
                throw new Exception($"OpenAI APIリクエストがキャンセルされました: {ex.Message}", ex);
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                LogDebug($"HTTP例外が発生しました: {ex.Message}, 所要時間={stopwatch.ElapsedMilliseconds}ms");
                throw new Exception($"OpenAI APIリクエスト中にネットワークエラーが発生しました: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogDebug($"例外が発生しました: {ex.GetType().Name}: {ex.Message}, 所要時間={stopwatch.ElapsedMilliseconds}ms");
                if (ex.InnerException != null)
                {
                    LogDebug($"内部例外: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                }
                throw new Exception($"OpenAI APIリクエスト中にエラーが発生しました: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// オブジェクトをJSON文字列にシリアライズします
        /// </summary>
        private string SerializeToJson<T>(T obj)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
                serializer.WriteObject(ms, obj);
                ms.Position = 0;
                using (StreamReader sr = new StreamReader(ms))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// JSON文字列からオブジェクトにデシリアライズします
        /// </summary>
        private T DeserializeFromJson<T>(string json)
        {
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
                return (T)serializer.ReadObject(ms);
            }
        }
    }

    /// <summary>
    /// OpenAI APIのチャット完了リクエストを表すクラス
    /// </summary>
    [DataContract]
    public class ChatCompletionRequest
    {
        [DataMember(Name = "model")]
        public string Model { get; set; }

        [DataMember(Name = "messages")]
        public List<Message> Messages { get; set; }

        [DataMember(Name = "temperature")]
        public double Temperature { get; set; }
    }

    /// <summary>
    /// OpenAI APIのメッセージを表すクラス
    /// </summary>
    [DataContract]
    public class Message
    {
        [DataMember(Name = "role")]
        public string Role { get; set; }

        [DataMember(Name = "content")]
        public string Content { get; set; }
    }

    /// <summary>
    /// OpenAI APIのチャット完了レスポンスを表すクラス
    /// </summary>
    [DataContract]
    public class ChatCompletionResponse
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "object")]
        public string Object { get; set; }

        [DataMember(Name = "created")]
        public long Created { get; set; }

        [DataMember(Name = "model")]
        public string Model { get; set; }

        [DataMember(Name = "choices")]
        public List<Choice> Choices { get; set; }

        [DataMember(Name = "usage")]
        public Usage Usage { get; set; }

        /// <summary>
        /// 最初の選択肢のコンテンツを取得します
        /// </summary>
        public string GetContent()
        {
            if (Choices != null && Choices.Count > 0 && Choices[0].Message != null)
            {
                return Choices[0].Message.Content;
            }
            return string.Empty;
        }
    }

    /// <summary>
    /// OpenAI APIのレスポンスの選択肢を表すクラス
    /// </summary>
    [DataContract]
    public class Choice
    {
        [DataMember(Name = "index")]
        public int Index { get; set; }

        [DataMember(Name = "message")]
        public Message Message { get; set; }

        [DataMember(Name = "finish_reason")]
        public string FinishReason { get; set; }
    }

    /// <summary>
    /// OpenAI APIのトークン使用量を表すクラス
    /// </summary>
    [DataContract]
    public class Usage
    {
        [DataMember(Name = "prompt_tokens")]
        public int PromptTokens { get; set; }

        [DataMember(Name = "completion_tokens")]
        public int CompletionTokens { get; set; }

        [DataMember(Name = "total_tokens")]
        public int TotalTokens { get; set; }
    }
}