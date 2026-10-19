#!/usr/bin/env python3
"""Compare actual C# draw captures against an approved external PNG.
No original artwork is bundled. Requires Pillow, optional external artwork run of
run-registration-presentation-contract.sh, and the matching external background.
Sequential alpha compositing is important; flattening a transparent overlay first
introduces rounding differences at translucent input-frame/button edges.
"""
import argparse
from pathlib import Path
import struct
from PIL import Image

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("capture", type=Path, help="C# output directory containing draws.csv and draw-N.raw")
parser.add_argument("background", type=Path, help="Already fitted 800x600 background PNG")
parser.add_argument("approved", type=Path, help="Approved full-page 800x600 PNG")
parser.add_argument("output", type=Path)
args = parser.parse_args()
image = Image.open(args.background).convert("RGBA")
assert image.size == (800, 600)
for line in (args.capture / "draws.csv").read_text().splitlines():
    filename, x, y, width, height = line.split(",")
    data = (args.capture / filename).read_bytes()
    w, h = struct.unpack_from("<ii", data)
    assert (w, h) == (int(width), int(height))
    texture = Image.frombytes("RGBA", (w, h), data[8:])
    image.alpha_composite(texture, (int(x), int(y)))
image.save(args.output)
expected = Image.open(args.approved).convert("RGBA")
assert expected.size == image.size
actual_bytes, expected_bytes = image.tobytes(), expected.tobytes()
count = sum(actual_bytes[i:i+4] != expected_bytes[i:i+4] for i in range(0, len(actual_bytes), 4))
print(f"Differing pixels: {count}; production C# artwork/coordinates, external decoded fixtures, no Unity execution")
raise SystemExit(0 if count == 0 else 1)
