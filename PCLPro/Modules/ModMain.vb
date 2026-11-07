Imports System.Collections.ObjectModel
Imports System.Windows.Interop
Imports System.Windows.Threading

Public Module ModMain

#Region "弹出提示"

    Public Enum HintType
        Blue
        Green
        Red
    End Enum
    Private Structure HintMessage
        Public Text As String
        Public Type As HintType
        Public Log As Boolean
    End Structure

    Private HintWaiting As New ConcurrentQueue(Of HintMessage)

    Public Sub Hint(Text As String, Optional Type As HintType = HintType.Blue, Optional Log As Boolean = True)
        HintWaiting.Enqueue(New HintMessage With {.Text = If(Text, ""), .Type = Type, .Log = Log})
    End Sub

    Private Sub HintTick()
        Try

            Dim CurrentHint As HintMessage = Nothing
            Do While HintWaiting.TryDequeue(CurrentHint)

                CurrentHint.Text = CurrentHint.Text.ReplaceLineEndings(" ", mergeMultiple:=True)

                If FrmMain.PanHint.Children.Count >= 20 Then GoTo EndHint

                Dim DoubleStack As Border = Nothing
                For Each stack As Border In FrmMain.PanHint.Children
                    If stack.Tag(0) AndAlso CType(stack.Child, TextBlock).Text = CurrentHint.Text Then DoubleStack = stack
                Next

                Dim TargetColor0, TargetColor1 As MyColor
                Dim Percent As Double = 0.3
                Select Case CurrentHint.Type
                    Case HintType.Blue
                        TargetColor0 = New MyColor(215, 37, 155, 252)
                        TargetColor1 = New MyColor(215, 10, 142, 252)
                    Case HintType.Green
                        TargetColor0 = New MyColor(215, 33, 177, 33)
                        TargetColor1 = New MyColor(215, 29, 160, 29)
                    Case Else
                        TargetColor0 = New MyColor(215, 255, 53, 11)
                        TargetColor1 = New MyColor(215, 255, 43, 0)
                End Select
                If Not IsNothing(DoubleStack) Then

                    If Not AniIsRun("Hint Show " & DoubleStack.Tag(1)) Then
                        AniStop("Hint Hide " & DoubleStack.Tag(1))
                        Dim Delay As Double = (800 + CurrentHint.Text.Length.Clamp(5, 23) * 180) * AniSpeed
                        AniStart({
                            AaX(DoubleStack, -12 - DoubleStack.Margin.Left, 50,, New AniEaseOutFluent),
                            AaX(DoubleStack, -8, 50, 50, New AniEaseInFluent),
                            AaX(DoubleStack, 8, 50, 100, New AniEaseOutFluent),
                            AaX(DoubleStack, -8, 50, 150, New AniEaseInFluent),
                            AaDouble(Sub(i)
                                         Percent += i
                                         Dim Gradient As LinearGradientBrush = DoubleStack.Background
                                         Gradient.GradientStops(0).Color = TargetColor0 * Percent + New MyColor(255, 255, 255) * (1 - Percent)
                                         Gradient.GradientStops(1).Color = TargetColor1 * Percent + New MyColor(255, 255, 255) * (1 - Percent)
                                     End Sub, 0.7, 250),
                            AaX(DoubleStack, -50, 200, Delay, New AniEaseInFluent),
                            AaOpacity(DoubleStack, -1, 150, Delay),
                            AaCode(Sub() DoubleStack.Tag(0) = False, Delay),
                            AaHeight(DoubleStack, -26, 100,, New AniEaseOutFluent, True),
                            AaCode(Sub() FrmMain.PanHint.Children.Remove(DoubleStack), , True)
                      }, "Hint Hide " & DoubleStack.Tag(1))
                    End If
                Else

                    Dim NewHintControl As New Border With {.Tag = {True, GetUuid()}, .Margin = New Thickness(-70, 0, 20, 0), .Opacity = 0, .Height = 0, .HorizontalAlignment = HorizontalAlignment.Left, .CornerRadius = New CornerRadius(0, 6, 6, 0)}
                    NewHintControl.Background = New LinearGradientBrush(New GradientStopCollection(New List(Of GradientStop) From {
                        New GradientStop(TargetColor0 * Percent + New MyColor(255, 255, 255) * (1 - Percent), 0),
                        New GradientStop(TargetColor1 * Percent + New MyColor(255, 255, 255) * (1 - Percent), 1)}), 90)
                    NewHintControl.Child = New TextBlock With {.TextTrimming = TextTrimming.CharacterEllipsis, .FontSize = 13, .Text = CurrentHint.Text, .Foreground = New MyColor(255, 255, 255), .Margin = New Thickness(33, 5, 8, 5)}

                    FrmMain.PanHint.Children.Add(NewHintControl)

                    Dim Animations As New List(Of AniData)
                    If FrmMain.PanHint.Children.Count > 1 Then

                        Animations.Add(AaHeight(NewHintControl, 26, 150, , New AniEaseOutFluent))
                    Else

                        NewHintControl.Height = 26
                    End If

                    Animations.AddRange({
                        AaX(NewHintControl, 30, 400, , New AniEaseOutElastic(AniEasePower.Weak)),
                        AaX(NewHintControl, 20, 220, , New AniEaseOutFluent),
                        AaOpacity(NewHintControl, 1, 100),
                        AaDouble(Sub(i)
                                     Percent += i
                                     Dim Gradient As LinearGradientBrush = NewHintControl.Background
                                     Gradient.GradientStops(0).Color = TargetColor0 * Percent + New MyColor(255, 255, 255) * (1 - Percent)
                                     Gradient.GradientStops(1).Color = TargetColor1 * Percent + New MyColor(255, 255, 255) * (1 - Percent)
                                 End Sub, 0.7, 250, 100)
                    })
                    AniStart(Animations, "Hint Show " & NewHintControl.Tag(1))

                    Dim Delay As Double = (800 + CurrentHint.Text.Length.Clamp(5, 23) * 180) * AniSpeed
                    AniStart({
                        AaX(NewHintControl, -50, 200, Delay, New AniEaseInFluent),
                        AaOpacity(NewHintControl, -1, 150, Delay),
                        AaCode(Sub() NewHintControl.Tag(0) = False, Delay),
                        AaHeight(NewHintControl, -26, 100,, New AniEaseOutFluent, True),
                        AaCode(Sub() FrmMain.PanHint.Children.Remove(NewHintControl), , True)
                    }, "Hint Hide " & NewHintControl.Tag(1))
                End If

