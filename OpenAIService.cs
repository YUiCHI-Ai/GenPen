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
        
        // デバッグログ用のフラグ（通常使用時は無効）
        public static bool EnableDebugLogging { get; set; } = false;

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
        /// デバッグログを記録します（通常使用時は無効）
        /// </summary>
        public static void LogDebug(string message)
        {
            if (!EnableDebugLogging) return;
            
            // デバッグ出力にのみ表示
            Debug.WriteLine(message);
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
            try
            {
                // APIキーを取得
                string apiKey = TokenManager.ApiKey;
                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new InvalidOperationException("APIキーが設定されていません。");
                }

                // HTTPクライアントの設定
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                // リクエストペイロードの作成
                var requestPayload = new ChatCompletionRequest
                {
                    Model = model,
                    Messages = new List<Message>
                    {
                        new Message { Role = "user", Content = prompt }
                    },
                    Temperature = temperature
                };

                // JSONシリアライズ
                string jsonPayload = SerializeToJson(requestPayload);

                // リクエスト送信
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                
                // 同期的にHTTPリクエストを実行
                var response = _httpClient.PostAsync(API_URL, content).ConfigureAwait(false).GetAwaiter().GetResult();
                
                // レスポンスの確認
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    throw new HttpRequestException($"APIリクエストが失敗しました。ステータスコード: {response.StatusCode}, エラー: {errorContent}");
                }

                // レスポンスの取得と解析
                string jsonResponse = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                var result = DeserializeFromJson<ChatCompletionResponse>(jsonResponse);
                
                return result;
            }
            catch (TaskCanceledException ex)
            {
                throw new Exception($"OpenAI APIリクエストがタイムアウトまたはキャンセルされました: {ex.Message}", ex);
            }
            catch (OperationCanceledException ex)
            {
                throw new Exception($"OpenAI APIリクエストがキャンセルされました: {ex.Message}", ex);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"OpenAI APIリクエスト中にネットワークエラーが発生しました: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
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
            try
            {
                // APIキーを取得
                string apiKey = TokenManager.ApiKey;
                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new InvalidOperationException("APIキーが設定されていません。");
                }

                // HTTPクライアントの設定
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                // リクエストペイロードの作成
                var requestPayload = new ChatCompletionRequest
                {
                    Model = model,
                    Messages = new List<Message>
                    {
                        new Message { Role = "user", Content = prompt }
                    },
                    Temperature = temperature
                };

                // JSONシリアライズ
                string jsonPayload = SerializeToJson(requestPayload);

                // リクエスト送信
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                
                // リクエスト送信
                var response = await _httpClient.PostAsync(API_URL, content, cancellationToken);
                
                // レスポンスの確認
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"APIリクエストが失敗しました。ステータスコード: {response.StatusCode}, エラー: {errorContent}");
                }

                // レスポンスの取得と解析
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var result = DeserializeFromJson<ChatCompletionResponse>(jsonResponse);
                
                return result;
            }
            catch (TaskCanceledException ex)
            {
                throw new Exception($"OpenAI APIリクエストがタイムアウトまたはキャンセルされました: {ex.Message}", ex);
            }
            catch (OperationCanceledException ex)
            {
                throw new Exception($"OpenAI APIリクエストがキャンセルされました: {ex.Message}", ex);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"OpenAI APIリクエスト中にネットワークエラーが発生しました: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
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