using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SugarGuard.Protector.Class;

namespace SugarGuard.Protector.Protections.Constants
{
    public class PosConstants
    {
        public PosConstants(SugarLib lib)
        {
            Main(lib);
        }

        void Main(SugarLib lib)
        {
            foreach (TypeDef type in lib.moduleDef.Types)
                foreach (MethodDef method in type.Methods)
                {
                    var encodedmethods = Constants.encodedMethods;
                    foreach (var emethod in encodedmethods)
                        if (emethod.eMethod == method)
                        {
                            method.Body.Instructions.Add(OpCodes.Ldstr.ToInstruction(emethod.eNum.ToString()));
                            method.Body.Instructions.Add(OpCodes.Pop.ToInstruction());
                            method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
                        }
                }
        }
    }
}