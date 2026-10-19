#!/usr/bin/env python3
"""Generate the seven approved candidate glyphs from an explicitly supplied WQY TTF.
Requires Pillow. Does not download a font or read any original game artwork.
The generated font data is GPL-2.0 with the upstream font-embedding exception.
"""
import argparse
import hashlib
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

SOURCE_SHA256 = "ce94c20796987a169ea08d24207c7a00ff457fe3b5dae5db21c7b51eab60eb3c"
CHARACTERS = "确认安全注册新"

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("font", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    digest = hashlib.sha256(args.font.read_bytes()).hexdigest()
    if digest != SOURCE_SHA256:
        parser.error("Input is not the reviewed WQY 12px font (SHA-256 mismatch)")
    font = ImageFont.truetype(str(args.font), 15)
    lines = [
        "# WQY registration candidate: seven editable 12x12 bitmap glyphs.",
        "# GPL-2.0 with upstream font embedding exception; see WqyRegistrationLicense.txt.",
        "# Copyright 2004-2010 The WenQuanYi Project Board of Trustees and Qianqian Fang.",
        "# Based on Firefly; TTF conversion by AmusementClub. See WqyRegistrationNotice.txt.",
        "# Modified 2026-10-19: seven-glyph subset; native bitmap shifted +1,+1 in a 12px cell.",
        "# This is a candidate font, not an identification of the original game typeface.",
        "# Preferred editable source: codepoint:12 comma-separated hexadecimal bitmap rows.",
        "# Each row is 12 bits, most significant bit on the left. No antialiasing/resampling.",
    ]
    for char in CHARACTERS:
        raw = Image.new("L", (12, 12))
        ImageDraw.Draw(raw).text((0, -2), char, font=font, fill=255)
        glyph = Image.new("L", (12, 12))
        glyph.paste(raw.crop((0, 0, 11, 11)), (1, 1))
        rows = [sum((1 << (11-x)) for x in range(12) if glyph.getpixel((x,y)) > 127) for y in range(12)]
        lines.append(f"{ord(char):04X}:" + ",".join(f"{row:03X}" for row in rows))
    args.output.write_text("\n".join(lines) + "\n", encoding="utf-8")

if __name__ == "__main__":
    main()
