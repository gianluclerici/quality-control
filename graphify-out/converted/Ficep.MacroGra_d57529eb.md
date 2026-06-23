<!-- converted from Ficep.MacroGra.docx -->

STORIA DELLE REVISIONI


# Libreria Ficep.MacroGra
- Ficep.MacroGra è una libreria C# utilizzata all’interno degli applicativi Ficep per il calcolo della descrizione grafica 3d delle features parametriche di taglio denominate MACRO

- La libreria è implementata come DLL .NETFramework 4.7.2
- La creazione dei modelli 3d si basa sulla sottrazione di volumi a partire dal profilo estruso della barra grezza
- Il progetto consta di una parte “kernel” (che comprende il motore grafico e un gruppo di funzioni ausiliarie costruite su di esso) e di una parte “feature” (che comprende la definizione delle funzioni atte alla rappresentazione delle features).
- Le features sono fori, aperture di varia forma, lavorazioni sulle estremità dei profili, per un totale di circa 300 diverse figure parametriche applicate ai vari profili di carpenteria (profili I, U, L, tubi tondi e tubi quadri, piatti).
- La parte di kernel è già stata sviluppata da Ficep all’interno di altri progetti DLL
- I dati di lavoro sono quelli di un FNC:
- Profilo e lunghezza pezzo
- Nome della macro e suoi parametri geometrici e di posizione
- Funzione mirroring a partire dal caso "master"(posizione iniziale piano A)
- Si partirà dalla libreria macro attualmente implementata in PEGASO che consta di:
- File ini di configurazione + bitmaps
- Come strumenti di lavoro avremo PEGASO + viewer 3D di debug per visualizzare il risultato grafico dell'output generato dal codice macro
- Per ogni nuova macro andrà creato un file cs contenente una nuova classe col nome della macro
- La classe consta solo del costruttore e di un metodo CreateMacro (definisce il caso "master" della macro e gestisce gli altri col mirroring)

# Formato FNC
FNC rappresenta il formato dei files di programmazione delle macchine Ficep su interfaccia PEGASO. Tramite questo formato viene descritto ciascun pezzo da lavorare in termini di:
- sezione profilo
- lunghezza pezzo
- lista operazioni macchina
Tra le operazioni ci sono anche le MACRO parametriche di scantonatura. Scopo della libreria Ficep.MacroGra è di produrre la rappresentazione grafica 3d dei pezzi al netto delle MACRO applicate. Per il debug del codice, si utilizzerà l’applicativo Ficep.RobServer che riceverà in ingresso files FNC, creati tramite PEGASO o editati manualmente tramite files di testo (NOTEPAD)












# Robot.ini
Robot.ini rappresenta il file di configurazione del progetto. All’interno di questo file sono elencate tutte le macro esistenti ed abilitate per ciascuna famiglia di profili. Per ciascuna di queste macro andrà poi aggiunta l’indicazione della funzione di grafica associata.
Il file Robot.ini si trova all’interno della cartella [MACRO] assieme ai files cs di definizione delle macro.
Esempio:
Comment=--------------------------------------------------------------------
Comment=    ROBOT.INI   V 6.02x1 10/05/22
Comment=    Configurazione ROBOT di taglio NOZOMI
Comment=--------------------------------------------------------------------

Comment=Direttorio contenente le macro figure
Comment=

Comment=--------------------------------------------------------------------
Comment=    Macro figure profili I
Comment=--------------------------------------------------------------------
[MACRO_I]
Comment=    Macro figure gruppo 1 - Scantonature sugli estremi

MAC:ESTI01=GRP1 BMP:G1F01_I.BMP VX:I A1a B1a C1a D1a E1a F1a G1a H1a I1a L1a M1 N1a O1a R1a S1a ALFA1 BETA1 T:OP
MAC:ESTF01=GRP1 BMP:G1F01_I.BMP VX:F A1a B1a C1a D1a E1a F1a G1a H1a I1a L1a M1 N1a O1a R1a S1a ALFA1 BETA1 T:OP
MAC:ESTI02=GRP1 BMP:G1F02_I.BMP VX:I A1a B1a C1a D1a E1a F1a G1a H1a I1a L1a R1a S1a ALFA1 BETA0 T:OP
MAC:ESTF02=GRP1 BMP:G1F02_I.BMP VX:F A1a B1a C1a D1a E1a F1a G1a H1a I1a L1a R1a S1a ALFA1 BETA0 T:OP
MAC:ESTIA03=GRP1 BMP:G1F09A.BMP VX:I SIDE:A ALFA1 BETA1 A1a T:OP GRA:ESTIA03
MAC:ESTIB03=GRP1 BMP:G1F09B.BMP VX:I SIDE:B ALFA1 BETA1 A1a T:OP
MAC:ESTFA03=GRP1 BMP:G1F09A.BMP VX:F SIDE:A ALFA1 BETA1 A1a T:OP GRA:ESTIA03
MAC:ESTFB03=GRP1 BMP:G1F09B.BMP VX:F SIDE:B ALFA1 BETA1 A1a T:OP
MAC:ESTIA04=GRP1 BMP:G1F07A_I.BMP VX:I VY:A A1a B1a C1a I1 ALFA1 T:OP

