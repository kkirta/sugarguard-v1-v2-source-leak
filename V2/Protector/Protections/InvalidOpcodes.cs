using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SugarGuard.Protector.Class;
using System;

namespace SugarGuard.Protector.Protections
{
    public class InvalidOpcodes
    {
        public InvalidOpcodes(SugarLib lib) => Main(lib);

        void Main(SugarLib lib)
        {
            foreach (TypeDef typeDef in lib.moduleDef.GetTypes())
            {
                foreach (MethodDef methodDef in typeDef.Methods)
                {
                    if (!methodDef.HasBody || !methodDef.Body.HasInstructions) continue;

                    methodDef.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Box, methodDef.Module.Import(typeof(Math))));
                }
            }
        }
    }
}