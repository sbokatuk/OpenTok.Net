# Upgrading from the pre-2.27.1.900 `OpenTok.Net` package

2.27.1.900 is a structural rewrite of how the `OpenTok.Net` package is built, not a native SDK bump.
It only affects `OpenTok.Net` itself - if you reference `OpenTok.Net.iOS` or `OpenTok.Net.Android`
directly, nothing here changes anything about those packages; they are built and published from
their own repositories, unaffected by this repository's rewrite.

## This repository no longer builds the platform packages

Before 2.27.1.900, this repository built `OpenTok.Net.iOS`, `OpenTok.Net.Android` and two Android
dependency packages (`OpenTok.Net.webrtc.Dependency.Android`,
`OpenTok.Net.mltransformers.Dependency.Android`) alongside the umbrella `OpenTok.Net`. It no longer
does: `OpenTok.Net.iOS` and `OpenTok.Net.Android` are built and published from their own
repositories, and `OpenTok.Net` depends on them as ordinary pinned NuGet packages (see
[`packaging.md`](packaging.md)). If you were building this repository from source to produce those
platform packages yourself, that workflow is gone - `build/loadnative.sh` and `build/bind.sh`, and
the `src/OpenTok.Net.iOS`/`src/OpenTok.Net.Android`/`src/OpenTok.Net.*.Dependency.Android` projects,
no longer exist here.

If you only ever consumed the published `OpenTok.Net` package, nothing changes: keep referencing
`OpenTok.Net`, and it keeps resolving `OpenTok.Net.iOS`/`OpenTok.Net.Android` per platform head the
same way it always has.

## The `OpenTok.Net` package no longer copies native files at build time

The old `OpenTok.Net` package was hand-assembled from the built iOS and Android packages' output and
carried an MSBuild target that copied native files from `native/` into `lib/<tfm>/` the first time
your project built. The current `OpenTok.Net` package is an ordinary façade: it references
`OpenTok.Net.iOS` on iOS heads and `OpenTok.Net.Android` on Android heads via a conditional
`PackageReference`, and NuGet resolves the right one per target framework the normal way. Nothing on
your side should need to change - you keep referencing `OpenTok.Net`.

## Target frameworks `OpenTok.Net` itself declares

`OpenTok.Net` now declares `net8.0-android34.0`, `net8.0-ios18.0`, `net9.0-android35.0`,
`net9.0-ios18.0`, `net10.0-android36.0` and `net10.0-ios26.0` - explicit OS versions, replacing the
old bare monikers (`net8.0-android`, `net8.0-ios`) and dropping `net7.0`. This is a claim about the
façade package only: whether `OpenTok.Net.iOS`/`OpenTok.Net.Android` themselves ship matching assets
is up to their own release cadence, and NuGet's target-framework compatibility rules let an older,
compatible asset from either satisfy a newer request in the same platform family regardless. If your
project still targets `net7.0-android`/`net7.0-ios`, stay on a pre-2.27.1.900 `OpenTok.Net` version
until you can move to `net8.0` or later.

## Versioning

Old `OpenTok.Net` versions used `<OpenTok iOS SDK version>.<build number>`, where the build number
was whatever was passed to `target.sh` by hand and did not always increase predictably (nuget.org has
published `2.27.1.701` and `2.27.1.817`, among other jumps). The current scheme is the same shape -
`<OpenTok iOS SDK version>.<façade revision>` - but the revision is this repository's own tracked
counter starting at 900, chosen to sit above every previously published `2.27.1.x` release rather
than continue their sequence. See the comment on `OpenTokBindingRevision` in `Directory.Build.props`.

## Repository layout, if you build from source

`Bindings/` is now `src/` (and now contains one project, the façade), `SampleApps/` is now
`samples/` (three sample projects consolidated into one multi-headed `samples/OpenTok.Maui.Sample`),
and `NugetPackages/` is gone - packing goes through `build/BuildNugets.sh` against
`build/packages.tsv` instead of hand-maintained `.nuspec` files. See [`packaging.md`](packaging.md).
