// Package: Vostok.Logging.Abstractions 1.0.23

using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;

// Package: Vostok.Logging.Formatting 1.0.8
var logger = new SynchronousConsoleLog();

logger.Error("Hello, World!");

return 122;
// Package: Vostok.Logging.Console 1.0.8