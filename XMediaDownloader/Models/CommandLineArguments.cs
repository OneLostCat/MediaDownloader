using Serilog.Events;

namespace XMediaDownloader.Models;

public record CommandLineArguments(
    string Username,
    FileInfo CookieFile,
    string OutputPath,
    MediaType DownloadType,
    bool WithoutDownloadInfo,
    bool WithoutDownloadMedia,
    DirectoryInfo StorageDir,
    LogEventLevel LogLevel);