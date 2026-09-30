"""Builds the application icon (bertahan.png/.ico/.icns) from the Blender UI renders.

    python packaging/icons/make_icons.py

The icon is the zombie warga's head on a sun disc, on the night-purple tile the menus use.
"""
import os

from PIL import Image, ImageChops, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
UI = os.path.join(HERE, "..", "..", "src", "Bertahan", "Assets", "UI")
SIZE = 1024

NIGHT = (0x1B, 0x15, 0x30, 255)
SUN = (0xFF, 0xD2, 0x3F, 255)
INK = (0x3A, 0x24, 0x12, 255)


def tile():
    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    pad = SIZE // 16
    d.rounded_rectangle((pad, pad, SIZE - pad, SIZE - pad), radius=SIZE // 5, fill=NIGHT, outline=INK, width=SIZE // 48)
    r = int(SIZE * 0.34)
    c = SIZE // 2
    d.ellipse((c - r, c - r - SIZE // 20, c + r, c + r - SIZE // 20), fill=SUN, outline=INK, width=SIZE // 64)
    return img


def head():
    z = Image.open(os.path.join(UI, "zombie_warga.png")).convert("RGBA")
    x0, y0, x1, y1 = z.getbbox()
    # the top third of the render is the head and hair
    h = int((y1 - y0) * 0.36)
    w = x1 - x0
    face = z.crop((x0, y0, x1, y0 + h))
    s = SIZE * 0.72 / max(w, h)
    return face.resize((int(face.width * s), int(face.height * s)), Image.LANCZOS)


def main():
    img = tile()
    face = head()
    layer = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    layer.alpha_composite(face, ((SIZE - face.width) // 2, int(SIZE * 0.2)))
    # keep the head inside the sun disc
    mask = Image.new("L", (SIZE, SIZE), 0)
    r = int(SIZE * 0.34) - SIZE // 64
    c = SIZE // 2
    ImageDraw.Draw(mask).ellipse((c - r, c - r - SIZE // 20, c + r, c + r - SIZE // 20), fill=255)
    layer.putalpha(ImageChops.darker(layer.getchannel("A"), mask))
    img.alpha_composite(layer)
    img.save(os.path.join(HERE, "bertahan.png"))
    img.save(os.path.join(HERE, "bertahan.ico"), sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
    img.save(os.path.join(HERE, "bertahan.icns"))
    img.resize((256, 256), Image.LANCZOS).save(os.path.join(HERE, "bertahan-256.png"))
    print("ok")


if __name__ == "__main__":
    main()