EndHint:
                If CurrentHint.Log Then Logger.Info($"弹出提示：{CurrentHint.Text}")
            Loop
        Catch ex As Exception
            Logger.Info(ex, "显示弹出提示失败")
        End Try
    End Sub
    Private Sub HideAllHint()
        For Each Control As Border In FrmMain.PanHint.Children
            Control.IsHitTestVisible = False
            AniStart({
                AaX(Control, -50, 200, , New AniEaseInFluent),
                AaOpacity(Control, -1, 150, , New AniEaseInFluent),
                AaCode(Sub() Control.Tag(0) = False),
                AaHeight(Control, -26, 100,, New AniEaseOutFluent, True),
                AaCode(Sub() FrmMain.PanHint.Children.Remove(Control), , True)
            }, "Hint Hide " & Control.Tag(1))
        Next
    End Sub

#End Region

#Region "弹窗"

    Public Class MyMsgBoxConverter
        Public Type As MyMsgBoxType
        Public Title As String
        Public Text As String

        Public Content As Object

        Public ValidateRules As Collection(Of Validate)

        Public HintText As String = ""

        Public HighLight As Boolean
        Public Button1 As String = "确定"
        Public Button2 As String = ""
        Public Button3 As String = ""

        Public Button1Action As Action = Nothing

        Public Button2Action As Action = Nothing

        Public Button3Action As Action = Nothing
        Public IsWarn As Boolean = False
        Public ForceWait As Boolean = False
        Public WaitFrame As New DispatcherFrame(True)

        Public IsExited As Boolean = False

        Public Result As Object
    End Class
    Public Enum MyMsgBoxType
        Text
        [Select]
        Input
        Login
    End Enum

    Public Function MyMsgBox(Caption As String, Optional Title As String = "提示",
                             Optional Button1 As String = "确定", Optional Button2 As String = "", Optional Button3 As String = "",
                             Optional IsWarn As Boolean = False, Optional HighLight As Boolean = True, Optional ForceWait As Boolean = False,
                             Optional Button1Action As Action = Nothing, Optional Button2Action As Action = Nothing, Optional Button3Action As Action = Nothing) As Integer

        Dim Converter As New MyMsgBoxConverter With {.Type = MyMsgBoxType.Text, .Button1 = Button1, .Button2 = Button2, .Button3 = Button3, .Text = Caption, .IsWarn = IsWarn, .Title = Title, .HighLight = HighLight, .ForceWait = True, .Button1Action = Button1Action, .Button2Action = Button2Action, .Button3Action = Button3Action}
        If Button2.Length > 0 OrElse ForceWait Then

            If FrmMain Is Nothing OrElse FrmMain.PanMsg Is Nothing AndAlso RunInUi() Then

                If Button2.Length > 0 Then
                    Dim RawResult As MsgBoxResult =
                        MsgBox(Caption, If(Button3.Length > 0, MsgBoxStyle.YesNoCancel, MsgBoxStyle.YesNo) + If(IsWarn, MsgBoxStyle.Critical, MsgBoxStyle.Question), Title)
                    Select Case RawResult
                        Case MsgBoxResult.Yes
                            Converter.Result = 1
                        Case MsgBoxResult.No
                            Converter.Result = 2
                        Case MsgBoxResult.Cancel
                            Converter.Result = 3
                    End Select
                Else
                    MsgBox(Caption, MsgBoxStyle.OkOnly + If(IsWarn, MsgBoxStyle.Critical, MsgBoxStyle.Question), Title)
                    Converter.Result = 1
                End If
                Logger.Warn($"主窗体加载完成前出现意料外的等待弹窗：{Button1},{Button2},{Button3}")
            Else
                WaitingMyMsgBox.Enqueue(Converter)
                Try
                    FrmMain.DragStop()
                    If RunInUi() Then MyMsgBoxTick()
                    ComponentDispatcher.PushModal()
                    Dispatcher.PushFrame(Converter.WaitFrame)
                Finally
                    ComponentDispatcher.PopModal()
                End Try
            End If
            Logger.Info($"普通弹框返回：{If(Converter.Result, "null")}")
            Return Converter.Result
        Else

            WaitingMyMsgBox.Enqueue(Converter)
            Return 1
        End If
    End Function

    Public Function MyMsgBoxInput(Title As String, Optional Text As String = Nothing, Optional DefaultInput As String = Nothing, Optional ValidateRules As Collection(Of Validate) = Nothing, Optional HintText As String = "", Optional Button1 As String = "确定", Optional Button2 As String = "取消", Optional IsWarn As Boolean = False) As String

        Dim Converter As New MyMsgBoxConverter With {.Text = If(Text, ""), .HintText = If(HintText, ""), .Type = MyMsgBoxType.Input, .ValidateRules = If(ValidateRules, New Collection(Of Validate)), .Button1 = Button1, .Button2 = Button2, .Content = DefaultInput, .IsWarn = IsWarn, .Title = Title}
        WaitingMyMsgBox.Enqueue(Converter)

        Try
            If FrmMain IsNot Nothing Then FrmMain.DragStop()
            If RunInUi() Then MyMsgBoxTick()
            ComponentDispatcher.PushModal()
            Dispatcher.PushFrame(Converter.WaitFrame)
        Finally
            ComponentDispatcher.PopModal()
        End Try
        Logger.Info($"输入弹框返回：{If(Converter.Result, "null")}")
        Return Converter.Result
    End Function

    Public Function MyMsgBoxSelect(Selections As IEnumerable(Of IMyRadio), Optional Title As String = "提示", Optional Button1 As String = "确定", Optional Button2 As String = "", Optional IsWarn As Boolean = False) As Integer?

        Dim Converter As New MyMsgBoxConverter With {.Type = MyMsgBoxType.Select, .Button1 = Button1, .Button2 = Button2, .Content = Selections, .IsWarn = IsWarn, .Title = Title}
        WaitingMyMsgBox.Enqueue(Converter)

        Try
            If FrmMain IsNot Nothing Then FrmMain.DragStop()
            If RunInUi() Then MyMsgBoxTick()
            ComponentDispatcher.PushModal()
            Dispatcher.PushFrame(Converter.WaitFrame)
        Finally
            ComponentDispatcher.PopModal()
        End Try
        Logger.Info($"选择弹框返回：{If(Converter.Result, "null")}")
        Return Converter.Result
    End Function

    Public WaitingMyMsgBox As New ConcurrentQueue(Of MyMsgBoxConverter)
    Public Sub MyMsgBoxTick()
        Try
            If FrmMain?.PanMsg Is Nothing OrElse FrmMain.WindowState = WindowState.Minimized Then Return
            Dim TargetMsgBox As MyMsgBoxConverter = Nothing
            If FrmMain.PanMsg.Children.Count > 0 Then

                FrmMain.PanMsg.Visibility = Visibility.Visible
            ElseIf WaitingMyMsgBox.TryDequeue(TargetMsgBox) Then

                FrmMain.PanMsg.Visibility = Visibility.Visible
                Select Case TargetMsgBox.Type
                    Case MyMsgBoxType.Input
                        FrmMain.PanMsg.Children.Add(New MyMsgInput(TargetMsgBox))
                    Case MyMsgBoxType.Select
                        FrmMain.PanMsg.Children.Add(New MyMsgSelect(TargetMsgBox))
                    Case MyMsgBoxType.Text
                        FrmMain.PanMsg.Children.Add(New MyMsgText(TargetMsgBox))
                    Case MyMsgBoxType.Login
                        FrmMain.PanMsg.Children.Add(New MyMsgLogin(TargetMsgBox))
                End Select
            Else

                If FrmMain.PanMsg.Visibility <> Visibility.Collapsed Then FrmMain.PanMsg.Visibility = Visibility.Collapsed
            End If
        Catch ex As Exception
            Logger.Error(ex, "处理等待中的弹窗失败")
        End Try
    End Sub

