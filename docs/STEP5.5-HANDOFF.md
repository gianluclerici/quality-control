# Handoff — Step 5.5: dimensionamento feature-relative (datum dal cloud), foro completo, scasso completo

> Documento di **ripresa lavoro** (sync tra computer). Leggilo per intero all'inizio della prossima
> sessione di Claude Code: contiene lo stato esatto, il piano deciso con l'utente, e i prossimi passi.
> **Stato: Fase A FATTA (stima datum dal cloud) — prossimo: Fase B.** Ultimo aggiornamento: 2026-06-24.

## 0. Come ripartire (checklist rapida)

1. `git pull` — il remote si chiama **`remote`** (NON `origin`): `git pull remote master` /
   `git push remote master`. URL = https://github.com/gianluclerici/quality-control.git, branch `master`.
   HEAD a questo handoff: **`c4e9199`** (questo doc), sopra **`aca698a`** ("... pannello feature ... QC Step 5.4").
2. Build + test: `dotnet test Ficep.QualityControl/Ficep.QualityControl.Core.Tests/Ficep.QualityControl.Core.Tests.csproj`
   → atteso **60 passed** (58 baseline + 2 della Fase A appena fatta).
3. I dati demo (`testdata/*.ply|*.step|*.macros.json`) **non sono in git** (`.gitignore`). Rigenerali
   (vedi §6) se servono per GUI/headless.
4. Regole di progetto (da `CLAUDE.md`): **graphify query PRIMA** di Read/Grep per localizzare codice
   (regola ferrea); **Eyeshot MCP come fonte primaria** per le API Eyeshot; **mai `goto`**; **evita di
   clonare Brep**; **tieni aggiornato `docs/ARCHITECTURE.md`** ad ogni step; `graphify update .` dopo le
   modifiche.
5. **Procediamo uno step alla volta, con un check con l'utente tra l'uno e l'altro.** In particolare:
   fermarsi dopo la **Fase A** (spike datum) prima di proseguire.
6. Il piano completo è nel file di piano locale (non in git):
   `C:\Users\gianluca.clerici\.claude\plans\peaceful-wondering-token.md`. Il riassunto sotto lo replica.

## 1. Dove siamo nella roadmap

- Step 1–4: ✅ completati. Step **5.1–5.4**: ✅ completati e committati (vedi `docs/STEP5-HANDOFF.md` e
  `ARCHITECTURE.md` §5.17–§5.21, §6.8–§6.9). L'ultimo commit `aca698a` include anche il **pannello
  feature in GUI** (lista feature + parametri nominali/misurati, macro auto-caricate dallo STEP).
- **Step 5.5 (questo handoff): Fase A FATTA; Fase B/C da implementare.**

## 2. Cosa vuole l'utente (richiesta)

Oggi la misura per-feature è **parziale e dipendente dall'allineamento**:
- **Foro**: misura solo il **diametro**. Per definirlo completamente servono **diametro + profondità +
  centro (x,y,z)**.
- **Scasso (SCAI01)**: misura solo **Length(A)/Depth(B)/Radius(R)**. L'utente vuole **misurarli tutti**
  i 6 parametri del contorno (**A,B,C,D,E,R**), espressi in un **sistema relativo dello scasso**.
- Il **frame relativo** ha origine in un **angolo della trave** scelto da Vx/lato:
  `iniziale·alaA = (0,0,0)`, `alaB = (0, prf.width, 0)`, `finale·alaA = (l,0,0)`, `finale·alaB = (l, prf.width, 0)`
  (`l` = lunghezza trave, `prf.width` = larghezza profilo = `SB`).
- **I riferimenti vanno ricavati dalla point cloud** stimando le dimensioni della trave (non dal nominale).
- **Vincolo aggiunto dall'utente:** alcuni parametri possono essere **0** (es. `R=0` → spigolo vivo,
  nessun raccordo). La misura deve gestirlo senza crashare e senza fittare l'elemento assente.

### Risposte dell'utente alle domande di pianificazione
- **Strategia:** *Spike-first sui datum* — implementare e testare prima la stima dimensioni trave dal
  cloud, check con l'utente, poi il resto.
- **Scope scasso:** *Tutti e 6 i parametri* (A,B,C,D,E,R), incl. bordo superiore inclinato (C,D,E).
- **Report:** *Feature-relative come valore primario* — il "Misurato" è il valore feature-relative;
  aggiungere righe per i datum (L/larghezza trave stimate) e per il centro foro (x,y,z).

## 3. Decisione di tecnica (ragionamento già fatto — non rifarlo)

