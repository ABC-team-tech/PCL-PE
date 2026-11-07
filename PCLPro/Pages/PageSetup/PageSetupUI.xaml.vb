Public Class PageSetupUI

    Private Sub PageSetupUI_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        PanBack.ScrollToHome()
        FontComboInit()
        ThemeCheckAll(True)
        If ThemeDontClick <> 0 Then
            Dim NewText As String = Nothing
            Select Case ThemeDontClick
                Case 1
                    NewText = "眼瞎白"
                Case 2
                    NewText = "真·滑稽彩"
            End Select
            For Each Control In PanLauncherTheme.Children
                If (TypeOf Control Is MyRadioBox) AndAlso CType(Control, MyRadioBox).IsEnabled Then CType(Control, MyRadioBox).Text = NewText
            Next
        End If

        AniControlEnabled += 1
        Refresh()
        AniControlEnabled -= 1

        Static Reloaded As Boolean = False
        If Reloaded Then Return
        Reloaded = True

        SliderLoad()

        PanLauncherHide.Visibility = Visibility.Collapsed

    End Sub
    Public Sub Refresh()
        Try
            SettingService.RefreshSettings(Me)
            FontComboSync()
            FontColorSync()
            FontSizeSync()
            BackgroundRefresh(False, False)

            If Settings.Get(Of Integer)("UiLogoType") = 2 Then Settings.Set("UiLogoType", 1)
            CheckLogoLeft.Visibility = If(RadioLogoType0.Checked, Visibility.Visible, Visibility.Collapsed)
            PanLogoChange.Visibility = If(RadioLogoType3.Checked, Visibility.Visible, Visibility.Collapsed)

            MusicRefreshUI()

            RefreshVideoBackgroundLabel()

            RefreshGlassSettings()

            OnMainPageTypeChanged()
        Catch ex As NullReferenceException
            Logger.Error(ex, "个性化设置项存在异常，已被自动重置", LogBehavior.Alert)
            Reset()
        Catch ex As Exception
            Logger.Error(ex, "重载个性化设置时出错")
        End Try
    End Sub
    Public Sub Reset()
        Try
            SettingService.ResetSettings(Me)
            Logger.Info("已初始化个性化设置！")
            Hint("已初始化个性化设置", HintType.Green, False)
        Catch ex As Exception
            Logger.Error(ex, "初始化个性化设置失败", LogBehavior.Alert)
        End Try
        Refresh()
    End Sub

    Private _FontComboLoading As Boolean = False

    Private ReadOnly Property FontFolder As String
        Get
            Return Paths.Base & "PCL\CustomFonts"
        End Get
    End Property

    Private Sub FontComboInit()
        If ComboFontFamily Is Nothing OrElse ComboFontFamily.Items.Count > 0 Then Return
        _FontComboLoading = True
        Try
            ComboFontFamily.Items.Add(New MyComboBoxItem With {.Content = "默认字体", .Tag = ""})

            Dim Names As New Dictionary(Of String, String)
            For Each Family As System.Windows.Media.FontFamily In System.Windows.Media.Fonts.SystemFontFamilies
                Dim Name As String = FontFamilyDisplayName(Family)
                If String.IsNullOrWhiteSpace(Name) OrElse String.IsNullOrWhiteSpace(Family.Source) Then Continue For
                If Not Names.ContainsKey(Name) Then Names.Add(Name, Family.Source)
            Next
            For Each Item As KeyValuePair(Of String, String) In Names.OrderBy(Function(i) i.Key, StringComparer.CurrentCulture)
                ComboFontFamily.Items.Add(New MyComboBoxItem With {.Content = Item.Key, .Tag = Item.Value})
            Next

            Dim Files As New List(Of String)
            If Directory.Exists(FontFolder) Then
                For Each Ext As String In {"*.ttf", "*.otf", "*.ttc"}
                    Files.AddRange(Directory.GetFiles(FontFolder, Ext))
                Next
            End If
            For Each FilePath As String In Files.OrderBy(Function(f) f, StringComparer.CurrentCulture)
                Dim Name As String = FontFileName(FilePath)
                Dim Source As String = FontFileSource(FilePath)
                If Name <> "" AndAlso Source <> "" Then
                    ComboFontFamily.Items.Add(New MyComboBoxItem With {.Content = Name & "（自定义导入）", .Tag = Source})
                End If
            Next
        Catch ex As Exception
            Logger.Warn(ex, "加载字体列表失败")
        Finally
            _FontComboLoading = False
        End Try
        FontComboSync()
    End Sub

    Private Function FontFamilyDisplayName(Family As System.Windows.Media.FontFamily) As String
        Try
            For Each Lang As System.Windows.Markup.XmlLanguage In Family.FamilyNames.Keys
                If Lang.IetfLanguageTag.ToLowerInvariant().StartsWith("zh") Then Return Family.FamilyNames(Lang)
            Next
            For Each Lang As System.Windows.Markup.XmlLanguage In Family.FamilyNames.Keys
                If Lang.IetfLanguageTag.ToLowerInvariant().StartsWith("en") Then Return Family.FamilyNames(Lang)
            Next
        Catch ex As Exception
        End Try
        Return Family.Source
    End Function

    Private Function FontFileName(FilePath As String) As String
        Try
            Using Col As New System.Drawing.Text.PrivateFontCollection()
                Col.AddFontFile(FilePath)
                If Col.Families.Length = 0 Then Return ""
                Return Col.Families(0).Name
            End Using
        Catch ex As Exception
            Return ""
        End Try
    End Function

    Private Function FontFileSource(FilePath As String) As String
        Dim Name As String = FontFileName(FilePath)
        If Name = "" Then Return ""
        Try
            Return New Uri(FilePath).AbsoluteUri & "#" & Name
        Catch ex As Exception
            Return ""
        End Try
    End Function

    Private Sub BtnFontImport_Click(sender As Object, e As EventArgs) Handles BtnFontImport.Click
        Dim FileName As String = Dialogs.SelectFile("选择字体文件", False, filter:={({"ttf", "otf", "ttc"}, "字体文件")}).FirstOrDefault()
        If String.IsNullOrEmpty(FileName) Then Return
        Dim Name As String = FontFileName(FileName)
        If Name = "" Then
            Hint("这个文件读不出字体信息，可能不是有效的字体文件。", HintType.Red)
            Return
        End If
        Try
            Directory.CreateDirectory(FontFolder)
            Dim Target As String = Path.Combine(FontFolder, Path.GetFileName(FileName))
            Dim Index As Integer = 1
            While File.Exists(Target)
                Target = Path.Combine(FontFolder, Path.GetFileNameWithoutExtension(FileName) & $" ({Index})" & Path.GetExtension(FileName))
                Index += 1
            End While
            File.Copy(FileName, Target)
            Dim Source As String = FontFileSource(Target)
            If Source = "" Then
                Hint("这个字体文件复制后无法加载，请换一个文件试试。", HintType.Red)
                Return
            End If
            ComboFontFamily.Items.Add(New MyComboBoxItem With {.Content = Name & "（自定义导入）", .Tag = Source})
            _FontComboLoading = True
            ComboFontFamily.SelectedItem = ComboFontFamily.Items(ComboFontFamily.Items.Count - 1)
            _FontComboLoading = False
            Settings.Set("UiFontFamily", Source)
            Hint($"已导入并应用字体「{Name}」。", HintType.Green)
        Catch ex As Exception
            Logger.Warn(ex, "导入字体失败")
            Hint("导入字体失败了，详细信息已写入日志。", HintType.Red)
        End Try
    End Sub

    Private Sub FontComboSync()
        If ComboFontFamily Is Nothing OrElse ComboFontFamily.Items.Count = 0 Then Return
        _FontComboLoading = True
        Try
            Dim Current As String = If(Settings.Get(Of String)("UiFontFamily"), "").Trim
            Dim Target As MyComboBoxItem = ComboFontFamily.Items.OfType(Of MyComboBoxItem)().FirstOrDefault(Function(i) CStr(i.Tag) = Current)
            ComboFontFamily.SelectedItem = If(Target, ComboFontFamily.Items(0))
        Finally
            _FontComboLoading = False
        End Try
    End Sub
    Private Sub ComboFontFamily_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboFontFamily.SelectionChanged
        If _FontComboLoading Then Return
        Dim Item As MyComboBoxItem = TryCast(ComboFontFamily.SelectedItem, MyComboBoxItem)
        If Item Is Nothing Then Return
        Settings.Set("UiFontFamily", CStr(Item.Tag))
    End Sub

    Private _FontSizeSyncing As Boolean = False
    Private Sub FontColorSync()
        Dim Value As String = If(Settings.Get(Of String)("UiFontColor"), "").Trim
        Dim TextColor As MyColor = ModSecret.Color1
        If Value <> "" Then
            Try
                TextColor = New MyColor(Value)
            Catch
                TextColor = ModSecret.Color1
            End Try
        End If
        RectFontColor.Fill = New SolidColorBrush(CType(TextColor, Color))
    End Sub
    Private Sub BtnFontColor_Click(sender As Object, e As EventArgs) Handles BtnFontColor.Click
        Try
            Dim Value As String = If(Settings.Get(Of String)("UiFontColor"), "").Trim
            Dim Cur As MyColor = ModSecret.Color1
            If Value <> "" Then
                Try
                    Cur = New MyColor(Value)
                Catch
                End Try
            End If
            Using Dlg As New System.Windows.Forms.ColorDialog()
                Dlg.FullOpen = True
                Dlg.Color = System.Drawing.Color.FromArgb(CInt(Cur.R), CInt(Cur.G), CInt(Cur.B))
                If Dlg.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
                    Settings.Set("UiFontColor", $"#{Dlg.Color.R:X2}{Dlg.Color.G:X2}{Dlg.Color.B:X2}")
                    FontColorSync()
                End If
            End Using
        Catch ex As Exception
            Logger.Warn(ex, "选择字体颜色失败")
        End Try
    End Sub
    Private Sub BtnFontColorReset_Click(sender As Object, e As EventArgs) Handles BtnFontColorReset.Click
        Settings.Set("UiFontColor", "")
        FontColorSync()
    End Sub
    Private Sub SliderFontSize_Change(sender As Object, user As Boolean) Handles SliderFontSize.Change
        If _FontSizeSyncing Then Return
        Dim Size As Integer = FormMain.FontSizeMin + SliderFontSize.Value
        Settings.Set("UiFontSize", Size)
        LabFontSize.Text = CStr(Size)
    End Sub
    Private Sub FontSizeSync()
        _FontSizeSyncing = True
        Try
            Dim Size As Integer = FormMain.GetFontSizeValue()
            SliderFontSize.Value = Size - FormMain.FontSizeMin
            LabFontSize.Text = CStr(Size)
        Finally
            _FontSizeSyncing = False
        End Try
    End Sub

    Private Sub BtnUIBgOpen_Click(sender As Object, e As EventArgs) Handles BtnBackgroundOpen.Click
        OpenExplorer(Paths.Base & "PCL\Pictures\")
    End Sub
    Private Sub BtnBackgroundPick_Click(sender As Object, e As EventArgs) Handles BtnBackgroundPick.Click
        Dim FileName As String = Dialogs.SelectFile("选择背景图片", False, filter:={({"png", "jpg", "jpeg", "bmp", "gif", "webp"}, "图片文件")}).FirstOrDefault()
        If String.IsNullOrWhiteSpace(FileName) Then Return
        Try
            DirectoryUtils.Create(Paths.Base & "PCL\Pictures\")
            Dim BaseName As String = IO.Path.GetFileNameWithoutExtension(FileName)
            Dim Extension As String = IO.Path.GetExtension(FileName)
            Dim TargetPath As String = Paths.Base & "PCL\Pictures\" & BaseName & Extension
            If IO.File.Exists(TargetPath) Then
                TargetPath = Paths.Base & "PCL\Pictures\" & BaseName & "_" & Now.ToString("yyyyMMddHHmmss") & Extension
            End If
            FileUtils.Copy(FileName, TargetPath)

            BackgroundRefresh(True, True, TargetPath)
            Hint("背景图片已更新！", HintType.Green)
        Catch ex As Exception
            Logger.Error(ex, "导入背景图片失败", LogBehavior.Alert)
        End Try
    End Sub
    Private Sub BtnBackgroundRefresh_Click(sender As Object, e As EventArgs) Handles BtnBackgroundRefresh.Click

        BackgroundRefresh(True, True, CurrentBackgroundPath(), AllowRandom:=False)
    End Sub
    Public Sub BackgroundRefreshUI(Show As Boolean, Count As Integer)
        If IsNothing(PanBackgroundOpacity) Then Return
        If Show Then
            PanBackgroundOpacity.Visibility = Visibility.Visible
            PanBackgroundBlur.Visibility = If(String.IsNullOrEmpty(Settings.Get(Of String)("UiVideoBackground")), Visibility.Visible, Visibility.Collapsed)
            PanBackgroundSuit.Visibility = Visibility.Visible
            BtnBackgroundClear.Visibility = Visibility.Visible
            CardBackground.Title = "背景图片（" & Count & " 张）"
        Else
            PanBackgroundOpacity.Visibility = Visibility.Collapsed
            PanBackgroundBlur.Visibility = Visibility.Collapsed
            PanBackgroundSuit.Visibility = Visibility.Collapsed
            BtnBackgroundClear.Visibility = Visibility.Collapsed
            CardBackground.Title = "背景图片"
        End If
        CardBackground.TriggerForceResize()
    End Sub

    Private Sub BtnBackgroundClear_Click(sender As Object, e As EventArgs) Handles BtnBackgroundClear.Click
        If MyMsgBox("即将删除背景图片文件夹中的所有文件。" & vbCrLf & "此操作不可撤销，是否确定？", "警告",, "取消", IsWarn:=True) = 1 Then
            DirectoryUtils.Delete(Paths.Base & "PCL\Pictures")
            BackgroundRefresh(False, True)
            Hint("背景图片已清空！", HintType.Green)
        End If
    End Sub

    Private Sub BtnVideoBackgroundPick_Click(sender As Object, e As EventArgs) Handles BtnVideoBackgroundPick.Click
        Dim FileName As String = Dialogs.SelectFile("选择视频背景", False, filter:={({"mp4", "m4v", "mov", "wmv", "avi", "mkv", "webm", "flv", "ts", "3gp", "mpg", "mpeg"}, "视频文件")}).FirstOrDefault()
        If String.IsNullOrWhiteSpace(FileName) Then Return
        If FormMain.VideoBackgroundApply(FileName) Then
            Hint("视频背景已启用！", HintType.Green)
            RefreshVideoBackgroundLabel()
        End If
    End Sub
    Private Sub BtnVideoBackgroundReset_Click(sender As Object, e As EventArgs) Handles BtnVideoBackgroundReset.Click
        FormMain.VideoBackgroundReset()
        Hint("已恢复静态壁纸！", HintType.Green)
        RefreshVideoBackgroundLabel()
    End Sub

    Friend Sub RefreshGlassSettings()
        If CardGlass Is Nothing Then Return
        CardGlass.Visibility = If(Settings.Get(Of Integer)("UiUiStyle") = 1, Visibility.Visible, Visibility.Collapsed)
    End Sub
    Friend Sub RefreshVideoBackgroundLabel()
        Dim Path As String = Settings.Get(Of String)("UiVideoBackground")
        LabVideoBackground.Text = If(String.IsNullOrEmpty(Path) OrElse Not IO.File.Exists(Path), "当前：静态壁纸", "当前：" & IO.Path.GetFileName(Path))
        If IsNothing(PanBackgroundBlur) Then Return
        Dim HasVideo As Boolean = Not String.IsNullOrEmpty(Path)
        Dim HasPicture As Boolean = FrmMain IsNot Nothing AndAlso FrmMain.ImgBack IsNot Nothing AndAlso FrmMain.ImgBack.Background IsNot Nothing
        PanBackgroundBlur.Visibility = If(HasPicture AndAlso Not HasVideo, Visibility.Visible, Visibility.Collapsed)
    End Sub

    Private Shared _CurrentBackgroundPath As String = ""

    Private Shared Function CurrentBackgroundPath() As String
        If Not String.IsNullOrEmpty(_CurrentBackgroundPath) Then Return _CurrentBackgroundPath
        Try
            If FrmMain IsNot Nothing AndAlso FrmMain.ImgBack IsNot Nothing Then Return TryCast(FrmMain.ImgBack.Tag, String)
        Catch
        End Try
        Return Nothing
    End Function

    Private Shared Function MatchBackgroundPath(Pic As List(Of String), Want As String) As String
        If String.IsNullOrEmpty(Want) Then Return Nothing
        For Each Item As String In Pic
            If String.Equals(Item, Want, StringComparison.OrdinalIgnoreCase) Then Return Item
        Next
        Dim Name As String = IO.Path.GetFileName(Want)
        For Each Item As String In Pic
            If String.Equals(IO.Path.GetFileName(Item), Name, StringComparison.OrdinalIgnoreCase) Then Return Item
        Next
        Return Nothing
    End Function

    Public Shared Sub BackgroundRefresh(IsHint As Boolean, Refresh As Boolean, Optional PreferredImage As String = Nothing, Optional AllowRandom As Boolean = True)
        Try

            DirectoryUtils.Create(Paths.Base & "PCL\Pictures\")
            Dim Pic As New List(Of String)
            For Each File In DirectoryUtils.EnumerateFiles(Paths.Base & "PCL\Pictures\", True)
                Dim Extension As String = PathUtils.GetExtension(File)
                If Extension = "ini" OrElse Extension = "db" Then Continue For

                Dim FileName As String = IO.Path.GetFileName(File)
                If FileName.StartsWithF(".trashed-") Then Continue For
                If FileName.StartsWithF(".") Then Continue For
                Pic.Add(File)
            Next

            If Not Pic.Any() Then
                If Refresh Then
                    If FrmMain.ImgBack.Visibility = Visibility.Collapsed Then
                        If IsHint Then Hint("未检测到可用背景图片！", HintType.Red)
                    Else
                        FrmMain.ImgBack.Visibility = Visibility.Collapsed

                        FrmMain.ImgBack.Background = Nothing
                        FrmMain.ImgBack.Effect = Nothing
                        FrmMain.ImgBack.Margin = New Thickness(0)
                        FrmMain.ClearBackgroundCaches()
                        _CurrentBackgroundPath = ""
                        Settings.Set("UiBackgroundCurrent", "")
                        FrmMain.ImgBack.Tag = Nothing
                        If IsHint Then Hint("背景图片已清除！", HintType.Green)
                    End If
                End If
                If Not IsNothing(FrmSetupUI) Then FrmSetupUI.BackgroundRefreshUI(False, 0)
            Else
                If Refresh Then

                    Dim Address As String = MatchBackgroundPath(Pic, PreferredImage)
                    If Address Is Nothing Then Address = MatchBackgroundPath(Pic, _CurrentBackgroundPath)
                    If Address Is Nothing Then Address = MatchBackgroundPath(Pic, CurrentBackgroundPath())

                    If Address Is Nothing Then Address = MatchBackgroundPath(Pic, Settings.Get(Of String)("UiBackgroundCurrent"))
                    If Address Is Nothing Then Address = If(AllowRandom, RandomOne(Pic), Pic(0))
                    Try
                        Logger.Info($"加载背景图片：{Address}")

                        If Not String.IsNullOrEmpty(Settings.Get(Of String)("UiVideoBackground")) Then
                            Logger.Info("已设置静态背景图片，自动关闭视频背景")
                            FormMain.VideoBackgroundReset()
                            If FrmSetupUI IsNot Nothing Then FrmSetupUI.RefreshVideoBackgroundLabel()
                        End If
                        FrmMain.ImgBack.Background = New MyBitmap(Address)
                        FrmMain.ImgBack.Visibility = Visibility.Visible

                        _CurrentBackgroundPath = Address
                        Settings.Set("UiBackgroundCurrent", Address)
                        FrmMain.ImgBack.Tag = Address
                        FrmMain.UpdateBackgroundAndTitleBar()
                        If IsHint Then Hint("背景图片已刷新：" & PathUtils.GetLastPart(Address), HintType.Green, False)
                    Catch ex As Exception
                        If ex.Message.Contains("参数无效") Then
                            Logger.Error($"刷新背景图片失败，该图片文件可能并非标准格式。{vbCrLf}你可以尝试使用画图打开该文件并重新保存，这会让图片变为标准格式。{vbCrLf}文件：{Address}", LogBehavior.Alert)
                        Else
                            Logger.Error(ex, $"刷新背景图片失败（{Address}）", LogBehavior.Alert)
                        End If
                    End Try
                End If
                If Not IsNothing(FrmSetupUI) Then FrmSetupUI.BackgroundRefreshUI(True, Pic.Count)
            End If

        Catch ex As Exception
            Logger.Error(ex, "刷新背景图片时出现未知错误")
        End Try
    End Sub

    Private Sub BtnLogoChange_Click(sender As Object, e As EventArgs) Handles BtnLogoChange.Click
        Dim FileName As String = Dialogs.SelectFile("选择图片", False, filter:={({"png", "jpg", "jpeg", "gif", "webp"}, "常用图片文件")}).FirstOrDefault()
        If String.IsNullOrEmpty(FileName) Then Return
        Dim TargetPath As String = Paths.Base & "PCL\Logo.png"
        Try

            FileUtils.Copy(FileName, TargetPath)

            FrmMain.ImageTitleLogo.Source = Nothing
            FrmMain.ImageTitleLogo.Source = TargetPath
        Catch ex As Exception
            If ex.Message.Contains("参数无效") Then
                Logger.Error($"改变标题栏图片失败，该图片文件可能并非标准格式。{vbCrLf}你可以尝试使用画图打开该文件并重新保存，这会让图片变为标准格式。{vbCrLf}文件：{TargetPath}", LogBehavior.Alert)
            Else
                Logger.Error(ex, "设置标题栏图片失败", LogBehavior.Alert)
            End If
            FrmMain.ImageTitleLogo.Source = Nothing
        End Try
    End Sub
    Private Sub RadioLogoType3_Check(sender As Object, e As RouteEventArgs) Handles RadioLogoType3.PreviewCheck
        If Not (AniControlEnabled = 0 AndAlso e.RaiseByMouse) Then Return
Refresh:

        Dim TargetPath As String = Paths.Base & "PCL\Logo.png"
        If FileUtils.Exists(TargetPath) Then
            Try
                FrmMain.ImageTitleLogo.Source = Nothing
                FrmMain.ImageTitleLogo.Source = TargetPath
            Catch ex As Exception
                If ex.Message.Contains("参数无效") Then
                    Logger.Error($"改变标题栏图片失败，该图片文件可能并非标准格式。{vbCrLf}你可以尝试使用画图打开该文件并重新保存，这会让图片变为标准格式。{vbCrLf}文件：{TargetPath}", LogBehavior.Alert)
                Else
                    Logger.Error(ex, "调整标题栏图片失败", LogBehavior.Alert)
                End If
                FrmMain.ImageTitleLogo.Source = Nothing
                e.Handled = True
                Try
                    FileUtils.Delete(TargetPath)
                Catch exx As Exception
                    Logger.Error(exx, "清理错误的标题栏图片失败", LogBehavior.Alert)
                End Try
            End Try
            Return
        End If

        Dim FileName As String = Dialogs.SelectFile("选择图片", False, filter:={({"png", "jpeg", "jpg", "gif", "webp"}, "常用图片文件")}).FirstOrDefault()
        If String.IsNullOrEmpty(FileName) Then
            FrmMain.ImageTitleLogo.Source = Nothing
            e.Handled = True
        Else
            Try
                FileUtils.Copy(FileName, TargetPath)
                GoTo Refresh
            Catch ex As Exception
                Logger.Error(ex, "复制标题栏图片失败", LogBehavior.Alert)
            End Try
        End If
    End Sub
    Private Sub BtnLogoDelete_Click(sender As Object, e As EventArgs) Handles BtnLogoDelete.Click
        Try
            FileUtils.Delete(Paths.Base & "PCL\Logo.png")
            RadioLogoType1.SetChecked(True, True)
            Hint("标题栏图片已清空！", HintType.Green)
        Catch ex As Exception
            Logger.Error(ex, "清空标题栏图片失败", LogBehavior.Alert)
        End Try
    End Sub

    Private Sub BtnMusicOpen_Click(sender As Object, e As EventArgs) Handles BtnMusicOpen.Click
        OpenExplorer(Paths.Base & "PCL\Musics\")
    End Sub
    Private Sub BtnMusicRefresh_Click(sender As Object, e As EventArgs) Handles BtnMusicRefresh.Click
        MusicRefreshPlay(True)
    End Sub
    Public Sub MusicRefreshUI()
        If PanBackgroundOpacity Is Nothing Then Return
        If MusicAllList.Any Then
            PanMusicVolume.Visibility = Visibility.Visible
            PanMusicDetail.Visibility = Visibility.Visible
            BtnMusicClear.Visibility = Visibility.Visible
            CardMusic.Title = "背景音乐（" &
                DirectoryUtils.EnumerateFiles(Paths.Base & "PCL\Musics\", True).Count(Function(f) Not {"ini", "jpg", "txt", "cfg", "lrc", "db", "png"}.Contains(PathUtils.GetExtension(f))) &
                " 首）"
        Else
            PanMusicVolume.Visibility = Visibility.Collapsed
            PanMusicDetail.Visibility = Visibility.Collapsed
            BtnMusicClear.Visibility = Visibility.Collapsed
            CardMusic.Title = "背景音乐"
        End If
        CardMusic.TriggerForceResize()
    End Sub
    Private Sub BtnMusicClear_Click(sender As Object, e As EventArgs) Handles BtnMusicClear.Click
        If MyMsgBox("即将删除背景音乐文件夹中的所有文件。" & vbCrLf & "此操作不可撤销，是否确定？", "警告",, "取消", IsWarn:=True) = 1 Then
            RunInThread(
            Sub()
                Hint("正在删除背景音乐……")

                MusicNAudio = Nothing
                MusicWaitingList = New List(Of String)
                MusicAllList = New List(Of String)
                Thread.Sleep(200)

                Try
                    DirectoryUtils.Delete(Paths.Base & "PCL\Musics")
                    Hint("背景音乐已删除！", HintType.Green)
                Catch ex As Exception
                    Logger.Error(ex, "删除背景音乐失败", LogBehavior.Alert)
                End Try
                Try
                    DirectoryUtils.Create(Paths.Base & "PCL\Musics\")
                    RunInUi(Sub() MusicRefreshPlay(False))
                Catch ex As Exception
                    Logger.Error(ex, "重建背景音乐文件夹失败", LogBehavior.Alert)
                End Try
            End Sub)
        End If
    End Sub
    Private Sub CheckMusicStart_Change() Handles CheckMusicStart.Change
        If AniControlEnabled <> 0 Then Return
        If CheckMusicStart.Checked Then CheckMusicStop.Checked = False
    End Sub
    Private Sub CheckMusicStop_Change() Handles CheckMusicStop.Change
        If AniControlEnabled <> 0 Then Return
        If CheckMusicStop.Checked Then CheckMusicStart.Checked = False
    End Sub

    Private Sub BtnCustomFile_Click(sender As Object, e As EventArgs) Handles BtnCustomFile.Click
        Try
            If FileUtils.Exists(Paths.Base & "PCL\Custom.xaml") Then
                If MyMsgBox("当前已存在布局文件，继续生成教学文件将会覆盖现有布局文件！", "覆盖确认", "继续", "取消", IsWarn:=True) = 2 Then Return
            End If
            ExtractResources(Paths.Base & "PCL\Custom.xaml", "Custom")
            Hint("教学文件已生成！", HintType.Green)
            OpenExplorer(Paths.Base & "PCL\Custom.xaml")
        Catch ex As Exception
            Logger.Error(ex, "生成教学文件失败")
        End Try
    End Sub
    Private Sub BtnCustomRefresh_Click() Handles BtnCustomRefresh.Click
        FrmLaunchRight.ForceRefresh()
        Hint("已刷新主页！", HintType.Green)
    End Sub
    Private Sub BtnCustomTutorial_Click(sender As Object, e As EventArgs) Handles BtnCustomTutorial.Click
        MyMsgBox("1. 点击 生成教学文件 按钮，这会在 PCL 文件夹下生成 Custom.xaml 布局文件。" & vbCrLf &
                 "2. 使用记事本等工具打开这个文件并进行修改，修改完记得保存。" & vbCrLf &
                 "3. 点击 刷新主页 按钮，查看主页现在长啥样了。" & vbCrLf &
                 vbCrLf &
                 "你可以在生成教学文件后直接刷新主页，对照着进行修改，更有助于理解。" & vbCrLf &
                 "直接将主页文件拖进 PCL 窗口也可以快捷加载。", "主页自定义教程")
    End Sub
    Public Shared Sub OnMainPageTypeChanged()
        If FrmSetupUI Is Nothing Then Return
        Select Case CInt(Settings.Get(Of Integer)("UiCustomType"))
            Case 0
                FrmSetupUI.PanCustomPreset.Visibility = Visibility.Collapsed
                FrmSetupUI.PanCustomLocal.Visibility = Visibility.Collapsed
                FrmSetupUI.PanCustomNet.Visibility = Visibility.Collapsed
                FrmSetupUI.HintCustom.Visibility = Visibility.Collapsed
                FrmSetupUI.HintCustomWarn.Visibility = Visibility.Collapsed
            Case 1
                FrmSetupUI.PanCustomPreset.Visibility = Visibility.Collapsed
                FrmSetupUI.PanCustomLocal.Visibility = Visibility.Visible
                FrmSetupUI.PanCustomNet.Visibility = Visibility.Collapsed
                FrmSetupUI.HintCustom.Visibility = Visibility.Visible
                FrmSetupUI.HintCustomWarn.Visibility = If(Settings.Get(Of Boolean)("HintCustomWarn"), Visibility.Collapsed, Visibility.Visible)
                FrmSetupUI.HintCustom.Text = $"从 PCL 文件夹下的 Custom.xaml 读取主页内容。{vbCrLf}你可以手动编辑该文件，向主页添加文本、图片、常用网站、快捷启动等功能。"
                CustomEventService.SetEventType(FrmSetupUI.HintCustom, CustomEvent.EventType.None)
            Case 2
                FrmSetupUI.PanCustomPreset.Visibility = Visibility.Collapsed
                FrmSetupUI.PanCustomLocal.Visibility = Visibility.Collapsed
                FrmSetupUI.PanCustomNet.Visibility = Visibility.Visible
                FrmSetupUI.HintCustom.Visibility = Visibility.Visible
                FrmSetupUI.HintCustomWarn.Visibility = If(Settings.Get(Of Boolean)("HintCustomWarn"), Visibility.Collapsed, Visibility.Visible)
                FrmSetupUI.HintCustom.Text = $"从指定网址联网获取主页内容。服主也可以用于动态更新服务器公告。{vbCrLf}如果你制作了稳定运行的联网主页，可以点击这条提示投稿，若合格即可加入预设！"
                CustomEventService.SetEventType(FrmSetupUI.HintCustom, CustomEvent.EventType.打开网页)
                CustomEventService.SetEventData(FrmSetupUI.HintCustom, "https://github.com/Meloong-Git/PCL/discussions/2528")
            Case 3
                FrmSetupUI.PanCustomPreset.Visibility = Visibility.Visible
                FrmSetupUI.PanCustomLocal.Visibility = Visibility.Collapsed
                FrmSetupUI.PanCustomNet.Visibility = Visibility.Collapsed
                FrmSetupUI.HintCustom.Visibility = Visibility.Collapsed
                FrmSetupUI.HintCustomWarn.Visibility = Visibility.Collapsed
        End Select
        FrmSetupUI.CardCustom.TriggerForceResize()
    End Sub
    Private Sub BtnCustomFolder_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnCustomFolder.Click
        Dim CustomPath As String = Paths.Base & "PCL\Custom.xaml"
        If FileUtils.Exists(CustomPath) Then
            OpenExplorer(CustomPath)
        Else
            OpenExplorer(Paths.Base & "PCL\")
        End If
    End Sub

    Private Sub LabLauncherTheme5Unlock_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles LabLauncherTheme5Unlock.MouseLeftButtonUp
        RadioLauncherTheme5Gray.Opacity -= 0.7
        RadioLauncherTheme5.Opacity += 0.7
        AniStart({
            AaOpacity(RadioLauncherTheme5Gray, 1, 1000 * AniSpeed, 500 * AniSpeed, New AniEaseInFluent),
            AaOpacity(RadioLauncherTheme5, -1, 1000 * AniSpeed, 500 * AniSpeed, New AniEaseInFluent)
        }, "ThemeUnlock")
        If RadioLauncherTheme5Gray.Opacity < 0.02 Then
            ThemeUnlock(5, UnlockHint:="隐藏主题 玄素黑 已解锁！")
            AniStop("ThemeUnlock")
            RadioLauncherTheme5.Checked = True
        End If
    End Sub
    Private Sub LabLauncherTheme11Click_MouseLeftButtonUp() Handles LabLauncherTheme11Click.MouseLeftButtonUp, RadioLauncherTheme11.MouseRightButtonUp
        If LabLauncherTheme11Click.Visibility = Visibility.Collapsed OrElse If(LabLauncherTheme11Click.ToolTip, "").ToString.Contains("点击") Then
            If MyMsgBox(
                "1. 不爬取或攻击相关服务或网站，不盗取相关账号，没有谜题可以或需要以此来解决。" & vbCrLf &
                "2. 不得篡改或损毁相关公开信息，请尽量让它们保持原状。" & vbCrLf &
                "3. 在你感到迷茫的时候，看看回声洞可能会给你带来惊喜。" & vbCrLf & vbCrLf &
                "若违规，可能会被从任意相关群中踢出！",
                "解密游戏的基本规则", "我知道了", "恕我拒绝") = 1 Then
                MyMsgBox("你需要用自己的智慧来找到下一步的线索……" & vbCrLf &
                         "初始线索：gnp.dorC61\60\20\0202\moc.x1xa.2s\\:sp" & "T".Lower & "th", "解密游戏")
            End If
        End If
    End Sub
    Private Sub LabLauncherTheme8Copy_MouseRightButtonUp() Handles LabLauncherTheme8Copy.MouseRightButtonUp, RadioLauncherTheme8.MouseRightButtonUp
        OpenWebsite("https://meloong.com/afd/a/LTCat")
    End Sub
    Private Sub LabLauncherTheme9Copy_MouseRightButtonUp() Handles LabLauncherTheme9Copy.MouseRightButtonUp, RadioLauncherTheme9.MouseRightButtonUp
        OpenWebsite("https://github.com/Meloong-Git/PCL/issues/")
    End Sub

    Private Sub RadioLauncherTheme14_Change(sender As Object, e As RouteEventArgs) Handles RadioLauncherTheme14.Changed
        If RadioLauncherTheme14.Checked Then
            If LabLauncherHue.Visibility = Visibility.Visible Then Return
            LabLauncherHue.Visibility = Visibility.Visible
            SliderLauncherHue.Visibility = Visibility.Visible
            LabLauncherSat.Visibility = Visibility.Visible
            SliderLauncherSat.Visibility = Visibility.Visible
            LabLauncherDelta.Visibility = Visibility.Visible
            SliderLauncherDelta.Visibility = Visibility.Visible
            LabLauncherLight.Visibility = Visibility.Visible
            SliderLauncherLight.Visibility = Visibility.Visible
        Else
            If LabLauncherHue.Visibility = Visibility.Collapsed Then Return
            LabLauncherHue.Visibility = Visibility.Collapsed
            SliderLauncherHue.Visibility = Visibility.Collapsed
            LabLauncherSat.Visibility = Visibility.Collapsed
            SliderLauncherSat.Visibility = Visibility.Collapsed
            LabLauncherDelta.Visibility = Visibility.Collapsed
            SliderLauncherDelta.Visibility = Visibility.Collapsed
            LabLauncherLight.Visibility = Visibility.Collapsed
            SliderLauncherLight.Visibility = Visibility.Collapsed
        End If
        CardLauncher.TriggerForceResize()
    End Sub
    Private Sub HSL_Change() Handles SliderLauncherHue.Change, SliderLauncherLight.Change, SliderLauncherSat.Change, SliderLauncherDelta.Change
        If AniControlEnabled <> 0 OrElse SliderLauncherSat Is Nothing OrElse Not SliderLauncherSat.IsLoaded Then Return
        ThemeRefresh()
    End Sub

#Region "功能隐藏"

    Private Shared _HiddenForceShow As Boolean = False

    Public Shared Property HiddenForceShow As Boolean
        Get
            Return _HiddenForceShow
        End Get
        Set(value As Boolean)
            _HiddenForceShow = value
            HiddenRefresh()
        End Set
    End Property

    Public Shared Sub HiddenRefresh() Handles Me.Loaded
        If FrmMain.PanTitleSelect Is Nothing OrElse Not FrmMain.PanTitleSelect.IsLoaded Then Return
        Try

            If Not HiddenForceShow AndAlso Settings.Get(Of Boolean)("UiHiddenPageDownload") AndAlso Settings.Get(Of Boolean)("UiHiddenPageLink") AndAlso Settings.Get(Of Boolean)("UiHiddenPageSetup") AndAlso Settings.Get(Of Boolean)("UiHiddenPageOther") Then

                FrmMain.PanTitleSelect.Visibility = Visibility.Collapsed
            Else

                FrmMain.PanTitleSelect.Visibility = Visibility.Visible
                FrmMain.BtnTitleSelect1.Visibility = If(Not HiddenForceShow AndAlso Settings.Get(Of Boolean)("UiHiddenPageDownload"), Visibility.Collapsed, Visibility.Visible)
                FrmMain.BtnTitleSelect2.Visibility = If(Not HiddenForceShow AndAlso Settings.Get(Of Boolean)("UiHiddenPageLink"), Visibility.Collapsed, Visibility.Visible)
                FrmMain.BtnTitleSelect3.Visibility = If(Not HiddenForceShow AndAlso Settings.Get(Of Boolean)("UiHiddenPageSetup"), Visibility.Collapsed, Visibility.Visible)
                FrmMain.BtnTitleSelect4.Visibility = If(Not HiddenForceShow AndAlso Settings.Get(Of Boolean)("UiHiddenPageOther"), Visibility.Collapsed, Visibility.Visible)
            End If

            FrmLaunchLeft.RefreshButtonsUI()
            If FrmSetupUI IsNot Nothing Then
                FrmSetupUI.CardSwitch.Visibility = If(Not HiddenForceShow AndAlso Settings.Get(Of Boolean)("UiHiddenFunctionHidden"), Visibility.Collapsed, Visibility.Visible)
            End If

            If FrmInstanceLeft IsNot Nothing Then
                FrmInstanceLeft.ItemModify.Visibility = If(Not HiddenForceShow AndAlso Settings.Get(Of Boolean)("UiHiddenFunctionInstanceModify"), Visibility.Collapsed, Visibility.Visible)
            End If

            If FrmSetupLeft IsNot Nothing Then
                FrmSetupLeft.ItemLaunch.Visibility = If(Not HiddenForceShow AndAlso Settings.Get(Of Boolean)("UiHiddenSetupLaunch"), Visibility.Collapsed, Visibility.Visible)
                FrmSetupLeft.ItemUI.Visibility = If(Not HiddenForceShow AndAlso Settings.Get(Of Boolean)("UiHiddenSetupUi"), Visibility.Collapsed, Visibility.Visible)
                FrmSetupLeft.ItemLink.Visibility = If(Not HiddenForceShow AndAlso Settings.Get(Of Boolean)("UiHiddenSetupLink"), Visibility.Collapsed, Visibility.Visible)
                FrmSetupLeft.ItemSystem.Visibility = If(Not HiddenForceShow AndAlso Settings.Get(Of Boolean)("UiHiddenSetupSystem"), Visibility.Collapsed, Visibility.Visible)

                Dim AvaliableCount As Integer = 0
                If Not Settings.Get(Of Boolean)("UiHiddenSetupLaunch") Then AvaliableCount += 1
                If Not Settings.Get(Of Boolean)("UiHiddenSetupUi") Then AvaliableCount += 1
                If Not Settings.Get(Of Boolean)("UiHiddenSetupLink") Then AvaliableCount += 1
                If Not Settings.Get(Of Boolean)("UiHiddenSetupSystem") Then AvaliableCount += 1
                FrmSetupLeft.PanItem.Visibility = If(AvaliableCount < 2 AndAlso Not HiddenForceShow, Visibility.Collapsed, Visibility.Visible)
            End If

            Dim OtherAvaliableCount As Integer = 0
            If Not Settings.Get(Of Boolean)("UiHiddenOtherAbout") Then OtherAvaliableCount += 1
            If Not Settings.Get(Of Boolean)("UiHiddenOtherTest") Then OtherAvaliableCount += 1
            If Not Settings.Get(Of Boolean)("UiHiddenOtherFeedback") Then OtherAvaliableCount += 1
            If Not Settings.Get(Of Boolean)("UiHiddenOtherVote") Then OtherAvaliableCount += 1
            If FrmOtherLeft IsNot Nothing Then
                FrmOtherLeft.ItemAbout.Visibility = If(Not HiddenForceShow AndAlso Settings.Get(Of Boolean)("UiHiddenOtherAbout"), Visibility.Collapsed, Visibility.Visible)
                FrmOtherLeft.ItemTest.Visibility = If(Not HiddenForceShow AndAlso Settings.Get(Of Boolean)("UiHiddenOtherTest"), Visibility.Collapsed, Visibility.Visible)
                FrmOtherLeft.ItemFeedback.Visibility = If(Not HiddenForceShow AndAlso Settings.Get(Of Boolean)("UiHiddenOtherFeedback"), Visibility.Collapsed, Visibility.Visible)

                FrmOtherLeft.PanItem.Visibility = If(OtherAvaliableCount < 2 AndAlso Not HiddenForceShow, Visibility.Collapsed, Visibility.Visible)
            End If
            If OtherAvaliableCount = 1 AndAlso Not HiddenForceShow Then
                If Not Settings.Get(Of Boolean)("UiHiddenOtherAbout") Then
                    FrmMain.BtnTitleSelect4.Text = "关于"
                ElseIf Not Settings.Get(Of Boolean)("UiHiddenOtherTest") Then
                    FrmMain.BtnTitleSelect4.Text = "百宝箱"
                Else
                    FrmMain.BtnTitleSelect4.Text = "反馈"
                End If
            Else
                FrmMain.BtnTitleSelect4.Text = "更多"
            End If

            If FrmMain.PageCurrent = FormMain.PageType.InstanceSelect Then FrmSelectRight.BtnEmptyDownload_Loaded()
            If FrmMain.PageCurrent = FormMain.PageType.Launch Then FrmLaunchLeft.RefreshButtonsUI()
            If FrmMain.PageCurrent = FormMain.PageType.InstanceSetup AndAlso FrmInstanceModDisabled IsNot Nothing Then FrmInstanceModDisabled.BtnDownload_Loaded()

            If FrmSetupUI IsNot Nothing Then FrmSetupUI.CardSwitch.Title = If(HiddenForceShow, "功能隐藏（已暂时关闭，按 F12 以重新启用）", "功能隐藏")
        Catch ex As Exception
            Logger.Error(ex, "刷新功能隐藏项目失败")
        End Try
    End Sub

    Private Sub HiddenSetupMain() Handles CheckHiddenPageSetup.Change

        If CheckHiddenPageSetup.Checked Then

            CheckHiddenSetupLaunch.Checked = True
            CheckHiddenSetupSystem.Checked = True
            CheckHiddenSetupLink.Checked = True
            CheckHiddenSetupUI.Checked = True
        Else

            If Settings.Get(Of Boolean)("UiHiddenSetupLaunch") AndAlso Settings.Get(Of Boolean)("UiHiddenSetupUi") AndAlso Settings.Get(Of Boolean)("UiHiddenSetupSystem") AndAlso Settings.Get(Of Boolean)("UiHiddenSetupLink") Then
                CheckHiddenSetupLaunch.Checked = False
                CheckHiddenSetupSystem.Checked = False
                CheckHiddenSetupLink.Checked = False
                CheckHiddenSetupUI.Checked = False
            End If
        End If
    End Sub
    Private Sub HiddenSetupSub() Handles CheckHiddenSetupLaunch.Change, CheckHiddenSetupSystem.Change, CheckHiddenSetupLink.Change, CheckHiddenSetupUI.Change

        If Settings.Get(Of Boolean)("UiHiddenSetupLaunch") AndAlso Settings.Get(Of Boolean)("UiHiddenSetupUi") AndAlso Settings.Get(Of Boolean)("UiHiddenSetupSystem") AndAlso Settings.Get(Of Boolean)("UiHiddenSetupLink") Then

            CheckHiddenPageSetup.Checked = True
        Else

            CheckHiddenPageSetup.Checked = False
        End If
    End Sub
    Private Sub HiddenOtherMain() Handles CheckHiddenPageOther.Change

        If CheckHiddenPageOther.Checked Then

            CheckHiddenOtherAbout.Checked = True
            CheckHiddenOtherTest.Checked = True
            CheckHiddenOtherFeedback.Checked = True
            CheckHiddenOtherVote.Checked = True
        Else

            If Settings.Get(Of Boolean)("UiHiddenOtherAbout") AndAlso Settings.Get(Of Boolean)("UiHiddenOtherTest") AndAlso
                Settings.Get(Of Boolean)("UiHiddenOtherVote") AndAlso Settings.Get(Of Boolean)("UiHiddenOtherFeedback") Then
                CheckHiddenOtherAbout.Checked = False
                CheckHiddenOtherTest.Checked = False
                CheckHiddenOtherFeedback.Checked = False
                CheckHiddenOtherVote.Checked = False
            End If
        End If
    End Sub
    Private Sub HiddenOtherSub(sender As Object, user As Boolean) Handles CheckHiddenOtherAbout.Change, CheckHiddenOtherTest.Change

        If Settings.Get(Of Boolean)("UiHiddenOtherAbout") AndAlso Settings.Get(Of Boolean)("UiHiddenOtherTest") Then

            CheckHiddenPageOther.Checked = True
        Else

            CheckHiddenPageOther.Checked = False
        End If

        If Not user Then Return
        If Settings.Get(Of Boolean)("UiHiddenOtherAbout") AndAlso Settings.Get(Of Boolean)("UiHiddenOtherTest") Then

            CheckHiddenOtherVote.Checked = True
        End If
    End Sub
    Private Sub HiddenOtherNet(sender As Object, user As Boolean) Handles CheckHiddenOtherFeedback.Change, CheckHiddenOtherVote.Change

        If Not user Then Return
        If Settings.Get(Of Boolean)("UiHiddenOtherAbout") AndAlso Settings.Get(Of Boolean)("UiHiddenOtherTest") AndAlso
            (Not Settings.Get(Of Boolean)("UiHiddenOtherFeedback") OrElse Not Settings.Get(Of Boolean)("UiHiddenOtherVote")) Then
            CheckHiddenOtherAbout.Checked = False
            CheckHiddenOtherTest.Checked = False
        End If
    End Sub

    Private Sub HiddenHint(sender As Object, user As Boolean) Handles CheckHiddenFunctionHidden.Change, CheckHiddenPageSetup.Change, CheckHiddenSetupUI.Change
        If AniControlEnabled = 0 AndAlso sender.Checked Then Hint("按 F12 即可暂时关闭功能隐藏设置。千万别忘了，要不然设置就改不回来了……")
    End Sub

#End Region

    Private Sub SliderLoad()
        SliderMusicVolume.GetHintText = Function(v) Math.Ceiling(v * 0.1) & "%"
        SliderLauncherTransparent.GetHintText = Function(v) Math.Round(40 + v * 0.1) & "%"
        SliderLauncherHue.GetHintText = Function(v) v & "°"
        SliderLauncherSat.GetHintText = Function(v) v & "%"
        SliderLauncherDelta.GetHintText =
        Function(Value As Integer) As String
            If Value > 90 Then
                Return "+" & (Value - 90)
            ElseIf Value = 90 Then
                Return 0
            Else
                Return Value - 90
            End If
        End Function
        SliderLauncherLight.GetHintText =
        Function(Value As Integer) As String
            If Value > 20 Then
                Return "+" & (Value - 20)
            ElseIf Value = 20 Then
                Return 0
            Else
                Return Value - 20
            End If
        End Function
        SliderBackgroundOpacity.GetHintText = Function(v) Math.Round(v * 0.1) & "%"
        SliderBackgroundBlur.GetHintText = Function(v) v & " 像素"
    End Sub

End Class
