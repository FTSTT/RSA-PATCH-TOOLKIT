Imports System.ComponentModel

Public Module UiSafe
    ' 通用：在创建控件的 UI 线程上执行 reader，并返回结果
    <EditorBrowsable(EditorBrowsableState.Never)>
    Private Function UIRead(Of T)(ctrl As Control, reader As Func(Of T), Optional fallback As T = Nothing) As T
        If ctrl Is Nothing Then Return fallback
        If ctrl.IsDisposed Then Return fallback
        Try
            ' 有些控件在句柄未创建但也不需要句柄的属性读取，这里允许直接读
            If ctrl.InvokeRequired AndAlso ctrl.IsHandleCreated Then
                Return CType(ctrl.Invoke(reader), T)
            ElseIf ctrl.InvokeRequired AndAlso Not ctrl.IsHandleCreated Then
                ' 句柄未创建且跨线程：无法安全调用，给回退值
                Return fallback
            Else
                Return reader()
            End If
        Catch ex As ObjectDisposedException
            Return fallback
        End Try
    End Function

    ' 线程安全读取控件值的统一入口
    Public Function GetControlValue(ctrl As Control) As Object
        Return UIRead(Of Object)(ctrl, Function() ReadValueCore(ctrl))
    End Function

    ' 在 UI 线程上执行的实际读取逻辑
    Private Function ReadValueCore(ctrl As Control) As Object
        ' 1) 文本类：TextBox / RichTextBox / MaskedTextBox / Label / LinkLabel / Button / CheckBox.Text 等
        If TypeOf ctrl Is TextBoxBase Then
            Return DirectCast(ctrl, TextBoxBase).Text
        End If
        If TypeOf ctrl Is MaskedTextBox Then
            Return DirectCast(ctrl, MaskedTextBox).Text
        End If
        If TypeOf ctrl Is Label Then
            Return DirectCast(ctrl, Label).Text
        End If
        If TypeOf ctrl Is LinkLabel Then
            Return DirectCast(ctrl, LinkLabel).Text
        End If
        If TypeOf ctrl Is Button Then
            Return DirectCast(ctrl, Button).Text
        End If

        ' 2) 勾选类
        If TypeOf ctrl Is CheckBox Then
            Return DirectCast(ctrl, CheckBox).Checked ' Boolean
        End If
        If TypeOf ctrl Is RadioButton Then
            Return DirectCast(ctrl, RadioButton).Checked ' Boolean
        End If
        If TypeOf ctrl Is CheckBox OrElse TypeOf ctrl Is RadioButton Then
            ' 已覆盖，上面返回 Boolean
        End If

        ' 3) 列表类：优先 SelectedValue（绑定时），否则 Text
        If TypeOf ctrl Is ComboBox Then
            Dim cb = DirectCast(ctrl, ComboBox)
            If Not String.IsNullOrEmpty(cb.ValueMember) AndAlso cb.DataSource IsNot Nothing Then
                Return cb.SelectedValue ' 可能为 Nothing
            End If
            If cb.SelectedItem IsNot Nothing Then
                Return cb.SelectedItem
            End If
            Return cb.Text ' 允许 DropDown 风格返回当前文本
        End If

        If TypeOf ctrl Is ListBox Then
            Dim lb = DirectCast(ctrl, ListBox)
            If Not String.IsNullOrEmpty(lb.ValueMember) AndAlso lb.DataSource IsNot Nothing Then
                Return lb.SelectedValue
            End If
            If lb.SelectionMode = SelectionMode.MultiExtended OrElse lb.SelectionMode = SelectionMode.MultiSimple Then
                ' 多选：返回选中项集合（可按需改为 SelectedIndices）
                Dim arr As New List(Of Object)
                For Each item In lb.SelectedItems
                    arr.Add(item)
                Next
                Return arr.ToArray()
            Else
                Return lb.SelectedItem
            End If
        End If

        ' 4) 数值/日期类
        If TypeOf ctrl Is NumericUpDown Then
            Return DirectCast(ctrl, NumericUpDown).Value ' Decimal
        End If
        If TypeOf ctrl Is TrackBar Then
            Return DirectCast(ctrl, TrackBar).Value ' Integer
        End If
        If TypeOf ctrl Is ProgressBar Then
            Return DirectCast(ctrl, ProgressBar).Value ' Integer
        End If
        If TypeOf ctrl Is DateTimePicker Then
            Return DirectCast(ctrl, DateTimePicker).Value ' Date
        End If

        ' 5) 通用回退：若控件有 Text 属性则返回 Text，否则 Nothing
        Dim prop = ctrl.GetType().GetProperty("Text")
        If prop IsNot Nothing Then
            Return prop.GetValue(ctrl, Nothing)
        End If

        Return Nothing
    End Function

    Public Sub RefreshSafe(ctrl As Control)
        If ctrl Is Nothing OrElse ctrl.IsDisposed Then Return

        If ctrl.InvokeRequired Then
            If ctrl.IsHandleCreated Then
                ctrl.BeginInvoke(New Action(Of Control)(AddressOf RefreshSafe), ctrl)
            Else
                ' ★ 关键修改：为 HandleCreated 使用带参数的 Lambda（sender As Object, e As EventArgs）
                AddHandler ctrl.HandleCreated,
                Sub(sender As Object, e As EventArgs)
                    ctrl.BeginInvoke(New Action(Of Control)(AddressOf RefreshSafe), ctrl)
                End Sub
            End If
        Else
            ctrl.Refresh()  ' 等同 Invalidate()+Update() 的同步重绘
        End If
    End Sub

    Public Sub SetLabelTextSafe(lbl As Label, s As String)
        If lbl Is Nothing OrElse lbl.IsDisposed Then Return

        If lbl.InvokeRequired Then
            ' 用 BeginInvoke 防止阻塞后台线程
            lbl.BeginInvoke(New Action(Of String)(Sub(txt) SetLabelTextSafe(lbl, txt)), s)
        Else
            lbl.Text = s
        End If
    End Sub
End Module
