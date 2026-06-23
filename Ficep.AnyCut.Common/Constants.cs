
namespace Ficep.AnyCut.Common
{
    public static class Constants
    {
        public enum CodPRF { PRF_L, PRF_I, PRF_F, PRF_U, PRF_D, PRF_Q, PRF_R, PRF_B, PRF_T, PRF_O, PRF_W = 18, PRF_P = 19 }
        public enum Piano { NULL, A, B, C, D, AB, E, F, G }
        public enum PosPoint { 
            POS_POINT_DEF = 0, 
            POS_POINT_TOP_WEB_SX = 1, 
            POS_POINT_TOP_WEB_DX = 2, 
            POS_POINT_BOT_WEB_SX = 3, 
            POS_POINT_BOT_WEB_DX = 4,
            POS_POINT_TOP_L_FLG_SX = 5, 
            POS_POINT_TOP_L_FLG_DX = 6,
            POS_POINT_BOT_L_FLG_SX = 7,
            POS_POINT_BOT_L_FLG_DX = 8,
            POS_POINT_TOP_R_FLG_SX = 9,
            POS_POINT_TOP_R_FLG_DX = 10,
            POS_POINT_BOT_R_FLG_SX = 11,
            POS_POINT_BOT_R_FLG_DX = 12,
            POS_POINT_L_FLG_EXT_SX = 13,
            POS_POINT_L_FLG_EXT_DX = 14,
            POS_POINT_R_FLG_EXT_SX = 15,
            POS_POINT_R_FLG_EXT_DX = 16
        }
        public enum PointPlane
        {
            POINT_PLANE_XY = 0,
            POINT_PLANE_XZ = 1,
            POINT_PLANE_YZ = 2
        }
        public enum PointVect
        {
            POINT_VECT_X = 0,
            POINT_VECT_Y = 1,
            POINT_VECT_Z = 2,
            POINT_VECT_YX = 3,
            POINT_VECT_ZX = 4,
            POINT_VECT_ZY = 5,
            POINT_VECT_XYZ = 6
        }

        public enum PointColDir
        {
            POINT_COLDIR_X_P = 0,
            POINT_COLDIR_X_N = 1,
            POINT_COLDIR_Y_P = 2,
            POINT_COLDIR_Y_N = 3,
            POINT_COLDIR_Z_P = 4,
            POINT_COLDIR_Z_N = 5
        }
        public enum PosPath
        {
            INVALID_POINT_PATH = 0,
            LINEAR_POINT_PATH = 1,
            NONLINEAR_POINT_PATH = 2
        }

        public enum Zone
        {
            NULL = 0,
            D_Y_MIN = 1,
            A = 2,
            C = 3,
            B = 4,
            D_Y_MAX = 5,
            INSIDE_PROFILE = 6
        }
        public enum AssiStatus
        {
            ASSI_INVALID = 0,
            ASSI_POS_UNREACHED = 1,
            ASSI_POS_REACHED_COLL = 2,
            ASSI_POS_REACHED = 3
        }

        public enum ClampPos
        {
            OPEN = 0,
            CLOSE = 1,
            FREEZE = 2
        }

        public enum VertClampPos
        {
            OPEN = 0,
            CLOSE = 1,
            PARTIAL = 2,
            OPEN_COMPLETELY = 3
        }

        public enum PathExec
        {
            PATH_EXEC_DEFAULT = 0,
            PATH_EXEC_BEFORE_TAGLIO = 1,
            PATH_EXEC_AFTER_TAGLIO = 2
        }

        public enum CellPos
        {
            UNDEF_CELL_POS = 0,
            OUTSIDE_CELL_IN = 1,
            CLAMPED_VICE_IN = 2,
            FALL_INSIDE_CELL = 3,
            CLAMPED_VICE_OUT = 4,
            CLAMPED_VICE_IN_OUT = 5,
            OUTSIDE_CELL_OUT = 6,
            INSIDE_CELL_1_ROLL = 7,
            INSIDE_CELL_2_ROLL = 8
        }
        public enum CollType
        {
            NO_COLL = 0,
            COLL_ON = 1,
            COLL_OFF = 2
        }

