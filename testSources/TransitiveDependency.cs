// Package: Vostok.Logging.Console 1.0.8

using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;

var logger = new SynchronousConsoleLog();

logger.Error("Hello, World!");

return 143;