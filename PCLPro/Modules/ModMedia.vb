Imports System.IO

Public Module ModMedia

#Region "格式判断"

    Private ReadOnly DirectVideoExtensionList As String() = {"mp4", "m4v", "mov", "wmv", "avi", "3gp"}

    Public ReadOnly AudioExtensionList As String() = {"wav", "mp3", "flac", "m4a", "aac", "wma", "ogg", "oga", "opus", "aiff", "aif", "ape", "wv", "amr", "mp2", "mka"}

    Public Function IsDirectVideo(Path As String) As Boolean
        Try
            Return DirectVideoExtensionList.Contains(PathUtils.GetExtension(Path))
        Catch
            Return False
        End Try
    End Function

    Public Function IsAudioFile(Path As String) As Boolean
        Try
            Return AudioExtensionList.Contains(PathUtils.GetExtension(Path))
        Catch
            Return False
        End Try
    End Function

#End Region

#Region "FFmpeg 解码器"

    Private Const FfmpegDownloadUrl As String = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip"

    Private _FfmpegExe As String = Nothing

    Public ReadOnly Property FfmpegExe As String
        Get
            If _FfmpegExe Is Nothing Then _FfmpegExe = Paths.Base & "PCL\Cache\FFmpeg\ffmpeg.exe"
            Return _FfmpegExe
        End Get
    End Property

    Public Function FfmpegReady() As Boolean
        Try
            Return IO.File.Exists(FfmpegExe)
        Catch
            Return False
        End Try
    End Function

    Private Function EnsureFfmpeg(Reason As String) As Boolean
        If FfmpegReady() Then Return True
        Dim Text As String = "播放" & Reason & "需要 FFmpeg 解码器。" & vbCrLf &
                             "首次使用需要下载约 90 MB，下载一次之后所有格式都可以直接播放或转换。" & vbCrLf & vbCrLf &
                             "是否现在下载？"
        If MyMsgBox(Text, "需要 FFmpeg 解码器", "下载", "取消", IsWarn:=True) <> 1 Then Return False
        Return DownloadFfmpeg()
    End Function

    Private Function DownloadFfmpeg() As Boolean
        Dim Folder As String = IO.Path.GetDirectoryName(FfmpegExe)
        Dim ZipPath As String = IO.Path.Combine(Folder, "ffmpeg.zip")
        Try
            DirectoryUtils.Create(Folder)
            UpdateProgressShow("下载 FFmpeg 解码器")
            UpdateProgressSet(-1, "正在连接下载源…")
            If Not UpdateDownloadFile(FfmpegDownloadUrl, ZipPath) Then
                UpdateProgressClose()
                Hint("FFmpeg 下载失败，请检查网络后重试。", HintType.Red)
                Return False
            End If
            UpdateProgressSet(-1, "正在解压 ffmpeg.exe…")
            Using Zip As IO.Compression.ZipArchive = IO.Compression.ZipFile.OpenRead(ZipPath)
                Dim Entry As IO.Compression.ZipArchiveEntry = Zip.Entries.FirstOrDefault(
                    Function(E As IO.Compression.ZipArchiveEntry) E.FullName.EndsWith("bin/ffmpeg.exe", StringComparison.OrdinalIgnoreCase))
                If Entry Is Nothing Then
                    UpdateProgressClose()
                    Hint("FFmpeg 压缩包内容异常，请稍后重试。", HintType.Red)
                    Return False
                End If
                If IO.File.Exists(FfmpegExe) Then IO.File.Delete(FfmpegExe)
                Entry.ExtractToFile(FfmpegExe, True)
            End Using
            Try
                IO.File.Delete(ZipPath)
            Catch
            End Try
            UpdateProgressClose()
            Logger.Info($"FFmpeg 解码器已就绪：{FfmpegExe}")
            Hint("FFmpeg 解码器下载完成！", HintType.Green)
            Return True
        Catch ex As Exception
            UpdateProgressClose()
            Logger.Error(ex, "下载 FFmpeg 解码器失败")
            Hint("FFmpeg 下载或解压失败，详情请查看日志。", HintType.Red)
            Return False
        End Try
    End Function

#End Region