**Si MANTIENE la famiglia di algoritmi attuale** ("orientamento noto dal cutter → ogni fit collassa
alla sua incognita minima"). RANSAC/PCA/OBB servono solo con orientamento **ignoto**; dopo l'ICP l'asse
trave è già allineato al nominale, quindi ogni piano-datum ha **normale nota** e si fitta con lo stesso
`PlaneFit.RobustOffset` (mediana robusta dell'offset) già usato per le pareti dello scasso — più
economico e **più accurato** che ristimare una normale da una patch rumorosa.

**La novità NON è un nuovo fitter, è il layer datum/feature-relative.** Misurando le distanze
**feature→datum** (entrambe stimate dallo stesso cloud) il risultato è **invariante all'allineamento**:
un bias ICP trasla feature e datum insieme, la loro differenza si conserva. Questo è esattamente
l'upgrade già pre-progettato in `docs/research/notch-parameter-extraction.md` §C e in `ARCHITECTURE.md`
§5.18 (limite noto).

## 4. Fatto chiave: la geometria SCAI01 è GIÀ in un frame locale

Il contorno della macro ([Ficep.MacroGra/Ficep.MacroGra/MACRO/SCAI01.cs](../Ficep.MacroGra/Ficep.MacroGra/MACRO/SCAI01.cs), `CreateMacro` ~righe 95-99)
è un poligono a 5 vertici nel piano di estrusione (anima "C"), **già nel frame locale** che l'utente
descrive (origine `P0=(0,0)` = angolo trave; i `Mirror*` lo posano su iniziale/finale e alaA/alaB):

```
P0 (0,        0)            origine = angolo datum (estremità trave × bordo ala)
P1 (A,        0)            back wall  -> A  (lunghezza, lungo l'asse trave)
P2 (A,        B)   fillet R inner corner -> B (profondità) + R (raggio raccordo)
P3 (E, D-(D-B)*E/A)         slant point -> E, D (bordo superiore inclinato)
P4 (0,        D+C)          top edge   -> C, D
```

Quindi "misurare tutti i parametri" = ricavare A,B,C,D,E,R da queste facce/vertici, ciascuno con
**orientamento noto** dal cutter, espressi come distanze relative all'origine-datum.

## 5. Piano a piccoli step

### Fase A — Stima datum dal cloud  ✅ **FATTA**
File nuovo `Ficep.QualityControl.Core/Features/BeamDatums.cs` (`BeamDatums` + `BeamDatumFrame`, public)
e test `Core.Tests/BeamDatumsTests.cs` (2 fatti). **Tutti i 60 test verdi.**

- **Frame trave confermato dal sorgente** (`EyeWorkPiece.CreateSolidRawPart`, profilo in `Plane.YZ`):
  **X = lunghezza**, **Y = altezza (`SA`)**, **Z = larghezza ala (`SB` = `prf.width`)**. (Doc anche in
  `BeamSpec`: "extruded along X; cross-section in YZ".)
- `BeamDatums.Estimate(baseSamples, normalCosThreshold=0.9, minPointsPerFace=10, faceBandMm=2.0)` →
  `BeamDatumFrame(XMin,XMax,YMin,YMax,ZMin,ZMax)` con `MeasuredLengthMm/HeightMm/WidthMm`. Per ciascuna
  delle 6 facce: seleziona i punti del **base bucket** la cui `SurfaceSample.Normal` è ~parallela alla
  normale uscente della faccia (`dot ≥ cos`), poi prende il **cluster più ESTERNO** (banda `faceBandMm`,
  ≥ `minPointsPerFace`) e ne fa la **mediana** (`PlaneFit.Median`).
- **Lezione chiave (importante per Fase B):** la mediana *globale* dei punti per-normale è **sbagliata**
  quando più facce parallele condividono la normale (lungo Z l'enorme fianco anima a z≈78.55 dominava →
  larghezza usciva 7.1 = `TA`, non 150). Si prende perciò il **cluster outermost**, non la mediana di
  tutti. (Lungo X/Y c'è una sola faccia per verso, ma la stessa logica vale e li rende robusti.)
- **Risultato sul demo (cloud pulito IPE300, densità 0.5):** length **1000.000**, height **300.000**,
  width **150.000**. Secondo test: le dimensioni sono **invarianti per traslazione** (le facce si
  selezionano per normale, la differenza faccia-faccia non cambia) — dimostra l'invarianza
  all'allineamento su cui poggia la misura feature-relative.
- **Base bucket** in pratica: `cloud.Where(s => segmentation.Classify(s.Position) < 0)` (vedi test). In
  Fase C il `PieceInspector` lo ricava applicando l'`alignment` ai sample (posizione+normale) e tenendo
  i `Classify < 0`, così i datum si stimano nel frame allineato.

> **Note per Fase B/C** (non ancora fatte): il `BeamDatumFrame` per ora espone solo i 6 offset + le
> dimensioni. Il **corner-origin** per Vx/lato (`X∈{XMin,XMax}` per `I/F`, `Z∈{ZMin,ZMax}` per ala A/B)
> e gli assi del frame feature-locale vanno aggiunti in Fase B (sono stati lasciati fuori di proposito
> per non fissare una convenzione prematura).

### Fase B — Frame feature-relative + parametri (dopo l'ok)
- **Foro** (`Features/HoleInspection.cs`): aggiungi
  - **Profondità** = estensione assiale dei punti del bucket foro lungo l'asse noto (`CutterAxis`),
    `max−min` proiezione; nominale dalla macro. *(Default: span forato; annotare, correggibile.)*
  - **Centro (x,y,z)** = centro `CircleFit` (piano ⟂ asse) + posizione assiale, espresso nel
    **BeamDatumFrame**. Nuove righe `CenterX/CenterY/CenterZ`.
- **Scasso** (`Features/NotchInspection.cs` + `Features/ExtrudedProfile.cs`): misura **A,B,C,D,E,R**
  come distanze **feature-relative** nel frame locale:
  - `A` back wall → piano estremità; `B` depth wall → bordo ala; `R` raccordo (fit tangente esistente);
  - `C,D,E`: estendere `ExtrudedProfile` a esporre il **vertice slant P3** e il **top P4**; fittare le
    facce/segmenti corrispondenti (offset a normale nota; slant per intersezione facce note).
- **Gestione parametri = 0 (degeneri)** — REQUISITO ESPLICITO:
  - i parametri a 0 si conoscono **a priori dall'input** (riceviamo la `MacroSpec` coi suoi parametri):
    la decisione di saltare un elemento si prende **dai nominali**, NON si rileva dalla geometria/cloud.
  - per ogni nominale `=0` l'elemento è **assente** → **non fittarlo**, riportare misurato `0` (o riga
    "n/a"), verdetto in tolleranza se misurato ~0.
  - `R=0`: nessuna faccia raccordo → `ExtrudedProfile` **non** deve assumere `HasFillet`; selezione del
    cutter-profilo robusta anche senza fillet; **saltare** il golden-section.
  - **guardie su tutte le divisioni** del contorno (`E/A`, `(D-B)*E/A`).

### Fase C — Orchestratore, GUI, test, docs
- `Features/PieceInspection.cs` `PieceInspector`: stima i **datum una volta** dal
  `SegmentedDeviationReport.Base`, passa il `BeamDatumFrame` a Hole/Notch. `DescribeNominal` produce i
  nominali delle nuove righe (centro/profondità/C,D,E) con `MeasuredMm = NaN`.
- `App/MainForm.cs`: il pannello mostra il valore **feature-relative come "Misurato"**, più righe per i
  **datum** (L/larghezza trave) e per il **centro foro (x,y,z)**. Riuso `RenderSelectedFeature`.
- Test: `BeamDatumsTests` (Fase A); estensioni a `HoleInspectionTests` (profondità+centro, caso
  passante), `NotchInspectionTests` (6 parametri feature-relative + **caso R=0/param degeneri**),
  `PieceInspectionTests` (datum nel report; **invarianza**: stesso verdetto applicando una trasformata
  extra al cloud).
- Docs: `ARCHITECTURE.md` (nuova **§5.22** + riga roadmap **5.5** + §6.8 aggiornata + conteggio test +
  data); aggiornare `docs/research/notch-parameter-extraction.md` (upgrade ora implementato).

## 6. Rigenerare i dati demo (non in git)

```
dotnet run --project Ficep.QualityControl/Ficep.QualityControl.Generator -- generate \
  --out testdata --seed 1234
```
Produce `grezzo.*`, `lavorato.*` (PLY rumoroso + `_clean` + STEP nominale) e `lavorato.macros.json`.
`PieceSpec` demo: IPE300 L=1000, `SCAI01 C/I/A` A80 B60 C40 D60 E40 R10, `INTC01 C/I/A` A500 B150 C40
(INTC01: `radius = C/2` ⇒ foro Ø40; centro ≈ (A,B) = (500,150)).

## 7. Riuso / mattoni esistenti (così il prossimo agente non li ri-cerca)
- `Features/PlaneFit.cs` — `RobustOffset(Vec3 normal, IEnumerable<Vec3> points)` (mediana): **datum +
  pareti**.
- `Features/CircleFit.cs` — `Fit(u,v)` Kåsa: **centro+Ø foro**.
- `Features/CutterAxis.cs` — `FromCutter(Brep)`: **asse foro**.
- `Features/ExtrudedProfile.cs` — frame locale (Origin/U/V), pareti `WallLine`, spigolo: **base scasso**;
  da estendere per slant/top + caso `R=0`.
- `Features/NotchInspection.cs` / `HoleInspection.cs` — da estendere.
- `Features/PieceInspection.cs` — `PieceInspector`; `SegmentedDeviationReport.Base` = **bucket corpo per
  i datum**.
- `Registration/RigidTransform.cs` — `Apply`, `Compose`, `Identity`: frame transforms.
- `Sampling/SurfaceSample.cs` — `Position` + `Normal` (la **normale per-punto** seleziona i punti del
  datum per direzione).
- `Model/BeamSpec.cs` — `SA` (altezza), `TA` (web), `SB` (larghezza ala = `prf.width`), `TB`, `Length`.

## 8. Stato git
- **Fase A committata e pushata** su `remote/master`: `Features/BeamDatums.cs` +
  `Core.Tests/BeamDatumsTests.cs` + questo handoff aggiornato + grafo graphify.
- Quando si riprende: partire dalla **Fase B** (vedi §5). Far girare i test (atteso 60), implementare
  foro completo + scasso 6 parametri feature-relative + gestione param a 0, **fermarsi per il check**
  prima di Fase C. Commit solo su richiesta esplicita dell'utente.
