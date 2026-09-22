# app.ico 생성기 (외부 라이브러리 없이 PNG + ICO 를 직접 씀)
# 실행: python3 app/tools/make_icon.py  →  app/app.ico
import math, struct, zlib, os

SIZE = 256
BG   = (14, 22, 38)      # --bg
RING = (255, 204, 51)    # --accent
HAND = (244, 247, 255)   # --ink

def smooth(d, w=1.2):
    """거리 d 가 0 이하이면 1, w 이상이면 0 (안티앨리어싱)"""
    if d <= 0: return 1.0
    if d >= w: return 0.0
    return 1.0 - d / w

def pixel(x, y):
    cx = cy = SIZE / 2
    px, py = x + 0.5, y + 0.5
    r = math.hypot(px - cx, py - cy)
    a_bg = smooth(r - 122)                       # 배경 원
    ring_r, ring_w = 92, 13
    a_ring = smooth(abs(r - ring_r) - ring_w)    # 노란 링
    # 흰색 바늘: 중심에서 12시 방향, 두께 12, 길이 70
    hx, hy = px - cx, py - cy
    a_hand = 0.0
    if -70 <= hy <= 6:
        a_hand = smooth(abs(hx) - 7)
    a_dot = smooth(r - 13)                       # 중심점
    # 합성 (위에서부터)
    col = (0, 0, 0); alpha = 0.0
    for c, a in ((BG, a_bg), (RING, a_ring), (HAND, max(a_hand, a_dot))):
        if a <= 0: continue
        alpha_n = a + alpha * (1 - a)
        col = tuple((c[i] * a + col[i] * alpha * (1 - a)) / alpha_n for i in range(3))
        alpha = alpha_n
    return (int(round(col[0])), int(round(col[1])), int(round(col[2])), int(round(alpha * 255)))

def png_bytes():
    raw = bytearray()
    for y in range(SIZE):
        raw.append(0)
        for x in range(SIZE):
            raw.extend(pixel(x, y))
    def chunk(tag, data):
        c = struct.pack('>I', len(data)) + tag + data
        return c + struct.pack('>I', zlib.crc32(tag + data) & 0xffffffff)
    ihdr = struct.pack('>IIBBBBB', SIZE, SIZE, 8, 6, 0, 0, 0)
    return (b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', ihdr)
            + chunk(b'IDAT', zlib.compress(bytes(raw), 9)) + chunk(b'IEND', b''))

png = png_bytes()
# ICO: 헤더(6) + 항목(16) + PNG 데이터 (256px 는 0 으로 표기)
ico = struct.pack('<HHH', 0, 1, 1)
ico += struct.pack('<BBBBHHII', 0, 0, 0, 0, 1, 32, len(png), 6 + 16)
ico += png
out = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'app.ico')
open(out, 'wb').write(ico)
print('wrote', os.path.normpath(out), len(ico), 'bytes')
