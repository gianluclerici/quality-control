# Ficep Quality Control — Architettura, scelte decisionali e riferimento del codice

Documento di riferimento del progetto di **controllo qualità (QC)**: confronto tra il **modello
nominale CAD** di un pezzo lavorato e una **scansione 3D** reale (nuvola di punti), per misurare gli
scostamenti rispetto alle tolleranze.

Contiene: architettura, **decisioni e motivazioni** (perché un algoritmo piuttosto che un altro), e un
**riferimento classe-per-classe e funzione-per-funzione**.

Ultimo aggiornamento: 2026-06-24.

---

## 1. Obiettivo

Dato un pezzo lavorato (es. profilo **IPE300** con scassi e fori) e una **scansione** da camera di
profondità / scanner 3D, vogliamo:

1. caricare il **nominale** (Brep CAD, STEP) e la **scansione** (nuvola PLY);
2. **allineare** la scansione al nominale (registrazione rigida / ICP);
3. **misurare** la distanza punto-superficie → **mappa di deviazione** colorata;
4. produrre un esito di **conformità** rispetto alle tolleranze.

Finché non c'è uno scanner reale, generiamo **scansioni sintetiche** dal nominale (campionamento +
rumore) per sviluppare e testare l'intera pipeline.

---

## 2. Architettura: una Core headless, due frontend

```
                ┌──────────────────────────────┐
                │  Ficep.QualityControl.Core    │   libreria headless, testabile
                │  (geometria, IO, ICP, misura) │
                └───────────────┬──────────────┘
              ┌─────────────────┴──────────────────┐
   ┌──────────┴───────────┐          ┌─────────────┴─────────────┐
   │ ...Generator (CLI)   │          │ ...App (WinForms + Eyeshot)│
   │ genera scan sintetici│          │ viewport: carica e mostra  │
   └──────────────────────┘          └────────────────────────────┘
```

| Progetto | Target | Responsabilità |
|----------|--------|----------------|
| `...Core` | `net8.0` | Tutta la logica: geometria, IO, generazione, registrazione, misura. Usa **devDept Eyeshot** come kernel. Niente UI. |
| `...Generator` | console `net8.0` | CLI headless: `generate` (file di test) + `inspect` (verdetto QC end-to-end). |
| `...App` | WinForms `net8.0-windows` | **Doppia modalità**: GUI con viewport Eyeshot, oppure `--headless`/`inspect` da terminale. Solo vista; la logica resta in Core. |

**Perché questa separazione:** la stessa logica gira da GUI *e* headless (batch/CI) ed è interamente
testabile con xUnit senza aprire finestre. Non è una "shell" separata: è *un* Core con due viste.

---

## 3. Roadmap e stato

| Step | Descrizione | Stato |
|------|-------------|-------|
| **1** | Generazione scansioni sintetiche (tassella → campiona → rumore → PLY/STEP) | ✅ fatto |
| **2** | App viewport + reader Core (carica Brep + nuvola, sovrapposti) | ✅ fatto, committato |
| **3** | **Registrazione / ICP**: allinea scansione al nominale | ✅ fatto (Core + test); collegato alla GUI (bottone *Allinea*) |
| **4** | **Misura tolleranze**: distanza nuvola↔nominale (con segno), statistiche, conformità, mappa deviazione colorata | ✅ fatto (Core + test + GUI) |
| **5.1** | **Segmentazione per feature**: cutter ri-derivati dal JSON + BVH etichettato → ogni punto a una feature; report deviazione per-feature | ✅ fatto (Core + test) |
| **5.2** | **Foro in tolleranza**: Ø misurato con fit di cerchio 2D (asse noto dal cutter), verdetto su banda | ✅ fatto (Core + test) |
| **5.3** | **Scasso in tolleranza**: lunghezza/profondità (fit piani ad orientamento noto) + raggio del raccordo (fit ad asse/centro vincolato) | ✅ fatto (Core + test) |
| **5.4** | **Demo headless** (`inspect` nel Generator) — orchestratore `PieceInspector` riusato da GUI e CLI; **App a doppia modalità** (GUI / `--headless`); **generazione scan da nominale in GUI** (σ/seed/densità); **pannello feature in GUI** (lista feature + parametri nominali/misurati, macro auto-caricate dallo STEP); riferimento classi §6 | ✅ fatto (Core + test + Generator + App) |
| **F** | Report, GD&T, scanner reale | ⬜ futuro |

---

## 4. Decisioni **architetturali**

1. **Core headless + due frontend** (vedi §2). Logica testabile, riusabile, eseguibile senza UI.
2. **Eyeshot MCP come fonte primaria** (regola in `CLAUDE.md`): per ogni API Eyeshot si interroga
   prima il server MCP `eyeshot`, poi l'XML offline. Ha evitato errori su `ReadSTEP.Result`,
   `Mesh.FindClosestTriangle` (che è *ray-based*, non point-to-surface), `ComputeDistances`,
   `InitializeViewports`.
3. **Mai `goto`** (regola di progetto): loop/flag/early-return/metodi estratti.
4. **Brep pesanti → niente clonazioni inutili**: si clona solo quando indispensabile.
5. **Strategie iniettabili**: writer/reader/sampler/exporter dietro interfacce, così sono sostituibili
   e mockabili.
6. **Determinismo**: ogni stadio stocastico (campionamento, rumore, sotto-campionamento ICP) accetta un
   *seed* → output riproducibile, test stabili.

---

## 5. Decisioni **algoritmiche** (perché A e non B)

### 5.1 Campionamento della superficie — *area-weighted* vs per-triangolo
**Scelto:** scegliere un triangolo con probabilità **proporzionale all'area** (tabella cumulata +
ricerca binaria) e poi un punto uniforme dentro al triangolo (√-trick baricentrico).
**Perché:** un "N punti per triangolo" sovra-campiona i triangoli piccoli e sotto-campiona quelli
grandi → densità non uniforme sulla superficie reale. La pesatura sull'area dà densità **uniforme
attesa** (punti/mm² costante), come la risoluzione di uno scanner.

