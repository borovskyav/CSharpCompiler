// Package: Vostok.Logging.Console 1.0.8
// Package: Moq 4.17.2

using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;

var logger = new SynchronousConsoleLog();

var anotherLogger = new LoggerThatCanLog(logger);

logger.Debug("Hello, World!");

var i = 0;
foreach (var arg in args)
{
    anotherLogger.Log("Print: {arg}", arg);
    i++;
}

anotherLogger.Log("Logged {number} messages", i);


return 0;