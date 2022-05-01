// Package: Vostok.Logging.Console 1.0.8

using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;

var logger = new SynchronousConsoleLog();

var anotherLogger = new LoggerThatCanLog(logger);

logger.Error("Hello, World!");

anotherLogger.Log("Hello, {World}!", "World");

return 37;