### 5.2 Rumore lungo la **normale** vs isotropo
**Scelto:** rumore gaussiano lungo la normale di superficie (errore di *range*).
**Perché:** una camera di profondità sbaglia soprattutto sulla distanza (lungo il raggio ≈ normale),
non tangenzialmente. Modellare l'errore lungo la normale è più realistico dell'isotropo.

### 5.3 Closest-point su mesh — **brute force vs k-NN centroidi vs BVH**
È la primitiva più importante (la usano ICP e misura). Tre opzioni valutate:
- **Brute force** (testa tutti i triangoli): esatto ma O(#punti × #triangoli) → troppo lento sul cloud
  pieno (~1.1 M punti × migliaia di triangoli = miliardi di test).
- **k-NN sui centroidi dei triangoli** (kd-tree, testa i 16 triangoli con centroide più vicino):
  veloce, **ma SBAGLIATO sulle nostre mesh**. Le facce piane della trave producono **pochi triangoli
  grandi**; il punto può stare su un triangolo grande il cui *centroide* è lontano, mentre i 16
  centroidi più vicini appartengono ad altri triangoli → distanza errata (misurato: fino a 168 mm di
  errore, ~65% dei punti). Il kd-tree in sé era corretto (0 errori su punti casuali): a fallire è
  l'**euristica del centroide** su mesh grossolane.
- **BVH (AABB tree) con closest-point branch-and-bound** ← **scelto**. Esatto come il brute force
  (verificato: 0 differenze), ma pota per distanza-box e visita prima il figlio più vicino →
  ~O(log n) per query. Robusto anche con triangoli grandi o mesh fini.

### 5.4 Registrazione — **point-to-plane vs point-to-point ICP**
**Scelto:** ICP **point-to-plane** (minimizza la distanza al *piano tangente* del nominale).
**Perché:** nel fitting di una nuvola su una superficie CAD converge in **molte meno iterazioni** e
permette ai punti di **scorrere lungo la superficie** (il point-to-point li "incolla" alla
corrispondenza puntuale e rallenta). Inoltre il point-to-point richiede l'allineamento ottimale via
**SVD/quaternioni** (decomposizione 3×3/4×4); il point-to-plane si risolve con un **sistema lineare
6×6** che sappiamo risolvere con Cholesky (vedi 5.6) senza dipendere da una SVD.

### 5.5 Stabilità ICP — bracci di leva **relativi al baricentro**
**Problema osservato:** con i punti lontani dall'origine (la trave occupa z∈[0,1000]), il passo di
Gauss-Newton con rotazione attorno all'**origine** ha bracci di leva enormi (`p×n` ~ 500 mm) →
**overshoot e divergenza** (RMS schizzava a 54 mm).
**Scelto:** formulare la rotazione attorno al **baricentro** dei punti correnti (`(p−centroide)×n`) e
ricomporre l'incremento come trasformazione mondo. Condiziona bene il sistema → convergenza stabile
(RMS 1.9 mm → 0.006 mm in 2 iterazioni nei test).

### 5.6 Solver lineare — **Cholesky vs LU/QR/SVD**
**Scelto:** **Cholesky** sul sistema normale 6×6 (simmetrico definito-positivo).
**Perché:** è il più veloce e semplice per SPD, ~40 righe senza dipendenze; se la matrice non è
definita-positiva (configurazione degenere) `Solve` ritorna `false` e l'ICP si ferma invece di produrre
un passo spazzatura. Non serve la generalità (e il costo) di QR/SVD.

### 5.7 Incremento di rotazione — **Rodrigues vs piccolo-angolo**
**Scelto:** ricostruire la rotazione dall'asse-angolo con **Rodrigues**.
**Perché:** resta una rotazione **propria** (ortonormale) per qualunque ampiezza dell'incremento,
evitando la deriva/non-ortogonalità dell'approssimazione `I+[ω]×` quando il passo non è minuscolo.

### 5.8 Matematica nei loop — **`Vec3` struct vs `Point3D`/`Vector3D`**
**Scelto:** un `Vec3` *value type* interno per il calcolo intensivo.
**Perché:** `Point3D`/`Vector3D` di Eyeshot sono *reference type*; usarli su milioni di punti × molte
iterazioni genererebbe pressione sul GC. Si converte da/verso i tipi Eyeshot solo ai bordi pubblici.

### 5.9 Quanti punti per l'ICP — **sotto-campionamento**
**Scelto:** ICP usa un sottoinsieme casuale (default ≤ 20000 punti, seeded; Fisher-Yates parziale).
**Perché:** la trasformazione rigida ha 6 gradi di libertà; poche migliaia di punti ben distribuiti la
determinano in modo stabile e **molto** più velocemente del cloud intero. La misura finale (Step 4)
userà invece tutti i punti.

### 5.10 Misura distanze — **`ComputeDistances` Eyeshot vs implementazione propria**
La licenza è **Ultimate**, quindi `ComputeDistances` (nuvola↔Brep, headless) è disponibile e sarà il
riferimento/colore per lo Step 4. **Ma** restituisce solo la distanza scalare per punto (niente punto
più vicino/normale) → **non basta** per le corrispondenze dell'ICP. Perciò lo Step 3 usa la nostra
`NominalSurface`, che dà punto+normale+distanza ed è indipendente dalla licenza (e fornisce le distanze
della mappa "gratis").

### 5.11 STEP via **Block/BlockReference**
Il nostro `BrepExporter` scrive il Brep dentro un `Block` con una `BlockReference` (come fa RobServer).
Perciò in lettura il solido non è in `Entities` ma nel blocco riferito: `BrepImporter` **risolve** il
riferimento (`BlockReference.GetEntities`) e applica la posa. (Confermato dal supporto Eyeshot.)

### 5.12 Viewport Eyeshot creato da codice
Un controllo `Design` costruito a mano ha `Viewports` **vuota**; al primo paint crasha
(`ArgumentOutOfRangeException` in `AdjustNearAndFarPlanes`). **Scelto:** aggiungere un `Viewport`
esplicito e dimensionato in `Viewports` (come serializza il designer) + guardia in `OnLoad`.

### 5.13 Misura — distanza **con segno** vs scalare
**Scelto:** per ogni punto, distanza **con segno** = `sign((p − puntoVicino) · normale) · |distanza|`.
**Perché:** in tolleranza conta *da che lato* della superficie sta il punto: **positivo** = materiale in
eccesso (fuori dal nominale, lungo la normale uscente), **negativo** = materiale mancante (dentro). Uno
scalare unsigned (come `ComputeDistances`) non distingue sovrametallo da sottometallo, distinzione
essenziale per il verdetto e per una mappa di deviazione leggibile. La nostra `NominalSurface` fornisce
punto+normale, quindi il segno è gratis.

### 5.14 Statistiche — RMS, media segnata, **P95** robusto
**Scelto:** oltre a min/max/media/RMS/devstd, il **95° percentile** dei valori assoluti.
**Perché:** il `MaxAbs` da solo è dominato da pochi outlier (un punto sporco di scansione). Il P95 dà un
"caso peggiore" stabile e rappresentativo. La media **segnata** evidenzia un bias sistematico (es.
allineamento non perfetto o sovrametallo uniforme) che l'RMS, sempre positivo, nasconde.

### 5.15 Verdetto di conformità — banda di tolleranza **segnata**
**Scelto:** `ToleranceBand(LowerMm, UpperMm)` (simmetrica `±t` o asimmetrica) con conteggio dei punti
dentro/fuori, `ConformanceRatio` e `IsConform` (tutti i punti dentro). **Perché:** un singolo numero
(pass/fail) non basta; il rapporto di conformità e gli istogrammi guidano l'accettazione e la diagnosi.

### 5.16 Mappa colore — **`Legend` Eyeshot** riusata
**Scelto:** colorare i punti con la palette `Legend.RedToBlue9` mappando la deviazione *segnata* sulla
banda `[−t, +t]` e mostrando la `Legend` del viewport. **Perché:** è l'idioma del sample Eyeshot
`ComputeDistance` (coerenza con la libreria), la barra-legenda è disegnata da Eyeshot tra `Min`/`Max` con
bin uniformi → la nostra binnatura uniforme combacia con la barra. La nuvola colorata è un
`PointCloud` *Multicolor* (`PointRGB` per punto), mentre la nuvola monocroma resta un `FastPointCloud`.

**Aggancio della legenda (control costruito in codice).** Il sample `ComputeDistance` usa
`design1.Legends[0]`, che esiste perché il control è creato dal *designer* WinForms. Il nostro `Design` è
costruito in codice, quindi — esattamente come per `Viewports` (vedi 5.12) — la collezione di legende parte
**vuota** e la mappa colore non avrebbe alcuna chiave visibile. Inoltre in questa build di Eyeshot
(2025.3.437) `Workspace.Legends` è **sola lettura**: l'array assegnabile è `Viewport.Legends`. Perciò
`MainForm.EnsureLegend()` crea una `Legend` *on-demand* e la aggancia al viewport
(`Viewports[0].Legends = new[]{ legend }`), nascosta finché non si misura. I default di `new Legend()` sono
adeguati (posizione 24,24; dimensione auto dagli item; `FormatString` di default `{0:+0.###;-0.###;0}` che
mostra già i valori **con segno**, coerente con la convenzione + esterno / − interno), quindi si impostano
solo `Items`, range (`SetRange`), `Title`/`Subtitle` e `Visible`.

### 5.17 Segmentazione per feature — **cutter ri-derivati vs riconoscimento topologico (AFR)**
**Scelto (Step 5.1):** segmentare la nuvola ri-derivando in modo deterministico i **solidi-utensile**
(cutter) di ogni feature dal `PieceSpec` (stesso codice della generazione, `BeamFactory.BuildMachined`),
tassellarli e costruire **un** `TriangleBvh` etichettato per triangolo; un punto entro `onSurfaceTol` dal
cutter più vicino appartiene a quella feature, gli altri sono *base*. **Perché:** il bordo del cutter
**coincide** con la superficie lavorata della feature, quindi è un primitivo di segmentazione esatto e
generale (foro e scasso allo stesso modo) e riusa il BVH già impiegato da ICP e misura. Il
riconoscimento feature "alla cieca" (AFR topologico) è stato **scartato** dall'utente per questo step: gli
input (macro + parametri nominali + Brep nominale + nuvola) sono noti, non serve indovinare.

### 5.18 Verifica dimensionale — **fit al minimo numero di incognite sfruttando la geometria nota**
Principio comune a 5.2 e 5.3: poiché orientamento/asse di ogni primitivo si ricavano **esatti** dal
cutter ri-derivato (e la nuvola è già allineata da ICP), ogni fit si riduce alla sua **incognita minima**,
più robusto ed economico delle macchine generali (RANSAC/PCA/cilindro non lineare) che servono quando
l'orientamento è ignoto. Dettaglio della ricerca in `docs/research/notch-parameter-extraction.md`.

- **Foro (5.2):** l'asse del cilindro è la normale di una faccia-tappo piana del cutter ⇒ noto. I punti
  del bucket foro si proiettano sul piano ⟂ all'asse e si fa un **fit di cerchio 2D algebrico (Kåsa)**,
  lineare (riusa `Cholesky`), Ø = 2·r. Ø nominale dai **parametri macro** (INTC `C`, fallback `F`). Niente
  fit di cilindro non lineare. Osservato: foro pulito Ø 40.04 (nom. 40), maggiorato +0.5 ⇒ Ø 41.00 esatto.

- **Scasso (5.3):** lo scasso è un **contorno 2D estruso** ⇒ pareti piane + un raccordo cilindrico
  ("fit piani + arco"). Tre scelte:
  - *Pareti (lunghezza/profondità)* — **fit di offset a normale nota** (A3): la normale viene dalla
    faccia del cutter, quindi resta una sola incognita, l'offset, stimato come **mediana** robusta di
    `n̂·p` (no eigen-solver, no RANSAC, robusto agli outlier per costruzione). Lunghezza = parete *back*
    (≈`A`), profondità = parete *depth* (≈`B`); le due pareti si identificano confrontando l'offset
    nominale del cutter con i valori macro.
  - *Raccordo (raggio)* — un fit di cerchio **libero** è mal condizionato sul quarto d'arco del raccordo
    (Kåsa dà R≈9.66, una raffinazione geometrica diverge a R≈11.05). Si sfrutta invece la **tangenza**:
    il raccordo è tangente alle due pareti già misurate, quindi il centro è vincolato a
    `corner + R·(û_back+û_depth)` e resta **una sola incognita** R, trovata per *golden-section* su
    `Σ(‖p−centro(R)‖−R)²`. Osservato: R≈9.95 (dev −0.05 mm), un ordine di grandezza meglio del fit libero.
    (Generalizzazione per spigoli non a 90°: centro sulla **bisettrice** a distanza `R/sin(θ/2)`.)
  - *Routing dei punti* — ogni punto va alla **faccia nominale più vicina**: entro una banda dal piano
    di una parete → quella parete; residuo vicino allo spigolo e fuori da entrambe le pareti → raccordo.

- **Limite noto:** lunghezza/profondità sono misurate come **posizioni nel frame allineato**, quindi
  ereditano l'accuratezza della registrazione (il raggio è intrinseco). Coerente con 5.2; l'upgrade
  invariante all'allineamento (distanze *feature-relative*: parete→fine trave, parete→bordo anima) è
  documentato nella nota di ricerca.

### 5.19 Pipeline end-to-end — **un orchestratore in Core, riusato da CLI e GUI** (Step 5.4)
**Scelto:** un solo `PieceInspector` (in `Core/Features`) che incatena tutta la pipeline QC — nominale →
tassellazione → ICP → segmentazione → misura per-feature → verdetto dimensionale — e ritorna un
`PieceInspectionReport`. I cutter vengono **raggruppati per macro** (una macro può espandersi in più
cutter: lo scasso demo in 4): ogni cutter-foro è misurato singolarmente (la sua sede), mentre i cutter di
uno scasso sono ispezionati insieme (un solo contorno, il cutter-profilo porta il raccordo). Un
`InspectionReportFormatter` rende il report come tabella testuale.
**Perché:** la stessa logica di orchestrazione serviva sia alla **demo headless** (comando `inspect`) sia
alla GUI; metterla in Core evita duplicazione e tiene i frontend sottili (la CLI/GUI solo parsing e
presentazione). Riusa pari pari i mattoni di 5.1–5.3 (`FeatureSegmentation`, `FeatureMeasurement`,
`HoleInspection`, `NotchInspection`). Verdetto demo: SCAI01 len 80.00 / dep 60.00 / R 9.95, INTC01 Ø 40.04
— tutto in banda ±0.5.

### 5.20 App a **doppia modalità** e **generazione scan da nominale** in GUI (Step 5.4)
**Scelto:** un **unico eseguibile** `...App` che, a seconda degli argomenti, lancia la **GUI** (nessun
argomento) oppure la **demo headless** (`--headless` / `inspect …`, stessa pipeline `PieceInspector`).
Essendo un `WinExe`, in modalità headless si **aggancia la console** del terminale chiamante
(`AttachConsole`, fallback `AllocConsole`) per stampare il verdetto. Inoltre la GUI può **generare lo scan
dal Brep nominale** già importato, con **densità / σ / seed** impostabili da toolbar (bottone *Genera scan
da nominale*), riusando `ScanGenerator.Sample` (nuovo overload **in-memory**, senza scrittura file).
**Perché:** un solo binario per entrambe le demo (richiesta utente) riduce attrito d'uso; generare lo scan
in-app evita il giro su file `generate`→`Carica scan` e permette di esplorare interattivamente l'effetto
di rumore/densità sulla misura sulla stessa geometria caricata.

### 5.21 **Pannello feature** in GUI: lista feature + parametri nominali/misurati (Step 5.4)
**Scelto:** un pannello a sinistra del viewport (due `ListView` in `SplitContainer`): in alto la **lista
delle feature** del pezzo, in basso la **tabella parametri** della feature selezionata (Nominale / Misurato
/ Deviazione / Tolleranza / Esito). Le feature compaiono **al caricamento dello STEP**: la GUI cerca accanto
allo STEP il file macro con **lo stesso nome base** (`<base>.macros.json`, poi `<base>.json`); se lo trova
ne ricostruisce il `MachinedBeam` (`BeamFactory.BuildMachined`) e mostra i **nominali** via
`PieceInspector.DescribeNominal` (nessuno scan necessario); se **non lo trova avvisa con un warning** senza
crashare (la mappa di deviazione resta comunque disponibile). Dopo *Misura*, la GUI chiama l'**overload di
`PieceInspector.Inspect` con la trasformata** già calcolata da *Allinea* (così misura feature e mappa colore
condividono la stessa registrazione), e la tabella mostra **nominale a fianco del misurato** con l'esito ±toll.
Il match nominale↔misurato avviene per `FeatureDescriptor.Id`.
**Perché:** porta in GUI le stesse informazioni della demo headless (`inspect`) in forma navigabile;
ricostruire le feature dalle macro è obbligato (lo STEP da solo non contiene i cutter — vedi §5.1/§5.19);
legare il JSON al nome dello STEP rende il flusso "carica e basta"; il fallback con warning evita crash su
STEP senza macro. `DescribeNominal` e l'overload con trasformata sono nati in Core per non duplicare il
raggruppamento per macro né l'ICP nella UI.

---

## 6. Riferimento **classi e funzioni**

> Convenzione: per ogni classe, l'elenco dei metodi pubblici/chiave con cosa fanno. I tipi
> `record (struct)` espongono le proprietà indicate dal costruttore.

### 6.1 `Model/` — descrizione del pezzo
- **`BeamSpec`** — geometria del profilo (es. `Ipe300(length)`): altezza, larghezza, spessori, lunghezza.
- **`MacroSpec`** — una lavorazione (classe macro es. `SCAI01`/`INTC01`, lato, versi, parametri `With(k,v)`).
- **`PieceSpec`** — `BeamSpec` + lista di `MacroSpec`: il pezzo nominale completo.
- **`PieceSpecSerializer`** — `Write(path, piece)` / lettura JSON del `PieceSpec`.

### 6.2 `Generation/` — costruzione nominale e scansione sintetica
- **`BeamFactory`**
  - `BuildRaw(beam)` → `Brep` della trave grezza (estrusione del profilo).
  - `BuildMachined(piece)` → `MachinedBeam` (Brep lavorato: sottrae le feature delle macro) + traccia.
  - `BrepTolerance` → tolleranza di costruzione/tassellazione del Brep.
- **`MachinedBeam`** — risultato della lavorazione: `Solid` (Brep) + `Trace` (log delle macro applicate).
- **`GenerationOptions`** (`record`) — `DensityPerMm2`, `SigmaMm`, `Seed`; `Default`/`DefaultDensityPerMm2`.
- **`ScanResult`** (`record struct`) — `TriangleCount`, `PointCount`.
- **`ScanGenerator`** — orchestratore della pipeline Step 1.
  - `Generate(brep, options, seedOffset, plyPath, stepPath)` → tassella il Brep, campiona il cloud
    pulito, applica rumore, scrive `*.ply` (rumoroso) + `*_clean.ply` (pulito) + STEP nominale; ritorna
    i conteggi. I seed per-stadio derivano dal seed master (riproducibilità per pezzo).

### 6.3 `Sampling/` — da Brep a punti
- **`BrepTessellator`** (static)
  - `ToMesh(brep, chordDeviation, angleDeviation=0)` → `Mesh` (`Brep.ConvertToMesh`,
    `natureType.Plain`): tolleranza di corda in mm, una normale per triangolo.
- **`SurfaceSample`** (`record struct`) — `Position` (Point3D) + `Normal` (Vector3D unitaria).
- **`IPointSampler`** — `Sample(mesh)` → lista di `SurfaceSample`.
- **`MeshSurfaceSampler`** (`IPointSampler`) — campionamento uniforme pesato sull'area (vedi 5.1).
  - costruttore `(densityPerMm2, seed?)`.
  - `Sample(mesh)`: precalcola normali e tabella cumulata delle aree, estrae N≈densità·areaTotale punti.
  - *(interni)* `PickTriangle` (ricerca binaria sulla cumulata), `PointInTriangle` (√-trick baricentrico).

### 6.4 `Noise/` — disturbo di misura
- **`GaussianRangeNoise`**
  - costruttore `(sigmaMm, seed?)`.
  - `Apply(samples)` → nuova lista con ogni `Position` spostata di `N(0,σ)` lungo la sua `Normal`
    (Box-Muller per le gaussiane).

### 6.5 `Io/` — lettura/scrittura
- **`IPointCloudWriter` / `PlyWriter`** — `Write(path, samples)` / `WriteTo(textWriter, samples)`: PLY
  ASCII, cultura invariante, posizione a 4 decimali e normali a 6.
- **`IPointCloudReader` / `PlyReader`** — `Read(path)` / `ReadFrom(textReader)`: parsa l'header, mappa
  x/y/z (obblig.) e nx/ny/nz (opz.) **per nome**, tollera ordine/colonne extra, default normale
  `AxisZ`, rifiuta il PLY binario, `FormatException` su header errato o corpo troncato.
- **`BrepExporter`** — `Export(path, brep)`: scrive il Brep in STEP (`WriteSTEP`) dentro `Block "part"`
  + `BlockReference`.
- **`BrepImporter`** — `Import(path)` → `IReadOnlyList<Brep>`: `ReadSTEP.DoWork`, poi
  `HarvestBreps(...)` (privata) appiattisce ricorsivamente le entità risolvendo ogni `BlockReference`
  (`GetEntities`) e applicando la posa (`GetFullTransformation`); clona solo se la posa non è identità;
  fallback sulle definizioni dei blocchi se `Entities` è vuoto.

### 6.6 `Registration/` — closest-point, ICP, algebra (Step 3, base dello Step 4)

**`Vec3`** (struct interno) — vettore 3D senza allocazioni: `+ - *`, `Dot`, `Cross`, `Length`,
`LengthSquared`, `Normalized`.

**`PointTriangleDistance`** (static interno)
- `ClosestPoint(p, a, b, c)` → punto del triangolo più vicino a `p` (regioni di Voronoi di Ericson:
  vertice/spigolo/faccia), esatto e senza allocazioni.

**`TriangleBvh`** (interno) — BVH di AABB sui triangoli per query closest-point esatta.
- `Build(a, b, c)` → costruisce l'albero (split sull'asse più lungo dei centroidi, foglie ≤ 4 tri).
- `ClosestTriangle(q, out closestPoint)` → indice del triangolo più vicino + punto, via
  branch-and-bound (figlio più vicino prima, potatura per `BoxDistSq` ≥ best).
