Option Strict On
Option Explicit On

Imports System

Namespace My.Resources

    <Global.System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0"),  _
     Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
     Global.System.Runtime.CompilerServices.CompilerGeneratedAttribute(),  _
     Global.Microsoft.VisualBasic.HideModuleNameAttribute()>  _
    Friend Module Resources

        Private resourceMan As Global.System.Resources.ResourceManager

        Private resourceCulture As Global.System.Globalization.CultureInfo

        <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Advanced)>  _
        Friend ReadOnly Property ResourceManager() As Global.System.Resources.ResourceManager
            Get
                If Object.ReferenceEquals(resourceMan, Nothing) Then
                    Dim temp As Global.System.Resources.ResourceManager = New Global.System.Resources.ResourceManager("PCL.Resources", GetType(Resources).Assembly)
                    resourceMan = temp
                End If
                Return resourceMan
            End Get
        End Property

        <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Advanced)>  _
        Friend Property Culture() As Global.System.Globalization.CultureInfo
            Get
                Return resourceCulture
            End Get
            Set
                resourceCulture = value
            End Set
        End Property

        Friend ReadOnly Property CacheCow_Client() As Byte()
            Get
                Dim obj As Object = ResourceManager.GetObject("CacheCow.Client", resourceCulture)
                Return CType(obj,Byte())
            End Get
        End Property

        Friend ReadOnly Property CacheCow_Client_FileStore() As Byte()
            Get
                Dim obj As Object = ResourceManager.GetObject("CacheCow.Client.FileStore", resourceCulture)
                Return CType(obj,Byte())
            End Get
        End Property

        Friend ReadOnly Property CacheCow_Common() As Byte()
            Get
                Dim obj As Object = ResourceManager.GetObject("CacheCow.Common", resourceCulture)
                Return CType(obj,Byte())
            End Get
        End Property

        Friend ReadOnly Property ThrottleDebounce() As Byte()
            Get
                Dim obj As Object = ResourceManager.GetObject("ThrottleDebounce", resourceCulture)
                Return CType(obj,Byte())
            End Get
        End Property

        Friend ReadOnly Property Custom() As Byte()
            Get
                Dim obj As Object = ResourceManager.GetObject("Custom", resourceCulture)
                Return CType(obj,Byte())
            End Get
        End Property

        Friend ReadOnly Property ForgeInstaller() As Byte()
            Get
                Dim obj As Object = ResourceManager.GetObject("ForgeInstaller", resourceCulture)
                Return CType(obj,Byte())
            End Get
        End Property

        Friend ReadOnly Property Help() As Byte()
            Get
                Dim obj As Object = ResourceManager.GetObject("Help", resourceCulture)
                Return CType(obj,Byte())
            End Get
        End Property

        Friend ReadOnly Property Imazen_WebP() As Byte()
            Get
                Dim obj As Object = ResourceManager.GetObject("Imazen.WebP", resourceCulture)
                Return CType(obj,Byte())
            End Get
        End Property

        Friend ReadOnly Property libwebp64() As Byte()
            Get
                Dim obj As Object = ResourceManager.GetObject("libwebp64", resourceCulture)
                Return CType(obj, Byte())
            End Get
        End Property

        Friend ReadOnly Property MeloongCore() As Byte()
            Get
                Dim obj As Object = ResourceManager.GetObject("MeloongCore", resourceCulture)
                Return CType(obj, Byte())
            End Get
        End Property

        Friend ReadOnly Property MeloongCore_Wpf() As Byte()
            Get
                Dim obj As Object = ResourceManager.GetObject("MeloongCore.Wpf", resourceCulture)
                Return CType(obj,Byte())
            End Get
        End Property

        Friend ReadOnly Property NAudio() As Byte()
            Get
                Dim obj As Object = ResourceManager.GetObject("NAudio", resourceCulture)
                Return CType(obj,Byte())
            End Get
        End Property

        Friend ReadOnly Property Newtonsoft_Json() As Byte()
            Get
                Dim obj As Object = ResourceManager.GetObject("Newtonsoft.Json", resourceCulture)
                Return CType(obj,Byte())
            End Get
        End Property

        Friend ReadOnly Property Ookii_Dialogs_Wpf() As Byte()
            Get
                Dim obj As Object = ResourceManager.GetObject("Ookii.Dialogs.Wpf", resourceCulture)
                Return CType(obj,Byte())
            End Get
        End Property

        Friend ReadOnly Property PCLCS() As Byte()
            Get
                Dim obj As Object = ResourceManager.GetObject("PCLCS", resourceCulture)
                Return CType(obj,Byte())
            End Get
        End Property

        Friend ReadOnly Property System_Net_Http_Formatting() As Byte()
            Get
                Dim obj As Object = ResourceManager.GetObject("System.Net.Http.Formatting", resourceCulture)
                Return CType(obj,Byte())
            End Get
        End Property

        Friend ReadOnly Property Transformer() As Byte()
            Get
                Dim obj As Object = ResourceManager.GetObject("Transformer", resourceCulture)
                Return CType(obj,Byte())
            End Get
        End Property
    End Module
End Namespace
