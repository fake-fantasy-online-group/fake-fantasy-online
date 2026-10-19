# Registration presentation contracts

Run after activating the managed toolchain and compiling current core dependencies:

```sh
source Documentation/tools/activate-dotnet-test-toolchain.sh
Tests/RegistrationPresentation/run-registration-presentation-contract.sh
```

The harness compiles the actual `RegistrationPixelArt.cs` and
`TitleFlow.RegistrationPresentation.cs` with deterministic Unity substitutes.
It checks the four field rows, masking and existing 24/40-character limits,
30-pixel mobile hit targets with a 31-pixel pitch, original image-button states,
submit/return routing, balanced 12-pixel action spacing, package-arrival retry,
repaint caching, safe missing/incompatible-art fallbacks, and ownership-safe cleanup.
It tests runtime native mask/color sampling and candidate-subset parsing without
embedding any original game pixels in the tests.

This does not run Unity, validate a device keyboard, contact a server, or prove
successful account creation. Existing registration validation and the packet's
invitation string are unchanged. The approved UI no longer offers that input;
`ClearRegisterFields` still clears it and the existing packet still sends it.
The current server's `validateRegistration` accepts an empty invitation and its
`decRegister` still consumes the fourth string before device ID and VM flag.

## Optional external-art comparison

Pass `input-directory output-directory` to the test script to capture the actual
production C# generated textures and draw coordinates. External input names are:
`loginbk-0.raw`, `topbar-0.raw`, `middlebar-0.raw`, `bottombar-0.raw`,
`editbk-0.raw`, `completebtn-{0,1,2}.raw`, and `returnbtn-{0,1,2}.raw`.
Each file is little-endian int32 width/height followed by top-down RGBA32 pixels.
Decode these from legally available original game resources outside the repository.
Use 05diff's 317x131 loginbk and the approved q21 generic window/input/button frames.
No original resource or capture should be added to Git.

```sh
Tests/RegistrationPresentation/run-registration-presentation-contract.sh "$INPUT" "$CAPTURE"
python3 Tests/RegistrationPresentation/compare-captured-artwork.py \
  "$CAPTURE" "$OLD_BACKGROUND" "$APPROVED_V5" "$RESULT"
```

On the inspected resources, the C# panel, five labels, four input frames and two
buttons matched the accepted v5-wqy full image with **zero differing pixels** when
composited sequentially onto its original background. The new bundled background
is intentionally separate. Raster captures are compositing evidence, not Unity
screenshots, and empty inputs do not validate caret/IME rendering.

## Candidate-font provenance

Seven editable glyphs are separately licensed font data in
`Resources/FakeFantasyOnline/Registration/WqyRegistrationSubset.txt`, accompanied
by the full GPL v2, upstream document-embedding exception, copyright and provenance.
The exception is not assumed to cover a game application; the independent font
resource's full preferred editable source is shipped under GPL terms. The original
four glyphs 帐号密码 are sampled at runtime, never checked into this repository.
`Documentation/tools/generate-registration-font-subset.py` reproduces the candidate
subset from the specifically reviewed WenQuanYi 12px TTF, checking its SHA-256.
This candidate is user-approved, not claimed to be the original game's font family.
