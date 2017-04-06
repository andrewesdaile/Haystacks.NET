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
        /// Returns the total number of data files stored in the haystack group.
        /// </summary>
        /// <returns></returns>
        public int DataFileCount()
        {
            int totalSize = 0;

            List<string> indexFiles = Directory.GetFiles(config.StackLocation, "*.index").ToList();

            foreach (string indexFile in indexFiles)
                totalSize += (int)(new FileInfo(indexFile).Length);

            return totalSize / 24;
        }

        /// <summary>
        /// Returns the total number of bytes stored in the haystack group.
        /// </summary>
        /// <returns></returns>
        public long DataSize()
        {
            long totalSize = 0;

            List<string> stackFiles = Directory.GetFiles(config.StackLocation, "*.stack").ToList();

            foreach (string stackFile in stackFiles)
                totalSize += (new FileInfo(stackFile).Length);

            return totalSize;
        }

        /// <summary>
        /// Writes a byte array of data into the haystack group.
        /// </summary>
        /// <param name="data">The data to be written.</param>
        /// <returns>Returns a NeedleInfo object with information about the data write operation.</returns>
        public NeedleInfo Write(byte[] data)
        {
            NeedleInfo info;

            using (MemoryStream stream = new MemoryStream(data))
            {
                info = Write(stream);
                stream.Flush();
                stream.Close();
            }

            return info;
        }

        /// <summary>
        /// Writes a file into the haystack group.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public NeedleInfo Write(string filename)
        {
            NeedleInfo info;

            using (FileStream stream = File.Open(filename, FileMode.Open, FileAccess.Read))
            {
                info = Write(stream);
                stream.Flush();
                stream.Close();
            }

            return info;
        }

        /// <summary>
        /// Writes a stream into the haystack group. 
        /// </summary>
        /// <param name="stream">The stream of data to write.</param>
        /// <returns>Returns a NeedleInfo object with information about the data write operation.</returns>
        public NeedleInfo Write(Stream stream)
        {
            NeedleInfo info = new NeedleInfo();

            //make sure that the input is not empty
            if (stream.Length == 0)
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
                for (int counter = 0; counter < stackSizes.Count; counter++)
                {
                    if (stackSizes[counter] + stream.Length <= config.MaximumStackSize)
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
            info.NeedleNumber = (int)((new FileInfo(targetIndex).Length) / 24L);
            info.StackOffset = new FileInfo(targetStack).Length;
            info.NeedleSize = stream.Length;

            //write the index entry
            using (FileStream indexStream = File.Open(targetIndex, FileMode.Open, FileAccess.Write))
            {
                indexStream.Seek(0, SeekOrigin.End);

                indexStream.Write(BitConverter.GetBytes(info.StackNumber), 0, 4);
                indexStream.Write(BitConverter.GetBytes(info.NeedleNumber), 0, 4);
                indexStream.Write(BitConverter.GetBytes(info.StackOffset), 0, 8);
                indexStream.Write(BitConverter.GetBytes(info.NeedleSize), 0, 8);
                indexStream.Flush();

                indexStream.Close();
            }

            ////write the stack entry
            //using (FileStream stackStream = File.Open(targetStack, FileMode.Open, FileAccess.Write))
            //{
            //    stackStream.Seek(0, SeekOrigin.End);

            //    stream.CopyTo(stackStream);
            //    stackStream.Flush();

            //    stackStream.Close();
            //}

            //write the stack entry
            byte[] buffer = new byte[WinFileIO.BlockSize];
            using (WinFileIO writer = new WinFileIO(buffer))
            {
                writer.OpenForWriting(targetStack);
                writer.Position = writer.Length;

                while (stream.Position < stream.Length)
                {
                    int bytesRead = stream.Read(buffer, 0, WinFileIO.BlockSize);
                    writer.WriteBlocks(bytesRead);
                }
            }

            return info;
        }

        /// <summary>
        /// Reads a file back out of a haystack.
        /// </summary>
        /// <param name="stackNumber">The stack that the data was stored in.</param>
        /// <param name="needleNumber">The needle number referencing the data chunk.</param>
        /// <returns>A byte array containing the original data.</returns>
        public byte[] Read(int stackNumber, int needleNumber)
        {
            byte[] output;

            using (MemoryStream stream = new MemoryStream())
            {
                Read(stream, stackNumber, needleNumber);
                stream.Flush();
                stream.Close();
                output = stream.ToArray();
            }

            return output;
        }

        /// <summary>
        /// Reads a file back out of a haystack.
        /// </summary>
        /// <param name="filename">The file to write the data into.</param>
        /// <param name="stackNumber">The stack that the data was stored in.</param>
        /// <param name="needleNumber">The needle number referencing the data chunk.</param>
        public void Read(string filename, int stackNumber, int needleNumber)
        {
            using (FileStream stream = File.Open(filename, FileMode.OpenOrCreate, FileAccess.Write))
            {
                Read(stream, stackNumber, needleNumber);
                stream.Flush();
                stream.Close();
            }
        }

        /// <summary>
        /// Reads a file back out of a haystack.
        /// </summary>
        /// <param name="stream">A stream to write the data into.</param>
        /// <param name="stackNumber">The stack that the data was stored in.</param>
        /// <param name="needleNumber">The needle number referencing the data chunk.</param>
        public void Read(Stream stream, int stackNumber, int needleNumber)
        {
            //make sure that the referenced stack exists
            string targetIndex = Path.Combine(config.StackLocation, stackNumber.ToString("0000000000") + ".index");
            string targetStack = Path.Combine(config.StackLocation, stackNumber.ToString("0000000000") + ".stack");

            if (!File.Exists(targetIndex))
                throw new Exception("The index file could not be found! Ensure that the stackNumber parameter is correct.");

            if (!File.Exists(targetStack))
                throw new Exception("The stack file could not be found! Ensure that the stackNumber parameter is correct.");

            long indexSize = new FileInfo(targetIndex).Length;
            if (((long)needleNumber + 1L) * 24L > indexSize)
                throw new Exception("The requested needle could not be found in this stack!");

            //read metadata from the index file
            long stackOffset;
            long needleLength;

            using (FileStream indexStream = File.Open(targetIndex, FileMode.Open, FileAccess.Read))
            {
                //seek to the entry for this needle
                indexStream.Seek((long)needleNumber * 24L, SeekOrigin.Begin);

                //skip the stack number and needle number
                indexStream.Seek(8, SeekOrigin.Current);

                //read the stack offset
                byte[] data = new byte[8];
                indexStream.Read(data, 0, 8);
                stackOffset = BitConverter.ToInt64(data, 0);

                //read the data length
                data = new byte[8];
                indexStream.Read(data, 0, 8);
                needleLength = BitConverter.ToInt64(data, 0);

                indexStream.Close();
            }

            ////read data from the stack file
            //using (FileStream stackStream = File.Open(targetStack, FileMode.Open, FileAccess.Read))
            //{
            //    //seek to the beginning of the needle
            //    stackStream.Seek(stackOffset, SeekOrigin.Begin);

            //    //read all the data for the needle
            //    byte[] buffer = new byte[81920];
            //    int chunkSize = 1;
            //    long bytesTransferred = 0;

            //    while (chunkSize > 0 && bytesTransferred <= needleLength)
            //    {
            //        chunkSize = stackStream.Read(buffer, 0, buffer.Length);
            //        bytesTransferred += chunkSize;

            //        if (bytesTransferred <= needleLength)
            //            stream.Write(buffer, 0, chunkSize);
            //        else
            //        {
            //            int lastChunkSize = (int)(needleLength % buffer.Length);
            //            stream.Write(buffer, 0, lastChunkSize);
            //        }
            //    }

            //    stream.Flush();
            //    stackStream.Close();
            //}

            //read data from the stack file
            byte[] buffer = new byte[WinFileIO.BlockSize];
            using (WinFileIO reader = new WinFileIO(buffer))
            {
                reader.OpenForReading(targetStack);

                //seek to the beginning of the needle
                reader.Position = stackOffset;

                //read all the data for the needle
                int chunkSize = 1;
                long bytesTransferred = 0;

                while (chunkSize > 0 && bytesTransferred <= needleLength)
                {
                    chunkSize = reader.ReadBlocks(WinFileIO.BlockSize);
                    bytesTransferred += chunkSize;

                    if (bytesTransferred <= needleLength)
                        stream.Write(buffer, 0, chunkSize);
                    else
                    {
                        int lastChunkSize = (int)(needleLength % buffer.Length);
                        stream.Write(buffer, 0, lastChunkSize);
                    }
                }
            }
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

                if (indexSize % 24L != 0L)
                {
                    //fault during index write

                    //chop off the garbage from the end of the index file
                    using (FileStream stream = File.Open(indexFile, FileMode.Open, FileAccess.Write))
                    {
                        stream.SetLength((indexSize / 24L) * 24L);
                        stream.Close();
                    }
                }
                else if (indexSize != 0)
                {
                    //fault during stack write (or possibly no fault)

                    //read metadata from the index file
                    long stackOffset;
                    long needleLength;

                    using (FileStream stream = File.Open(indexFile, FileMode.Open, FileAccess.Read))
                    {
                        //seek to the entry for the last needle
                        stream.Seek(-24L, SeekOrigin.End);

                        //skip the stack number and needle number
                        stream.Seek(8, SeekOrigin.Current);

                        //read the stack offset
                        byte[] data = new byte[8];
                        stream.Read(data, 0, 8);
                        stackOffset = BitConverter.ToInt64(data, 0);

                        //read the data length
                        data = new byte[8];
                        stream.Read(data, 0, 8);
                        needleLength = BitConverter.ToInt64(data, 0);

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
                            stream.SetLength(indexSize - 24L);
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
