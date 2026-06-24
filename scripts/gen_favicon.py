"""Generate favicon.png (64x64) matching favicon.svg design."""
from PIL import Image, ImageDraw, ImageFont
import os

size = 64
img = Image.new("RGBA", (size, size), (0x1a, 0x1a, 0x2e, 0xff))
draw = ImageDraw.Draw(img)

# Rounded rectangle background
draw.rounded_rectangle([(0, 0), (size-1, size-1)], radius=12, fill=(0x1a, 0x1a, 0x2e))

# Draw the character 尘
# Try various font paths
font_paths = [
    "C:/Windows/Fonts/simsun.ttc",
    "C:/Windows/Fonts/msyh.ttc",
    "C:/Windows/Fonts/yahei.ttf",
    "C:/Windows/Fonts/msyhbd.ttc",
    "C:/Windows/Fonts/Deng.ttf",
]
font = None
for fp in font_paths:
    if os.path.exists(fp):
        try:
            font = ImageFont.truetype(fp, 38)
            break
        except:
            continue

if font is None:
    font = ImageFont.load_default()

# Gradient: top-left cyan -> bottom-right blue
# We'll just draw the character once and overlay a gradient
# Simpler: gradient background
draw.rounded_rectangle([(0, 0), (size-1, size-1)], radius=12, fill=(0x1a, 0x1a, 0x2e))

# Draw 尘 character in light color (will handle via mask)
draw.text((32, 44), "尘", fill=(0x00, 0xd4, 0xaa), font=font, anchor="mm")

# Add a slight gradient effect - overlay a thin gradient rect from bottom
for i in range(8):
    alpha = int(30 * (1 - i / 8))
    draw.rectangle([(0, size-8+i), (size-1, size-8+i+1)], fill=(0x00, 0xa3, 0xff, alpha))

out_path = r"E:\projects\rechenz.github.io\static\favicon.png"
img.save(out_path, "PNG")
print(f"Saved {out_path} ({os.path.getsize(out_path)} bytes)")

# Also generate favicon.ico (multi-size)
ico_path = r"E:\projects\rechenz.github.io\static\favicon.ico"
img.save(ico_path, "ICO", sizes=[(16, 16), (32, 32), (48, 48)])
print(f"Saved {ico_path}")
