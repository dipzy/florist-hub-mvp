#!/usr/bin/env python3
"""Gera o ícone da app (PNG) sem dependências. Verde, pétalas e centro."""
import struct
import zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RES = ROOT / "mobile/androidApp/src/main/res"
STORE = ROOT / "mobile/androidApp/fastlane/metadata/android/pt-PT/images"


def write_png(path: Path, size: int) -> None:
    raw = bytearray()
    for y in range(size):
        raw.append(0)
        for x in range(size):
            raw.extend(pixel(x, y, size))
    png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", size, size, 8, 6, 0, 0, 0))
    png += chunk(b"IDAT", zlib.compress(bytes(raw), 9)) + chunk(b"IEND", b"")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(png)


def chunk(tag: bytes, data: bytes) -> bytes:
    return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)


def pixel(x: int, y: int, size: int) -> tuple[int, int, int, int]:
    cx = cy = (size - 1) / 2
    nx = (x - cx) / (size * 0.42)
    ny = (y - cy) / (size * 0.42)
    if nx * nx + ny * ny > 1:
        return (0, 0, 0, 0)
    green = (31, 107, 74, 255)
    petal = (232, 138, 165, 255)
    center = (242, 193, 78, 255)
    for angle in range(5):
        px = 0.46 * __import__("math").cos(angle * 1.256637 + -1.5708)
        py = 0.46 * __import__("math").sin(angle * 1.256637 + -1.5708)
        if (nx - px) ** 2 + (ny - py) ** 2 < 0.16:
            return petal
    if nx * nx + ny * ny < 0.07:
        return center
    return green


def main() -> None:
    densities = {
        "mipmap-mdpi": 48,
        "mipmap-hdpi": 72,
        "mipmap-xhdpi": 96,
        "mipmap-xxhdpi": 144,
        "mipmap-xxxhdpi": 192,
    }
    for folder, size in densities.items():
        write_png(RES / folder / "ic_launcher.png", size)
        write_png(RES / folder / "ic_launcher_round.png", size)
    write_png(STORE / "icon.png", 512)


if __name__ == "__main__":
    main()
