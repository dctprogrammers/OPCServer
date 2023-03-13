using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DCTProgram
{
    internal class OPCServerConfiguration
    {
        public OPCServerConfiguration()
        {
            Initialize();
        }

        [OnDeserializing()]
        private void Initialize(StreamingContext context)
        {
            Initialize();
        }

        private void Initialize()
        {

        }
    }
}
