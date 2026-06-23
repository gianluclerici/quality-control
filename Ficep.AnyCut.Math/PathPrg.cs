
namespace Ficep.AnyCut.Mathematics
{
    public class PathPrg
    {
        public int Tipo { get; set; }
        public int Piano { get; set; }
        public int NSeq { get; set; }
        public int PrbCode { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double R { get; set; }
        public double DN { get; set; }
        public double AngX { get; set; }
        public double AngZ { get; set; }
        public double Depth { get; set; }
        public double CHE { get; set; }
        public double CHI { get; set; }
        public double OrgX { get; set; }
        public double OrgY { get; set; }
        public int Tool { get; set; }
        public int Riferimento { get; set; }
        public int CompKerf { get; set; }
        public double Tp { get; set; }
        public double DisToolMat { get; set; }
        public double Feed { get; set; }
        public double Amp { get; set; }
        public double Rnd { get; set; }
        public double CoreAng { get; set; }
        public int AsseCont { get; set; }
        public int CodM { get; set; }
        public int CodMPalp { get; set; }           //	Codice M di palpatura (solo per true HOLE)
        public int CodT { get; set; }
        public int AsseRot { get; set; }
        public bool If_AxOk { get; set; } 
        public int TestColl { get; set; }
        public bool Reverse{ get; set; }
        public bool Bridge { get; set; }
        public bool CutLink { get; set; }
        public bool RatHole { get; set; }
        public bool AsseD_DC_FIX { get; set; }      //	Asse D bloccato sull'anima nella posiz. filo fisso
        public bool AsseD_DC_MOB { get; set; }      //	Asse D bloccato sull'anima nella posiz. filo mobile
        public bool AsseE_DAB_B_TOP { get; set; }   //	Asse E orientato con torcia dal basso verso l'alto		(piani DA e DB)
        public bool AsseE_DAB_TOP_B { get; set; }   //	Asse E orientato con torcia dall'alto verso il basso	(piani DA e DB)
        public int FixedTool { get; set; }          //	Se diverso da 0, rappresenta il codice TS da riportare su eventuale MACRO temporanea
        public int Rele { get; set; }               //	Se diverso da 0, va interpretato (con segno + o -) come indice relè da settare
        public double OvrSpeed { get; set; }        //	Se diversa da 0, diventata l'override di velocità (bypassa anche l'eventuale FEED)
        public int OvrContinuo { get; set; }        //  Override CONTINUO/PASSOPASSO
        public bool DisToolMatSaturata { get; set; }//	true se la DisToolMat non va maggiorata sul punto di attacco.
        public bool MarkingPath { get; set; }       //	Percorso di marcatura
        public bool TrueHolePath { get; set; }      //	Percorso true HOLE
        public bool SepCutPath { get; set; }        //	Percorso appartenente a un TAGLIO di separazione
        public bool ReqFinalPathRepos { get; set; } //	Percorso che va eseguito con posiz. asse X vicino all'uscita
        public bool ApplyLeadInLeadOut { get; set; }//	Richiesta applicazione campi LEAD-IN e LEAD-OUT.
        public int IdxTH { get; set; }              //	Indice tabella true-hole (solo se trueHolePath = true)
        public double LeadInX { get; set; }         //	Movimento ausiliario di LEAD-IN da eseguire sul primo punto (direzione X)
        public double LeadInY { get; set; }         //	Movimento ausiliario di LEAD-IN da eseguire sul primo punto (direzione Y)
        public double LeadOutX { get; set; }        //	Movimento ausiliario di LEAD-OUT da eseguire sul primo punto (direzione X)
        public double LeadOutY { get; set; }        //	Movimento ausiliario di LEAD-OUT da eseguire sul primo punto (direzione Y)
        public double PrbDX { get; set; }           //	Offset DX per palpatura supplementare
        public double OverlapCuttedLen { get; set; }//	Sovrapposizione	del tratto finale con materiale già tagliato
                                                    //	Gestione vettori
        public double Pos_x { get; set; }
        public double Pos_y { get; set; }
        public double Pos_z { get; set; }
        public double Vec_x { get; set; }
        public double Vec_y { get; set; }
        public double Vec_z { get; set; }
                                                    //	Generazione linee di scarico
        public bool ReqGenUnload { get; set; }      //	Forzatura generazione linee T800 di scarico su completamento del pezzo
                                                    //	Informazioni supplementari per la gestione della posizione del taglio rispetto alla barra.
        public bool Condiviso { get; set; }         //	true se il punto appartiene a un PATH condiviso tra 2 pezzi.		
        public bool Iniziale { get; set; }          //	true se il punto appartiene a un PATH INIZIALE.		
                                                    //
        public int SourceSeqPathIdx { get; set; }   //	Indice Path all'interno della lista sequenze in ingresso 
        public RobSeq_Seq RobSeqSEQ { get; set; }   //	Puntatore sequenza associata
        public int SourceIdxSubSeq { get; set; }	//	Indice sottosequenza all'interno della sequenza associata
    }
}
