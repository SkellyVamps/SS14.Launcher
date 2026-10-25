# Space Station 14, Echoes of the Void launcher

This fork of SS14.Launcher can also run engine builds published by the voice chat engine fork
(`SkellyVamps/RobustToolbox`), so players get voice chat with no manual installs. It still plays every official server.

What's different from upstream:

* Engine versions from `ConfigConstants.CustomRobustBuildsManifest` are merged with the official manifest
  (official versions always win; fork versions use a 4-part version like `293.0.1.1`).
* Engines signed with `SS14.Launcher/signing_key_custom` are accepted in addition to the official `signing_key`.
  **That file must contain your fork's Ed25519 public key (PEM) before you publish a release.**
* Separate data directory (`launcher-voice`) so it never conflicts with an installed official launcher.
* The official "launcher out of date" check is disabled (it would send players to the official launcher).
* Branded "Space Station 14, Echoes of the Void": window title, header logo, app names
  (`SS14 Echoes of the Void Launcher.exe` / `.app`), its own macOS bundle identifiers, release zips named
  `EchoesOfTheVoid_Launcher_<platform>.zip`. The official seasonal logo/icon overrides are never applied (they would
  replace the fork's branding), and their option is hidden.
* Multiple accounts are always on: the account menu has "Back to login screen" to log in with another account, and
  lists saved accounts to switch between.
* Failing to reach the auth server (offline, auth outage) shows an error instead of crashing on startup.

Release: create a GitHub release (new tag `vX.Y.Z`, target `voice-chat`) or push a tag; `publish-release.yml` builds
Windows, macOS and Linux zips and attaches them to that release.
