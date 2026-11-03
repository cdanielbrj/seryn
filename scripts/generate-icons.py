"""Generate PNG app icons from the same native wave mark; stdlib only."""
import math
import struct
import zlib
from pathlib import Path

target = Path(__file__).resolve().parents[1] / 'src/Seryn.Web/public/icons'


def icon(size, name):
    scale = 3
    width = size * scale
    pixels = bytearray([17, 126, 120] * width * width)
    # Smooth three-wave mark, fully inside the maskable safe circle.
    radius = .024 * width
    for row in (-.145, 0, .145):
        for step in range(width * 2):
            x = width * (.245 + .51 * step / (width * 2 - 1))
            y = width * (.5 + row - .045 * math.sin(step / (width * 2 - 1) * 2 * math.pi))
            for py in range(max(0, int(y-radius)), min(width, int(y+radius)+1)):
                for px in range(max(0, int(x-radius)), min(width, int(x+radius)+1)):
                    if (px-x)**2 + (py-y)**2 <= radius**2:
                        offset = (py * width + px) * 3
                        pixels[offset:offset+3] = b'\xff\xff\xff'
    raw = bytearray()
    for y in range(size):
        raw.append(0)
        for x in range(size):
            for c in range(3):
                raw.append(sum(pixels[((y*scale+dy)*width+x*scale+dx)*3+c]
                               for dy in range(scale) for dx in range(scale)) // (scale*scale))

    def chunk(kind, data):
        return struct.pack('!I', len(data)) + kind + data + struct.pack('!I', zlib.crc32(kind+data))
    png = b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('!2I5B', size, size, 8, 2, 0, 0, 0))
    (target / name).write_bytes(png + chunk(b'IDAT', zlib.compress(raw)) + chunk(b'IEND', b''))


for dimension, filename in [(180, 'apple-touch-icon.png'), (192, 'icon-192.png'),
                            (512, 'icon-512.png'), (512, 'icon-maskable.png')]:
    icon(dimension, filename)