- *(interni)* `BuildNode`, `Search`, `BoxDistSq`, `Partition`, `ComputeBounds`, `LongestCentroidAxis`.

**`SurfaceProjection`** (`record struct`) — `Point`, `Normal`, `Distance` (esito di una proiezione).

**`NominalSurface`** — superficie nominale interrogabile (mesh + BVH). Primitiva condivisa ICP/misura.
- `FromMesh(mesh)` / `FromMeshes(meshes)` → costruiscono gli array per-triangolo (vertici, normale
  geometrica) e il BVH; `FromMeshes` aggrega più solidi (STEP multi-solido) in un unico BVH.
- `ClosestPoint(Point3D)` / `ClosestPoint(Vec3)` → `SurfaceProjection` (punto, normale, distanza).
- *(test)* `ClosestPointBruteForce(q)` → versione di riferimento che scandisce tutti i triangoli.

**`RigidTransform`** (struct) — rototraslazione (rotazione 3×3 + traslazione).
- `Identity`, `FromTranslation(tx,ty,tz)`, *(interno)* `FromRotationVector(wx,wy,wz,tx,ty,tz)` (Rodrigues).
- `Apply(Point3D)` / `Apply(Vec3)` → trasforma un punto.
- `Compose(inner)` → `this ∘ inner` (applica `inner`, poi `this`): accumula gli incrementi ICP.
- `ToTransformation()` → `Transformation` Eyeshot (layout matrice [riga,colonna] validato da test).

