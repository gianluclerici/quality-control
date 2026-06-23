
namespace Ficep.AnyCut.Mathematics
{
    public class Piece
    {
        public uint Np { get; set; }             //  Numeropezzo
        public double Lp {get;set;}              //	Lunghezza pezzo
        public double LpInBarra {get;set;}      //	Lunghezza pezzo effettiva (occup. in barra)
        public double BarLength {get;set;}      //  Lunghezza barra cui il pezzo appartiene
        public double AngAI {get;set;}          //  Angolo anima iniziale
        public double AngAF {get;set;}          //  Angolo anima finale
        public double AngBI {get;set;}          //  Angolo ala iniziale
        public double AngBF {get;set;}          //  Angolo ala finale
        public double OffsXNesting {get;set;}   //  Offset X pezzo.
        public double OffsYNesting {get;set;}   //  Offset Y pezzo.
        public PRF_Info Prf {get;set;}            //	Struttura dati profilo
        public uint DisplayList {get;set;}   //	Numero display list per grafica
        public bool Selected {get;set;}      //	Flag pezzo selezionato (per grafica)
        public bool Unloaded {get;set;}      //	Flag pezzo scaricato (per grafica)
        public bool Cutted {get;set;}            //	Flag pezzo separato dalla barra	
        public bool IsScrap {get;set;}       //	Flag pezzo sfrido
        public double OffsXUnload {get;set;} //  Offset X scarico.
        public double OffsYUnload {get;set;} //  Offset Y scarico.
        public double TraslaX {get;set;}     //	Valore TraslaX da applicare al pezzo	
        public bool RotX {get;set;}          //	Flag rotazione X
        public bool RotY {get;set;}          //	Flag rotazione Y
        public long NSequenceOpe {get;set;}  //	Numero sequenze operazioni
        //public void* SequenceOpe {get;set;}  //	Puntatore memoria operazioni
        //public void* LinkFncOpeFirst {get;set;}//  Link alla prima ope Fnc
        //public void* LinkFncOpeLast {get;set;} //  Link all'ultima ope Fnc
        //public bool DeAllocSequenceOpe {get;set;}    //	true se deallocando la memoria pezzo devo deallocare anche gli oggetti FncOpe
        public bool MainAssembly {get;set;}      //	MainPart di un assembly
        public double AssemblyX {get;set;}          //	Posizione X rispetto alla main part dell'assembly
        public double AssemblyY {get;set;}          //	Posizione Y rispetto alla main part dell'assembly
        public double AssemblyZ {get;set;}          //	Posizione Z rispetto alla main part dell'assembly
        public double AssemblyRX {get;set;}     //	Rotazione asse X rispetto alla main part dell'assembly
        public double AssemblyRY {get;set;}     //	Rotazione asse Y rispetto alla main part dell'assembly
        public double AssemblyRZ {get;set;}    //	Rotazione asse Z rispetto alla main part dell'assembly
        public string Contract {get;set;}
        public string Drawing {get;set;}
        public string Mark {get;set;}
        public string Position {get;set;}
    }
}
