Public Class PageSetupLink

    Private Sub PageSetupLink_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        PanBack.ScrollToHome()

        Static IsLoaded As Boolean = False
        If IsLoaded Then Return
        IsLoaded = True

        AniControlEnabled += 1
        Reload()
        AniControlEnabled -= 1

    End Sub
    Public Sub Reload()
        SettingService.RefreshSettings(Me)
    End Sub

    Public Sub Reset()
        Try
            SettingService.ResetSettings(Me)
            Logger.Info("已初始化联机页设置")
            Hint("已初始化联机页设置！", HintType.Green, False)
        Catch ex As Exception
            Logger.Error(ex, "初始化联机页设置失败", LogBehavior.Alert)
        End Try
        Reload()
    End Sub

End Class
