Imports System.Windows.Threading

Public Interface IRefreshable
    Sub Refresh()
End Interface

Public Interface IDispatcherUnhandledException
    Sub DispatcherUnhandledException(sender As Object, e As DispatcherUnhandledExceptionEventArgs)
End Interface

Public Interface IMyRadio
    Event Check(sender As Object, e As RouteEventArgs)
    Event Changed(sender As Object, e As RouteEventArgs)
End Interface
