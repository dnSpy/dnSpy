abstract class QuickSortProgram
{
    public static void Main(System.String[] args)
    {
        IL_00: nop                # Pop0->Push0   
        IL_01: ldarg args         # Pop0->Push1   
        IL_02: ldlen              # Popref->Pushi   
        IL_03: conv.i4            # Pop1->Pushi   
        IL_04: newarr System.Int32# Popi->Pushref   
        IL_09: stloc V_0          # Pop1->Push0   
        IL_0A: ldc.i4 0           # Pop0->Pushi   
        IL_0B: stloc V_1          # Pop1->Push0   
        IL_0C: br IL_1F           # Pop0->Push0 Flow=Branch  
        IL_0E: nop                # Pop0->Push0   
        IL_0F: ldloc V_0          # Pop0->Push1   
        IL_10: ldloc V_1          # Pop0->Push1   
        IL_11: ldarg args         # Pop0->Push1   
        IL_12: ldloc V_1          # Pop0->Push1   
        IL_13: ldelem.ref         # Popref_popi->Pushref   
        IL_14: call Parse()       # Varpop->Varpush Flow=Call  
        IL_19: stelem.i4          # Popref_popi_popi->Push0   
        IL_1A: nop                # Pop0->Push0   
        IL_1B: ldloc V_1          # Pop0->Push1   
        IL_1C: ldc.i4 1           # Pop0->Pushi   
        IL_1D: add.ovf            # Pop1_pop1->Push1   
        IL_1E: stloc V_1          # Pop1->Push0   
        IL_1F: ldloc V_1          # Pop0->Push1   
        IL_20: ldloc V_0          # Pop0->Push1   
        IL_21: ldlen              # Popref->Pushi   
        IL_22: conv.i4            # Pop1->Pushi   
        IL_23: clt                # Pop1_pop1->Pushi   
        IL_25: stloc V_2          # Pop1->Push0   
        IL_26: ldloc V_2          # Pop0->Push1   
        IL_27: brtrue IL_0E       # Popi->Push0 Flow=Cond_Branch  
        IL_29: ldloc V_0          # Pop0->Push1   
        IL_2A: ldc.i4 0           # Pop0->Pushi   
        IL_2B: ldloc V_0          # Pop0->Push1   
        IL_2C: ldlen              # Popref->Pushi   
        IL_2D: conv.i4            # Pop1->Pushi   
        IL_2E: ldc.i4 1           # Pop0->Pushi   
        IL_2F: sub.ovf            # Pop1_pop1->Push1   
        IL_30: call QuickSort()   # Varpop->Varpush Flow=Call  
        IL_35: nop                # Pop0->Push0   
        IL_36: ldc.i4 0           # Pop0->Pushi   
        IL_37: stloc V_1          # Pop1->Push0   
        IL_38: br IL_5C           # Pop0->Push0 Flow=Branch  
        IL_3A: nop                # Pop0->Push0   
        IL_3B: ldloc V_0          # Pop0->Push1   
        IL_3C: ldloc V_1          # Pop0->Push1   
        IL_3D: ldelema System.Int32# Popref_popi->Pushi   
        IL_42: call ToString()    # Varpop->Varpush Flow=Call  
        IL_47: ldstr \" \"        # Pop0->Pushref   
        IL_4C: call Concat()      # Varpop->Varpush Flow=Call  
        IL_51: call Write()       # Varpop->Varpush Flow=Call  
        IL_56: nop                # Pop0->Push0   
        IL_57: nop                # Pop0->Push0   
        IL_58: ldloc V_1          # Pop0->Push1   
        IL_59: ldc.i4 1           # Pop0->Pushi   
        IL_5A: add.ovf            # Pop1_pop1->Push1   
        IL_5B: stloc V_1          # Pop1->Push0   
        IL_5C: ldloc V_1          # Pop0->Push1   
        IL_5D: ldloc V_0          # Pop0->Push1   
        IL_5E: ldlen              # Popref->Pushi   
        IL_5F: conv.i4            # Pop1->Pushi   
        IL_60: clt                # Pop1_pop1->Pushi   
        IL_62: stloc V_2          # Pop1->Push0   
        IL_63: ldloc V_2          # Pop0->Push1   
        IL_64: brtrue IL_3A       # Popi->Push0 Flow=Cond_Branch  
        IL_66: ret                # Varpop->Push0 Flow=Return  
    }
    public static void QuickSort(System.Int32[] array, int left, int right)
    {
        IL_00: nop                # Pop0->Push0   
        IL_01: ldarg right        # Pop0->Push1   
        IL_02: ldarg left         # Pop0->Push1   
        IL_03: cgt                # Pop1_pop1->Pushi   
        IL_05: ldc.i4 0           # Pop0->Pushi   
        IL_06: ceq                # Pop1_pop1->Pushi   
        IL_08: stloc V_2          # Pop1->Push0   
        IL_09: ldloc V_2          # Pop0->Push1   
        IL_0A: brtrue IL_34       # Popi->Push0 Flow=Cond_Branch  
        IL_0C: nop                # Pop0->Push0   
        IL_0D: ldarg left         # Pop0->Push1   
        IL_0E: ldarg right        # Pop0->Push1   
        IL_0F: add.ovf            # Pop1_pop1->Push1   
        IL_10: ldc.i4 2           # Pop0->Pushi   
        IL_11: div                # Pop1_pop1->Push1   
        IL_12: stloc V_0          # Pop1->Push0   
        IL_13: ldarg array        # Pop0->Push1   
        IL_14: ldarg left         # Pop0->Push1   
        IL_15: ldarg right        # Pop0->Push1   
        IL_16: ldloc V_0          # Pop0->Push1   
        IL_17: call Partition()   # Varpop->Varpush Flow=Call  
        IL_1C: stloc V_1          # Pop1->Push0   
        IL_1D: ldarg array        # Pop0->Push1   
        IL_1E: ldarg left         # Pop0->Push1   
        IL_1F: ldloc V_1          # Pop0->Push1   
        IL_20: ldc.i4 1           # Pop0->Pushi   
        IL_21: sub.ovf            # Pop1_pop1->Push1   
        IL_22: call QuickSort()   # Varpop->Varpush Flow=Call  
        IL_27: nop                # Pop0->Push0   
        IL_28: ldarg array        # Pop0->Push1   
        IL_29: ldloc V_1          # Pop0->Push1   
        IL_2A: ldc.i4 1           # Pop0->Pushi   
        IL_2B: add.ovf            # Pop1_pop1->Push1   
        IL_2C: ldarg right        # Pop0->Push1   
        IL_2D: call QuickSort()   # Varpop->Varpush Flow=Call  
        IL_32: nop                # Pop0->Push0   
        IL_33: nop                # Pop0->Push0   
        IL_34: ret                # Varpop->Push0 Flow=Return  
    }
    private static int Partition(System.Int32[] array, int left, int right, int pivotIndex)
    {
        IL_00: nop                # Pop0->Push0   
        IL_01: ldarg array        # Pop0->Push1   
        IL_02: ldarg pivotIndex   # Pop0->Push1   
        IL_03: ldelem.i4          # Popref_popi->Pushi   
        IL_04: stloc V_0          # Pop1->Push0   
        IL_05: ldarg array        # Pop0->Push1   
        IL_06: ldarg pivotIndex   # Pop0->Push1   
        IL_07: ldarg right        # Pop0->Push1   
        IL_08: call Swap()        # Varpop->Varpush Flow=Call  
        IL_0D: nop                # Pop0->Push0   
        IL_0E: ldarg left         # Pop0->Push1   
        IL_0F: stloc V_1          # Pop1->Push0   
        IL_10: ldarg left         # Pop0->Push1   
        IL_11: stloc V_2          # Pop1->Push0   
        IL_12: br IL_35           # Pop0->Push0 Flow=Branch  
        IL_14: nop                # Pop0->Push0   
        IL_15: ldarg array        # Pop0->Push1   
        IL_16: ldloc V_2          # Pop0->Push1   
        IL_17: ldelem.i4          # Popref_popi->Pushi   
        IL_18: ldloc V_0          # Pop0->Push1   
        IL_19: cgt                # Pop1_pop1->Pushi   
        IL_1B: stloc V_4          # Pop1->Push0   
        IL_1D: ldloc V_4          # Pop0->Push1   
        IL_1F: brtrue IL_30       # Popi->Push0 Flow=Cond_Branch  
        IL_21: nop                # Pop0->Push0   
        IL_22: ldarg array        # Pop0->Push1   
        IL_23: ldloc V_1          # Pop0->Push1   
        IL_24: ldloc V_2          # Pop0->Push1   
        IL_25: call Swap()        # Varpop->Varpush Flow=Call  
        IL_2A: nop                # Pop0->Push0   
        IL_2B: ldloc V_1          # Pop0->Push1   
        IL_2C: ldc.i4 1           # Pop0->Pushi   
        IL_2D: add.ovf            # Pop1_pop1->Push1   
        IL_2E: stloc V_1          # Pop1->Push0   
        IL_2F: nop                # Pop0->Push0   
        IL_30: nop                # Pop0->Push0   
        IL_31: ldloc V_2          # Pop0->Push1   
        IL_32: ldc.i4 1           # Pop0->Pushi   
        IL_33: add.ovf            # Pop1_pop1->Push1   
        IL_34: stloc V_2          # Pop1->Push0   
        IL_35: ldloc V_2          # Pop0->Push1   
        IL_36: ldarg right        # Pop0->Push1   
        IL_37: clt                # Pop1_pop1->Pushi   
        IL_39: stloc V_4          # Pop1->Push0   
        IL_3B: ldloc V_4          # Pop0->Push1   
        IL_3D: brtrue IL_14       # Popi->Push0 Flow=Cond_Branch  
        IL_3F: ldarg array        # Pop0->Push1   
        IL_40: ldarg right        # Pop0->Push1   
        IL_41: ldloc V_1          # Pop0->Push1   
        IL_42: call Swap()        # Varpop->Varpush Flow=Call  
        IL_47: nop                # Pop0->Push0   
        IL_48: ldloc V_1          # Pop0->Push1   
        IL_49: stloc V_3          # Pop1->Push0   
        IL_4A: br IL_4C           # Pop0->Push0 Flow=Branch  
        IL_4C: ldloc V_3          # Pop0->Push1   
        IL_4D: ret                # Varpop->Push0 Flow=Return  
    }
    private static void Swap(System.Int32[] array, int index1, int index2)
    {
        IL_00: nop                # Pop0->Push0   
        IL_01: ldarg array        # Pop0->Push1   
        IL_02: ldarg index1       # Pop0->Push1   
        IL_03: ldelem.i4          # Popref_popi->Pushi   
        IL_04: stloc V_0          # Pop1->Push0   
        IL_05: ldarg array        # Pop0->Push1   
        IL_06: ldarg index1       # Pop0->Push1   
        IL_07: ldarg array        # Pop0->Push1   
        IL_08: ldarg index2       # Pop0->Push1   
        IL_09: ldelem.i4          # Popref_popi->Pushi   
        IL_0A: stelem.i4          # Popref_popi_popi->Push0   
        IL_0B: ldarg array        # Pop0->Push1   
        IL_0C: ldarg index2       # Pop0->Push1   
        IL_0D: ldloc V_0          # Pop0->Push1   
        IL_0E: stelem.i4          # Popref_popi_popi->Push0   
        IL_0F: ret                # Varpop->Push0 Flow=Return  
    }
}
