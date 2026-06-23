using System.Runtime.InteropServices;

namespace Ficep.AnyCut.Common
{
    /// <summary>
    /// Enumerati di tutti i codici di errore
    /// </summary>
    public static class AnyCutErr
    {
        public enum AnyCutRetValue : uint
        {
            NoErr,
            //  Errori nella tipologia di impianto
            MachineTypeNotAdmitted,             //  ERR_ANYCUT001
            //  Errori di configurazione
            InvalideAIRobotConfiguration,       //  ERR_ANYCUT002
            AnyCutConfigNotExisting,            //  ERR_ANYCUT003
            PlantFileNotExisting,               //  ERR_ANYCUT004
            WrongPlantFileExtension,            //  ERR_ANYCUT004
            //  Errori nei dati di ingresso da Polaris
            WrongPolarisInputData,              //  ERR_ANYCUT005
            CreateOpeGroupFailed,               //  ERR_ANYCUT006
            CuttingToolNotSpecified,            //  ERR_ANYCUT007
            ToolTypeNotDefined,                 //  ERR_ANYCUT008
            WrongMarkingTool,                   //  ERR_ANYCUT009
            ValidationFailed,                   //  ERR_ANYCUT030
            //  Errori nelle operazioni su file
            FolderCreationFailed,               //  ERR_ANYCUT010
            //  Errori RobServer (ROBOT)
            StartRobServerFailed,               //  ERR_ANYCUT011
            RobServerConnectionFailed,          //  ERR_ANYCUT011
            //  Errori macro (ROBOT)
            MacroValidationFailed,              //  ERR_ANYCUT012
            MacroExtractionFailed,              //  ERR_ANYCUT013
            //  Errori nella lettura dei dati da tabella
            SetupOxycuttingNotCorrect,          //  ERR_ANYCUT014
            OxyToolNotFoundInTable,             //  ERR_ANYCUT014
            SetupPlasmaNotCorrect,              //  ERR_ANYCUT015
            PlasmaToolNotFoundInTable,          //  ERR_ANYCUT015
            LaserToolNotFoundInTable,           //  ERR_ANYCUT016
            ReadingCuttingTableFailed,          //  ERR_ANYCUT017
            NullKerfNotAdmitted,                //  ERR_ANYCUT018
            //  Errori sw interni
            InternalSwFault,                    //  ERR_ANYCUT019
            //  Errori di matematica
            MathErrCCInt,                       //  ERR_ANYCUT020
            MathErrCenCoin,                     //  ERR_ANYCUT021
            MathErrLCInt,                       //  ERR_ANYCUT022
            InvKinematicsFailed,                //  ERR_ANYCUT023
            BevelRadiusTooSmall,                //  ERR_ANYCUT024
            GetVectorFailed,
            MathErrIntersez,
            //  Errori nella validazione dei dati del programma sorgente
            ValidationFailedGeneric,            //  ERR_ANYCUT025
            ValidationFailedPathCNullKerfComp,  //  ERR_ANYCUT026
            ValidationFailedPathCBevelGeometry, //  ERR_ANYCUT027
            ValidationFailedVertexAngleOverride,//  ERR_ANYCUT028
            ValidationFailedVertexVectorValues, //  ERR_ANYCUT029
            //  Errore generico
            GenericAnyCutErr,                   //  ERR_ANYCUT100
            //  Errori Robot
            TaskProcessorFailed,                //  ERR_ANYCUT101
            SequencePlannerFailed,              //  ERR_ANYCUT102
            AIRobotCruErrData,                  //  ERR_ANYCUT103
            AIRobotCruNoErr,                    //  ERR_ANYCUT104       NON E' UN VERO ERRORE
            AIRobotCruCompNoSel,                //  ERR_ANYCUT105
            AIRobotCruErrSoftware,              //  ERR_ANYCUT106
            AIRobotCruErrRaggio,                //  ERR_ANYCUT107
            AIRobotErrFanucXMax,                //  ERR_ANYCUT108
            AIRobotErrPathNotFeasibleOnSideD,   //  ERR_ANYCUT109
            AIRobotErrCalcExecutionTime,        //  ERR_ANYCUT110
            AIRobotErrCreateRepos,              //  ERR_ANYCUT111
            AIRobotErrCreateTask,               //  ERR_ANYCUT112
            AIRobotErrCreatePath,               //  ERR_ANYCUT113
            AIRobotErrCreatePoint,              //  ERR_ANYCUT114
            AIRobotErrSortRepos,                //  ERR_ANYCUT115
            AIRobotErrTouchProbe,               //  ERR_ANYCUT116
            AIRobotErrGetSepCut,                //  ERR_ANYCUT117
            AIRobotErrPostProcessor,            //  ERR_ANYCUT118
            AIRobotErrCreateMachining,          //  ERR_ANYCUT119
            AIRobotErrCreateMachiningWithNewRepos,  //  ERR_ANYCUT120
            AIRobotErrCreatePathMachining,      //  ERR_ANYCUT121
            AIRobotErrApplyExtAxes,             //  ERR_ANYCUT122
            AIRobotErrApplyDefaultToolZAngle,   //  ERR_ANYCUT123
            AIRobotErrApplyToolPosVect,         //  ERR_ANYCUT124
            AIRobotErrCorrectCollidingPointIncreaseToolOffset,  //  ERR_ANYCUT125
            AIRobotErrOptimizeJ6,               //  ERR_ANYCUT126
            AIRobotErrInvalidDH,                //  ERR_ANYCUT127
            AIRobotErrInvKineCalcErr,           //  ERR_ANYCUT128
            AIRobotErrInvKineAbort,             //  ERR_ANYCUT129
            AIRobotErrReachibility,             //  ERR_ANYCUT130
            AIRobotPathPlannerConfigErr,        //  ERR_ANYCUT131
            AIRobotPathPlannerStartColl,        //  ERR_ANYCUT132
            AIRobotPathPlannerTargetColl,       //  ERR_ANYCUT133
            AIRobotPathPlannerPathColl,         //  ERR_ANYCUT134
            AIRobotPathPlannerFailed,           //  ERR_ANYCUT135
            AIRobotApproachRetreatFailed,       //  ERR_ANYCUT136
            AIRobotApplyStitchesFailed,         //  ERR_ANYCUT137
            AIRobotApplyExtXFailed,             //  ERR_ANYCUT138
            AIRobotPieceTranslationFailed,      //  ERR_ANYCUT139
            AIRobotKinematicsFailed,            //  ERR_ANYCUT140
            AIRobotStartMissionNullLength,      //  ERR_ANYCUT141
            //  Errori Driver robot
            AIRobotDrvRobotMissing,             //  ERR_ANYCUT142
            //  Errori DrvFanuc
            AIRobotDrvFanucErrOpenFile,         //  ERR_ANYCUT143
            AIRobotDrvFanucErrConfig,           //  ERR_ANYCUT144
        }
    }
}
