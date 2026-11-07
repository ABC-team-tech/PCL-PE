Public Class PageLoginAuth
    Private IsFirstLoad As Boolean = True

    Public ReadOnly AuthServerPresets As New List(Of String) From {
        "https://littleskin.cn/api/yggdrasil",
        "https://mcskin.ml/api/yggdrasil",
        "https://skin.flowing.top/api/yggdrasil"
    }

    Private Function NormalizeServer(Input As String) As String
        Return NormalizeServerShared(Input)
    End Function

    Public Shared Sub SaveServerSettings(Server As String)
        If AniControlEnabled <> 0 Then Return
        Server = NormalizeServerShared(Server)
        If Server = "" Then Return
        Settings.Set("CacheAuthServerServer", Server)
        If McInstanceSelected IsNot Nothing Then Settings.Set("VersionServerAuthServer", Server, Instance:=McInstanceSelected)
    End Sub

    Public Sub Reload(KeepInput As Boolean)

        Dim SavedServer As String = NormalizeServer(Settings.Get(Of String)("CacheAuthServerServer"))
        If SavedServer = "" AndAlso McInstanceSelected IsNot Nothing Then
            SavedServer = NormalizeServer(Settings.Get(Of String)("VersionServerAuthServer", Instance:=McInstanceSelected))
        End If
        If SavedServer <> "" Then Settings.Set("CacheAuthServerServer", SavedServer)
        Dim ServerList As New List(Of String)
        If SavedServer <> "" AndAlso Not AuthServerPresets.Contains(SavedServer) Then ServerList.Add(SavedServer)
        ServerList.AddRange(AuthServerPresets)
        ComboServer.ItemsSource = ServerList
        ComboServer.Text = If(KeepInput AndAlso ComboServer.Text <> "", ComboServer.Text, SavedServer)

        CheckRemember.Checked = Settings.Get(Of Boolean)("LoginRemember")
        If KeepInput AndAlso Not IsFirstLoad Then

            Dim Input As String = ComboName.Text
            ComboName.ItemsSource = If(Settings.Get(Of String)("LoginAuthEmail") = "", Nothing, Settings.Get(Of String)("LoginAuthEmail").Split("¨"))
            ComboName.Text = Input
        Else

            If Settings.Get(Of String)("LoginAuthEmail") = "" Then
                ComboName.ItemsSource = Nothing
            Else
                ComboName.ItemsSource = Settings.Get(Of String)("LoginAuthEmail").Split("¨")
                ComboName.Text = Settings.Get(Of String)("LoginAuthEmail").BeforeFirst("¨")
                If Settings.Get(Of Boolean)("LoginRemember") Then TextPass.Password = Settings.Get(Of String)("LoginAuthPass").BeforeFirst("¨").Trim
            End If
        End If
        IsFirstLoad = False
    End Sub

    Public Shared Function GetLoginData() As McLoginServer
        Dim Server As String = GetServerAddress() & "/authserver"
        If FrmLoginAuth Is Nothing Then
            Return New McLoginServer(McLoginType.Auth) With {.Token = "Auth", .BaseUrl = Server, .UserName = "", .Password = "", .Description = "Authlib-Injector", .Type = McLoginType.Auth}
        Else
            Return New McLoginServer(McLoginType.Auth) With {.Token = "Auth", .BaseUrl = Server, .UserName = FrmLoginAuth.ComboName.Text.Replace("¨", "").Trim, .Password = FrmLoginAuth.TextPass.Password.Replace("¨", "").Trim, .Description = "Authlib-Injector", .Type = McLoginType.Auth}
        End If
    End Function

    Public Shared Function IsVaild(LoginData As McLoginServer) As String

        If GetServerAddress() = "" Then Return "请先填写外置登录地址！"
        If LoginData Is Nothing Then Return "请先填写外置登录地址！"
        If LoginData.UserName = "" Then Return "账号不能为空！"
        If LoginData.Password = "" Then Return "密码不能为空！"
        Return ""
    End Function
    Public Function IsVaild() As String
        Return IsVaild(GetLoginData())
    End Function

    Public Shared Function GetServerAddress() As String
        Dim Server As String = ""

        If FrmLoginAuth IsNot Nothing AndAlso FrmLoginAuth.Dispatcher.CheckAccess() Then
            Server = NormalizeServerShared(FrmLoginAuth.ComboServer.Text)
        End If

        If Server = "" Then Server = NormalizeServerShared(Settings.Get(Of String)("CacheAuthServerServer"))

        If Server = "" AndAlso McInstanceSelected IsNot Nothing Then
            Server = NormalizeServerShared(Settings.Get(Of String)("VersionServerAuthServer", Instance:=McInstanceSelected))
        End If
        Return Server
    End Function
    Public Shared Function NormalizeServerShared(Input As String) As String
        If Input Is Nothing Then Return ""
        Dim Server As String = Input.Replace("¨", "").Trim
        If Server = "" Then Return ""
        If Not Server.Contains("://") Then Server = "https://" & Server
        Server = Server.TrimEnd("/"c)

        If Server.EndsWithF("/authserver", True) Then Server = Server.Substring(0, Server.Length - 11).TrimEnd("/"c)
        Return Server
    End Function

    Private Sub ComboName_TextChanged(sender As Object, e As TextChangedEventArgs) Handles ComboName.TextChanged
        If sender.Text = "" Then TextPass.Password = ""
        If AniControlEnabled = 0 Then Settings.Set("CacheAuthAccess", "")
    End Sub
    Private Sub TextPass_PasswordChanged(sender As Object, e As RoutedEventArgs) Handles TextPass.PasswordChanged
        If AniControlEnabled = 0 Then Settings.Set("CacheAuthAccess", "")
    End Sub
    Private Sub ComboName_SelectionChanged(sender As MyComboBox, e As SelectionChangedEventArgs) Handles ComboName.SelectionChanged
        If sender.SelectedIndex = -1 OrElse Not Settings.Get(Of Boolean)("LoginRemember") Then
            TextPass.Password = ""
        Else
            TextPass.Password = Settings.Get(Of String)("LoginAuthPass").ToString.Split("¨")(sender.SelectedIndex).Trim
        End If
    End Sub
    Private Sub CheckBoxChange(sender As MyCheckBox, e As Object) Handles CheckRemember.Change
        If AniControlEnabled = 0 Then Settings.Set(sender.Tag, sender.Checked)
    End Sub

    Private _serverDebounce As System.Windows.Threading.DispatcherTimer

    Private Sub QueueFetchServerInfo()
        If AniControlEnabled <> 0 OrElse IsFirstLoad Then Return
        If _serverDebounce Is Nothing Then
            _serverDebounce = New System.Windows.Threading.DispatcherTimer With {.Interval = TimeSpan.FromMilliseconds(600)}
            AddHandler _serverDebounce.Tick,
                Sub(s As Object, ev As EventArgs)
                    _serverDebounce.Stop()
                    Settings.Set("CacheAuthAccess", "")
                    FetchServerInfo(NormalizeServerShared(ComboServer.Text))
                End Sub
        End If
        _serverDebounce.Stop()
        _serverDebounce.Start()
    End Sub
    Private Sub ComboServer_TextChanged(sender As Object, e As TextChangedEventArgs) Handles ComboServer.TextChanged

        Dim Server As String = NormalizeServerShared(ComboServer.Text)
        If Server.Contains(".") Then SaveServerSettings(Server)
        QueueFetchServerInfo()
    End Sub
    Private Sub ComboServer_SelectionChanged(sender As MyComboBox, e As SelectionChangedEventArgs) Handles ComboServer.SelectionChanged
        QueueFetchServerInfo()
    End Sub

    Private Sub FetchServerInfo(Server As String)
        If Server = "" Then Return
        SaveServerSettings(Server)
        RunInNewThread(Sub()
                           Try
                               Dim Info As String = NetRequestByClientRetry(Server, Encoding:=Encoding.UTF8)
                               Dim JsonObject As JObject = JObject.Parse(Info)
                               Dim Name As String = If(JsonObject("meta")?("serverName"), "")
                               Dim Register As String = If(JsonObject("meta")?("links")?("registerHomepage"), "")
                               RunInUi(Sub()
                                           Dim RegisterAddress As String = If(Register, "")
                                           If RegisterAddress = "" AndAlso Info.Contains("/auth/register") Then RegisterAddress = Server.Replace("/api/yggdrasil", "") & "/auth/register"
                                           If Name <> "" Then
                                               Settings.Set("CacheAuthServerName", Name)
                                               If McInstanceSelected IsNot Nothing AndAlso Settings.Get(Of Integer)("VersionServerLogin", Instance:=McInstanceSelected) = 4 Then Settings.Set("VersionServerAuthName", Name, Instance:=McInstanceSelected)
                                           End If
                                           Settings.Set("CacheAuthServerRegister", RegisterAddress)
                                           If RegisterAddress <> "" AndAlso McInstanceSelected IsNot Nothing AndAlso Settings.Get(Of Integer)("VersionServerLogin", Instance:=McInstanceSelected) = 4 Then Settings.Set("VersionServerAuthRegister", RegisterAddress, Instance:=McInstanceSelected)
                                       End Sub)
                           Catch ex As Exception

                           End Try
                       End Sub, "皮肤站元数据")
    End Sub

    Private Sub ComboName_TextChanged() Handles ComboName.TextChanged
        BtnLink.Content = If(ComboName.Text = "", "注册账号", "找回密码")
    End Sub
    Private Sub Btn_Click(sender As Object, e As EventArgs) Handles BtnLink.Click
        If BtnLink.Content = "注册账号" Then
            OpenWebsite(If(McInstanceSelected IsNot Nothing, Settings.Get(Of String)("VersionServerAuthRegister", Instance:=McInstanceSelected), Settings.Get(Of String)("CacheAuthServerRegister")))
        Else
            Dim Website As String = If(McInstanceSelected IsNot Nothing, Settings.Get(Of String)("VersionServerAuthRegister", Instance:=McInstanceSelected), Settings.Get(Of String)("CacheAuthServerRegister"))
            OpenWebsite(Website.Replace("/auth/register", "/auth/forgot"))
        End If
    End Sub

    Private Sub ReloadRegisterButton() Handles Me.Loaded
        Dim Address As String = If(McInstanceSelected IsNot Nothing, Settings.Get(Of String)("VersionServerAuthRegister", Instance:=McInstanceSelected), Settings.Get(Of String)("CacheAuthServerRegister"))
        BtnLink.Visibility = If(String.IsNullOrEmpty(New ValidateHttp().Validate(Address)), Visibility.Visible, Visibility.Collapsed)
    End Sub

End Class
