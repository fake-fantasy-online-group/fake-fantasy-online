# Asset source and publication boundary

These rules apply to all work in this repository, including cloud tasks and releases.

- Original game PKG images, effects, icons, models and audio must reference their existing package paths directly. Do not extract, rename, move or repack package content into copies for upload or publication, including private resource releases and download manifests, not just GitHub.
- Extraction is allowed only for temporary local inspection or preview. Keep extracted files out of commits, release artifacts and download manifests. Never automatically publish inspection, preview or QA output directories.
- Recoloring, cropping, format conversion, tracing or remixing original game content does not make it fully original. Do not use these changes to bypass this boundary.
- New code, parameters and EFF configuration are allowed when they are genuinely new configuration that references original PKG content without embedding or bundling that content.
- Only 100% original artwork with clear, owned source may be published as new assets, including on GitHub. For qualifying files, add narrowly targeted `.gitignore` exceptions only; preserve the existing two original PNG exceptions. Never add broad allowlists for original game art.
- Never expand release or download allowlists merely to accept copied package assets. Tests must verify that original package references resolve, not require loose duplicates; do not introduce a full hash-deduplication pipeline as a substitute for direct references.
- Public source may contain code, genuine configuration and qualifying fully original artwork, but not external game resources. Private release authorization does not permit extracted copies either. Keep private deployment paths, domains, credentials and operational settings out of public source and these instructions.
