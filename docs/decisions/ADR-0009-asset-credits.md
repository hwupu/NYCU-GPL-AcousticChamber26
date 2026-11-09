# ADR-0009: Every third-party asset is credited (2026-11-09)

Status: **accepted** (PO 2026-11-09: "we should keep track of asset credits, including images and models. Add md file for asset
credits. remind me if i forget to provide credit for new assets")

**Context.** This is an academic project in a public repository. Papers, theses and the app itself must credit what they use, and
some licences require it (CC BY, OFL, MIT). Credits so far were scattered (HW2's `Audio/README.md`) or missing (images, icons, fonts).

**Decision**
- One file, **`docs/AssetCredits.md`**, lists every asset the project did not make itself, and the ones it made, by type: audio,
  images, models, fonts, icons, other. Per entry: file path(s), title, creator, source URL, licence (SPDX id where one exists),
  attribution text, date retrieved, changes made, and whether it may be redistributed in this public repository.
- An asset is added to the file **before** it is committed. Unknown source or licence = status "to check"; such an asset is not
  committed until the PO resolves it.
- Generated stimuli (GPL.Stimuli) and assets made by Editor scripts need no credit; they are listed once as "generated".
- Packages (Meta XR SDKs, Unity packages) are covered by their own licences in `Packages/`, not listed per file.
- The AI assistant reminds the PO whenever a new asset appears without an entry.
- `docs/AssetCredits.md` stays out of git until the PO says otherwise (PO 2026-11-09).

**Consequences**
- A Credits page in the app (needed when CC BY assets ship in a build) is a later UI ticket, designed in the Storybook first.
- Open items in the file now: `Radio.jpg` and the HW2 slides (source not recorded), the HW2 recordings (licence problems, to be
  replaced).
