using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Windows.Forms;

namespace GenPen
{
    /// <summary>
    /// システムプロンプトの読み込みと管理を担当するクラス
    /// </summary>
    public class PromptTemplate
    {
        private const string PROMPT_FOLDER = "GenPen";
        private const string PROMPT_FILE = "prompt.txt";
        private static string _promptTemplate = string.Empty;
        private static readonly Dictionary<string, string> _variables = new Dictionary<string, string>();

        /// <summary>
        /// プロンプトテンプレートを取得または設定します
        /// </summary>
        public static string Template
        {
            get
            {
                if (string.IsNullOrEmpty(_promptTemplate))
                {
                    LoadPromptTemplate();
                }
                return _promptTemplate;
            }
            set
            {
                _promptTemplate = value;
                SavePromptTemplate();
            }
        }

        /// <summary>
        /// プロンプトテンプレートファイルのパスを取得します
        /// </summary>
        public static string PromptFilePath
        {
            get
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string promptFolderPath = Path.Combine(appDataPath, PROMPT_FOLDER);
                
                if (!Directory.Exists(promptFolderPath))
                {
                    Directory.CreateDirectory(promptFolderPath);
                }
                
                return Path.Combine(promptFolderPath, PROMPT_FILE);
            }
        }

        /// <summary>
        /// プロンプトテンプレートを保存します
        /// </summary>
        private static void SavePromptTemplate()
        {
            try
            {
                File.WriteAllText(PromptFilePath, _promptTemplate, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"プロンプトテンプレートの保存中にエラーが発生しました: {ex.Message}", 
                    "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// プロンプトテンプレートを読み込みます
        /// </summary>
        private static void LoadPromptTemplate()
        {
            try
            {
                if (File.Exists(PromptFilePath))
                {
                    _promptTemplate = File.ReadAllText(PromptFilePath, Encoding.UTF8);
                }
                else
                {
                    // デフォルトのプロンプトテンプレートを作成
                    _promptTemplate = CreateDefaultPromptTemplate();
                    SavePromptTemplate();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"プロンプトテンプレートの読み込み中にエラーが発生しました: {ex.Message}", 
                    "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _promptTemplate = CreateDefaultPromptTemplate();
            }
        }

        /// <summary>
        /// デフォルトのプロンプトテンプレートを作成します
        /// </summary>
        private static string CreateDefaultPromptTemplate()
        {
            return @"あなたは建築・デザイン分野の専門家です。
以下の条件に基づいて、アイデアや解決策を提案してください。

## コンテキスト
{context}

## 要件
{requirements}

## 制約条件
{constraints}

## 出力形式
{output_format}

できるだけ具体的で実用的な提案をお願いします。";
        }

        /// <summary>
        /// 変数を設定します
        /// </summary>
        public static void SetVariable(string key, string value)
        {
            _variables[key] = value;
        }

        /// <summary>
        /// 変数をクリアします
        /// </summary>
        public static void ClearVariables()
        {
            _variables.Clear();
        }

        /// <summary>
        /// プロンプトテンプレートに変数を適用して完成したプロンプトを取得します
        /// </summary>
        public static string GetFormattedPrompt()
        {
            string formattedPrompt = Template;
            
            foreach (var variable in _variables)
            {
                formattedPrompt = formattedPrompt.Replace("{" + variable.Key + "}", variable.Value);
            }
            
            // 未設定の変数を空文字に置換
            foreach (string placeholder in FindPlaceholders(formattedPrompt))
            {
                formattedPrompt = formattedPrompt.Replace("{" + placeholder + "}", "");
            }
            
            return formattedPrompt;
        }

        /// <summary>
        /// プロンプト内のプレースホルダーを検索します
        /// </summary>
        private static IEnumerable<string> FindPlaceholders(string text)
        {
            List<string> placeholders = new List<string>();
            int startIndex = 0;
            
            while ((startIndex = text.IndexOf('{', startIndex)) >= 0)
            {
                int endIndex = text.IndexOf('}', startIndex);
                if (endIndex > startIndex)
                {
                    string placeholder = text.Substring(startIndex + 1, endIndex - startIndex - 1);
                    placeholders.Add(placeholder);
                    startIndex = endIndex + 1;
                }
                else
                {
                    break;
                }
            }
            
            return placeholders;
        }

        /// <summary>
        /// プロンプトテンプレートが存在するかどうかを確認します
        /// </summary>
        public static bool PromptTemplateExists()
        {
            return File.Exists(PromptFilePath);
        }

        /// <summary>
        /// プロンプトテンプレートが存在しない場合、デフォルトのテンプレートを作成します
        /// </summary>
        public static void EnsurePromptTemplateExists()
        {
            if (!PromptTemplateExists())
            {
                Template = CreateDefaultPromptTemplate();
            }
        }
    }
}