**`Cholesky`** (static interno)
- `Solve(A, b, x, n)` → risolve il sistema SPD `A x = b`; ritorna `false` se `A` non è definita-positiva.

**`IcpOptions`** (`record`) — `MaxIterations` (60), `ConvergenceTranslationMm` (1e-5),
`ConvergenceRotationRad` (1e-6), `MaxSourcePoints` (20000), `Seed` (12345), `MaxPairDistanceMm` (∞, gate
outlier).

**`RegistrationResult`** (`record struct`) — `Transform`, `Iterations`, `RmsErrorMm`, `Converged`.

**`IcpRegistration`** — ICP point-to-plane (vedi 5.4–5.7).
- `Register(source, target, options?)` → `RegistrationResult`. Per iterazione:
  *pass 1* proietta i punti (cache corrispondenze) e calcola il baricentro;
  *pass 2* costruisce il sistema normale 6×6 (riga `[(p−centroide)×n, n]`, residuo `n·(q−p)`);
  risolve (Cholesky), costruisce l'incremento (rotazione attorno al baricentro), compone, verifica la
  convergenza. Alla fine valuta l'RMS punto-superficie.
- *(interni)* `EvaluateRms`, `Subsample` (Fisher-Yates parziale seeded).

### 6.7 `Measurement/` — misura tolleranze e mappa di deviazione (Step 4)

