using System;
abstract class QuickSortProgram
{
    public static void Main(System.String[] args)
    {
        System.Int32[] V_0;
        int V_1;
        bool V_2;
        // No-op 
        // Stack: {}
        System.String[] expr01 = args;
        // Stack: {expr01}
        int expr02 = expr01.Length;
        // Stack: {expr02}
        int expr03 = (Int32)expr02;
        // Stack: {expr03}
        System.Int32[] expr04 = new int[expr03];
        // Stack: {expr04}
        V_0 = expr04;
        // Stack: {}
        int expr0A = 0;
        // Stack: {expr0A}
        V_1 = expr0A;
        // Stack: {}
        goto IL_1F;
        // Stack: {}
        IL_0E: // No-op 
        // Stack: {}
        System.Int32[] expr0F = V_0;
        // Stack: {expr0F}
        int expr10 = V_1;
        // Stack: {expr0F, expr10}
        System.String[] expr11 = args;
        // Stack: {expr0F, expr10, expr11}
        int expr12 = V_1;
        // Stack: {expr0F, expr10, expr11, expr12}
        string expr13 = expr11[expr12];
        // Stack: {expr0F, expr10, expr13}
        int expr14 = System.Int32.Parse(expr13);
        // Stack: {expr0F, expr10, expr14}
        expr0F[expr10] = expr14;
        // Stack: {}
        // No-op 
        // Stack: {}
        int expr1B = V_1;
        // Stack: {expr1B}
        int expr1C = 1;
        // Stack: {expr1B, expr1C}
        int expr1D = expr1B + expr1C;
        // Stack: {expr1D}
        V_1 = expr1D;
        // Stack: {}
        IL_1F: int expr1F = V_1;
        // Stack: {expr1F}
        System.Int32[] expr20 = V_0;
        // Stack: {expr1F, expr20}
        int expr21 = expr20.Length;
        // Stack: {expr1F, expr21}
        int expr22 = (Int32)expr21;
        // Stack: {expr1F, expr22}
        bool expr23 = expr1F < expr22;
        // Stack: {expr23}
        V_2 = expr23;
        // Stack: {}
        bool expr26 = V_2;
        // Stack: {expr26}
        if (expr26) goto IL_0E; 
        // Stack: {}
        System.Int32[] expr29 = V_0;
        // Stack: {expr29}
        int expr2A = 0;
        // Stack: {expr29, expr2A}
        System.Int32[] expr2B = V_0;
        // Stack: {expr29, expr2A, expr2B}
        int expr2C = expr2B.Length;
        // Stack: {expr29, expr2A, expr2C}
        int expr2D = (Int32)expr2C;
        // Stack: {expr29, expr2A, expr2D}
        int expr2E = 1;
        // Stack: {expr29, expr2A, expr2D, expr2E}
        int expr2F = expr2D - expr2E;
        // Stack: {expr29, expr2A, expr2F}
        QuickSortProgram.QuickSort(expr29, expr2A, expr2F);
        // Stack: {}
        // No-op 
        // Stack: {}
        int expr36 = 0;
        // Stack: {expr36}
        V_1 = expr36;
        // Stack: {}
        goto IL_5C;
        // Stack: {}
        IL_3A: // No-op 
        // Stack: {}
        System.Int32[] expr3B = V_0;
        // Stack: {expr3B}
        int expr3C = V_1;
        // Stack: {expr3B, expr3C}
        object expr3D = expr3B[expr3C];
        // Stack: {expr3D}
        string expr42 = expr3D.ToString();
        // Stack: {expr42}
        string expr47 = " ";
        // Stack: {expr42, expr47}
        string expr4C = System.String.Concat(expr42, expr47);
        // Stack: {expr4C}
        System.Console.Write(expr4C);
        // Stack: {}
        // No-op 
        // Stack: {}
        // No-op 
        // Stack: {}
        int expr58 = V_1;
        // Stack: {expr58}
        int expr59 = 1;
        // Stack: {expr58, expr59}
        int expr5A = expr58 + expr59;
        // Stack: {expr5A}
        V_1 = expr5A;
        // Stack: {}
        IL_5C: int expr5C = V_1;
        // Stack: {expr5C}
        System.Int32[] expr5D = V_0;
        // Stack: {expr5C, expr5D}
        int expr5E = expr5D.Length;
        // Stack: {expr5C, expr5E}
        int expr5F = (Int32)expr5E;
        // Stack: {expr5C, expr5F}
        bool expr60 = expr5C < expr5F;
        // Stack: {expr60}
        V_2 = expr60;
        // Stack: {}
        bool expr63 = V_2;
        // Stack: {expr63}
        if (expr63) goto IL_3A; 
        // Stack: {}
        return;
        // Stack: {}
    }
    public static void QuickSort(System.Int32[] array, int left, int right)
    {
        int V_0;
        int V_1;
        bool V_2;
        // No-op 
        // Stack: {}
        int expr01 = right;
        // Stack: {expr01}
        int expr02 = left;
        // Stack: {expr01, expr02}
        bool expr03 = expr01 > expr02;
        // Stack: {expr03}
        int expr05 = 0;
        // Stack: {expr03, expr05}
        bool expr06 = expr03 == (expr05 != 0);
        // Stack: {expr06}
        V_2 = expr06;
        // Stack: {}
        bool expr09 = V_2;
        // Stack: {expr09}
        if (expr09) goto IL_34; 
        // Stack: {}
        // No-op 
        // Stack: {}
        int expr0D = left;
        // Stack: {expr0D}
        int expr0E = right;
        // Stack: {expr0D, expr0E}
        int expr0F = expr0D + expr0E;
        // Stack: {expr0F}
        int expr10 = 2;
        // Stack: {expr0F, expr10}
        int expr11 = expr0F / expr10;
        // Stack: {expr11}
        V_0 = expr11;
        // Stack: {}
        System.Int32[] expr13 = array;
        // Stack: {expr13}
        int expr14 = left;
        // Stack: {expr13, expr14}
        int expr15 = right;
        // Stack: {expr13, expr14, expr15}
        int expr16 = V_0;
        // Stack: {expr13, expr14, expr15, expr16}
        int expr17 = QuickSortProgram.Partition(expr13, expr14, expr15, expr16);
        // Stack: {expr17}
        V_1 = expr17;
        // Stack: {}
        System.Int32[] expr1D = array;
        // Stack: {expr1D}
        int expr1E = left;
        // Stack: {expr1D, expr1E}
        int expr1F = V_1;
        // Stack: {expr1D, expr1E, expr1F}
        int expr20 = 1;
        // Stack: {expr1D, expr1E, expr1F, expr20}
        int expr21 = expr1F - expr20;
        // Stack: {expr1D, expr1E, expr21}
        QuickSortProgram.QuickSort(expr1D, expr1E, expr21);
        // Stack: {}
        // No-op 
        // Stack: {}
        System.Int32[] expr28 = array;
        // Stack: {expr28}
        int expr29 = V_1;
        // Stack: {expr28, expr29}
        int expr2A = 1;
        // Stack: {expr28, expr29, expr2A}
        int expr2B = expr29 + expr2A;
        // Stack: {expr28, expr2B}
        int expr2C = right;
        // Stack: {expr28, expr2B, expr2C}
        QuickSortProgram.QuickSort(expr28, expr2B, expr2C);
        // Stack: {}
        // No-op 
        // Stack: {}
        // No-op 
        // Stack: {}
        IL_34: return;
        // Stack: {}
    }
    private static int Partition(System.Int32[] array, int left, int right, int pivotIndex)
    {
        int V_0;
        int V_1;
        int V_2;
        int V_3;
        bool V_4;
        // No-op 
        // Stack: {}
        System.Int32[] expr01 = array;
        // Stack: {expr01}
        int expr02 = pivotIndex;
        // Stack: {expr01, expr02}
        int expr03 = expr01[expr02];
        // Stack: {expr03}
        V_0 = expr03;
        // Stack: {}
        System.Int32[] expr05 = array;
        // Stack: {expr05}
        int expr06 = pivotIndex;
        // Stack: {expr05, expr06}
        int expr07 = right;
        // Stack: {expr05, expr06, expr07}
        QuickSortProgram.Swap(expr05, expr06, expr07);
        // Stack: {}
        // No-op 
        // Stack: {}
        int expr0E = left;
        // Stack: {expr0E}
        V_1 = expr0E;
        // Stack: {}
        int expr10 = left;
        // Stack: {expr10}
        V_2 = expr10;
        // Stack: {}
        goto IL_35;
        // Stack: {}
        IL_14: // No-op 
        // Stack: {}
        System.Int32[] expr15 = array;
        // Stack: {expr15}
        int expr16 = V_2;
        // Stack: {expr15, expr16}
        int expr17 = expr15[expr16];
        // Stack: {expr17}
        int expr18 = V_0;
        // Stack: {expr17, expr18}
        bool expr19 = expr17 > expr18;
        // Stack: {expr19}
        V_4 = expr19;
        // Stack: {}
        bool expr1D = V_4;
        // Stack: {expr1D}
        if (expr1D) goto IL_30; 
        // Stack: {}
        // No-op 
        // Stack: {}
        System.Int32[] expr22 = array;
        // Stack: {expr22}
        int expr23 = V_1;
        // Stack: {expr22, expr23}
        int expr24 = V_2;
        // Stack: {expr22, expr23, expr24}
        QuickSortProgram.Swap(expr22, expr23, expr24);
        // Stack: {}
        // No-op 
        // Stack: {}
        int expr2B = V_1;
        // Stack: {expr2B}
        int expr2C = 1;
        // Stack: {expr2B, expr2C}
        int expr2D = expr2B + expr2C;
        // Stack: {expr2D}
        V_1 = expr2D;
        // Stack: {}
        // No-op 
        // Stack: {}
        IL_30: // No-op 
        // Stack: {}
        int expr31 = V_2;
        // Stack: {expr31}
        int expr32 = 1;
        // Stack: {expr31, expr32}
        int expr33 = expr31 + expr32;
        // Stack: {expr33}
        V_2 = expr33;
        // Stack: {}
        IL_35: int expr35 = V_2;
        // Stack: {expr35}
        int expr36 = right;
        // Stack: {expr35, expr36}
        bool expr37 = expr35 < expr36;
        // Stack: {expr37}
        V_4 = expr37;
        // Stack: {}
        bool expr3B = V_4;
        // Stack: {expr3B}
        if (expr3B) goto IL_14; 
        // Stack: {}
        System.Int32[] expr3F = array;
        // Stack: {expr3F}
        int expr40 = right;
        // Stack: {expr3F, expr40}
        int expr41 = V_1;
        // Stack: {expr3F, expr40, expr41}
        QuickSortProgram.Swap(expr3F, expr40, expr41);
        // Stack: {}
        // No-op 
        // Stack: {}
        int expr48 = V_1;
        // Stack: {expr48}
        V_3 = expr48;
        // Stack: {}
        goto IL_4C;
        // Stack: {}
        IL_4C: int expr4C = V_3;
        // Stack: {expr4C}
        return expr4C;
        // Stack: {}
    }
    private static void Swap(System.Int32[] array, int index1, int index2)
    {
        int V_0;
        // No-op 
        // Stack: {}
        System.Int32[] expr01 = array;
        // Stack: {expr01}
        int expr02 = index1;
        // Stack: {expr01, expr02}
        int expr03 = expr01[expr02];
        // Stack: {expr03}
        V_0 = expr03;
        // Stack: {}
        System.Int32[] expr05 = array;
        // Stack: {expr05}
        int expr06 = index1;
        // Stack: {expr05, expr06}
        System.Int32[] expr07 = array;
        // Stack: {expr05, expr06, expr07}
        int expr08 = index2;
        // Stack: {expr05, expr06, expr07, expr08}
        int expr09 = expr07[expr08];
        // Stack: {expr05, expr06, expr09}
        expr05[expr06] = expr09;
        // Stack: {}
        System.Int32[] expr0B = array;
        // Stack: {expr0B}
        int expr0C = index2;
        // Stack: {expr0B, expr0C}
        int expr0D = V_0;
        // Stack: {expr0B, expr0C, expr0D}
        expr0B[expr0C] = expr0D;
        // Stack: {}
        return;
        // Stack: {}
    }
}
