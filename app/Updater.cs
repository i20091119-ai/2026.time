using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GnmcTimer
{
    /// <summary>
    /// 켤 때 한 번 GitHub 의 version.txt 를 확인하고, 다르면 timer.html 을 교체한다.
    /// 어떤 경우에도 예외를 밖으로 던지지 않는다 (타이머는 항상 떠야 함).
    /// </summary>
    class Updater
    {
        const string RepoRaw = "https://raw.githubusercontent.com/i20091119-ai/2026.time/main";
        const int CheckTimeoutSec = 8;
        const int DownloadTimeoutSec = 60;

        readonly string dir;
        readonly string htmlFile;
        readonly string versionFile;
        readonly string logFile;

        public Updater(string dir)
        {
            this.dir = dir;
            htmlFile = Path.Combine(dir, "timer.html");
            versionFile = Path.Combine(dir, "version.txt");
            logFile = Path.Combine(dir, "update.log");
        }

        public void Log(string msg)
        {
            string line = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + msg;
            try { File.AppendAllText(logFile, line + Environment.NewLine, Encoding.UTF8); } catch { }
        }

        /// <summary>업데이트를 시도하고, 실행할 버전 문자열을 돌려준다.</summary>
        public string Run()
        {
            try { ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12; } catch { }
            try
            {
                if (File.Exists(logFile) && new FileInfo(logFile).Length > 200 * 1024) File.Delete(logFile);
            }
            catch { }

            string local = ReadLocalVersion();

            // 0) 첫 실행: timer.html 이 없으면 내장본을 꺼내 놓는다
            if (!File.Exists(htmlFile))
            {
                try
                {
                    File.WriteAllBytes(htmlFile, ReadEmbedded("timer.html"));
                    local = Encoding.UTF8.GetString(ReadEmbedded("version.txt")).Trim();
                    File.WriteAllText(versionFile, local, Encoding.ASCII);
                    Log("내장 timer.html 설치 (버전 " + local + ")");
                }
                catch (Exception ex)
                {
                    Log("내장 파일 설치 실패: " + Root(ex).Message);
                }
            }

            // 1) 원격 버전 확인
            string remote = null;
            try
            {
                remote = Encoding.UTF8.GetString(Fetch("version.txt", CheckTimeoutSec)).Trim();
                if (remote.Length == 0) remote = null;
            }
            catch (Exception ex)
            {
                Log("업데이트 확인 실패 (인터넷 없음 또는 서버 오류) - 기존 버전 " + local + " 으로 실행: " + Root(ex).Message);
            }

            // 2) 다르면 교체
            if (remote != null && remote != local)
            {
                Log("업데이트 발견: " + local + " -> " + remote);
                try
                {
                    byte[] html = Fetch("timer.html", DownloadTimeoutSec);
                    string text = Encoding.UTF8.GetString(html);
                    if (html.Length < 10 || text.IndexOf("</html>", StringComparison.OrdinalIgnoreCase) < 0)
                        throw new Exception("timer.html 내용이 손상되어 있음");

                    string tmp = htmlFile + ".new";
                    File.WriteAllBytes(tmp, html);
                    File.Copy(tmp, htmlFile, true);
                    File.Delete(tmp);
                    File.WriteAllText(versionFile, remote, Encoding.ASCII);
                    local = remote;
                    Log("업데이트 완료: " + remote);
                }
                catch (Exception ex)
                {
                    Log("다운로드 실패 - 기존 파일 유지: " + Root(ex).Message);
                }
            }
            else if (remote != null)
            {
                Log("최신 버전 (" + local + ") - 업데이트 없음");
            }

            return local;
        }

        string ReadLocalVersion()
        {
            try
            {
                if (File.Exists(versionFile)) return File.ReadAllText(versionFile).Trim();
            }
            catch { }
            return "0";
        }

        static byte[] Fetch(string name, int timeoutSec)
        {
            string url = RepoRaw + "/" + Uri.EscapeDataString(name)
                         + "?t=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            // Application.Run 전이라 동기화 컨텍스트가 없어 .Result 로 기다려도 교착이 없다.
            return Task.Run(async () =>
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(timeoutSec);
                    client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };
                    using (var resp = await client.GetAsync(url).ConfigureAwait(false))
                    {
                        resp.EnsureSuccessStatusCode();
                        return await resp.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                    }
                }
            }).GetAwaiter().GetResult();
        }

        static byte[] ReadEmbedded(string name)
        {
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                if (s == null) throw new FileNotFoundException("내장 리소스 없음: " + name);
                using (var ms = new MemoryStream())
                {
                    s.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }

        static Exception Root(Exception ex)
        {
            while (ex.InnerException != null) ex = ex.InnerException;
            return ex;
        }
    }
}
