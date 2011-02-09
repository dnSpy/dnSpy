using System;
abstract class QuickSortProgram
{
    public static void Main(System.String[] args)
    {
        BasicBlock_1:
        System.Int32[] V_0 = new int[((int)args.Length)];
        int i = 0;
        goto BasicBlock_3;
        BasicBlock_2:
        V_0[i] = System.Int32.Parse(args[i]);
        i = (i + 1);
        goto BasicBlock_3;
        BasicBlock_3:
        if (i < ((int)V_0.Length)) goto BasicBlock_2; 
        goto BasicBlock_4;
        BasicBlock_4:
        QuickSortProgram.QuickSort(V_0, 0, (((int)V_0.Length) - 1));
        int j = 0;
        goto BasicBlock_6;
        BasicBlock_5:
        System.Console.Write(System.String.Concat((V_0[j]).ToString(), " "));
        j = (j + 1);
        goto BasicBlock_6;
        BasicBlock_6:
        if (j < ((int)V_0.Length)) goto BasicBlock_5; 
        goto BasicBlock_7;
        BasicBlock_7:
        return;
    }
    public static void QuickSort(System.Int32[] array, int left, int right)
    {
        BasicBlock_9:
        if (right <= left) goto BasicBlock_11; 
        goto BasicBlock_10;
        BasicBlock_10:
        int i = ((left + right) / 2);
        int j = QuickSortProgram.Partition(array, left, right, i);
        QuickSortProgram.QuickSort(array, left, (j - 1));
        QuickSortProgram.QuickSort(array, (j + 1), right);
        goto BasicBlock_11;
        BasicBlock_11:
        return;
    }
    private static int Partition(System.Int32[] array, int left, int right, int pivotIndex)
    {
        BasicBlock_13:
        int i = array[pivotIndex];
        QuickSortProgram.Swap(array, pivotIndex, right);
        int j = left;
        int k = left;
        goto BasicBlock_17;
        BasicBlock_14:
        if (array[k] > i) goto BasicBlock_16; 
        goto BasicBlock_15;
        BasicBlock_15:
        QuickSortProgram.Swap(array, j, k);
        j = (j + 1);
        goto BasicBlock_16;
        BasicBlock_16:
        k = (k + 1);
        goto BasicBlock_17;
        BasicBlock_17:
        if (k < right) goto BasicBlock_14; 
        goto BasicBlock_18;
        BasicBlock_18:
        QuickSortProgram.Swap(array, right, j);
        return j;
    }
    private static void Swap(System.Int32[] array, int index1, int index2)
    {
        BasicBlock_20:
        int i = array[index1];
        array[index1] = array[index2];
        array[index2] = i;
        return;
    }
}
