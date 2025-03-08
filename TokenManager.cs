using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace GenPen
{
    /// <summary>
    /// OpenAI APIキーの管理と保存を担当するクラス
    /// </summary>
    public class TokenManager
    {
        private const string CONFIG_FOLDER = "GenPen";
        private const string CONFIG_FILE = "config.dat";
        private static string _apiKey = string.Empty;

        /// <summary>
        /// APIキーを取得または設定します
        /// </summary>
        public static string ApiKey
        {
            get
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    LoadApiKey();
                }
                return _apiKey;
            }
            set
            {
                _apiKey = value;
                SaveApiKey();
            }
        }

        /// <summary>
        /// APIキーが設定されているかどうかを確認します
        /// </summary>
        public static bool IsApiKeySet
        {
            get
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    LoadApiKey();
                }
                return !string.IsNullOrEmpty(_apiKey);
            }
        }

        /// <summary>
        /// 設定ファイルのパスを取得します
        /// </summary>
        private static string ConfigFilePath
        {
            get
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string configFolderPath = Path.Combine(appDataPath, CONFIG_FOLDER);
                
                if (!Directory.Exists(configFolderPath))
                {
                    Directory.CreateDirectory(configFolderPath);
                }
                
                return Path.Combine(configFolderPath, CONFIG_FILE);
            }
        }

        // 暗号化のための固定キーとIV（実際の実装ではより安全な方法を検討）
        private static readonly byte[] EncryptionKey = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 };
        private static readonly byte[] EncryptionIV = new byte[] { 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };

        /// <summary>
        /// APIキーを暗号化して保存します
        /// </summary>
        private static void SaveApiKey()
        {
            if (string.IsNullOrEmpty(_apiKey))
                return;

            try
            {
                // AESを使用して暗号化
                using (Aes aes = Aes.Create())
                {
                    aes.Key = EncryptionKey;
                    aes.IV = EncryptionIV;

                    using (ICryptoTransform encryptor = aes.CreateEncryptor())
                    using (MemoryStream ms = new MemoryStream())
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        byte[] data = Encoding.UTF8.GetBytes(_apiKey);
                        cs.Write(data, 0, data.Length);
                        cs.FlushFinalBlock();
                        File.WriteAllBytes(ConfigFilePath, ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"APIキーの保存中にエラーが発生しました: {ex.Message}",
                    "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 保存されたAPIキーを読み込みます
        /// </summary>
        private static void LoadApiKey()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    byte[] encryptedData = File.ReadAllBytes(ConfigFilePath);
                    
                    // AESを使用して復号化
                    using (Aes aes = Aes.Create())
                    {
                        aes.Key = EncryptionKey;
                        aes.IV = EncryptionIV;

                        using (ICryptoTransform decryptor = aes.CreateDecryptor())
                        using (MemoryStream ms = new MemoryStream(encryptedData))
                        using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        using (StreamReader sr = new StreamReader(cs, Encoding.UTF8))
                        {
                            _apiKey = sr.ReadToEnd();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"APIキーの読み込み中にエラーが発生しました: {ex.Message}",
                    "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _apiKey = string.Empty;
            }
        }

        /// <summary>
        /// 設定ダイアログを表示します
        /// </summary>
        /// <returns>ダイアログの結果</returns>
        public static DialogResult ShowSettingsDialog()
        {
            using (var form = new TokenForm())
            {
                form.ApiKey = ApiKey;
                DialogResult result = form.ShowDialog();
                
                if (result == DialogResult.OK)
                {
                    ApiKey = form.ApiKey;
                }
                
                return result;
            }
        }

        /// <summary>
        /// 毎回起動時に設定ダイアログを表示します
        /// </summary>
        /// <returns>ダイアログの結果</returns>
        public static DialogResult EnsureApiKeyIsSet()
        {
            // 毎回起動時に設定ダイアログを表示
            return ShowSettingsDialog();
        }
    }
}