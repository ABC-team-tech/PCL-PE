Imports NAudio.Wave

Public Module ModMusic

#Region "播放列表"

    Public MusicWaitingList As List(Of String) = Nothing

    Public MusicAllList As List(Of String) = Nothing

    Private Sub MusicListInit(ForceReload As Boolean, Optional PreventFirst As String = Nothing)
        If ForceReload Then MusicAllList = Nothing
        Try

            If MusicAllList Is Nothing Then
                MusicAllList = New List(Of String)
                DirectoryUtils.Create(Paths.Base & "PCL\Musics\")
                For Each File In DirectoryUtils.EnumerateFiles(Paths.Base & "PCL\Musics\", True)

                    If ModMedia.IsAudioFile(File) Then MusicAllList.Add(File)
                Next
            End If

            MusicWaitingList = If(Settings.Get(Of Boolean)("UiMusicRandom"), New List(Of String)(MusicAllList).Shuffle().ToList, New List(Of String)(MusicAllList))
            If PreventFirst IsNot Nothing AndAlso MusicWaitingList.FirstOrDefault = PreventFirst Then

                MusicWaitingList.RemoveAt(0)
                MusicWaitingList.Add(PreventFirst)
            End If
        Catch ex As Exception
            Logger.Error(ex, "初始化音乐列表失败")
        End Try
    End Sub

    Private Function DequeueNextMusicAddress() As String
        Dim Result As String

        If MusicAllList Is Nothing OrElse Not MusicAllList.Any() OrElse Not MusicWaitingList.Any() Then MusicListInit(False)

        If MusicWaitingList.Any() Then
            Result = MusicWaitingList(0)
            MusicWaitingList.RemoveAt(0)
        Else
            Result = Nothing
        End If
        If Not MusicWaitingList.Any() Then MusicListInit(False, Result)
        Return Result
    End Function

#End Region

#Region "UI 控制"

    Private Sub MusicRefreshUI()
        RunInUi(
        Sub()
            Try

                If Not MusicAllList.Any() Then

                    FrmMain.BtnExtraMusic.Show = False
                Else

                    FrmMain.BtnExtraMusic.Show = True
                    Dim ToolTipText As String
                    If MusicState = MusicStates.Pause Then
                        FrmMain.BtnExtraMusic.Logo = Logo.IconPlay
                        FrmMain.BtnExtraMusic.LogoScale = 0.8
                        ToolTipText = "已暂停：" & PathUtils.GetFileNameWithoutExtension(MusicCurrent)
                        If MusicAllList.Count > 1 Then
                            ToolTipText += vbCrLf & "左键恢复播放，右键播放下一曲。"
                        Else
                            ToolTipText += vbCrLf & "左键恢复播放，右键重新从头播放。"
                        End If
                    Else
                        FrmMain.BtnExtraMusic.Logo = Logo.IconMusic
                        FrmMain.BtnExtraMusic.LogoScale = 1
                        ToolTipText = "正在播放：" & PathUtils.GetFileNameWithoutExtension(MusicCurrent)
                        If MusicAllList.Count > 1 Then
                            ToolTipText += vbCrLf & "左键暂停，右键播放下一曲。"
                        Else
                            ToolTipText += vbCrLf & "左键暂停，右键重新从头播放。"
                        End If
                    End If
                    FrmMain.BtnExtraMusic.ToolTip = ToolTipText
                End If
                If FrmSetupUI IsNot Nothing Then FrmSetupUI.MusicRefreshUI()

            Catch ex As Exception
                Logger.Error(ex, "刷新背景音乐 UI 失败")
            End Try
        End Sub)
    End Sub

    Public Sub MusicControlPause()
        If MusicNAudio Is Nothing Then
            Hint("音乐播放尚未开始！", HintType.Red)
        Else
            Select Case MusicState
                Case MusicStates.Pause
                    MusicResume()
                Case MusicStates.Play
                    MusicPause()
                Case Else
                    Logger.Warn("音乐目前为停止状态，已强制尝试开始播放")
                    MusicRefreshPlay(False)
            End Select
        End If
    End Sub

    Public Sub MusicControlNext()
        If MusicAllList.IsSingle Then
            MusicStartPlay(MusicCurrent)
            Hint("重新播放：" & PathUtils.GetLastPart(MusicCurrent), HintType.Green)
        Else
            Dim Address As String = DequeueNextMusicAddress()
            If Address Is Nothing Then
                Hint("没有可以播放的音乐！", HintType.Red)
            Else
                MusicStartPlay(Address)
                Hint("正在播放：" & PathUtils.GetLastPart(Address), HintType.Green)
            End If
        End If
        MusicRefreshUI()
    End Sub

#End Region

#Region "主状态控制"

    Public ReadOnly Property MusicState As MusicStates
        Get
            If MusicNAudio Is Nothing Then Return MusicStates.Stop
            Select Case MusicNAudio.PlaybackState
                Case 0
                    Return MusicStates.Stop
                Case 2
                    Return MusicStates.Pause
                Case Else
                    Return MusicStates.Play
            End Select
        End Get
    End Property
    Public Enum MusicStates
        [Stop]
        Play
        Pause
    End Enum

    Public Sub MusicRefreshPlay(ShowHint As Boolean, Optional IsFirstLoad As Boolean = False)
        Try

            MusicListInit(True)
            If Not MusicAllList.Any() Then
                If MusicNAudio Is Nothing Then
                    If ShowHint Then Hint("未检测到可用的背景音乐！", HintType.Red)
                Else
                    MusicNAudio = Nothing
                    If ShowHint Then Hint("背景音乐已清除！", HintType.Green)
                End If
            Else
                Dim Address As String = DequeueNextMusicAddress()
                If Address Is Nothing Then
                    If ShowHint Then Hint("没有可以播放的音乐！", HintType.Red)
                Else
                    Try
                        MusicStartPlay(Address, IsFirstLoad)
                        If ShowHint Then Hint("背景音乐已刷新：" & PathUtils.GetLastPart(Address), HintType.Green, False)
                    Catch
                    End Try
                End If
            End If
            MusicRefreshUI()

        Catch ex As Exception
            Logger.Error(ex, "刷新背景音乐播放失败")
        End Try
    End Sub

    Private Sub MusicStartPlay(Address As String, Optional IsFirstLoad As Boolean = False)
        If Address Is Nothing Then Return
        Logger.Info($"播放开始：{Address}")
        MusicCurrent = Address
        RunInNewThread(Sub() MusicLoop(IsFirstLoad), "Music", ThreadPriority.BelowNormal)
    End Sub

    Public Function MusicPause() As Boolean
        If MusicState = MusicStates.Play Then
            RunInThread(
            Sub()
                Logger.Info("已暂停播放")
                MusicNAudio?.Pause()
                MusicRefreshUI()
            End Sub)
            Return True
        Else
            Logger.Info($"无需暂停播放，当前状态为 {MusicState}")
            Return False
        End If
    End Function

    Public Function MusicResume() As Boolean
        If MusicState = MusicStates.Play OrElse Not MusicAllList.Any() Then
            Logger.Info($"无需继续播放，当前状态为 {MusicState}")
            Return False
        Else
            RunInThread(
            Sub()
                Logger.Info("已恢复播放")
                Try
                    MusicNAudio?.Play()
                Catch
                    MusicNAudio?.Stop()
                    MusicNAudio?.Play()
                End Try
                MusicRefreshUI()
            End Sub)
            Return True
        End If
    End Function

#End Region

    Public MusicNAudio = Nothing

    Private MusicCurrent As String = ""

    Private Function MusicCreateReader(Address As String) As WaveStream
        Try
            Return New AudioFileReader(Address)
        Catch ex As Exception
            Logger.Warn(ex, $"AudioFileReader 无法加载，换用 MediaFoundationReader 重试（{PathUtils.GetLastPart(Address)}）")
        End Try
        Try
            Return New MediaFoundationReader(Address)
        Catch ex As Exception
            Logger.Warn(ex, $"MediaFoundationReader 也无法加载，稍后尝试 FFmpeg 转换（{PathUtils.GetLastPart(Address)}）")
        End Try
        Dim Converted As String = ModMedia.PrepareAudioForPlayback(Address)
        If Converted Is Nothing Then Throw New InvalidOperationException("不支持的音乐格式：" & PathUtils.GetExtension(Address))
        Return New AudioFileReader(Converted)
    End Function

    Private Sub MusicLoop(Optional IsFirstLoad As Boolean = False)
        Dim CurrentWave As WaveOutEvent = Nothing
        Dim Reader As WaveStream = Nothing
        Try

            CurrentWave = New WaveOutEvent()
            MusicNAudio = CurrentWave
            CurrentWave.DeviceNumber = -1
            Try
                Reader = MusicCreateReader(MusicCurrent)
            Catch ex As Exception
                Logger.Warn(ex, "加载音频文件失败，尝试使用 FFmpeg 转换后播放")
                Dim Converted As String = ModMedia.PrepareAudioForPlayback(MusicCurrent)
                If Converted Is Nothing Then Throw
                Reader = New AudioFileReader(Converted)
            End Try
            CurrentWave.Init(Reader)
            CurrentWave.Play()

            If IsFirstLoad AndAlso Not Settings.Get(Of Boolean)("UiMusicAuto") Then CurrentWave.Pause()
            MusicRefreshUI()

            Dim PreviousVolume = 0
            While CurrentWave.Equals(MusicNAudio) AndAlso Not CurrentWave.PlaybackState = PlaybackState.Stopped
                If Settings.Get(Of Integer)("UiMusicVolume") <> PreviousVolume Then

                    PreviousVolume = Settings.Get(Of Integer)("UiMusicVolume")
#Disable Warning BC40008
                    CurrentWave.Volume = PreviousVolume / 1000
#Enable Warning BC40008
                End If

                Dim Percent = Reader.CurrentTime.TotalMilliseconds / Reader.TotalTime.TotalMilliseconds
                RunInUi(Sub() FrmMain.BtnExtraMusic.Progress = Percent)
                Thread.Sleep(100)
            End While

            If CurrentWave.PlaybackState = PlaybackState.Stopped AndAlso MusicAllList.Any Then MusicStartPlay(DequeueNextMusicAddress)
        Catch ex As Exception
            Logger.Warn(ex, $"播放音乐出现内部错误（{MusicCurrent}）")
            If TypeOf ex Is NAudio.MmException AndAlso ex.Message.Contains("AlreadyAllocated") Then
                Hint("你的音频设备正被其他程序占用。请在关闭占用的程序后重启 PCL PE，才能恢复音乐播放功能！", HintType.Red)
                Thread.Sleep(1000000000)
            End If
            If TypeOf ex Is NAudio.MmException AndAlso (ex.Message.Contains("NoDriver") OrElse ex.Message.Contains("BadDeviceId")) Then
                Hint("由于音频设备变更，音乐播放功能在重启 PCL 后才能恢复！", HintType.Red)
                Thread.Sleep(1000000000)
            End If
            If Not ModMedia.IsAudioFile(MusicCurrent) OrElse
                ex.Message.Contains("0xC00D36C4") Then
                Hint("播放音乐失败（" & PathUtils.GetLastPart(MusicCurrent) & "）：该文件可能已损坏，或者转换失败。可尝试把音乐转换为 .wav、.mp3 或 .flac 后重试", HintType.Red)
            Else
                Logger.Error(ex, $"播放音乐失败（{PathUtils.GetLastPart(MusicCurrent)}）", LogBehavior.Toast)
            End If

            MusicAllList.Remove(MusicCurrent)
            MusicWaitingList.Remove(MusicCurrent)
            MusicRefreshUI()

            Thread.Sleep(2000)
            If TypeOf ex Is FileNotFoundException Then
                MusicRefreshPlay(True, IsFirstLoad)
            Else
                MusicStartPlay(DequeueNextMusicAddress(), IsFirstLoad)
            End If
        Finally
            If CurrentWave IsNot Nothing Then CurrentWave.Dispose()
            If Reader IsNot Nothing Then Reader.Dispose()
            MusicRefreshUI()
        End Try
    End Sub

End Module
