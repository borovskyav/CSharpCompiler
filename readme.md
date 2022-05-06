**C# Compiler test exercise for JetBrains**

Write and package a C# program which does the following:

- It receives one or more C# files as program arguments
- Fetches NuGet packages specified in file comments (official NuGet client could be used for that)
- Compiles files with Roslyn to an executable file
- Runs it with all other arguments 
- The code should be reliable, clear and understandable

**How to use**

Download suitable version for your operation system from releases page.

Run CSharpCompiler executable file:

```bash
./CSharpCompiler [Flags | -allowUnsafe] [Files | 1.cs 2.cs 3.cs] -- [Compiled program arguments | 1 2 3]
```

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
- [x] Рефакторинг
  - [x] Декомпозиция
  - [x] Packages Extractor
    - [x] Рефакторинг
    - [x] Научиться обрабатывать /* */
    - [x] Обрабать ситуацию с дублирующимися библиотеками
  - [x] NuGet Downloader
    - [x] Рефакторинг
    - [x] Кастомные NuGet-source
    - [x] Транзитивные зависимости NuGet-пакетов
    - [x] Что будет происходить если резолвер не смог разрезолвить граф? (*)
  - [x] DLL Extractor
    - [x] Рефакторинг
    - [x] Выбирать наиболее подходящую версию пакета
  - [x] Roslyn Compiler
    - [x] Рефакторинг
    - [x] Unsafe флаг
    - [x] Подключить правильные референсы
  - [x] DLL Runner
    - [x] InProcess or OutOfProcess
- [x] Проверить что скомпилированная DLL работает
  - [x] Собрать все файлы в одной папке
  - [x] Генерация runtimeconfig.json
- [x] Additional requirements

  - [x] Compilation result should be cached between runs at a system-specific proper location following
    operating system conventions for that. Note: не увидел смысла кешировать библиотеки, так как в большинстве случаев всё и так лежит в глобальном кеше.
  - [x] Many instances of the program may be running in parallel
- [x] Прокинуть CancellationToken везде, где я забыл это сделать
- [x] Сделать красивые сообщения логгирования
- [ ] Доделки
  - [ ] Прибраться в использовании Microsoft.NETCore.App.Ref - не тянуть весь пакет, брать только список зависимостей (*)
  - [x] Переехать на RemoteDependencyWalker (*)
  - [ ] Научиться указывать source пакета прямо в файле
  - [ ] Настроить CI проверки работы скомпилированной DLL (*)
  - [ ] Правильное подлючение runtime-пакетов (*)
  - [ ] MacOS ARM (.tar.gz) (*)
