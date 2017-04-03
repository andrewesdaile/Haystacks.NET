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
        static string dataLocation;

        static void Main(string[] args)
        {
            dataLocation = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Data");

            GeneralTest();

            //TestLargeFile();

            Console.ReadKey();
        }

        static void GeneralTest()
        {
            //remove old files if the demo was run before
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
                catch (Exception ex)
                {

                }
            }
        }

        static void TestLargeFile()
        {
            string largeFileInput = Path.Combine(dataLocation, "largefile.bin");
            string largeFileOutput = Path.Combine(dataLocation, "largefile-out.bin");

            //create the test file of pseudorandom data
            Console.WriteLine("Creating 3GB test file");
            long fileLength = 3000000000;
            Random random = new Random();

            using (FileStream stream = File.Open(largeFileInput, FileMode.Create, FileAccess.Write))
            {
                for (int counter = 0; counter < (fileLength / 81920L) + 1; counter++)
                {
                    byte[] buffer = new byte[81920];
                    random.NextBytes(buffer);
                    stream.Write(buffer, 0, buffer.Length);
                    stream.Flush();
                }

                stream.Close();
            }

            //initialise stacker
            ConfigInfo config = new ConfigInfo();
            config.StackLocation = dataLocation;
            config.MaximumStackSize = 10L * 1000L * 1000L * 1000L;

            Stacker stacker = new Stacker(config);

            //write the file to the stack
            Console.WriteLine("Writing the test file to the stack");
            NeedleInfo needle = stacker.Write(largeFileInput);

            //read the file back from the stack
            Console.WriteLine("Reading the test file from the stack");
            stacker.Read(largeFileOutput, 0, 0);
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
