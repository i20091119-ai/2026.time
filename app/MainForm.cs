using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace GnmcTimer
{
    /// <summary>
    /// 테두리 없는 전체화면 창에 WebView2 로 timer.html 을 띄운다.
    /// - 항상 전체화면 (창 크기가 바뀌어도 다시 최대화, 항상 맨 위)
    /// - 페이지의 종료 버튼 → postMessage("exit") → 앱 종료.  Alt+F4 도 됨
    /// - WebView2 런타임이 없으면 Edge 키오스크로 대체 실행
    /// </summary>
    class MainForm : Form
    {
        static readonly Color Bg = Color.FromArgb(14, 22, 38);

        readonly string url;
        readonly Action<string> log;
        readonly WebView2 web;

        public MainForm(string url, Action<string> log)
        {
            this.url = url;
            this.log = log;

            Text = "경남수학문화관 타이머";
            BackColor = Bg;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = Screen.PrimaryScreen.Bounds;
            WindowState = FormWindowState.Maximized;
            TopMost = true;
            ShowInTaskbar = true;

            web = new WebView2();
            web.Dock = DockStyle.Fill;
            web.DefaultBackgroundColor = Bg;
            Controls.Add(web);

            Load += OnLoad;
            Resize += (s, e) => { if (WindowState != FormWindowState.Maximized) WindowState = FormWindowState.Maximized; };
            Deactivate += (s, e) => { TopMost = true; };
        }

        async void OnLoad(object sender, EventArgs e)
        {
            try
            {
                string userData = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "GNMC-Timer", "WebView2");
                // 사용자 조작 없이도 소리가 나도록 자동재생 허용
                var opts = new CoreWebView2EnvironmentOptions("--autoplay-policy=no-user-gesture-required");
                var env = await CoreWebView2Environment.CreateAsync(null, userData, opts);
                await web.EnsureCoreWebView2Async(env);

                var st = web.CoreWebView2.Settings;
                st.AreDefaultContextMenusEnabled = false;
                st.AreDevToolsEnabled = false;
                st.IsZoomControlEnabled = false;
                st.IsPinchZoomEnabled = false;
                st.IsStatusBarEnabled = false;
                st.AreBrowserAcceleratorKeysEnabled = false;
                st.IsSwipeNavigationEnabled = false;

                web.CoreWebView2.NewWindowRequested += (s, a) => { a.Handled = true; };
                web.CoreWebView2.WebMessageReceived += (s, a) =>
                {
                    string msg = null;
                    try { msg = a.TryGetWebMessageAsString(); } catch { }
                    if (msg == "exit")
                    {
                        log("종료 버튼으로 종료");
                        Close();
                    }
                };

                web.CoreWebView2.Navigate(url);
                web.Focus();
            }
            catch (Exception ex)
            {
                log("WebView2 초기화 실패: " + ex.Message);
                try
                {
                    Process.Start("msedge",
                        "--kiosk \"" + url + "\" --edge-kiosk-type=fullscreen --no-first-run --overscroll-history-navigation=0");
                    log("Edge 키오스크로 대체 실행");
                }
                catch (Exception ex2)
                {
                    log("Edge 실행 실패: " + ex2.Message);
                    MessageBox.Show(
                        "화면을 띄울 수 없습니다.\nMicrosoft Edge(WebView2 런타임)가 설치되어 있어야 합니다.\n\n" + ex.Message,
                        "타이머", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                Close();
            }
        }
    }
}
