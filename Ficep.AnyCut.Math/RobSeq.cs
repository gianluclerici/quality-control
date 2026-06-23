
namespace Ficep.AnyCut.Mathematics
{
    public class RobSeq_Seq
    {
        public int Idx { get; set; }                        //	Indice SEQUENZA
        public int NSubSeq { get; set; }                    //	Numero di sottosequenze
        public RobSeq_SubSeq RobSeqSUBSEQ { get; set; }     //	SOTTO-SEQUENZE
        public RobSeq_Taglio RobSeqTAGLIO { get; set; }     //	Eventuale TAGLIO associato
        public int NP { get; set; }                         //	Numero pezzo
        public int SUT { get; set; }                        //	Torcia selezionata (1 = Plasma, 2 = Ossitaglio)
        public int UT { get; set; }                         //	Numero utensile in Tool Management
        public int UM { get; set; }                         //	Marcatura (UM=1) / Taglio (UM=0)
        public int TH { get; set; }                         //	Indice tabella true Hole
        public double TOFF { get; set; }                    //	Tempo di anticipo spegnimento plasma
        public int TB { get; set; }                         //	Indice tabella trueBevel per plasma HPR
        public int IT { get; set; }                         //	Indice tabella di taglio per plasma HPR
        public string Name { get; set; }                    //	Nome sequenza
        public bool PositionAssigned { get; set; }          //	Posizione assegnata da lettura file di configurazione MACRO
        public int Position  { get; set; }                  //	Posizione rispetto al pezzo letta da file di configurazione MACRO
        public double MinX { get; set; }                    //	Minimo valore X punti della SEQUENZA
        public double MaxX { get; set; }                    //	Massimo valore X punti della SEQUENZA
        public double NestingMinX { get; set; }             //	Minimo valore X punti della SEQUENZA all'interno del Nesting
        public double NestingMaxX { get; set; }             //	Massimo valore X punti della SEQUENZA all'interno del Nesting
        public bool SideA { get; set; }                     //	Lavorazioni presenti sul piano A
        public bool SideB { get; set; }                     //	Lavorazioni presenti sul piano B
        public bool SideC { get; set; }                     //	Lavorazioni presenti sul piano C
        public bool SideD { get; set; }                     //	Lavorazioni presenti sul piano D
        public bool ReqInsStitch { get; set; }              //	Richiesta inserimento automatico stitch
        public bool FullSeparation { get; set; }            //	true se la sequenza separa completamente il materiale
    }

    public class RobSeq_SubSeq
    {
        public double NestingMinX { get; set; }             //	Minimo valore X punti della SOTTO-SEQUENZA all'interno del Nesting
        public double NestingMaxX { get; set; }				//	Massimo valore X punti della SOTTO-SEQUENZA all'interno del Nesting
    }

    public class RobSeq_Taglio
    {
        public int NP { get; set; }                         //	Numero pezzo
        public int Idx { get; set; }                        //	Indice TAGLIO
        public double X { get; set; }                       //	Quota asse X
        public bool TAGLIO_I { get; set; }                  //	Variabile da trasportare invariata da ROBSEQ_IN verso ROBSEQ_OUT
        public double ANGA { get; set; }                    //	Angolo anima
        public double ANGB { get; set; }                    //	Angolo ali
        public int CNW { get; set; }                        //	Codice CNW
        public bool RIM { get; set; }                       //	true = rimanenza
        public double X_IN { get; set; }                    //	Quota X come da ROBSEQ_IN
        public double LBR { get; set; }                     //	LBR
        public bool TAGLIO_C { get; set; }                  //	true = taglio condiviso, false = taglio associato a uno scrap
        public int TotalNumberPath { get; set; }            //	Numero totale di PATH di cui si compone il TAGLIO
        public int CounterPath { get; set; }                //	Contatore Path di cui si compone il TAGLIO
        public bool SecondTAGLIO { get; set; }              //	Secondo TAGLIO associato allo stesso NP
        public bool Stitched { get; set; }                  //	TAGLIO con stitches (almeno 1)
        public bool TaglioIniziale { get; set; }            //	true = taglio iniziale, false = taglio finale
        public bool LastReqFinalPathRepos { get; set; }		//	Richiesta di riposizionamento pezzo per l'esecuzione dell'ultimo PATH
    }
}