#Region "格式转换"

    Private Function CacheTarget(Source As String, ExtensionName As String) As String
        Dim Info As New IO.FileInfo(Source)
        Dim Key As String = Source.ToLowerInvariant() & "|" & Info.Length & "|" & Info.LastWriteTimeUtc.Ticks
        Dim Hash As String
        Using Sha As New Security.Cryptography.SHA1Managed()
            Hash = BitConverter.ToString(Sha.ComputeHash(Text.Encoding.UTF8.GetBytes(Key))).Replace("-", "")
        End Using
        Dim Folder As String = Paths.Base & "PCL\Cache\Media\"
        DirectoryUtils.Create(Folder)
        Return Folder & PathUtils.GetFileNameWithoutExtension(Source) & "_" & Hash.Substring(0, 12) & "." & ExtensionName
    End Function

    Public Function PrepareVideoForPlayback(Source As String, Optional ForceConvert As Boolean = False) As String
        Try
            If Source Is Nothing OrElse Not IO.File.Exists(Source) Then Return Nothing
            If Not ForceConvert AndAlso IsDirectVideo(Source) Then Return Source
            Dim Target As String = CacheTarget(Source, "mp4")
            If IO.File.Exists(Target) AndAlso New IO.FileInfo(Target).Length > 0 Then Return Target
            If Not EnsureFfmpeg("该视频格式") Then Return Nothing
            Return RunFfmpeg(Source, Target,
                             "-c:v libx264 -preset veryfast -crf 23 -pix_fmt yuv420p -c:a aac -b:a 128k -movflags +faststart",
                             "正在转换视频背景")
        Catch ex As Exception
            Logger.Error(ex, "视频格式转换失败")
            Return Nothing
        End Try
    End Function

    Public Function PrepareAudioForPlayback(Source As String) As String
        Try
            If Source Is Nothing OrElse Not IO.File.Exists(Source) Then Return Nothing
            Dim Target As String = CacheTarget(Source, "wav")
            If IO.File.Exists(Target) AndAlso New IO.FileInfo(Target).Length > 0 Then Return Target
            If Not EnsureFfmpeg("该音乐格式") Then Return Nothing
            Return RunFfmpeg(Source, Target, "-vn -ac 2 -ar 44100 -acodec pcm_s16le", "正在转换音频")
        Catch ex As Exception
            Logger.Error(ex, "音频格式转换失败")
            Return Nothing
        End Try
    End Function

    Private Function RunFfmpeg(Source As String, Target As String, Args As String, ProgressText As String) As String
        Dim PartFile As String = Target & ".part"
        Try
            DirectoryUtils.Create(IO.Path.GetDirectoryName(Target))
            If IO.File.Exists(PartFile) Then IO.File.Delete(PartFile)
            UpdateProgressShow(ProgressText)
            UpdateProgressSet(-1, ProgressText & "…（仅首次需要，文件较大时可能要多等几分钟）")
            Dim Command As String = "-y -hide_banner -nostdin -i """ & Source & """ " & Args & " """ & PartFile & """"
            Dim Info As New Diagnostics.ProcessStartInfo With {
                .FileName = FfmpegExe,
                .Arguments = Command,
                .CreateNoWindow = True,
                .UseShellExecute = False,
                .WorkingDirectory = IO.Path.GetDirectoryName(FfmpegExe)}
            Using P As Diagnostics.Process = Diagnostics.Process.Start(Info)
                P.WaitForExit()
                If P.ExitCode <> 0 Then
                    UpdateProgressClose()
                    Try
                        If IO.File.Exists(PartFile) Then IO.File.Delete(PartFile)
                    Catch
                    End Try
                    Logger.Warn($"FFmpeg 转换失败（退出码 {P.ExitCode}）：{Source}")
                    Hint("格式转换失败，该文件可能已损坏或编码过于特殊。", HintType.Red)
                    Return Nothing
                End If
            End Using
            If Not IO.File.Exists(PartFile) Then
                UpdateProgressClose()
                Return Nothing
            End If
            If IO.File.Exists(Target) Then IO.File.Delete(Target)
            IO.File.Move(PartFile, Target)
            UpdateProgressClose()
            Logger.Info($"格式转换完成：{Target}")
            Return Target
        Catch ex As Exception
            UpdateProgressClose()
            Logger.Error(ex, "调用 FFmpeg 转换失败")
            Hint("调用 FFmpeg 失败，详情请查看日志。", HintType.Red)
            Return Nothing
        End Try
    End Function

#End Region

End Module
