Imports System.Globalization
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text

Module PanelOptionSettings

    ' === 对外入口：保存 ===
    Public Sub SavePanelOptionToIni(panel As Panel, Optional section As String = "PanelOption", Optional ext As String = ".ini")
        If panel Is Nothing Then Return
        Dim ini = IniUtil.GetExeSiblingIniPath(ext)

        For Each ctrl In EnumerateChildren(panel)
            Dim key = SafeKey(ctrl)
            If key Is Nothing Then Continue For

            Dim value As String = ControlToString(ctrl)
            If value IsNot Nothing Then
                ini.IniWrite(section, key, value)
            End If
        Next
    End Sub

    ' === 对外入口：读取并应用 ===
    Public Sub LoadPanelOptionFromIni(panel As Panel, Optional section As String = "PanelOption", Optional ext As String = ".ini")
        If panel Is Nothing Then Return
        Dim ini = IniUtil.GetExeSiblingIniPath(ext)

        For Each ctrl In EnumerateChildren(panel)
            Dim key = SafeKey(ctrl)
            If key Is Nothing Then Continue For

            Dim saved = ini.IniRead(section, key, Nothing)
            If saved IsNot Nothing AndAlso saved <> "" Then
                StringToControl(ctrl, saved)
            End If
        Next
    End Sub

    ' ===== 工具：递归枚举子控件 =====
    Private Iterator Function EnumerateChildren(parent As Control) As IEnumerable(Of Control)
        For Each c As Control In parent.Controls
            Yield c
            If c.HasChildren Then
                For Each cc In EnumerateChildren(c)
                    Yield cc
                Next
            End If
        Next
    End Function

    ' ===== 键名：优先用控件 Name，没有则跳过 =====
    Private Function SafeKey(ctrl As Control) As String
        Dim n = ctrl.Name
        If String.IsNullOrWhiteSpace(n) Then Return Nothing
        Return n
    End Function

    ' ===== 控件 -> 字符串（保存）=====
    Private Function ControlToString(ctrl As Control) As String
        If TypeOf ctrl Is TabPage Or TypeOf ctrl Is Label Or TypeOf ctrl Is LinkLabel Or TypeOf ctrl Is Button Then
            Return Nothing
        End If

        ' 文本类
        If TypeOf ctrl Is TextBoxBase Then
            Return DirectCast(ctrl, TextBoxBase).Text
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

        ' 勾选类
        If TypeOf ctrl Is CheckBox Then
            Return If(DirectCast(ctrl, CheckBox).Checked, "1", "0")
        End If
        If TypeOf ctrl Is RadioButton Then
            Return If(DirectCast(ctrl, RadioButton).Checked, "1", "0")
        End If

        ' 列表类（优先 SelectedValue -> SelectedIndex -> Text）
        If TypeOf ctrl Is ComboBox Then
            Dim cb = DirectCast(ctrl, ComboBox)
            If Not String.IsNullOrEmpty(cb.ValueMember) AndAlso cb.DataSource IsNot Nothing AndAlso cb.SelectedValue IsNot Nothing Then
                Return "VAL:" & cb.SelectedValue.ToString()
            End If
            If cb.SelectedIndex >= 0 Then
                Return "IDX:" & cb.SelectedIndex.ToString(CultureInfo.InvariantCulture)
            End If
            Return "TXT:" & If(cb.Text, "")
        End If

        If TypeOf ctrl Is ListBox Then
            Dim lb = DirectCast(ctrl, ListBox)
            If lb.SelectionMode = SelectionMode.MultiExtended OrElse lb.SelectionMode = SelectionMode.MultiSimple Then
                Dim idxs = (From i As Integer In lb.SelectedIndices Select i.ToString(CultureInfo.InvariantCulture))
                Return "LIDX:" & String.Join(",", idxs)
            Else
                If lb.SelectedIndex >= 0 Then
                    Return "LIDX:" & lb.SelectedIndex.ToString(CultureInfo.InvariantCulture)
                End If
                Return "LTEXT:" & (If(lb.Text, ""))
            End If
        End If

        ' 数值/日期类
        If TypeOf ctrl Is NumericUpDown Then
            Return DirectCast(ctrl, NumericUpDown).Value.ToString(CultureInfo.InvariantCulture)
        End If
        If TypeOf ctrl Is TrackBar Then
            Return DirectCast(ctrl, TrackBar).Value.ToString(CultureInfo.InvariantCulture)
        End If
        If TypeOf ctrl Is ProgressBar Then
            Return DirectCast(ctrl, ProgressBar).Value.ToString(CultureInfo.InvariantCulture)
        End If
        If TypeOf ctrl Is DateTimePicker Then
            Return DirectCast(ctrl, DateTimePicker).Value.ToString("o", CultureInfo.InvariantCulture) ' ISO8601
        End If

        ' 兜底：尝试 Text
        Dim p = ctrl.GetType().GetProperty("Text")
        If p IsNot Nothing Then
            Dim v = TryCast(p.GetValue(ctrl, Nothing), String)
            Return If(v, "")
        End If

        Return Nothing
    End Function

    ' ===== 字符串 -> 控件（读取）=====
    Private Sub StringToControl(ctrl As Control, saved As String)
        ' 文本类
        If TypeOf ctrl Is TextBoxBase Then
            DirectCast(ctrl, TextBoxBase).Text = saved
            Exit Sub
        End If
        If TypeOf ctrl Is Label Then
            DirectCast(ctrl, Label).Text = saved : Exit Sub
        End If
        If TypeOf ctrl Is LinkLabel Then
            DirectCast(ctrl, LinkLabel).Text = saved : Exit Sub
        End If
        If TypeOf ctrl Is Button Then
            DirectCast(ctrl, Button).Text = saved : Exit Sub
        End If

        ' 勾选类
        If TypeOf ctrl Is CheckBox Then
            DirectCast(ctrl, CheckBox).Checked = (saved = "1") : Exit Sub
        End If
        If TypeOf ctrl Is RadioButton Then
            DirectCast(ctrl, RadioButton).Checked = (saved = "1") : Exit Sub
        End If

        ' 列表类
        If TypeOf ctrl Is ComboBox Then
            Dim cb = DirectCast(ctrl, ComboBox)
            If saved.StartsWith("VAL:") Then
                Dim val = saved.Substring(4)
                Try
                    cb.SelectedValue = val
                    If cb.SelectedIndex < 0 Then
                        ' 回退：找不到对应 Value 时按文本匹配
                        For i = 0 To cb.Items.Count - 1
                            Dim it = cb.Items(i)
                            If it IsNot Nothing AndAlso it.ToString() = val Then
                                cb.SelectedIndex = i : Exit For
                            End If
                        Next
                    End If
                Catch
                End Try
            ElseIf saved.StartsWith("IDX:") Then
                Dim s = saved.Substring(4)
                Dim idx As Integer
                If Integer.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, idx) Then
                    If idx >= 0 AndAlso idx < cb.Items.Count Then cb.SelectedIndex = idx
                End If
            ElseIf saved.StartsWith("TXT:") Then
                cb.Text = saved.Substring(4)
            Else
                ' 旧格式或纯文本兜底
                cb.Text = saved
            End If
            Exit Sub
        End If

        If TypeOf ctrl Is ListBox Then
            Dim lb = DirectCast(ctrl, ListBox)
            If saved.StartsWith("LIDX:") Then
                Dim body = saved.Substring(5)
                Dim parts = body.Split(New Char() {","c}, StringSplitOptions.RemoveEmptyEntries)
                lb.ClearSelected()
                For Each part In parts
                    Dim idx As Integer
                    If Integer.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, idx) Then
                        If idx >= 0 AndAlso idx < lb.Items.Count Then lb.SetSelected(idx, True)
                    End If
                Next
            ElseIf saved.StartsWith("LTEXT:") Then
                lb.SelectedItem = saved.Substring(6)
            Else
                ' 兜底
                lb.SelectedItem = saved
            End If
            Exit Sub
        End If

        ' 数值/日期类
        If TypeOf ctrl Is NumericUpDown Then
            Dim v As Decimal
            If Decimal.TryParse(saved, NumberStyles.Float, CultureInfo.InvariantCulture, v) Then
                Dim nud = DirectCast(ctrl, NumericUpDown)
                v = Math.Min(nud.Maximum, Math.Max(nud.Minimum, v))
                nud.Value = v
            End If
            Exit Sub
        End If
        If TypeOf ctrl Is TrackBar Then
            Dim v As Integer
            If Integer.TryParse(saved, NumberStyles.Integer, CultureInfo.InvariantCulture, v) Then
                Dim tb = DirectCast(ctrl, TrackBar)
                v = Math.Min(tb.Maximum, Math.Max(tb.Minimum, v))
                tb.Value = v
            End If
            Exit Sub
        End If
        If TypeOf ctrl Is ProgressBar Then
            Dim v As Integer
            If Integer.TryParse(saved, NumberStyles.Integer, CultureInfo.InvariantCulture, v) Then
                Dim pb = DirectCast(ctrl, ProgressBar)
                v = Math.Min(pb.Maximum, Math.Max(pb.Minimum, v))
                pb.Value = v
            End If
            Exit Sub
        End If
        If TypeOf ctrl Is DateTimePicker Then
            Dim dt As Date
            If Date.TryParse(saved, CultureInfo.InvariantCulture, Globalization.DateTimeStyles.RoundtripKind, dt) Then
                DirectCast(ctrl, DateTimePicker).Value = dt
            End If
            Exit Sub
        End If

        ' 兜底：尝试 Text
        Dim p = ctrl.GetType().GetProperty("Text")
        If p IsNot Nothing AndAlso p.CanWrite Then
            Try : p.SetValue(ctrl, saved, Nothing) : Catch : End Try
        End If
    End Sub