        public enum ProbingRef { BOTTOM_REF, MIDDLE_REF, TOP_REF, WEB_REF, UNDEF_REF }
        public enum LeadPoints { NO_LEAD_ON, LEAD_ON_TOP, LEAD_ON_BOTTOM, LEAD_ON_CORE, LEAD_ON_EDGE, LEAD_ON_BOTTOM_EDGE = 1, LEAD_ON_BOTTOM_BORDER, LEAD_ON_TOP_EDGE, LEAD_ON_TOP_BORDER, MAX_PUNTI_INTERSEZ = 200, MAX_PUNTI_LEAD_IN = 20, TAGLI_X_IN_PINZA_ALLOWED_WITH_MESSAGE = 0, TAGLI_X_IN_PINZA_ALLOWED_ALWAYS, TAGLI_X_IN_PINZA_NOT_PERMITTED }
        public enum References { TOP_LEFT = 1, TOP_RIGHT, BOTTOM_LEFT, BOTTOM_RIGHT, MIDDLE_LEFT, MIDDLE_RIGHT, MIDDLE_LEFT_2, MIDDLE_RIGHT_2, MIDDLE_LEFT_C, MIDDLE_RIGHT_C }

        public enum BevelType 
        {
            NONE = 0,
            A = 1,
            B = 2,
            V = 3,
            Y = 4,
            K = 5,
            STRAIGHT = 10
        }

        public enum ExtXMode
        {
            EXT_X_REPOSITIONER = 0,
            EXT_X_INTERPOLATED = 1
        }

        public enum UnloadType : int
        {
            UNDEFINED_UNLOAD		= -1,		//	Scarico indefinito
            NO_UNLOAD				= 0,		//	Nessuno scarico, nè manuale nè automatico
            NO_UNLOAD_MSG			= 1,		//	Nessuno scarico, nè manuale nè automatico + emissione messaggio
            MANUAL_UNLOAD			= 2,		//	Scarico manuale
            AUTOMATIC_UNLOAD		= 3,		//	Scarico automatico
            NO_UNLOAD_PINCHER_BACK	= 4,		//	Come NO_UNLOAD, preceduto da un arretramento pinza per agevolare la caduta del pezzo in buca
            AUTOMATIC_UNLOAD_PINCHER_FORWARD =	5,		//	Come AUTOMATIC_UNLOAD, preceduto da un avanzamento pinza per spingere in uscita il pezzo
            M30_UNLOAD				= 6		//	Scarico materiale in pinza su M30 (gestione PLC)
        }

        public enum GripperType : int 
        {
            NO_GRIPPER_RETRACT			= 0,
            GRIPPER_RETRACT_PARTIAL		= 1,
            GRIPPER_RETRACT_COMPLETE	= 2
        }

        //	Tipologia connection POINT TO POINT
        public enum ConnectionType : int 
        {
            FREE_COLLISION_CONNECTION   = 1,
            JOINT_CONNECTION            = 2,
            LINEAR_CONNECTION           = 3
        }

        public enum ProbingType : int
        {
            Null = 0,
            Laser = 1,
            Mechanical = 2,
            LaserMechanical = 3,
            Default = 4,
        }


        //
        //	Orientamento Frame
        //
        //	XP		Asse X+
        //	XN		Asse X-
        //	YP		Asse Y+
        //	YN		Asse Y-
        //	ZP		Asse Z+
        //	ZN		Asse Z-
        //
        public enum FrameOrientation : int
        {
            XP_YP_ZP =	1,		//	X = XP	Y = YP	Z = ZP
            XP_YN_ZN =	2,		//	X = XP	Y = YN	Z = ZN
            XP_ZP_YN =	3,		//	X = XP	Y = ZP	Z = YN
            XP_ZN_YP =	4,		//	X = XP	Y = ZN	Z = YP
            
            XN_YP_ZN =	5,		//	X = XN	Y = YP	Z = ZN
            XN_YN_ZP =	6,		//	X = XN	Y = YN	Z = ZP
            XN_ZP_YP =	7,		//	X = XN	Y = ZP	Z = YP
            XN_ZN_YN =	8,		//	X = XN	Y = ZN	Z = YN
            
            YP_XP_ZN =	9,		//	X = YP	Y = XP	Z = ZN
            YP_XN_ZP =	10,		//	X = YP	Y = XN	Z = ZP
            YP_ZP_XP =	11,		//	X = YP	Y = ZP	Z = XP
            YP_ZN_XN =	12,		//	X = YP	Y = ZN	Z = XN
            
            YN_XP_ZP =	13,		//	X = YN	Y = XP	Z = ZP
            YN_XN_ZN =	14,		//	X = YN	Y = XN	Z = ZN
            YN_ZP_XN =	15,		//	X = YN	Y = ZP	Z = XN
            YN_ZN_XP =	16,		//	X = YN	Y = ZN	Z = XP
            
