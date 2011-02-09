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
            ConditionalNode_16:
            BasicBlock_3:
            if (!(i < ((int)V_0.Length))) {
                break;
                Block_14:
            }
            else {
                goto BasicBlock_2;
                Block_15:
            }
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
            ConditionalNode_19:
            BasicBlock_6:
            if (!(j < ((int)V_0.Length))) {
                break;
                Block_17:
            }
            else {
                goto BasicBlock_5;
                Block_18:
            }
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
        ConditionalNode_28:
        BasicBlock_21:
        if (!(right <= left)) {
            goto Block_26;
            Block_26:
            BasicBlock_22:
            int i = ((left + right) / 2);
            int j = QuickSortProgram.Partition(array, left, right, i);
            QuickSortProgram.QuickSort(array, left, (j - 1));
            QuickSortProgram.QuickSort(array, (j + 1), right);
            goto BasicBlock_23;
        }
        else {
            goto BasicBlock_23;
            Block_27:
        }
        BasicBlock_23:
        return;
    }
    private static int Partition(System.Int32[] array, int left, int right, int pivotIndex)
    {
        BasicBlock_30:
        int i = array[pivotIndex];
        QuickSortProgram.Swap(array, pivotIndex, right);
        int j = left;
        int k = left;
        goto Loop_38;
        Loop_38:
        for (;;) {
            ConditionalNode_43:
            BasicBlock_34:
            if (!(k < right)) {
                break;
                Block_41:
            }
            else {
                goto ConditionalNode_46;
                Block_42:
            }
            ConditionalNode_46:
            BasicBlock_31:
            if (!(array[k] > i)) {
                goto Block_44;
                Block_44:
                BasicBlock_32:
                QuickSortProgram.Swap(array, j, k);
                j = (j + 1);
                goto BasicBlock_33;
            }
            else {
                goto BasicBlock_33;
                Block_45:
            }
            BasicBlock_33:
            k = (k + 1);
            continue;
        }
        BasicBlock_35:
        QuickSortProgram.Swap(array, right, j);
        return j;
    }
    private static void Swap(System.Int32[] array, int index1, int index2)
    {
        BasicBlock_48:
        int i = array[index1];
        array[index1] = array[index2];
        array[index2] = i;
        return;
    }
}
