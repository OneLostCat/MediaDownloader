using Serilog.Events;

namespace XMediaDownloader.Models;

public record CommandLineArguments(
    string Username,
    FileInfo CookieFile,
    DirectoryInfo OutputDir,
    string OutputPathFormat,
    MediaType DownloadType,
    bool WithoutDownloadInfo,
    bool WithoutDownloadMedia,
    DirectoryInfo StorageDir,
    // DirectoryInfo WorkDir,
    LogEventLevel LogLevel
);
