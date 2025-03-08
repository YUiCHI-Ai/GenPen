using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace GenPen
{
    /// <summary>
    /// AIレスポンスをGrasshopper用のデータ構造に変換するためのクラス
    /// </summary>
    [DataContract]
    public class PromptData
    {
        /// <summary>
        /// AIからのアドバイスやコメント
        /// </summary>
        [DataMember(Name = "advice")]
        public string Advice { get; set; }

        /// <summary>
        /// コンポーネントノードのリスト
        /// </summary>
        [DataMember(Name = "components")]
        public List<ComponentNode> Components { get; set; }

        /// <summary>
        /// コンポーネント間の接続情報のリスト
        /// </summary>
        [DataMember(Name = "connections")]
        public List<ConnectionInfo> Connections { get; set; }

        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public PromptData()
        {
            Components = new List<ComponentNode>();
            Connections = new List<ConnectionInfo>();
        }

        /// <summary>
        /// JSON文字列からPromptDataオブジェクトを生成します
        /// </summary>
        /// <param name="json">JSON文字列</param>
        /// <returns>PromptDataオブジェクト</returns>
        public static PromptData FromJson(string json)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(PromptData));
                    return (PromptData)serializer.ReadObject(ms);
                }
            }
            catch (SerializationException ex)
            {
                throw new FormatException($"JSONのデシリアライズに失敗しました: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// PromptDataオブジェクトをJSON文字列に変換します
        /// </summary>
        /// <returns>JSON文字列</returns>
        public string ToJson()
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(PromptData));
                    serializer.WriteObject(ms, this);
                    ms.Position = 0;
                    using (StreamReader sr = new StreamReader(ms))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch (SerializationException ex)
            {
                throw new FormatException($"JSONのシリアライズに失敗しました: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// PromptDataオブジェクトの内容を検証します
        /// </summary>
        /// <returns>検証結果と問題点のリスト</returns>
        public (bool IsValid, List<string> Issues) Validate()
        {
            List<string> issues = new List<string>();

            // コンポーネントの検証
            if (Components == null || Components.Count == 0)
            {
                issues.Add("コンポーネントが定義されていません");
            }
            else
            {
                // コンポーネントIDの重複チェック
                var duplicateIds = Components
                    .GroupBy(c => c.Id)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicateIds.Any())
                {
                    issues.Add($"重複するコンポーネントIDが存在します: {string.Join(", ", duplicateIds)}");
                }

                // 各コンポーネントの検証
                foreach (var component in Components)
                {
                    if (component.Id <= 0)
                    {
                        issues.Add($"コンポーネント '{component.Name}' のIDが無効です");
                    }

                    if (string.IsNullOrEmpty(component.Name))
                    {
                        issues.Add($"コンポーネント ID:{component.Id} の名前が設定されていません");
                    }
                }
            }

            // 接続情報の検証
            if (Connections != null && Connections.Count > 0)
            {
                foreach (var connection in Connections)
                {
                    // 接続元の検証
                    if (connection.Source == null)
                    {
                        issues.Add("接続元が定義されていません");
                    }
                    else if (connection.Source.ComponentId <= 0)
                    {
                        issues.Add($"接続元のコンポーネントIDが無効です: {connection.Source.ComponentId}");
                    }
                    else if (string.IsNullOrEmpty(connection.Source.ParameterName))
                    {
                        issues.Add($"接続元のパラメータ名が設定されていません: コンポーネントID {connection.Source.ComponentId}");
                    }
                    else if (Components != null && !Components.Any(c => c.Id == connection.Source.ComponentId))
                    {
                        issues.Add($"接続元のコンポーネントID {connection.Source.ComponentId} が存在しません");
                    }

                    // 接続先の検証
                    if (connection.Target == null)
                    {
                        issues.Add("接続先が定義されていません");
                    }
                    else if (connection.Target.ComponentId <= 0)
                    {
                        issues.Add($"接続先のコンポーネントIDが無効です: {connection.Target.ComponentId}");
                    }
                    else if (string.IsNullOrEmpty(connection.Target.ParameterName))
                    {
                        issues.Add($"接続先のパラメータ名が設定されていません: コンポーネントID {connection.Target.ComponentId}");
                    }
                    else if (Components != null && !Components.Any(c => c.Id == connection.Target.ComponentId))
                    {
                        issues.Add($"接続先のコンポーネントID {connection.Target.ComponentId} が存在しません");
                    }
                }
            }

            return (issues.Count == 0, issues);
        }

        /// <summary>
        /// ファイルからPromptDataオブジェクトを読み込みます
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        /// <returns>PromptDataオブジェクト</returns>
        public static PromptData FromFile(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                return FromJson(json);
            }
            catch (IOException ex)
            {
                throw new IOException($"ファイルの読み込みに失敗しました: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// PromptDataオブジェクトをファイルに保存します
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        public void SaveToFile(string filePath)
        {
            try
            {
                string json = ToJson();
                File.WriteAllText(filePath, json);
            }
            catch (IOException ex)
            {
                throw new IOException($"ファイルの保存に失敗しました: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// コンポーネントノードを表すクラス
    /// </summary>
    [DataContract]
    public class ComponentNode
    {
        /// <summary>
        /// コンポーネントの一意のID
        /// </summary>
        [DataMember(Name = "id")]
        public int Id { get; set; }

        /// <summary>
        /// コンポーネントの名前
        /// </summary>
        [DataMember(Name = "name")]
        public string Name { get; set; }

        /// <summary>
        /// コンポーネントの値（オプション）
        /// </summary>
        [DataMember(Name = "value")]
        public string Value { get; set; }

        /// <summary>
        /// コンポーネントの階層レベル（配置時に使用）
        /// </summary>
        [DataMember(Name = "tier")]
        public int Tier { get; set; }

        /// <summary>
        /// コンポーネントの追加情報（オプション）
        /// </summary>
        [DataMember(Name = "metadata", EmitDefaultValue = false)]
        public Dictionary<string, string> Metadata { get; set; }

        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public ComponentNode()
        {
            Metadata = new Dictionary<string, string>();
        }

        /// <summary>
        /// パラメータ付きコンストラクタ
        /// </summary>
        public ComponentNode(int id, string name, string value = null, int tier = 0)
        {
            Id = id;
            Name = name;
            Value = value;
            Tier = tier;
            Metadata = new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// コンポーネント間の接続情報を表すクラス
    /// </summary>
    [DataContract]
    public class ConnectionInfo
    {
        /// <summary>
        /// 接続元のパラメータ情報
        /// </summary>
        [DataMember(Name = "source")]
        public ParameterInfo Source { get; set; }

        /// <summary>
        /// 接続先のパラメータ情報
        /// </summary>
        [DataMember(Name = "target")]
        public ParameterInfo Target { get; set; }

        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public ConnectionInfo()
        {
        }

        /// <summary>
        /// パラメータ付きコンストラクタ
        /// </summary>
        public ConnectionInfo(ParameterInfo source, ParameterInfo target)
        {
            Source = source;
            Target = target;
        }

        /// <summary>
        /// 接続情報が有効かどうかを確認します
        /// </summary>
        /// <returns>有効な場合はtrue、それ以外はfalse</returns>
        public bool IsValid()
        {
            return Source != null && Target != null && Source.IsValid() && Target.IsValid();
        }
    }

    /// <summary>
    /// パラメータ情報を表すクラス
    /// </summary>
    [DataContract]
    public class ParameterInfo
    {
        /// <summary>
        /// パラメータが属するコンポーネントのID
        /// </summary>
        [DataMember(Name = "componentId")]
        public int ComponentId { get; set; }

        /// <summary>
        /// パラメータの名前
        /// </summary>
        [DataMember(Name = "parameterName")]
        public string ParameterName { get; set; }

        /// <summary>
        /// パラメータのインデックス（オプション）
        /// </summary>
        [DataMember(Name = "index")]
        public int? Index { get; set; }

        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public ParameterInfo()
        {
        }

        /// <summary>
        /// パラメータ付きコンストラクタ
        /// </summary>
        public ParameterInfo(int componentId, string parameterName, int? index = null)
        {
            ComponentId = componentId;
            ParameterName = parameterName;
            Index = index;
        }

        /// <summary>
        /// パラメータ情報が有効かどうかを確認します
        /// </summary>
        /// <returns>有効な場合はtrue、それ以外はfalse</returns>
        public bool IsValid()
        {
            return ComponentId > 0 && !string.IsNullOrEmpty(ParameterName);
        }
    }
}