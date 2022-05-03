/* 
    Package: Vostok.Logging.Abstractions 1.0.23
    Package: Vostok.Logging.Formatting 1.0.8
*/    

using Vostok.Logging.Abstractions;

public class LoggerThatCanLog
{
    private readonly ILog logger;

    public LoggerThatCanLog(ILog logger)
    {
        this.logger = logger.ForContext<LoggerThatCanLog>();
    }

    public void Log(string message, params object[] objects)
    {
        // Package: Vostok.Logging.Console 1.0.3
        logger.Info(message, objects);
    }
}