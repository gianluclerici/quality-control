# Handoff — Step 5: Segmentazione per feature + verifica dimensionale

> Documento di **ripresa lavoro** (sync tra computer). Leggilo per intero all'inizio della prossima
> sessione di Claude Code: contiene lo stato esatto, cosa è già fatto e come, e i prossimi passi.
> Ultimo aggiornamento: 2026-06-23.

## 0. Come ripartire (checklist rapida)

1. `git pull` (questo commit è su `master`, remote `origin`
   = https://github.com/gianluclerici/quality-control.git).
2. Build + test: `dotnet test Ficep.QualityControl/Ficep.QualityControl.Core.Tests/Ficep.QualityControl.Core.Tests.csproj`
   → atteso **46 passed**.
3. I dati demo (`*.ply`, `*.step`, `*.macros.json`) **non sono in git** (artefatti generati,
   `.gitignore`). Rigenerali (vedi §5) se servono per GUI/headless.
4. Regole di progetto da rispettare (da `CLAUDE.md`): **Eyeshot MCP come fonte primaria** per le API
   Eyeshot (server `eyeshot`); **mai `goto`**; **evita di clonare Brep** se non indispensabile;
   **tieni aggiornato `docs/ARCHITECTURE.md`** (vedi §6: è volutamente *non* ancora aggiornato per lo
   Step 5).
5. Il piano completo approvato è in `C:\Users\gcler\.claude\plans\cozy-painting-spring.md` (locale,
   non in git — il riassunto sotto lo replica).

## 1. Dove siamo nella roadmap

- Step 1–4: ✅ completati (vedi `docs/ARCHITECTURE.md`). Lo Step 4 (misura deviazione nuvola↔nominale,
  mappa colore, verdetto) è chiuso con 43 test.
- **Step 5 (in corso): segmentazione per feature + verifica dimensionale dei parametri.**
  - Voce di roadmap di riferimento: `ARCHITECTURE.md` §8.2 *"Segmentazione per feature/macro:
    misurare separatamente fori, scassi, facce → tolleranze per feature"*.

### Scope deciso con l'utente (importante)

Niente riconoscimento feature "alla cieca" (AFR). Gli **input** sono: `macros.json` (`PieceSpec`:
feature + parametri **nominali**) + **Brep nominale** + **point cloud**. L'obiettivo è **segmentare**
la nuvola per feature e poi **misurare i parametri di ogni feature dalla nuvola** e verificare se sono
**in tolleranza** rispetto al nominale (il nominale geometrico si ricava esatto dalla geometria del
cutter, non da una mappatura fragile di lettere macro).

## 2. Piano a piccoli step (stato)

| Step | Descrizione | Stato |
|------|-------------|-------|
| **5.1** | Esporre i cutter dal `BeamFactory` + **segmentazione per feature** + report deviazione per-feature + test | ✅ **FATTO** (questo commit) |
| **5.2** | **Parametro del foro in tolleranza**: Ø nominale dalla geometria del cutter; Ø misurato con fit di cerchio 2D (asse noto → fit lineare Kåsa); banda → in/fuori tolleranza | ⬜ prossimo |
| **5.3** | Parametri dello **scasso** (lunghezza/profondità/raggio) da fit piani+arco | ⬜ |
| **5.4** | **Demo headless** (comando `inspect` nel Generator) + **aggiornamento `docs/ARCHITECTURE.md`** (roadmap, decisioni, riferimento classi) | ⬜ |

Procediamo **uno step alla volta**, con un check con l'utente tra l'uno e l'altro.

## 3. Cosa è stato fatto nello Step 5.1 (e come)

Tutto in `Ficep.QualityControl.Core` (headless, testabile), nessuna GUI.

### Idea
Dal `PieceSpec` (JSON) si ri-derivano in modo deterministico i **solidi-utensile** (cutter) di ogni
feature con `BeamFactory.BuildMachined` (lo stesso codice della generazione). Il **bordo del cutter
coincide con la superficie della feature sul pezzo** → un punto scan vicino al cutter (entro
`onSurfaceTol`) appartiene a quella feature; gli altri sono "base" (corpo trave). La classificazione
usa lo **stesso BVH esatto** (`TriangleBvh`) già usato da ICP e dalla misura.

### File nuovi — namespace `Ficep.QualityControl.Core.Features`
- `Features/FeatureKind.cs` — enum `{ Base, Hole, Notch, Other }` + `FeatureKinds.FromMacroClassName`
  (mappa per prefisso: `INTC*`→Hole, `SCAI*`→Notch).
- `Features/FeatureDescriptor.cs` — `record struct` identità feature: `Id` (1-based; 0 = `Base`),
  `MacroIndex`, `MacroClassName`, `Kind`, `Label`. Statico `FeatureDescriptor.Base`.
- `Features/FeatureCutter.cs` — `record`: `FeatureDescriptor Descriptor` + `Brep Cutter`.
- `Features/FeatureSegmentation.cs` — `FromCutters(cutters, onSurfaceToleranceMm=1.0,
  chordToleranceMm=0.2)`: tassella ogni cutter, costruisce **un** `TriangleBvh` etichettato per
  triangolo; `Classify(Point3D) → int` = indice in `Features`, oppure **-1** = base (se la distanza al
  cutter più vicino supera `onSurfaceTol`).
- `Features/FeatureDeviation.cs` — `record struct` `(FeatureDescriptor Feature, DeviationReport Report)`.
- `Features/SegmentedDeviationReport.cs` — `Overall` (tutta la nuvola) + `Base` (corpo) +
  `Features` (un `DeviationReport` per feature). I bucket **partizionano** la nuvola.
- `Features/FeatureMeasurement.cs` — `Measure(scan, nominal, segmentation, alignment?, tolerance?) →
  SegmentedDeviationReport`. Proietta i punti (riusa il core di `DeviationMeasurement`), poi instrada
  ogni deviazione nel bucket della sua feature.

### File modificati
- `Generation/BeamFactory.cs` — `MachinedBeam` ora espone
  `IReadOnlyList<FeatureCutter> Features` (default vuoto). `BuildMachined` registra ogni cutter
  effettivamente sottratto, con id progressivo 1-based e `Kind` dal nome macro. **Nessun clone del
  Brep**: il `feature.Solid` è già una copia di proprietà del builder e `Brep.Difference` non consuma
  gli operandi (referenziato direttamente).
- `Measurement/DeviationMeasurement.cs` — estratto un helper interno condiviso
  `internal static (PointDeviation[], double[]) ProjectAll(scan, nominal, transform)` con la
  convenzione del **segno** (+ esterno / − interno) in **un solo posto**; `Measure` e
  `FeatureMeasurement` lo riusano. Comportamento pubblico di `Measure` invariato.

### Test nuovo — `Core.Tests/FeatureSegmentationTests.cs` (3 fatti)
- I cutter sono esposti e taggati (≥1 Hole INTC01, ≥1 Notch SCAI01; id 1..N contigui).
- La segmentazione instrada i punti foro/scasso ai bucket per `Kind`, base resta maggioranza, la
  partizione è esatta, RMS≈0 su nuvola pulita.
- Grezzo senza macro → tutto in `Base`, `Features` vuoto.

### Risultato osservato (pezzo demo IPE300 + SCAI01 + INTC01)
Il pezzo demo produce **5 cutter**: **SCAI01 → 4 (Notch)**, **INTC01 → 1 (Hole)** — utile da ricordare,
una macro può espandersi in più cutter. Su nuvola pulita densità 0.3 pt/mm² (~341k punti):
`base=339629, hole=298, notch=872`. Tutti i **46 test verdi**.

## 4. Decisioni di design rilevanti (per non rifare i ragionamenti)

- **Segmentazione via cutter ri-derivati dal JSON** (non via riconoscimento topologico): robusta,
  generale (foro e scasso allo stesso modo), riusa il BVH esatto. L'AFR "alla cieca" è stato
  esplicitamente **scartato** dall'utente per questo step.
- **`Classify` = cutter più vicino entro `onSurfaceTol`**: senza la soglia *ogni* punto verrebbe
  assegnato al cutter più vicino (anche la base). La soglia (default 1.0 mm) è la "porta" che dice
  "questo punto è su una superficie lavorata". Per i test su nuvola pulita si usa 0.5 mm.
- **Layering pulito**: `Generation → Features → {Registration, Sampling, Measurement}`. Niente cicli.
  Si è scelto un `FeatureMeasurement` separato invece di aggiungere `MeasureSegmented` a
  `DeviationMeasurement`, per non far dipendere il layer Measurement dal layer Features.
- **`Vec3`/`TriangleBvh` sono `internal`** ma nello stesso assembly Core → riusati direttamente da
  `Features`.

## 5. Rigenerare i dati demo (non in git)

```
dotnet run --project Ficep.QualityControl/Ficep.QualityControl.Generator -- generate \
  --out Ficep.QualityControl/Ficep.QualityControl.Generator/Assets --seed 1234
```
Produce `grezzo.*`, `lavorato.*` (PLY rumoroso + `_clean` + STEP nominale) e `lavorato.macros.json`.
Il `PieceSpec` demo (anche per i test) è: IPE300 L=1000, `SCAI01 C/I/A` A80 B60 C40 D60 E40 R10, e
`INTC01 C/I/A` A500 B150 C40. (INTC01: `radius = C/2` ⇒ **foro Ø40**; centro (A,B).)

## 6. Da fare ALL'INIZIO dello Step 5.4 (promemoria)

`docs/ARCHITECTURE.md` **non è ancora aggiornato** per lo Step 5 (per scelta di piano: aggiornamento
unico a fine Step 5). Quando si chiude lo Step 5, aggiornare: roadmap §3 (nuova riga Step 5), decisioni
§5 (segmentazione via cutter; fit cerchio ad asse noto; nominale da geometria), riferimento classi §6
(nuova §6.x `Features/`), e la data "Ultimo aggiornamento".

## 7. Dettaglio del prossimo step (5.2) — foro in tolleranza

Obiettivo: primo "verdetto dimensionale". Per la feature `Hole` (INTC01):
- **Nominale**: Ø dalla geometria del cutter cilindrico (raggio del `Brep.CreateCylinder`,
  vedi `Ficep.MacroGra/Ficep.MacroGra/MACRO/INTC01.cs` ~riga 99-141). Asse = asse del cutter → **noto**.
- **Misurato**: prendere i punti scan del bucket della feature (`FeatureMeasurement` /
  `FeatureSegmentation.Classify`), proiettarli sul piano ⟂ all'asse noto e fare un **fit di cerchio 2D
  ai minimi quadrati (algebrico, lineare — Kåsa)** → Ø e centro misurati. (Con l'asse noto NON serve
  un fit di cilindro non lineare.)
- **Tolleranza**: `FeatureParameter { Name, NominalMm, MeasuredMm, ToleranceMm, InTolerance }` +
  `FeatureInspectionReport` per feature. Test con oracolo: foro perfetto → Ø≈nominale, in banda; foro
  con raggio gonfiato di δ → Ø≈nominale+2δ, banda stretta lo rifiuta.
- Riuso: `FeatureSegmentation`/`SegmentedDeviationReport` (5.1), `ToleranceBand`
  (`Measurement/ToleranceBand.cs`), `Vec3` per la matematica.
