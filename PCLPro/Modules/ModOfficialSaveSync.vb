Imports System.IO

Public Module ModOfficialSaveSync

    Private HasAutoChecked As Boolean = False

    Public Class SaveItem
        Public Property Source As String
        Public Property Name As String
    End Class

    Public Sub StartAutoCheck()
        If HasAutoChecked Then Return
        HasAutoChecked = True
        If Not Settings.Get(Of Boolean)("UiAutoSyncOfficialSaves") Then Return
        RunInNewThread(
            Sub()
                Try
                    Thread.Sleep(2500)
                    RunInUiWait(Sub() CheckAndSync(False))
                Catch ex As Exception
                    Logger.Warn(ex, "自动检测官方存档失败")
                End Try
            End Sub, "官方存档同步")
    End Sub

    Public Function GetTargetSavesFolder() As String
        Dim Folder As String = McFolderSelected
        If String.IsNullOrWhiteSpace(Folder) Then Return Nothing
        Return Folder & "saves\"
    End Function

    Public Function GetCandidateFolders() As List(Of String)
        Dim Raw As String() = {
            Paths.AppData & ".minecraft\",
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) & "\.minecraft\",
            Paths.AppDataThenName & ".minecraft\",
            Paths.Base & ".minecraft\",
            Paths.AppData & "PCL2\.minecraft\",
            Paths.AppData & ".hmcl\.minecraft\"
        }
        Dim Result As New List(Of String)
        For Each Folder As String In Raw
            If String.IsNullOrWhiteSpace(Folder) Then Continue For
            Dim Fixed As String = Folder.Replace("/", "\")
            If Not Fixed.EndsWith("\") Then Fixed &= "\"
            If Directory.Exists(Fixed) AndAlso Not Result.Contains(Fixed) Then Result.Add(Fixed)
        Next
        Return Result
    End Function

    Public Function ScanPendingSaves() As List(Of SaveItem)
        Dim Result As New List(Of SaveItem)
        Dim Target As String = GetTargetSavesFolder()
        If Target Is Nothing Then Return Result

        Dim Known As New List(Of String)
        If Directory.Exists(Target) Then
            For Each Dir As String In Directory.GetDirectories(Target)
                Known.Add(IO.Path.GetFileName(Dir).ToLowerInvariant())
            Next
        End If
        For Each Folder As String In GetCandidateFolders()
            If Target.StartsWith(Folder, StringComparison.OrdinalIgnoreCase) Then Continue For
            Dim Saves As String = Folder & "saves\"
            If Not Directory.Exists(Saves) Then Continue For
            Dim Dirs As String()
            Try
                Dirs = Directory.GetDirectories(Saves)
            Catch ex As Exception
                Logger.Warn(ex, $"枚举存档目录失败：{Saves}")
                Continue For
            End Try
            For Each Dir As String In Dirs
                Try
                    If Not File.Exists(Dir & "\level.dat") Then Continue For
                    Dim Name As String = IO.Path.GetFileName(Dir)
                    Dim Key As String = Name.ToLowerInvariant()
                    If Known.Contains(Key) Then Continue For
                    Known.Add(Key)
                    Result.Add(New SaveItem With {.Source = Dir, .Name = Name})
                Catch ex As Exception
                    Logger.Warn(ex, $"检测存档失败：{Dir}")
                End Try
            Next
        Next
        Return Result
    End Function

    Public Function CheckAndSync(IsManual As Boolean) As Integer
        If Not IsManual AndAlso Not Settings.Get(Of Boolean)("UiAutoSyncOfficialSaves") Then Return 0
        Dim Target As String = GetTargetSavesFolder()
        If Target Is Nothing Then
            If IsManual Then Hint("还没有选择游戏文件夹，无法同步存档！", HintType.Red)
            Return 0
        End If
        Dim Pending As List(Of SaveItem)
        Try
            Pending = ScanPendingSaves()
        Catch ex As Exception
            Logger.Warn(ex, "扫描官方存档失败")
            If IsManual Then Hint("扫描存档失败，详情请查看日志。", HintType.Red)
            Return 0
        End Try
        If Pending.Count = 0 Then
            If IsManual Then Hint("没有发现需要同步的官方存档。", HintType.Green)
            Return 0
        End If

        Dim Names As New List(Of String)
        For i As Integer = 0 To Math.Min(Pending.Count, 6) - 1
            Names.Add("· " & Pending(i).Name)
        Next
        Dim ListText As String = String.Join(vbCrLf, Names)
        If Pending.Count > 6 Then ListText &= vbCrLf & $"……共 {Pending.Count} 个存档"
        Dim Content As String = $"检测到官方版 PCL2 中有 {Pending.Count} 个存档尚未同步到当前游戏文件夹：{vbCrLf}{vbCrLf}{ListText}{vbCrLf}{vbCrLf}是否复制过来？（同名存档不会被覆盖）"
        Dim Choice As Integer = MyMsgBox(Content, "同步官方存档", "立即同步", "以后再说")
        If Choice <> 1 Then Return 0
        Dim Copied As Integer = 0
        Try
            Directory.CreateDirectory(Target)
        Catch ex As Exception
            Logger.Warn(ex, "创建存档目录失败")
            Hint("创建存档目录失败，请检查游戏文件夹权限。", HintType.Red)
            Return 0
        End Try
        For Each Item As SaveItem In Pending
            Try
                Dim Dest As String = Target & Item.Name
                If Directory.Exists(Dest) Then Continue For
                DirectoryUtils.Copy(Item.Source, Dest)
                Copied += 1
                Logger.Info($"已同步存档：{Item.Source} → {Dest}")
            Catch ex As Exception
                Logger.Warn(ex, $"同步存档失败：{Item.Name}")
            End Try
        Next
        If Copied = 0 Then
            Hint("存档同步失败，详情请查看日志。", HintType.Red)
        Else
            Hint($"已同步 {Copied} 个存档到当前游戏文件夹！", HintType.Green)
        End If
        Return Copied
    End Function

End Module
