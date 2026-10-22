# Voice chat launcher fork

This fork of SS14.Launcher can also run engine builds published by the voice chat engine fork
(`SkellyVamps/RobustToolbox`), so players get voice chat with no manual installs. It still plays every official server.

What's different from upstream:

* Engine versions from `ConfigConstants.CustomRobustBuildsManifest` are merged with the official manifest
  (official versions always win; fork versions use a 4-part version like `293.0.1.1`).
* Engines signed with `SS14.Launcher/signing_key_custom` are accepted in addition to the official `signing_key`.
  **That file must contain your fork's Ed25519 public key (PEM) before you publish a release.**
* Separate data directory (`launcher-voice`) so it never conflicts with an installed official launcher.
* The official "launcher out of date" check is disabled (it would send players to the official launcher).

Release: push a tag `vX.Y.Z` and `publish-release.yml` builds Windows, macOS and Linux zips into a GitHub release.
