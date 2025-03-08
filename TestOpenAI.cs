using System;
using System.Threading.Tasks;

namespace GenPen
{
    /// <summary>
    /// OpenAI APIとの通信をテストするためのクラス
    /// </summary>
    public class TestOpenAI
    {
        /// <summary>
        /// テストを実行します
        /// </summary>
        public static async Task RunTest()
        {
            Console.WriteLine("OpenAI API通信テストを開始します...");

            try
            {
                // APIキーが設定されているか確認
                if (!TokenManager.IsApiKeySet)
                {
                    Console.WriteLine("エラー: APIキーが設定されていません。");
                    Console.WriteLine("TokenManager.ShowSettingsDialog()を使用してAPIキーを設定してください。");
                    return;
                }

                // OpenAIServiceのインスタンスを作成
                var openAIService = new OpenAIService();

                // テストプロンプト
                string prompt = "こんにちは、あなたは誰ですか？";
                string model = "gpt-3.5-turbo";
                double temperature = 0.7;

                Console.WriteLine($"プロンプト: {prompt}");
                Console.WriteLine($"モデル: {model}");
                Console.WriteLine($"温度: {temperature}");
                Console.WriteLine("APIリクエスト送信中...");

                // リクエスト送信
                var response = await openAIService.SendRequestAsync(prompt, model, temperature);

                // レスポンス表示
                Console.WriteLine("\n--- レスポンス ---");
                Console.WriteLine($"モデル: {response.Model}");
                Console.WriteLine($"トークン使用量: {response.Usage.TotalTokens}");
                Console.WriteLine($"内容: {response.GetContent()}");
                Console.WriteLine("--- レスポンス終了 ---\n");

                Console.WriteLine("テストが正常に完了しました。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"エラーが発生しました: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"内部エラー: {ex.InnerException.Message}");
                }
            }
        }

        /// <summary>
        /// コンソールアプリケーションとして実行するためのメインメソッド
        /// </summary>
        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("OpenAI APIテストプログラム");
            Console.WriteLine("========================");

            // APIキーが設定されていない場合は設定ダイアログを表示
            if (!TokenManager.IsApiKeySet)
            {
                Console.WriteLine("APIキーが設定されていません。設定ダイアログを表示します...");
                var result = TokenManager.ShowSettingsDialog();
                if (result != System.Windows.Forms.DialogResult.OK)
                {
                    Console.WriteLine("APIキーの設定がキャンセルされました。");
                    Console.WriteLine("Enterキーを押して終了...");
                    Console.ReadLine();
                    return;
                }
            }

            // テスト実行
            RunTest().Wait();

            Console.WriteLine("Enterキーを押して終了...");
            Console.ReadLine();
        }
    }
}