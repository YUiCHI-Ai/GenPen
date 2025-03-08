using System;
using System.IO;

namespace GenPen
{
    /// <summary>
    /// PromptDataクラスのテスト用クラス
    /// </summary>
    public class PromptDataTest
    {
        /// <summary>
        /// JSONデシリアライズのテストを実行します
        /// </summary>
        /// <param name="jsonFilePath">JSONファイルのパス</param>
        public static void TestJsonDeserialization(string jsonFilePath)
        {
            try
            {
                Console.WriteLine("=== PromptDataテスト開始 ===");
                Console.WriteLine($"JSONファイル: {jsonFilePath}");
                Console.WriteLine();

                // JSONファイルの読み込み
                Console.WriteLine("JSONファイルを読み込み中...");
                PromptData promptData = PromptData.FromFile(jsonFilePath);
                Console.WriteLine("JSONファイルの読み込みに成功しました。");
                Console.WriteLine();

                // デシリアライズされたオブジェクトの内容を表示
                Console.WriteLine("=== デシリアライズされたデータ ===");
                Console.WriteLine($"アドバイス: {promptData.Advice}");
                Console.WriteLine();

                Console.WriteLine("=== コンポーネント ===");
                if (promptData.Components != null && promptData.Components.Count > 0)
                {
                    foreach (var component in promptData.Components)
                    {
                        Console.WriteLine($"ID: {component.Id}, 名前: {component.Name}, 値: {component.Value ?? "なし"}, 階層: {component.Tier}");
                        
                        if (component.Metadata != null && component.Metadata.Count > 0)
                        {
                            Console.WriteLine("  メタデータ:");
                            foreach (var meta in component.Metadata)
                            {
                                Console.WriteLine($"    {meta.Key}: {meta.Value}");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("コンポーネントはありません。");
                }
                Console.WriteLine();

                Console.WriteLine("=== 接続情報 ===");
                if (promptData.Connections != null && promptData.Connections.Count > 0)
                {
                    foreach (var connection in promptData.Connections)
                    {
                        Console.WriteLine($"接続: {connection.Source.ComponentId}.{connection.Source.ParameterName} -> {connection.Target.ComponentId}.{connection.Target.ParameterName}");
                    }
                }
                else
                {
                    Console.WriteLine("接続情報はありません。");
                }

                // 接続情報が表示されない問題を確認
                Console.WriteLine($"接続情報の数: {promptData.Connections?.Count ?? 0}");
                if (promptData.Connections != null && promptData.Connections.Count > 0)
                {
                    Console.WriteLine("接続情報の詳細:");
                    for (int i = 0; i < promptData.Connections.Count; i++)
                    {
                        var connection = promptData.Connections[i];
                        Console.WriteLine($"接続 {i+1}:");
                        Console.WriteLine($"  Source: ComponentId={connection.Source?.ComponentId}, ParameterName={connection.Source?.ParameterName}");
                        Console.WriteLine($"  Target: ComponentId={connection.Target?.ComponentId}, ParameterName={connection.Target?.ParameterName}");
                    }
                }
                Console.WriteLine();

                // バリデーション結果の表示
                Console.WriteLine("=== バリデーション結果 ===");
                var validationResult = promptData.Validate();
                if (validationResult.IsValid)
                {
                    Console.WriteLine("バリデーション成功: データは有効です。");
                }
                else
                {
                    Console.WriteLine("バリデーション失敗: 以下の問題があります。");
                    foreach (var issue in validationResult.Issues)
                    {
                        Console.WriteLine($"- {issue}");
                    }
                }

                // JSONシリアライズのテスト
                Console.WriteLine();
                Console.WriteLine("=== JSONシリアライズのテスト ===");
                string serializedJson = promptData.ToJson();
                Console.WriteLine("シリアライズされたJSON:");
                Console.WriteLine(serializedJson);

                Console.WriteLine();
                Console.WriteLine("=== PromptDataテスト終了 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"エラーが発生しました: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"内部エラー: {ex.InnerException.Message}");
                }
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// メインメソッド
        /// </summary>
        public static void Main()
        {
            string jsonFilePath = "PromptDataSample.json";
            TestJsonDeserialization(jsonFilePath);
        }
    }
}