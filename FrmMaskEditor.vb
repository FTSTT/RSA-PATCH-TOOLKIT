' FrmMaskEditor.vb — 合并最新版（含：像素级同步滚动、A 框“软禁用”+消息过滤防拖动、
' B 直接键入替换最近 "*"、加粗变红、保存/随机填充、精简用户侧说明）

Imports System.Runtime.InteropServices
Imports System.Security.Cryptography
Imports System.Text

Public Class FrmMaskEditor
    ' ====== 批处理抑制/防抖 ======
    Private _suspendN As Integer = 0
    Private _formatting As Boolean = False
    Private _boldIndex As Integer = -1

    ' ====== A 框软禁用开关 ======
    Private _aLocked As Boolean = False

    ' ====== 提示实时刷新 ======
    Private WithEvents hintTimer As Timer
    Private _lastAllowed As String = Nothing
    Private _lastALen As Integer = -1

    ' ====== SendMessage 重载（WM_SETREDRAW + EM_GET/SETSCROLLPOS 共用）======
    <StructLayout(LayoutKind.Sequential)>
    Private Structure POINTAPI
        Public x As Integer
        Public y As Integer
    End Structure
    ' lParam 指针版
    <DllImport("user32.dll", CharSet:=CharSet.Auto, EntryPoint:="SendMessage")>
    Private Shared Function SendMessage(hWnd As IntPtr, msg As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr
    End Function
    ' lParam 结构版
    <DllImport("user32.dll", CharSet:=CharSet.Auto, EntryPoint:="SendMessage")>
    Private Shared Function SendMessage(hWnd As IntPtr, msg As Integer, wParam As IntPtr, ByRef pt As POINTAPI) As IntPtr
    End Function

    Private Const WM_SETREDRAW As Integer = &HB
    Private Const EM_GETSCROLLPOS As Integer = &H4DD   ' WM_USER + 221
    Private Const EM_SETSCROLLPOS As Integer = &H4DE   ' WM_USER + 222

    ' ====== 鼠标/键盘消息常量（拦截 A）======
    Private Const WM_LBUTTONDOWN As Integer = &H201
    Private Const WM_LBUTTONUP As Integer = &H202
    Private Const WM_LBUTTONDBLCLK As Integer = &H203
    Private Const WM_MOUSEMOVE As Integer = &H200
    Private Const WM_MOUSEWHEEL As Integer = &H20A
    Private Const WM_RBUTTONDOWN As Integer = &H204
    Private Const WM_MBUTTONDOWN As Integer = &H207
    Private Const WM_SETFOCUS As Integer = &H7
    Private Const WM_KEYDOWN As Integer = &H100
    Private Const WM_KEYUP As Integer = &H101

    ' ====== 滚动同步与消息过滤 ======
    Private _syncingScroll As Boolean = False
    Private _aMsgFilter As AInputBlocker

    ' ====== 停止/恢复重绘，减少闪烁 ======
    Private Sub SetRedraw(ctrl As Control, enable As Boolean)
        If ctrl Is Nothing OrElse ctrl.IsDisposed Then Return
        SendMessage(ctrl.Handle, WM_SETREDRAW, New IntPtr(If(enable, 1, 0)), IntPtr.Zero)
        If enable Then ctrl.Invalidate()
    End Sub
    Private Sub BeginUIUpdate()
        _suspendN += 1
        If _suspendN = 1 Then
            _formatting = True
            rtbA.SuspendLayout()
            rtbB.SuspendLayout()
            SetRedraw(rtbA, False)
            SetRedraw(rtbB, False)
        End If
    End Sub
    Private Sub EndUIUpdate()
        If _suspendN = 0 Then Return
        _suspendN -= 1
        If _suspendN = 0 Then
            SetRedraw(rtbA, True)
            SetRedraw(rtbB, True)
            rtbA.ResumeLayout()
            rtbB.ResumeLayout()
            _formatting = False
        End If
    End Sub

    ' ====== 像素级水平滚动同步 ======
    Private Function GetScrollPos(rtb As RichTextBox) As POINTAPI
        Dim p As POINTAPI
        SendMessage(rtb.Handle, EM_GETSCROLLPOS, IntPtr.Zero, p)
        Return p
    End Function
    Private Sub SetScrollPos(rtb As RichTextBox, p As POINTAPI)
        SendMessage(rtb.Handle, EM_SETSCROLLPOS, IntPtr.Zero, p)
    End Sub
    Private Sub SyncScrollFromB()
        If _syncingScroll Then Return
        If rtbB.IsDisposed OrElse rtbA.IsDisposed Then Return
        _syncingScroll = True
        Try
            Dim pB = GetScrollPos(rtbB)
            Dim pA = GetScrollPos(rtbA)
            pA.x = pB.x ' 同步水平像素位置
            SetScrollPos(rtbA, pA)
        Finally
            _syncingScroll = False
        End Try
    End Sub

    ' ====== A 框软禁用（外观不变） ======
    Private Sub LockA(disabled As Boolean)
        _aLocked = disabled
        rtbA.ReadOnly = True                 ' 始终只读，保留颜色
        rtbA.TabStop = Not disabled
        rtbA.Cursor = If(disabled, Cursors.Arrow, Cursors.IBeam)
        rtbA.ShortcutsEnabled = Not disabled
        rtbA.HideSelection = True
        If disabled Then
            rtbA.ContextMenuStrip = New ContextMenuStrip() ' 屏蔽右键菜单
        Else
            rtbA.ContextMenuStrip = Nothing
        End If
    End Sub

    ' 无跳动聚焦到 B：保持插入点与像素滚动
    Private Sub FocusBPreservingState()
        Dim selStart = rtbB.SelectionStart
        Dim selLen = rtbB.SelectionLength
        Dim pB = GetScrollPos(rtbB)

        BeginUIUpdate()
        rtbB.Focus()
        rtbB.SelectionStart = selStart
        rtbB.SelectionLength = selLen
        SetScrollPos(rtbB, pB)
        EndUIUpdate()

        BeginInvoke(New MethodInvoker(AddressOf SyncScrollFromB))
    End Sub

    ' 将 B 的光标定位到 idx，并完成加粗与像素同步
    Private Sub FocusBAtIndex(idx As Integer)
        idx = Math.Min(Math.Max(0, idx), rtbB.TextLength)
        BeginUIUpdate()
        rtbB.Focus()
        rtbB.SelectionStart = idx
        rtbB.SelectionLength = 0
        EndUIUpdate()
        UpdateBoldForCaret(rtbB)
        BeginInvoke(New MethodInvoker(AddressOf SyncScrollFromB))
    End Sub

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub FrmMaskEditor_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' 未提供原始字符串则直接退出
        If String.IsNullOrEmpty(AppGlobals.g_InputStr) Then
            MessageBox.Show(CStr(Loc.T("Text.c6b74c91", "未提供原始字符串，无法编辑掩码。")), CStr(Loc.T("Text.b25b7a81", "提示")), MessageBoxButtons.OK, MessageBoxIcon.Information)
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
            Return
        End If

        ' 启动/展示时重置随机结果
        AppGlobals.g_InputStr_rnd = Nothing

        ' 等宽字体
        rtbA.Font = New Font("Consolas", 10.0F, FontStyle.Regular)
        rtbB.Font = New Font("Consolas", 10.0F, FontStyle.Regular)

        ' A：只读文本
        Dim a As String = AppGlobals.g_InputStr
        rtbA.Text = a
        rtbA.ReadOnly = True

        ' B：掩码加载/补齐
        Dim n = a.Length
        Dim b As String = If(AppGlobals.g_SavedMask, String.Empty)
        If String.IsNullOrEmpty(b) Then b = New String("*"c, n)
        If b.Length > n Then b = b.Substring(0, n)
        If b.Length < n Then b &= New String("*"c, n - b.Length)

        BeginUIUpdate()
        rtbB.MaxLength = n
        rtbB.Text = b
        rtbB.SelectionStart = 0
        rtbB.SelectionLength = 0

        ' 单行高度：为 B 预留水平滚动条高度，避免遮挡
        PrepareSingleLine(rtbA, addHScrollHeight:=False)
        PrepareSingleLine(rtbB, addHScrollHeight:=True)

        ' 禁止粘贴（避免破坏长度/模式）
        rtbB.ShortcutsEnabled = False
        rtbA.ShortcutsEnabled = False
        EndUIUpdate()

        ' 软禁用 A（外观不变）
        LockA(True)

        ' 安装消息过滤器：拦截 A 的鼠标/键盘消息，避免拖动滚动
        _aMsgFilter = New AInputBlocker(Me)
        Application.AddMessageFilter(_aMsgFilter)

        ' 提示（允许字符按变量动态展示）
        txtHint.Text = BuildHintText(rtbA.TextLength, AppGlobals.g_AllowedChars)
        _lastAllowed = AppGlobals.g_AllowedChars
        _lastALen = rtbA.TextLength
        hintTimer = New Timer() With {.Interval = 500}
        hintTimer.Start()

        ' 初始着色与滚动对齐
        RecolorAFromB()
        SyncScrollFromB()
    End Sub

    Private Sub FrmMaskEditor_FormClosed(sender As Object, e As FormClosedEventArgs) Handles MyBase.FormClosed
        If _aMsgFilter IsNot Nothing Then
            Application.RemoveMessageFilter(_aMsgFilter)
            _aMsgFilter = Nothing
        End If
        If hintTimer IsNot Nothing Then
            hintTimer.Stop()
            hintTimer.Dispose()
            hintTimer = Nothing
        End If
    End Sub

    Private Sub PrepareSingleLine(rtb As RichTextBox, addHScrollHeight As Boolean)
        rtb.Multiline = True
        rtb.WordWrap = False
        Dim lineH = TextRenderer.MeasureText("A", rtb.Font).Height
        Dim extra = If(addHScrollHeight, SystemInformation.HorizontalScrollBarHeight, 0)
        rtb.Height = lineH + 6 + extra
    End Sub

    ' ========== B 直接键入替换最近 "*" ==========
    Private Function FindStarIndexForTyping(startPos As Integer) As Integer
        Dim s = rtbB.Text
        If startPos < 0 Then startPos = 0
        If startPos > s.Length Then startPos = s.Length
        Dim i As Integer = s.IndexOf("*"c, startPos)
        If i = -1 AndAlso startPos > 0 Then
            i = s.LastIndexOf("*"c, startPos - 1)
        End If
        Return i
    End Function

    Private Sub rtbB_KeyPress(sender As Object, e As KeyPressEventArgs) Handles rtbB.KeyPress
        If e.KeyChar = ChrW(Keys.Return) OrElse e.KeyChar = ChrW(Keys.LineFeed) Then
            e.Handled = True : Return
        End If
        If Char.IsControl(e.KeyChar) Then Return

        If e.KeyChar <> "*"c AndAlso Not IsAllowed(e.KeyChar) Then
            System.Media.SystemSounds.Beep.Play()
            e.Handled = True : Return
        End If

        Dim caret As Integer = rtbB.SelectionStart
        Dim target As Integer

        If e.KeyChar = "*"c Then
            If rtbB.TextLength = 0 Then e.Handled = True : Return
            target = Math.Min(Math.Max(0, caret), rtbB.TextLength - 1)
        Else
            target = FindStarIndexForTyping(caret)
            If target = -1 Then
                System.Media.SystemSounds.Beep.Play()
                e.Handled = True : Return
            End If
        End If

        ReplaceAt(rtbB, target, e.KeyChar)
        BeginUIUpdate()
        rtbB.SelectionStart = Math.Min(target + 1, rtbB.TextLength)
        rtbB.SelectionLength = 0
        EndUIUpdate()
        e.Handled = True

        RecolorAFromB()
        UpdateBoldForCaret(rtbB)
        BeginInvoke(New MethodInvoker(AddressOf SyncScrollFromB))
    End Sub

    Private Sub rtbB_KeyDown(sender As Object, e As KeyEventArgs) Handles rtbB.KeyDown
        If rtbB.TextLength = 0 Then Return
        Select Case e.KeyCode
            Case Keys.Back
                Dim pos = rtbB.SelectionStart
                If pos > 0 Then
                    pos -= 1
                    ReplaceAt(rtbB, pos, "*"c)
                    BeginUIUpdate()
                    rtbB.SelectionStart = pos
                    rtbB.SelectionLength = 0
                    EndUIUpdate()
                    e.Handled = True : e.SuppressKeyPress = True
                End If
            Case Keys.Delete
                Dim pos = rtbB.SelectionStart
                If pos < rtbB.TextLength Then
                    ReplaceAt(rtbB, pos, "*"c)
                    BeginUIUpdate()
                    rtbB.SelectionStart = pos
                    rtbB.SelectionLength = 0
                    EndUIUpdate()
                    e.Handled = True : e.SuppressKeyPress = True
                End If
        End Select
        RecolorAFromB()
        UpdateBoldForCaret(rtbB)
        BeginInvoke(New MethodInvoker(AddressOf SyncScrollFromB))
    End Sub

    Private Sub rtbB_TextChanged(sender As Object, e As EventArgs) Handles rtbB.TextChanged
        If _suspendN > 0 Then Return
        RecolorAFromB()
        UpdateBoldForCaret(rtbB)
        BeginInvoke(New MethodInvoker(AddressOf SyncScrollFromB))
    End Sub

    ' ========== 滚动/光标同步 ==========
    Private Sub rtbB_HScrolled(sender As Object, e As EventArgs) Handles rtbB.HScrolled
        SyncScrollFromB()
    End Sub
    Private Sub rtbB_KeyUp(sender As Object, e As KeyEventArgs) Handles rtbB.KeyUp
        UpdateBoldForCaret(rtbB)
        BeginInvoke(New MethodInvoker(AddressOf SyncScrollFromB))
    End Sub
    Private Sub rtbB_MouseUp(sender As Object, e As MouseEventArgs) Handles rtbB.MouseUp
        UpdateBoldForCaret(rtbB)
        BeginInvoke(New MethodInvoker(AddressOf SyncScrollFromB))
    End Sub

    ' ========== 允许字符 ==========
    Private Function IsAllowed(ch As Char) As Boolean
        Dim setStr = AppGlobals.g_AllowedChars
        If String.IsNullOrEmpty(setStr) Then Return True
        Return setStr.IndexOf(ch) >= 0
    End Function

    ' 替换指定索引字符（保持长度不变）
    Private Sub ReplaceAt(rtb As RichTextBox, index As Integer, ch As Char)
        Dim s = rtb.Text
        Dim sb As New StringBuilder(s)
        sb(index) = ch
        BeginUIUpdate()
        rtb.Text = sb.ToString()
        EndUIUpdate()
    End Sub

    ' ========== A 中蓝色标注（区间批量设置，减少事件与闪烁）==========
    Private Sub RecolorAFromB()
        If rtbA.TextLength <> rtbB.TextLength Then Return

        BeginUIUpdate()
        Dim selStart = rtbA.SelectionStart
        Dim selLen = rtbA.SelectionLength

        rtbA.Select(0, rtbA.TextLength)
        rtbA.SelectionColor = Color.Black
        rtbA.SelectionFont = New Font(rtbA.Font, FontStyle.Regular)

        Dim b = rtbB.Text
        Dim i As Integer = 0
        While i < b.Length
            If b(i) <> "*"c Then
                Dim start = i
                i += 1
                While i < b.Length AndAlso b(i) <> "*"c
                    i += 1
                End While
                Dim length = i - start
                rtbA.Select(start, length)
                rtbA.SelectionColor = Color.Blue
            Else
                i += 1
            End If
        End While

        rtbA.Select(selStart, selLen)
        EndUIUpdate()
    End Sub

    ' ========== 加粗+红色（同步到 A/B），并避免递归 ==========
    Private Sub UpdateBoldForCaret(source As RichTextBox)
        If _formatting Then Return
        Dim pos = source.SelectionStart
        If pos < 0 OrElse pos >= Math.Min(rtbA.TextLength, rtbB.TextLength) Then
            ClearBold() : Return
        End If
        If _boldIndex = pos Then Return

        BeginUIUpdate()
        If _boldIndex >= 0 Then
            ResetBaseStyleInA(_boldIndex)
            SetCharRegularBlack(rtbB, _boldIndex)
        End If
        SetCharBoldRed(rtbA, pos)
        SetCharBoldRed(rtbB, pos)
        _boldIndex = pos
        EndUIUpdate()
    End Sub

    Private Sub ClearBold()
        If _boldIndex < 0 Then Return
        BeginUIUpdate()
        ResetBaseStyleInA(_boldIndex)
        SetCharRegularBlack(rtbB, _boldIndex)
        _boldIndex = -1
        EndUIUpdate()
    End Sub

    Private Sub ResetBaseStyleInA(index As Integer)
        If index < 0 OrElse index >= rtbA.TextLength Then Return
        Dim savedSel = rtbA.SelectionStart
        Dim savedLen = rtbA.SelectionLength
        rtbA.Select(index, 1)
        Dim isNonStar = (rtbB.Text(index) <> "*"c)
        rtbA.SelectionFont = New Font(rtbA.Font, FontStyle.Regular)
        rtbA.SelectionColor = If(isNonStar, Color.Blue, Color.Black)
        rtbA.Select(savedSel, savedLen)
    End Sub

    Private Sub SetCharBoldRed(rtb As RichTextBox, index As Integer)
        If index < 0 OrElse index >= rtb.TextLength Then Return
        Dim savedSel = rtb.SelectionStart
        Dim savedLen = rtb.SelectionLength
        rtb.Select(index, 1)
        rtb.SelectionFont = New Font(rtb.Font, FontStyle.Bold)
        rtb.SelectionColor = Color.Red
        rtb.Select(savedSel, savedLen)
    End Sub

    Private Sub SetCharRegularBlack(rtb As RichTextBox, index As Integer)
        If index < 0 OrElse index >= rtb.TextLength Then Return
        Dim savedSel = rtb.SelectionStart
        Dim savedLen = rtb.SelectionLength
        rtb.Select(index, 1)
        rtb.SelectionFont = New Font(rtb.Font, FontStyle.Regular)
        rtb.SelectionColor = Color.Black
        rtb.Select(savedSel, savedLen)
    End Sub

    ' ========== Base64 末尾 "=" 计数/确认 ==========
    Private Function CountTrailingEquals(a As String) As Integer
        If String.IsNullOrEmpty(a) Then Return 0
        Dim i As Integer = a.Length - 1, cnt As Integer = 0
        While i >= 0 AndAlso a(i) = "="c
            cnt += 1 : i -= 1
        End While
        Return cnt
    End Function

    Private Sub ResetTailPositionsToStar(countEq As Integer)
        If countEq <= 0 Then Return
        Dim startIdx As Integer = rtbB.TextLength - countEq
        For i = startIdx To rtbB.TextLength - 1
            ReplaceAt(rtbB, i, "*"c)
        Next
        BeginUIUpdate()
        rtbB.SelectionStart = Math.Max(0, startIdx)
        rtbB.SelectionLength = Math.Max(0, countEq)
        EndUIUpdate()
        RecolorAFromB()
        UpdateBoldForCaret(rtbB)
        AppendWarn(CStr(Loc.T("Text.61243d4c", "尝试修改 A 结尾的 ""="" 已被取消；对应位置已重置为 ""*""。")))
    End Sub

    ' ========== 保存 ==========
    Private Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        If Not DoSaveCore(saveAndRandom:=False) Then Return
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Sub btnSaveRnd_Click(sender As Object, e As EventArgs) Handles btnSaveRnd.Click
        If Not DoSaveCore(saveAndRandom:=True) Then Return
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Function DoSaveCore(saveAndRandom As Boolean) As Boolean
        Dim err As String = Nothing
        If Not ValidateMask(err) Then
            MessageBox.Show(err, CStr(Loc.T("Text.6e30d0e9", "校验失败")), MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return False
        End If

        Dim aText = rtbA.Text
        Dim bText = rtbB.Text
        Dim eqCnt As Integer = CountTrailingEquals(aText)
        If eqCnt > 0 Then
            Dim tailStart As Integer = bText.Length - eqCnt
            Dim changedPadding As Boolean = False
            For i = tailStart To bText.Length - 1
                If bText(i) <> "*"c Then changedPadding = True : Exit For
            Next
            If changedPadding Then
                Dim msg = CStr(Loc.T("Text.fc2ee7be", "A 的结尾包含 ""=""（Base64 填充位）。修改这些位置可能改变解码长度。")) &
                          Environment.NewLine & Environment.NewLine &
                          CStr(Loc.T("Text.3c3023db", "是否仍要继续保存？")) & Environment.NewLine &
                          CStr(Loc.T("Text.2f961a3a", "【是】：继续保存    【否】：取消并将这些位置重置为 ""*""。"))
                Dim r = MessageBox.Show(msg, CStr(Loc.T("Text.8b78ab78", "确认修改 Base64 填充位")), MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                If r = DialogResult.No Then
                    ResetTailPositionsToStar(eqCnt)
                    Return False
                End If
            End If
        End If

        AppGlobals.g_SavedMask = rtbB.Text


        Dim frozenRanges As String = NonStarToFrozenRanges(rtbB.Text)
        AppGlobals.g_frozenRanges = frozenRanges         '根据掩码生成的冻结区
        If saveAndRandom Then
            Dim baseStr As String = OverlayNonStar(rtbA.Text, rtbB.Text)
            Dim allowed As String = If(AppGlobals.g_AllowedChars, String.Empty)
            Dim seed As Integer = GenerateSeed()
            Dim rndFilled As String = CrcNeutralVariantGenerator.RandomFillUsingAllowedChars(baseStr, frozenRanges, allowed, seed)
            AppGlobals.g_InputStr_rnd = rndFilled
        End If

        Return True
    End Function

    Private Function ValidateMask(ByRef errorMessage As String) As Boolean
        Dim aLen = rtbA.TextLength
        Dim b = rtbB.Text
        If b.Length <> aLen Then
            errorMessage = CStr(Loc.T("Text.c3fbcf86", "掩码长度必须等于 A 的长度（")) & aLen.ToString() & CStr(Loc.T("Text.3741c822", "），当前为 ")) & b.Length.ToString() & CStr(Loc.T("Text.6cd0cdac", "。"))
            Return False
        End If

        Dim allow = AppGlobals.g_AllowedChars
        If Not String.IsNullOrEmpty(allow) Then
            For i As Integer = 0 To b.Length - 1
                Dim ch = b(i)
                If ch <> "*"c AndAlso allow.IndexOf(ch) < 0 Then
                    errorMessage = CStr(Loc.T("Text.e1c096e7", "发现不被允许的字符：位置 ")) & (i + 1).ToString() & CStr(Loc.T("Text.269638d7", "，字符 """)) & ch & CStr(Loc.T("Text.5a4dcca1", """。仅允许：[")) & allow & CStr(Loc.T("Text.c601a942", "] 或 ""*""。"))
                    Return False
                End If
            Next
        End If

        errorMessage = Nothing
        Return True
    End Function

    ' ========== 精简用户侧说明 ==========
    Private Function BuildHintText(aLen As Integer, allowed As String) As String
        Dim sb As New StringBuilder()
        sb.AppendLine(CStr(Loc.T("Text.afa5ad95", "【使用说明】")))
        sb.AppendLine(CStr(Loc.T("Text.b1fbcad1", "• 目标：用下框 B 的掩码覆盖 A；""*"" 表示保留。")))
        sb.AppendLine(CStr(Loc.T("Text.4ade3dad", "• A：只读；点 A 的字符，B 光标跳到同位置。")))
        sb.AppendLine(CStr(Loc.T("Text.1acdac63", "• B：直接键入=替换最近的 ""*""；按 ""*""/Backspace/Delete 将该位设为 ""*""。")))
        sb.AppendLine(CStr(Loc.T("Text.3a57610b", "• 高亮：当前位在 A/B 同时加粗并变红；A 中非 ""*"" 位为蓝色。")))
        sb.AppendLine(CStr(Loc.T("Text.f2ad17a9", "• 滚动：仅 B 显示水平滚动条；A 与 B 像素级同步。")))
        If String.IsNullOrEmpty(allowed) Then
            sb.AppendLine(CStr(Loc.T("Text.3f7f9799", "• 允许字符：不限制（""*"" 始终可用）。")))
        Else
            sb.AppendLine(CStr(Loc.T("Text.bcbd2a27", "• 允许字符：[")) & allowed & CStr(Loc.T("Text.aedecba9", "]（""*"" 始终可用）。")))
        End If
        sb.AppendLine(CStr(Loc.T("Text.af77b409", "• 长度：B 必须与 A 等长（当前：")) & aLen.ToString() & CStr(Loc.T("Text.802700ee", "）。")))
        sb.AppendLine(CStr(Loc.T("Text.cf1c3838", "• 【保存】：校验并写入掩码。")))
        sb.AppendLine(CStr(Loc.T("Text.22461aa0", "• 【保存并随机填充】：以 B 的非 ""*"" 位为冻结区生成变体并保存结果。")))
        sb.Append(CStr(Loc.T("Text.2379ad29", "• 注意：若 A 末尾含 ""=""（Base64 填充位），修改对应位会提示确认。")))
        Return sb.ToString()
    End Function

    Private Sub AppendWarn(line As String)
        If txtHint.TextLength > 0 Then txtHint.AppendText(Environment.NewLine)
        txtHint.AppendText("⚠ " & line)
    End Sub

    Private Sub hintTimer_Tick(sender As Object, e As EventArgs) Handles hintTimer.Tick
        Dim changed As Boolean = False
        If _lastAllowed <> AppGlobals.g_AllowedChars Then
            _lastAllowed = AppGlobals.g_AllowedChars
            changed = True
        End If
        If _lastALen <> rtbA.TextLength Then
            _lastALen = rtbA.TextLength
            changed = True
        End If
        If changed Then
            txtHint.Text = BuildHintText(_lastALen, _lastAllowed)
        End If
    End Sub

    ' ========== 工具 ==========
    Private Function GenerateSeed() As Integer
        Dim bytes(3) As Byte
        Using rng = RandomNumberGenerator.Create()
            rng.GetBytes(bytes)
        End Using
        Dim val As Integer = BitConverter.ToInt32(bytes, 0)
        If val = Integer.MinValue Then Return 0
        Return Math.Abs(val)
    End Function

    ' B 的非 "*" 片段 -> 冻结区字符串（1-based 闭区间）
    Private Function NonStarToFrozenRanges(s As String) As String
        If s Is Nothing Then Return ""
        Dim n As Integer = s.Length
        If n = 0 Then Return ""
        Dim sb As New StringBuilder()
        Dim i As Integer = 0
        While i < n
            If s.Chars(i) <> "*"c Then
                Dim start1 As Integer = i + 1
                i += 1
                While i < n AndAlso s.Chars(i) <> "*"c
                    i += 1
                End While
                Dim end1 As Integer = i
                If sb.Length > 0 Then sb.Append(","c)
                If start1 = end1 Then
                    sb.Append(start1.ToString())
                Else
                    sb.Append(start1.ToString()).Append("-"c).Append(end1.ToString())
                End If
            Else
                i += 1
            End If
        End While
        Return sb.ToString()
    End Function

    ' 用 B 的非 "*" 覆盖 A
    Private Function OverlayNonStar(a As String, b As String) As String
        If a Is Nothing Then a = ""
        If b Is Nothing Then b = ""
        If b.Length > a.Length Then b = b.Substring(0, a.Length)
        If a.Length = 0 Then Return ""
        Dim sb As New StringBuilder(a)
        For i As Integer = 0 To b.Length - 1
            Dim ch As Char = b.Chars(i)
            If ch <> "*"c Then sb(i) = ch
        Next
        Return sb.ToString()
    End Function

    ' ====== 内部：拦截 A 的输入，阻止拖动/滚动；转交到 B ======
    Private Class AInputBlocker
        Implements IMessageFilter

        Private ReadOnly _frm As FrmMaskEditor
        Public Sub New(frm As FrmMaskEditor)
            _frm = frm
        End Sub

        Public Function PreFilterMessage(ByRef m As Message) As Boolean Implements IMessageFilter.PreFilterMessage
            If _frm Is Nothing OrElse _frm.rtbA Is Nothing Then Return False
            If Not _frm._aLocked Then Return False
            If m.HWnd <> _frm.rtbA.Handle Then Return False

            Select Case m.Msg
                Case WM_LBUTTONDOWN, WM_LBUTTONDBLCLK
                    Dim lp As Integer = m.LParam.ToInt32()
                    Dim x As Integer = CShort(lp And &HFFFF)
                    Dim y As Integer = CShort((lp >> 16) And &HFFFF)
                    Dim idx As Integer = _frm.rtbA.GetCharIndexFromPosition(New Point(x, y))
                    _frm.FocusBAtIndex(idx)
                    Return True
                Case WM_MOUSEMOVE, WM_LBUTTONUP, WM_MOUSEWHEEL, WM_RBUTTONDOWN, WM_MBUTTONDOWN, WM_SETFOCUS, WM_KEYDOWN, WM_KEYUP
                    _frm.FocusBPreservingState()
                    Return True
            End Select
            Return False
        End Function
    End Class
End Class
