# Haystacks.NET


Efficient storage of millions of files

This project was inspired by the haystacks used by Facebook:

https://code.facebook.com/posts/685565858139515/needle-in-a-haystack-efficient-storage-of-billions-of-photos/

It is written in 100% managed C#.

Instructions
---

The project comes with a test harness that demonstrates all the features of the library.

To see how it works, open the solution file in Visual Studio and open the Program.cs file in the TestHarness project.

Now, open the Data folder under %project%\TestHarness\bin\Debug\Data. When you run the test harness you'll be able to see the haystacks get generated and output files being extracted back out.

How It Works
---

Haystacks.NET differs from the Facebook implementation in a number of ways. It is simpler, and the current incarnation uses only synchronous calls. It retains fault tolerance, with a simple recovery call being all that is required to restore viability after a crash or power failure.

Stacks contain needles, with each needle being a file that has been inserted into the stack.

Each stack has an index file associated with it, which contains information about all the needles in the stack, as can be seen in the following diagram:

![diagram](https://raw.githubusercontent.com/andrewesdaile/Haystacks.NET/master/img/diagram.png)

Index entries have the following format:

- StackNumber : (32 bits) The ID number of the stack that contains the needle (stacks are numbered sequentially).
- NeedleNumber : (32 bits) The ID number of the needle within the stack (needles are numbered sequentially).
- StackOffset : (64 bits) The offset position at which the needle begins in the stack file.
- NeedleSize : (32 bits) The number of bytes in the needle.

Limitations
---

Maximum size of input files:
2^31 = 2,147,483,647 bytes = 2.15 GB

Maximum size of each haystack:
2^63 = 9,223,372,036,854,775,807 = 9223.37 PB

