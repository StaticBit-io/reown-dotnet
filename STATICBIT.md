# StaticBit fork of the Reown .NET SDK

This repository is a fork of [reown-com/reown-dotnet](https://github.com/reown-com/reown-dotnet).
The `staticbit/release` branch is the maintained line: every `StaticBit.Reown.*` package is built
from it, and fixes for the StaticBit wallet land here.

Upstream does not accept external contributions to the .NET SDK at present, but it does fix
reported issues and patches security problems
([maintainer's answer](https://github.com/reown-com/reown-dotnet/pull/314#issuecomment-6826687638)).
So a defect found here is reported upstream as an issue, and the fix stays in this branch until
upstream ships its own.

The packages are internal to the StaticBit-io organisation feed. They are not affiliated with or
endorsed by Reown, Inc.

## Changes against upstream

The base is upstream 1.7.1: upstream `develop` was merged on 18 November 2026 (merge `1a2b936`).
Upstream `develop` past the 1.7.1 release only touched Unity packages and CI at that point.

| Change | Files | Upstream report | Upstream PR |
|---|---|---|---|
| Relay subscriptions are restored after an interrupted transport restart: the reconnect loop runs until the subscriptions are replayed, a failed resubscription fails the open, subscription bookkeeping is thread-safe and tied to the connection it was made on, and `WithTimeout` reports the outcome of the task it wraps | `Relayer.cs`, `Subscriber.cs`, `TopicMap.cs`, `JsonRpcProvider.cs`, `WebsocketConnection.cs`, `WebsocketConnectionBuilder.cs`, `Extensions.cs`, `SignClient.cs` | [#313](https://github.com/reown-com/reown-dotnet/issues/313) | [#314](https://github.com/reown-com/reown-dotnet/pull/314) |
| `KeepAliveInterval` and `KeepAliveTimeout` are not set on `browser-wasm`, where the setter throws `PlatformNotSupportedException` | `ClientWebSocketTransport.cs` | [#346](https://github.com/reown-com/reown-dotnet/issues/346) | [#360](https://github.com/reown-com/reown-dotnet/pull/360) (covers `KeepAliveInterval` only) |
| The `AddressProvider` session handlers tolerate a null `DefaultSession` | `AddressProvider.cs` | [#361](https://github.com/reown-com/reown-dotnet/issues/361) | [#362](https://github.com/reown-com/reown-dotnet/pull/362) |

Not in this branch: per-entry isolation in `FileSystemStorage`
([#347](https://github.com/reown-com/reown-dotnet/issues/347),
[#348](https://github.com/reown-com/reown-dotnet/pull/348), branch
`staticbit/storage-entry-isolation`).

Every `src/` file changed by StaticBit carries a notice at the top. A new file touched by a fix
gets the same notice in the same commit.

## Releases

Each published version has an annotated tag `sb/<version>` on the commit it was built from,
taken from the `repository commit` in the package's nuspec. Packages `1.6.1-sb.12` to `sb.14` were
packed over a stale `build/` directory and their `Reown.Sign.dll` is older than the tagged commit;
their tags say so.

The prefix names the upstream release the branch is based on. Versions up to `1.6.1-sb.16` carry
`1.6.1` for historical reasons: the code had moved to the 1.7.0 line before `sb.12`. The `sb`
number keeps growing across prefixes, so it alone orders the releases.

### Making a release

1. Bump `StaticBitVersion` in `src/Directory.Build.targets`. GitHub Packages never overwrites a
   version, so any change after publishing needs a new number.
2. Delete `build/` completely. Every project writes to the shared `build/<Project>/bin/`, and
   MSBuild reuses stale outputs from it without rebuilding a project whose dependency changed.
3. Build and pack:

   ```bash
   dotnet build Reown.NoUnity.slnf -c Release -p:StaticBitPackaging=true --restore
   dotnet pack Reown.NoUnity.slnf -c Release -p:StaticBitPackaging=true --no-build -o <dir>
   ```

   `--no-build` without the build step before it fails with `NU5026`.
4. Check the packages before publishing, by content rather than by date: every DLL has the same
   build time, the expected fixes are present (for example `ConnectedAndReplayed` in `Reown.Core`,
   `IsBrowser` in `Reown.Core.Network.WebSocket`), and every `StaticBit.Reown.*` dependency is on
   the version being released.
5. Run the wallet against the packages from a local feed before publishing.
6. Commit, tag `sb/<version>`, push the branch and the tag.
7. Publish the eight packages the wallet consumes: `Core`, `Core.Common`, `Core.Crypto`,
   `Core.Network`, `Core.Network.WebSocket`, `Core.Storage`, `Sign`, `WalletKit`.
   `Sign.Nethereum` is built by the solution filter but has never been published.

   ```bash
   dotnet nuget push "<dir>/StaticBit.Reown.*.nupkg" --source https://nuget.pkg.github.com/StaticBit-io/index.json --api-key <token with write:packages>
   ```

### Licence obligations

The SDK is under the WalletConnect Community License (`LICENSE.md` in each project). For this
fork that means: modified files carry a change notice (§1(c)(i)); packages carry the licence and
the copyright line `Portions © 2025 Reown, Inc. All Rights Reserved` (§2(a)); package ids are
prefixed so nothing is published under Reown's name (§2(d)). `src/Directory.Build.targets` sets
the package side of this.

## Other branches

`staticbit/relay-recovery`, `staticbit/wasm-keepalive-guard`,
`staticbit/address-provider-null-default` and `staticbit/storage-entry-isolation` are the head
branches of the upstream pull requests above. Changes are made on `staticbit/release` first.
