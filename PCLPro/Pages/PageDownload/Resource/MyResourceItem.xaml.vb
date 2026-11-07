Public Class MyResourceItem

#Region "基础属性"
    Public Uuid As Integer = GetUuid()

    Public Property Title As String
        Get
            Return LabTitle.Text
        End Get
        Set(value As String)
            If LabTitle.Text = value Then Return
            LabTitle.Text = value
        End Set
    End Property

    Public Property SubTitle As String
        Get
            Return If(LabTitleRaw?.Text, "")
        End Get
        Set(value As String)
            If LabTitleRaw.Text = value Then Return
            LabTitleRaw.Text = value
            LabTitleRaw.Visibility = If(value = "", Visibility.Collapsed, Visibility.Visible)
        End Set
    End Property

    Public Property Description As String
        Get
            Return LabInfo.Text
        End Get
        Set(value As String)
            If LabInfo.Text = value Then Return
            LabInfo.Text = value
        End Set
    End Property

    Private Sub LabInfo_MouseEnter(sender As Object, e As MouseEventArgs) Handles LabInfo.MouseEnter
        ToolTipInfo.Content = LabInfo.Text
        LabInfo.ToolTip = If(IsTextTrimmed(LabInfo), ToolTipInfo, Nothing)
    End Sub

    Public WriteOnly Property Tags As List(Of String)
        Set(Tags As List(Of String))
            PanTags.Children.Clear()
            PanTags.Visibility = If(Tags.Any(), Visibility.Visible, Visibility.Collapsed)
            For Each TagText In Tags
                Dim BorderTag As New Border With {
                    .Background = New MyColor("#11000000"), .Padding = New Thickness(3, 1, 3, 1), .CornerRadius = New CornerRadius(3),
                    .Margin = New Thickness(0, 0, 3, 0), .SnapsToDevicePixels = True, .UseLayoutRounding = False}
                Dim LabTag As New TextBlock With {
                    .Text = TagText, .Foreground = New MyColor("#868686"), .FontSize = 11}
                BorderTag.Child = LabTag
                PanTags.Children.Add(BorderTag)
            Next
        End Set
    End Property

#End Region

#Region "点击"

    Public Event Click(sender As Object, e As MouseButtonEventArgs)
    Private Sub Button_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles Me.PreviewMouseLeftButtonUp
        If IsMouseDown Then
            RaiseEvent Click(sender, e)
            If e.Handled Then Return
            Logger.Info($"按下资源工程列表项：{LabTitle.Text}")
        End If
    End Sub
    Private Sub MyResourceItem_Click(sender As MyResourceItem, e As EventArgs) Handles Me.Click

        Dim Titles As New List(Of String)
        If FrmMain.PageCurrent.Page = FormMain.PageType.ResourceDetail Then
            For Each Card As MyCard In FrmDownloadResourceDetail.PanResults.Children
                If Card.Title <> "" AndAlso Not Card.IsSwapped Then Titles.Add(Card.Title)
            Next
            Logger.Info($"记录当前已展开的卡片：{String.Join("、", Titles)}")
        End If

        Dim TargetType As ResourceTypes
        Dim TargetInstance As String = Nothing
        Dim TargetLoader As ModLoaders = ModLoaders.None
        If FrmMain.PageCurrent.Page = FormMain.PageType.Download Then

            Select Case FrmMain.PageCurrentSub
                Case FormMain.PageSubType.DownloadMod
                    TargetType = ResourceTypes.Mod
                    TargetInstance = FrmDownloadMod.Content.Loader.Input.GameVersion
                    TargetLoader = FrmDownloadMod.Content.Loader.Input.ModLoaders
                Case FormMain.PageSubType.DownloadPack
                    TargetType = ResourceTypes.ModPack
                    TargetInstance = FrmDownloadPack.Content.Loader.Input.GameVersion
                Case FormMain.PageSubType.DownloadDataPack
                    TargetType = ResourceTypes.DataPack
                    TargetInstance = FrmDownloadDataPack.Content.Loader.Input.GameVersion
                Case FormMain.PageSubType.DownloadResourcePack
                    TargetType = ResourceTypes.ResourcePack
                    TargetInstance = FrmDownloadResourcePack.Content.Loader.Input.GameVersion
                Case FormMain.PageSubType.DownloadShader
                    TargetType = ResourceTypes.Shader
                    TargetInstance = FrmDownloadShader.Content.Loader.Input.GameVersion
                Case FormMain.PageSubType.DownloadMap
                    TargetType = ResourceTypes.Map
                    TargetInstance = FrmDownloadMap.Content.Loader.Input.GameVersion
            End Select
        Else

            TargetType = ResourceTypes.Any
            TargetInstance = FrmMain.PageCurrent.Additional(2)
            TargetLoader = FrmMain.PageCurrent.Additional(3)
        End If
        FrmMain.PageChange(New FormMain.PageStackData With {.Page = FormMain.PageType.ResourceDetail,
                           .Additional = {sender.Tag, Titles, TargetInstance, TargetLoader, TargetType}})
    End Sub

    Private IsMouseDown As Boolean = False
    Private Sub Button_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles Me.PreviewMouseLeftButtonDown
        If IsMouseOver AndAlso CanInteraction Then IsMouseDown = True
    End Sub
    Private Sub Button_MouseLeave(sender As Object, e As Object) Handles Me.MouseLeave, Me.PreviewMouseLeftButtonUp
        IsMouseDown = False
    End Sub

