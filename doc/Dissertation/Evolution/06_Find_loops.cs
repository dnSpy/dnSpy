using System;
abstract class QuickSortProgram
{
    public static void Main(System.String[] args)
    {
        BasicBlock_1:
        System.Int32[] V_0 = new int[((int)args.Length)];
        int i = 0;
        goto Loop_8;
        Loop_8:
        for (;;) {
            BasicBlock_3:
            if (i < ((int)V_0.Length)) goto BasicBlock_2; 
            break;
            BasicBlock_2:
            V_0[i] = System.Int32.Parse(args[i]);
            i = (i + 1);
            continue;
        }
        BasicBlock_4:
        QuickSortProgram.QuickSort(V_0, 0, (((int)V_0.Length) - 1));
        int j = 0;
        goto Loop_11;
        Loop_11:
        for (;;) {
            BasicBlock_6:
            if (j < ((int)V_0.Length)) goto BasicBlock_5; 
            break;
            BasicBlock_5:
            System.Console.Write(System.String.Concat((V_0[j]).ToString(), " "));
            j = (j + 1);
            continue;
        }
        BasicBlock_7:
        return;
    }
    public static void QuickSort(System.Int32[] array, int left, int right)
    {
        BasicBlock_15:
        if (right <= left) goto BasicBlock_17; 
        goto BasicBlock_16;
        BasicBlock_16:
        int i = ((left + right) / 2);
        int j = QuickSortProgram.Partition(array, left, right, i);
        QuickSortProgram.QuickSort(array, left, (j - 1));
        QuickSortProgram.QuickSort(array, (j + 1), right);
        goto BasicBlock_17;
        BasicBlock_17:
        return;
    }
    private static int Partition(System.Int32[] array, int left, int right, int pivotIndex)
    {
        BasicBlock_21:
        int i = array[pivotIndex];
        QuickSortProgram.Swap(array, pivotIndex, right);
        int j = left;
        int k = left;
        goto Loop_29;
        Loop_29:
        for (;;) {
            BasicBlock_25:
            if (k < right) goto BasicBlock_22; 
            break;
            BasicBlock_22:
            if (array[k] > i) goto BasicBlock_24; 
            goto BasicBlock_23;
            BasicBlock_23:
            QuickSortProgram.Swap(array, j, k);
            j = (j + 1);
            goto BasicBlock_24;
            BasicBlock_24:
            k = (k + 1);
            continue;
        }
        BasicBlock_26:
        QuickSortProgram.Swap(array, right, j);
        return j;
    }
    private static void Swap(System.Int32[] array, int index1, int index2)
    {
        BasicBlock_33:
        int i = array[index1];
        array[index1] = array[index2];
        array[index2] = i;
        return;
    }
}
