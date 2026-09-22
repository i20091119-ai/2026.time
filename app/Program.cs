using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace GnmcTimer
{
    /// <summary>
    /// 경남수학문화관 SW체험 타이머 - Windows 실행파일
    ///
    /// 실행 순서
    ///  1) exe 와 같은 폴더에 timer.html 이 없으면 내장된 것을 꺼내 놓는다
    ///  2) GitHub main 의 version.txt 를 확인해 다르면 timer.html 을 새로 받는다 (실패하면 기존 파일 유지)
    ///  3) 전체화면 창(WebView2)으로 timer.html 을 띄운다
    /// </summary>
    static class Program
    {
        [STAThread]
        static void Main()
        {
            bool created;
            using (var mutex = new Mutex(true, "GNMC-Timer-SingleInstance", out created))
            {
                if (!created) return; // 이미 실행 중이면 두 번 띄우지 않음

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                string dir = AppDomain.CurrentDomain.BaseDirectory;
                var updater = new Updater(dir);
                string version = updater.Run();

                string html = Path.Combine(dir, "timer.html");
                string url = new Uri(html).AbsoluteUri
                             + "?v=" + Uri.EscapeDataString(version)
                             + "&app=exe";

                updater.Log("실행: 버전 " + version + " / " + url);
                Application.Run(new MainForm(url, updater.Log));
            }
        }
    }
}