## COMMENTI
I commenti sono identificati dalla stringa “Comment=”. Tutto il testo della linea a destra di “Comment=” viene escluso dall’interpretazione
Esempio:
Comment=    Macro figure profili I

## SEZIONI
Le sezioni sono gruppi di linee racchiuse tra un identificatore di inizio sezione e il successivo oppure la fine del file. Le possibili sezioni sono:
[CONFIG] 			Non utilizzata dalla grafica
[ROBOT] 			Non utilizzata dalla grafica
[MACRO_TAGLIO] 		Non utilizzata dalla grafica
[MACRO_TAGLIO_G100] 	Non utilizzata dalla grafica
[MACRO_PALPA]		Non utilizzata dalla grafica
[MACRO_I] 			Sezione macro profili I
[MACRO_U] 			Sezione macro profili U
[MACRO_L] 			Sezione macro profili L
[MACRO_Q] 			Sezione macro profili Q
[MACRO_R]			Sezione macro profili R
[MACRO_F] 			Sezione macro profili F
[MACRO_P] 			Sezione macro profili P
[MACRO_O] 			Sezione macro profili O
[MACRO_D]			Sezione macro profili D
[MACRO_B]			Sezione macro profili B
Le sezioni che contengono informazioni di configurazione per la grafica sono tutte quelle del tipo [MACRO_x].
Il progetto si prefigge di gestire solo i profili IULQRF e quindi le sole sezioni da considerare saranno quelle in azzurro:
[MACRO_I] [MACRO_U] [MACRO_L] [MACRO_Q] [MACRO_F]
## Linee MAC:
All’interno delle sezioni [MACRO_x] si trovano solo linee di commento (Comment=) e linee di descrizione delle macro esistenti/abilitate per il profilo specifico. Queste ultime sono del tipo:
MAC:ESTI11=GRP1 BMP:G1F11.BMP VX:I A1 B1 C1a D1a E1 F1 G1a H1a I1a J1a K1a L1a M1a N1a R1a S1a ALFA1 BETA1 T:OP GRA:ESTI11


Il blocco GRA:ClassName è quello che ci dà l’associazione tra il nome della macro e la corrispondente classe all’interno del progetto C#. Ogni qualvolta ci sarà da graficare la macro MacroName, verrà ricercata la definizione della classe ClassName all’interno del progetto e ne verrà invocato il metodo CreateMacro.

# Cartella MACRO
All’interno del progetto esiste una cartella MACRO contenente tutti i file cs associati alle singole macro e un file di configurazione Robot.ini.

## Creazione di una nuova macro
La creazione di una nuova macro deve seguire i seguenti step:
- Identificazione di MacroName e ClassName
- Ad esempio, supponiamo di voler aggiungere una classe che implementi la grafica per la macro ESTI01 di un profilo I. All’interno del file Robot.ini, ricerco la sezione [MACRO_I] e al suo interno la linea MAC:ESTI01

- [MACRO_I]
- MAC:ESTI01=GRP1 BMP:G1F01_I.BMP VX:I A1a B1a C1a D1a E1a F1a G1a H1a I1a L1a M1 N1a O1a R1a S1a ALFA1 BETA1 T:OP

- Accanto a questa linea ne segue una identica che differisce per i blocchi MAC:ESTF01 e VX:F:

- MAC:ESTF01=GRP1 BMP:G1F01_I.BMP VX:F A1a B1a C1a D1a E1a F1a G1a H1a I1a L1a M1 N1a O1a R1a S1a ALFA1 BETA1 T:OP

- Le 2 linee MAC:ESTI01 e MAC:ESTF01 in realtà descrivono la stessa macro utilizzata in 2 posizioni distinte del profilo (lato Iniziale e lato Finale).
- La grafica di queste 2 macro coinciderà a meno di un mirroring e per questo motivo preferiremo utilizzare una unica ClassName all’interno della quale verrà poi gestito il mirroring per i 2 casi.
- La regola prevederà di avere ClassName coincidente con il MacroName della prima delle macro da raggruppare, nel caso specifico ESTI01. Dovrò aggiungere in fondo alla 2 linee un blocco che crei l’assocazione con ClassName:

- MAC:ESTI01=GRP1 BMP:G1F01_I.BMP VX:I A1a B1a C1a D1a E1a F1a G1a H1a I1a L1a M1 N1a O1a R1a S1a ALFA1 BETA1 T:OP GRA:ESTI01
- MAC:ESTF01=GRP1 BMP:G1F01_I.BMP VX:F A1a B1a C1a D1a E1a F1a G1a H1a I1a L1a M1 N1a O1a R1a S1a ALFA1 BETA1 T:OP GRA:ESTI01



- Altri esempi di assegnazione della ClassName:
MAC:ESTI02=GRP1 BMP:G1F02_I.BMP VX:I A1a B1a C1a D1a E1a F1a G1a H1a I1a L1a R1a S1a ALFA1 BETA0 T:OP GRA:ESTI02
MAC:ESTF02=GRP1 BMP:G1F02_I.BMP VX:F A1a B1a C1a D1a E1a F1a G1a H1a I1a L1a R1a S1a ALFA1 BETA0 T:OP GRA:ESTI02
MAC:SCAI01=GRP2 BMP:G2F01_I.BMP VX:I SIDE:A A1a B1a C1a D1a E1a F1a G1a H1a I1a J1a K1a R1a ALFA1 BETA1 DC1a OPT1 T:OP GRA:SCAI01
MAC:SCAF01=GRP2 BMP:G2F01_I.BMP VX:F SIDE:A A1a B1a C1a D1a E1a F1a G1a H1a I1a J1a K1a R1a ALFA1 BETA1 DC1a OPT1 T:OP GRA:SCAI01
MAC:SCBI01=GRP2 BMP:G2F01_I.BMP VX:I SIDE:B A1a B1a C1a D1a E1a F1a G1a H1a I1a J1a K1a R1a ALFA1 BETA1 DC1a OPT1 T:OP GRA:SCAI01
- MAC:SCBF01=GRP2 BMP:G2F01_I.BMP VX:F SIDE:B A1a B1a C1a D1a E1a F1a G1a H1a I1a J1a K1a R1a ALFA1 BETA1 DC1a OPT1 T:OP GRA:SCAI01
- MAC:SAAI01=GRP2 BMP:G2F05_I.BMP VX:I VY:A SIDE:A A1a B1a C1a D1a E1a F1 G1a H1a I1 R1a OPT1 T:OP GRA:SAAI01
- MAC:SABI01=GRP2 BMP:G2F05_I.BMP VX:I VY:B SIDE:A A1a B1a C1a D1a E1a F1 G1a H1a I1 R1a OPT1 T:OP GRA:SAAI01
- MAC:SABF01=GRP2 BMP:G2F05_I.BMP VX:F VY:B SIDE:A A1a B1a C1a D1a E1a F1 G1a H1a I1 R1a OPT1 T:OP GRA:SAAI01
- MAC:SAAF01=GRP2 BMP:G2F05_I.BMP VX:F VY:A SIDE:A A1a B1a C1a D1a E1a F1 G1a H1a I1 R1a OPT1 T:OP GRA:SAAI01
- MAC:SBBI01=GRP2 BMP:G2F05_I.BMP VX:I VY:B SIDE:B A1a B1a C1a D1a E1a F1 G1a H1a I1 R1a OPT1 T:OP GRA:SAAI01
- MAC:SBAI01=GRP2 BMP:G2F05_I.BMP VX:I VY:A SIDE:B A1a B1a C1a D1a E1a F1 G1a H1a I1 R1a OPT1 T:OP GRA:SAAI01
- MAC:SBBF01=GRP2 BMP:G2F05_I.BMP VX:F VY:B SIDE:B A1a B1a C1a D1a E1a F1 G1a H1a I1 R1a OPT1 T:OP GRA:SAAI01
- MAC:SBAF01=GRP2 BMP:G2F05_I.BMP VX:F VY:A SIDE:B A1a B1a C1a D1a E1a F1 G1a H1a I1 R1a OPT1 T:OP GRA:SAAI01

- Creazione di una nuova classe ClassName
- Creare un nuovo file ClassName.cs all’interno della cartella MACRO e aggiungere una nuova classe ClassName derivata da EyeMacro. Nel nostro esempio ClassName=ESTI01:

