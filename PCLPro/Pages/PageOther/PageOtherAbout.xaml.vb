Public Class PageOtherAbout

    Private Shadows IsLoaded As Boolean = False
    Private Sub PageOtherAbout_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        PanBack.ScrollToHome()

        If IsLoaded Then Return
        IsLoaded = True

        ItemAboutPcl.Info = ItemAboutPcl.Info.Replace("%VERSION%", VersionDisplay).Replace("%VERSIONCODE%", VersionCode).Replace("%BRANCH%", CInt(BuildType))

        RunInNewThread(
        Sub()
            Try
                Dim ResultJson As String = NetRequestByClient("https://api.github.com/repos/Hex-Dragon/PCL2/contributors?per_page=100",
                                                              HttpMethod.Get, SimulateBrowserHeaders:=True)
                Dim Names As New List(Of String)
                For Each Entry In JArray.Parse(ResultJson)
                    Names.Add(Entry("login").ToString())
                Next
                If Names.Count = 0 Then Throw New Exception("名单为空")
                RunInUi(Sub() LabThanks.Text = Names.Join("、"))
            Catch ex As Exception
                Logger.Warn($"获取贡献者名单失败：{ex.Message}")
                RunInUi(Sub() LabThanks.Text = "龙腾猫跃，以及每一位参与 PCL 开源仓库建设的贡献者与玩家。" & vbCrLf & "（在线名单暂时无法获取，可以稍后在联网时重新打开本页面）")
            End Try
        End Sub, "贡献者名单加载")

    End Sub

    Public Shared Sub CopyIdentify()
        ClipboardSet(Identify)
    End Sub

    Private Sub BtnAboutUpdate_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnAboutUpdate.Click
        UpdateCheckByButton()
    End Sub

End Class
