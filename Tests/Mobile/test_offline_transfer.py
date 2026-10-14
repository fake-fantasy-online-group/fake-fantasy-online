import hashlib
import importlib.util
from pathlib import Path
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('transfer', ROOT / 'Documentation/tools/push-mobile-offline-resources.py')
transfer = importlib.util.module_from_spec(spec)
spec.loader.exec_module(transfer)

class ManifestContracts(unittest.TestCase):
    def test_manifest_and_hash(self):
        with tempfile.TemporaryDirectory() as d:
            f = Path(d) / 'm.tsv'
            text = 'FFO-MOBILE-OFFLINE-1\na.pkg\tdata/a.pkg\t3\t' + hashlib.sha256(b'abc').hexdigest() + '\n'
            f.write_text(text)
            version, rows = transfer.entries(f)
            self.assertEqual(version, hashlib.sha256(text.encode()).hexdigest())
            self.assertEqual(rows[0][2], 3)
            f.write_text(text + text.split('\n')[1] + '\n')
            with self.assertRaises(ValueError): transfer.entries(f)
            f.write_text(text.replace('FFO-MOBILE-OFFLINE-1', 'FFO-MOBILE-1'))
            with self.assertRaises(ValueError): transfer.entries(f)
    def test_bad_paths(self):
        for name in ['../a', '/a', 'x/../a', 'a\\b', 'a//b', 'a%2f', 'a.part', 'a.', 'x:abc']:
            with self.subTest(name=name), self.assertRaises(ValueError): transfer.safe_relative(name)
    def test_empty(self):
        with tempfile.TemporaryDirectory() as d:
            f = Path(d) / 'm.tsv'; f.write_text('FFO-MOBILE-OFFLINE-1\n')
            with self.assertRaises(ValueError): transfer.entries(f)
    def test_stream_hash(self):
        with tempfile.TemporaryDirectory() as d:
            f=Path(d)/'fixture'; data=b'abcd'*1000000; f.write_bytes(data)
            self.assertEqual(transfer.digest(f), hashlib.sha256(data).hexdigest())

if __name__ == '__main__': unittest.main()