#End Region

#Region "页面声明"

    Public FrmMain As FormMain

    Public FrmLaunchLeft As PageLaunchLeft
    Public FrmLaunchRight As PageLaunchRight
    Public FrmSelectLeft As PageSelectLeft
    Public FrmSelectRight As PageSelectRight
    Public FrmSpeedLeft As PageSpeedLeft
    Public FrmSpeedRight As PageSpeedRight

    Public FrmLinkMain As PageLinkMain

    Public FrmDownloadLeft As PageDownloadLeft
    Public FrmDownloadInstall As PageDownloadInstall
    Public FrmDownloadMod As PageDownloadMod
    Public FrmDownloadPack As PageDownloadPack
    Public FrmDownloadDataPack As PageDownloadDataPack
    Public FrmDownloadShader As PageDownloadShader
    Public FrmDownloadResourcePack As PageDownloadResourcePack
    Public FrmDownloadMap As PageDownloadMap

    Public FrmSetupLeft As PageSetupLeft
    Public FrmSetupLaunch As PageSetupLaunch
    Public FrmSetupUI As PageSetupUI
    Public FrmSetupSystem As PageSetupSystem
    Public FrmSetupLink As PageSetupLink

    Public FrmOtherLeft As PageOtherLeft
    Public FrmOtherAbout As PageOtherAbout
    Public FrmOtherTest As PageOtherTest
    Public FrmOtherFeedback As PageOtherFeedback

    Public FrmLoginLegacy As PageLoginLegacy
    Public FrmLoginNide As PageLoginNide
    Public FrmLoginNideSkin As PageLoginNideSkin
    Public FrmLoginAuth As PageLoginAuth
    Public FrmLoginAuthSkin As PageLoginAuthSkin
    Public FrmLoginMs As PageLoginMs
    Public FrmLoginMsSkin As PageLoginMsSkin

    Public FrmInstanceLeft As PageInstanceLeft
    Public FrmInstanceOverall As PageInstanceOverall
    Public FrmInstanceMod As PageInstanceMod
    Public FrmInstanceModDisabled As PageInstanceModDisabled
    Public FrmInstanceSetup As PageInstanceSetup
    Public FrmInstanceExport As PageInstanceExport
    Public FrmDownloadModify As PageDownloadModify

    Public FrmDownloadResourceDetail As PageDownloadResourceDetail

