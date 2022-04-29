**C# Compiler test exercise for JetBrains**

Write and package a C# program which does the following:

- It receives one or more C# files as program arguments
- Fetches NuGet packages specified in file comments (official NuGet client could be used for that)
- Compiles files with Roslyn to an executable file
- Runs it with all other arguments 
- The code should be reliable, clear and understandable

**Todo's**

- [ ] 
- [ ] 

Additional requirements:

- [ ]  It should natively work on:
  - [ ]  Windows 32-bits and 64-bits
  - [ ]  Linux glibc and musl (or Mac, your choice)
- [ ]  Compilation result should be cached between runs at a system-specific proper location following
  operating system conventions for that
- [ ]  Many instances of the program may be running in parallel
- [ ]  The program should be automatically tested
- [ ]  The proper error handling should be implemented
- [ ]  Unsafe code support as an option

**Expected artifacts:**

- GitHub repository with sources
- Self-contained .zip distributions for Windows
- Self-contained .tar.gz distributions for Linux (or Mac)