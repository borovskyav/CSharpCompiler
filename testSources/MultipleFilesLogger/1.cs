// Package: Vostok.Logging.Console 1.0.8
// Package: Vostok.Logging.Abstractions 1.0.1

using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;

var logger = new SynchronousConsoleLog();

var anotherLogger = new LoggerThatCanLog(logger);

logger.Debug("Hello, World!");

var i = 0;
foreach (var arg in args)
{
    // Package: Moq 4.17.2
    anotherLogger.Log("Print: {arg}", arg);
    i++;
}

anotherLogger.Log("Logged {number} messages", i);


return 0;