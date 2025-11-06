' HScrollRichTextBox.vb

Public Class HScrollRichTextBox
    Inherits RichTextBox

    Public Event HScrolled As EventHandler
    Private Const WM_HSCROLL As Integer = &H114

    Protected Overrides Sub WndProc(ByRef m As Message)
        MyBase.WndProc(m)
        If m.Msg = WM_HSCROLL Then
            RaiseEvent HScrolled(Me, EventArgs.Empty)
        End If
    End Sub
End Class
