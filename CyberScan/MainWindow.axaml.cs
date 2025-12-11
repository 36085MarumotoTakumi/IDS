using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CyberScan
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            WriteLog("SYSTEM READY. WAITING FOR TARGET...");
        }

        // 「スキャン開始」ボタンが押されたときの処理
        private async void OnScanClick(object sender, RoutedEventArgs e)
        {
            // XAMLで定義した名前(IpAddressInput)でアクセス
            var ip = IpAddressInput.Text;

            if (string.IsNullOrWhiteSpace(ip))
            {
                WriteLog("[ERROR] ターゲットIPが入力されていません。");
                return;
            }

            // GUIをロックして連打防止
            ScanButton.IsEnabled = false;
            WriteLog("==========================================");
            WriteLog($"[*] TARGET: {ip} に対する偵察を開始します...");
            WriteLog("[*] 攻撃ツール 'Nmap' をロード中...");
            WriteLog("==========================================");

            // Nmapを非同期で実行
            await RunAttackToolAsync("nmap", $"-sV {ip}");

            WriteLog("\n[COMPLETE] 作戦終了。");
            ScanButton.IsEnabled = true;
        }

        // コマンドを実行する共通関数
        private async Task RunAttackToolAsync(string command, string args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = psi };

                // リアルタイム出力のハンドリング
                process.OutputDataReceived += (s, e) => 
                {
                    if (e.Data != null)
                    {
                        // ここで初心者向けに翻訳！
                        var translated = TranslateForBeginner(e.Data);
                        // UIスレッドで描画更新
                        Dispatcher.UIThread.Post(() => WriteLog(translated));
                    }
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data != null) Dispatcher.UIThread.Post(() => WriteLog($"[STDERR] {e.Data}"));
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync();
            }
            catch (Exception ex)
            {
                WriteLog($"[CRITICAL ERROR] 実行に失敗しました: {ex.Message}");
                WriteLog("※ Nmapがインストールされているか確認してください (sudo apt install nmap)");
            }
        }

        // ★重要: 専門用語を分かりやすく翻訳するロジック
        private string TranslateForBeginner(string rawLog)
        {
            // そのまま表示する行と、解説を加える行を分ける
            string output = rawLog;

            if (rawLog.Contains("80/tcp") && rawLog.Contains("open"))
            {
                output += "\n   >>> 【解説】Webサーバー(HTTP)を発見！ Webサイトの改ざんやSQLインジェクションの標的になり得ます。";
            }
            else if (rawLog.Contains("22/tcp") && rawLog.Contains("open"))
            {
                output += "\n   >>> 【解説】SSHポートを発見！ 管理者ログインの入口です。パスワード総当たりの標的です。";
            }
            else if (rawLog.Contains("443/tcp") && rawLog.Contains("open"))
            {
                output += "\n   >>> 【解説】HTTPS(暗号化Web)を発見！ 安全そうに見えますが、サーバー設定ミスがあるかもしれません。";
            }
            else if (rawLog.Contains("3306/tcp") && rawLog.Contains("open"))
            {
                output += "\n   >>> 【解説】MySQLデータベースを発見！ 顧客情報などが漏洩するリスクがあります。";
            }

            return output;
        }

        // ログエリアに追記し、自動スクロールする
        private void WriteLog(string message)
        {
            if (LogOutput == null || LogScrollViewer == null) return;

            LogOutput.Text += $"{message}\n";
            LogScrollViewer.ScrollToEnd();
        }
    }
}
