**C# Compiler test exercise for JetBrains**

Write and package a C# program which does the following:

- It receives one or more C# files as program arguments
- Fetches NuGet packages specified in file comments (official NuGet client could be used for that)
- Compiles files with Roslyn to an executable file
- Runs it with all other arguments 
- The code should be reliable, clear and understandable

**Todo:**

- [x] Работающий прототип
- [ ] Интеграционные тесты с разнымы сценариями файлов
  - [ ] Выделить мета-класс для удобного тестирования
- [ ] CI для тестирования
- [ ] CI для выкладывания релизов по тегу
  - [ ] Запустить и протестировать Self-contained
  - [ ] Windows x86-x64 (.zip)
  - [ ] MacOS x86 (.tar.gz)
  - [ ] MacOS ARM (.tar.gz) - со звездочкой, если будет время
- [ ] Рефакторинг
  - [ ] Декомпозиция
- [ ] Additional requirements

  - [ ] Compilation result should be cached between runs at a system-specific proper location following
    operating system conventions for that
  - [ ] Many instances of the program may be running in parallel
  - [ ] The program should be automatically tested
  - [ ] Unsafe code support as an option
