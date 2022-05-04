**C# Compiler test exercise for JetBrains**

Write and package a C# program which does the following:

- It receives one or more C# files as program arguments
- Fetches NuGet packages specified in file comments (official NuGet client could be used for that)
- Compiles files with Roslyn to an executable file
- Runs it with all other arguments 
- The code should be reliable, clear and understandable

**Todo:**

- [x] Работающий прототип
- [x] Интеграционные тесты с разнымы сценариями файлов
  - [x] Выделить мета-класс для удобного тестирования
- [x] Продумать Program.cs и всякие красивые остановки на Ctrl+C
- [x] CI для тестирования
- [x] CI для выкладывания релизов по тегу
  - [x] Запустить и протестировать Self-contained
  - [x] Windows x86-x64 (.zip)
  - [x] MacOS x86 (.tar.gz)
  - [x] Linux x86 (.tag.gz)
  - [ ] MacOS ARM (.tar.gz) (*)
- [ ] Рефакторинг
  - [x] Декомпозиция
  - [x] Packages Extractor
    - [x] Рефакторинг
    - [x] Научиться обрабатывать /* */
    - [x] Обрабать ситуацию с дублирующимися библиотеками
  - [ ] NuGet Downloader
    - [x] Рефакторинг
    - [ ] Кастомные NuGet-source
    - [x] Транзитивные зависимости NuGet-пакетов
  - [x] DLL Extractor
    - [x] Рефакторинг
    - [x] Выбирать наиболее подходящую версию пакета
    - [ ] Правильное подлючение runtime-пакетов (*)
  - [x] Roslyn Compiler
    - [x] Рефакторинг
    - [x] Unsafe флаг
    - [x] Подключить правильные референсы
  - [x] DLL Runner
    - [x] InProcess or OutOfProcess
- [x] Проверить что скомпилированная DLL работает
  - [x] Собрать все файлы в одной папке
  - [x] Генерация runtimeconfig.json
  - [ ] Настроить CI проверки работы скомпилированной DLL (*)
- [ ] Additional requirements

  - [ ] Compilation result should be cached between runs at a system-specific proper location following
    operating system conventions for that
  - [x] Many instances of the program may be running in parallel
- [x] Прокинуть CancellationToken везде, где я забыл это сделать
- [x] Сделать красивые сообщения логгирования
- [ ] Прибраться в использовании Microsoft.NETCore.App.Ref - не тянуть весь пакет, брать только список зависимостей (*)
