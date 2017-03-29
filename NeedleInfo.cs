using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haystacks
{
    public struct NeedleInfo
    {
        /// <summary>
        /// The number of the stack that the data was written into. The combination of NeedleNumber and StackNumber is unique.
        /// </summary>
        public int StackNumber { get; set; }

        /// <summary>
        /// A sequential number for the file / data once it has been inserted into a haystack.
        /// </summary>
        public int NeedleNumber { get; set; }

        /// <summary>
        /// The position within the stack that the data was written to.
        /// </summary>
        public long StackOffset { get; set; }
    }
}
