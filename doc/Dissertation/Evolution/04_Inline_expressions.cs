using System;
abstract class QuickSortProgram
{
    public static void Main(System.String[] args)
    {
        System.Int32[] V_0 = new int[((Int32)args.Length)];
        int V_1 = 0;
        goto IL_1C;
        IL_0D: V_0[V_1] = System.Int32.Parse(args[V_1]);
        V_1 = (V_1 + 1);
        IL_1C: if (V_1 < ((Int32)V_0.Length)) goto IL_0D; 
        QuickSortProgram.QuickSort(V_0, 0, (((Int32)V_0.Length) - 1));
        int V_2 = 0;
        goto IL_51;
        IL_32: System.Console.Write(System.String.Concat((V_0[V_2]).ToString(), " "));
        V_2 = (V_2 + 1);
        IL_51: if (V_2 < ((Int32)V_0.Length)) goto IL_32; 
    }
    public static void QuickSort(System.Int32[] array, int left, int right)
    {
        if (right <= left) goto IL_28; 
        int V_0 = ((left + right) / 2);
        int V_1 = QuickSortProgram.Partition(array, left, right, V_0);
        QuickSortProgram.QuickSort(array, left, (V_1 - 1));
        QuickSortProgram.QuickSort(array, (V_1 + 1), right);
        IL_28: return;
    }
    private static int Partition(System.Int32[] array, int left, int right, int pivotIndex)
    {
        int V_0 = array[pivotIndex];
        QuickSortProgram.Swap(array, pivotIndex, right);
        int V_1 = left;
        int V_2 = left;
        goto IL_28;
        IL_12: if (array[V_2] > V_0) goto IL_24; 
        QuickSortProgram.Swap(array, V_1, V_2);
        V_1 = (V_1 + 1);
        IL_24: V_2 = (V_2 + 1);
        IL_28: if (V_2 < right) goto IL_12; 
        QuickSortProgram.Swap(array, right, V_1);
        return V_1;
    }
    private static void Swap(System.Int32[] array, int index1, int index2)
    {
        int V_0 = array[index1];
        array[index1] = array[index2];
        array[index2] = V_0;
    }
}