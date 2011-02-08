using System;
abstract class QuickSortProgram
{
    public static void Main(System.String[] args)
    {
        System.Int32[] V_0;
        int V_1;
        bool V_2;
        // No-op 
        System.String[] expr01 = args;
        int expr02 = expr01.Length;
        int expr03 = (Int32)expr02;
        System.Int32[] expr04 = new int[expr03];
        V_0 = expr04;
        int expr0A = 0;
        V_1 = expr0A;
        goto IL_1F;
        IL_0E: // No-op 
        System.Int32[] expr0F = V_0;
        int expr10 = V_1;
        System.String[] expr11 = args;
        int expr12 = V_1;
        string expr13 = expr11[expr12];
        int expr14 = System.Int32.Parse(expr13);
        expr0F[expr10] = expr14;
        // No-op 
        int expr1B = V_1;
        int expr1C = 1;
        int expr1D = expr1B + expr1C;
        V_1 = expr1D;
        IL_1F: int expr1F = V_1;
        System.Int32[] expr20 = V_0;
        int expr21 = expr20.Length;
        int expr22 = (Int32)expr21;
        bool expr23 = expr1F < expr22;
        V_2 = expr23;
        bool expr26 = V_2;
        if (expr26) goto IL_0E; 
        System.Int32[] expr29 = V_0;
        int expr2A = 0;
        System.Int32[] expr2B = V_0;
        int expr2C = expr2B.Length;
        int expr2D = (Int32)expr2C;
        int expr2E = 1;
        int expr2F = expr2D - expr2E;
        QuickSortProgram.QuickSort(expr29, expr2A, expr2F);
        // No-op 
        int expr36 = 0;
        V_1 = expr36;
        goto IL_5C;
        IL_3A: // No-op 
        System.Int32[] expr3B = V_0;
        int expr3C = V_1;
        object expr3D = expr3B[expr3C];
        string expr42 = expr3D.ToString();
        string expr47 = " ";
        string expr4C = System.String.Concat(expr42, expr47);
        System.Console.Write(expr4C);
        // No-op 
        // No-op 
        int expr58 = V_1;
        int expr59 = 1;
        int expr5A = expr58 + expr59;
        V_1 = expr5A;
        IL_5C: int expr5C = V_1;
        System.Int32[] expr5D = V_0;
        int expr5E = expr5D.Length;
        int expr5F = (Int32)expr5E;
        bool expr60 = expr5C < expr5F;
        V_2 = expr60;
        bool expr63 = V_2;
        if (expr63) goto IL_3A; 
        return;
    }
    public static void QuickSort(System.Int32[] array, int left, int right)
    {
        int V_0;
        int V_1;
        bool V_2;
        // No-op 
        int expr01 = right;
        int expr02 = left;
        bool expr03 = expr01 > expr02;
        int expr05 = 0;
        bool expr06 = expr03 == (expr05 != 0);
        V_2 = expr06;
        bool expr09 = V_2;
        if (expr09) goto IL_34; 
        // No-op 
        int expr0D = left;
        int expr0E = right;
        int expr0F = expr0D + expr0E;
        int expr10 = 2;
        int expr11 = expr0F / expr10;
        V_0 = expr11;
        System.Int32[] expr13 = array;
        int expr14 = left;
        int expr15 = right;
        int expr16 = V_0;
        int expr17 = QuickSortProgram.Partition(expr13, expr14, expr15, expr16);
        V_1 = expr17;
        System.Int32[] expr1D = array;
        int expr1E = left;
        int expr1F = V_1;
        int expr20 = 1;
        int expr21 = expr1F - expr20;
        QuickSortProgram.QuickSort(expr1D, expr1E, expr21);
        // No-op 
        System.Int32[] expr28 = array;
        int expr29 = V_1;
        int expr2A = 1;
        int expr2B = expr29 + expr2A;
        int expr2C = right;
        QuickSortProgram.QuickSort(expr28, expr2B, expr2C);
        // No-op 
        // No-op 
        IL_34: return;
    }
    private static int Partition(System.Int32[] array, int left, int right, int pivotIndex)
    {
        int V_0;
        int V_1;
        int V_2;
        int V_3;
        bool V_4;
        // No-op 
        System.Int32[] expr01 = array;
        int expr02 = pivotIndex;
        int expr03 = expr01[expr02];
        V_0 = expr03;
        System.Int32[] expr05 = array;
        int expr06 = pivotIndex;
        int expr07 = right;
        QuickSortProgram.Swap(expr05, expr06, expr07);
        // No-op 
        int expr0E = left;
        V_1 = expr0E;
        int expr10 = left;
        V_2 = expr10;
        goto IL_35;
        IL_14: // No-op 
        System.Int32[] expr15 = array;
        int expr16 = V_2;
        int expr17 = expr15[expr16];
        int expr18 = V_0;
        bool expr19 = expr17 > expr18;
        V_4 = expr19;
        bool expr1D = V_4;
        if (expr1D) goto IL_30; 
        // No-op 
        System.Int32[] expr22 = array;
        int expr23 = V_1;
        int expr24 = V_2;
        QuickSortProgram.Swap(expr22, expr23, expr24);
        // No-op 
        int expr2B = V_1;
        int expr2C = 1;
        int expr2D = expr2B + expr2C;
        V_1 = expr2D;
        // No-op 
        IL_30: // No-op 
        int expr31 = V_2;
        int expr32 = 1;
        int expr33 = expr31 + expr32;
        V_2 = expr33;
        IL_35: int expr35 = V_2;
        int expr36 = right;
        bool expr37 = expr35 < expr36;
        V_4 = expr37;
        bool expr3B = V_4;
        if (expr3B) goto IL_14; 
        System.Int32[] expr3F = array;
        int expr40 = right;
        int expr41 = V_1;
        QuickSortProgram.Swap(expr3F, expr40, expr41);
        // No-op 
        int expr48 = V_1;
        V_3 = expr48;
        goto IL_4C;
        IL_4C: int expr4C = V_3;
        return expr4C;
    }
    private static void Swap(System.Int32[] array, int index1, int index2)
    {
        int V_0;
        // No-op 
        System.Int32[] expr01 = array;
        int expr02 = index1;
        int expr03 = expr01[expr02];
        V_0 = expr03;
        System.Int32[] expr05 = array;
        int expr06 = index1;
        System.Int32[] expr07 = array;
        int expr08 = index2;
        int expr09 = expr07[expr08];
        expr05[expr06] = expr09;
        System.Int32[] expr0B = array;
        int expr0C = index2;
        int expr0D = V_0;
        expr0B[expr0C] = expr0D;
        return;
    }
}