**`ToleranceBand`** (`record struct`) — banda di accettazione segnata `[LowerMm, UpperMm]`.
- `Symmetric(halfWidthMm)` → `±t`; `Contains(signedDeviationMm)` → dentro la banda?

**`PointDeviation`** (`record struct`) — `Point` (posizione allineata) + `SignedDistanceMm` (segno: + esterno).

**`DeviationStatistics`** (`record struct`) — `Count`, `MinMm`, `MaxMm`, `MeanMm` (segnati), `StdDevMm`,
`RmsMm`, `MeanAbsMm`, `MaxAbsMm`, `P95AbsMm`.
- `Compute(signedDeviations)` → calcola tutto in una passata; il P95 su copia ordinata degli `|d|`
  (interpolazione lineare tra ranghi). `Empty` per zero punti.

**`DeviationReport`** — esito del confronto nuvola↔nominale.
- `Deviations`, `Statistics`, `Tolerance?`, `InToleranceCount`, `OutOfToleranceCount`,
  `ConformanceRatio` (∈[0,1]), `IsConform` (banda presente e tutti i punti dentro).

**`DeviationMeasurement`** — motore di misura (usa `NominalSurface`, vedi 5.10/5.13).
- `Measure(scan, nominal, alignment?, tolerance?)` → `DeviationReport`. Per ogni punto: applica
  l'`alignment` (ICP), proietta sul nominale, calcola la distanza **con segno** dal lato della normale,
  accumula le statistiche e il conteggio dentro/fuori banda.