            ZP_XP_YP =	17,		//	X = ZP	Y = XP	Z = YP
            ZP_XN_YN =	18,		//	X = ZP	Y = XN	Z = YN
            ZP_YP_XN =	19,		//	X = ZP	Y = YP	Z = XN
            ZP_YN_XP =	20,		//	X = ZP	Y = YN	Z = XP
            
            ZN_XP_YN =	21,		//	X = ZN	Y = XP	Z = YN
            ZN_XN_YP =	22,		//	X = ZN	Y = XN	Z = YP
            ZN_YP_XP =	23,		//	X = ZN	Y = YP	Z = XP
            ZN_YN_XN =	24,		//	X = ZN	Y = YN	Z = XN
        }

        public enum CartesianAx : int
        {
            NULL = 0,
            X = 1, 
            Y = 2, 
            Z = 3
        }

        public const uint MAX_UT = 2;           //  Numero di utensili UT

        public const uint UT_UNDEFINED = 0;
        public const uint UT_PLASMA = 1;
        public const uint UT_OXY = 2;

        public const uint IDX_UT_PLASMA = 0;    //  Indice utensile plasma
        public const uint IDX_UT_OXY = 1;       //  Indice utensile ossitaglio

        public const double TOLL_RAGGI = 0.01;  //  Tolleranza raggi
        public const double TOLL_ANGOLI = 0.01; //  Tolleranza angoli

        public const uint AI_CORR_PROBE = 12;   //	Dimensione arry correttori di palpatura

        //
        //	Indici (a partire da 1) dei correttori applicati in direzione Y (orizzontale).
        //
        //					DA					DB
        //				3	|					|	6
        //					|	7 =(2 + 5)/2	|
        //				2	|--------xx---------|	5
        //					|					|
        //				1	|		 xx			|	4
        //						8 =(1 + 4)/2
        //							(solo DD)

        public const uint AI_PROBE_CORR_BOTTOM_DA_Y = 1;
        public const uint AI_PROBE_CORR_MIDDLE_DA_Y = 2;
        public const uint AI_PROBE_CORR_TOP_DA_Y = 3;
        //
        public const uint AI_PROBE_CORR_BOTTOM_DB_Y = 4;
        public const uint AI_PROBE_CORR_MIDDLE_DB_Y = 5;
        public const uint AI_PROBE_CORR_TOP_DB_Y = 6;
        //
        public const uint AI_PROBE_CORR_MIDDLE_DC_Y = 7;        //	Si ottiene dalla media dei correttori 2 e 5
                                                                //
        public const uint AI_PROBE_CORR_MIDDLE_DD_Y = 8;		//	Si ottiene dalla media dei correttori 1 e 4

        //
        //	Indici (a partire da 1) dei correttori applicati in direzione Z (verticale).
        //
        //				2___DA				  DB____4
        //					|					|
        //					|	5      6     7  |
        //	8 =(1 + 2)/2	x-------------------x  9 = (3 + 4) / 2
        //					|					|
        //				1___|		   x		|___3
        //					   10 = (1 + 3)/2
        //							(solo DD)
        //
        public const uint AI_PROBE_CORR_BOTTOM_DA_Z = 1;
        public const uint AI_PROBE_CORR_MIDDLE_DA_Z = 8;	//	Si ottiene dalla media dei correttori 1 e 2
        public const uint AI_PROBE_CORR_TOP_DA_Z = 2;
        //
        public const uint AI_PROBE_CORR_BOTTOM_DB_Z = 3;
        public const uint AI_PROBE_CORR_MIDDLE_DB_Z = 9;	//	Si ottiene dalla media dei correttori 3 e 4
        public const uint AI_PROBE_CORR_TOP_DB_Z = 4;
        //
        public const uint AI_PROBE_CORR_BOTTOM_DC_Z = 5;
        public const uint AI_PROBE_CORR_MIDDLE_DC_Z = 6;
        public const uint AI_PROBE_CORR_TOP_DC_Z = 7;
        //
        public const uint AI_PROBE_CORR_MIDDLE_DD_Z = 10;   //	Si ottiene dalla media dei correttori 1 e 3

        public const uint BOTTOM_REF = 0;	//	Bottom reference
        public const uint MIDDLE_REF = 1;	//	Middle reference
        public const uint TOP_REF = 2;	    //	Top reference
        public const uint WEB_REF = 3;      //	Web reference (solo per piani DA/DB di profili I/W)

