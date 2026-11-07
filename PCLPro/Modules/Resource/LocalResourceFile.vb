Public Class LocalResourceFile

#Region "文件信息"

    Public ReadOnly File As FileInfo
    Public Sub New(FileInfo As FileInfo)
        Me.File = FileInfo
    End Sub
    Public Sub New(FullName As String)
        Me.New(FileUtils.GetInfo(FullName))
    End Sub

    Public ReadOnly Property IsEnabled As Boolean
        Get
            Return {".jar", ".zip", ".litemod"}.Contains(File.Extension.Lower)
        End Get
    End Property

    Public ReadOnly Property EnabledName As String
        Get
            Return File.Name.Replace(".disabled", "").Replace(".old", "")
        End Get
    End Property

    Public ReadOnly Property EnabledFullName As String
        Get
            Return $"{File.DirectoryName}\{EnabledName}"
        End Get
    End Property

#End Region

#Region "信息项"

    Public Event OnUpdate(sender As LocalResourceFile)

    Public Property Display As String
        Get
            If _Display Is Nothing Then _Display = ProjectVersion?.Display
            If _Display Is Nothing Then _Display = PathUtils.GetFileNameWithoutExtension(EnabledName)
            Return _Display
        End Get
        Set(value As String)
            If _Display Is Nothing AndAlso value IsNot Nothing AndAlso Not value.Contains("modname") AndAlso
               value.Lower <> "name" AndAlso value.Count > 1 AndAlso Val(value).ToString <> value Then
                _Display = value
            End If
        End Set
    End Property
    Private _Display As String = Nothing

    Public Property Description As String
        Get
            Return _Description
        End Get
        Set(value As String)
            If _Description Is Nothing AndAlso value IsNot Nothing AndAlso value.Count > 2 Then
                _Description = value.ToString.Trim(vbLf)

                If _Description.Lower.LastIndexOfAny("qwertyuiopasdfghjklzxcvbnm0123456789") = _Description.Count - 1 Then _Description += "."
            End If
        End Set
    End Property
    Private _Description As String = Nothing

    Public Property Version As String
        Get
            If _Version Is Nothing Then _Version = ProjectVersion?.Version
            Return _Version
        End Get
        Set(value As String)
            If _Version IsNot Nothing AndAlso _Version.RegexCheck("[0-9.\-]+") Then Return
            If value IsNot Nothing AndAlso value.ContainsIgnoreCase("version") Then value = "version"
            _Version = value
        End Set
    End Property
    Public _Version As String = Nothing

#End Region

