using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SugarGuard.Protector.Class;
using dnlib.DotNet.Emit;

namespace SugarGuard.Protector.Protections.Mutation.Stages
{
    public class IntsToRandom
    {
        private MethodDef method { get; set; }
        public IntsToRandom(MethodDef method) => this.method = method;
        public static Random rnd = new Random();
        public void Execute(ref int i)
        {
            int valor = method.Body.Instructions[i].GetLdcI4Value();
            int seed = rnd.Next(1, int.MaxValue);
            int nextValue = randomAssist(seed, valor);
            if (nextValue == 0) return;
            method.Body.Instructions[i] = OpCodes.Ldc_I4.ToInstruction(seed);
            method.Body.Instructions.Insert(++i, OpCodes.Newobj.ToInstruction(method.Module.Import(typeof(Random).GetConstructor(new Type[] { typeof(int) }))));
            method.Body.Instructions.Insert(++i, OpCodes.Ldc_I4.ToInstruction(nextValue));
            method.Body.Instructions.Insert(++i, OpCodes.Callvirt.ToInstruction(method.Module.Import(typeof(Random).GetMethod("Next", new Type[] { typeof(int) }))));
        }

        private int randomAssist(int seed, int returnValue)
        {
            RandomHelper helper = new RandomHelper(seed);
            int test = (int)Math.Round((returnValue / (helper.InternalSample() * 4.6566128752457969E-10)));
            if (test < 0) return 0;
            if (new Random(seed).Next(test) == returnValue)
                return test;
            else if (new Random(seed).Next(test + 1) == returnValue)
                return test + 1;
            else
                return 0;
        }
    }
}
