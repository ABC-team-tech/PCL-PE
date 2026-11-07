Public Class LazyLoadBehavior

    Public Shared Sub OnFirstEnterScrollViewerViewport(obj As DependencyObject, value As Action)
        obj.SetValue(IsInViewportProperty, value)
    End Sub

    Private Shared ReadOnly IsInViewportProperty As DependencyProperty =
        DependencyProperty.RegisterAttached("IsInViewport", GetType(Action), GetType(LazyLoadBehavior),
        New PropertyMetadata(Nothing, AddressOf OnIsInViewportChanged))

    Private Shared Sub OnIsInViewportChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
        Dim Element = TryCast(d, FrameworkElement)
        If Element Is Nothing OrElse e.NewValue Is Nothing Then Return

        Dim Handled As Boolean = False
        Dim Handler As EventHandler =
        Sub()
            If Handled Then Return

            If Element.RenderSize.Width < Double.Epsilon Then Return
            If Not Element.IsVisible Then Return

            Dim Scroll = FindParentScrollViewer(Element)
            If Scroll Is Nothing Then Return
            If Not New Rect(0, 0, Scroll.ViewportWidth, Scroll.ViewportHeight).IntersectsWith(
               Element.TransformToAncestor(Scroll).TransformBounds(New Rect(New Point(0, 0), Element.RenderSize))) Then Return

            Handled = True
            CType(e.NewValue, Action).Invoke()
            RemoveHandler Element.LayoutUpdated, Handler
        End Sub
        AddHandler Element.LayoutUpdated, Handler
    End Sub

    Private Shared Function FindParentScrollViewer(d As DependencyObject) As ScrollViewer
        While d IsNot Nothing
            If TypeOf d Is ScrollViewer Then Return CType(d, ScrollViewer)
            d = VisualTreeHelper.GetParent(d)
        End While
        Return Nothing
    End Function

End Class