#End Region
#Region "愚人节"

    Private _IsAprilEnabled As Boolean? = Nothing
    Public ReadOnly Property IsAprilEnabled As Boolean
        Get
            If _IsAprilEnabled Is Nothing Then
                _IsAprilEnabled =
                    Date.Now.Month = 4 AndAlso Date.Now.Day = 1 AndAlso
                    Settings.Get(Of Integer)("AprilYear") <> Date.Now.Year
            End If
            Return _IsAprilEnabled.Value
        End Get
    End Property
    Public IsAprilGiveup As Boolean = False
    Private AprilDifficultiedDistance As Integer = 0
    Private Sub TimerFool()
        Try
            If FrmLaunchLeft Is Nothing OrElse FrmLaunchLeft.AprilPosTrans Is Nothing OrElse FrmMain.lastMouseArg Is Nothing Then Return
            If IsAprilGiveup OrElse FrmMain.PageCurrent <> FormMain.PageType.Launch OrElse AniControlEnabled <> 0 OrElse Not FrmLaunchLeft.BtnLaunch.IsLoaded Then Return

            Dim MousePos = FrmMain.lastMouseArg.GetPosition(FrmMain)
            Static IdieCount As Integer = 0
            Static MousePosLast As New Point(0, 0)
            If MousePos = MousePosLast Then
                IdieCount += 1
            Else
                MousePosLast = MousePos
                IdieCount = 0
            End If

            Dim Direction As Vector
            Dim Distance As Double
            Dim ButtonWidth = FrmLaunchLeft.BtnLaunch.ActualWidth / 2, ButtonHeight = FrmLaunchLeft.BtnLaunch.ActualHeight / 2
            Dim Vec As Vector = FrmMain.lastMouseArg.GetPosition(FrmLaunchLeft.BtnLaunch) - New Vector(ButtonWidth, ButtonHeight)
            Dim Dir As New Vector(Vec.X, Vec.Y)
            Dir.Normalize()
            Direction = -Dir
            Distance = New Vector(Math.Max(0, Math.Abs(Vec.X) - ButtonWidth), Math.Max(0, Math.Abs(Vec.Y) - ButtonHeight)).Length
            Dim BreathScale = Math.Sin(Timer150Count / 37.5 * Math.PI)
            Dim Difficulty As Double = 1 / (1 + (AprilDifficultiedDistance / 6000) ^ 2) + 0.25
            Dim Acc = Math.Max(0, -0.65 - Math.Log((Distance / Difficulty + 0.4) / 200)) * Direction * Difficulty

            If IdieCount >= 64 * 5 Then
                Dim SafeDist As Vector = FrmMain.lastMouseArg.GetPosition(FrmMain.PanMain) - New Vector(ButtonWidth, FrmMain.PanMain.ActualHeight - ButtonHeight * 3)
                Dim Back As New Vector(FrmLaunchLeft.AprilPosTrans.X, FrmLaunchLeft.AprilPosTrans.Y)
                If SafeDist.Length > 250 AndAlso Back.Length > 0.4 Then
                    Acc -= Back * 0.0005
                    Back.Normalize()
                    Acc -= Back * 0.15
                End If
            End If

            Static Speed As New Vector(0, 0)
            Dim Relative As Point = FrmLaunchLeft.BtnLaunch.TranslatePoint(New Point(0, 0), FrmMain.PanForm)
            If Relative.X < -ButtonWidth * 2 Then
                FrmLaunchLeft.AprilPosTrans.X += FrmMain.PanForm.ActualWidth + ButtonWidth * 2
                Speed.X -= 80
                If Relative.Y < 0 Then
                    FrmLaunchLeft.AprilPosTrans.Y += ButtonHeight * 2.5
                ElseIf Relative.Y > FrmMain.PanForm.ActualHeight - ButtonHeight * 2 Then
                    FrmLaunchLeft.AprilPosTrans.Y -= ButtonHeight * 2.5
                End If
            ElseIf Relative.X > FrmMain.PanForm.ActualWidth Then
                FrmLaunchLeft.AprilPosTrans.X -= FrmMain.PanForm.ActualWidth + ButtonWidth * 2
                Speed.X += 80
                If Relative.Y < 0 Then
                    FrmLaunchLeft.AprilPosTrans.Y += ButtonHeight * 2.5
                ElseIf Relative.Y > FrmMain.PanForm.ActualHeight - ButtonHeight * 2 Then
                    FrmLaunchLeft.AprilPosTrans.Y -= ButtonHeight * 2.5
                End If
            ElseIf Relative.Y < -ButtonHeight * 2 Then
                FrmLaunchLeft.AprilPosTrans.Y += FrmMain.PanForm.ActualHeight + ButtonHeight * 2
                Speed.Y -= 25
                If Relative.X < 0 Then
                    FrmLaunchLeft.AprilPosTrans.X += ButtonWidth * 2
                ElseIf Relative.X > FrmMain.PanForm.ActualWidth - ButtonWidth * 2 Then
                    FrmLaunchLeft.AprilPosTrans.X -= ButtonWidth * 2
                End If
            ElseIf Relative.Y > FrmMain.PanForm.ActualHeight Then
                FrmLaunchLeft.AprilPosTrans.Y -= FrmMain.PanForm.ActualHeight + ButtonHeight * 2
                Speed.Y += 25
                If Relative.X < 0 Then
                    FrmLaunchLeft.AprilPosTrans.X += ButtonWidth * 2
                ElseIf Relative.X > FrmMain.PanForm.ActualWidth - ButtonWidth * 2 Then
                    FrmLaunchLeft.AprilPosTrans.X -= ButtonWidth * 2
                End If
            End If

            Speed = Speed * 0.8 + Acc
            Dim SpeedValue = Math.Min(60, Speed.Length)
            If SpeedValue < 0.01 Then Return
            Speed.Normalize()
            Speed *= SpeedValue
            AprilDifficultiedDistance += SpeedValue
            FrmLaunchLeft.AprilPosTrans.X += Speed.X
            FrmLaunchLeft.AprilPosTrans.Y += Speed.Y

            FrmLaunchLeft.AprilScaleTrans.ScaleX = (1 - (Math.Abs(Direction.X) - Math.Abs(Direction.Y)) * (SpeedValue / 160)).Clamp(0.2, 1.8)
            FrmLaunchLeft.AprilScaleTrans.ScaleY = (1 - (Math.Abs(Direction.Y) - Math.Abs(Direction.X)) * (SpeedValue / 100)).Clamp(0.2, 1.8)

            Static GiveUpDistance As Double = -1500
            GiveUpDistance += SpeedValue
            If GiveUpDistance > 2500 Then
                GiveUpDistance = 0
                Select Case RandomInteger(0, 3)
                    Case 0
                        Hint("放弃吧！只需要点一下右下角的小白旗……", HintType.Red)
                    Case 1
                        Hint("看到右下角的那面小白旗了吗？", HintType.Red)
                    Case 2
                        Hint("这里建议点一下右下角的小白旗投降呢.jpg", HintType.Red)
                    Case 3
                        Hint("右下角的小白旗永远等着你……", HintType.Red)
                End Select
            End If

        Catch ex As Exception
            Logger.Error(ex, "愚人节移动出错")
        End Try
    End Sub

