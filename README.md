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

Limitations
---

Maximum size of input files:
2^31 = 2,147,483,647 bytes = 2.15 GB

Maximum size of each haystack:
2^63 = 9,223,372,036,854,775,807 = 9223.37 PB

Enjoy!
