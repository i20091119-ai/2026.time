# ============================================================
#  경남수학문화관 SW체험 타이머 - 실행 및 자동 업데이트 스크립트
#  (타이머시작.bat 이 이 파일을 호출합니다. 직접 수정할 일은 거의 없음)
#
#  동작 순서
#   1) GitHub 에 있는 version.txt 를 읽어 로컬 version.txt 와 비교
#   2) 다르면 최신 timer.html 등을 내려받아 교체 (실패 시 기존 파일 유지)
#   3) 같거나 인터넷이 안 되면 그대로 진행
#   4) Microsoft Edge 키오스크(전체화면)로 timer.html 실행
# ============================================================

$ErrorActionPreference = 'Stop'

# ----- 설정 -----
$RepoRaw  = 'https://raw.githubusercontent.com/i20091119-ai/2026.time/main'
$Files    = @('timer.html', 'launcher.ps1', '설치안내.txt')   # 업데이트로 교체되는 파일
$TimeoutSec = 8                                               # 업데이트 확인 대기 시간(초)
$KillExistingEdge = $false                                    # $true 면 기존 Edge 창을 모두 닫고 시작

# ----- 경로 -----
$Dir         = Split-Path -Parent $MyInvocation.MyCommand.Path
$VersionFile = Join-Path $Dir 'version.txt'
$LogFile     = Join-Path $Dir 'update.log'
$TimerHtml   = Join-Path $Dir 'timer.html'

function Log([string]$msg) {
    $line = '[{0}] {1}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $msg
    try { Add-Content -Path $LogFile -Value $line -Encoding UTF8 } catch {}
    Write-Host $line
}

function Get-NoCacheUrl([string]$name) {
    $stamp = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
    return '{0}/{1}?t={2}' -f $RepoRaw, [Uri]::EscapeDataString($name), $stamp
}

try { [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 } catch {}

# 로그가 너무 커지면 비움 (200KB 초과 시)
try { if ((Test-Path $LogFile) -and (Get-Item $LogFile).Length -gt 200KB) { Remove-Item $LogFile -Force } } catch {}

# ----- 1) 버전 확인 -----
$localVer = '0'
if (Test-Path $VersionFile) {
    try { $localVer = (Get-Content $VersionFile -Raw).Trim() } catch {}
}

$remoteVer = $null
try {
    $resp = Invoke-WebRequest -Uri (Get-NoCacheUrl 'version.txt') -UseBasicParsing -TimeoutSec $TimeoutSec
    $remoteVer = ([string]$resp.Content).Trim()
    if ($remoteVer -eq '') { $remoteVer = $null }
} catch {
    Log ('업데이트 확인 실패 (인터넷 없음 또는 서버 오류) - 기존 버전 ' + $localVer + ' 으로 실행: ' + $_.Exception.Message)
}

# ----- 2) 업데이트 -----
if ($remoteVer -and ($remoteVer -ne $localVer)) {
    Log ('업데이트 발견: ' + $localVer + ' -> ' + $remoteVer)
    $tmp = Join-Path $Dir '_update_tmp'
    $ok  = $true
    try {
        New-Item -ItemType Directory -Force -Path $tmp | Out-Null
        foreach ($f in $Files) {
            $dst = Join-Path $tmp $f
            Invoke-WebRequest -Uri (Get-NoCacheUrl $f) -OutFile $dst -UseBasicParsing -TimeoutSec 60
            if (-not (Test-Path $dst) -or (Get-Item $dst).Length -lt 10) { throw ('빈 파일: ' + $f) }
        }
        # timer.html 이 온전한지 간단 검사
        $html = Get-Content (Join-Path $tmp 'timer.html') -Raw
        if ($html -notmatch '</html>') { throw 'timer.html 내용이 손상되어 있음' }
    } catch {
        $ok = $false
        Log ('다운로드 실패 - 기존 파일 유지: ' + $_.Exception.Message)
    }

    if ($ok) {
        try {
            foreach ($f in $Files) {
                Copy-Item (Join-Path $tmp $f) (Join-Path $Dir $f) -Force
                try { Unblock-File (Join-Path $Dir $f) } catch {}
            }
            Set-Content -Path $VersionFile -Value $remoteVer -Encoding ASCII -NoNewline
            $localVer = $remoteVer
            Log ('업데이트 완료: ' + $remoteVer)
        } catch {
            Log ('파일 교체 실패: ' + $_.Exception.Message)
        }
    }
    Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
} elseif ($remoteVer) {
    Log ('최신 버전 (' + $localVer + ') - 업데이트 없음')
}

# ----- 3) 실행 -----
if (-not (Test-Path $TimerHtml)) {
    Log ('[오류] timer.html 을 찾을 수 없습니다: ' + $TimerHtml)
    [void][System.Reflection.Assembly]::LoadWithPartialName('System.Windows.Forms')
    [System.Windows.Forms.MessageBox]::Show('timer.html 파일을 찾을 수 없습니다.' + "`n" + $TimerHtml, '타이머') | Out-Null
    exit 1
}

if ($KillExistingEdge) {
    Get-Process msedge -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 500
}

$url = 'file:///' + ($TimerHtml -replace '\\', '/') + '?v=' + [Uri]::EscapeDataString($localVer)
$edgeArgs = @(
    '--kiosk', $url,
    '--edge-kiosk-type=fullscreen',
    '--no-first-run',
    '--disable-features=msEdgeReadAloud',
    '--overscroll-history-navigation=0'
)

$edge = 'msedge'
foreach ($base in @(${env:ProgramFiles(x86)}, $env:ProgramFiles)) {
    if (-not $base) { continue }
    $c = Join-Path $base 'Microsoft\Edge\Application\msedge.exe'
    if (Test-Path $c) { $edge = $c; break }
}

Log ('실행: 버전 ' + $localVer + ' / ' + $url)
Start-Process -FilePath $edge -ArgumentList $edgeArgs
