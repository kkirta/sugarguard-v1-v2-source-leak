using dnlib.DotNet;
using SugarGuard.Protector.Class;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SugarGuard.Protector.Protections.Mutation.Stages;
using dnlib.DotNet.Emit;
using static SugarGuard.Protector.Class.MutationHelper;

namespace SugarGuard.Protector.Protections.Mutation
{
    public class Mutation
    {
        public Mutation(SugarLib lib)
        {
            Phase(lib);
        }
        static Random rnd = new Random();
        public void Phase(SugarLib lib)
        {
            ModuleDef module = lib.moduleDef;
            foreach (TypeDef type in module.GetTypes())
            {
                if (type.IsGlobalModuleType) continue;
                foreach (MethodDef method in type.Methods)
                {
                    if (!canMutateMethod(method)) continue;
                    new MethodPreparation(method).Execute();

                    var inliner = new IntsToInliner(method);
                    var arrays = new IntsToArray(method);
                    var alloc = new IntsToStackalloc(method);
                    var maths = new IntsToMath(method);
                    var locals = new LocalsToCustomLocal(method);
                    var randoms = new IntsToRandom(method);

                    for (int i = 0; i < method.Body.Instructions.Count; i++)
                    {
                        if (method.Body.Instructions[i].IsLdcI4() && CanObfuscate(method.Body.Instructions, i))
                        {
                            switch (rnd.Next(0, 5))
                            {
                                /// Incompatibilidade

                                case 1: maths.Execute(ref i); break;
                                case 2: locals.Execute(ref i); break;
                                case 3: randoms.Execute(ref i); break;
                                case 4: alloc.Execute(ref i); break; 
                            }
                        }
                    }

                    /// Incompatibilidade

                    //alloc.Inject();
                    //var blocks = ParseMethod(method);
                    //var blockedInstructions = new List<Instruction>();
                    //for (int i = 0; i < blocks.Count; i++)
                    //{
                    //    var block = blocks[i];
                    //    bool can = true;
                    //    for (int j = 0; j < block.Instructions.Count; j++)
                    //    {
                    //        if (blockedInstructions.Contains(block.Instructions[j]))
                    //        {
                    //            blockedInstructions.Remove(block.Instructions[j]);
                    //            can = false;
                    //        }
                    //        if (block.Instructions[j].Operand is Instruction)
                    //        {
                    //            blockedInstructions.Add((Instruction)block.Instructions[j].Operand);
                    //        }
                    //    }
                    //    if (blockedInstructions.Count < 1 && can)
                    //    {
                    //        for (int j = 0; j < block.Instructions.Count; j++)
                    //        {
                    //            var start = method.Body.Instructions.IndexOf(block.Instructions[0]);
                    //            var reali = method.Body.Instructions.IndexOf(block.Instructions[j]);
                    //            if (block.Instructions[j].IsLdcI4())
                    //            {
                    //                switch (rnd.Next(0, 3))
                    //                {
                    //                    case 1: inliner.Execute(reali, start); break;
                    //                    case 2: arrays.Execute(reali, start); break;
                    //                }
                    //            }
                    //        }
                    //    }
                    //}
                    //continue;
                }
            }
        }

        public bool canMutateMethod(MethodDef method)
        {
            return method.HasBody && method.Body.HasInstructions;
        }
    }
}
