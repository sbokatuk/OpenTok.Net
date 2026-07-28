# Curated release notes

`.github/workflows/release.yml` looks for a file named after the version being released, e.g.
`2.34.1.1.md`, and uses it as the GitHub release body verbatim. When no such file exists, the
workflow falls back to a generated `git log` summary since the previous tag.

Add a file here before tagging a release you want curated notes for; otherwise the generated
changelog is fine.
