Public Class SettingService

#Region "属性"

    Public Shared Function GetKey(obj As DependencyObject) As String
        If obj Is Nothing Then Return Nothing
        Return obj.GetValue(KeyProperty)
    End Function
    Public Shared Sub SetKey(obj As DependencyObject, value As String)
        obj.SetValue(KeyProperty, value)
    End Sub
    Public Shared ReadOnly KeyProperty As DependencyProperty =
        DependencyProperty.RegisterAttached("Key", GetType(String), GetType(SettingService), New PropertyMetadata(Nothing))

    Public Shared Function GetValue(obj As DependencyObject) As String
        If obj Is Nothing Then Return Nothing
        Return obj.GetValue(ValueProperty)
    End Function
    Public Shared Sub SetValue(obj As DependencyObject, value As String)
        obj.SetValue(ValueProperty, value)
    End Sub
    Public Shared ReadOnly ValueProperty As DependencyProperty =
        DependencyProperty.RegisterAttached("Value", GetType(String), GetType(SettingService), New PropertyMetadata(Nothing))

#End Region

    Public Shared Sub ResetSettings(Target As DependencyObject)
        LogicalTreeHelper.GetChildren(Target).OfType(Of DependencyObject)().ForAll(Sub(c) ResetSettings(c))
        Dim Key = GetKey(Target)
        If Key Is Nothing Then Return
        Settings.Reset(Key, Instance:=PageInstanceLeft.Instance)
    End Sub

    Public Shared Sub RefreshSettings(Target As DependencyObject)
        LogicalTreeHelper.GetChildren(Target).OfType(Of DependencyObject)().ForAll(Sub(c) RefreshSettings(c))
        Dim Key = GetKey(Target)
        If Key Is Nothing Then Return
        Dim NewValue = Settings.Get(Key, Instance:=PageInstanceLeft.Instance)
        If TypeOf Target Is ISettingControl Then DirectCast(Target, ISettingControl).RefreshSetting(NewValue)
    End Sub

    Public Shared Sub SaveSetting(sender As Object)
        If AniControlEnabled <> 0 Then Return
        If TypeOf sender IsNot ISettingControl Then Return
        Dim Key = GetKey(sender)
        If Key Is Nothing Then Return
        Dim NewValue As String = DirectCast(sender, ISettingControl).GetCurrentSetting()
        If NewValue Is Nothing Then Return
        Settings.Set(Key, NewValue, Instance:=PageInstanceLeft.Instance)
    End Sub

End Class

Public Interface ISettingControl

    Sub RefreshSetting(NewValue As String)

    Function GetCurrentSetting() As String

End Interface