### 6.8 `Features/` — segmentazione e verifica dimensionale (Step 5)

**Identità e cutter**
- **`FeatureKind`** (enum `Base/Hole/Notch/Other`) + **`FeatureKinds.FromMacroClassName`** (per prefisso:
  `INTC*`→Hole, `SCAI*`→Notch).
- **`FeatureDescriptor`** (`record struct`) — identità feature: `Id` (1-based; 0 = `Base`), `MacroIndex`,
  `MacroClassName`, `Kind`, `Label`; statico `Base`.
- **`FeatureCutter`** (`record`) — `Descriptor` + `Brep Cutter` (il solido-utensile ri-derivato).

**Segmentazione (5.1)**
- **`FeatureSegmentation`** — `FromCutters(cutters, onSurfaceTol=1.0, chordTol=0.2)`: tassella ogni cutter,
  costruisce **un** `TriangleBvh` etichettato per triangolo; `Classify(Point3D)→int` = indice in `Features`,
  oppure −1 = base (distanza al cutter più vicino > `onSurfaceTol`). Vedi 5.17.
- **`FeatureDeviation`** (`record struct`) — `(FeatureDescriptor Feature, DeviationReport Report)`.
- **`SegmentedDeviationReport`** — `Overall` (tutta la nuvola) + `Base` (corpo) + `Features` (un report per
  feature); i bucket **partizionano** la nuvola. `Features` è **parallelo per indice** a `MachinedBeam.Features`.
