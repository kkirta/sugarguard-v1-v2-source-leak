using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SugarGuard.Protector.Protections.Mutation.Stages
{
    public class IntsToMethodPointer
    {
        private MethodDef method { get; set; }
        public IntsToMethodPointer(MethodDef method)
        {
            this.method = method;
        }

        public void Execute()
        {
            
        }
    }
}
