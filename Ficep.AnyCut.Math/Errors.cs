
namespace Ficep.AnyCut.Mathematics
{
    public static class Errors
    {
        public enum MathErr : uint
        { 
            MATH_PROC_OK = 0, 
            MATH_ERR_R_LEN = 301, 
            MATH_ERR_C_NULL = 302, 
            MATH_ERR_NO_INTERSEZ = 303, 
            MATH_ERR_CEN_COIN = 304, 
            MATH_ERR_L_C_INT = 305, 
            MATH_ERR_C_C_INT = 306, 
            MATH_ERR_DATA = 307 
        }
        public enum ParserErr : uint 
        {
            PARSER_NO_ERR, 
            PARSER_ERR_NOINI, 
            PARSER_ERR_MEM, 
            PARSER_ERR_DIV_BY0, 
            PARSER_ERR_PAR, 
            PARSER_ERR_UNKNOWN, 
            PARSER_ERR_OPERANDO, 
            PARSER_ERR_CMD_MISSING, 
            PARSER_ERR_OPE_MISSING, 
            PARSER_ERR_DOMINIO, 
            PARSER_ERR_OVERFLOW 
        }
    }
}