        public const uint CORR_COPLANARITY_X_DA_BOTTOM		=	0;
        public const uint CORR_COPLANARITY_X_DA_TOP			=	1;
        public const uint CORR_COPLANARITY_X_DB_BOTTOM		=	2;
        public const uint CORR_COPLANARITY_X_DB_TOP			=	3;
        public const uint CORR_COPLANARITY_X_DC_FIX			=	4;
        public const uint CORR_COPLANARITY_X_DC_MOB			=	5;
        public const uint CORR_COPLANARITY_X_DD_FIX			=	6;
        public const uint CORR_COPLANARITY_X_DD_MIDDLE_FIX	=	7;
        public const uint CORR_COPLANARITY_X_DD_MOB			=	8;
        public const uint CORR_COPLANARITY_X_DD_MIDDLE_MOB  =   9;

        public const uint CORR_COPLANARITY_X_DA_ROUND		=	0;
        public const uint CORR_COPLANARITY_X_DB_ROUND		=	1;
        public const uint CORR_COPLANARITY_X_DC_ROUND		=	2;
        public const uint CORR_COPLANARITY_X_DD_FIX_ROUND	=	3;
        public const uint CORR_COPLANARITY_X_DD_MOB_ROUND   =   4;

        public const uint CORR_EXTCHAMFER_X_DA_INI		    =   0;
        public const uint CORR_EXTCHAMFER_X_DB_INI		    =   1;
        public const uint CORR_EXTCHAMFER_X_DC_FIX_INI	    =   2;
        public const uint CORR_EXTCHAMFER_X_DC_MOB_INI	    =   3;
        public const uint CORR_EXTCHAMFER_X_DD_FIX_INI	    =   4;
        public const uint CORR_EXTCHAMFER_X_DD_MOB_INI	    =   5;
        public const uint CORR_EXTCHAMFER_X_DA_FIN		    =   6;
        public const uint CORR_EXTCHAMFER_X_DB_FIN		    =   7;
        public const uint CORR_EXTCHAMFER_X_DC_FIX_FIN	    =	8;
        public const uint CORR_EXTCHAMFER_X_DC_MOB_FIN	    =	9;
        public const uint CORR_EXTCHAMFER_X_DD_FIX_FIN	    =	10;
        public const uint CORR_EXTCHAMFER_X_DD_MOB_FIN      =   11;

        public const uint CORR_EXTCHAMFER_ANG_DD_FIX_INI    =   0;
        public const uint CORR_EXTCHAMFER_ANG_DD_MOB_INI    =   1;
        public const uint CORR_EXTCHAMFER_ANG_DD_FIX_FIN    =   2;
        public const uint CORR_EXTCHAMFER_ANG_DD_MOB_FIN    =   3;

        public const uint METODO_J6_DEFAULT                 =   0;
        public const uint METODO_J6_Z0_PRECEDENTE           =   1;
        public const uint METODO_J6_SECANTI                 =   2;


        //	Massimo numero di assi interni (robot)
        public const int MAX_ROB_AX = 6;
        //	Massimo numero di assi (interni + esterni)
        public const int MAX_AX = 36;

        public const string AI_PRG_TOUCH_PROBE = "TOUCH_PROBE";

        //	Numero di default assi per nodi CONTROLLER e TASKS.
        public const int DEF_NASSI = 15;

        public const int SMARTCOLL_JOINT_REVOLUTE = 0;
        public const int SMARTCOLL_JOINT_PRISMATIC = 1;

        //	Direzione vettori di rotazione / traslazione
        public const int VECT_X_PLUS = 0;
        public const int VECT_X_MINUS = 1;
        public const int VECT_Y_PLUS = 2;
        public const int VECT_Y_MINUS = 3;
        public const int VECT_Z_PLUS = 4;
        public const int VECT_Z_MINUS = 5;

        //	Indici associati alle tecnologie di taglio e utilizzati per l'ordinamento dei PATH.
        public const int BASE_IDX_TEC = 0;
        public const int BASE_IDX_TEC_PLASMA_MARK = 0;
        public const int BASE_IDX_TEC_PLASMA_TH = 1000;
        public const int BASE_IDX_TEC_PLASMA_CUT = 10000;

        //  Matrice identità
        public static double[] IdentityMatrix = {  1, 0, 0, 0,
                            0, 1, 0, 0,
                            0, 0, 1, 0,
                            0, 0, 0, 1
                            };

    }
}
