Imports System.Text
Imports System.Text.Json
Imports System.Windows.Forms

Public Module Loc
    Private _str As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
    Private _controls As New Dictionary(Of String, Dictionary(Of String, String))(StringComparer.OrdinalIgnoreCase)

    Public Function T(key As String, [default] As String, ParamArray args() As Object) As String
        Dim s As String = Nothing
        If key IsNot Nothing AndAlso _str.TryGetValue(key, s) Then
            Try
                If args IsNot Nothing AndAlso args.Length > 0 Then
                    Return String.Format(s, args)
                End If
                Return s
            Catch
            End Try
        End If
        If args IsNot Nothing AndAlso args.Length > 0 Then
            Return String.Format([default], args)
        End If
        Return [default]
    End Function

    Public Sub LoadJson(jsonText As String)
        _str.Clear() : _controls.Clear()
        Dim doc = JsonDocument.Parse(jsonText)
        Dim root = doc.RootElement
        If root.TryGetProperty("strings", Nothing) Then
            For Each p In root.GetProperty("strings").EnumerateObject()
                _str(p.Name) = p.Value.GetString()
            Next
        Else
            For Each p In root.EnumerateObject()
                _str(p.Name) = p.Value.GetString()
            Next
        End If
        If root.TryGetProperty("controls", Nothing) Then
            For Each f In root.GetProperty("controls").EnumerateObject()
                Dim map As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
                For Each kv In f.Value.EnumerateObject()
                    map(kv.Name) = kv.Value.GetString()
                Next
                _controls(f.Name) = map
            Next
        End If
    End Sub

    Public Sub LoadJsonFile(path As String, Optional encoding As Encoding = Nothing)
        If encoding Is Nothing Then encoding = New UTF8Encoding(False)
        Dim txt = System.IO.File.ReadAllText(path, encoding)
        LoadJson(txt)
    End Sub

    Private Sub ApplyPlaceholderText(root As Control, map As Dictionary(Of String, String))
        ' TextBox / ComboBox（部分版本不支持）/ ToolStripTextBox
        ' 逐控件查找键：<Name>.PlaceholderText
        ' 若存在则读取并应用翻译，默认值用控件当前 PlaceholderText（如可用）或空串

        ' 1) 常规控件（TextBox、RichTextBox 等）
        Dim stack As New Stack(Of Control)()
        stack.Push(root)
        While stack.Count > 0
            Dim c = stack.Pop()
            Dim k As String = Nothing
            If map.TryGetValue(c.Name & ".PlaceholderText", k) Then
                Try
                    ' 只有支持该属性的控件才会成功设置
                    Dim prop = c.GetType().GetProperty("PlaceholderText")
                    If prop IsNot Nothing AndAlso prop.CanWrite Then
                        Dim cur As String = ""
                        Try
                            Dim curVal = prop.GetValue(c, Nothing)
                            If curVal IsNot Nothing Then cur = CStr(curVal)
                        Catch
                        End Try
                        prop.SetValue(c, T(k, cur), Nothing)
                    End If
                Catch
                    ' 忽略不支持的控件
                End Try
            End If
            For Each ch As Control In c.Controls
                stack.Push(ch)
            Next
        End While

        ' 2) ToolStripTextBox（在 ToolStrip 内）
        Dim strips = root.FindForm().Controls.OfType(Of ToolStrip)()
        For Each ts In strips
            For Each item As ToolStripItem In ts.Items
                Dim tsti = TryCast(item, ToolStripTextBox)
                If tsti Is Nothing Then Continue For
                Dim k As String = Nothing
                If map.TryGetValue(tsti.Name & ".PlaceholderText", k) Then
                    Try
                        ' ToolStripTextBox 控件内部是 TextBox 控件
                        Dim inner = tsti.Control
                        If inner IsNot Nothing Then
                            Dim prop = inner.GetType().GetProperty("PlaceholderText")
                            If prop IsNot Nothing AndAlso prop.CanWrite Then
                                Dim cur As String = ""
                                Try
                                    Dim curVal = prop.GetValue(inner, Nothing)
                                    If curVal IsNot Nothing Then cur = CStr(curVal)
                                Catch
                                End Try
                                prop.SetValue(inner, T(k, cur), Nothing)
                            End If
                        End If
                    Catch
                    End Try
                End If
            Next
        Next
    End Sub

    ' === 新增：重建 ComboBox.Items ===
    Private Sub ApplyComboBoxItems(root As Control, map As Dictionary(Of String, String))
        For Each cb As ComboBox In GetAllControls(Of ComboBox)(root)
            Dim rebuilt As Boolean = False
            Dim newItems As New List(Of String)
            Dim idx As Integer = 0
            While True
                Dim k As String = Nothing
                If Not map.TryGetValue(cb.Name & ".Items[" & idx.ToString() & "]", k) Then
                    Exit While
                End If
                newItems.Add(T(k, If(idx < cb.Items.Count, CStr(cb.Items(idx)), "")))
                rebuilt = True
                idx += 1
            End While
            If rebuilt Then
                Dim sel = cb.SelectedIndex
                cb.BeginUpdate()
                cb.Items.Clear()
                For Each s In newItems : cb.Items.Add(s) : Next
                cb.EndUpdate()
                ' 保留选择索引（越界则取消选择）
                If sel >= 0 AndAlso sel < cb.Items.Count Then cb.SelectedIndex = sel Else cb.SelectedIndex = -1
            End If
        Next
    End Sub

    ' === 新增：重建 ListBox.Items（同样的键命名）===
    Private Sub ApplyListBoxItems(root As Control, map As Dictionary(Of String, String))
        For Each lb As ListBox In GetAllControls(Of ListBox)(root)
            Dim rebuilt As Boolean = False
            Dim newItems As New List(Of String)
            Dim idx As Integer = 0
            While True
                Dim k As String = Nothing
                If Not map.TryGetValue(lb.Name & ".Items[" & idx.ToString() & "]", k) Then
                    Exit While
                End If
                newItems.Add(T(k, If(idx < lb.Items.Count, CStr(lb.Items(idx)), "")))
                rebuilt = True
                idx += 1
            End While
            If rebuilt Then
                Dim sel = lb.SelectedIndex
                lb.BeginUpdate()
                lb.Items.Clear()
                For Each s In newItems : lb.Items.Add(s) : Next
                lb.EndUpdate()
                If sel >= 0 AndAlso sel < lb.Items.Count Then lb.SelectedIndex = sel Else lb.SelectedIndex = -1
            End If
        Next
    End Sub

    Private Sub ApplyControlRecursive(c As Control, formName As String, map As Dictionary(Of String, String))
        For Each child As Control In c.Controls
            Dim k As String = Nothing
            If map.TryGetValue(child.Name & ".Text", k) Then
                child.Text = T(k, child.Text)
            End If
            ApplyControlRecursive(child, formName, map)
        Next
    End Sub

    Private Sub ApplyToolStrips(root As Control, formName As String, map As Dictionary(Of String, String))
        Dim strips = root.FindForm().Controls.OfType(Of ToolStrip)()
        For Each ts In strips
            For Each item As ToolStripItem In ts.Items
                Dim k As String = Nothing
                If map.TryGetValue(item.Name & ".Text", k) Then
                    item.Text = T(k, item.Text)
                End If
                Dim mi = TryCast(item, ToolStripMenuItem)
                If mi IsNot Nothing AndAlso mi.HasDropDownItems Then
                    For Each di As ToolStripItem In mi.DropDownItems
                        Dim kd As String = Nothing
                        If map.TryGetValue(di.Name & ".Text", kd) Then
                            di.Text = T(kd, di.Text)
                        End If
                    Next
                End If
            Next
        Next
    End Sub

    Private Sub ApplyDataGridViews(root As Control, formName As String, map As Dictionary(Of String, String))
        Dim grids = root.FindForm().Controls.OfType(Of DataGridView)()
        For Each g In grids
            For Each col As DataGridViewColumn In g.Columns
                Dim k As String = Nothing
                If map.TryGetValue("grid.Columns." & col.Name & ".HeaderText", k) Then
                    col.HeaderText = T(k, col.HeaderText)
                ElseIf map.TryGetValue(col.Name & ".HeaderText", k) Then
                    col.HeaderText = T(k, col.HeaderText)
                End If
            Next
        Next
    End Sub

    ' 新增：完整快照
    Private Class Snapshot
        Public Text As String
        Public Placeholder As String
    End Class

    ' 常规控件文本/占位符：控件 -> 快照
    Private _snap As New Dictionary(Of Control, Snapshot)
    ' DataGridView 列头：列 -> HeaderText
    Private _gridHeader As New Dictionary(Of DataGridViewColumn, String)
    ' ToolStrip / MenuStrip / StatusStrip 项文本：项 -> Text（递归包含所有层级）
    Private _tsText As New Dictionary(Of ToolStripItem, String)
    ' ComboBox / ListBox 的 Items 快照
    Private _comboItems As New Dictionary(Of ComboBox, List(Of String))
    Private _listBoxItems As New Dictionary(Of ListBox, List(Of String))

    Public Property IsEnabled As Boolean = False

    ' 清空已加载的语言数据（不应用，还原由 DisableAndRestore 完成）
    Public Sub ClearLanguage()
        _str.Clear()
        _controls.Clear()
        IsEnabled = False
    End Sub

    ' ☆ 新增：确保为当前控件树“补齐快照”（只为未采集过的控件/列/项采集）
    Public Sub EnsureCaptured(root As Control)
        If root Is Nothing Then Return
        CaptureSnapshots(root)      ' 控件 Text / Placeholder
        CaptureToolStrips(root)     ' 所有层级 ToolStripItem.Text
        CaptureGridHeaders(root)    ' DataGridView 列头
        CaptureComboAndListItems(root) ' ComboBox/ListBox.Items
    End Sub

    ' 还原整个控件树的默认值（不依赖 _controls）
    Public Sub RestoreDefaults(root As Control)
        If root Is Nothing Then Return

        ' ☆ 如果还没采快照，先采（防止空快照导致只还原一部分）
        EnsureCaptured(root)

        ' 1) 控件 Text / PlaceholderText
        Dim stack As New Stack(Of Control)
        stack.Push(root)
        While stack.Count > 0
            Dim c = stack.Pop()
            Dim snap As Snapshot = Nothing
            If _snap.TryGetValue(c, snap) Then
                Try : c.Text = snap.Text : Catch : End Try
                Try
                    Dim prop = c.GetType().GetProperty("PlaceholderText")
                    If prop IsNot Nothing AndAlso prop.CanWrite Then
                        prop.SetValue(c, snap.Placeholder, Nothing)
                    End If
                Catch
                End Try
            End If
            For Each ch As Control In c.Controls
                stack.Push(ch)
            Next
        End While

        ' 2) ToolStrip / 所有层级菜单项
        For Each kv In _tsText
            Try : kv.Key.Text = kv.Value : Catch : End Try
        Next

        ' 3) DataGridView 列头
        For Each kv In _gridHeader
            Try : kv.Key.HeaderText = kv.Value : Catch : End Try
        Next

        ' 4) ComboBox / ListBox Items（保留选择索引）
        For Each kv In _comboItems
            Dim cb = kv.Key : Dim items = kv.Value
            Try
                Dim sel = cb.SelectedIndex
                cb.BeginUpdate()
                cb.Items.Clear()
                For Each s In items : cb.Items.Add(s) : Next
                cb.EndUpdate()
                If sel >= 0 AndAlso sel < cb.Items.Count Then cb.SelectedIndex = sel Else cb.SelectedIndex = -1
            Catch
            End Try
        Next
        For Each kv In _listBoxItems
            Dim lb = kv.Key : Dim items = kv.Value
            Try
                Dim sel = lb.SelectedIndex
                lb.BeginUpdate()
                lb.Items.Clear()
                For Each s In items : lb.Items.Add(s) : Next
                lb.EndUpdate()
                If sel >= 0 AndAlso sel < lb.Items.Count Then lb.SelectedIndex = sel Else lb.SelectedIndex = -1
            Catch
            End Try
        Next
    End Sub


    ' 禁用语言并还原默认
    Public Sub DisableAndRestore(root As Control)
        EnsureCaptured(root)        ' ☆ 先补齐快照（即使之前没应用过语言）
        RestoreDefaults(root)       ' 再按快照还原
        ClearLanguage()
    End Sub

    ' —— 在 ApplyTo 中，首次应用时采集默认值（添加/修改以下片段）——

    Public Sub ApplyTo(root As Control)
        If root Is Nothing Then Return
        IsEnabled = True

        ' ☆ 应用前，先确保快照齐全（只采一次）
        EnsureCaptured(root)

        Dim form = root.FindForm()
        Dim formName As String = form.Name
        Dim map As Dictionary(Of String, String) = Nothing
        If Not _controls.TryGetValue(formName, map) Then
            Return
        End If

        ' 原有应用：Form.Text / 控件树 / ToolStrip / DGV / Placeholder / ComboBox Items / ListBox Items ...
        Dim key As String = Nothing
        If map.TryGetValue("$this.Text", key) Then
            form.Text = T(key, form.Text)
        End If

        ApplyControlRecursive(root, formName, map)
        ApplyToolStrips(root, formName, map)
        ApplyDataGridViews(root, formName, map)

        ApplyPlaceholderText(root, map)
        ApplyComboBoxItems(root, map)
        ApplyListBoxItems(root, map)
    End Sub

    ' 控件树：Text / PlaceholderText
    Private Sub CaptureSnapshots(root As Control)
        Dim st As New Stack(Of Control)
        st.Push(root)
        While st.Count > 0
            Dim c = st.Pop()
            If Not _snap.ContainsKey(c) Then
                Dim ph As String = Nothing
                Try
                    Dim prop = c.GetType().GetProperty("PlaceholderText")
                    If prop IsNot Nothing AndAlso prop.CanRead Then
                        Dim val = prop.GetValue(c, Nothing)
                        If val IsNot Nothing Then ph = CStr(val)
                    End If
                Catch
                End Try
                _snap(c) = New Snapshot With {.Text = c.Text, .Placeholder = ph}
            End If
            For Each ch As Control In c.Controls
                st.Push(ch)
            Next
        End While
    End Sub

    ' ToolStrip / MenuStrip / StatusStrip：递归采集所有层级 Item.Text
    Private Sub CaptureToolStrips(root As Control)
        Dim strips = root.FindForm().Controls.OfType(Of ToolStrip)()
        For Each ts In strips
            CaptureToolStripItems(ts.Items)
        Next
    End Sub

    Private Sub CaptureToolStripItems(items As ToolStripItemCollection)
        For Each it As ToolStripItem In items
            If Not _tsText.ContainsKey(it) Then
                _tsText(it) = it.Text
            End If
            Dim mi = TryCast(it, ToolStripMenuItem)
            If mi IsNot Nothing AndAlso mi.HasDropDownItems Then
                CaptureToolStripItems(mi.DropDownItems)
            End If
        Next
    End Sub

    ' DataGridView 列头
    Private Sub CaptureGridHeaders(root As Control)
        For Each g In root.FindForm().Controls.OfType(Of DataGridView)()
            For Each col As DataGridViewColumn In g.Columns
                If Not _gridHeader.ContainsKey(col) Then
                    _gridHeader(col) = col.HeaderText
                End If
            Next
        Next
    End Sub

    ' ComboBox / ListBox 的 Items
    Private Sub CaptureComboAndListItems(root As Control)
        For Each cb As ComboBox In GetAllControls(Of ComboBox)(root)
            If Not _comboItems.ContainsKey(cb) Then
                Dim lst As New List(Of String)
                For Each it In cb.Items
                    lst.Add(If(it IsNot Nothing, it.ToString(), ""))
                Next
                _comboItems(cb) = lst
            End If
        Next
        For Each lb As ListBox In GetAllControls(Of ListBox)(root)
            If Not _listBoxItems.ContainsKey(lb) Then
                Dim lst As New List(Of String)
                For Each it In lb.Items
                    lst.Add(If(it IsNot Nothing, it.ToString(), ""))
                Next
                _listBoxItems(lb) = lst
            End If
        Next
    End Sub

    ' 通用递归
    Private Function GetAllControls(Of T As Control)(root As Control) As IEnumerable(Of T)
        Dim list As New List(Of T)
        Dim st As New Stack(Of Control)
        st.Push(root)
        While st.Count > 0
            Dim c = st.Pop()
            If TypeOf c Is T Then list.Add(DirectCast(c, T))
            For Each ch As Control In c.Controls
                st.Push(ch)
            Next
        End While
        Return list
    End Function

End Module
