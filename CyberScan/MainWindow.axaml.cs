using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace CyberScan
{
    public partial class MainWindow : Window
    {
        // デフォルトのターゲットIP（ファイルがない場合用）
        private string _targetIp = "127.0.0.1";
        // IPアドレスを記述しておく設定ファイル名
        private const string ConfigFileName = "target.txt";

        public MainWindow()
        {
            InitializeComponent();
            
            // キーボード操作のイベントハンドラを追加
            this.KeyDown += OnKeyDown;
            // ウィンドウが開いたときのイベント
            this.Opened += OnWindowOpened;
        }

        // ウィンドウが表示された直後に実行される初期化処理
        private void OnWindowOpened(object? sender, EventArgs e)
        {
            LoadTargetIp();
            WriteLog("SYSTEM INITIALIZED.");
            WriteLog($"TARGET LOCKED: {_targetIp}");
            WriteLog("WAITING FOR USER AUTHORIZATION...");
        }

        // 設定ファイル(target.txt)からIPアドレスを読み込む
        private void LoadTargetIp()
        {
            try
            {
                // 実行ファイルと同じ場所に target.txt があるか確認
                if (File.Exists(ConfigFileName))
                {
                    string content = File.ReadAllText(ConfigFileName).Trim();
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        _targetIp = content;
                    }
                }
                else
                {
                    // ファイルがない場合はデフォルト値で作成しておく（親切設計）
                    File.WriteAllText(ConfigFileName, "127.0.0.1");
                    WriteLog($"[CONFIG] {ConfigFileName} not found. Created default.");
                }
            }
            catch (Exception ex)
            {
                WriteLog($"[ERROR] Config load failed: {ex.Message}");
            }
            
            // 画面のIP表示を更新
            TargetIpDisplay.Text = _targetIp;
        }

        // キーボードショートカット処理 (管理者用機能)
        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            // Ctrl + Q でアプリを強制終了
            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.Q)
            {
                Close();
            }
            
            // F11 でフルスクリーンとウィンドウモードの切り替え
            if (e.Key == Key.F11)
            {
                if (WindowState == WindowState.FullScreen)
                {
                    // ウィンドウモードに戻す
                    WindowState = WindowState.Normal;
                    SystemDecorations = SystemDecorations.Full;
                    Topmost = false;
                }
                else
                {
                    // フルスクリーン（キオスクモード）にする
                    WindowState = WindowState.FullScreen;
                    SystemDecorations = SystemDecorations.None;
                    Topmost = true;
                }
            }
        }

        // --- フェーズ1: ポートスキャン (偵察) ---
        private async void OnScanClick(object sender, RoutedEventArgs e)
        {
            ScanButton.IsEnabled = false;
            StatusText.Text = "STATUS: SCANNING NETWORK...";
            StatusText.Foreground = Avalonia.Media.Brushes.Yellow;

            WriteLog("\n==========================================");
            WriteLog($"[*] INITIATING PORT SCAN ON {_targetIp}...");
            WriteLog("==========================================");

            // Nmap実行
            // -F: Fast Mode (主要100ポートのみスキャンして時間を短縮)
            // -sV: Version Scan (動いているサービス名を取得)
            await RunAttackToolAsync("nmap", $"-F -sV {_targetIp}");

            WriteLog("\n[SCAN COMPLETE] ANALYZING VULNERABILITIES...");
            
            // フェーズ切り替え: スキャンボタンを隠し、攻撃メニューを表示
            Phase1Panel.IsVisible = false;
            Phase2Panel.IsVisible = true;
            
            // ボタンを有効化（本来は開いているポートに応じて分岐するが、体験用なのですべて有効化）
            WebAttackButton.IsEnabled = true; 
            BruteForceButton.IsEnabled = true;

            StatusText.Text = "STATUS: VULNERABILITY DETECTED. SELECT ACTION.";
            StatusText.Foreground = Avalonia.Media.Brushes.Red;
        }

        // --- フェーズ2A: Web脆弱性スキャン ---
        private async void OnWebAttackClick(object sender, RoutedEventArgs e)
        {
            DisableAttackButtons();
            StatusText.Text = "STATUS: EXECUTING WEB EXPLOIT...";

            WriteLog("\n==========================================");
            WriteLog("[*] STARTING WEB VULNERABILITY SCAN...");
            WriteLog("==========================================");

            // 体験用にNmapのスクリプト機能を使ってWeb情報を取得
            // (実際のNiktoなどは時間がかかるため、http-titleなどで代用)
            await RunAttackToolAsync("nmap", $"-p 80,443 --script http-title,http-headers,http-methods {_targetIp}");

            WriteLog("\n[ATTACK FINISHED] REPORT GENERATED.");
            EnableAttackButtons();
            StatusText.Text = "STATUS: READY FOR NEXT COMMAND.";
        }

        // --- フェーズ2B: パスワードクラック (SSH/FTP) ---
        private async void OnBruteForceClick(object sender, RoutedEventArgs e)
        {
            DisableAttackButtons();
            StatusText.Text = "STATUS: CRACKING PASSWORDS...";

            WriteLog("\n==========================================");
            WriteLog("[*] INITIATING BRUTE FORCE ATTACK (SSH)...");
            WriteLog("==========================================");

            // 体験用にNmapのSSH認証方式確認スクリプトを実行
            // (実際のHydraは辞書ファイルが必要で複雑なため、雰囲気重視)
            await RunAttackToolAsync("nmap", $"-p 22 --script ssh-auth-methods {_targetIp}");

            WriteLog("\n[ATTACK FINISHED] ACCESS ATTEMPTS LOGGED.");
            EnableAttackButtons();
            StatusText.Text = "STATUS: READY FOR NEXT COMMAND.";
        }

        // --- システムリセット（最初に戻る） ---
        private void OnResetClick(object sender, RoutedEventArgs e)
        {
            // 画面と状態を初期化
            LogOutput.Text = "";
            Phase2Panel.IsVisible = false;
            Phase1Panel.IsVisible = true;
            ScanButton.IsEnabled = true;
            
            StatusText.Text = "STATUS: WAITING FOR COMMAND";
            StatusText.Foreground = Avalonia.Media.Brushes.Yellow;
            
            // IPを再読み込み（運用中にファイルを書き換えた場合に対応）
            LoadTargetIp(); 
            WriteLog("SYSTEM RESET. READY.");
        }

        private void DisableAttackButtons()
        {
            WebAttackButton.IsEnabled = false;
            BruteForceButton.IsEnabled = false;
        }

        private void EnableAttackButtons()
        {
            WebAttackButton.IsEnabled = true;
            BruteForceButton.IsEnabled = true;
        }

        // --- コマンド実行エンジン (Nmapなどを裏で動かす) ---
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

                // 標準出力をリアルタイムで取得
                process.OutputDataReceived += (s, e) => 
                {
                    if (e.Data != null)
                    {
                        // 初心者向けに翻訳して表示
                        var translated = TranslateForBeginner(e.Data);
                        // UIスレッドで描画
                        Dispatcher.UIThread.Post(() => WriteLog(translated));
                    }
                };
                
                // エラー出力も取得
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
                WriteLog($"[ERROR] Command Execution Failed: {ex.Message}");
                // ツールが入っていない場合のデモ用フェイルセーフ
                await Task.Delay(1000);
                WriteLog($"Simulating result for {command}...");
                WriteLog("Target appears to be secure or tool not installed.");
            }
        }

        // --- ログ翻訳ロジック ---
        private string TranslateForBeginner(string rawLog)
        {
            string output = rawLog;
            
            // 特定のキーワードが見つかったら、解説文を追記する
            if (rawLog.Contains("80/tcp") && rawLog.Contains("open")) 
                output += "   <-- [発見] Webサーバー(HTTP)が動いています。Webサイトへの攻撃が可能です。";
            
            if (rawLog.Contains("443/tcp") && rawLog.Contains("open")) 
                output += "   <-- [発見] 暗号化Webサーバー(HTTPS)です。設定ミスがあれば侵入できます。";

            if (rawLog.Contains("22/tcp") && rawLog.Contains("open")) 
                output += "   <-- [発見] SSH(遠隔操作)ポートです。パスワード総当たり攻撃の標的になります。";

            if (rawLog.Contains("http-title"))
                output = $"[情報収集] Webサイトのタイトルを取得: {rawLog.Trim()}";

            if (rawLog.Contains("script execution failed"))
                output = "[INFO] スクリプトの実行はスキップされました。";
                
            return output;
        }

        // --- ログ出力ヘルパー ---
        private void WriteLog(string message)
        {
            if (LogOutput == null || LogScrollViewer == null) return;
            LogOutput.Text += $"{message}\n";
            // 常に最新の行が見えるようにスクロール
            LogScrollViewer.ScrollToEnd();
        }
    }
}