- **`FeatureMeasurement`** — `Measure(scan, nominal, segmentation, alignment?, tolerance?)` →
  `SegmentedDeviationReport`. Riusa il core di proiezione di `DeviationMeasurement`, poi instrada ogni
  deviazione nel bucket della sua feature.

**Verdetto dimensionale — comune (5.2/5.3)**
- **`FeatureParameter`** (`record struct`) — un parametro: `Name`, `NominalMm`, `MeasuredMm`, `ToleranceMm`,
  `InTolerance`; `DeviationMm`; factory `Judge(name, nominal, measured, tol)` (banda simmetrica).
- **`FeatureInspectionReport`** (`record`) — `Feature` + `Parameters` + `PointCount` + `InTolerance`
  (tutti i parametri in banda).

**Foro (5.2)**
- **`CircleFit`** (`internal`) — fit di cerchio **Kåsa** (`u²+v²+Du+Ev+F=0`), risolve la 3×3 SPD riusando
  `Registration/Cholesky`; ritorna `CircleFitResult(CenterU, CenterV, Radius)`.
- **`CutterAxis`** (`internal`) — `CylinderAxis(Point, Direction)` + `FromCutter(Brep)` (asse dalla
  faccia-tappo piana).
- **`HoleInspection`** — `Inspect(hole, scanPoints, nominalDiameterMm, tol=0.5)`: proietta i punti sul piano
  ⟂ all'asse noto, fit Kåsa, Ø=2·r; + static `NominalDiameterFromMacro` (INTC `C`, fallback `F`).

**Scasso (5.3)**
- **`PlaneFit`** (`internal`) — `Median` + `RobustOffset(normal, points)` = mediana di `n̂·p` (fit di offset
  a normale nota).
- **`ExtrudedProfile`** (`internal`) — deriva dal cutter-profilo l'asse di estrusione, la base profilo
  (`Origin/U/V`), le pareti `WallLine` (normale/offset mondo + retta in coord. profilo), le pareti
  *back*/*depth* e lo spigolo; `HasFillet(Brep)` (qualche faccia non-piana), `FromCutter(Brep, NotchNominals)`.
- **`NotchInspection`** — `Inspect(notchCutters, scanPoints, NotchNominals, NotchTolerance?, wallBandMm)`:
  seleziona il cutter-profilo (`HasFillet`), misura *Length*/*Depth* (offset robusto) e *Radius* (fit
  tangente a 1 incognita, golden-section); + static `NominalsFromMacro` (A/B/R). `NotchNominals` /
  `NotchTolerance` `record struct`.

**Orchestrazione end-to-end (5.4)**
- **`InspectionOptions`** (`record`) — `Align`, `OnSurfaceToleranceMm`, `HoleToleranceMm`, `NotchTolerance`.
- **`PieceInspectionReport`** (`record`) — `Aligned`, `Alignment` (`RegistrationResult`), `Deviation`
  (`SegmentedDeviationReport`), `Features` (un `FeatureInspectionReport` per feature), `InTolerance`.
- **`PieceInspector`** — orchestratore. Tre API pubbliche (raggruppamento per macro condiviso, `GroupByMacro`):
  - `Inspect(machined, macros, scan, brepTol, options?)`: tassella il nominale → `NominalSurface`,
    ICP (se `Align`), `FeatureSegmentation`+`FeatureMeasurement`, poi `HoleInspection`/`NotchInspection`. Vedi 5.19.
  - `Inspect(…, RigidTransform alignment, options?)`: **overload** che usa una trasformata già calcolata dal
    chiamante (salta l'ICP), così la GUI condivide la registrazione di *Allinea* tra mappa colore e misura feature. Vedi 5.21.
  - `DescribeNominal(machined, macros, options?)`: **solo nominali, senza scan** — un `FeatureInspectionReport`
    per feature con `MeasuredMm = NaN` e lo **stesso `FeatureDescriptor.Id`** che produrrebbe `Inspect`
    (così GUI può fare il match nominale↔misurato). Vedi 5.21.
- **`InspectionReportFormatter`** — `Format(report)`/`FormatText(report)`: rende il verdetto come tabella
  testuale (allineamento, riga per parametro nominale/misurato/dev/tol/PASS-FAIL, esito complessivo).

### 6.9 Frontend
- **`Generator/Program`** — CLI a due comandi:
  - `generate --out <dir> [--density N] [--sigma S] [--seed K]` — scrive `grezzo.*`, `lavorato.*`,
    `lavorato.macros.json`.
  - `inspect --demo [--density N] [--seed K] [--no-align] [--tol M]` **oppure** `inspect --macros <f>
    --scan <f> […]` — esegue `PieceInspector` e stampa il verdetto (`InspectionReportFormatter`); exit ≠ 0
    se il pezzo non è conforme. `--demo` è autosufficiente (ricampiona una nuvola pulita del pezzo demo).
- **`App/Program`** — `[STAThread] Main(args)` a **doppia modalità**: senza argomenti avvia la GUI
  (`Application.Run(new MainForm())`); con `--headless`/`inspect` aggancia la console (`AttachConsole`,
  fallback `AllocConsole`) e delega a `HeadlessInspection.Run` (stessa pipeline `PieceInspector`). Vedi 5.20.
- **`App/HeadlessInspection`** — runner headless dell'App: parsing argomenti (`--demo` / `--macros`+`--scan`,
  `--no-align`, `--tol`, `--density`, `--seed`), costruzione del pezzo, `PieceInspector`, stampa col
  formatter.
