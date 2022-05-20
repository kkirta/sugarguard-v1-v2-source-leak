using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Pdb;
using SugarGuard.Protector.Class;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SugarGuard.Protector.Protections.ControlFlow.BlockParser;

namespace SugarGuard.Protector.Protections.ControlFlow
{
    public class ControlFlow
    {
        public ControlFlow(SugarLib lib)
        {
            Main(lib);
        }

        void Main(SugarLib lib)
        {
            foreach (TypeDef type in lib.moduleDef.Types)
                foreach (MethodDef method in type.Methods)
                {
                    if (!method.HasBody || !method.Body.HasInstructions) continue;
                    if (method.ReturnType != null)
                    {
                        var body = method.Body;
                        body.SimplifyBranches();

                        ScopeBlock root = ParseBody(body);

                        new SwitchMangler().Mangle(body, root, lib, method, method.ReturnType);

                        body.Instructions.Clear();
                        root.ToBody(body);

                        if (body.PdbMethod != null)
                        {
                            body.PdbMethod = new PdbMethod()
                            {
                                Scope = new PdbScope()
                                {
                                    Start = body.Instructions.First(),
                                    End = body.Instructions.Last()
                                }
                            };
                        }

                        method.CustomDebugInfos.RemoveWhere(cdi => cdi is PdbStateMachineHoistedLocalScopesCustomDebugInfo);

                        foreach (ExceptionHandler eh in body.ExceptionHandlers)
                        {
                            var index = body.Instructions.IndexOf(eh.TryEnd) + 1;
                            eh.TryEnd = index < body.Instructions.Count ? body.Instructions[index] : null;
                            index = body.Instructions.IndexOf(eh.HandlerEnd) + 1;
                            eh.HandlerEnd = index < body.Instructions.Count ? body.Instructions[index] : null;
                        }
                    }

                }
        }
    }
}