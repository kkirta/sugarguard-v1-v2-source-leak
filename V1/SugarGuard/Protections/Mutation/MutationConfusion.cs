using dnlib.DotNet.Emit;
using dnlib.DotNet;
using SugarGuard.Core;
using SugarGuard.Helpers.MethodBlocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SugarGuard.Protections.Mutation
{
    public class MutationConfusion : Protection
    {
        public override string Name => "Mutation Confusion";
        public override void Execute(Context context)
        {
            var module = context.Module;
            var cctor = context.Cctor;
            foreach (var type in module.GetTypes())
            {
                if (type.IsGlobalModuleType)
                    continue;
                if (type.Namespace == "Costura")
                    continue;
                foreach (var method in type.Methods)
                {
                    if (!method.HasBody || !method.Body.HasInstructions)
                        continue;
                    PhaseMutation(method, cctor, module);
                }
            }
        }

        public static void PhaseMutation(MethodDef method, MethodDef cctor, ModuleDefMD module)
        {
            var instructions = new List<Instruction>();
            var currentBlocks = method.GetBlocks();
            var finalBlocks = new List<Block>();
            var mutationBlocks = new List<Block>();
            var initLocal = new Local(module.CorLibTypes.Int32);
            var initValue = Utils.RandomBigInt32();
            var local = new Int32Local(initLocal, initValue);
            var field = Utils.CreateField(new FieldSig(module.CorLibTypes.Int32));
            var cctorBody = cctor.Body.Instructions;

            module.GlobalType.Fields.Add(field);

            cctorBody.Insert(0, OpCodes.Ldc_I4.ToInstruction(initValue));
            cctorBody.Insert(1, OpCodes.Stsfld.ToInstruction(field));

            method.Body.Variables.Add(initLocal);
            instructions.Add(OpCodes.Nop.ToInstruction());
            instructions.Add(OpCodes.Ldsfld.ToInstruction(field));
            instructions.Add(OpCodes.Stloc_S.ToInstruction(initLocal));

            var initBlock = new Block(instructions.ToArray());

            finalBlocks.Add(initBlock);

            foreach (var block in currentBlocks)
            {
                if (block.IsSafe && !block.IsBranched && !block.IsException)
                {
                    bool tried = false;
                GenBlock:
                    if (Utils.RandomBoolean() || mutationBlocks.Count == 0 || tried)
                    {
                        for (int i = 0; i < Utils.rnd.Next(2, 5); i++)
                        {
                            var mutationBlock = BlockHandler.GenerateBlock(local, BlockType.Arithmethic);
                            finalBlocks.Add(mutationBlock);
                            mutationBlocks.Add(mutationBlock);
                        }
                    }
                    else
                    {
                        var bef = local.Value;
                        var index = Utils.rnd.Next(0, mutationBlocks.Count);
                        var randMutationBlock = mutationBlocks[index];
                        var result = BlockHandler.Emulate(randMutationBlock.Instructions, local);

                        if (randMutationBlock.Values.Contains(result))
                        {
                            tried = true;
                            goto GenBlock;
                        }
                        else
                        {
                            local.Value = result;

                            randMutationBlock.Values.Add(result);
                            var branch = BlockHandler.GenerateBranch(local.Value, initLocal, block.Instructions[0]);
                            randMutationBlock.Instructions.AddRange(branch);

                            block.Instructions.Insert(0, OpCodes.Br.ToInstruction(randMutationBlock.Instructions[0]));
                        }
                    }
                }

                for (int i = 0; i < block.Instructions.Count; i++)
                {
                    var instr = block.Instructions[i];

                    if (!instr.IsLdcI4())
                        continue;

                    var value = instr.GetLdcI4Value();
                    var currentValue = local.Value;
                    var code = Utils.GetCode(true);

                    var replaceValue = BlockHandler.Calculate(value, currentValue, code);

                    instr.OpCode = OpCodes.Ldc_I4;
                    instr.Operand = replaceValue;

                    block.Instructions.Insert(i + 1, OpCodes.Ldloc_S.ToInstruction(initLocal));
                    block.Instructions.Insert(i + 2, code.ToOpCode().ToInstruction());

                    i += 2;
                }
                finalBlocks.Add(block);
            }

            method.Body.Instructions.Clear();

            foreach (var block in finalBlocks)
                foreach (var instr in block.Instructions)
                    method.Body.Instructions.Add(instr);

        }
    }
}