- **`App/MainForm`** — viewport Eyeshot (`Design`) + **pannello feature** a sinistra (`SplitContainer`:
  lista feature `_featureList` sopra, tabella parametri `_paramGrid` sotto) + pipeline QC
  (Genera/Allinea/Misura). Mantiene lo stato: Breps nominali, campioni scan, `NominalSurface` (cache),
  `alignment` corrente, entità nuvola mostrata, e — quando lo STEP ha un file macro — `MachinedBeam`,
  lista macro, feature nominali (`DescribeNominal`) e dizionario delle feature misurate per `FeatureDescriptor.Id`.
  - costruttore: init del controllo con `BeginInit`/`Viewport` esplicito/`EndInit`; il viewport vive in
    `Panel2` di un `SplitContainer` (verticale), il pannello feature in `Panel1`; toolbar (campi *dens*,
    *σmm*, *seed*, *Toll. ±mm*) e statusbar.
  - `OnLoad` → guardia `InitializeViewports` se `Viewports` vuota.
  - `LoadNominal()` → `BrepImporter` → Breps grigi (memorizzati, invalida la `NominalSurface` cache); poi
    `TryLoadFeatures(stepPath)`.
  - `TryLoadFeatures` / `FindMacrosFile` → cerca `<base>.macros.json` poi `<base>.json` accanto allo STEP;
    se trovato ricostruisce il `MachinedBeam` e popola la lista feature coi nominali (`DescribeNominal`);
    se assente/illeggibile **mostra un warning** (`MessageBox`) senza crashare. Vedi 5.21.
  - `LoadScan()` → `PlyReader` → `FastPointCloud` blu; azzera l'`alignment`; `ClearMeasuredFeatures`.
  - `GenerateScanFromNominal()` → **genera lo scan dal Brep nominale** con `ScanGenerator.Sample`
    (densità/σ/seed da toolbar, `ParseGenerationOptions`); lo imposta come scan corrente; `ClearMeasuredFeatures`. Vedi 5.20.
  - `RunAlign()` → `IcpRegistration.Register`; memorizza la trasformazione, mostra la nuvola allineata,
    riporta RMS/iterazioni in statusbar.
  - `RunMeasure()` → `DeviationMeasurement.Measure` con `alignment` e banda; colora la nuvola
    (`PointCloud` Multicolor + `Legend`), scrive verdetto + statistiche in statusbar; poi
    `MeasureFeatures(scan)` → `PieceInspector.Inspect(…, alignment, …)` riempie i parametri misurati nella
    tabella (fallimento non fatale → nota in statusbar). Vedi 5.21.
  - *(pannello feature)* `BuildInspectionOptions`, `ResetFeatureInspection`, `ClearMeasuredFeatures`,
    `PopulateFeatureList`, `RefreshFeatureList`, `RenderSelectedFeature` (match nominale↔misurato per nome
    parametro), helper `Fmt`/`FmtSigned`.
  - *(interni)* `EnsureNominalSurface` (tassella i Breps, chord 0.2 mm, `FromMeshes`),
    `BuildFastCloud`, `BuildColouredCloud` (mappa deviazione→`Legend.RedToBlue9`),
    `EnsureLegend` (crea e aggancia la `Legend` al viewport alla prima misura, vedi 5.16) /
    `HideLegend` (la nasconde quando si carica/genera/allinea/svuota), `ShowCloud`
    (sostituisce l'entità nuvola), `ClearScene()`, `RunGuarded(action)` (errori con MessageBox).

---

## 7. Test (xUnit, 58 verdi)

- **Generazione/IO:** `ScanGeneratorTests`, `PlyWriterTests`, `PlyReaderTests`, `BrepImporterTests`
  (round-trip del solido "bloccato"), `MeshSurfaceSamplerTests`, `GaussianRangeNoiseTests`,
  `BeamFactoryTests`, `PieceSpecSerializerTests`.
- **Registrazione (`RegistrationTests`):** closest-point su punto in superficie (≈0) e con offset noto
  lungo la normale; layout matrice di `ToTransformation`; **recupero di un disallineamento rigido noto**
  (RMS ≈ 0 dopo l'ICP).
- **Misura (`MeasurementTests`):** scan perfetto → conforme e RMS≈0; offset lungo la normale → deviazione
  **positiva** ≈ offset; offset all'interno → deviazione **negativa**; banda stretta su offset uniforme →
  non conforme (ratio 0); l'`alignment` ICP è applicato prima di misurare (RMS da grande a ≈0); statistiche
  (`Compute`) confrontate con valori calcolati a mano.
- **Feature (Step 5):** `FeatureSegmentationTests` (cutter esposti/taggati, instradamento foro/scasso,
  partizione esatta, grezzo→tutto base); `HoleInspectionTests` (Ø nominale da macro, recupero cerchio noto,
  foro pulito Ø≈40.04, maggiorato +0.5 → rifiutato); `NotchInspectionTests` (nominali A/B/R, scasso pulito
  len 80.00/dep 60.00/R 9.95, banda raggio stretta rifiuta); `PieceInspectionTests` (orchestratore:
  demo → 1 scasso + 1 foro in banda, modalità `--no-align`; **`DescribeNominal`** elenca scasso+foro coi
  nominali e `MeasuredMm = NaN`, con gli stessi `FeatureDescriptor.Id` di `Inspect`; **overload con
  trasformata** identità misura come `--no-align`).

Eyeshot gira headless anche nei test (translator STEP, tassellazione): la licenza lo consente.

---

## 8. Prossimi passi (Step F)

1. **Report di conformità** esportabile (CSV/PDF): statistiche, istogramma delle deviazioni, verdetto.
2. **Segmentazione per feature/macro**: misurare separatamente fori, scassi, facce → tolleranze per feature.
3. **GD&T**: planarità, perpendicolarità, posizione vere rispetto a datum, non solo distanza punto-superficie.
4. **Scanner reale**: sostituire la scansione sintetica con l'acquisizione da camera/scanner 3D.
5. (Opz.) Variante `ComputeDistances` (Ultimate) come cross-check unsigned della mappa (vedi 5.10).