- ESTI01.CS
using Ficep3DUtility;
using Ficep.DataCore;
using devDept.Eyeshot.Entities;
using System.Collections.Generic;
using System.Linq;

namespace FicepMacroGra
{
public class ESTI01 : EyeMacro
{

public ESTI01 (IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, double tolThickness = 0.01, double brepTol = 0.01, double surplus = 1)
: base(wp, param, macroClassName, macroName, tolThickness, brepTol, surplus)
{
ProfilesEnabled = "I";
}

public override bool CreateMacro()
{
//
//  Qui va inserito il codice della grafica
//
return true;
}

}
}

- Tutte le classi create saranno derivate da EyeMacro e conterranno 2 sole funzioni:
- Il costruttore, con una sola istruzione che imposta i profili gestiti (ProfilesEnabled)
- CreateMacro, che rappresenta la funzione in cui inserire la descrizione grafica

- Creazione veloce di una nuova classe derivata da EyeMacro
- E’ stato creato un template VisualStudio per la creazione rapida di una nuova classe derivata da EyeMacro.

- Per prima cosa va installato il template copiando il file:
Ficep.MacroGra\Templates\MacroTemplate.zip
Nella cartella utenti:
C:\Users\UserName\Documents\Visual Studio 2022\Templates\ItemTemp
- Usando il right click posizionato sulla cartella MACRO del progetto, si va a selezionare l’inserimento di un nuovo item:


- Selezionare “Macro Template” e modificare il nome del file cs, inserendo come nome del file il ClassName:




## File excel di configurazione
Per condensare in un foglio di calcolo le informazioni riguardanti le macro della libreria, è stato creato un file Excel “LibreriaRobot.xlsx” all’interno della cartella MACRO.


# Profili
## PROFILI
Le sezioni dei profili sono descritte in forma parametrica, specificando larghezza e spessore dei piani che lo compongono. Qui sotto riportiamo profilo per profilo il significato assunto dalle variabili SA, TA, SB, TB.








# Strumenti di debug
## Progetto VisualStudio
La libreria Ficep.MacroGra è stata sviluppata in ambiente Microsoft Visual Studio 2022
## Repository
Il repository GIT verrà condiviso su Ficep GIT Server dove verrà creato un branch di sviluppo
## Ficep.RobServer
Il debug della libreria avverrà all’interno del progetto Ficep.RobServer che la utilizza. L’applicativo Ficep.RobServer va eseguito passando un solo argomento in ingresso che corrisponde al path completo del file Robot.ini:

Le operazioni da eseguire per il debug sono le seguenti:
- Apertura di un file FNC all’interno di Ficep.RobServer: i files FNC saranno stati creati in precedenza utilizzando l’applicativo PEGASO oppure editandoli manualmente con un editor di testo.


- Debug delle varie funzioni CreateMacro


- Visualizzazione grafica delle entità grafiche create tramite finestra di viewer 3d all’interno di Ficep.RobServer

## PEGASO
Il software PEGASO è l’attuale HMI installata sulle macchine Ficep; utilizza le MACRO all’interno dei sui programmi. Le MACRO sono programmate attraverso linee di tipo COPE che presentano una dialog per l’editazione dei parametri.



PEGASO presenta una preview 2d della grafica del pezzo in cui sono graficate le operazioni del programma, disegnando aree colorate sovrapposte al disegno dei piani profilo.
Questa grafica 2d può essere utilizzata come punto di partenza per comprendere il risultato finale della lavorazione e aiutare a creare i corretti volumi da sottrarre nell’implementazione delle MACRO della libreria Ficep.MacroGra.
| Revisione | Redazione | Data | Approvazione | Data | Descrizione modifiche |
| --- | --- | --- | --- | --- | --- |
| 1.0 | M.Menoni | 20/11/23 |  |  | Prima versione del documento |
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |
| MAC:ESTI11=GRP1 | ESTI11 è il MacroName (nome della macro)
GRP1 sta ad indicare l’appartenenza della macro al gruppo 1 |
| --- | --- |
| BMP:G1F11.BMP | G1F11.BMP è il nome del file di bitmap |
| VX:I |  |
| A1 B1 C1a D1a E1 F1 G1a H1a I1a J1a K1a L1a M1a N1a R1a S1a ALFA1 BETA1 | Parametri abilitati per la macro.
Il postfisso a sta ad indicare un parametro che rappresenta una grandezza lineare |
| T:OP | Indicazione delle tecnologie abilitate:
T:P solo plasma
T:O solo ossitaglio
T:OP ossitaglio e plasma |
| GRA:ESTI11 | ESTI11 è il ClassName (nome della classe all’interno del progetto C#) |