#End Region

#Region "后加载指向背景"

    Private _RectBack As Border = Nothing
    Public ReadOnly Property RectBack As Border
        Get
            If _RectBack Is Nothing Then
                Dim Rect As New Border With {
                    .Name = "RectBack",
                    .CornerRadius = New CornerRadius(3),
                    .RenderTransform = New ScaleTransform(0.8, 0.8),
                    .RenderTransformOrigin = New Point(0.5, 0.5),
                    .BorderThickness = New Thickness(GetWPFSize(1)),
                    .SnapsToDevicePixels = True,
                    .IsHitTestVisible = False,
                    .Opacity = 0
                }
                Rect.SetResourceReference(Border.BackgroundProperty, "ColorBrush7")
                Rect.SetResourceReference(Border.BorderBrushProperty, "ColorBrush6")
                SetColumnSpan(Rect, 999)
                SetRowSpan(Rect, 999)
                Children.Insert(0, Rect)
                _RectBack = Rect

            End If
            Return _RectBack
        End Get
    End Property

#End Region

    Private StateLast As String

    Public Property CanInteraction As Boolean = True
    Public Sub RefreshColor(sender As Object, e As EventArgs) Handles Me.MouseEnter, Me.MouseLeave, Me.MouseLeftButtonDown, Me.MouseLeftButtonUp
        If Not CanInteraction Then Return

        Dim StateNew As String, Time As Integer
        If IsMouseOver Then
            If IsMouseDown Then
                StateNew = "MouseDown"
                Time = 120
            Else
                StateNew = "MouseOver"
                Time = 120
            End If
        Else
            StateNew = "Idle"
            Time = 180
        End If
        If StateLast = StateNew Then Return
        StateLast = StateNew

        If IsLoaded AndAlso AniControlEnabled = 0 Then

            Dim Ani As New List(Of AniData)
            If IsMouseOver Then
                Ani.AddRange({
                    AaColor(RectBack, Border.BackgroundProperty, If(IsMouseDown, "ColorBrush6", "ColorBrushBg1"), Time),
                    AaOpacity(RectBack, 1 - RectBack.Opacity, Time,, New AniEaseOutFluent)
                })
                If IsMouseDown Then
                    Ani.Add(AaScaleTransform(RectBack, 0.996 - CType(RectBack.RenderTransform, ScaleTransform).ScaleX, Time * 1.2,, New AniEaseOutFluent))
                Else
                    Ani.Add(AaScaleTransform(RectBack, 1 - CType(RectBack.RenderTransform, ScaleTransform).ScaleX, Time * 1.2,, New AniEaseOutFluent))
                End If
            Else
                Ani.AddRange({
                    AaOpacity(RectBack, -RectBack.Opacity, Time),
                    AaColor(RectBack, Border.BackgroundProperty, If(IsMouseDown, "ColorBrush6", "ColorBrush7"), Time),
                    AaScaleTransform(RectBack, 0.996 - CType(RectBack.RenderTransform, ScaleTransform).ScaleX, Time,, New AniEaseOutFluent),
                    AaScaleTransform(RectBack, -0.196, 1,,, True)
                })
            End If
            AniStart(Ani, "MyResourceItem Color " & Uuid)
        Else

            AniStop("MyResourceItem Color " & Uuid)
            If _RectBack IsNot Nothing Then RectBack.Opacity = 0
        End If
    End Sub

End Class
