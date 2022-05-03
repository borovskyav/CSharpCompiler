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
  - [ ] MacOS ARM (.tar.gz) - со звездочкой, если будет время
- [ ] Рефакторинг
  - [x] Декомпозиция
  - [x] packages extractor
    - [x] Научиться обрабатывать /* */
    - [ ] Обрабать ситуацию с дублирующимися библиотеками
  - [x] NuGet downloader
    - [ ] Кастомные NuGet-source
    - [ ] Транзитивные зависимости NuGet-пакетов
  - [x] DLL-extractor
    - [ ] Тесты
    - [ ] Выбирать наиболее подходящую версию пакета
  - [ ] Roslyn compiler
    - [ ] unsafe флаг
  - [ ] DLL Runner
    - [ ] InProcess or OutOfProcess
- [ ] Проверить что скомпилированная DLL работает
  - [ ] Собрать все файлы в одной папке
  - [ ] Генерация runtimeconfig.json
- [ ] Additional requirements

  - [ ] Compilation result should be cached between runs at a system-specific proper location following
    operating system conventions for that
  - [ ] Many instances of the program may be running in parallel
- [ ] Прокинуть CancellationToken везде, где я забыл это сделать
- [ ] Сделать красивые сообщения логгирования
