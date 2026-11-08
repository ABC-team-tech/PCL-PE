Imports System.Runtime.CompilerServices
Imports System.Xaml

Public Module ModBase

#Region "声明"

    Public Const VersionBaseName As String = "1.3.7"
    Public Const CommitHash As String = ""
#If RELEASE Then
    Public Const VersionCode As Integer = 436
#Else
    Public Const VersionCode As Integer = 436
#End If

    Public Const VersionDisplay As String = BuildTypeDisplay & " " & VersionBaseName
#If DEBUG Then
    Public Const BuildTypeDisplay As String = "开发版"
    Public Const BuildType As BuildTypes = BuildTypes.Debug
#ElseIf RELEASE Then
    Public Const BuildTypeDisplay As String = "正式版"
    Public Const BuildType As BuildTypes = BuildTypes.Release
#Else
    Public Const BuildTypeDisplay As String = "快照版"
    Public Const BuildType As BuildTypes = BuildTypes.Snapshot
#End If
    Public Enum BuildTypes
        Debug = 100
        Release = 50
        Snapshot = 0
    End Enum

    Public Handle As IntPtr

    Public PathExe As String = Paths.Base & AppDomain.CurrentDomain.SetupInformation.ApplicationName

    Public PathImage As String = "pack://application:,,,/PCL Professional Edition;component/Images/"

    Public Function NormalizePathImage(Path As String) As String
        If Path Is Nothing Then Return Path
        Const Head As String = "pack://application:,,,/"
        Const Marker As String = ";component/Images/"
        If Not Path.StartsWithF(Head) OrElse Path.StartsWithF(PathImage) Then Return Path
        Dim Index As Integer = Path.IndexOf(Marker, StringComparison.OrdinalIgnoreCase)
        If Index < 0 Then Return Path
        Return PathImage & Path.Substring(Index + Marker.Length)
    End Function

    Public PathTemp As String = PathUtils.AddSlashSuffix(If(Settings.Get(Of String)("SystemSystemCache") = "", Path.GetTempPath() & "PCL\", Settings.Get(Of String)("SystemSystemCache")).Replace("/", "\"))

    Public Lang As String = "zh_CN"

    Public ApplicationStartTick As Long = GetTimeMs()

    Public ApplicationOpenTime As Date = Date.Now

    Public Identify As String = GetIdentify()

    Public IsProgramEnding As Boolean = False

    Public IsGBKEncoding As Boolean = Encoding.Default.CodePage = 936

    Public OsDrive As String = Environment.GetLogicalDrives().First(Function(p) Directory.Exists(p))

    Public ReadOnly Property ModeDebug As Boolean
        Get
            Return Settings.Get(Of Boolean)("SystemDebugMode")
        End Get
    End Property

#End Region

#Region "矢量图标"

    Public Class Logo

        Public Const IconButtonLikeLine As String = "M512 896a42.666667 42.666667 0 0 1-30.293333-12.373333l-331.52-331.946667a224.426667 224.426667 0 0 1 0-315.733333 223.573333 223.573333 0 0 1 315.733333 0L512 282.026667l46.08-46.08a223.573333 223.573333 0 0 1 315.733333 0 224.426667 224.426667 0 0 1 0 315.733333l-331.52 331.946667A42.666667 42.666667 0 0 1 512 896zM308.053333 256a136.533333 136.533333 0 0 0-97.28 40.106667 138.24 138.24 0 0 0 0 194.986666L512 792.746667l301.226667-301.653334a138.24 138.24 0 0 0 0-194.986666 141.653333 141.653333 0 0 0-194.56 0l-76.373334 76.8a42.666667 42.666667 0 0 1-60.586666 0L405.333333 296.106667A136.533333 136.533333 0 0 0 308.053333 256z"

        Public Const IconButtonLikeFill As String = "M700.856 155.543c-74.769 0-144.295 72.696-190.046 127.26-45.737-54.576-115.247-127.26-190.056-127.26-134.79 0-244.443 105.78-244.443 235.799 0 77.57 39.278 131.988 70.845 175.713C238.908 694.053 469.62 852.094 479.39 858.757c9.41 6.414 20.424 9.629 31.401 9.629 11.006 0 21.998-3.215 31.398-9.63 9.782-6.662 240.514-164.703 332.238-291.701 31.587-43.724 70.874-98.143 70.874-175.713-0.001-130.02-109.656-235.8-244.445-235.8z m0 0"

        Public Const IconButtonDelete As String = "M520.192 0C408.43 0 317.44 82.87 313.563 186.734H52.736c-29.038 0-52.663 21.943-52.663 49.079s23.625 49.152 52.663 49.152h58.075v550.473c0 103.35 75.118 187.757 167.717 187.757h472.43c92.599 0 167.716-83.894 167.716-187.757V285.477h52.59c29.038 0 52.59-21.943 52.663-49.08-0.073-27.135-23.625-49.151-52.663-49.151H726.235C723.237 83.017 631.955 0 520.192 0zM404.846 177.957c3.803-50.03 50.176-89.015 107.447-89.015 57.197 0 103.57 38.985 106.788 89.015H404.92zM284.379 933.669c-33.353 0-69.997-39.351-69.997-95.525v-549.01H833.39v549.522c0 56.247-36.645 95.525-69.998 95.525H284.379v-0.512z M357.23 800.695a48.274 48.274 0 0 0 47.616-49.006V471.7a48.274 48.274 0 0 0-47.543-49.08 48.274 48.274 0 0 0-47.69 49.006V751.69c0 27.282 20.846 49.006 47.617 49.006z m166.62 0a48.274 48.274 0 0 0 47.688-49.006V471.7a48.274 48.274 0 0 0-47.689-49.08 48.274 48.274 0 0 0-47.543 49.006V751.69c0 27.282 21.431 49.006 47.543 49.006z m142.92 0a48.274 48.274 0 0 0 47.543-49.006V471.7a48.274 48.274 0 0 0-47.543-49.08 48.274 48.274 0 0 0-47.616 49.006V751.69c0 27.282 20.773 49.006 47.543 49.006z"

        Public Const IconButtonStop As String = "M508 990.4c-261.6 0-474.4-212-474.4-474.4S246.4 41.6 508 41.6s474.4 212 474.4 474.4S769.6 990.4 508 990.4zM508 136.8c-209.6 0-379.2 169.6-379.2 379.2 0 209.6 169.6 379.2 379.2 379.2s379.2-169.6 379.2-379.2C887.2 306.4 717.6 136.8 508 136.8zM697.6 563.2 318.4 563.2c-26.4 0-47.2-21.6-47.2-47.2 0-26.4 21.6-47.2 47.2-47.2l379.2 0c26.4 0 47.2 21.6 47.2 47.2C744.8 542.4 724 563.2 697.6 563.2z"

        Public Const IconButtonCheck As String = "M512 0a512 512 0 1 0 512 512A512 512 0 0 0 512 0z m0 921.6a409.6 409.6 0 1 1 409.6-409.6 409.6 409.6 0 0 1-409.6 409.6z M716.8 339.968l-256 253.44L328.192 460.8A51.2 51.2 0 0 0 256 532.992l168.448 168.96a51.2 51.2 0 0 0 72.704 0l289.28-289.792A51.2 51.2 0 0 0 716.8 339.968z"

        Public Const IconButtonEdit As String = "M732.64 64.32C688.576 21.216 613.696 21.216 569.6 64.32L120.128 499.52c-17.6 12.896-26.432 30.144-30.848 51.68L32 870.048c0 25.856 8.8 56 26.432 73.248 17.632 17.216 17.632 48.704 88.64 48.704h13.248l326.08-56c22.016-4.32 39.68-12.928 52.864-30.176l449.472-435.2c22.048-21.536 35.264-47.36 35.264-77.536 0-30.176-13.216-56-35.264-77.568l-256.096-251.2zM139.712 903.776l56-326.912 311.04-295.136 267.104 269.44-310.976 295.168-323.168 57.44zM844.576 467.84l-273.984-260.672 61.856-59.84c8.832-8.512 26.528-8.512 39.776 0l234.24 226.496c4.384 4.288 8.832 12.8 8.832 17.088s-4.416 8.544-8.864 12.8l-61.856 64.128z"

        Public Const IconButtonSetup As String = "M651.946667 1001.813333c-22.186667 0-42.666667-10.24-61.44-27.306666-23.893333-23.893333-49.493333-35.84-75.093334-35.84-29.013333 0-56.32 11.946667-73.386666 30.72v3.413333c-17.066667 17.066667-42.666667 27.306667-66.56 27.306667h-6.826667c-6.826667 0-11.946667-1.706667-15.36-1.706667l-6.826667-1.706667c-64.853333-20.48-121.173333-54.613333-168.96-98.986666-29.013333-23.893333-37.546667-63.146667-25.6-95.573334 8.533333-23.893333 5.12-51.2-10.24-75.093333-15.36-27.306667-34.133333-40.96-59.733333-47.786667h-1.706667l-5.12-1.706666c-35.84-8.533333-61.44-34.133333-66.56-69.973334C1.706667 575.146667 0 537.6 0 512c0-32.426667 3.413333-63.146667 8.533333-93.866667v-6.826666l3.413334-8.533334c10.24-23.893333 23.893333-40.96 44.373333-51.2 5.12-3.413333 11.946667-6.826667 20.48-8.533333 27.306667-8.533333 51.2-25.6 63.146667-44.373333 13.653333-23.893333 17.066667-52.906667 10.24-81.92-11.946667-34.133333 0-71.68 30.72-93.866667 44.373333-37.546667 97.28-68.266667 158.72-93.866667l3.413333-1.706666c44.373333-13.653333 75.093333 3.413333 92.16 20.48 23.893333 23.893333 49.493333 35.84 75.093333 35.84 30.72 0 56.32-10.24 71.68-30.72l3.413334-3.413334c27.306667-27.306667 63.146667-35.84 93.866666-22.186666 63.146667 22.186667 117.76 54.613333 165.546667 97.28 29.013333 23.893333 37.546667 63.146667 25.6 95.573333-8.533333 23.893333-5.12 51.2 10.24 75.093333 15.36 27.306667 34.133333 40.96 59.733333 47.786667h1.706667l5.12 1.706667c35.84 8.533333 61.44 34.133333 66.56 71.68 6.826667 30.72 10.24 63.146667 11.946667 93.866666v3.413334c0 32.426667-3.413333 63.146667-8.533334 93.866666v6.826667l-3.413333 8.533333c-10.24 23.893333-23.893333 40.96-44.373333 51.2-5.12 3.413333-11.946667 6.826667-20.48 8.533334-27.306667 8.533333-51.2 25.6-63.146667 46.08-13.653333 23.893333-17.066667 52.906667-10.24 81.92 11.946667 35.84-1.706667 75.093333-30.72 95.573333-44.373333 35.84-95.573333 66.56-157.013333 92.16-15.36 3.413333-27.306667 3.413333-35.84 3.413333z m3.413333-83.626666z m1.706667 0zM517.12 853.333333c47.786667 0 93.866667 20.48 134.826667 59.733334 1.706667 1.706667 3.413333 1.706667 3.413333 3.413333 52.906667-22.186667 97.28-49.493333 136.533333-80.213333l1.706667-1.706667v-3.413333c-13.653333-52.906667-8.533333-104.106667 17.066667-148.48 23.893333-39.253333 64.853333-69.973333 114.346666-85.333334 1.706667 0 3.413333-1.706667 6.826667-6.826666 5.12-25.6 8.533333-51.2 8.533333-78.506667-1.706667-29.013333-3.413333-56.32-10.24-81.92v-5.12h-1.706666c-51.2-11.946667-90.453333-39.253333-119.466667-87.04-27.306667-44.373333-34.133333-100.693333-17.066667-148.48l-1.706666-1.706667h-3.413334c-39.253333-35.84-85.333333-63.146667-136.533333-80.213333H648.533333s-1.706667 1.706667-3.413333 1.706667c-32.426667 39.253333-80.213333 59.733333-136.533333 59.733333-47.786667 0-93.866667-20.48-134.826667-59.733333l-1.706667-1.706667h-1.706666c-54.613333 22.186667-98.986667 49.493333-136.533334 80.213333l-1.706666 1.706667v3.413333c13.653333 52.906667 8.533333 104.106667-17.066667 148.48-23.893333 39.253333-64.853333 69.973333-114.346667 85.333334-1.706667 0-3.413333 1.706667-6.826666 6.826666-6.826667 25.6-8.533333 51.2-8.533334 78.506667 0 30.72 3.413333 58.026667 6.826667 76.8l1.706667 5.12h1.706666c51.2 11.946667 90.453333 39.253333 119.466667 87.04 27.306667 44.373333 34.133333 100.693333 17.066667 148.48l1.706666 1.706667 1.706667 1.706666c37.546667 35.84 83.626667 63.146667 134.826667 80.213334 1.706667 0 3.413333 0 3.413333 1.706666h1.706667s1.706667 0 5.12-1.706666c34.133333-37.546667 81.92-59.733333 136.533333-59.733334z m-6.826667-146.773333c-110.933333 0-199.68-85.333333-199.68-196.266667 0-109.226667 87.04-196.266667 199.68-196.266666s199.68 85.333333 199.68 196.266666c-1.706667 109.226667-88.746667 196.266667-199.68 196.266667z m0-307.2c-63.146667 0-114.346667 49.493333-114.346666 110.933333 0 63.146667 49.493333 110.933333 114.346666 110.933334 30.72 0 59.733333-11.946667 80.213334-32.426667 20.48-20.48 32.426667-49.493333 32.426666-78.506667 0-63.146667-49.493333-110.933333-112.64-110.933333z"

        Public Const IconButtonReset As String = "M667.6817627 313.65283203l-45.28564454 55.76660156L858.06933594 391.27124023 787.61950684 165.93066406l-56.01379395 69.01611328A354.47387695 354.47387695 0 0 0 520.89892578 165.93066406C324.87536621 165.93066406 165.93066406 324.43041992 165.93066406 519.91015625c0 195.52917481 158.94470215 353.97949219 354.96826172 353.97949219a355.06713867 355.06713867 0 0 0 331.73217774-227.66418458 50.52612305 50.52612305 0 0 0-29.21813966-65.25878905 50.77331543 50.77331543 0 0 0-65.50598144 29.16870117A253.61938477 253.61938477 0 0 1 520.94836426 772.78796387c-140.05920411 0-253.61938477-113.21411133-253.61938477-252.87780762 0-139.61425781 113.56018067-252.82836914 253.61938477-252.82836914 53.59130859 0 104.46350098 16.61132813 146.73339843 46.57104492"

        Public Const IconButtonRefresh As String = "M512.0 838.3c-80.2 0-153.4-29.3-210.2-77.4l75.5-75.5c11.5-11.5 25.8-22.0 25.8-37.0a27.2 27.2 0 0 0-27.1-27.1H104.0c-27.1 0-27.1 23.9-27.1 27.1v271.9a27.1 27.1 0 0 0 27.1 27.1c15.0 0 27.8-16.6 42.5-31.2l77.9-77.9c76.6 67.7 177.1 108.9 287.4 108.9 221.7 0 404.5-166.0 431.2-380.6h-109.8c-25.9 154.2-159.7 271.9-321.3 271.9zM919.9 76.6c-15.0 0-27.8 16.6-42.5 31.3L799.5 185.8c-76.5-67.7-177.1-108.9-287.4-108.9-221.8 0-404.5 166.1-431.3 380.6H190.6c25.9-154.2 159.7-271.9 321.4-271.9 80.2 0 153.4 29.3 210.1 77.4l-75.5 75.5c-11.6 11.5-25.8 22.0-25.8 37.1a27.2 27.2 0 0 0 27.1 27.1h271.9c27.1 0 27.1-23.9 27.1-27.1V103.8a27.1 27.1 0 0 0-27.1-27.1z"

        Public Const IconButtonSave As String = "M819.392 0L1024 202.752v652.16a168.96 168.96 0 0 1-168.832 168.768h-104.192a47.296 47.296 0 0 1-10.752 0H283.776a47.232 47.232 0 0 1-10.752 0H168.832A168.96 168.96 0 0 1 0 854.912V168.768A168.96 168.96 0 0 1 168.832 0h650.56z m110.208 854.912V242.112l-149.12-147.776H168.896c-41.088 0-74.432 33.408-74.432 74.432v686.144c0 41.024 33.344 74.432 74.432 74.432h62.4v-190.528c0-33.408 27.136-60.544 60.544-60.544h440.448c33.408 0 60.544 27.136 60.544 60.544v190.528h62.4c41.088 0 74.432-33.408 74.432-74.432z m-604.032 74.432h372.864v-156.736H325.568v156.736z m403.52-596.48a47.168 47.168 0 1 1 0 94.336H287.872a47.168 47.168 0 1 1 0-94.336h441.216z m0-153.728a47.168 47.168 0 1 1 0 94.4H287.872a47.168 47.168 0 1 1 0-94.4h441.216z"

        Public Const IconButtonInfo As String = "M512 917.333333c223.861333 0 405.333333-181.472 405.333333-405.333333S735.861333 106.666667 512 106.666667 106.666667 288.138667 106.666667 512s181.472 405.333333 405.333333 405.333333z m0 106.666667C229.226667 1024 0 794.773333 0 512S229.226667 0 512 0s512 229.226667 512 512-229.226667 512-512 512z m-32-597.333333h64a21.333333 21.333333 0 0 1 21.333333 21.333333v320a21.333333 21.333333 0 0 1-21.333333 21.333333h-64a21.333333 21.333333 0 0 1-21.333333-21.333333V448a21.333333 21.333333 0 0 1 21.333333-21.333333z m0-192h64a21.333333 21.333333 0 0 1 21.333333 21.333333v64a21.333333 21.333333 0 0 1-21.333333 21.333333h-64a21.333333 21.333333 0 0 1-21.333333-21.333333v-64a21.333333 21.333333 0 0 1 21.333333-21.333333z"

        Public Const IconButtonList As String = "M384 128h640v128H384zM160 192m-96 0a96 96 0 1 0 192 0 96 96 0 1 0-192 0ZM384 448h640v128H384zM160 512m-96 0a96 96 0 1 0 192 0 96 96 0 1 0-192 0ZM384 768h640v128H384zM160 832m-96 0a96 96 0 1 0 192 0 96 96 0 1 0-192 0Z"

        Public Const IconButtonOpen As String = "M889.018182 418.909091H884.363636V316.509091a93.090909 93.090909 0 0 0-99.607272-89.832727h-302.545455l-93.090909-76.334546A46.545455 46.545455 0 0 0 358.865455 139.636364H146.152727A93.090909 93.090909 0 0 0 46.545455 229.469091V837.818182a46.545455 46.545455 0 0 0 46.545454 46.545454 46.545455 46.545455 0 0 0 16.756364-3.258181 109.381818 109.381818 0 0 0 25.134545 3.258181h586.472727a85.178182 85.178182 0 0 0 87.04-63.301818l163.374546-302.545454a46.545455 46.545455 0 0 0 5.585454-21.876364A82.385455 82.385455 0 0 0 889.018182 418.909091z m-744.727273-186.181818h198.283636l93.09091 76.334545a46.545455 46.545455 0 0 0 29.323636 10.705455h319.301818a12.101818 12.101818 0 0 1 6.516364 0V418.909091H302.545455a85.178182 85.178182 0 0 0-87.04 63.301818L139.636364 622.778182V232.727273a19.549091 19.549091 0 0 1 6.516363 0z m578.094546 552.029091a27.461818 27.461818 0 0 0-2.792728 6.516363H154.530909l147.083636-272.290909a27.461818 27.461818 0 0 0 2.792728-6.981818h565.061818z"

        Public Const IconButtonArrowUp As String = "M554 333V853h-85V333l-228 228-60-60L512 170l331 331-60 60L554 333z"

        Public Const IconButtonArrowDown As String = "M554 691V171h-85V691L241 463l-60 60L512 854l331-331-60-60L554 691z"

        Public Const IconButtonCard As String = "M834.5 684.1c-31.2-70.4-98.9-120.9-179.1-127.3 63.5-8.5 112.6-63 112.6-128.8 0-71.8-58.2-130-130-130s-130 58.2-130 130c0 65.9 49 120.3 112.6 128.8-80.2 6.4-148 57-179.1 127.3-8.7 19.7 6 42 27.6 42 12.1 0 22.7-7.5 27.7-18.5 24.3-53.9 78.5-91.5 141.3-91.5s117 37.6 141.3 91.5c5 11.1 15.6 18.5 27.7 18.5 21.4 0 36.1-22.3 27.4-42zM567.9 427.9c0-38.6 31.4-70 70-70s70 31.4 70 70-31.4 70-70 70-70-31.4-70-70zM460.3 347.9H216.9c-16.6 0-30 13.4-30 30s13.4 30 30 30h243.3c16.6 0 30-13.4 30-30 0.1-16.5-13.4-30-29.9-30zM367.4 459.6H216.9c-16.6 0-30 13.4-30 30s13.4 30 30 30h150.4c16.6 0 30-13.4 30-30 0.1-16.6-13.4-30-29.9-30zM297.4 571.2H217c-16.6 0-30 13.4-30 30s13.4 30 30 30h80.4c16.6 0 30-13.4 30-30 0-16.5-13.5-30-30-30zM900 236v552H124V236h776m0-60H124c-33.1 0-60 26.9-60 60v552c0 33.1 26.9 60 60 60h776c33.1 0 60-26.9 60-60V236c0-33.1-26.9-60-60-60z"

        Public Const IconButtonCross As String = "F1 M 26.9166,22.1667L 37.9999,33.25L 49.0832,22.1668L 53.8332,26.9168L 42.7499,38L 53.8332,49.0834L 49.0833,53.8334L 37.9999,42.75L 26.9166,53.8334L 22.1666,49.0833L 33.25,38L 22.1667,26.9167L 26.9166,22.1667 Z"

        Public Const IconButtonMojang As String = "M9.183,18.967c-0.109-2.239,1.336-5.119,3.92-4.96c3.657-0.027,7.319-0.044,10.977,0.005	c3.712,0.214,6.596,2.759,9.652,4.533c0.181-1.697-0.197-3.717,1.237-4.982c1.899,2.091,3.143,4.894,5.677,6.334	c1.577,0.805,2.973-0.668,4.221-1.44c1.55,2.305,2.108,5.075,2.622,7.752c0.657,3.438,0.947,6.925,1.111,10.413	c-1.734-0.733-3.355-1.708-4.971-2.671c-1.396-4.933-5.349-8.656-9.723-11.059c-3.46-1.817-7.752-2.185-11.338-0.52	c-3.761,1.593-6.285,5.666-5.891,9.75c0.121,4.577,3.17,8.765,7.391,10.456c7.746,3.285,16.407,2.234,24.521,1.243	c0.454,2.315-1.511,4.527-3.695,4.889c-10.577,0.038-21.148-0.011-31.725,0.022c-2.217,0.279-4.079-1.879-3.964-4.008	C9.167,36.141,9.216,27.556,9.183,18.967z M40.114,8.09c0.294-0.801,0.872-1.417,1.542-1.924c1.379,2.589,2.742,5.482,2.311,8.491	c-0.332,2.54-4.42,2.507-5.052,0.163C38.349,12.527,39.209,10.178,40.114,8.09z M0,53 l0.1,0 z"

        Public Const IconButtonUser As String = "M660.338 528.065c63.61-46.825 105.131-121.964 105.131-206.83 0-141.7-115.29-256.987-256.997-256.987-141.706 0-256.998 115.288-256.998 256.987 0 85.901 42.52 161.887 107.456 208.562-152.1 59.92-260.185 207.961-260.185 381.077 0 21.276 17.253 38.53 38.53 38.53 21.278 0 38.53-17.254 38.53-38.53 0-183.426 149.232-332.671 332.667-332.671 1.589 0 3.113-0.207 4.694-0.244 0.8 0.056 1.553 0.244 2.362 0.244 183.434 0 332.664 149.245 332.664 332.671 0 21.276 17.255 38.53 38.533 38.53 21.277 0 38.53-17.254 38.53-38.53 0-174.885-110.354-324.13-264.917-382.809z m-331.803-206.83c0-99.22 80.72-179.927 179.935-179.927s179.937 80.708 179.937 179.927c0 99.203-80.721 179.91-179.937 179.91s-179.935-80.708-179.935-179.91z"

        Public Const IconButtonShield As String = "M511.488256 95.184408c35.310345 22.516742 95.184408 55.78011 167.34033 84.437781 75.738131 29.681159 148.405797 40.93953 191.392304 45.033483v353.615193c0 73.691154-50.662669 164.781609-136.123938 244.101949C649.65917 901.181409 558.568716 942.12094 512 942.12094c-46.568716 0-137.65917-40.93953-222.096952-119.748126C204.441779 742.54073 153.77911 651.450275 153.77911 577.247376v-353.103448c42.474763-4.093953 116.165917-15.352324 191.904048-45.545227 75.226387-30.192904 133.565217-63.456272 165.805098-83.414293M512 0c-4.093953 0-8.187906 1.535232-11.258371 3.582209l-14.84058 10.234882c-1.023488 0.511744-67.550225 47.592204-170.410794 88.531735-100.813593 39.916042-198.556722 41.963018-199.58021 41.963018l-25.075462 0.511744c-10.746627 0.511744-18.934533 8.187906-18.934533 18.422789v414.000999c0 216.97951 286.064968 446.24088 440.09995 446.24088s440.09995-229.261369 440.09995-445.729136V163.758121c0-10.234883-8.69965-18.422789-18.934533-18.422789l-24.563718-0.511744c-1.023488 0-98.766617-2.046977-199.58021-41.963018-103.372314-40.93953-170.410795-88.01999-170.922538-88.531734L523.258371 3.582209c-3.070465-2.558721-7.164418-3.582209-11.258371-3.582209z M743.308346 410.930535l-260.477761 260.477761c-15.864068 15.864068-41.963018 15.864068-57.827087 0l-144.823588-144.823588c-15.864068-15.864068-15.864068-41.963018 0-57.827087 8.187906-8.187906 18.422789-11.770115 29.169415-11.770115 10.234883 0 20.981509 4.093953 29.169416 11.770115l115.654173 115.654173L685.993003 352.591704c15.864068-15.864068 41.963018-15.864068 57.827087 0 15.352324 16.375812 15.352324 42.474763-0.511744 58.338831z"

        Public Const IconButtonOffline As String = "M533.293176 788.841412a60.235294 60.235294 0 1 1 85.202824 85.202823l-42.616471 42.586353c-129.355294 129.385412-339.124706 129.385412-468.510117 0-129.385412-129.385412-129.385412-339.124706 0-468.510117l42.586353-42.616471a60.235294 60.235294 0 1 1 85.202823 85.202824l-42.61647 42.586352a210.823529 210.823529 0 1 0 298.164706 298.164706l42.586352-42.61647z m255.548236-255.548236l42.61647-42.586352a210.823529 210.823529 0 1 0-298.164706-298.164706l-42.586352 42.61647a60.235294 60.235294 0 1 1-85.202824-85.202823l42.616471-42.586353c129.355294-129.385412 339.124706-129.385412 468.510117 0 129.385412 129.385412 129.385412 339.124706 0 468.510117l-42.586353 42.616471a60.235294 60.235294 0 1 1-85.202823-85.202824zM192.542118 192.542118a60.235294 60.235294 0 0 1 85.202823 0l553.712941 553.712941a60.235294 60.235294 0 0 1-85.202823 85.202823L192.542118 277.744941a60.235294 60.235294 0 0 1 0-85.202823z"

        Public Const IconButtonServer As String = "M224 160a64 64 0 0 0-64 64v576a64 64 0 0 0 64 64h576a64 64 0 0 0 64-64V224a64 64 0 0 0-64-64H224z m0 384h576v256H224v-256z m192 96v64h320v-64H416z m-128 0v64h64v-64H288zM224 224h576v256H224V224z m192 96v64h320v-64H416z m-128 0v64h64v-64H288z"

        Public Const IconMusic As String = "M348.293565 716.53287V254.797913c0-41.672348 28.004174-78.358261 68.919652-90.37913L815.994435 40.826435c62.775652-18.610087 125.907478 26.579478 125.907478 89.933913v539.158261c8.013913 42.25113-8.94887 89.177043-47.014956 127.109565a232.848696 232.848696 0 0 1-170.785392 65.758609c-61.885217-2.938435-111.081739-33.435826-129.113043-80.050087-18.031304-46.614261-2.137043-102.177391 41.672348-145.853218a232.848696 232.848696 0 0 1 170.785391-65.80313c21.014261 1.024 40.514783 5.164522 57.878261 12.065391V233.338435c0-12.109913-10.551652-20.034783-20.569044-20.034783a24.620522 24.620522 0 0 0-5.787826 0.934957L439.785739 338.18713a19.545043 19.545043 0 0 0-14.825739 19.144348v438.984348H423.846957c11.53113 43.987478-5.164522 94.208-45.412174 134.322087a232.848696 232.848696 0 0 1-170.785392 65.758609c-61.885217-2.938435-111.081739-33.435826-129.113043-80.050087-18.031304-46.614261-2.137043-102.177391 41.672348-145.853218a232.848696 232.848696 0 0 1 170.785391-65.80313c20.791652 1.024 40.069565 5.075478 57.299478 11.842783z"

        Public Const IconPlay As String = "M803.904 463.936a55.168 55.168 0 0 1 0 96.128l-463.616 264.448C302.848 845.888 256 819.136 256 776.448V247.616c0-42.752 46.848-69.44 84.288-48.064l463.616 264.384z"
    End Class

#End Region

#Region "自定义类"

    Public Class MyColor

        Public A As Double = 255
        Public R As Double = 0
        Public G As Double = 0
        Public B As Double = 0

        Public Shared Widening Operator CType(str As String) As MyColor
            Return New MyColor(str)
        End Operator
        Public Shared Widening Operator CType(col As Color) As MyColor
            Return New MyColor(col)
        End Operator
        Public Shared Widening Operator CType(conv As MyColor) As Color
            Return Color.FromArgb(ClampToByte(conv.A), ClampToByte(conv.R), ClampToByte(conv.G), ClampToByte(conv.B))
        End Operator
        Public Shared Widening Operator CType(conv As MyColor) As System.Drawing.Color
            Return System.Drawing.Color.FromArgb(ClampToByte(conv.A), ClampToByte(conv.R), ClampToByte(conv.G), ClampToByte(conv.B))
        End Operator
        Public Shared Widening Operator CType(bru As SolidColorBrush) As MyColor
            Return New MyColor(bru.Color)
        End Operator
        Public Shared Widening Operator CType(conv As MyColor) As SolidColorBrush
            Return New SolidColorBrush(Color.FromArgb(ClampToByte(conv.A), ClampToByte(conv.R), ClampToByte(conv.G), ClampToByte(conv.B)))
        End Operator
        Public Shared Widening Operator CType(bru As Brush) As MyColor
            Return New MyColor(bru)
        End Operator
        Public Shared Widening Operator CType(conv As MyColor) As Brush
            Return New SolidColorBrush(Color.FromArgb(ClampToByte(conv.A), ClampToByte(conv.R), ClampToByte(conv.G), ClampToByte(conv.B)))
        End Operator

        Public Shared Operator +(a As MyColor, b As MyColor) As MyColor
            Return New MyColor With {.A = a.A + b.A, .B = a.B + b.B, .G = a.G + b.G, .R = a.R + b.R}
        End Operator
        Public Shared Operator -(a As MyColor, b As MyColor) As MyColor
            Return New MyColor With {.A = a.A - b.A, .B = a.B - b.B, .G = a.G - b.G, .R = a.R - b.R}
        End Operator
        Public Shared Operator *(a As MyColor, b As Double) As MyColor
            Return New MyColor With {.A = a.A * b, .B = a.B * b, .G = a.G * b, .R = a.R * b}
        End Operator
        Public Shared Operator /(a As MyColor, b As Double) As MyColor
            Return New MyColor With {.A = a.A / b, .B = a.B / b, .G = a.G / b, .R = a.R / b}
        End Operator
        Public Shared Operator =(a As MyColor, b As MyColor) As Boolean
            If IsNothing(a) AndAlso IsNothing(b) Then Return True
            If IsNothing(a) OrElse IsNothing(b) Then Return False
            Return a.A = b.A AndAlso a.R = b.R AndAlso a.G = b.G AndAlso a.B = b.B
        End Operator
        Public Shared Operator <>(a As MyColor, b As MyColor) As Boolean
            If IsNothing(a) AndAlso IsNothing(b) Then Return False
            If IsNothing(a) OrElse IsNothing(b) Then Return True
            Return Not (a.A = b.A AndAlso a.R = b.R AndAlso a.G = b.G AndAlso a.B = b.B)
        End Operator

        Public Shared Function Lerp(ValueA As MyColor, ValueB As MyColor, Percent As Double) As MyColor
            Return Round(ValueA * (1 - Percent) + ValueB * Percent, 6)
        End Function
        Public Shared Function Round(col As MyColor, Optional w As Integer = 0) As MyColor
            Return New MyColor With {.A = Math.Round(col.A, w), .R = Math.Round(col.R, w), .G = Math.Round(col.G, w), .B = Math.Round(col.B, w)}
        End Function

        Public Shared Function ClampToByte(d As Double) As Byte
            If d < 0 Then d = 0
            If d > 255 Then d = 255
            Return Math.Round(d)
        End Function

        Public Sub New()
        End Sub
        Public Sub New(col As Color)
            Me.A = col.A
            Me.R = col.R
            Me.G = col.G
            Me.B = col.B
        End Sub
        Public Sub New(HexString As String)
            Dim StringColor As Media.Color = ColorConverter.ConvertFromString(HexString)
            A = StringColor.A
            R = StringColor.R
            G = StringColor.G
            B = StringColor.B
        End Sub
        Public Sub New(newA As Double, col As MyColor)
            Me.A = newA
            Me.R = col.R
            Me.G = col.G
            Me.B = col.B
        End Sub
        Public Sub New(newR As Double, newG As Double, newB As Double)
            Me.A = 255
            Me.R = newR
            Me.G = newG
            Me.B = newB
        End Sub
        Public Sub New(newA As Double, newR As Double, newG As Double, newB As Double)
            Me.A = newA
            Me.R = newR
            Me.G = newG
            Me.B = newB
        End Sub
        Public Sub New(brush As Brush)

            If TypeOf brush Is SolidColorBrush Then
                Dim Color As Color = CType(brush, SolidColorBrush).Color
                A = Color.A
                R = Color.R
                G = Color.G
                B = Color.B
            ElseIf TypeOf brush Is GradientBrush Then
                Dim Gradient = CType(brush, GradientBrush)
                Dim SumA As Double = 0, SumR As Double = 0, SumG As Double = 0, SumB As Double = 0, WeightSum As Double = 0
                For Each StopColor In Gradient.GradientStops
                    Dim Weight As Double = Math.Max(0.0001, StopColor.Color.A / 255.0)
                    SumA += StopColor.Color.A * Weight
                    SumR += StopColor.Color.R * Weight
                    SumG += StopColor.Color.G * Weight
                    SumB += StopColor.Color.B * Weight
                    WeightSum += Weight
                Next
                If WeightSum <= 0 Then
                    A = 255 : R = 255 : G = 255 : B = 255
                Else
                    A = SumA / WeightSum : R = SumR / WeightSum : G = SumG / WeightSum : B = SumB / WeightSum
                End If
            Else
                A = 255 : R = 255 : G = 255 : B = 255
            End If
        End Sub
        Public Sub New(brush As SolidColorBrush)
            Dim Color As Color = brush.Color
            A = Color.A
            R = Color.R
            G = Color.G
            B = Color.B
        End Sub
        Public Sub New(obj As Object)
            If obj Is Nothing Then
                A = 255 : R = 255 : G = 255 : B = 255
            Else
                If TypeOf obj Is SolidColorBrush Then

                    Dim Color As Color = CType(obj, SolidColorBrush).Color
                    A = Color.A
                    R = Color.R
                    G = Color.G
                    B = Color.B
                ElseIf TypeOf obj Is GradientBrush Then
                    Dim TempBrush As New MyColor(CType(obj, GradientBrush))
                    A = TempBrush.A : R = TempBrush.R : G = TempBrush.G : B = TempBrush.B
                Else
                    A = obj.A
                    R = obj.R
                    G = obj.G
                    B = obj.B
                End If
            End If
        End Sub

        Public Function Hue(v1 As Double, v2 As Double, vH As Double) As Double
            If vH < 0 Then vH += 1
            If vH > 1 Then vH -= 1
            If vH < 0.16667 Then Return v1 + (v2 - v1) * 6 * vH
            If vH < 0.5 Then Return v2
            If vH < 0.66667 Then Return v1 + (v2 - v1) * (4 - vH * 6)
            Return v1
        End Function
        Public Function FromHSL(sH As Double, sS As Double, sL As Double) As MyColor
            A = 255
            If sS = 0 Then
                R = sL * 2.55 : G = R : B = R
                Return Me
            End If

            Dim H = sH / 360
            Dim S = sS / 100
            Dim L = sL / 100
            S = If(L < 0.5, S * L + L, S * (1.0 - L) + L)
            L = 2 * L - S
            R = 255 * Hue(L, S, H + 1 / 3)
            G = 255 * Hue(L, S, H)
            B = 255 * Hue(L, S, H - 1 / 3)
            Return Me
        End Function
        Public Function FromHSL2(sH As Double, sS As Double, sL As Double) As MyColor
            A = 255
            If sS = 0 Then
                R = sL * 2.55 : G = R : B = R
                Return Me
            End If

            sH = (sH + 3600000) Mod 360
            Dim cent As Double() = {
                +0.1, -0.06, -0.3,
                -0.19, -0.15, -0.24,
                -0.32, -0.09, +0.18,
                +0.05, -0.12, -0.02,
                +0.1, -0.06}

            Dim center As Double = sH / 30.0
            Dim intCenter As Integer = Math.Floor(center)
            center = 50 - (
                 (1 - center + intCenter) * cent(intCenter) + (center - intCenter) * cent(intCenter + 1)
                ) * sS
            sL = If(sL < center, sL / center, 1 + (sL - center) / (100 - center)) * 50
            FromHSL(sH, sS, sL)
            Return Me
        End Function

        Public Overrides Function ToString() As String
            Return "(" & A & "," & R & "," & G & "," & B & ")"
        End Function
        Public Overrides Function Equals(obj As Object) As Boolean
            Return Me = obj
        End Function

    End Class

    Public Class MyRect

        Public Property Width As Double = 0
        Public Property Height As Double = 0
        Public Property Left As Double = 0
        Public Property Top As Double = 0

        Public Sub New()
        End Sub
        Public Sub New(left As Double, top As Double, width As Double, height As Double)
            Me.Left = left
            Me.Top = top
            Me.Width = width
            Me.Height = height
        End Sub

    End Class

    Public Enum LoadState
        Waiting
        Loading
        Finished
        Failed
        Canceled
    End Enum

    Public Enum ProcessReturnValues

        Success = 0

        Fail = 1

        Exception = 2

        Timeout = 3

        Cancel = 4

        TaskDone = 5
    End Enum

#End Region

#Region "文件"

    Private ReadOnly IniCache As New ConcurrentDictionary(Of String, ConcurrentDictionary(Of String, String))

    Public Sub IniClearCache(FileName As String)
        If Not FileName.Contains(":\") Then FileName = $"{Paths.Base}PCL\{FileName}.ini"
        IniCache.Remove(FileName)
    End Sub

    Private Function IniGetContent(FileName As String) As ConcurrentDictionary(Of String, String)
        Try

            If Not FileName.Contains(":\") Then FileName = $"{Paths.Base}PCL\{FileName}.ini"

            Dim Cache As ConcurrentDictionary(Of String, String) = Nothing
            If IniCache.TryGetValue(FileName, Cache) Then Return Cache

            If Not FileUtils.Exists(FileName) Then Return Nothing
            Dim Ini As New ConcurrentDictionary(Of String, String)
            For Each Line In FileUtils.ReadAsLines(FileName, True)
                Dim Index As Integer = Line.IndexOfF(":")
                If Index > 0 Then Ini(Line.Substring(0, Index)) = Line.Substring(Index + 1)
            Next
            IniCache(FileName) = Ini
            Return Ini
        Catch ex As Exception
            Logger.Error(ex, $"生成 ini 文件缓存失败（{FileName}）", LogBehavior.Toast)
            Return Nothing
        End Try
    End Function

    Public Function ReadIni(FileName As String, Key As String, Optional DefaultValue As String = "") As String
        Dim Content = IniGetContent(FileName)
        If Content Is Nothing Then Return DefaultValue
        Return Content.GetOrDefault(Key, DefaultValue)
    End Function

    Public Function HasIniKey(FileName As String, Key As String) As Boolean
        Dim Content = IniGetContent(FileName)
        Return Content IsNot Nothing AndAlso Content.ContainsKey(Key)
    End Function

    Public Sub DeleteIniKey(FileName As String, Key As String)
        WriteIni(FileName, Key, Nothing)
    End Sub

    Public Sub WriteIni(FileName As String, Key As String, Value As String)
        Try

            If Key.Contains(":") Then Throw New Exception($"尝试写入 ini 文件 {FileName} 的键名中包含了冒号：{Key}")
            Key = Key.ReplaceLineEndings("")
            Value = Value?.ReplaceLineEndings("")

            Dim Content = IniGetContent(FileName)
            If Content Is Nothing Then Content = New ConcurrentDictionary(Of String, String)

            If Value Is Nothing Then
                If Not Content.Remove(Key) Then Return
            Else
                Dim ExistingValue As String = Nothing
                If Content.TryGetValue(Key, ExistingValue) AndAlso ExistingValue = Value Then Return
                Content(Key) = Value
            End If

            Dim FileContent As New StringBuilder
            For Each Pair In Content
                FileContent.Append(Pair.Key)
                FileContent.Append(":")
                FileContent.Append(Pair.Value)
                FileContent.Append(vbCrLf)
            Next
            If Not FileName.Contains(":\") Then FileName = $"{Paths.Base}PCL\{FileName}.ini"

            FileName = If(FileName.Contains(":\"), FileName, Paths.Base & FileName)
            FileUtils.Write(FileName, FileContent.ToString)
        Catch ex As Exception
            Logger.Error(ex, $"写入文件失败（{FileName} → {Key}:{Value}）", LogBehavior.Toast)
        End Try
    End Sub

    Public Function CheckPermission(Folder As String) As Boolean
        Try
            If String.IsNullOrEmpty(Folder) Then Return False
            Folder = PathUtils.AddSlashSuffix(Folder)
            If Folder.EndsWithF(":\System Volume Information\", True) OrElse Folder.EndsWithF(":\$RECYCLE.BIN\", True) Then Return False
            If Not DirectoryUtils.Exists(Folder) Then Return False
            Dim TestFilePath As String = $"{Folder}CheckPermission{GetUuid()}.txt"
            FileUtils.Write(TestFilePath, "临时文件，用于检测该文件夹的权限是否正常。")
            FileUtils.Delete(TestFilePath)
            Return True
        Catch ex As Exception
            Logger.Warn(ex, $"没有对文件夹 {Folder} 的权限，请尝试以管理员权限运行 PCL")
            Return False
        End Try
    End Function

    Public Sub CheckPermissionWithException(Folder As String)
        If String.IsNullOrWhiteSpace(Folder) Then Throw New ArgumentNullException("文件夹名不能为空！")
        Folder = PathUtils.AddSlashSuffix(Folder)
        If Folder.EndsWithF(":\System Volume Information\", True) OrElse Folder.EndsWithF(":\$RECYCLE.BIN\", True) Then Throw New UnauthorizedAccessException("没有对系统文件夹的权限！")
        If Not DirectoryUtils.Exists(Folder) Then Throw New DirectoryNotFoundException("文件夹不存在！")
        Dim TestFilePath As String = $"{Folder}CheckPermission{GetUuid()}.txt"
        FileUtils.Write(TestFilePath, "临时文件，用于检测该文件夹的权限是否正常。")
        FileUtils.Delete(TestFilePath)
    End Sub

#End Region

#Region "文本"
    Public vbLQ As Char = Convert.ToChar(8220)
    Public vbRQ As Char = Convert.ToChar(8221)

    Public Function Val(Str As Object) As Double
        If Str Is Nothing Then Return 0
        Try
            Return If(TypeOf Str Is String AndAlso Str = "&", 0, Conversion.Val(Str))
        Catch
            Return 0
        End Try
    End Function

#End Region

#Region "搜索"

    Private Function SearchSimilarity(Source As String, Query As String) As Double
        If String.IsNullOrEmpty(Source) OrElse String.IsNullOrEmpty(Query) Then Return 0
        Dim qp As Integer = 0, lenSum As Double = 0
        Dim str As New StringBuilder(Source.Length)
        str.Append(Source.Lower().Replace(" ", ""))
        Query = Query.Lower().Replace(" ", "")
        Dim sourceLength As Integer = str.Length, queryLength As Integer = Query.Length
        If queryLength = 0 Then Return 0
        Do While qp < queryLength

            Dim sp As Integer = 0, lenMax As Integer = 0, spMax As Integer = 0
            Dim currentSourceLength As Integer = str.Length
            Do While sp < currentSourceLength
                Dim len As Integer = 0
                While (qp + len) < queryLength AndAlso (sp + len) < currentSourceLength AndAlso str(sp + len) = Query(qp + len)
                    len += 1
                End While

                If len > lenMax Then
                    lenMax = len
                    spMax = sp
                End If

                sp += If(len > 0, len, 1)
            Loop
            If lenMax > 0 Then
                str.Remove(spMax, lenMax)

                Dim IncWeight = (Math.Pow(1.4, 3 + lenMax) - 3.6)
                IncWeight *= 1 + 0.3 * Math.Max(0, 3 - Math.Abs(qp - spMax))
                lenSum += IncWeight
            End If
            qp += If(lenMax > 0, lenMax, 1)
        Loop

        Return (lenSum / queryLength) * (3 / Math.Sqrt(sourceLength + 15)) * If(queryLength <= 2, 3 - queryLength, 1)
    End Function

    Private Function SearchSimilarityWeighted(Source As List(Of SearchSource), Query As String) As Double
        Dim TotalWeight As Double = 0
        Dim Sum As Double = 0
        For Each Pair In Source
            If Pair.Aliases.Any Then
                Sum += Pair.Aliases.Max(Function(a) SearchSimilarity(a, Query)) * Pair.Weight
            End If
            TotalWeight += Pair.Weight
        Next
        Return Sum / TotalWeight
    End Function

    Public Class SearchEntry(Of T)

        Public Item As T

        Public SearchSource As List(Of SearchSource)

        Public Similarity As Double

        Public AbsoluteRight As Boolean
        Public Overrides Function ToString() As String
            Return Math.Round(Similarity, 3) & " - " & Item.ToString()
        End Function
    End Class

    Public Class SearchSource
        Public Aliases As String()
        Public Weight As Double
        Public Sub New(Aliases As String(), Optional Weight As Double = 1)
            Me.Aliases = Aliases
            Me.Weight = Weight
        End Sub
        Public Sub New(Text As String, Optional Weight As Double = 1)
            Me.Aliases = {Text}
            Me.Weight = Weight
        End Sub
    End Class

    Public Function Search(Of T)(Entries As List(Of SearchEntry(Of T)), Query As String, Optional MaxBlurCount As Integer = 5, Optional MinBlurSimilarity As Double = 0.1) As List(Of SearchEntry(Of T))

        Dim ResultList As New List(Of SearchEntry(Of T))
        If Not Entries.Any() Then Return ResultList

        Dim QueryParts = Query.Split(" ", True)
        Dim Candidates As New List(Of SearchEntry(Of T))
        For Each Entry In Entries
            Entry.Similarity = SearchSimilarityWeighted(Entry.SearchSource, Query)
            Entry.AbsoluteRight =
                QueryParts.All(
                Function(QueryPart) Entry.SearchSource.Any(
                Function(Source) Source.Aliases.Any(
                Function([Alias]) [Alias].Replace(" ", "").ContainsIgnoreCase(QueryPart))))
            If Entry.AbsoluteRight OrElse Entry.Similarity >= MinBlurSimilarity Then Candidates.Add(Entry)
        Next

        Candidates = Candidates.SortByComparison(Function(Left, Right) If(Left.AbsoluteRight <> Right.AbsoluteRight, Left.AbsoluteRight, Left.Similarity > Right.Similarity))

        Dim BlurCount As Integer = 0
        For Each Entry In Candidates
            If Entry.AbsoluteRight Then
                ResultList.Add(Entry)
            Else
                If BlurCount = MaxBlurCount Then Exit For
                ResultList.Add(Entry)
                BlurCount += 1
            End If
        Next
        Return ResultList
    End Function

#End Region

#Region "系统"

    <Extension> Public Function GetResultWithTimeout(Of T)(TargetTask As Task(Of T), TokenSource As CancellationTokenSource, TimeoutMs As Integer) As T
        Dim DelayTask = Task.Delay(TimeoutMs)
        If Task.WhenAny(TargetTask, DelayTask).ConfigureAwait(False).GetAwaiter().GetResult() Is DelayTask Then
            TokenSource.Cancel()
            Throw New TimeoutException($"任务超时（{TimeoutMs} ms）")
        End If
        Return TargetTask.Run()
    End Function

    <Extension> Public Sub GetResultWithTimeout(TargetTask As Task, TokenSource As CancellationTokenSource, TimeoutMs As Integer)
        Dim DelayTask = Task.Delay(TimeoutMs)
        If Task.WhenAny(TargetTask, DelayTask).ConfigureAwait(False).GetAwaiter().GetResult() Is DelayTask Then
            TokenSource.Cancel()
            Throw New TimeoutException($"任务超时（{TimeoutMs} ms）")
        End If
        TargetTask.Run()
    End Sub

    Public PathPure As New Lazy(Of String)(
    Function()
        If Paths.Base.IsAsciiOnly() Then
            Return Paths.Base & "PCL\"
        ElseIf Paths.AppDataThenName.IsAsciiOnly() Then
            Return Paths.AppDataThenName
        ElseIf PathTemp.IsAsciiOnly() Then
            Return PathTemp
        Else
            Return OsDrive & "ProgramData\PCL\"
        End If
    End Function)

    Public Class RestartException
        Inherits Exception
    End Class

    Public Function RunAsAdmin(Argument As String) As Integer
        Dim NewProcess = StartProcess(New ProcessStartInfo(PathExe) With {.Verb = "runas", .Arguments = Argument})
        NewProcess.WaitForExit()
        Return NewProcess.ExitCode
    End Function

    Private Uuid As Integer = 1
    Private UuidLock As Object

    Public Function GetUuid() As Integer
        If UuidLock Is Nothing Then UuidLock = New Object
        SyncLock UuidLock
            Uuid += 1
            Return Uuid
        End SyncLock
    End Function

    Public Function GetTimeMs() As Long
        Return Stopwatch.GetTimestamp() \ (Stopwatch.Frequency \ 1000L)
    End Function

    Public Function GetUnixTimestampUtc() As Long
        Return (Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000
    End Function

    Public Function ToUnixTimestampUtc(Time As Date) As Long
        Return (Time.ToUniversalTime().Ticks - 621355968000000000) / 10000000
    End Function

    Public Function GetDate(timeStamp As Integer) As Date
        Return DateTimeOffset.FromUnixTimeSeconds(timeStamp).LocalDateTime
    End Function

    Public Function GetLocalTime(UtcDate As Date) As Date
        Return DateTime.SpecifyKind(UtcDate, DateTimeKind.Utc).ToLocalTime
    End Function

    Public NotInheritable Class RouteEventArgs
        Inherits EventArgs
        Public RaiseByMouse As Boolean
        Public Handled As Boolean = False
        Public Sub New(Optional RaiseByMouse As Boolean = False)
            Me.RaiseByMouse = RaiseByMouse
        End Sub
    End Class

    Public Function StartProcess(FileName As String, Optional Arguments As String = "") As Process
        Return StartProcess(New ProcessStartInfo With {.FileName = FileName, .Arguments = Arguments})
    End Function

    Public Function StartProcess(StartInfo As ProcessStartInfo) As Process
        StartInfo.FileName = PathUtils.ToShortPath(StartInfo.FileName)
        If Not String.IsNullOrEmpty(StartInfo.WorkingDirectory) Then StartInfo.WorkingDirectory = PathUtils.ToShortPath(StartInfo.WorkingDirectory)
        Logger.Info($"启动进程：{StartInfo.FileName} {StartInfo.Arguments}")
        Return Process.Start(StartInfo)
    End Function

    Public Function RunInNewThread(Action As Action, Optional Name As String = Nothing, Optional Priority As ThreadPriority = ThreadPriority.Normal) As Thread
        Dim th As New Thread(
        Sub()
            Try
                Action()
            Catch ex As Exception
                Logger.Error(ex, $"{Name}：线程执行失败")
            End Try
        End Sub) With {.Name = If(Name, "Runtime New Invoke " & GetUuid() & "#"), .Priority = Priority}
        th.Start()
        Return th
    End Function

    Public Function RunInUiWait(Of Output)(Action As Func(Of Output)) As Output
        If RunInUi() Then
            Return Action()
        Else
            Return Application.Current.Dispatcher.Invoke(Action)
        End If
    End Function

    Public Sub RunInUiWait(Action As Action)
        If RunInUi() Then
            Action()
        Else
            Application.Current.Dispatcher.Invoke(Action)
        End If
    End Sub

    Public Sub RunInUi(Action As Action, Optional ForceWaitUntilLoaded As Boolean = False)
        If ForceWaitUntilLoaded Then
            Application.Current.Dispatcher.InvokeAsync(Action, Threading.DispatcherPriority.Loaded)
        ElseIf RunInUi() Then
            Action()
        Else
            Application.Current.Dispatcher.InvokeAsync(Action)
        End If
    End Sub

    Public Sub RunInThread(Action As Action)
        If RunInUi() Then
            RunInNewThread(Action, "Invoke " & GetUuid())
        Else
            Action()
        End If
    End Sub

    <Extension> Public Function SortByComparison(Of T)(List As IList(Of T), SortRule As Func(Of T, T, Boolean)) As List(Of T)
        Dim NewList As New List(Of T)
        While List.Any
            Dim Highest = List(0)
            For i = 1 To List.Count - 1
                If SortRule(List(i), Highest) Then Highest = List(i)
            Next
            List.Remove(Highest)
            NewList.Add(Highest)
        End While
        Return NewList
    End Function

    Public Function GetProgramArgument(Name As String, Optional DefaultValue As Object = "")
        Dim AllArguments() As String = Command.Split(" ")
        For i = 0 To AllArguments.Length - 1
            If AllArguments(i) = "-" & Name Then
                If AllArguments.Length = i + 1 OrElse AllArguments(i + 1).StartsWithF("-") Then Return True
                Return AllArguments(i + 1)
            End If
        Next
        Return DefaultValue
    End Function

    Public Sub OpenWebsite(Url As String)
        Try
            If Not Url.StartsWithF("http", True) AndAlso Not Url.StartsWithF("minecraft://", True) AndAlso Not Url.StartsWithF("minecraft-preview://", True) Then
                Throw New Exception(Url & " 不是一个有效的网址，它必须以 http 开头！")
            End If
            Logger.Info($"正在打开网页：{Url}")
            StartProcess(Url)
        Catch ex As Exception
            Logger.Warn(ex, $"无法打开网页（{Url}）")
            ClipboardSet(Url, False)
            MyMsgBox("可能由于浏览器未正确配置，PCL 无法为你打开网页。" & vbCrLf & "网址已经复制到剪贴板，若有需要可以手动粘贴访问。" & vbCrLf &
                     $"网址：{Url}", "无法打开网页")
        End Try
    End Sub

    Public Sub OpenExplorer(Location As String)
        Try
            Location = Location.Replace("/", "\").Trim(" "c, """"c)
            Logger.Info($"正在打开资源管理器：{Location}")
            If Location.EndsWithF("\") Then
                StartProcess(Location)
            Else
                StartProcess("explorer", $"/select,""{Location}""")
            End If
        Catch ex As Exception
            Logger.Error(ex, "打开资源管理器失败，请尝试关闭安全软件（如 360 安全卫士）", LogBehavior.Alert)
        End Try
    End Sub

    Public Sub ClipboardSet(Text As String, Optional ShowSuccessHint As Boolean = True)
        RunInThread(
        Sub()
            Try
                Retrier.Attempt(delay:=Function(Attempt) TimeSpan.FromMilliseconds(200), maxAttempts:=4, isRetryAllowed:=Function(ex) True, action:=
                Sub()
                    RunInUiWait(
                    Sub()
                        My.Computer.Clipboard.Clear()
                        If Not String.IsNullOrEmpty(Text) Then My.Computer.Clipboard.SetText(Text)
                    End Sub)
                End Sub)
                If ShowSuccessHint Then Hint("已成功复制！", HintType.Green)
            Catch ex As Exception
                Logger.Error(ex, "可能由于剪贴板被其他程序占用，文本复制失败", LogBehavior.Toast)
            End Try
        End Sub)
    End Sub

    Public Function ClipboardGetText() As String
        Dim Result As String = Nothing
        RunInUiWait(
        Sub()
            Try
                If My.Computer.Clipboard.ContainsText() Then Result = My.Computer.Clipboard.GetText()
            Catch ex As Exception
                Logger.Warn(ex, "获取剪贴板文本失败")
            End Try
        End Sub)
        Return Result
    End Function

    Public Sub ExtractResources(FilePath As String, ResourceName As String)
        Dim Resource = My.Resources.ResourceManager.GetObject(ResourceName)
        If TypeOf Resource Is Byte() Then
            Dim Bytes = DirectCast(Resource, Byte())
            If FileUtils.Exists(FilePath) AndAlso CryptographyUtils.ComputeFileHash(FilePath) = CryptographyUtils.ComputeHash(Bytes) Then Return
            Logger.Info($"将资源写入到文件：{ResourceName} → {FilePath}")
            FileUtils.Write(FilePath, Bytes)
        ElseIf TypeOf Resource Is String Then
            Dim Content = DirectCast(Resource, String)
            If FileUtils.Exists(FilePath) AndAlso FileUtils.ReadAsString(FilePath) = Content Then Return
            Logger.Info($"将资源写入到文件：{ResourceName} → {FilePath}")
            FileUtils.Write(FilePath, Content)
        Else
            Throw New Exception($"资源 {ResourceName} 的类型不支持：{Resource.GetType.Name}")
        End If
    End Sub

#End Region

#Region "UI"

    Public ReadOnly DPI As Integer = System.Drawing.Graphics.FromHwnd(IntPtr.Zero).DpiX

    Public Function GetPixelSize(WPFSize As Double) As Double
        Return WPFSize / 96 * DPI
    End Function

    Public Function GetWPFSize(PixelSize As Double) As Double
        Return PixelSize * 96 / DPI
    End Function

    Public Function GetObjectFromXML(Str As XElement)
        Return GetObjectFromXML(Str.ToString)
    End Function

    Public Function GetObjectFromXML(Str As String) As Object

        Dim AsmName As String = Reflection.Assembly.GetExecutingAssembly().GetName().Name
        If AsmName <> "Plain Craft Launcher Enhanced" Then
            Str = Str.Replace("Plain Craft Launcher Enhanced", AsmName)
        End If
        Str = Str.
            Replace("EventType=""", "local:CustomEventService.EventType=""").
            Replace("EventData=""", "local:CustomEventService.EventData=""").
            Replace("EventType='", "local:CustomEventService.EventType='").
            Replace("EventData='", "local:CustomEventService.EventData='").
            Replace("Property=""EventType""", "Property=""local:CustomEventService.EventType""").
            Replace("Property=""EventData""", "Property=""local:CustomEventService.EventData""").
            Replace("Property='EventType'", "Property='local:CustomEventService.EventType'").
            Replace("Property='EventData'", "Property='local:CustomEventService.EventData'")
        Using Stream As New MemoryStream(Encoding.UTF8.GetBytes(Str))

            Using Reader As New XamlXmlReader(Stream)
                While Reader.Read()
                    For Each BlackListType In {GetType(WebBrowser), GetType(Frame), GetType(MediaElement), GetType(ObjectDataProvider), GetType(XamlReader), GetType(Window), GetType(XmlDataProvider), GetType(SettingService)}
                        If Reader.Type IsNot Nothing AndAlso BlackListType.IsAssignableFrom(Reader.Type.UnderlyingType) Then Throw New UnauthorizedAccessException($"基于安全考虑，不允许使用 {BlackListType.Name} 类型。")
                        If Reader.Value IsNot Nothing AndAlso Reader.Value = BlackListType.Name Then Throw New UnauthorizedAccessException($"基于安全考虑，不允许使用 {BlackListType.Name} 值。")
                    Next
                    For Each BlackListMember In {"Code", "FactoryMethod", "Static"}
                        If Reader.Member IsNot Nothing AndAlso Reader.Member.Name = BlackListMember Then Throw New UnauthorizedAccessException($"基于安全考虑，不允许使用 {BlackListMember} 成员。")
                    Next
                End While
            End Using

            Stream.Position = 0
            Using Writer As New StreamWriter(Stream)
                Writer.Write(Str)
                Writer.Flush()
                Stream.Position = 0
                Return Markup.XamlReader.Load(Stream)
            End Using
        End Using
    End Function

    Private ReadOnly UiThreadId As Integer = Thread.CurrentThread.ManagedThreadId

    Public Function RunInUi() As Boolean
        Return Thread.CurrentThread.ManagedThreadId = UiThreadId
    End Function

    <Extension> Public Function IsVisibleInForm(element As FrameworkElement) As Boolean
        If Not element.IsVisible Then Return False
        Dim bounds As Rect = element.TransformToAncestor(FrmMain).TransformBounds(New Rect(0, 0, element.ActualWidth, element.ActualHeight))
        Dim rect As New Rect(0, 0, FrmMain.ActualWidth, FrmMain.ActualHeight)
        Return rect.Contains(bounds.TopLeft) OrElse rect.Contains(bounds.BottomRight)
    End Function

    <Extension> Public Function IsTextTrimmed(TextBlock As TextBlock) As Boolean
        Dim typeface As New Typeface(TextBlock.FontFamily, TextBlock.FontStyle, TextBlock.FontWeight, TextBlock.FontStretch)
        Dim formattedText As New FormattedText(TextBlock.Text, Thread.CurrentThread.CurrentCulture, TextBlock.FlowDirection, typeface, TextBlock.FontSize, TextBlock.Foreground, DPI)
        Return formattedText.Width > TextBlock.ActualWidth
    End Function

    <Extension> Public Function ToVisibility(IsVisible As Boolean) As Visibility
        Return If(IsVisible, Visibility.Visible, Visibility.Collapsed)
    End Function

#End Region

#Region "随机"

    Private ReadOnly Random As New Random

    Public Function RandomOne(Of T)(objects As ICollection(Of T)) As T
        Return objects(RandomInteger(0, objects.Count - 1))
    End Function

    Public Function RandomInteger(min As Integer, max As Integer) As Integer
        Return Math.Floor((max - min + 1) * Random.NextDouble) + min
    End Function

    <Extension> Public Iterator Function Shuffle(Of T)(Raw As IEnumerable(Of T)) As IEnumerable(Of T)
        Dim RawCopy As New List(Of T)(Raw)
        Do While RawCopy.Any
            Dim i As Integer = RandomInteger(0, RawCopy.Count - 1)
            Yield RawCopy(i)
            RawCopy.RemoveAt(i)
        Loop
    End Function

#End Region

#Region "自动更新检查"

    Private Const UpdateRepo As String = "ABC-team-tech/PCL-PE-Unofficial"
    Private Const UpdateApi As String = "https://api.github.com/repos/" & UpdateRepo & "/releases/latest"

    Private ReadOnly UpdateMirrors As String() = {"https://ghfast.top/", "https://gh-proxy.com/", "https://ghproxy.net/"}

    Public Class UpdateRelease
        Public VersionName As String = ""
        Public Changelog As String = ""
        Public AssetName As String = ""
        Public AssetUrl As String = ""
        Public AssetSize As Long = 0
        Public IsInstaller As Boolean = False
    End Class

    Public Sub CheckUpdateAtStartup()
        If BuildType = BuildTypes.Debug Then Return
        If Not String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PCL_DEBUG_PAGE")) Then Return
        If Not Settings.Get(Of Boolean)("UiCheckUpdateAtStartup") Then Return
        RunInNewThread(
        Sub()
            Thread.Sleep(4000)
            Try
                Dim Release As UpdateRelease = FetchLatestRelease()
                If Release Is Nothing Then
                    Logger.Info("检查更新：无法获取最新版本信息，本次跳过")
                    Return
                End If
                If CompareVersion(Release.VersionName, VersionBaseName) <= 0 Then
                    Logger.Info($"检查更新：已是最新版本（{VersionBaseName}）")
                    Return
                End If
                Logger.Info($"检查更新：发现新版本 {Release.VersionName}，当前 {VersionBaseName}")
                Dim Desc As String = ""
                If Not String.IsNullOrWhiteSpace(Release.Changelog) Then Desc = vbCrLf & vbCrLf & "更新内容：" & vbCrLf & Release.Changelog.Trim
                RunInUi(
                Sub()
                    If MyMsgBox(LinkFixTip(Release.VersionName) & $"检测到新版本 {Release.VersionName}，当前版本 {VersionBaseName}。" & vbCrLf & "是否立即下载并更新？" & Desc,
                                "发现新版本", "立即更新", "稍后") = 1 Then
                        DownloadUpdate(Release)
                    End If
                End Sub)
            Catch ex As Exception
                Logger.Warn(ex, "检查更新失败")
            End Try
        End Sub, "Update Checker")
    End Sub

    Public Sub CheckUpdateManually()
        RunInNewThread(
        Sub()
            Try
                Logger.Info("手动检查更新：开始")
                Dim Release As UpdateRelease = FetchLatestRelease()
                If Release Is Nothing Then
                    RunInUi(Sub() MyMsgBox("无法获取最新版本信息。" & vbCrLf &
                                           "请检查网络后重试，或稍后前往 GitHub Releases 查看。",
                                           "检查更新", IsWarn:=True))
                    Return
                End If
                If CompareVersion(Release.VersionName, VersionBaseName) <= 0 Then
                    Logger.Info($"手动检查更新：已是最新版本（{VersionBaseName}）")
                    RunInUi(Sub() Hint($"当前版本 {VersionBaseName} 已是最新版本！", HintType.Green))
                    Return
                End If
                Logger.Info($"手动检查更新：发现新版本 {Release.VersionName}，当前 {VersionBaseName}")
                Dim Desc As String = ""
                If Not String.IsNullOrWhiteSpace(Release.Changelog) Then Desc = vbCrLf & vbCrLf & "更新内容：" & vbCrLf & Release.Changelog.Trim
                RunInUi(
                Sub()
                    If MyMsgBox(LinkFixTip(Release.VersionName) & $"检测到新版本 {Release.VersionName}，当前版本 {VersionBaseName}。" & vbCrLf & "是否立即下载并更新？" & Desc,
                                "发现新版本", "立即更新", "稍后") = 1 Then
                        DownloadUpdate(Release)
                    End If
                End Sub)
            Catch ex As Exception
                Logger.Warn(ex, "手动检查更新失败")
                RunInUi(Sub() MyMsgBox("检查更新失败：" & ex.Message & vbCrLf & $"可前往 https://github.com/{UpdateRepo}/releases 手动查看。", "检查更新", IsWarn:=True))
            End Try
        End Sub, "Update Checker Manual")
    End Sub

    Private Function LinkFixTip(VersionName As String) As String
        If VersionName Is Nothing OrElse Not VersionName.StartsWith("1.3.2", StringComparison.Ordinal) Then Return ""
        Return "该版本可能修复了联机功能（加入房间失败）。" & vbCrLf &
               "联机需要双方都把启动器更新到 1.3.2。" & vbCrLf & vbCrLf
    End Function

    Private Function FetchLatestRelease() As UpdateRelease
        Dim Raw As String = Nothing
        For Each Prefix As String In UpdatePrefixOrder()
            Try
                Raw = UpdateHttpGet(Prefix & UpdateApi)
                If Raw IsNot Nothing Then Exit For
            Catch ex As Exception
                Logger.Info($"检查更新：{If(Prefix, "直连")}失败：{ex.Message}")
            End Try
        Next
        If String.IsNullOrEmpty(Raw) Then Return Nothing
        Dim Json As Newtonsoft.Json.Linq.JObject
        Try
            Json = Newtonsoft.Json.Linq.JObject.Parse(Raw)
        Catch ex As Exception
            Return Nothing
        End Try
        Dim Tag As String = If(Json("tag_name")?.ToString, "").Trim.TrimStart("v"c, "V"c)
        If Tag = "" Then Return Nothing
        Dim Rel As New UpdateRelease With {.VersionName = Tag}
        Dim Body As String = If(Json("body")?.ToString, "")
        If Body.Length > 500 Then Body = Body.Substring(0, 500) & "……"
        Rel.Changelog = FilterUpdateChangelog(Body)

        Dim Assets = TryCast(Json("assets"), Newtonsoft.Json.Linq.JArray)
        If Assets IsNot Nothing Then
            Dim ExeAssets = Assets.Cast(Of Newtonsoft.Json.Linq.JObject)().
                Where(Function(a) If(a("name")?.ToString, "").ToLower.EndsWith(".exe")).ToList
            Dim Chosen = ExeAssets.FirstOrDefault(Function(a) Not IsInstallerName(a("name")?.ToString))
            If Chosen Is Nothing Then Chosen = ExeAssets.FirstOrDefault()
            If Chosen IsNot Nothing Then
                Rel.AssetName = Chosen("name")?.ToString
                Rel.AssetUrl = Chosen("browser_download_url")?.ToString
                Long.TryParse(Chosen("size")?.ToString, Rel.AssetSize)
                Rel.IsInstaller = IsInstallerName(Rel.AssetName)
            End If
        End If
        If String.IsNullOrEmpty(Rel.AssetUrl) Then Rel.AssetUrl = $"https://github.com/{UpdateRepo}/releases"
        Return Rel
    End Function

    Private Function IsInstallerName(Name As String) As Boolean
        If String.IsNullOrEmpty(Name) Then Return False
        Name = Name.ToLower
        Return Name.Contains("setup") OrElse Name.Contains("install")
    End Function

    Private Function FilterUpdateChangelog(Body As String) As String
        If String.IsNullOrWhiteSpace(Body) Then Return ""
        Dim Lines = Body.Replace(vbCrLf, vbLf).Split(ControlChars.Lf).
            Where(Function(l) Not String.IsNullOrWhiteSpace(l) AndAlso Not l.StartsWith("PCL PE（")).ToArray
        Return String.Join(vbCrLf, Lines.Take(8))
    End Function

    Private Sub DownloadUpdate(Release As UpdateRelease)

        If String.IsNullOrEmpty(Release.AssetName) Then
            If MyMsgBox($"发现新版本 {Release.VersionName}，但该版本没有提供可下载的程序文件。" & vbCrLf &
                        "是否打开 GitHub Releases 页面手动下载？", "发现新版本", "打开页面", "稍后") = 1 Then
                OpenWebsite($"https://github.com/{UpdateRepo}/releases")
            End If
            Return
        End If
        Dim SizeText As String = If(Release.AssetSize > 0, $"（约 {Math.Round(Release.AssetSize / 1048576.0, 1)} MB）", "")

        Dim ChosenPrefix As String = SelectUpdateMirror(SizeText)
        If ChosenPrefix Is Nothing Then Return
        RunInNewThread(
        Sub()
            Try
                UpdateProgressShow("正在下载更新")
                Dim Dir As String = IO.Path.Combine(PathTemp, "Update")
                IO.Directory.CreateDirectory(Dir)
                Dim Target As String = IO.Path.Combine(Dir, Release.AssetName)
                If Not UpdateDownloadFile(Release.AssetUrl, Target, ChosenPrefix) Then Throw New Exception("选定下载源与备用镜像均下载失败")
                UpdateProgressSet(100, "下载完成")

                Dim Info As New IO.FileInfo(Target)
                If Info.Length < 512 * 1024 Then Throw New Exception($"下载到的文件只有 {Info.Length} 字节，可能不是有效的更新包")
                Logger.Info($"更新包已下载：{Target}（{Math.Round(Info.Length / 1048576.0, 1)} MB）")
                RunInUi(
                Sub()
                    UpdateProgressClose()
                    If Release.IsInstaller Then
                        If MyMsgBox("更新包已下载完成，将启动安装程序。" & vbCrLf & "安装过程中请按提示操作，安装完成后启动器会退出。",
                                    "更新就绪", "启动安装程序", "稍后") = 1 Then
                            Diagnostics.Process.Start(Target)
                            Application.Current.Shutdown()
                        End If
                    Else
                        If MyMsgBox("更新包已下载完成，是否立即替换当前版本并重启？", "更新就绪", "立即重启更新", "稍后") = 1 Then
                            RunSelfUpdate(Target)
                        End If
                    End If
                End Sub)
            Catch ex As Exception
                UpdateProgressClose()
                Logger.Warn(ex, "下载更新包失败")
                RunInUi(Sub() MyMsgBox("更新包下载失败：" & ex.Message & vbCrLf & $"可前往 https://github.com/{UpdateRepo}/releases 手动下载。", "更新失败", IsWarn:=True))
            End Try
        End Sub, "Update Downloader")
    End Sub

    Private Sub RunSelfUpdate(NewExe As String)
        Try
            Dim CurExe As String = PathExe
            Dim RunnerDir As String = IO.Path.Combine(PathTemp, "Update")
            IO.Directory.CreateDirectory(RunnerDir)
            Dim Runner As String = IO.Path.Combine(RunnerDir, "PCLProUpdateRunner.exe")

            IO.File.Copy(CurExe, Runner, True)
            Dim Info As New Diagnostics.ProcessStartInfo(Runner, "--update") With {
                .CreateNoWindow = True,
                .UseShellExecute = False,
                .WorkingDirectory = RunnerDir}
            Info.EnvironmentVariables("PCL_UPDATE_OLD") = CurExe
            Info.EnvironmentVariables("PCL_UPDATE_NEW") = NewExe
            Info.EnvironmentVariables("PCL_UPDATE_PID") = Diagnostics.Process.GetCurrentProcess().Id.ToString()
            Info.EnvironmentVariables("PCL_UPDATE_SELF") = Runner
            Info.EnvironmentVariables("PCL_UPDATE_RESTART") = "1"
            Diagnostics.Process.Start(Info)
            Logger.Info($"已启动自更新替换进程：{Runner}")

            FormMain.EndProgramForce()
        Catch ex As Exception
            Logger.Warn(ex, "启动自更新脚本失败")
            MyMsgBox("自动替换失败：" & ex.Message & vbCrLf & $"请手动下载并覆盖：https://github.com/{UpdateRepo}/releases", "更新失败", IsWarn:=True)
        End Try
    End Sub

    Private Function UpdateHttpGet(Url As String) As String
        Dim Req As Net.HttpWebRequest = CType(Net.WebRequest.Create(Url), Net.HttpWebRequest)
        Req.UserAgent = "PCLPro-Updater"
        Req.Timeout = 8000
        Req.ReadWriteTimeout = 8000
        Req.AutomaticDecompression = Net.DecompressionMethods.GZip Or Net.DecompressionMethods.Deflate
        Using Resp As Net.HttpWebResponse = CType(Req.GetResponse(), Net.HttpWebResponse)
            Using Reader As New IO.StreamReader(Resp.GetResponseStream(), Text.Encoding.UTF8)
                Return Reader.ReadToEnd()
            End Using
        End Using
    End Function

    Private UpdatePrefixOrderCache As String() = Nothing

    Private UpdateCostCache As New Dictionary(Of String, Long)

    Friend Function UpdatePrefixOrder() As String()
        If UpdatePrefixOrderCache IsNot Nothing Then Return UpdatePrefixOrderCache
        Dim Prefixes As String() = {"", UpdateMirrors(0), UpdateMirrors(1), UpdateMirrors(2)}
        Dim Costs(Prefixes.Length - 1) As Long
        Dim Probes As New List(Of Threading.Thread)
        For i As Integer = 0 To Prefixes.Length - 1
            Dim Index As Integer = i
            Dim Probe As New Threading.Thread(Sub()
                                                  Costs(Index) = Long.MaxValue
                                                  Try
                                                      Dim Req As Net.HttpWebRequest = CType(Net.WebRequest.Create(Prefixes(Index) & "https://github.com/" & UpdateRepo), Net.HttpWebRequest)
                                                      Req.Method = "HEAD"
                                                      Req.UserAgent = "PCLPro-Updater"
                                                      Req.Timeout = 4000
                                                      Req.AllowAutoRedirect = True
                                                      Dim Start As Long = GetTimeMs()
                                                      Using Resp As Net.WebResponse = Req.GetResponse()
                                                          Costs(Index) = GetTimeMs() - Start
                                                      End Using
                                                  Catch ex As Exception
                                                      Logger.Info($"下载源测速失败（{If(String.IsNullOrEmpty(Prefixes(Index)), "直连", Prefixes(Index).TrimEnd("/"c))}）：{ex.Message}")
                                                  End Try
                                              End Sub)
            Probe.IsBackground = True
            Probes.Add(Probe)
            Probe.Start()
        Next
        For Each Probe As Threading.Thread In Probes
            Probe.Join()
        Next
        Dim Measured As New List(Of Tuple(Of Long, String))
        For i As Integer = 0 To Prefixes.Length - 1
            Measured.Add(Tuple.Create(Costs(i), Prefixes(i)))
        Next
        UpdateCostCache = Measured.ToDictionary(Function(t) t.Item2, Function(t) t.Item1)
        UpdatePrefixOrderCache = Measured.OrderBy(Function(t) t.Item1).Select(Function(t) t.Item2).ToArray()
        Logger.Info("下载源测速结果：" & String.Join(" → ", Measured.OrderBy(Function(t) t.Item1).
            Select(Function(t) If(String.IsNullOrEmpty(t.Item2), "直连", t.Item2.TrimEnd("/"c)) & "(" & If(t.Item1 = Long.MaxValue, "不可用", t.Item1 & "ms") & ")")))
        Return UpdatePrefixOrderCache
    End Function

    Private UpdateProgressWindow As Window = Nothing
    Private UpdateProgressBar As ProgressBar = Nothing
    Private UpdateProgressLabel As TextBlock = Nothing

    Friend Sub UpdateProgressShow(Title As String)
        RunInUi(Sub()
                    Try
                        If UpdateProgressWindow IsNot Nothing Then Return
                        Dim W As New Window With {
                            .Title = Title, .Width = 420, .Height = 150, .ResizeMode = ResizeMode.CanMinimize,
                            .WindowStyle = WindowStyle.SingleBorderWindow, .ShowInTaskbar = True,
                            .WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            .Background = Brushes.White}
                        If FrmMain IsNot Nothing AndAlso FrmMain.IsLoaded Then W.Owner = FrmMain
                        Dim Pan As New StackPanel With {.Margin = New Thickness(25, 24, 25, 20)}
                        UpdateProgressLabel = New TextBlock With {.Text = "正在准备下载…", .FontSize = 13, .TextTrimming = TextTrimming.CharacterEllipsis}
                        UpdateProgressBar = New ProgressBar With {.Height = 9, .Margin = New Thickness(0, 14, 0, 0), .Minimum = 0, .Maximum = 100}
                        Pan.Children.Add(UpdateProgressLabel)
                        Pan.Children.Add(UpdateProgressBar)
                        W.Content = Pan
                        UpdateProgressWindow = W
                        W.Show()
                    Catch ex As Exception
                        Logger.Warn(ex, "显示更新进度窗口失败")
                    End Try
                End Sub)
    End Sub

    Friend Sub UpdateProgressSet(Percent As Integer, Text As String)
        RunInUi(Sub()
                    Try
                        If UpdateProgressBar Is Nothing Then Return
                        If Percent >= 0 Then UpdateProgressBar.Value = Math.Min(100, Percent)
                        If Text IsNot Nothing Then UpdateProgressLabel.Text = Text
                    Catch
                    End Try
                End Sub)
    End Sub

    Friend Sub UpdateProgressClose()
        RunInUi(Sub()
                    Try
                        UpdateProgressWindow?.Close()
                    Catch
                    End Try
                    UpdateProgressWindow = Nothing
                    UpdateProgressBar = Nothing
                    UpdateProgressLabel = Nothing
                End Sub)
    End Sub

    Private Function SelectUpdateMirror(SizeText As String) As String
        Dim Ordered As String() = UpdatePrefixOrder()
        Dim Lines As New List(Of String)
        Dim Best As String = Nothing
        Dim BestCost As Long = Long.MaxValue
        For Each Prefix As String In Ordered
            Dim Cost As Long = Long.MaxValue
            If UpdateCostCache.TryGetValue(Prefix, Cost) AndAlso Cost < BestCost Then
                BestCost = Cost
                Best = Prefix
            End If
            Lines.Add("· " & UpdateMirrorName(Prefix) & "：" & If(Cost = Long.MaxValue, "无法连接", Cost & " ms"))
        Next
        Logger.Info("更新下载源测速：" & String.Join("，", Lines))
        If Best Is Nothing Then
            If MyMsgBox("所有下载源都无法连接。" & vbCrLf & "是否仍然尝试直接从 GitHub 下载？",
                        "选择下载源", "仍然尝试", "取消", IsWarn:=True) = 1 Then Return ""
            Return Nothing
        End If
        Dim Text As String = $"即将下载更新{SizeText}。" & vbCrLf & vbCrLf &
                             "各下载源测速结果（数字越小越快）：" & vbCrLf &
                             String.Join(vbCrLf, Lines) & vbCrLf & vbCrLf &
                             $"推荐使用「{UpdateMirrorName(Best)}」。"
        Select Case MyMsgBox(Text, "选择下载源", $"使用{UpdateMirrorName(Best)}", "使用直连", "取消")
            Case 1
                Return Best
            Case 2
                Return ""
            Case Else
                Return Nothing
        End Select
    End Function

    Private Function UpdateMirrorName(Prefix As String) As String
        If String.IsNullOrEmpty(Prefix) Then Return "直连"
        Return Prefix.Replace("https://", "").Replace("http://", "").TrimEnd("/"c)
    End Function

    Friend Function UpdateDownloadFile(Url As String, Target As String, Optional PreferredPrefix As String = Nothing) As Boolean
        Dim Prefixes As String() = UpdatePrefixOrder()
        If PreferredPrefix IsNot Nothing Then

            Dim Rest As String() = Prefixes.Where(Function(p) p <> PreferredPrefix).ToArray()
            Prefixes = New String() {PreferredPrefix}.Concat(Rest).ToArray()
        End If
        For Each Prefix As String In Prefixes
            Dim TempFile As String = Target & ".part"
            Dim SourceText As String = If(String.IsNullOrEmpty(Prefix), "直连", Prefix.TrimEnd("/"c))
            Try
                Dim Req As Net.HttpWebRequest = CType(Net.WebRequest.Create(Prefix & Url), Net.HttpWebRequest)
                Req.UserAgent = "PCLPro-Updater"
                Req.Timeout = 20000
                Req.ReadWriteTimeout = 60000
                Req.AllowAutoRedirect = True
                Using Resp As Net.HttpWebResponse = CType(Req.GetResponse(), Net.HttpWebResponse)
                    Dim Total As Long = Resp.ContentLength
                    Dim Done As Long = 0
                    UpdateProgressSet(0, $"正在从 {SourceText} 下载…")
                    Using Input As IO.Stream = Resp.GetResponseStream(), Output As New IO.FileStream(TempFile, IO.FileMode.Create, IO.FileAccess.Write)
                        Dim Buffer(8191) As Byte
                        Do
                            Dim Count As Integer = Input.Read(Buffer, 0, Buffer.Length)
                            If Count <= 0 Then Exit Do
                            Output.Write(Buffer, 0, Count)
                            Done += Count
                            If Total > 0 Then
                                UpdateProgressSet(CInt(Math.Floor(Done * 100.0 / Total)),
                                                  $"正在从 {SourceText} 下载… {Math.Round(Done / 1048576.0, 1)} / {Math.Round(Total / 1048576.0, 1)} MB")
                            Else
                                UpdateProgressSet(-1, $"正在从 {SourceText} 下载… 已下载 {Math.Round(Done / 1048576.0, 1)} MB")
                            End If
                        Loop
                    End Using
                End Using
                If IO.File.Exists(Target) Then IO.File.Delete(Target)
                IO.File.Move(TempFile, Target)
                Logger.Info($"更新包下载成功：{If(String.IsNullOrEmpty(Prefix), "直连", "镜像 " & Prefix)}")
                Return True
            Catch ex As Exception
                Logger.Info($"更新包下载失败（{If(String.IsNullOrEmpty(Prefix), "直连", "镜像 " & Prefix)}）：{ex.Message}")
                Try
                    If IO.File.Exists(TempFile) Then IO.File.Delete(TempFile)
                Catch
                End Try
            End Try
        Next
        Return False
    End Function

    Private Const TerracottaAssetName As String = "Terracotta_Module.zip"

    Public Function DownloadTerracottaModule(TargetFolder As String) As Boolean

        Dim Raw As String = Nothing
        For Each Prefix As String In UpdatePrefixOrder()
            Try
                Raw = UpdateHttpGet(Prefix & UpdateApi)
                If Raw IsNot Nothing Then Exit For
            Catch ex As Exception
                Logger.Info($"获取联机模块信息失败（{If(String.IsNullOrEmpty(Prefix), "直连", "镜像 " & Prefix)}）：{ex.Message}")
            End Try
        Next
        If String.IsNullOrEmpty(Raw) Then Return False
        Dim Url As String = Nothing
        Try
            Dim Assets = TryCast(Newtonsoft.Json.Linq.JObject.Parse(Raw)("assets"), Newtonsoft.Json.Linq.JArray)
            If Assets IsNot Nothing Then
                Dim Found = Assets.Cast(Of Newtonsoft.Json.Linq.JObject)().
                    FirstOrDefault(Function(a) If(a("name")?.ToString, "") = TerracottaAssetName)
                If Found IsNot Nothing Then Url = Found("browser_download_url")?.ToString
            End If
        Catch ex As Exception
            Logger.Warn(ex, "解析联机模块资产失败")
        End Try
        If String.IsNullOrEmpty(Url) Then
            Logger.Warn($"最新 Release 中未找到联机模块资产（{TerracottaAssetName}）")
            Return False
        End If

        Dim ZipPath As String = PathTemp & "Terracotta_Module.zip"
        If Not UpdateDownloadFile(Url, ZipPath) Then Return False

        Try
            DirectoryUtils.Create(TargetFolder)
            Using Zip As IO.Compression.ZipArchive = IO.Compression.ZipFile.OpenRead(ZipPath)
                For Each Entry As IO.Compression.ZipArchiveEntry In Zip.Entries
                    If String.IsNullOrEmpty(Entry.Name) Then Continue For
                    Dim Dest As String = IO.Path.Combine(TargetFolder, Entry.Name)
                    Entry.ExtractToFile(Dest, True)
                Next
            End Using
            Logger.Info($"陶瓦联机模块已下载并解压：{TargetFolder}")
            Return True
        Catch ex As Exception
            Logger.Warn(ex, "解压陶瓦联机模块失败")
            Return False
        Finally
            Try
                If IO.File.Exists(ZipPath) Then IO.File.Delete(ZipPath)
            Catch
            End Try
        End Try
    End Function

    Private Function CompareVersion(NewVer As String, OldVer As String) As Integer
        Dim A As Integer() = ParseVersionParts(NewVer)
        Dim B As Integer() = ParseVersionParts(OldVer)
        For i As Integer = 0 To 2
            If A(i) <> B(i) Then Return If(A(i) > B(i), 1, -1)
        Next
        Return 0
    End Function
    Private Function ParseVersionParts(Ver As String) As Integer()
        Dim Parts As New List(Of Integer)
        For Each Seg As String In Ver.Split("."c)
            Dim Num As Integer = 0
            Integer.TryParse(New String(Seg.Where(Function(c) Char.IsDigit(c)).ToArray()), Num)
            Parts.Add(Num)
        Next
        Do While Parts.Count < 3
            Parts.Add(0)
        Loop
        Return New Integer() {Parts(0), Parts(1), Parts(2)}
    End Function

#End Region

End Module
