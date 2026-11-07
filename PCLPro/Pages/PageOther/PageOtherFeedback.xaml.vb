Public Class PageOtherFeedback

    Private Sub BtnCopyQQ_Click(sender As Object, e As RoutedEventArgs) Handles BtnCopyQQ.Click
        ClipboardSet("1125298021")
        Hint("已复制作者 QQ 群号", HintType.Green)
    End Sub

    Private Sub BtnOpenLog_Click(sender As Object, e As RoutedEventArgs) Handles BtnOpenLog.Click
        Try
            OpenExplorer(Paths.Base & "PCL\Log1.txt")
        Catch ex As Exception
            Logger.Error(ex, "打开日志文件夹失败")
            Hint("打开日志文件夹失败：" & ex.Message, HintType.Red)
        End Try
    End Sub

End Class
