using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Haystacks
{
    public class Stacker
    {
        private ConfigInfo config;

        public Stacker(ConfigInfo config)
        {
            this.config = config;
        }

        /// <summary>
        /// Writes a byte array of data into the haystack group.
        /// </summary>
        /// <param name="data">The data to be written.</param>
        /// <returns>Returns a NeedleInfo object with information about the data write operation.</returns>
        public NeedleInfo Write(byte[] data)
        {
            NeedleInfo info = new NeedleInfo();

            //make sure that the input is not empty
            if (data.Length == 0)
                throw new Exception("The input data cannot be empty.");

            //get the lists of existing files and their sizes
            List<string> indexFiles = Directory.GetFiles(config.StackLocation, "*.index").ToList();
            List<string> stackFiles = Directory.GetFiles(config.StackLocation, "*.stack").ToList();

            List<long> stackSizes = new List<long>();
            foreach (string stackFile in stackFiles)
                stackSizes.Add(new FileInfo(stackFile).Length);

            //determine the best file to write the data to
            int targetFileNumber = -1;

            if (indexFiles.Count == 0)
                targetFileNumber = 0;
            else
            {
                //scan to find a stack with enough room
                for (int counter = 0; counter < stackSizes.Count; counter ++)
                {
                    if (stackSizes[counter] + data.Length <= config.MaximumStackSize)
                    {
                        targetFileNumber = counter;
                        break;
                    }
                }

                //if no files have capacity then start a new one
                if (targetFileNumber == -1)
                    targetFileNumber = stackSizes.Count;
            }

            //create the index and stack files if they don't yet exist
            string targetIndex = Path.Combine(config.StackLocation, targetFileNumber.ToString("0000000000") + ".index");
            string targetStack = Path.Combine(config.StackLocation, targetFileNumber.ToString("0000000000") + ".stack");

            if (!File.Exists(targetIndex))
                File.Create(targetIndex).Close();

            if (!File.Exists(targetStack))
                File.Create(targetStack).Close();

            //fill in the information struct
            info.StackNumber = targetFileNumber;
            info.NeedleNumber = (int)((new FileInfo(targetIndex).Length) / 20L);
            info.StackOffset = new FileInfo(targetStack).Length;

            //write the index entry
            using (FileStream stream = File.Open(targetIndex, FileMode.Open, FileAccess.Write))
            {
                stream.Seek(0, SeekOrigin.End);

                stream.Write(BitConverter.GetBytes(info.StackNumber), 0, 4);
                stream.Write(BitConverter.GetBytes(info.NeedleNumber), 0, 4);
                stream.Write(BitConverter.GetBytes(info.StackOffset), 0, 8);
                stream.Write(BitConverter.GetBytes(data.Length), 0, 4);
                stream.Flush();

                stream.Close();
            }

            //write the stack entry
            using (FileStream stream = File.Open(targetStack, FileMode.Open, FileAccess.Write))
            {
                stream.Seek(0, SeekOrigin.End);

                stream.Write(data, 0, data.Length);
                stream.Flush();

                stream.Close();
            }

            return info;
        }

        /// <summary>
        /// Reads a chunk of data back out of a haystack.
        /// </summary>
        /// <param name="stackNumber">The stack that the data was stored in.</param>
        /// <param name="needleNumber">The needle number referencing the data chunk.</param>
        /// <returns>A byte array containing the original data.</returns>
        public byte[] Read(int stackNumber, int needleNumber)
        {
            byte[] output;

            //make sure that the referenced stack exists
            string targetIndex = Path.Combine(config.StackLocation, stackNumber.ToString("0000000000") + ".index");
            string targetStack = Path.Combine(config.StackLocation, stackNumber.ToString("0000000000") + ".stack");

            if (!File.Exists(targetIndex))
                throw new Exception("The index file could not be found! Ensure that the stackNumber parameter is correct.");

            if (!File.Exists(targetStack))
                throw new Exception("The stack file could not be found! Ensure that the stackNumber parameter is correct.");

            long indexSize = new FileInfo(targetIndex).Length;
            if (((long)needleNumber + 1L) * 20L > indexSize)
                throw new Exception("The requested needle could not be found in this stack!");

            //read metadata from the index file
            long stackOffset;
            int needleLength;

            using (FileStream stream = File.Open(targetIndex, FileMode.Open, FileAccess.Read))
            {
                //seek to the entry for this needle
                stream.Seek((long)needleNumber * 20L, SeekOrigin.Begin);

                //skip the stack number and needle number
                stream.Seek(8, SeekOrigin.Current);

                //read the stack offset
                byte[] data = new byte[8];
                stream.Read(data, 0, 8);
                stackOffset = BitConverter.ToInt64(data, 0);

                //read the data length
                data = new byte[4];
                stream.Read(data, 0, 4);
                needleLength = BitConverter.ToInt32(data, 0);

                stream.Close();
            }

            //read data from the stack file
            using (FileStream stream = File.Open(targetStack, FileMode.Open, FileAccess.Read))
            {
                //seek to the beginning of the needle
                stream.Seek(stackOffset, SeekOrigin.Begin);

                //read all the data for the needle
                output = new byte[needleLength];
                stream.Read(output, 0, needleLength);

                stream.Close();
            }

            return output;
        }

        /// <summary>
        /// Performs a recovery after a fault, e.g. power lost while writing to a file. If no corruption occurred then the files are left untouched.
        /// </summary>
        public void Recover()
        {
            //get the lists of existing files and their sizes
            List<string> indexFiles = Directory.GetFiles(config.StackLocation, "*.index").ToList();

            //check if failure happened during the index write or the stack write
            foreach (string indexFile in indexFiles)
            {
                long indexSize = new FileInfo(indexFile).Length;

                if (indexSize % 20L != 0L)
                {
                    //fault during index write

                    //chop off the garbage from the end of the index file
                    using (FileStream stream = File.Open(indexFile, FileMode.Open, FileAccess.Write))
                    {
                        stream.SetLength((indexSize / 20L) * 20L);
                        stream.Close();
                    }
                }
                else if (indexSize != 0)
                {
                    //fault during stack write (or possibly no fault)

                    //read metadata from the index file
                    long stackOffset;
                    int needleLength;

                    using (FileStream stream = File.Open(indexFile, FileMode.Open, FileAccess.Read))
                    {
                        //seek to the entry for the last needle
                        stream.Seek(-20L, SeekOrigin.End);

                        //skip the stack number and needle number
                        stream.Seek(8, SeekOrigin.Current);

                        //read the stack offset
                        byte[] data = new byte[8];
                        stream.Read(data, 0, 8);
                        stackOffset = BitConverter.ToInt64(data, 0);

                        //read the data length
                        data = new byte[4];
                        stream.Read(data, 0, 4);
                        needleLength = BitConverter.ToInt32(data, 0);

                        stream.Close();
                    }

                    //get the path of the stack file
                    string stackFile = Path.GetFileName(indexFile).ToLower().Replace(".index", ".stack");
                    stackFile = Path.Combine(config.StackLocation, stackFile);

                    long stackSize = new FileInfo(stackFile).Length;

                    //did I/O fail during the needle write?
                    if (stackSize != stackOffset + needleLength)
                    {
                        //chop off the last entry of the index file
                        using (FileStream stream = File.Open(indexFile, FileMode.Open, FileAccess.Write))
                        {
                            stream.SetLength(indexSize - 20L);
                            stream.Close();
                        }

                        //chop off the garbage from the end of the stack file
                        using (FileStream stream = File.Open(stackFile, FileMode.Open, FileAccess.Write))
                        {
                            stream.SetLength(stackOffset);
                            stream.Close();
                        }
                    }
                }
            }
        }

    }
}
