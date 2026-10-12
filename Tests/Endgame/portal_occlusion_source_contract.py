from pathlib import Path
root=Path(__file__).resolve().parents[2]
p=root/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
s=(p/'ReferenceLegacyMapObjectRenderer.cs').read_text()
assert s.index('PreparePortalAnchors(sourceMap);') < s.index('foreach (GsmMap.Obj source in sourceMap.libObjs)')
assert s.index('PlaceSlices(receiver, value, parent') < s.index('TryPlacePortalAperture(receiver, value, parent') < s.index('TryPlaceFootprintSlices(receiver, value, parent')
assert s.index('DisposePortalApertures();') < s.index('DisposeFootprintStrips();')
assert 'int sortingOrder = animate ? value.SourceY + image.clipBy : requestedSortingOrder;' in s
for n in ['ReferencePortalApertureProfile','ReferenceLegacyMapObjectRenderer.PortalAperture']:
 assert 'guid: ' in (p/(n+'.cs.meta')).read_text()
print('PASS 6 portal production wiring/source and metadata checks')
