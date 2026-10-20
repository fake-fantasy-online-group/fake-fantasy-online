# Downloaded resource location

Windows Players store remote resources at `<exe directory>/DownloadedResources/Remote`, derived from Unity's absolute `Application.dataPath`, never the working directory. Settings, update-address configuration and client-package downloads remain in their existing per-user directories. Android, macOS and the Unity Editor keep their existing `persistentDataPath/MobileResources/Remote` location; this change never writes inside a macOS `.app`.

The startup gate activates the verified generation from the selected resource store. Windows package loaders then use that activated root rather than allowing old adjacent packages to shadow downloaded resources. Incomplete downloads are never activated.

There is no legacy-directory search, migration or compatibility fallback. Existing files in a previous user-directory location are neither copied nor deleted. A Windows installation location without write permission fails visibly; resources never silently fall back to C drive/user storage.

## Future upgrades and uninstall

The desktop self-updater reserves `DownloadedResources` against package contents and carries the directory across Windows app replacement by same-volume rename, retaining it through rollback/recovery. A newly exposed live application already contains its resources. The updater does not copy or hash the resource tree. macOS update behavior remains unchanged.

The separately maintained Windows NSIS installer must keep `DownloadedResources` out of its generated owned-payload list. Its existing owned-file-only uninstall and non-recursive final `RMDir` leave runtime resources and unrelated user files intact. This source change does not modify or publish that external installer template.

## Validation

`Tests/Mobile/run-resource-storage-contract.sh` checks absolute/cwd-independent selection and unchanged sandbox selection. `run-resource-loader-root-contract.sh` compiles the actual loader selection methods for Windows, Android, macOS and Windows Editor, including Windows no-fallback behavior. Desktop updater contracts cover resource preservation, interruption/recovery, ownership conflicts and package reservation.

Cloud managed tests do not substitute for an actual Windows Player run. Before release, verify writable/protected installation paths, update rollback and NSIS upgrade/uninstall on Windows. No version change or package publication is part of this patch.
