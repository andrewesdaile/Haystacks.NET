using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haystacks
{
    public struct ConfigInfo
    {
        /// <summary>
        /// Specifies the location on the hard drive where the stack files are.
        /// </summary>
        public string StackLocation { get; set; }

        /// <summary>
        /// The maximum stack size in bytes. When writing a new file, if the file size would exceed this number then a new file is started.
        /// </summary>
        public long MaximumStackSize { get; set; }
    }
}