#End Region

#Region "系统"

    Public Sub ShowWindowToTop(Handle As IntPtr)
        Try
            PostMessage(Handle, 400 * 16 + 2, 0, 0)
            SetForegroundWindow(Handle)
        Catch ex As Exception
            Logger.Error(ex, "设置窗口置顶失败", LogBehavior.Toast)
        End Try
    End Sub
    Public Declare Function FindWindow Lib "user32" Alias "FindWindowA" (ClassName As String, WindowName As String) As IntPtr
    Public Declare Function SetForegroundWindow Lib "user32" (hWnd As IntPtr) As Integer
    Public Declare Function ShowWindow Lib "user32" (hWnd As IntPtr, nCmdShow As Integer) As Boolean
    Public Const SW_SHOW As Integer = 5
    Public Const SW_RESTORE As Integer = 9
    Public Const SW_MAXIMIZE As Integer = 3
    Private Declare Function PostMessage Lib "user32" Alias "PostMessageA" (hWnd As IntPtr, msg As UInteger, wParam As Long, lParam As Long) As Boolean

    Public Sub SetGPUPreference(Executeable As String)
        Const REG_KEY As String = "HKEY_CURRENT_USER\Software\Microsoft\DirectX\UserGpuPreferences"
        Const REG_VALUE As String = "GpuPreference=2;"

        If RegistryUtils.Read(REG_KEY, Executeable).result?.ToString() = REG_VALUE Then
            Logger.Info($"无需调整显卡设置：{Executeable}")
            Return
        End If

        RegistryUtils.Write(REG_KEY, Executeable, REG_VALUE)
        Logger.Info($"已调整显卡设置：{Executeable}")
    End Sub

    Public Function ArgumentReplace(Text As String, Optional EscapeHandler As Func(Of String, String) = Nothing, Optional ReplaceTime As Boolean = True) As String
        If Text Is Nothing OrElse Not Text.Contains("{") Then Return Text

        Static Replacer As Func(Of String, String) =
        Function(s As String) As String
            If s Is Nothing Then Return ""
            If EscapeHandler Is Nothing Then Return s
            If s.Contains(":\") Then
                Dim IsFolder = s.EndsWithF("\") OrElse s.EndsWithF("/")
                s = PathUtils.ToShortPath(s)
                If IsFolder Then s = PathUtils.AddSlashSuffix(s)
            End If
            Return EscapeHandler(s)
        End Function

        Text = Text.Replace("{pcl_version}", Replacer(VersionBaseName))
        Text = Text.Replace("{pcl_version_code}", Replacer(VersionCode))
        Text = Text.Replace("{pcl_version_branch}", Replacer(BuildTypeDisplay))
        Text = Text.Replace("{pcl_build_type}", Replacer(BuildType.ToString))
        Text = Text.Replace("{pcl_branch}", Replacer(VersionBranchMain))
        Text = Text.Replace("{identify}", Replacer(Identify))
        Text = Text.Replace("{path}", Replacer(Paths.Base))
        Text = Text.Replace("{path_with_name}", Replacer(PathExe))
        Text = Text.Replace("{path_temp}", Replacer(PathTemp))
        Text = Text.Replace("{pcl_md5}", Function() Replacer(CryptographyUtils.ComputeFileHash(PathExe, CryptographyUtils.HashMethod.Md5)))
        Text = Text.Replace("{pcl_sha1}", Function() Replacer(CryptographyUtils.ComputeFileHash(PathExe, CryptographyUtils.HashMethod.Sha1)))

        If ReplaceTime Then
            Text = Text.Replace("{date}", Replacer(Date.Now.ToString("yyyy'/'M'/'d")))
            Text = Text.Replace("{time}", Replacer(Date.Now.ToString("HH':'mm':'ss")))
        End If

        Text = Text.Replace("{java}", Replacer(McLaunchJavaSelected?.Folder))
        Text = Text.Replace("{minecraft}", Replacer(McFolderSelected))
        If McInstanceSelected?.IsLoaded Then
            Text = Text.Replace("{version_path}", Replacer(McInstanceSelected.PathVersion)) : Text = Text.Replace("{verpath}", Replacer(McInstanceSelected.PathVersion))
            Text = Text.Replace("{version_indie}", Replacer(McInstanceSelected.PathIndie)) : Text = Text.Replace("{verindie}", Replacer(McInstanceSelected.PathIndie))
            Text = Text.Replace("{name}", Replacer(McInstanceSelected.Name))
            If {"unknown", "old", "pending"}.Contains(McInstanceSelected.Version.VanillaName.Lower) Then
                Text = Text.Replace("{version}", Replacer(McInstanceSelected.Name))
            Else
                Text = Text.Replace("{version}", Replacer(McInstanceSelected.Version.VanillaName))
            End If
            Text = Text.Replace("{forge}", Replacer(If(McInstanceSelected.Version.ModLoaders.HasFlagF(ModLoaders.Forge), McInstanceSelected.Version.ModLoaderVersions(ModLoaders.Forge), "")))
            Text = Text.Replace("{fabric}", Replacer(If(McInstanceSelected.Version.ModLoaders.HasFlagF(ModLoaders.Fabric), McInstanceSelected.Version.ModLoaderVersions(ModLoaders.Fabric), "")))
            Text = Text.Replace("{neoforge}", Replacer(If(McInstanceSelected.Version.ModLoaders.HasFlagF(ModLoaders.NeoForge), McInstanceSelected.Version.ModLoaderVersions(ModLoaders.NeoForge), "")))
            Text = Text.Replace("{liteloader}", Replacer(If(McInstanceSelected.Version.ModLoaders.HasFlagF(ModLoaders.LiteLoader), "true", "")))
        Else
            Text = Text.Replace("{version_path}", Replacer(Nothing)) : Text = Text.Replace("{verpath}", Replacer(Nothing))
            Text = Text.Replace("{version_indie}", Replacer(Nothing)) : Text = Text.Replace("{verindie}", Replacer(Nothing))
            Text = Text.Replace("{name}", Replacer(Nothing))
            Text = Text.Replace("{version}", Replacer(Nothing))
            Text = Text.Replace("{forge}", Replacer(Nothing))
            Text = Text.Replace("{fabric}", Replacer(Nothing))
            Text = Text.Replace("{neoforge}", Replacer(Nothing))
            Text = Text.Replace("{liteloader}", Replacer(Nothing))
        End If

        If McLoginLoader.State = LoadState.Finished Then
            Text = Text.Replace("{user}", Replacer(McLoginLoader.Output.Name))
            Text = Text.Replace("{uuid}", Replacer(McLoginLoader.Output.Uuid?.Lower))
            Select Case McLoginLoader.Input.Type
                Case McLoginType.Legacy
                    Text = Text.Replace("{login}", Replacer("离线"))
                Case McLoginType.Ms
                    Text = Text.Replace("{login}", Replacer("正版"))
                Case McLoginType.Nide
                    Text = Text.Replace("{login}", Replacer("统一通行证"))
                Case McLoginType.Auth
                    Text = Text.Replace("{login}", Replacer("Authlib-Injector"))
            End Select
        Else
            Text = Text.Replace("{user}", Replacer(Nothing))
            Text = Text.Replace("{uuid}", Replacer(Nothing))
            Text = Text.Replace("{login}", Replacer(Nothing))
        End If

        Text = Text.RegexReplace("\{hint\}", Function() Replacer(PageOtherTest.GetRandomHint()))
        Text = Text.RegexReplace("\{cave\}", Function() Replacer(PageOtherTest.GetRandomCave()))
        Text = Text.RegexReplace("\{setup:([a-zA-Z0-9]+)\}", Function(m) Replacer(Settings.GetSafe(m.Groups(1).Value, McInstanceSelected)))
        Text = Text.RegexReplace("\{varible:([^:\}]+)(?::([^\}]+))?\}", Function(m) Replacer(RegistryUtils.TryRead($"HKEY_CURRENT_USER\Software\{RegFolder}", $"CustomEvent{m.Groups(1).Value}", m.Groups(2).Value)))
        Text = Text.RegexReplace("\{variable:([^:\}]+)(?::([^\}]+))?\}", Function(m) Replacer(RegistryUtils.TryRead($"HKEY_CURRENT_USER\Software\{RegFolder}", $"CustomEvent{m.Groups(1).Value}", m.Groups(2).Value)))
        Return Text
    End Function

#End Region

#Region "任务缓存"

    Private IsTaskTempCleared As Boolean = False
    Private IsTaskTempClearing As Boolean = False

    Public Sub TryClearTaskTemp()
        If Not IsTaskTempCleared Then
            IsTaskTempCleared = True
            IsTaskTempClearing = True
            Try
                Logger.Info("开始清理任务缓存文件夹")
                DirectoryUtils.Delete($"{OsDrive}ProgramData\PCL\TaskTemp\")
                DirectoryUtils.Delete($"{PathTemp}TaskTemp\")
                Logger.Info("已清理任务缓存文件夹")
            Catch ex As Exception
                Logger.Warn(ex, "清理任务缓存文件夹失败")
            Finally
                IsTaskTempClearing = False
            End Try
        ElseIf IsTaskTempClearing Then

            Do While IsTaskTempClearing
                Thread.Sleep(1)
            Loop
        End If
    End Sub

    Public Function RequestTaskTempFolder(Optional RequireNonSpace As Boolean = False) As String
        TryClearTaskTemp()
        Dim ResultFolder As String
        Try
            ResultFolder = $"{PathTemp}TaskTemp\{GetUuid()}-{RandomInteger(0, 1000000)}\"
            If RequireNonSpace AndAlso ResultFolder.Contains(" ") Then Exit Try
            DirectoryUtils.Create(ResultFolder)
            CheckPermissionWithException(ResultFolder)
            Return ResultFolder
        Catch
        End Try

        ResultFolder = $"{OsDrive}ProgramData\PCL\TaskTemp\{GetUuid()}-{RandomInteger(0, 1000000)}\"
        DirectoryUtils.Create(ResultFolder)
        CheckPermission(ResultFolder)
        Return ResultFolder
    End Function

#End Region

#Region "反馈与遥测"

    Public Sub Feedback(Optional ShowMsgbox As Boolean = True, Optional ForceOpenLog As Boolean = False)
        On Error Resume Next
        FeedbackInfo()
        If ForceOpenLog OrElse (ShowMsgbox AndAlso MyMsgBox("若你在汇报一个 Bug，请点击 打开文件夹 按钮，并上传 Log(1~5).txt 中包含错误信息的文件。" & vbCrLf & "游戏崩溃一般与启动器无关，请不要因为游戏崩溃而提交反馈。", "反馈提交提醒", "打开文件夹", "不需要") = 1) Then
            OpenExplorer(Paths.Base & "PCL\Log1.txt")
        End If
        OpenWebsite("https://github.com/Meloong-Git/PCL/issues/")
    End Sub

    Public Sub FeedbackInfo()
        On Error Resume Next
        Logger.Warn($"诊断信息：{vbCrLf}" &
            "操作系统：" & My.Computer.Info.OSFullName & vbCrLf &
            "剩余内存：" & Int(My.Computer.Info.AvailablePhysicalMemory / 1024 / 1024) & " M / " & Int(My.Computer.Info.TotalPhysicalMemory / 1024 / 1024) & " M" & vbCrLf &
            "DPI：" & DPI & "（" & Math.Round(DPI / 96, 2) * 100 & "%）" & vbCrLf &
            "MC 文件夹：" & If(McFolderSelected, "Nothing") & vbCrLf &
            "文件位置：" & Paths.Base)
    End Sub

    Public Sub Telemetry([Event] As String, ParamArray Datas As String())
        If BuildType = BuildTypes.Debug Then Return
        If Not Settings.Get(Of Boolean)("SystemSystemTelemetry") Then Return
        If Not ClsBaseUrl.StartsWithF("http") Then Return
        RunInNewThread(
        Sub()
            Try
                Logger.Info($"匿名数据上报：{[Event]}")
                Dim Url As String = $"{ClsBaseUrl}&Event={StringUtils.UrlEscape([Event])}"
                For i = 0 To Datas.Length - 1 Step 2
                    Url &= "&" & StringUtils.UrlEscape(Datas(i)) & "=" & StringUtils.UrlEscape(Datas(i + 1).ReplaceLineEndings(vbLf, mergeMultiple:=True))
                Next
                NetRequestByClient(Url, MakeLog:=False)
            Catch ex As Exception
                Logger.Warn(ex, "匿名数据上报失败")
            End Try
        End Sub, "Telemetry", ThreadPriority.Lowest)
    End Sub

#End Region

    Public DragControl = Nothing
    Private Timer4Count As Integer = 0
    Private Timer150Count As Integer = 0
    Private Sub TimerMain()
        Try
#Region "每 50ms 执行一次的代码"
            HintTick()
            MyMsgBoxTick()
            FrmMain.DragTick()
            LoaderTaskbarProgressRefresh()
            If ThemeDontClick = 2 Then ThemeRefresh()
#End Region
        Catch ex As Exception
            Logger.Error(ex, "短程主时钟执行异常", LogBehavior.AlertThenCrash)
        End Try
        Timer4Count += 1
        If Timer4Count = 4 Then
            Timer4Count = 0
            Try
#Region "每 250ms 执行一次的代码"
                If ThemeNow = 12 Then ThemeRefresh()
#End Region
            Catch ex As Exception
                Logger.Warn(ex, "中程主时钟执行异常")
            End Try
        End If
        Timer150Count += 1
        If Timer150Count = 150 Then
            Timer150Count = 0
            Try
#Region "每 7.5s 执行一次的代码"
                If FrmMain.BtnExtraApril_ShowCheck AndAlso AprilDifficultiedDistance <> 0 Then FrmMain.BtnExtraApril.Ribble()

                RunInUi(
                Sub()
                    If Not FrmMain.Hidden Then
                        If FrmMain.Top < -9000 Then FrmMain.Top = 100
                        If FrmMain.Left < -9000 Then FrmMain.Left = 100
                    End If
                End Sub)
#End Region
            Catch ex As Exception
                Logger.Error(ex, "长程主时钟执行异常", LogBehavior.AlertThenCrash)
            End Try
        End If

    End Sub
    Public Sub TimerMainStart()
        RunInNewThread(
        Sub()
            Try
                Do While True
                    RunInUiWait(AddressOf TimerMain)
                    Thread.Sleep(50 * 0.98)
                Loop
            Catch ex As Exception
                Logger.Error(ex, "程序主时钟出错")
            End Try
        End Sub, "Timer Main")
        If Not IsAprilEnabled Then Return
        RunInNewThread(
        Sub()
            Try
                Dim LastTime = My.Computer.Clock.TickCount
                Do While True
                    If LastTime <> My.Computer.Clock.TickCount Then
                        LastTime = My.Computer.Clock.TickCount
                        RunInUiWait(AddressOf TimerFool)
                    End If
                    Thread.Sleep(1)
                Loop
            Catch ex As Exception
                Logger.Error(ex, "愚人节主时钟出错")
            End Try
        End Sub, "Timer Main Fool")
    End Sub

End Module
