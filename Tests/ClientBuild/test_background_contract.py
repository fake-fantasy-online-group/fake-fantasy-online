"""Source/package boundary checks; these do not claim a built Unity Player was tested."""
import hashlib
from pathlib import Path
import re
import struct
import subprocess
import unittest

ROOT = Path(__file__).resolve().parents[2]
ASSET = Path('Assets/FakeFantasyOnline/Runtime/Presentation/Resources/FakeFantasyOnline/Bootstrap/TaoistCloudSanctuary.png')


class BundledBackgroundContract(unittest.TestCase):
    def test_original_art_is_present_and_importable(self):
        data = (ROOT / ASSET).read_bytes()
        self.assertEqual(data[:8], b'\x89PNG\r\n\x1a\n')
        self.assertEqual(struct.unpack('>II', data[16:24]), (1448, 1086))
        self.assertEqual(len(data), 3244769)
        self.assertEqual(hashlib.sha256(data).hexdigest(), 'f6fe66fc183ad1b0bc1e6ce2bbcb778ea98b8b597c7681daaa7dd1041a2e8d21')
        metadata = Path(str(ROOT / ASSET) + '.meta').read_text()
        for setting in ['enableMipMap: 0', 'isReadable: 0', 'nPOTScale: 0', 'maxTextureSize: 2048', 'alphaUsage: 0']:
            self.assertIn(setting, metadata)
        self.assertRegex(metadata, r'(?m)^guid: [a-f0-9]{32}$')
        self.assertIn('assetBundleName:\n', metadata)

    def test_exact_source_asset_is_allowed_without_allowing_game_art(self):
        result = subprocess.run(['git', 'check-ignore', '--no-index', '--quiet', str(ASSET)], cwd=ROOT)
        self.assertEqual(result.returncode, 1, 'The original bootstrap image must be addable without forcing ignored game art')
        for private in [ASSET.with_name('other.png'), Path('Assets/Resources/MobileStartupBackground.png'), Path('Assets/StreamingAssets/gameassets/background.png')]:
            with self.subTest(path=str(private)):
                result = subprocess.run(['git', 'check-ignore', '--no-index', '--quiet', str(private)], cwd=ROOT)
                self.assertEqual(result.returncode, 0, 'Unreviewed artwork must remain ignored')

    def test_runtime_key_matches_bundled_asset_and_stage_boundary(self):
        renderer = (ROOT / 'Assets/FakeFantasyOnline/Runtime/Presentation/EmbeddedClientBackground.cs').read_text()
        key = re.search(r'ResourcePath = "([^"]+)"', renderer).group(1)
        self.assertEqual(key + '.png', str(ASSET).split('/Resources/', 1)[1])
        stage = (ROOT / 'Assets/FakeFantasyOnline/Editor/ResourceFreeBuildStage.cs').read_text()
        self.assertIn('EmbeddedResourcesRelativePath + "/FakeFantasyOnline/Bootstrap/TaoistCloudSanctuary.png"', stage)
        self.assertIn('ValidateEmbeddedBootstrapAssets(assetsRoot)', stage)
        desktop = (ROOT / 'Assets/FakeFantasyOnline/Editor/ClientBuild.cs').read_text()
        self.assertIn('ResourceFreeBuildStage.ValidateEmbeddedBootstrapAssets(UnityEngine.Application.dataPath)', desktop)
        self.assertNotIn('StreamingAssets', key)
        for forbidden in ['ClientResourcePaths', 'PackageReader', 'UnityWebRequest', 'File.ReadAllBytes', 'LoadImage(', 'new Texture2D']:
            self.assertNotIn(forbidden, renderer)

    def test_startup_and_selection_share_background_preserving_creation_art(self):
        installer = (ROOT / 'Assets/FakeFantasyOnline/Runtime/Bootstrap/MobileResourceInstaller.cs').read_text()
        self.assertIn('EmbeddedClientBackground.Preload()', installer)
        self.assertIn('EmbeddedClientBackground.Draw(', installer)
        self.assertNotIn('Resources.Load<Texture2D>("MobileStartupBackground")', installer)
        title = (ROOT / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/TitleFlow.GuiContext.cs').read_text()
        self.assertIn('if (!creationPage)', title)
        self.assertIn('EmbeddedClientBackground.Draw(', title)
        self.assertIn('selBg ?? cbg ?? bg', title)
        self.assertNotIn('loginBg ?? bg', title)
        dispatch = (ROOT / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/TitleFlow.NativeGui.cs').read_text()
        selection = dispatch.split('if (page == TitlePage.CharacterSelection)', 1)[1].split('if (page == TitlePage.ServerSelection)', 1)[0]
        creation = dispatch.split('if (page == TitlePage.CharacterCreation)', 1)[1].split('if (page == TitlePage.CharacterSelection)', 1)[0]
        self.assertIn('BeginGuiFrameForIndependentRuntime(false)', selection)
        self.assertIn('BeginGuiFrameForIndependentRuntime(true)', creation)
        adapter = (ROOT / 'Assets/FakeFantasyOnline/Runtime/Presentation/RecoveredTitleCharacterSurface.cs').read_text()
        self.assertIn('BeginGuiFrameForIndependentRuntime(false)', adapter)


if __name__ == '__main__':
    unittest.main()
