using Serilog.Events;

namespace MediaDownloader.Models;

public record CommandLineArguments(
    string Username,
    string CookieFile,
    string OutputDir,
    string OutputPathFormat,
    MediaType DownloadType,
    bool WithoutDownloadInfo,
    bool WithoutDownloadMedia,
    string WorkDir,
    LogEventLevel LogLevel
);
