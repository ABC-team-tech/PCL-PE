Public Class MyImage
    Inherits Image

#Region "公开属性"

    Public FileCacheExpiredTime As New TimeSpan(7, 0, 0, 0)

    Public Property EnableCache As Boolean
        Get
            Return GetValue(EnableCacheProperty)
        End Get
        Set(value As Boolean)
            SetValue(EnableCacheProperty, value)
        End Set
    End Property
    Public Shared Shadows ReadOnly EnableCacheProperty As DependencyProperty = DependencyProperty.Register(
        "EnableCache", GetType(Boolean), GetType(MyImage), New PropertyMetadata(True))

    Public Shadows Property Source As String
        Get
            Return _Source
        End Get
        Set(value As String)
            If value = "" Then value = Nothing
            If _Source = value Then Return
            _Source = value
            If Not IsInitialized Then Return
            Load()
        End Set
    End Property
    Private _Source As String = ""
    Public Shared Shadows ReadOnly SourceProperty As DependencyProperty = DependencyProperty.Register(
        "Source", GetType(String), GetType(MyImage), New PropertyMetadata(New PropertyChangedCallback(
    Sub(sender, e) If sender IsNot Nothing Then CType(sender, MyImage).Source = e.NewValue.ToString())))

    Public Property FallbackSource As String
        Get
            Return _FallbackSource
        End Get
        Set(value As String)
            _FallbackSource = value
        End Set
    End Property
    Private _FallbackSource As String = Nothing

    Public Property LoadingSource As String
        Get
            Return _LoadingSource
        End Get
        Set(value As String)
            _LoadingSource = value
        End Set
    End Property
    Private _LoadingSource As String = "pack://application:,,,/images/Icons/NoIcon.png"

#End Region

    Public Property ActualSource As String
        Get
            Return _ActualSource
        End Get
        Set(value As String)
            If value = "" Then value = Nothing
            If _ActualSource = value Then Return
            _ActualSource = value
            Try
                Dim Bitmap As ImageSource = If(value Is Nothing, Nothing, New MyBitmap(value))
                RunInUi(Sub() MyBase.Source = Bitmap)
            Catch ex As Exception
                Logger.Warn(ex, $"加载图片失败（{value}）")
                Try
                    If value.StartsWithF(PathTemp, True) Then FileUtils.Delete(value)
                Catch
                End Try
            End Try
        End Set
    End Property
    Private _ActualSource As String = Nothing

    Private Sub Load() _
        Handles Me.Initialized

        If Source Is Nothing Then
            ActualSource = Nothing
            Return
        End If

        If Not Source.StartsWithF("http") Then
            ActualSource = Source
            Return
        End If

        Dim EnableCache As Boolean = Me.EnableCache
        Dim TempPath As String = $"{PathTemp}MyImage\{Source.GetStableHashCode()}.png{If(EnableCache, "", GetUuid())}"
        Dim TempFile = FileUtils.GetInfo(TempPath)
        If EnableCache AndAlso TempFile.Exists Then
            ActualSource = TempPath
            If (Date.Now - TempFile.LastWriteTime) < FileCacheExpiredTime Then Return
        Else
            ActualSource = LoadingSource
        End If
        RunInNewThread(
        Sub()
            Dim IsLocalFallback As Boolean = Not String.IsNullOrEmpty(FallbackSource) AndAlso Not FallbackSource.StartsWithF("http")
            Try

                NetDownloadByLoader(
                    If(FallbackSource IsNot Nothing AndAlso FallbackSource.StartsWithF("http") AndAlso Source <> FallbackSource,
                        {Source, FallbackSource}, {Source}),
                    TempPath, SimulateBrowserHeaders:=True)
                ActualSource = TempPath
                If Not EnableCache Then FileUtils.Delete(TempPath)
            Catch ex As Exception
                Try
                    If TempPath IsNot Nothing Then FileUtils.Delete(TempPath)
                Catch
                End Try

                If IsLocalFallback Then
                    Try
                        ActualSource = FallbackSource
                        Logger.Warn(ex, $"下载图片失败，使用本地备用图片（{Source}）")
                        Return
                    Catch exx As Exception
                        Logger.Error(ex, $"下载图片失败（{Source}）", LogBehavior.Toast)
                        Logger.Error(exx, $"加载备用图片失败（{FallbackSource}）", LogBehavior.Toast)
                    End Try
                Else
                    Logger.Error(ex, $"下载图片失败（{Source}，备用：{FallbackSource}）", LogBehavior.Toast)
                End If
            End Try
        End Sub, "MyImage PicLoader " & GetUuid() & "#", ThreadPriority.BelowNormal)
    End Sub

End Class