#Region "网络信息"

    Public Property Project As ResourceProject
        Get
            Return _Project
        End Get
        Set(value As ResourceProject)
            _Project = value
            RaiseEvent OnUpdate(Me)
        End Set
    End Property
    Private _Project As ResourceProject

    Public ProjectVersion As ResourceVersion

    Public Property UpdateFile As ResourceVersion
        Get
            Return _UpdateFile
        End Get
        Set(value As ResourceVersion)
            _UpdateFile = value
            RaiseEvent OnUpdate(Me)
        End Set
    End Property
    Private _UpdateFile As ResourceVersion

    Public ChangelogUrls As New List(Of String)

    Public OnlineDataLoaded As Boolean = False

    Public Function ToJson() As JObject
        Dim Json As New JObject
        If Project IsNot Nothing Then Json.Add("Project", Project.ToJson())
        Json.Add("ChangelogUrls", New JArray(ChangelogUrls))
        Json.Add("OnlineDataLoaded", OnlineDataLoaded)
        If ProjectVersion IsNot Nothing Then Json.Add("Version", ProjectVersion.ToCacheJson())
        If UpdateFile IsNot Nothing Then Json.Add("UpdateFile", UpdateFile.ToCacheJson())
        Return Json
    End Function

    Public Sub FromJson(Json As JObject)
        OnlineDataLoaded = Json("OnlineDataLoaded")
        If Json.ContainsKey("Project") Then Project = New ResourceProject(Json("Project"))
        If Json.ContainsKey("ChangelogUrls") Then ChangelogUrls = Json("ChangelogUrls").ToObject(Of List(Of String))
        If Json.ContainsKey("Version") Then ProjectVersion = ResourceVersion.FromCacheJson(Json("Version"))
        If Json.ContainsKey("UpdateFile") Then UpdateFile = ResourceVersion.FromCacheJson(Json("UpdateFile"))
    End Sub

    Public ReadOnly Property HasUpdate As Boolean
        Get
            Return Not Settings.Get(Of Boolean)("UiHiddenFunctionModUpdate") AndAlso Not Settings.Get(Of Boolean)("VersionAdvanceDisableModUpdate", Instance:=PageInstanceLeft.Instance) AndAlso UpdateFile IsNot Nothing
        End Get
    End Property

    Public ReadOnly Property CurseForgeHash As UInteger
        Get
            If _CurseForgeHash Is Nothing Then

                Dim CacheKey As String = $"{EnabledFullName}-{File.LastWriteTime.ToLongTimeString}-{File.Length}-C".GetStableHashCode()
                Dim Cached As String = ReadIni(PathTemp & "Cache\ModHash.ini", CacheKey)
                If Cached <> "" AndAlso Cached.RegexCheck("^\d+$") Then
                    _CurseForgeHash = Cached
                    Return _CurseForgeHash
                End If

                Dim data As New List(Of Byte)
                For Each b As Byte In FileUtils.ReadAsBytes(File.FullName)
                    If b = 9 OrElse b = 10 OrElse b = 13 OrElse b = 32 Then Continue For
                    data.Add(b)
                Next

                Dim length As Integer = data.Count
                Dim h As UInteger = 1 Xor length
                Dim i As Integer
                For i = 0 To length - 4 Step 4
                    Dim k As UInteger = data(i) Or CUInt(data(i + 1)) << 8 Or CUInt(data(i + 2)) << 16 Or CUInt(data(i + 3)) << 24
                    k = (k * &H5BD1E995L) And &HFFFFFFFFL
                    k = k Xor (k >> 24)
                    k = (k * &H5BD1E995L) And &HFFFFFFFFL
                    h = (h * &H5BD1E995L) And &HFFFFFFFFL
                    h = h Xor k
                Next
                Select Case length - i
                    Case 3
                        h = h Xor (data(i) Or CUInt(data(i + 1)) << 8)
                        h = h Xor (CUInt(data(i + 2)) << 16)
                        h = (h * &H5BD1E995L) And &HFFFFFFFFL
                    Case 2
                        h = h Xor (data(i) Or CUInt(data(i + 1)) << 8)
                        h = (h * &H5BD1E995L) And &HFFFFFFFFL
                    Case 1
                        h = h Xor data(i)
                        h = (h * &H5BD1E995L) And &HFFFFFFFFL
                End Select
                h = h Xor (h >> 13)
                h = (h * &H5BD1E995L) And &HFFFFFFFFL
                h = h Xor (h >> 15)
                _CurseForgeHash = h

                WriteIni(PathTemp & "Cache\ModHash.ini", CacheKey, h.ToString)
            End If
            Return _CurseForgeHash
        End Get
    End Property
    Private _CurseForgeHash As UInteger?

    Public ReadOnly Property ModrinthHash As String
        Get
            If _ModrinthHash Is Nothing Then

                Dim CacheKey As String = $"{EnabledFullName}-{File.LastWriteTime.ToLongTimeString}-{File.Length}-M".GetStableHashCode()
                Dim Cached As String = ReadIni(PathTemp & "Cache\ModHash.ini", CacheKey)
                If Cached <> "" Then
                    _ModrinthHash = Cached
                    Return _ModrinthHash
                End If

                _ModrinthHash = CryptographyUtils.ComputeFileHash(File.FullName, CryptographyUtils.HashMethod.Sha1)

                WriteIni(PathTemp & "Cache\ModHash.ini", CacheKey, _ModrinthHash)
            End If
            Return _ModrinthHash
        End Get
    End Property
    Private _ModrinthHash As String

#End Region

#Region "API"

    Public Overrides Function ToString() As String
        Return File.FullName
    End Function
    Public Overrides Function Equals(obj As Object) As Boolean
        Dim Target = TryCast(obj, LocalResourceFile)
        Return Target IsNot Nothing AndAlso PathUtils.IsSame(File.FullName, Target.File.FullName)
    End Function

#End Region

End Class
