﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haystacks
{
    /// <summary>
    /// Information about a needle, corresponds to an entry in an index file.
    /// </summary>
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
        /// The position within the stack that the data was written to, i.e. where the needle starts.
        /// </summary>
        public long StackOffset { get; set; }

        /// <summary>
        /// The size of the needle in bytes.
        /// </summary>
        public long NeedleSize { get; set; }
    }
}