End Module
Module IniUtil
    <DllImport("kernel32", CharSet:=CharSet.Unicode, SetLastError:=True)>
    Private Function WritePrivateProfileString(lpAppName As String, lpKeyName As String, lpString As String, lpFileName As String) As Integer
    End Function

    <DllImport("kernel32", CharSet:=CharSet.Unicode, SetLastError:=True)>
    Private Function GetPrivateProfileString(lpAppName As String, lpKeyName As String, lpDefault As String,
                                            lpReturnedString As StringBuilder, nSize As Integer, lpFileName As String) As Integer
    End Function

    ' 取 EXE 同名 ini 路径，例如 app.exe -> app.ini
    Public Function GetExeSiblingIniPath(Optional ext As String = ".ini") As String
        If String.IsNullOrWhiteSpace(ext) Then ext = ".ini"
        If Not ext.StartsWith(".") Then ext = "." & ext
        Dim exe = Application.ExecutablePath
        Dim dir = Path.GetDirectoryName(exe)
        Dim baseName = Path.GetFileNameWithoutExtension(exe)
        Return Path.Combine(dir, baseName & ext)
    End Function

    <System.Runtime.CompilerServices.Extension>
    Public Sub IniWrite(path As String, section As String, key As String, value As String)
        If String.IsNullOrEmpty(section) OrElse String.IsNullOrEmpty(key) Then Return
        WritePrivateProfileString(section, key, value, path)
    End Sub

    <System.Runtime.CompilerServices.Extension>
    Public Function IniRead(path As String, section As String, key As String, Optional [default] As String = "") As String
        Dim sb As New StringBuilder(4096)
        GetPrivateProfileString(section, key, [default], sb, sb.Capacity, path)
        Return sb.ToString()
    End Function
End Module
