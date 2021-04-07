using dnlib.DotNet.Emit;
using System;
using System.Linq;
using UnSealer.Core;
using UnSealer.Core.Utils.Dnlib.CawkRuntime.ConversionBack;

namespace UnSealer.Protections.Dnlib
{
    public class CawkModdedDevirt : Protection
    {
        public override string Name => "CawkVM Volt Mod DeVirtualizator";
        public override string Author => "andmuchmore";
        public override ProtectionType Type => ProtectionType.Dnlib;
        public override string Description => "A DeVirtualizer Modded For CawkVM";
        public override void Execute(Context Context)
        {
            if (Context.SysModule != null && Context.DnModule != null)
            {
                foreach (var TypeDef in Context.DnModule.Types.Where(x => x.HasMethods && !x.IsGlobalModuleType))
                {
                    foreach (var MethodDef in TypeDef.Methods.Where(x => x.HasBody))
                    {
                        var IL = MethodDef.Body.Instructions;
                        if(IL.Count > 4)
                        {
                        
                        for (int i = 0; i < IL.Count - 4; i++)
                        {
                            if (IL[i].OpCode != OpCodes.Ldc_I4)
                                continue;
                            if (IL[i + 1].OpCode != OpCodes.Ldstr)
                                continue;
                            if (IL[i + 2].OpCode != OpCodes.Ldc_I4)
                                continue;
                            if (IL[i + 3].OpCode != OpCodes.Ldloc)
                                continue;
                            if (IL[i + 4].OpCode != OpCodes.Call)
                                continue;
                            {
                                try
                                {
                                    Initialize.InitalizeMod(Context.SysModule);
                                    var objParams = IL[i + 3].Operand;
                                    int xorKey = Int32.Parse(IL[i + 2].Operand.ToString());
                                    string resName = IL[i + 1].Operand.ToString();
                                    int methodIndex = Int32.Parse(IL[i].Operand.ToString());

                                    object[] Params = new object[MethodDef.Parameters.Count]; int Index = 0;
                                    foreach (var Param in MethodDef.Parameters) 
                                    { 
                                        Params[Index++] = Param.Type.Next; 
                                    }
                                   
                                    var methodBase = Context.SysModule.ResolveMethod(MethodDef.MDToken.ToInt32());
                                    var dynamicMethod = ConvertBack.ModRunner(resName, xorKey, methodIndex, Params, methodBase, Context.SysModule.Assembly);
                                    var dynamicReader = Activator.CreateInstance(
                                                        typeof(System.Reflection.Emit.DynamicMethod).Module.GetTypes()
                                                        .FirstOrDefault(t => t.Name == "DynamicResolver"),
                                                        (System.Reflection.BindingFlags)(-1), null, new object[] { dynamicMethod.GetILGenerator() }, null);
                                    var dynamicMethodBodyReader = new DynamicMethodBodyReader(MethodDef.Module, dynamicReader);
                                    dynamicMethodBodyReader.Read();
                                    MethodDef.Body = dynamicMethodBodyReader.GetMethod().Body;
                                    
                                    Context.Log.Debug($"Done Devirtualize Method : {MethodDef.Name}");
                                    }
                                catch (Exception ex)
                                {
                                    Context.Log.Error(ex.Message);
                                }
                            }
                        }
                        }
                    }
                }
            }
        }
    }
}