using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using Haystacks;

namespace TestHarness
{
    class Program
    {
        static void Main(string[] args)
        {
            //remove old files if the demo was run before
            string dataLocation = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Data");

            foreach (string filename in Directory.GetFiles(dataLocation, "*.index"))
                File.Delete(filename);

            foreach (string filename in Directory.GetFiles(dataLocation, "*.stack"))
                File.Delete(filename);

            //configure the stacker, note that in real life the stack size will be much larger than 10MB!
            ConfigInfo config = new ConfigInfo();
            config.StackLocation = dataLocation;
            config.MaximumStackSize = 10 * 1000 * 1000;

            Stacker stacker = new Stacker(config);

            //write all the input files into the haystack group
            string inputLocation = Path.Combine(config.StackLocation, "input");

            Dictionary<string, NeedleInfo> needles = new Dictionary<string, NeedleInfo>();

            foreach (string file in Directory.GetFiles(inputLocation, "*"))
            {
                byte[] data = File.ReadAllBytes(file);
                needles.Add(file, stacker.Write(data));
            }

            ////retrieve the files back out and put them in the output folder
            //string outputLocation = Path.Combine(config.StackLocation, "output");

            //foreach (NeedleInfo needle in needles)
            //{
            //    byte[] data = stacker.Read(needle.StackNumber, needle.NeedleNumber);
            //    string filename = needle.StackNumber.ToString() + "-" + needle.NeedleNumber.ToString() + ".jpg";
            //    File.WriteAllBytes(Path.Combine(outputLocation, filename), data);
            //}

            //simulate a corrupted index by adding some data to the end of an index file
            string corruptIndex = Path.Combine(dataLocation, "0000000001.index");

            using (FileStream stream = File.Open(corruptIndex, FileMode.Open, FileAccess.Write))
            {
                stream.Seek(0L, SeekOrigin.End);
                stream.Write(BitConverter.GetBytes(0L), 0, 8);
                stream.Flush();
                stream.Close();
            }

            //simulate a corrupted stack by chopping some data from the end of a stack file
            string corruptStack = Path.Combine(dataLocation, "0000000000.stack");

            using (FileStream stream = File.Open(corruptStack, FileMode.Open, FileAccess.Write))
            {
                long fileSize = new FileInfo(corruptStack).Length;
                stream.SetLength(fileSize - 10000);
                stream.Close();
            }

            //fix the corrupt files by calling recover
            stacker.Recover();

            //write out the remaining files and compare inputs & outputs to prove that everything is OK
            string outputLocation = Path.Combine(config.StackLocation, "output");
            int fileNumber = 0;

            foreach (string inputFilename in needles.Keys)
            {
                try
                {
                    //write the output file
                    NeedleInfo needle = needles[inputFilename];
                    byte[] data = stacker.Read(needle.StackNumber, needle.NeedleNumber);
                    string outputFilename = needle.StackNumber.ToString() + "-" + needle.NeedleNumber.ToString() + ".jpg";
                    outputFilename = Path.Combine(outputLocation, outputFilename);
                    File.WriteAllBytes(outputFilename, data);

                    //compare the input and output files
                    if (CompareFiles(inputFilename, outputFilename))
                        Console.WriteLine(Path.GetFileName(outputFilename) + " : OK");
                    else
                        Console.WriteLine(Path.GetFileName(outputFilename) + " : Error");

                    fileNumber++;
                }
                catch
                {
                }
            }

            Console.ReadKey();
        }

        //returns true if two files are the same
        static bool CompareFiles(string filename1, string filename2)
        {
            byte[] data1 = File.ReadAllBytes(filename1);
            byte[] data2 = File.ReadAllBytes(filename2);

            if (data1.Length != data2.Length)
                return false;

            bool isIdentical = true;

            for (int counter = 0; counter < data1.Length; counter ++)
            {
                if (data1[counter] != data2[counter])
                {
                    isIdentical = false;
                    break;
                }
            }

            return isIdentical;
        }

    }
}
