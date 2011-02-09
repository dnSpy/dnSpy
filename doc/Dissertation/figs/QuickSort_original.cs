using System;
static class QuickSortProgram
{
    public static void Main(string[] args)
    {
        int[] intArray = new int[args.Length];
        for (int i = 0; i < intArray.Length; i++) {
            intArray[i] = int.Parse(args[i]);
        }
        QuickSort(intArray, 0, intArray.Length - 1);
        for (int i = 0; i < intArray.Length; i++) {
            Console.Write(intArray[i].ToString() + " ");
        }
    }
    public static void QuickSort(int[] array, int left, int right)
    {
        if (right > left) {
            int pivotIndex = (left + right) / 2;
            int pivotNew = Partition(array, left, right, pivotIndex);
            QuickSort(array, left, pivotNew - 1);
            QuickSort(array, pivotNew + 1, right);
        }
    }
    static int Partition(int[] array, int left, int right, int pivotIndex)
    {
        int pivotValue = array[pivotIndex];
        Swap(array, pivotIndex, right);
        int storeIndex = left;
        for(int i = left; i < right; i++) {
            if (array[i] <= pivotValue) {
                Swap(array, storeIndex, i);
                storeIndex = storeIndex + 1;
            }
        }
        Swap(array, right, storeIndex);
        return storeIndex;
    }
    static void Swap(int[] array, int index1, int index2)
    {
        int tmp = array[index1];
        array[index1] = array[index2];
        array[index2] = tmp;
    }
}
