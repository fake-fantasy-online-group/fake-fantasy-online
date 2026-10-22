using System;
using System.Reflection;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    internal static class NameTagContract
    {
        private static int assertions, comparisons;
        private const BindingFlags Fields = BindingFlags.Static | BindingFlags.NonPublic;
        private static void Check(bool value, string label)
        { assertions++; if (!value) throw new Exception(label); }
        private static object Read(Type font, string field) => font.GetField(field, Fields).GetValue(null);
        private static void Set(Type font, string field, object value) => font.GetField(field, Fields).SetValue(null, value);
        private static void ResetFont(Type font)
        {
            Set(font, "atlas", null); Set(font, "idx", null); Set(font, "wid", null); Set(font, "tried", false);
        }
        private static ResLoader Bind(int pattern = 0)
        {
            var loader = new ResLoader(pattern); ResLoader.I = loader;
            ResetFont(typeof(BitmapFont)); ResetFont(typeof(LegacyBitmapFont));
            BitmapFont.EnsureForIndependentRuntime(); LegacyBitmapFont.EnsureForIndependentRuntime();
            return loader;
        }
        private static bool Same(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;
        private static void PixelsEqual(Texture2D actual, Texture2D expected, string label)
        {
            comparisons++;
            Check(actual.width == expected.width && actual.height == expected.height, label + " dimensions");
            for (int i = 0; i < actual.Pixels.Length; i++)
                Check(Same(actual.Pixels[i], expected.Pixels[i]), label + " pixel " + i);
        }
        private static void ShaderEqual(SpriteRenderer renderer, Texture2D expected, string label)
        {
            // ReferenceNameTag.shader multiplies sampled RGBA by vertex color.
            var texture = renderer.sprite.texture;
            Check(texture.width == expected.width && texture.height == expected.height, label + " dimensions");
            var color = renderer.color;
            for (int i = 0; i < texture.Pixels.Length; i++)
            {
                var pixel = texture.Pixels[i]; var target = expected.Pixels[i];
                Check(Math.Abs(pixel.r / 255f * color.r - target.r / 255f) < .000001f &&
                    Math.Abs(pixel.g / 255f * color.g - target.g / 255f) < .000001f &&
                    Math.Abs(pixel.b / 255f * color.b - target.b / 255f) < .000001f &&
                    Math.Abs(pixel.a / 255f * color.a - target.a / 255f) < .000001f,
                    label + " shader pixel " + i);
            }
        }
        private static Texture2D Oracle(string name, Color color) =>
            LegacyBitmapFont.BakeNameTag(name, "", color, color, 1);
        private static void NoTextureWork(string label)
        {
            Check(Texture2D.AllReads == 0 && Texture2D.Created == 0 && Texture2D.Applies == 0 &&
                Texture2D.Sets == 0 && Sprite.Creates == 0, label);
        }
        private static void TestPixelEquivalence()
        {
            Bind();
            var icon = new Texture2D(3, 17, TextureFormat.RGBA32, false);
            for (int i = 0; i < icon.Pixels.Length; i++)
                icon.Pixels[i] = new Color32((byte)(i * 3), 181, 46, new byte[] { 7, 8, 255 }[i % 3]);
            string[] names = { "名字怪物", "A 名字 B", "  A  ", "A🙂缺B", "名字｜A", "｜", "AB" };
            Color[] colors = { new Color(.23f, .51f, .87f, 1), new Color(.91f, .11f, .37f, .43f),
                new Color(.11f, .81f, .27f, 7f / 255), new Color(.31f, .41f, .17f, 8f / 255), new Color(.6f, .2f, .9f, 0) };
            foreach (string name in names) foreach (int scale in new[] { 0, 1, 2 }) foreach (Color fill in colors)
                foreach (bool multiline in new[] { false, true }) foreach (bool withIcon in new[] { false, true })
                {
                    string title = multiline ? "称A" : "";
                    string[] top = multiline ? new[] { "顶名字", "A B" } : null;
                    string bottom = multiline ? "底｜B" : null;
                    Color titleColor = new Color(.3f, .8f, .2f, .5f);
                    Color topColor = new Color(.9f, .2f, .6f, .7f);
                    Color bottomColor = new Color(.2f, .7f, .9f, .3f);
                    var expected = LegacyBitmapFont.BakeNameTag(name, title, fill, titleColor, scale, top, topColor, bottom, bottomColor, withIcon ? icon : null);
                    var actual = BitmapFont.BakeNameTag(name, title, fill, titleColor, scale, top, topColor, bottom, bottomColor, withIcon ? icon : null);
                    PixelsEqual(actual, expected, "Legacy bitmap equivalence");
                }
            Check(BitmapFont.BakeNameTag(null, "", Color.white, Color.white, 1) == null, "Null name returns null");
            Check(BitmapFont.BakeNameTag("", "", Color.white, Color.white, 1) == null, "Empty name returns null");
            var loader = Bind(); Array.Clear(loader.Atlas.Pixels, 0, loader.Atlas.Pixels.Length);
            loader.Atlas.Pixels[47 * 48] = new Color32(255, 0, 0, 7);
            loader.Atlas.Pixels[47 * 48 + 1] = new Color32(0, 255, 0, 8);
            loader.Atlas.Pixels[47 * 48 + 2] = new Color32(0, 0, 255, 255);
            var glyph = BitmapFont.BakeNameTag("A", "", Color.white, Color.white, 1);
            Check(glyph.Pixels[(glyph.height - 2) * glyph.width + 1].a == 0, "Source alpha 7 is skipped");
            Check(glyph.Pixels[(glyph.height - 2) * glyph.width + 2].a == 8, "Source alpha 8 is retained");
            Check(glyph.Pixels[(glyph.height - 2) * glyph.width + 3].a == 255, "Source alpha 255 is retained");
            Console.WriteLine("PASS legacy bitmap equivalence: " + comparisons + " complete pixel comparisons; Chinese, mixed ASCII, spaces, missing glyphs, fullwidth separator, scale 0/1/2, all lines, icons, and fill alpha 0/7/8/0.43/255.");
        }
        private static void TestBindingCache()
        {
            var loader = Bind(); Texture2D.ResetCounters();
            for (int i = 0; i < 100; i++) BitmapFont.BakeNameTag("名字" + i, "", Color.white, Color.white, 1);
            Check(Texture2D.AllReads == 1 && Texture2D.Applies == 100, "100 names read atlas exactly once");
            var identity = BitmapFont.NameTagIdentity;
            var alpha = (byte[])Read(typeof(BitmapFont), "nameTagAlpha");
            Check(alpha.Length == loader.Atlas.width * loader.Atlas.height, "Cache stores one byte per atlas pixel, not RGBA");
            Texture2D.ResetCounters();
            for (int i = 0; i < 100; i++) new MonsterEntity { mname = "怪物" + i }.UpdateNameTagForIndependentRuntime();
            Check(Texture2D.AllReads == 0 && ReferenceEquals(identity, BitmapFont.NameTagIdentity), "New map entities preserve font binding/cache");
            var replacement = new ResLoader(1);
            Set(typeof(BitmapFont), "atlas", replacement.Atlas);
            Set(typeof(LegacyBitmapFont), "atlas", replacement.Atlas);
            var newIdentity = BitmapFont.NameTagIdentity;
            Check(!ReferenceEquals(identity, newIdentity) && Read(typeof(BitmapFont), "nameTagAlpha") == null, "Atlas-only replacement invalidates old alpha immediately on binding observation");
            Texture2D.ResetCounters();
            var fresh = BitmapFont.BakeNameTag("AB名字", "", Color.white, Color.white, 1);
            Check(Texture2D.AllReads == 1, "New atlas read exactly once");
            PixelsEqual(fresh, Oracle("AB名字", Color.white), "Rebound atlas does not use old pixels");
            var indices = (ushort[])((ushort[])Read(typeof(BitmapFont), "idx")).Clone();
            indices['A'] = indices['B'];
            Set(typeof(BitmapFont), "idx", indices); Set(typeof(LegacyBitmapFont), "idx", indices);
            identity = BitmapFont.NameTagIdentity;
            Check(!ReferenceEquals(identity, newIdentity), "Index-only replacement changes generation");
            Texture2D.ResetCounters(); fresh = BitmapFont.BakeNameTag("AB", "", Color.white, Color.white, 1);
            Check(Texture2D.AllReads == 1, "Index replacement reads new generation once");
            PixelsEqual(fresh, Oracle("AB", Color.white), "Replacement index applied");
            var widths = (byte[])((byte[])Read(typeof(BitmapFont), "wid")).Clone(); widths[indices['B']] = 12;
            Set(typeof(BitmapFont), "wid", widths); Set(typeof(LegacyBitmapFont), "wid", widths);
            Check(!ReferenceEquals(identity, BitmapFont.NameTagIdentity), "Width-only replacement changes generation");
            Texture2D.ResetCounters(); fresh = BitmapFont.BakeNameTag("AB", "", Color.white, Color.white, 1);
            Check(Texture2D.AllReads == 1, "Widths replacement reads new generation once");
            PixelsEqual(fresh, Oracle("AB", Color.white), "Replacement width applied");
            loader = Bind(2); loader.Atlas.FailRead = true;
            bool failed = false;
            try { BitmapFont.BakeNameTag("A", "", Color.white, Color.white, 1); } catch (InvalidOperationException) { failed = true; }
            Check(failed && Read(typeof(BitmapFont), "nameTagAlpha") == null, "Failed alpha read is never published");
            loader.Atlas.FailRead = false; Texture2D.ResetCounters();
            BitmapFont.BakeNameTag("A", "", Color.white, Color.white, 1);
            BitmapFont.BakeNameTag("B", "", Color.white, Color.white, 1);
            Check(Texture2D.AllReads == 1, "Read failure can retry, then reuse successful plane");
            loader = new ResLoader(3); ResLoader.I = loader; ResetFont(typeof(BitmapFont));
            BitmapFont.EnsureForIndependentRuntime();
            Check(Read(typeof(BitmapFont), "nameTagAlpha") == null, "Ensure discards old alpha on real rebinding");
            Console.WriteLine("PASS alpha cache: one atlas read per binding generation across 100 names; map-style entity recreation reuses it; atlas, index, widths and failed-read cases covered.");
        }
#if !NAME_TAG_COLOR_OVERRIDE
        private static void TestLevelChange()
        {
            int[] levels = { 60, 53, 50, 41 }; // Every difficulty threshold crossed by player 50 -> 51.
            foreach (int level in levels)
            {
                Bind(); CharData.level = 50;
                var old = new LegacyMonsterEntity[100]; var current = new MonsterEntity[100];
                Texture2D.ResetCounters();
                for (int i = 0; i < old.Length; i++) { old[i] = new LegacyMonsterEntity { Level = level }; old[i].UpdateNameTagForIndependentRuntime(); }
                Check(Texture2D.AllReads == 100 && Texture2D.Applies == 100, "Old initial batch read/upload count");
                Texture2D.ResetCounters();
                for (int i = 0; i < current.Length; i++) { current[i] = new MonsterEntity { Level = level }; current[i].UpdateNameTagForIndependentRuntime(); }
                Check(Texture2D.AllReads == 1 && Texture2D.Applies == 100, "New initial batch reads once and uploads original 100 sprites");
                var original = current[0].NameTagRendererForIndependentRuntime.sprite;
                var previousColor = current[0].NameTagRendererForIndependentRuntime.color;
                CharData.level = 51; Texture2D.ResetCounters();
                foreach (var monster in old) monster.UpdateNameTagForIndependentRuntime();
                Check(Texture2D.Created == 100 && Texture2D.AllReads == 100 && Texture2D.Applies == 100, "Old level crossing rebakes all 100");
                Texture2D.ResetCounters();
                foreach (var monster in current) monster.UpdateNameTagForIndependentRuntime();
                NoTextureWork("New level crossing has no bake textures, atlas read, SetPixels, Apply or sprites");
                Check(SpriteRenderer.ColorSets == 100, "Each crossed monster tint is written once");
                Check(ReferenceEquals(original, current[0].NameTagRendererForIndependentRuntime.sprite), "Level crossing reuses exact sprite");
                Check(previousColor != current[0].NameTagRendererForIndependentRuntime.color, "Crossing actually changes color");
                for (int i = 0; i < current.Length; i++)
                    ShaderEqual(current[i].NameTagRendererForIndependentRuntime, old[i].NameTagRendererForIndependentRuntime.sprite.texture, "Tint equals old colored pixels at level " + level);
                // Warm the managed runtime before measuring allocations; GPU/Unity work is not measured.
                for (int pass = 0; pass < 20; pass++) foreach (var monster in current) monster.UpdateNameTagForIndependentRuntime();
                Texture2D.ResetCounters();
                for (int pass = 0; pass < 100; pass++) foreach (var monster in current) monster.UpdateNameTagForIndependentRuntime();
                Check(SpriteRenderer.ColorSets == 0, "Stable 100-monster frames do not dirty renderer colors");
                NoTextureWork("Stable 100-monster frames have no texture work");
                CharData.level = 50; Texture2D.ResetCounters(); long start = GC.GetAllocatedBytesForCurrentThread();
                foreach (var monster in current) monster.UpdateNameTagForIndependentRuntime();
                long bytes = GC.GetAllocatedBytesForCurrentThread() - start;
                Check(bytes == 0, "Crossing 100 names allocates zero managed bytes in API-substitute harness");
                NoTextureWork("Repeated reverse crossing also performs no texture work");
            }
            foreach (bool friendly in new[] { false, true })
            {
                Bind(); CharData.level = 50; var monster = new MonsterEntity { Level = 55, friendly = friendly };
                monster.UpdateNameTagForIndependentRuntime(); var sprite = monster.NameTagRendererForIndependentRuntime.sprite;
                var color = monster.NameTagRendererForIndependentRuntime.color;
                CharData.level = 51; Texture2D.ResetCounters(); monster.UpdateNameTagForIndependentRuntime();
                NoTextureWork("Within-band/friendly update has no texture work");
                Check(SpriteRenderer.ColorSets == 0, "Within-band/friendly updates do not write renderer tint");
                Check(color == monster.NameTagRendererForIndependentRuntime.color && ReferenceEquals(sprite, monster.NameTagRendererForIndependentRuntime.sprite), "Within-band/friendly visual state unchanged");
            }
            Console.WriteLine("PASS actual TargetNameColor: 100 monsters x all 4 threshold crossings at player 50 -> 51. Old: 100 reads/100 uploads; new: 0 reads/0 uploads/0 new sprites and 0 managed bytes. Friendly and within-band controls pass.");
        }
#endif
        private static void TestMonsterInvalidation()
        {
            Bind(); CharData.level = 50;
            var monster = new MonsterEntity(); monster.UpdateNameTagForIndependentRuntime();
            var old = monster.NameTagRendererForIndependentRuntime.sprite;
            monster.mname = "名字B"; Texture2D.ResetCounters(); monster.UpdateNameTagForIndependentRuntime();
            Check(Texture2D.Applies == 1 && Texture2D.AllReads == 0 && old.Destroyed && old.texture.Destroyed, "Actual text change rebakes once and destroys superseded sprite/texture");
            old = monster.NameTagRendererForIndependentRuntime.sprite;
            ShaderEqual(monster.NameTagRendererForIndependentRuntime, Oracle(monster.mname, GameHud.TargetNameColor(monster)), "Changed name appearance");
            Bind(3); Texture2D.ResetCounters(); monster.UpdateNameTagForIndependentRuntime();
            Check(Texture2D.Applies == 1 && Texture2D.AllReads == 1 && old.Destroyed, "Already-ready font replacement invalidates monster sprite");
            ShaderEqual(monster.NameTagRendererForIndependentRuntime, Oracle(monster.mname, GameHud.TargetNameColor(monster)), "Rebound monster appearance");
            old = monster.NameTagRendererForIndependentRuntime.sprite;
            monster.mname = "A怪物"; Texture2D.FailNextApply = true; bool failed = false;
            try { monster.UpdateNameTagForIndependentRuntime(); } catch (InvalidOperationException) { failed = true; }
            Check(failed && ReferenceEquals(old, monster.NameTagRendererForIndependentRuntime.sprite) && !old.Destroyed, "Failed bake preserves previous sprite and success state");
            Texture2D.ResetCounters(); monster.UpdateNameTagForIndependentRuntime();
            Check(Texture2D.Applies == 1 && old.Destroyed, "Failed name change retries on next update");
            ShaderEqual(monster.NameTagRendererForIndependentRuntime, Oracle(monster.mname, GameHud.TargetNameColor(monster)), "Retried name has correct color/text");
            monster.dying = true; Texture2D.ResetCounters(); monster.UpdateNameTagForIndependentRuntime();
            Check(!monster.NameTagRootForIndependentRuntime.activeSelf, "Dying hides tag"); NoTextureWork("Hidden name has no texture work");
            monster.dying = false; monster.UpdateNameTagForIndependentRuntime();
            Check(monster.NameTagRootForIndependentRuntime.activeSelf, "Visible again restores same tag"); NoTextureWork("Visible again reuses tag");
            GameSettings.ShowMonsterNames = false; monster.UpdateNameTagForIndependentRuntime();
            Check(!monster.NameTagRootForIndependentRuntime.activeSelf, "Monster-name setting preserved"); GameSettings.ShowMonsterNames = true;
            monster.friendly = true; CharData.petShow = false; monster.UpdateNameTagForIndependentRuntime();
            Check(!monster.NameTagRootForIndependentRuntime.activeSelf, "Pet visibility setting preserved"); CharData.petShow = true;
            monster.petOwnerId = 2; CharData.nameMode = 3; monster.UpdateNameTagForIndependentRuntime();
            Check(!monster.NameTagRootForIndependentRuntime.activeSelf, "F11 pet visibility preserved"); CharData.nameMode = 0;
            Console.WriteLine("PASS monster invalidation: text, font generation, failed upload/retry, original sprite destruction and visibility.");
        }
#if NAME_TAG_COLOR_OVERRIDE
        private static void TestFractionalAlpha()
        {
            Bind(); var monster = new MonsterEntity();
            Color[] colors = { new Color(.11f, .71f, .31f, 1), new Color(.23f, .51f, .87f, .43f),
                new Color(.91f, .21f, .17f, .43f), new Color(.2f, .3f, .8f, 0), new Color(.7f, .2f, .9f, 1) };
            foreach (var color in colors)
            {
                GameHud.Override = color; Texture2D.ResetCounters(); monster.UpdateNameTagForIndependentRuntime();
                Check(Texture2D.Applies == 1, "Opaque/fractional mode and fractional-color changes rebake");
                var renderer = monster.NameTagRendererForIndependentRuntime;
                Check(renderer.color == (color.a == 1 ? (Color)(Color32)color : Color.white), "Fractional-alpha colored textures are not tinted again");
                ShaderEqual(renderer, Oracle(monster.mname, color), "Fractional alpha preserves legacy shadow and max-alpha overlap");
                bool blackShadow = false;
                foreach (var pixel in renderer.sprite.texture.Pixels)
                    if (pixel.r == 0 && pixel.g == 0 && pixel.b == 0 && pixel.a == 255) blackShadow = true;
                Check(blackShadow, "Shadow includes original opaque black pixels even with fractional/zero fill alpha");
                Texture2D.ResetCounters(); monster.UpdateNameTagForIndependentRuntime(); NoTextureWork("Unchanged fractional or opaque state reuses sprite");
            }
            var before = monster.NameTagRendererForIndependentRuntime.sprite;
            GameHud.Override = new Color(.4f, .5f, .6f, .2f); Texture2D.FailNextApply = true;
            try { monster.UpdateNameTagForIndependentRuntime(); } catch (InvalidOperationException) { }
            Check(ReferenceEquals(before, monster.NameTagRendererForIndependentRuntime.sprite), "Failed mode switch preserves old sprite");
            GameHud.Override = new Color(.3f, .9f, .1f, 1); Texture2D.ResetCounters(); monster.UpdateNameTagForIndependentRuntime();
            NoTextureWork("Failed fractional bake does not poison opaque cache state");
            ShaderEqual(monster.NameTagRendererForIndependentRuntime, Oracle(monster.mname, GameHud.Override), "Failed mode retry cannot show wrong tint");
            Console.WriteLine("PASS injected future alpha colors: exact legacy fractional-alpha/zero-alpha shadow semantics, mode transitions and failed-switch retry. Production MonsterEntity.NameTag.cs compiled unchanged; only GameHud is substituted for this run.");
        }
#endif
        private static void Main()
        {
#if NAME_TAG_COLOR_OVERRIDE
            TestFractionalAlpha();
#else
            TestPixelEquivalence(); TestBindingCache(); TestLevelChange(); TestMonsterInvalidation();
#endif
            Console.WriteLine("PASS " + assertions + " name-tag assertions. CPU/API-substitute contracts only; not Unity/device/frame-time evidence.");
        }
    }
}
