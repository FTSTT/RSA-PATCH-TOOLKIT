Imports System.Globalization
Imports System.IO
Imports System.Numerics
Imports System.Reflection
Imports System.Text
Imports System.Text.Json
Imports System.Text.RegularExpressions
Imports System.Threading

Public Class frmPrimeKit
    Private _cts As New Threading.CancellationTokenSource()

    Public Shared hasEq As Boolean = False
    ' 放到窗体代码或模块中（新增）
    Public Enum TryDirection
        Forward
        Backward
        CurrentOnly
    End Enum

    Private Const HEXSET As String = "0123456789ABCDEF"
    Private Const DECSET As String = "0123456789"
    Private Const B64SET As String = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"

    ' 标志变量，防止自动更改格式时触发 SelectedIndexChanged
    Private isAutoChanging As Boolean = False
    ' 存储原始格式
    Private previousFormat As String = String.Empty

    Public Const README_MARKUP As String =
    "<SIZE=16><B>第一部分：新手指南</B></SIZE>

<B>欢迎</B>使用 RSA PATCH TOOLKIT！本节帮助你快速完成一次从粘贴到查看结果的完整流程。

<B>界面速览</B>
- 顶部：<B>输入区</B>（粘贴/识别/格式转换/翻转）。
- 中部：<B>参数与选项区</B>（微调字节模式、固定 CRC32 模式与通用选项）。
- 底部：<B>操作与日志</B>（开始/停止/清空日志/帮助/日志窗口）。

<B>快速开始</B>
1) 点击 <B>粘贴[P]</B> 导入 N；程序会自动识别格式（Hex/Decimal/Base64）。
2) 若识别不符，点击 <B>isHEX / isDecimal / isBase64</B> 手动纠正（不会触发转换）。
3) 需要格式转换？更改 <B>格式下拉框</B> 即可（例如 Hex → Base64）。
4) 选择运行模式：
   - <B>微调字节模式</B>：设定““<B>尝试替换位置</B>””与““<B>方向</B>””，可选““<B>按半字节</B>””与““<B>输入的HEX是字符串</B>””。
   - <B>固定 CRC32 模式</B>：点击 <B>掩码</B> 定义可变(*)/冻结位，配置允许字符集与 <B>Unicode</B> 开关。
5) 点击 <B>开始</B>。候选会输出到日志窗口，差异位置以 <COLOR=red><B>红色粗体</B></COLOR> 标注。

<COLOR=#FF5500><B>常用快捷</B></COLOR>
- <B>翻转[R]</B>：仅对 Hex 生效，按字节序反转。
- <B>Dry Run</B>：只演示流程不做真实计算。
- <B>帮助</B>：输出内置示例（带标记）。

<SIZE=16><B>第二部分：选项说明</B></SIZE>

<B>顶部输入区</B>
- <B>TextN</B>：承载 N 的文本，支持 Hex/Decimal/Base64。
- <B>ComboFormat</B>：切换显示/转换格式（<B>HEX</B>/<B>hex</B>/<B>Decimal</B>/<B>Base64</B>）。
- <B>粘贴[P]</B>：清理空白并自动识别格式；Base64 会自动补齐““=””。
- <B>翻转[R]</B>：Hex 下按 2 字符＝1 字节整体反转。
- <B>isHEX / isDecimal / isBase64 / isHex(lower)</B>：仅改下拉框文本，不转换内容。

<B>中部选项区</B>
1. <B>微调字节模式</B>
   - <B>尝试替换位置</B>（1 基）：设置起点。
   - <B>方向</B>：仅当前 / 向左 / 向右。
   - <B>按半字节</B>：对 nibble（高/低 4 位）进行微调。
   - <B>输入的HEX是字符串</B>：按字符或按字节处理 Hex。

2. <B>固定 CRC32 模式</B>
   - <B>掩码</B>：使用““*””标示可变位，其他字符为冻结。
   - <B>允许字符集</B>：限制候选字符范围（Hex/Dec/Base64 或自定义）。
   - <B>Unicode</B>：ASCII 与 Unicode(UTF-16LE) 路径切换。
   - <B>需要字符数</B>：设置有效可修改位数量。

<B>通用选项</B>
- <B>Dry Run</B>：仅记录流程不执行计算。
- <B>Rsa Test</B>：对 N/D 进行随机加/解密验证（数学一致性）。
- <B>Brent 迭代次数</B>：控制 Rho–Brent 因子搜索迭代上限。
- <B>小素数试除</B>：启用基础试除。
- <B>尝试小曲线</B>：ECM 小曲线数量。
- <B>结果数量下限</B>：最少生成条数。
- <B>结果记录日志</B>：将候选写入日志。
- <B>线程数</B>：设置并发度。
- <B>接受素数 N</B>：允许 N 为素数。

<B>底部操作与日志</B>
- <B>开始</B>：读取选项并运行，结果以标记富文本打印到 <B>RichTextLog</B>。
- <B>停止</B>：中止当前任务。
- <B>清空日志</B>：清除输出。
- <B>帮助</B>：显示内置帮助与示例。

<SIZE=14><B>掩码编辑器（FrmMaskEditor）</B></SIZE>
- <B>A 框</B>：显示原始字符串，冻结段以蓝色提示；光标联动高亮。
- <B>B 框</B>：编辑掩码，““*””=可变；删除恢复为““*””。
- <B>保存 / 保存并随机填充</B>：校验并持久化，可立即生成一次随机解。
- <B>提示区</B>：动态展示允许字符与长度信息。"



    ' 放到你的 Form 里
    Private Class ExeItem
        Public Property Display As String     ' 显示文本（文件名 + 相对路径）
        Public Property FullPath As String    ' 完整路径（ValueMember）
        Public Overrides Function ToString() As String
            Return Display
        End Function
    End Class


    ' 计算相对路径的小工具（.NET Framework 下没有 Path.GetRelativePath）
    Private Function GetRelativePathSafe(baseDir As String, fullPath As String) As String
        Try
            Dim b = Path.GetFullPath(If(baseDir, ""))
            Dim f = Path.GetFullPath(If(fullPath, ""))
            If Not f.StartsWith(b, StringComparison.OrdinalIgnoreCase) Then Return fullPath
            Dim rel = f.Substring(b.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            Return rel
        Catch
            Return fullPath
        End Try
    End Function
    Private Sub setAllowedChars()
        Select Case ComboFormat.SelectedItem.ToString()
            Case "HEX"
                ComboBoxAllowedChars.Text = "0123456789ABCDEF"
            Case "hex"
                ComboBoxAllowedChars.Text = "0123456789abcdef"
            Case "Decimal"
                ComboBoxAllowedChars.Text = "0123456789"
            Case "Base64"
                ComboBoxAllowedChars.Text = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"
            Case Else
                ComboBoxAllowedChars.Text = ""
        End Select
    End Sub


    ' ButtonPaste 点击事件：去除空格和换行，并自动识别格式
    Private Sub ButtonPaste_Click_1(sender As Object, e As EventArgs) Handles ButtonPaste.Click
        ' 获取剪贴板内容并清理
        Dim clipboardText As String = Clipboard.GetText()
        clipboardText = clipboardText.Replace(Environment.NewLine, "").Replace(" ", "")
        If clipboardText = "" Then
            Exit Sub
        End If

        TextN.Text = clipboardText
        hasEq = (clipboardText IsNot Nothing) AndAlso clipboardText.IndexOf("="c) >= 0



        ' 自动识别格式并更新 ComboBox
        isAutoChanging = True ' 禁止触发事件
        ButtonReverse.Enabled = False
        If IsDecimal(clipboardText) Then
            ComboFormat.SelectedItem = "Decimal"
        ElseIf IsHex(clipboardText) Then
            ButtonReverse.Enabled = True

            ' 只要出现小写 a-f，就判为 hex，否则判为 HEX
            If Regex.IsMatch(clipboardText, "[a-f]") Then
                ComboFormat.SelectedItem = "hex"
            Else
                ComboFormat.SelectedItem = "HEX"
            End If
        ElseIf IsBase64(clipboardText) Then
            'todo 兼容问题要解决
            ComboFormat.SelectedItem = "Base64"
        Else
            ComboFormat.SelectedItem = "unknown"
            isAutoChanging = False ' 恢复事件触发
            Exit Sub
        End If

        previousFormat = ComboFormat.SelectedItem.ToString()

        setAllowedChars()

        isAutoChanging = False ' 恢复事件触发
    End Sub


    ' 判断字符串是否为 Hex 格式
    Private Function IsHex(input As String) As Boolean
        Return System.Text.RegularExpressions.Regex.IsMatch(input, "^[0-9A-Fa-f]+$")
    End Function

    ' 判断字符串是否为 Decimal 格式
    Private Function IsDecimal(input As String) As Boolean
        Return System.Text.RegularExpressions.Regex.IsMatch(input, "^\d+$")
    End Function

    ' 判断字符串是否为 Base64 格式
    Private Function IsBase64(input As String) As Boolean
        Try
            Dim bytes As Byte() = Convert.FromBase64String(AddBase64Padding(input))

            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    ' ComboBox 格式选择更改事件
    Private Sub ComboFormat_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboFormat.SelectedIndexChanged
        If isAutoChanging Then Exit Sub

        Dim currentFormat As String = ComboFormat.SelectedItem.ToString()
        Dim prev As String = previousFormat

        Dim prevIsHex As Boolean = (prev = "HEX" OrElse prev = "hex")
        Dim currIsHex As Boolean = (currentFormat = "HEX" OrElse currentFormat = "hex")

        ButtonReverse.Enabled = currIsHex
        setAllowedChars()

        If prevIsHex AndAlso currIsHex Then
            ' 同为十六进制：仅切换大小写
            If currentFormat = "HEX" Then
                TextN.Text = TextN.Text.ToUpperInvariant()
            Else
                TextN.Text = TextN.Text.ToLowerInvariant()
            End If
        Else
            Select Case prev
                Case "HEX", "hex"
                    If currentFormat = "Decimal" Then
                        TextN.Text = ConvertHexToDecimal(TextN.Text)
                    ElseIf currentFormat = "Base64" Then
                        TextN.Text = ConvertHexToBase64(TextN.Text)
                    End If

                Case "Decimal"
                    If currIsHex Then
                        Dim hex = ConvertDecimalToHex(TextN.Text) ' 注意：内部默认大写
                        TextN.Text = If(currentFormat = "HEX", hex.ToUpperInvariant(), hex.ToLowerInvariant())
                    ElseIf currentFormat = "Base64" Then
                        TextN.Text = ConvertDecimalToBase64(TextN.Text)
                    End If

                Case "Base64"
                    If currIsHex Then
                        Dim hex = ConvertBase64ToHex(TextN.Text)  ' 注意：内部默认大写
                        TextN.Text = If(currentFormat = "HEX", hex.ToUpperInvariant(), hex.ToLowerInvariant())
                    ElseIf currentFormat = "Decimal" Then
                        TextN.Text = ConvertBase64ToDecimal(TextN.Text)
                    End If
            End Select
        End If

        previousFormat = currentFormat
    End Sub

    ' —— 小工具：Hex <-> Bytes（保证偶数字符 / 大端） ——
    Private Function HexToBytes(hex As String) As Byte()
        Dim s = New String(hex.Where(Function(c) Not Char.IsWhiteSpace(c)).ToArray())
        If s.Length = 0 Then Return Array.Empty(Of Byte)()
        If (s.Length And 1) = 1 Then s = "0" & s ' 奇数长度左补0
        Dim buf(s.Length \ 2 - 1) As Byte
        For i = 0 To buf.Length - 1
            buf(i) = Convert.ToByte(s.Substring(i * 2, 2), 16)
        Next
        Return buf
    End Function

    Private Function BytesToHex(b() As Byte) As String
        Dim sb As New StringBuilder(b.Length * 2)
        For Each x In b
            sb.Append(x.ToString("X2"))
        Next
        Return sb.ToString()
    End Function

    ' —— 小工具：Base64 规范化（补齐=到4的倍数） ——
    Private Function NormalizeBase64(b64 As String) As String
        Dim s = b64.Trim()
        Dim mod4 = s.Length Mod 4
        If mod4 <> 0 Then s = s.PadRight(s.Length + (4 - mod4), "="c)
        Return s
    End Function

    ' —— 小工具：BigInteger 与“大端无符号”字节互转（避免符号位/小端陷阱） ——
    Private Function BigIntToBigEndianUnsignedBytes(n As BigInteger) As Byte()
        If n.Sign = 0 Then Return New Byte() {0}
        ' 用 "X" 得到无符号十六进制，再走 HexToBytes，天然是大端且无符号
        Dim hex = n.ToString("X")           ' 大数十六进制（不带0x）
        Return HexToBytes(hex)
    End Function

    Private Function BigEndianUnsignedBytesToBigInt(bytesBE As Byte()) As BigInteger
        If bytesBE Is Nothing OrElse bytesBE.Length = 0 Then Return BigInteger.Zero
        ' BigInteger(byte[]) 期望小端；反转，并追加0字节避免被当成负数
        Dim le = bytesBE.Reverse().ToList()
        le.Add(0)
        Return New BigInteger(le.ToArray())
    End Function


    ' 如果你需要直接拿到 BigInteger（非负）
    Private Function ParseHexAsUnsignedBigInteger(hexStr As String) As BigInteger
        If hexStr Is Nothing Then Return BigInteger.Zero

        ' 1) 清洗：去空白/下划线，去 0x 前缀
        Dim clean As String = Regex.Replace(hexStr, "[\s_]", "")
        If clean.StartsWith("0x", StringComparison.OrdinalIgnoreCase) Then
            clean = clean.Substring(2)
        End If
        If clean.Length = 0 Then Return BigInteger.Zero

        ' 2) 奇数位数前补 0，使其成为完整字节
        If (clean.Length Mod 2) = 1 Then clean = "0" & clean

        ' 3) 解析为大端字节
        Dim be(clean.Length \ 2 - 1) As Byte
        For i = 0 To be.Length - 1
            be(i) = Convert.ToByte(clean.Substring(2 * i, 2), 16)
        Next

        ' 4) 转为小端并在最高位追加 0x00，强制无符号
        Array.Reverse(be) ' 大端 -> 小端
        Dim le = be.Concat(New Byte() {0}).ToArray()

        ' 5) 构造 BigInteger（小端、带符号；但我们已加 0x00，故为非负）
        Return New BigInteger(le)
    End Function
    ' BigInteger -> hex：非零无前导0；0 返回 "00"
    Private Function BigIntegerToHexNoLeadingZero(x As BigInteger) As String
        If x.IsZero Then Return "00"

        Dim be = ToBigEndianUnsignedBytes(x) ' 最小长度大端
        Dim sb As New System.Text.StringBuilder(be.Length * 2)
        For Each b In be
            sb.Append(b.ToString("X2"))
        Next
        ' 去掉前导 '0'，保证非零时无前导0
        Dim hex = sb.ToString().TrimStart("0"c)
        If hex.Length = 0 Then hex = "00" ' 理论不会触发，但稳妥起见
        Return hex
    End Function

    ' 无符号大端字节序（最小长度表示）
    Private Function ToBigEndianUnsignedBytes(x As BigInteger) As Byte()
        If x = 0 Then Return New Byte() {0}
        Dim le = x.ToByteArray() ' 小端、二补码
        If le.Length >= 2 AndAlso le(le.Length - 1) = 0 AndAlso (le(le.Length - 2) And &H80) = 0 Then
            Array.Resize(le, le.Length - 1)
        End If
        Array.Reverse(le)
        Return le
    End Function

    ' =========================
    ' 六种转换（按你要求的命名）
    ' =========================

    ' 1) Decimal -> Hex（纯进制，支持大数）
    ' dec 字符串 -> hex（无符号，大端；非零无前导0；0 返回 "00"）
    Private Function ConvertDecimalToHex(decStr As String) As String
        If decStr Is Nothing Then Throw New ArgumentNullException(NameOf(decStr))
        Dim clean = decStr.Trim().Replace("_", "").Replace(",", "")

        Dim n As BigInteger
        If Not BigInteger.TryParse(clean, NumberStyles.Integer, CultureInfo.InvariantCulture, n) Then
            Throw New FormatException(CStr(Loc.T("Text.9a65a895", "无效的十进制数字。")))
        End If
        If n.Sign < 0 Then
            Throw New ArgumentOutOfRangeException(NameOf(decStr), CStr(Loc.T("Text.efe09ab2", "仅支持无符号（非负）十进制。")))
        End If

        Return BigIntegerToHexNoLeadingZero(n)
    End Function

    ' 2) Hex -> Decimal（纯进制，支持大数）
    Private Function ConvertHexToDecimal(hexStr As String) As String
        Dim n = ParseHexAsUnsignedBigInteger(hexStr)
        Return n.ToString()
    End Function

    ' 3) Decimal -> Base64（按规范：dec→hex→bytes→base64）
    Private Function ConvertDecimalToBase64(decStr As String) As String
        Dim bytesBE = HexToBytes(ConvertDecimalToHex(decStr))
        Dim s = Convert.ToBase64String(bytesBE) ' 自动标准 padding
        Return If(hasEq, s, RemoveBase64Padding(s))
    End Function

    ' 4) Base64 -> Decimal（按规范：base64→bytes→hex→dec，实作直接 bytes→BigInteger）
    Private Function ConvertBase64ToDecimal(b64 As String) As String
        Dim s = NormalizeBase64(b64)
        Dim bytesBE = Convert.FromBase64String(AddBase64Padding(s))
        Return ConvertHexToDecimal(BytesToHex(bytesBE))
    End Function

    ' 5) Hex -> Base64（字节流转换：hex→bytes→base64）
    Private Function ConvertHexToBase64(hexStr As String) As String
        Dim bytesBE = HexToBytes(hexStr)
        Dim s = Convert.ToBase64String(bytesBE) ' 自动标准 padding
        Return If(hasEq, s, RemoveBase64Padding(s))
    End Function

    ' 6) Base64 -> Hex（字节流转换：base64→bytes→hex）
    Private Function ConvertBase64ToHex(b64 As String) As String
        Dim s = NormalizeBase64(b64)
        Dim bytesBE = Convert.FromBase64String(AddBase64Padding(s))
        Return BytesToHex(bytesBE)
    End Function



    ' ButtonReverse 点击事件：翻转 Hex 字符串
    Private Sub ButtonReverse_Click(sender As Object, e As EventArgs) Handles ButtonReverse.Click
        Dim originalText As String = TextN.Text
        Dim reversedText As String = ""

        ' 每2个字符为一组进行翻转
        For i As Integer = 0 To originalText.Length - 2 Step 2
            reversedText = originalText.Substring(i, 2) & reversedText
        Next

        TextN.Text = reversedText
    End Sub



    Private Sub ChangeComboFormatText(formatStr)
        '修正识别错过的格式文字 不改变N的实际内容格式
        isAutoChanging = True ' 禁止触发事件
        ComboFormat.SelectedItem = formatStr
        previousFormat = ComboFormat.SelectedItem.ToString()
        isAutoChanging = False ' 恢复事件触发
    End Sub
    ' 1) 根据长度补齐 Base64 末尾的 '='
    Public Function AddBase64Padding(ByVal b64 As String) As String
        If b64 Is Nothing Then Return Nothing
        Dim s As String = b64.Trim()
        Dim r As Integer = s.Length Mod 4
        Select Case r
            Case 0
                Return s                 ' 已经对齐，无需填充
            Case 2
                Return s & "=="          ' 余 2 -> 补 2 个 '='
            Case 3
                Return s & "="           ' 余 3 -> 补 1 个 '='
            Case 1
                Throw New ArgumentException(CStr(Loc.T("Text.98ad608a", "无效的 Base64 长度（len % 4 = 1），无法仅靠填充 '=' 修复。")))
            Case Else
                Return s                 ' 理论上不会走到这里
        End Select
    End Function

    ' 2) 去除 Base64 末尾的 '='（仅修剪尾部，不影响中间字符）
    Public Function RemoveBase64Padding(ByVal b64 As String) As String
        If b64 Is Nothing Then Return Nothing
        Return b64.Trim().TrimEnd("="c)
    End Function


    Private Sub ButtonIsHexUpper_Click(sender As Object, e As EventArgs) Handles ButtonIsHexUpper.Click
        ChangeComboFormatText("HEX")
    End Sub
    Private Sub ButtonIsHexlower_Click(sender As Object, e As EventArgs) Handles ButtonIsHexlower.Click
        ChangeComboFormatText("hex")
    End Sub

    Private Sub ButtonIsDecimal_Click(sender As Object, e As EventArgs) Handles ButtonIsDecimal.Click
        ChangeComboFormatText("Decimal")
    End Sub

    Private Sub ButtonIsBase64_Click(sender As Object, e As EventArgs) Handles ButtonIsBase64.Click
        ChangeComboFormatText("Base64")
    End Sub
    ' 调用入口：LogRich(RichTextLog, CStr(Loc.T("Text.767d8bae", "文本…"))) 
    ' 传 Nothing 或空串 => 清空
    Public Sub LogRich(rtb As RichTextBox, Optional markup As String = Nothing, Optional append As Boolean = True)
        If rtb Is Nothing Then Return

        If rtb.InvokeRequired Then
            rtb.BeginInvoke(Sub() LogRich(rtb, markup, append))
            Return
        End If

        rtb.WordWrap = True

        If String.IsNullOrEmpty(markup) Then
            rtb.Clear()
            Return
        End If

        If Not append Then rtb.Clear()

        RenderMarkupIntoRtb(rtb, markup)

        ' 自动滚动到末尾
        rtb.SelectionStart = rtb.TextLength
        rtb.ScrollToCaret()
    End Sub

    ' =============== 下面是内部实现 ===============

    Private Class StyleFrame
        Public Fore As Color
        Public Bold As Boolean
        Public SizePt As Single
        Public TagName As String ' "ROOT" / "COLOR" / "B" / "SIZE"
    End Class

    Private Sub RenderMarkupIntoRtb(rtb As RichTextBox, markup As String)
        Dim baseFont = rtb.Font
        Dim baseFore = rtb.ForeColor

        Dim stack As New Stack(Of StyleFrame)
        stack.Push(New StyleFrame With {.Fore = baseFore, .Bold = baseFont.Bold, .SizePt = baseFont.SizeInPoints, .TagName = "ROOT"})

        Dim tagRe As New Regex("<(?<close>/)?(?<name>B|COLOR|SIZE)(?:=(?<arg>[^>]+))?>", RegexOptions.IgnoreCase)
        Dim pos As Integer = 0

        For Each m As Match In tagRe.Matches(markup)
            ' 1) 输出标签前的纯文本
            If m.Index > pos Then
                AppendStyledText(rtb, markup.Substring(pos, m.Index - pos), stack.Peek(), baseFont)
            End If

            ' 2) 处理标签
            Dim name = m.Groups("name").Value.ToUpperInvariant()
            Dim isClose = m.Groups("close").Success

            If Not isClose Then
                Dim top = stack.Peek()
                Dim cur As New StyleFrame With {.Fore = top.Fore, .Bold = top.Bold, .SizePt = top.SizePt, .TagName = name}

                Select Case name
                    Case "B"
                        cur.Bold = True
                    Case "COLOR"
                        Dim arg = m.Groups("arg").Value.Trim()
                        cur.Fore = ParseColor(arg, top.Fore)
                    Case "SIZE"
                        Dim arg = m.Groups("arg").Value.Trim()
                        Dim sz As Single
                        If Single.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, sz) AndAlso sz > 0F Then
                            cur.SizePt = sz
                        End If
                End Select
                stack.Push(cur)
            Else
                ' 关闭标签：弹到匹配的那个（容错）
                While stack.Count > 1 AndAlso stack.Peek().TagName <> name
                    stack.Pop()
                End While
                If stack.Count > 1 AndAlso stack.Peek().TagName = name Then
                    stack.Pop()
                End If
            End If

            pos = m.Index + m.Length
        Next

        ' 收尾输出末段
        If pos < markup.Length Then
            AppendStyledText(rtb, markup.Substring(pos), stack.Peek(), baseFont)
        End If
    End Sub

    Private Sub AppendStyledText(rtb As RichTextBox, text As String, st As StyleFrame, baseFont As Font)
        If String.IsNullOrEmpty(text) Then Return
        Dim start = rtb.TextLength
        rtb.SelectionStart = start
        rtb.SelectionLength = 0

        ' 构造字体（仅控制粗体与字号，保留原字体家族）
        Dim fs As FontStyle = If(st.Bold, FontStyle.Bold, FontStyle.Regular)
        Dim f As New Font(baseFont.FontFamily, st.SizePt, fs, GraphicsUnit.Point)

        rtb.SelectionColor = st.Fore
        rtb.SelectionFont = f
        rtb.AppendText(text)
    End Sub

    Private Function ParseColor(arg As String, fallback As Color) As Color
        If String.IsNullOrWhiteSpace(arg) Then Return fallback
        arg = arg.Trim()

        ' 支持 #RRGGBB / #RGB
        If arg.StartsWith("#") Then
            Dim hex = arg.Substring(1)
            If hex.Length = 3 Then
                hex = New String({hex(0), hex(0), hex(1), hex(1), hex(2), hex(2)})
            End If
            If hex.Length = 6 AndAlso Regex.IsMatch(hex, "^[0-9A-Fa-f]{6}$") Then
                Dim r = Convert.ToInt32(hex.Substring(0, 2), 16)
                Dim g = Convert.ToInt32(hex.Substring(2, 2), 16)
                Dim b = Convert.ToInt32(hex.Substring(4, 2), 16)
                Return Color.FromArgb(r, g, b)
            End If
            Return fallback
        End If

        ' 常见颜色名（系统已内置很多），不区分大小写
        Dim c = Color.FromName(arg)
        If c.IsKnownColor OrElse c.IsNamedColor Then Return c

        ' 尝试 html 解析（包含一些别名）
        Try
            Dim html = ColorTranslator.FromHtml(arg)
            If html.A > 0 Then Return html
        Catch
        End Try

        Return fallback
    End Function
    ' 按要求变换 N 并输出到日志
    Public Sub MutateNAndLog(
    rtb As RichTextBox,
    currentText As String,
    currentFormat As String,           ' "Hex" / "Decimal" / "Base64"
    startIndex As Integer,             ' 1-based
    direction As TryDirection,         ' Forward/Backward/CurrentOnly
    treatHexAsHexString As Boolean,    ' 是否按字符串操作（如果 False, 按字节流）
    nibbleOnly As Boolean,             ' 是否仅尝试半字节
    maxPerPos As Integer,              ' 每个位置的最大变换次数
    maxTotal As Integer)               ' 总计变换次数限制

        If String.IsNullOrEmpty(currentText) Then Return
        Dim text = currentText
        Dim len = text.Length
        If startIndex < 1 Then startIndex = 1
        If startIndex > len Then startIndex = len

        ' 遍历顺序
        Dim positions As IEnumerable(Of Integer)
        Select Case direction
            Case TryDirection.Forward
                positions = Enumerable.Range(startIndex, len - startIndex + 1)
            Case TryDirection.Backward
                positions = Enumerable.Range(1, startIndex).Reverse()
            Case Else ' CurrentOnly
                positions = {startIndex}
        End Select

        Dim totalEmitted As Integer = 0

        ' 逐位置尝试
        For Each pos In positions
            Dim origCh As Char = text(pos - 1)
            Dim emitted As Integer = 0


            If String.Equals(currentFormat, "HEX", StringComparison.Ordinal) _
                 OrElse String.Equals(currentFormat, "hex", StringComparison.Ordinal) Then
                ' 如果 treatHexAsHexString=True，按字符串处理（每字符逐个变换）
                If treatHexAsHexString Then
                    '分支测试通过
                    ' 获取该字符的大小写类型（大写/小写）
                    Dim isLower = Char.IsLower(origCh)
                    Dim allowed = If(isLower, "abcdef", "ABCDEF") & "0123456789"

                    If nibbleOnly Then
                        ' 对该字符的 ASCII 做半字节替换，结果必须仍是 Base64 有效字符（不处理 '='）
                        For Each cand In NibbleMutationsAscii(origCh, allowed)
                            ' 避免把 '=' 加出来（只允许标准字符集）
                            If cand = "="c Then Continue For
                            Dim candidate = text.Remove(pos - 1, 1).Insert(pos - 1, cand)
                            LogRich(rtb, "[Hex string nibble] " & MarkAt(candidate, pos) & vbCrLf)
                            emitted += 1
                            totalEmitted += 1
                            If emitted >= maxPerPos OrElse totalEmitted >= maxTotal Then Exit For
                        Next
                    Else
                        For Each c In allowed
                            If c = origCh Then Continue For
                            Dim candidate = text.Remove(pos - 1, 1).Insert(pos - 1, c)
                            LogRich(rtb, "[Hex string] " & MarkAt(candidate, pos) & vbCrLf)
                            emitted += 1
                            totalEmitted += 1
                            If emitted >= maxPerPos OrElse totalEmitted >= maxTotal Then Exit For
                        Next
                    End If
                Else
                    If nibbleOnly Then
                        Dim isLower = Char.IsLower(origCh)
                        Dim allowed = If(isLower, "abcdef", "ABCDEF") & "0123456789"
                        For Each c In allowed
                            If c = origCh Then Continue For
                            Dim candidate = text.Remove(pos - 1, 1).Insert(pos - 1, c)
                            LogRich(rtb, "[Hex nibble] " & MarkAt(candidate, pos) & vbCrLf)
                            emitted += 1
                            totalEmitted += 1
                            If emitted >= maxPerPos OrElse totalEmitted >= maxTotal Then Exit For
                        Next
                    Else
                        ' 将 Hex 视为字节流（每 2 个字符一组）处理
                        Dim posAdjusted = (pos - 1) \ 2 ' 自动调整位置为字节流
                        Dim hexByte = text.Substring(posAdjusted * 2, 2) ' 获取 2 位字节流
                        Dim firstByteAllowed As IEnumerable(Of String)

                        ' 如果是第一个字节，允许 01 到 FF（不允许 00）
                        If posAdjusted = 0 Then
                            firstByteAllowed = Enumerable.Range(1, 255).Select(Function(i) i.ToString("X2"))
                        Else
                            ' 后续字节允许 00 到 FF
                            firstByteAllowed = Enumerable.Range(0, 256).Select(Function(i) i.ToString("X2"))
                        End If

                        ' 遍历允许的值并变换，标红变换的位置
                        For Each b In firstByteAllowed
                            ' 确保替换的是 2 位字节，不会发生不合法的溢出
                            Dim candidate = text.Remove(posAdjusted * 2, 2).Insert(posAdjusted * 2, b.ToString())
                            ' 确保替换后的字符串格式正确
                            If candidate.Length = text.Length Then
                                LogRich(rtb, "[Hex byte] " & MarkAt(candidate, pos) & vbCrLf)
                                emitted += 1
                                totalEmitted += 1
                            End If
                            If emitted >= maxPerPos OrElse totalEmitted >= maxTotal Then Exit For
                        Next
                    End If
                End If
            ElseIf currentFormat = "Decimal" Then
                '分支测试通过
                If nibbleOnly Then
                    ' 对该字符的 ASCII 做半字节替换（高/低 4 位），结果必须仍是十进制字符
                    For Each cand In NibbleMutationsAscii(origCh, DECSET)
                        Dim candidate = text.Remove(pos - 1, 1).Insert(pos - 1, cand)
                        LogRich(rtb, "[Dec nibble] " & MarkAt(candidate, pos) & vbCrLf)
                        emitted += 1
                        totalEmitted += 1
                        If emitted >= maxPerPos OrElse totalEmitted >= maxTotal Then Exit For
                    Next
                Else
                    For Each c In DECSET
                        If c = origCh Then Continue For
                        Dim candidate = text.Remove(pos - 1, 1).Insert(pos - 1, c)
                        LogRich(rtb, "[Dec] " & MarkAt(candidate, pos) & vbCrLf)
                        emitted += 1
                        totalEmitted += 1
                        If emitted >= maxPerPos OrElse totalEmitted >= maxTotal Then Exit For
                    Next
                End If

            ElseIf currentFormat = "Base64" Then
                '分支测试通过 Base64编码解码问题容后 结尾A的问题
                If nibbleOnly Then
                    ' 对该字符的 ASCII 做半字节替换，结果必须仍是 Base64 有效字符（不处理 '='）
                    Dim allowed = B64SET
                    For Each cand In NibbleMutationsAscii(origCh, allowed)
                        ' 避免把 '=' 加出来（只允许标准字符集）
                        If cand = "="c Then Continue For
                        Dim candidate = text.Remove(pos - 1, 1).Insert(pos - 1, cand)
                        LogRich(rtb, "[B64 nibble] " & MarkAt(candidate, pos) & vbCrLf)
                        emitted += 1
                        totalEmitted += 1
                        If emitted >= maxPerPos OrElse totalEmitted >= maxTotal Then Exit For
                    Next
                Else
                    For Each c In B64SET
                        If c = origCh Then Continue For
                        Dim candidate = text.Remove(pos - 1, 1).Insert(pos - 1, c)
                        LogRich(rtb, "[B64] " & MarkAt(candidate, pos) & vbCrLf)
                        emitted += 1
                        totalEmitted += 1
                        If emitted >= maxPerPos OrElse totalEmitted >= maxTotal Then Exit For
                    Next
                End If
            End If

            ' 总计限制
            If totalEmitted >= maxTotal Then Exit For
        Next
    End Sub

    Public Function MutateN(
   rtb As RichTextBox,
   currentText As String,
   currentFormat As String,           ' "Hex" / "Decimal" / "Base64"
   startIndex As Integer,             ' 1-based
   direction As TryDirection,         ' Forward/Backward/CurrentOnly
   treatHexAsHexString As Boolean,    ' 是否按字符串操作（如果 False, 按字节流）
   nibbleOnly As Boolean,             ' 是否仅尝试半字节
   maxPerPos As Integer,              ' 每个位置的最大变换次数
   maxTotal As Integer) As List(Of String) ' ← 改为返回候选列表
        '---------------------------------------------
        ' 修改点：
        ' 1) 将 Sub 改为 Function，返回 List(Of String)
        ' 2) 所有 LogRich(...) 注释掉
        ' 3) 每次生成 candidate 时 results.Add(candidate)
        '---------------------------------------------
        Dim results As New List(Of String)()

        If String.IsNullOrEmpty(currentText) Then Return results
        Dim text = currentText
        Dim len = text.Length
        If startIndex < 1 Then startIndex = 1
        If startIndex > len Then startIndex = len

        ' 遍历顺序
        Dim positions As IEnumerable(Of Integer)
        Select Case direction
            Case TryDirection.Forward
                positions = Enumerable.Range(startIndex, len - startIndex + 1)
            Case TryDirection.Backward
                positions = Enumerable.Range(1, startIndex).Reverse()
            Case Else ' CurrentOnly
                positions = {startIndex}
        End Select

        Dim totalEmitted As Integer = 0

        ' 逐位置尝试
        For Each pos In positions
            Dim origCh As Char = text(pos - 1)
            Dim emitted As Integer = 0

            If String.Equals(currentFormat, "HEX", StringComparison.Ordinal) _
            OrElse String.Equals(currentFormat, "hex", StringComparison.Ordinal) Then

                If treatHexAsHexString Then
                    ' 按 Hex 字符串处理
                    Dim isLower = Char.IsLower(origCh)
                    Dim allowed = If(isLower, "abcdef", "ABCDEF") & "0123456789"

                    If nibbleOnly Then
                        For Each cand In NibbleMutationsAscii(origCh, allowed)
                            If cand = "="c Then Continue For
                            Dim candidate = text.Remove(pos - 1, 1).Insert(pos - 1, cand)
                            'LogRich(rtb, "[Hex string nibble] " & MarkAt(candidate, pos) & vbCrLf)
                            results.Add(candidate)
                            emitted += 1 : totalEmitted += 1
                            If emitted >= maxPerPos OrElse totalEmitted >= maxTotal Then Exit For
                        Next
                    Else
                        For Each c In allowed
                            If c = origCh Then Continue For
                            Dim candidate = text.Remove(pos - 1, 1).Insert(pos - 1, c)
                            'LogRich(rtb, "[Hex string] " & MarkAt(candidate, pos) & vbCrLf)
                            results.Add(candidate)
                            emitted += 1 : totalEmitted += 1
                            If emitted >= maxPerPos OrElse totalEmitted >= maxTotal Then Exit For
                        Next
                    End If
                Else
                    If nibbleOnly Then
                        Dim isLower = Char.IsLower(origCh)
                        Dim allowed = If(isLower, "abcdef", "ABCDEF") & "0123456789"
                        For Each c In allowed
                            If c = origCh Then Continue For
                            Dim candidate = text.Remove(pos - 1, 1).Insert(pos - 1, c)
                            'LogRich(rtb, "[Hex nibble] " & MarkAt(candidate, pos) & vbCrLf)
                            results.Add(candidate)
                            emitted += 1 : totalEmitted += 1
                            If emitted >= maxPerPos OrElse totalEmitted >= maxTotal Then Exit For
                        Next
                    Else
                        ' 将 Hex 视为字节流（每 2 个字符一组）处理
                        Dim posAdjusted = (pos - 1) \ 2
                        Dim hexByte = text.Substring(posAdjusted * 2, 2)

                        Dim firstByteAllowed As IEnumerable(Of String)
                        If posAdjusted = 0 Then
                            firstByteAllowed = Enumerable.Range(1, 255).Select(Function(i) i.ToString("X2"))
                        Else
                            firstByteAllowed = Enumerable.Range(0, 256).Select(Function(i) i.ToString("X2"))
                        End If

                        For Each b In firstByteAllowed
                            Dim candidate = text.Remove(posAdjusted * 2, 2).Insert(posAdjusted * 2, b.ToString())
                            If candidate.Length = text.Length Then
                                'LogRich(rtb, "[Hex byte] " & MarkAt(candidate, pos) & vbCrLf)
                                results.Add(candidate)
                                emitted += 1 : totalEmitted += 1
                            End If
                            If emitted >= maxPerPos OrElse totalEmitted >= maxTotal Then Exit For
                        Next
                    End If
                End If

            ElseIf currentFormat = "Decimal" Then
                If nibbleOnly Then
                    For Each cand In NibbleMutationsAscii(origCh, DECSET)
                        Dim candidate = text.Remove(pos - 1, 1).Insert(pos - 1, cand)
                        'LogRich(rtb, "[Dec nibble] " & MarkAt(candidate, pos) & vbCrLf)
                        results.Add(candidate)
                        emitted += 1 : totalEmitted += 1
                        If emitted >= maxPerPos OrElse totalEmitted >= maxTotal Then Exit For
                    Next
                Else
                    For Each c In DECSET
                        If c = origCh Then Continue For
                        Dim candidate = text.Remove(pos - 1, 1).Insert(pos - 1, c)
                        'LogRich(rtb, "[Dec] " & MarkAt(candidate, pos) & vbCrLf)
                        results.Add(candidate)
                        emitted += 1 : totalEmitted += 1
                        If emitted >= maxPerPos OrElse totalEmitted >= maxTotal Then Exit For
                    Next
                End If

            ElseIf currentFormat = "Base64" Then
                If nibbleOnly Then
                    Dim allowed = B64SET
                    For Each cand In NibbleMutationsAscii(origCh, allowed)
                        If cand = "="c Then Continue For
                        Dim candidate = text.Remove(pos - 1, 1).Insert(pos - 1, cand)
                        'LogRich(rtb, "[B64 nibble] " & MarkAt(candidate, pos) & vbCrLf)
                        results.Add(candidate)
                        emitted += 1 : totalEmitted += 1
                        If emitted >= maxPerPos OrElse totalEmitted >= maxTotal Then Exit For
                    Next
                Else
                    For Each c In B64SET
                        If c = origCh Then Continue For
                        Dim candidate = text.Remove(pos - 1, 1).Insert(pos - 1, c)
                        'LogRich(rtb, "[B64] " & MarkAt(candidate, pos) & vbCrLf)
                        results.Add(candidate)
                        emitted += 1 : totalEmitted += 1
                        If emitted >= maxPerPos OrElse totalEmitted >= maxTotal Then Exit For
                    Next
                End If
            End If

            If totalEmitted >= maxTotal Then Exit For
        Next

        Return results
    End Function

    ' 对比原始文本与变更后的文本，并标记差异部分
    Private Function compareAndMarkRed(originalText As String, candidateText As String) As String
        If String.IsNullOrEmpty(originalText) OrElse String.IsNullOrEmpty(candidateText) Then
            Return candidateText
        End If

        Dim result As New StringBuilder()
        Dim maxLength = Math.Max(originalText.Length, candidateText.Length)

        Dim i As Integer = 0
        While i < maxLength
            Dim originalChar As Char = If(i < originalText.Length, originalText(i), ChrW(0))
            Dim candidateChar As Char = If(i < candidateText.Length, candidateText(i), ChrW(0))

            If (originalChar) = (candidateChar) Then
                ' 如果相同，直接添加原字符
                result.Append(candidateChar)
            Else
                ' 如果不同，标红加粗
                result.Append("<COLOR=red><B>" & candidateChar & "</B></COLOR>")
            End If
            i += 1
        End While

        Return result.ToString()
    End Function

    ' 在 pos 位置把字符包上红色加粗标签
    Private Function MarkAt(s As String, Optional pos1Based As Integer = 0) As String
        If String.IsNullOrEmpty(s) Then Return s
        'If pos1Based < 1 OrElse pos1Based > s.Length Then Return s
        'Dim i = pos1Based - 1
        'Return s.Substring(0, i) & "<COLOR=red><B>" & s(i) & "</B></COLOR>" & s.Substring(i + 1)
        Return compareAndMarkRed(TextN.Text, s)
    End Function
    ' 对单个 ASCII 字符做“半字节”替换：
    ' - 改高 4 位或低 4 位（两类都尝试）
    ' - 产出的新字符需在 allowed 集合内才返回
    Private Function NibbleMutationsAscii(orig As Char, allowedSet As String) As IEnumerable(Of Char)
        Dim results As New List(Of Char)
        Dim b As Integer = AscW(orig) And &HFF
        Dim hi = (b >> 4) And &HF
        Dim lo = b And &HF

        ' 改高 4 位
        For v = 0 To 15
            If v = hi Then Continue For
            Dim nb = ((v And &HF) << 4) Or lo
            Dim ch = ChrW(nb)
            If allowedSet.IndexOf(ch) >= 0 AndAlso ch <> orig Then results.Add(ch)
        Next
        ' 改低 4 位
        For v = 0 To 15
            If v = lo Then Continue For
            Dim nb = (hi << 4) Or (v And &HF)
            Dim ch = ChrW(nb)
            If allowedSet.IndexOf(ch) >= 0 AndAlso ch <> orig Then results.Add(ch)
        Next

        Return results.Distinct()
    End Function



    Private Sub ButtonClearLog_Click(sender As Object, e As EventArgs) Handles ButtonClearLog.Click
        LogRich(RichTextLog)
    End Sub

    Private Sub ButtonHelp_Click(sender As Object, e As EventArgs) Handles ButtonHelp.Click
        Dim readme As String = (CStr(Loc.T("Text.help", README_MARKUP)))
        LogRich(RichTextLog)
        LogRich(RichTextLog, readme)

        ' 将富文本框滚动到第一行
        RichTextLog.SelectionStart = 0 ' 定位到文本的起始位置（第0个字符）
        RichTextLog.ScrollToCaret()    ' 滚动到光标所在位置（此时光标在第一行）

    End Sub

    ' 计算标准 CRC32（与库内实现一致，用于测试）
    Private ReadOnly CrcTable As UInteger() = BuildCrcTable()
    Private Function BuildCrcTable() As UInteger()
        Dim poly As UInteger = &HEDB88320UI
        Dim t(255) As UInteger
        For i As Integer = 0 To 255
            Dim c As UInteger = CUInt(i)
            For j As Integer = 0 To 7
                If (c And 1UI) <> 0UI Then c = (c >> 1) Xor poly Else c >>= 1
            Next
            t(i) = c
        Next
        Return t
    End Function
    Private Function Crc32(data As Byte()) As UInteger
        Dim c As UInteger = &HFFFFFFFFUI
        For i As Integer = 0 To data.Length - 1
            Dim idx As Integer = CInt((c Xor data(i)) And &HFFUI)
            c = CrcTable(idx) Xor (c >> 8)
        Next
        Return c Xor &HFFFFFFFFUI
    End Function
    Private Sub TextN_TextChanged(sender As Object, e As EventArgs) Handles TextN.TextChanged
        NumericUpDownReplacePosition.Maximum = Len(TextN.Text)
        AppGlobals.g_InputStr_rnd = Nothing '掩码
        AppGlobals.g_SavedMask = Nothing '随机填充种子
        AppGlobals.g_frozenRanges = Nothing         '根据掩码生成的冻结区
        TextBoxFrozenRanges.Text = ""
    End Sub
    ' 递归返回所有子控件
    Public Iterator Function Descendants(root As Control) As IEnumerable(Of Control)
        For Each c As Control In root.Controls
            Yield c
            For Each cc In Descendants(c)
                Yield cc
            Next
        Next
    End Function


    Private Async Sub ButtonTry_Click(sender As Object, e As EventArgs) Handles ButtonTry.Click
        SavePanelOptionToIni(PanelOption)  ' section 默认 "PanelOption"，扩展名默认 ".ini"
        If Trim(TextN.Text) = "" Then
            MsgBox(CStr(Loc.T("Text.748f3c02", "先粘贴原始数据，再尝试点击 开始 按钮")))
            Exit Sub
        End If
        AppGlobals.g_needChars=UiSafe .GetControlValue (NumericUpDownNeedChars)

        ButtonTry.Enabled = False
        ButtonStop.Enabled = True

        PanelOption.Enabled = False
        PanelOption.Refresh()
        LogRich(RichTextLog)
        RichTextLog.Refresh()

        Dim mode As Int16 = TabControl1.SelectedIndex
        Dim optionStr As String
        '识别选项设置
        '尝试替换位置(Of Decimal)
        Dim startIndex As Long = UiSafe.GetControlValue(NumericUpDownReplacePosition)

        Dim direction As TryDirection
        Select Case UiSafe.GetControlValue(ComboBoxReplacePosition)
            Case CStr(Loc.T("Text.4373dbf9", "自动向左侧尝试"))
                direction = TryDirection.Backward
            Case CStr(Loc.T("Text.105d3b0c", "自动向右侧尝试"))
                direction = TryDirection.Forward
            Case Else
                direction = TryDirection.CurrentOnly
        End Select
        Dim treatHexAsHexString As Boolean = UiSafe.GetControlValue(CheckBoxIsHexString)
        '仅尝试半字节替换
        Dim nibbleOnly As Boolean = UiSafe.GetControlValue(CheckBoxNibble)


        Dim frozenRanges As String = UiSafe.GetControlValue(TextBoxFrozenRanges)
        Dim unicodeMode As Boolean = UiSafe.GetControlValue(CheckBoxUnicode)
        Dim allowedChars As String = UiSafe.GetControlValue(ComboBoxAllowedChars)
        LogRich(RichTextLog, CStr(Loc.T("Text.5a20ef9c", "<B>变换调整模式：</B>")))
        If (mode = 0) Then
            '微调字节模式
            LogRich(RichTextLog, CStr(Loc.T("Text.0fbfd6e1", "<B><color=blue>微调字节模式</color></B>")) & vbCrLf)


            optionStr = CStr(Loc.T("Text.97fbdf69", "仅尝试半字节替换=")) & nibbleOnly & vbCrLf
            optionStr = optionStr & CStr(Loc.T("Text.999a065f", "尝试替换位置(Decimal)=")) & startIndex & vbCrLf
            optionStr = optionStr & CStr(Loc.T("Text.38ffa7e4", "尝试方向=")) & UiSafe.GetControlValue(ComboBoxReplacePosition) & vbCrLf

        Else
            '固定CRC32模式
            If unicodeMode Then
                LogRich(RichTextLog, CStr(Loc.T("Text.4b70538d", "<B><color=blue>固定CRC32模式(Unicode)</color></B>")) & vbCrLf)
            Else
                LogRich(RichTextLog, CStr(Loc.T("Text.94870247", "<B><color=blue>固定CRC32模式</color></B>")) & vbCrLf)
            End If

            optionStr = CStr(Loc.T("Text.b79a498a", "不可改变区域=")) & frozenRanges & vbCrLf
            optionStr = optionStr & CStr(Loc.T("Text.2321c43c", "Unicode字符串=")) & unicodeMode & vbCrLf
            optionStr = optionStr & CStr(Loc.T("Text.534adbe5", "允许字符=")) & allowedChars & vbCrLf

            If Not AppGlobals.g_SavedMask = Nothing Then '掩码 
                optionStr = optionStr & CStr(Loc.T("Text.767b8718", "字符串掩码=")) & AppGlobals.g_SavedMask & vbCrLf
            End If
            If Not AppGlobals.g_InputStr_rnd = Nothing Then '随机填充种子
                optionStr = optionStr & CStr(Loc.T("Text.35e6295e", "字符串种子=")) & AppGlobals.g_InputStr_rnd & vbCrLf

            End If
        End If

        LogRich(RichTextLog, optionStr)
        Dim ok As Boolean = (MsgBox(CStr(Loc.T("Text.00868f85", "请确认参数：")) & vbCrLf & vbCrLf & RichTextLog.Text & vbCrLf & vbCrLf & CStr(Loc.T("Text.b8e50b30", "是否继续？")), MsgBoxStyle.OkCancel Or MsgBoxStyle.Question Or MsgBoxStyle.DefaultButton2, CStr(Loc.T("Text.40e9b687", "执行前确认"))) = MsgBoxResult.Ok)
        If Not ok Then
            ButtonStop.PerformClick()
            GoTo ExitIt

        End If

        Dim varsListAll As List(Of String)
        Dim varsList As List(Of String)

        resultCount = UiSafe.GetControlValue(NumericUpDownResultCount)
        reTryCount = 16

        Dim isDryRun As Boolean = UiSafe.GetControlValue(CheckBoxDryRun)
        If isDryRun Then
            LogRich(RichTextLog, CStr(Loc.T("Text.318ba4e9", "<color=red>演练（Dry Run）模式，只打印部分变换结果，不进行分解尝试。</color>")) & vbCrLf)
            LogRich(RichTextLog, CStr(Loc.T("Text.f46b6f4c", "<color=green>演练（Dry Run）模式，开始执行变换调整。。。。</color>")) & vbCrLf)
        End If
        Try
            RichTextLog.Refresh()

            If (mode = 0) Then
                LogRich(RichTextLog, CStr(Loc.T("Text.6a36ea3f", "<B>微调字节模式</B>")) & vbCrLf)
                RichTextLog.Refresh()
                '微调字节模式
                varsList = MutateN(RichTextLog, TextN.Text, ComboFormat.SelectedItem.ToString(),
                              startIndex:=startIndex,
                              direction:=direction,
                              treatHexAsHexString:=treatHexAsHexString,
                              nibbleOnly:=nibbleOnly,
                              maxPerPos:=256,
                              maxTotal:=4096)
                If isDryRun Then
                    For i As Integer = 0 To varsList.Count - 1
                        LogRich(RichTextLog, MarkAt(varsList(i)) & vbCrLf)
                        If i > 32 Then Exit For
                    Next
                    GoTo ExitIt
                End If


                GlobalFifo.Clear() '清空结果队列
                ThreadHub.SetMaxConcurrent(UiSafe.GetControlValue(NumericUpDownThreads)) '新的并发上限


                StartLoop(CStr(Loc.T("Text.daad030f", "开始工作")))
                ThreadHub.Resume()

                For i As Integer = 0 To varsList.Count - 1

                    Dim dataStr = varsList(i)
                    Dim DecimalStr = getDecimalStr(dataStr)
                    Dim timeOutMs As Integer = UiSafe.GetControlValue(NumericUpDownTimeOutMs)

                    ThreadHub.Enqueue(DirectCast(AddressOf WorkTask, Action(Of String, Integer, CancellationToken)), DecimalStr, timeOutMs)

                Next
                Await Task.Delay(3000, _cts.Token) ' 无阻塞“睡眠”直到被取消
                reTryCount = 0


            Else
                LogRich(RichTextLog, CStr(Loc.T("Text.926168f0", "<B>固定CRC32模式</B>")) & vbCrLf)
                RichTextLog.Refresh()
                '固定CRC32模式

                Dim kindStr As String = "unknown"
                Select Case ComboFormat.SelectedItem.ToString()
                    Case "HEX"
                        If Not UiSafe.GetControlValue(CheckBoxIsHexString) Then
                            kindStr = "bytes"
                        Else
                            kindStr = "HEX"
                        End If
                    Case "hex"
                        If Not UiSafe.GetControlValue(CheckBoxIsHexString) Then
                            kindStr = "bytes"
                        Else
                            kindStr = "hex"
                        End If
                    Case "Decimal"
                        kindStr = "dec"
                    Case "Base64"
                        kindStr = "base64"
                    Case Else
                        GoTo ExitIt

                End Select

                Dim cts As New Threading.CancellationTokenSource()

                Dim newN As String = TextN.Text
                If Not AppGlobals.g_InputStr_rnd = Nothing Then '随机填充种子
                    newN = AppGlobals.g_InputStr_rnd
                ElseIf Not AppGlobals.g_SavedMask = Nothing Then '掩码 
                    newN = OverlayNonStar(newN, AppGlobals.g_SavedMask)
                End If

                If Not newN = TextN.Text Then

                    Dim tries As Integer = 0
                    Dim varsResult As String
                    Do
                        varsResult = Await CrcNeutralVariantGenerator.GenerateZeroDeltaVariantsABAsync(TextN.Text, newN, kindStr, True, unicodeMode, frozenRanges, allowedChars, cts.Token)
                        If Not String.IsNullOrWhiteSpace(varsResult) Then Exit Do ' 有结果就退出
                        tries += 1
                    Loop While tries < 7


                    If varsResult = Nothing Then
                        If Not (MsgBox(CStr(Loc.T("Text.ea4ba433", "掩码字符串无法求解，回退使用原始输入N字符串。是否继续")), MsgBoxStyle.OkCancel Or MsgBoxStyle.Question Or MsgBoxStyle.DefaultButton2, CStr(Loc.T("Text.40e9b687", "执行前确认"))) = MsgBoxResult.Ok) Then
                            GoTo ExitIt
                        End If
                        varsResult = TextN.Text
                    Else
                        newN = varsResult
                    End If
                    'LogRich(RichTextLog, MarkAt(newN) & vbCrLf)
                    LogRich(RichTextLog, CStr(Loc.T("Text.c6c59349", "种子N:")) & MarkAt(varsResult) & vbCrLf)
                    RichTextLog.Refresh()
                End If

                GlobalFifo.Clear() '清空结果队列
                ThreadHub.SetMaxConcurrent(UiSafe.GetControlValue(NumericUpDownThreads)) '新的并发上限


                StartLoop(CStr(Loc.T("Text.daad030f", "开始工作")))
                ThreadHub.Resume()
                While reTryCount > 0 And resultCount > 0

                    varsList = Await CrcNeutralVariantGenerator.GenerateZeroDeltaVariantsAsync(newN, kindStr, 64, True, unicodeMode, frozenRanges, allowedChars, cts.Token)

                    ReconcileLists(varsList, varsListAll)
                    If varsList.Count = 0 Then
                        LogRich(RichTextLog, CStr(Loc.T("Text.a0900ee7", "<color=red>未生成到变体（请增大输入长度或放宽冻结约束）。</color>")))
                        GoTo ExitIt
                    End If

                    If isDryRun Then
                        For i As Integer = 0 To varsList.Count - 1
                            LogRich(RichTextLog, MarkAt(varsList(i)) & vbCrLf)
                            If i > 32 Then Exit For
                        Next
                        GoTo ExitIt
                    End If

                    For i As Integer = 0 To varsList.Count - 1

                        Dim dataStr = varsList(i)
                        Dim DecimalStr = getDecimalStr(dataStr)
                        Dim timeOutMs As Integer = UiSafe.GetControlValue(NumericUpDownTimeOutMs)

                        ThreadHub.Enqueue(DirectCast(AddressOf WorkTask, Action(Of String, Integer, CancellationToken)), DecimalStr, timeOutMs)

                    Next

                    Await Task.Delay(3000, _cts.Token) ' 无阻塞“睡眠”直到被取消

                    reTryCount = reTryCount - 1
                End While
                '_cts.Cancel()


            End If
            GoTo ExitIt

        Catch ex As Exception
            Dim dr = MessageBox.Show(Me,
                         CStr(Loc.T("Text.f9107b43", "抱歉，操作未完成。")) & vbCrLf &
                         CStr(Loc.T("Text.9eb9c871", "原因：")) & ex.Message & vbCrLf & vbCrLf &
                         CStr(Loc.T("Text.d611ed15", "需要查看详细信息吗？")),
                         CStr(Loc.T("Text.6a365d01", "操作失败")),
                         MessageBoxButtons.YesNo,
                         MessageBoxIcon.Error)
            If dr = DialogResult.Yes Then ShowExceptionDetails(Me, ex)
        End Try




ExitIt:
    End Sub
    Private Sub WorkTask(bigNumber As String, ms As Integer, token As CancellationToken)
        Dim endAt As Date = DateTime.UtcNow.AddMilliseconds(ms)
        Try

            If EgwNative.IsProbablePrime(bigNumber) Then
                'LogRich(RichTextLog, CStr(Loc.T("Text.51413f13", "发现素数N")) & vbCrLf)
                If UiSafe.GetControlValue(CheckBoxAcceptPrimeN) Then
                    GlobalFifo.Push(New FactorData(bigNumber, 1, bigNumber, "IsProbablePrime"))
                End If
                Exit Sub
            End If
            If token.IsCancellationRequested Or DateTime.UtcNow > endAt Then GoTo failed

            Dim quotient As String

            If UiSafe.GetControlValue(CheckBoxBrent) Then
                ' 使用 Brent's 因数分解算法
                Dim factor As String = EgwNative.BrentFactorization(bigNumber, UiSafe.GetControlValue(NumericUpDownBrent))
                If factor IsNot Nothing Then
                    'LogRich(RichTextLog, CStr(Loc.T("Text.f928b95f", "发现因数=")) & factor & vbCrLf)
                    quotient = EgwNative.TDivQR(bigNumber, factor).q
                    If EgwNative.IsProbablePrime(quotient) Then
                        GlobalFifo.Push(New FactorData(bigNumber, factor, quotient, "BrentFactorization"))
                        ' LogRich(RichTextLog, CStr(Loc.T("Text.9d0542e9", "发现素数对=")) & factor & " " & quotient & vbCrLf)
                    End If

                    'LogRich(RichTextLog, CStr(Loc.T("Text.39483051", "商是合数")) & vbCrLf）
                    Exit Sub
                End If
            End If

            If token.IsCancellationRequested Or DateTime.UtcNow > endAt Then GoTo failed

            If UiSafe.GetControlValue(CheckBoxSmallPrimes) Then
                '试除法 设置试除范围 [2, 16777216]
                Dim r = ParseRangeOrDefault(UiSafe.GetControlValue(ComboBoxSmallPrimes))
                Dim smallFactor As String = EgwNative.TrialDivisionStr(bigNumber, r.min, r.max)
                If smallFactor IsNot Nothing Then
                    'LogRich(RichTextLog, CStr(Loc.T("Text.f928b95f", "发现因数=")) & smallFactor & vbCrLf)
                    quotient = EgwNative.TDivQR(bigNumber, smallFactor).q
                    If EgwNative.IsProbablePrime(quotient) Then
                        GlobalFifo.Push(New FactorData(bigNumber, smallFactor, quotient, "TrialDivisionStr"))
                        'LogRich(RichTextLog, CStr(Loc.T("Text.9d0542e9", "发现素数对=")) & smallFactor & " " & quotient & vbCrLf)
                    Else
                        'LogRich(RichTextLog, CStr(Loc.T("Text.39483051", "商是合数")) & vbCrLf）
                    End If
                    Exit Sub
                End If
            End If

            If token.IsCancellationRequested Or DateTime.UtcNow > endAt Then GoTo failed

            If UiSafe.GetControlValue(CheckBoxSmallCurve) Then
                '随便来几条曲线试试水 PM1 PP1 ECM
                Dim iii = UiSafe.GetControlValue(NumericUpDownSmallCurve)
                Dim b1List = EgwNative.GetRecommendedB1List(bigNumber, UiSafe.GetControlValue(NumericUpDownSmallCurve))
                For Each item In b1List
                    Dim f1 = EgwNative.PM1_FindFactor(bigNumber, curves:=item.Item2, B1:=item.Item1 / 1000)
                    If Not (String.IsNullOrEmpty(f1)) Then
                        If Not EgwNative.IsProbablePrime(f1) Then
                            Exit Sub
                        End If
                        'LogRich(RichTextLog, CStr(Loc.T("Text.d292d05f", "PM1发现因数=")) & f1 & vbCrLf)
                        quotient = EgwNative.TDivQR(bigNumber, f1).q
                        If EgwNative.IsProbablePrime(quotient) Then
                            GlobalFifo.Push(New FactorData(bigNumber, f1, quotient, "PM1_FindFactor"))
                            'LogRich(RichTextLog, CStr(Loc.T("Text.028ec8be", "PM1发现素数对=")) & f1 & " " & quotient & vbCrLf)
                        Else
                            'LogRich(RichTextLog, CStr(Loc.T("Text.39483051", "商是合数")) & vbCrLf）
                        End If
                        Exit Sub
                    End If

                    If token.IsCancellationRequested Or DateTime.UtcNow > endAt Then GoTo failed


                    Dim f2 = EgwNative.PP1_FindFactor(bigNumber, curves:=item.Item2, B1:=item.Item1 / 1000)
                    If Not (String.IsNullOrEmpty(f2)) Then
                        If Not EgwNative.IsProbablePrime(f2) Then
                            Exit Sub
                        End If
                        'LogRich(RichTextLog, CStr(Loc.T("Text.6afe3d1c", "PP1发现因数=")) & f2 & vbCrLf)
                        quotient = EgwNative.TDivQR(bigNumber, f2).q
                        If EgwNative.IsProbablePrime(quotient) Then
                            GlobalFifo.Push(New FactorData(bigNumber, f2, quotient, "PP1_FindFactor"))
                            'LogRich(RichTextLog, CStr(Loc.T("Text.df865b4d", "PP1发现素数对=")) & f2 & " " & quotient & vbCrLf)
                        Else
                            'LogRich(RichTextLog, CStr(Loc.T("Text.39483051", "商是合数")) & vbCrLf）
                        End If
                        Exit Sub
                    End If

                    If token.IsCancellationRequested Or DateTime.UtcNow > endAt Then GoTo failed


                    Dim f As String = EgwNative.ECM_FindFactor(bigNumber, curves:=item.Item2, B1:=item.Item1 / 20)
                    If Not (String.IsNullOrEmpty(f)) Then
                        If Not EgwNative.IsProbablePrime(f) Then
                            Exit Sub
                        End If

                        'LogRich(RichTextLog, CStr(Loc.T("Text.630509ca", "ECM发现因数=")) & f & vbCrLf)
                        quotient = EgwNative.TDivQR(bigNumber, f).q
                        If EgwNative.IsProbablePrime(quotient) Then
                            GlobalFifo.Push(New FactorData(bigNumber, f, quotient, "ECM_FindFactor"))
                            'LogRich(RichTextLog, CStr(Loc.T("Text.c7c7caa5", "ECM发现素数对=")) & f & " " & quotient & vbCrLf)
                        Else
                            'LogRich(RichTextLog, CStr(Loc.T("Text.39483051", "商是合数")) & vbCrLf）
                        End If
                        Exit Sub
                    End If
                    'LogRich(RichTextLog, item.Item1 & vbCrLf）
                    'RichTextLog.Refresh()

                Next

            End If

failed:
            GlobalFifo.Push(New FactorData(bigNumber, 0, 0, CStr(Loc.T("Text.33d2cf2d", "未找到因数"))))

            If token.IsCancellationRequested Or DateTime.UtcNow > endAt Then Exit Sub





        Catch ex As ThreadInterruptedException
            Throw
        End Try
    End Sub

    Function getDecimalStr(strIn)
        Select Case ComboFormat.SelectedItem.ToString()
            Case "HEX", "hex"
                getDecimalStr = ConvertHexToDecimal(strIn)
            Case "Base64"
                getDecimalStr = ConvertBase64ToDecimal(strIn)
            Case Else '"Decimal" or unknown
                getDecimalStr = strIn
        End Select
    End Function
    Function getStrFromHex(strIn)
        Select Case ComboFormat.SelectedItem.ToString()
            Case "HEX"
                getStrFromHex = strIn.ToUpper()
            Case "hex"
                getStrFromHex = strIn.ToLower()
            Case "Base64"
                getStrFromHex = ConvertHexToBase64(strIn)
            Case "Decimal"
                getStrFromHex = ConvertHexToDecimal(strIn)
            Case Else '"Decimal" or unknown
                getStrFromHex = strIn
        End Select
    End Function
    Private Sub ShowExceptionDetails(owner As IWin32Window, ex As Exception)
        Using f As New Form()
            f.Text = CStr(Loc.T("Text.f98a6cf3", "错误详情"))
            f.StartPosition = FormStartPosition.CenterParent
            f.FormBorderStyle = FormBorderStyle.Sizable
            f.MinimizeBox = False : f.MaximizeBox = True
            f.ClientSize = New Size(720, 420)

            Dim tb As New TextBox() With {
            .Multiline = True, .ReadOnly = True,
            .ScrollBars = ScrollBars.Both, .WordWrap = False,
            .Dock = DockStyle.Fill,
            .Text = ex.ToString()
        }
            Dim copyBtn As New Button() With {.Text = CStr(Loc.T("Text.03c1f929", "复制到剪贴板")), .Dock = DockStyle.Bottom, .Height = 36}
            AddHandler copyBtn.Click, Sub()
                                          Clipboard.SetText(tb.Text)
                                          MessageBox.Show(owner, CStr(Loc.T("Text.8ca050e9", "已复制。")), CStr(Loc.T("Text.b25b7a81", "提示")), MessageBoxButtons.OK, MessageBoxIcon.Information)
                                      End Sub

            f.Controls.Add(tb)
            f.Controls.Add(copyBtn)
            f.ShowDialog(owner)
        End Using
    End Sub

    Private Sub SetLabelInfo(s)
        UiSafe.SetLabelTextSafe(LabelInfo, s)
        UiSafe.RefreshSafe(LabelInfo)
    End Sub


    ' 在主窗体或任意 UI 线程位置调用
    Private Sub btnEditMask_Click(sender As Object, e As EventArgs) Handles btnEditMask.Click
        ' 1) 先准备全局数据
        AppGlobals.g_InputStr = TextN.Text                  ' A 的原始字符串
        'AppGlobals.g_AllowedChars = "0123456789ABCDEF"          ' 可选：允许的字符集
        AppGlobals.g_AllowedChars = UiSafe.GetControlValue(ComboBoxAllowedChars)
        AppGlobals.g_SavedMask = AppGlobals.g_SavedMask         ' 若有历史可保留



        ' 2) 打开模态对话框（阻塞，直到用户 保存/取消 关闭）
        Using dlg As New FrmMaskEditor()
            dlg.ShowInTaskbar = False     ' 对话框不占任务栏
            dlg.TopMost = True            ' （可选）强制前台，盖住其它应用窗口
            ' 传 owner 可确保置顶于本窗体并居中（窗体 Designer 已设置 CenterParent）
            Dim result = dlg.ShowDialog(Me)

            If result = DialogResult.OK Then
                ' 3) 读取结果（已通过长度与字符集校验）
                Dim mask = AppGlobals.g_SavedMask
                ' TODO: 使用 mask
                If Not AppGlobals.g_frozenRanges = Nothing Then
                    Dim sFrozenRanges = TextBoxFrozenRanges.Text
                    Dim isMatch As Boolean = (sFrozenRanges IsNot Nothing) AndAlso sFrozenRanges.StartsWith(AppGlobals.g_frozenRanges & " ", StringComparison.Ordinal)
                    If Not isMatch Then
                        TextBoxFrozenRanges.Text = AppGlobals.g_frozenRanges & " " & TextBoxFrozenRanges.Text
                    End If
                Else
                    ' 用户取消或被校验拦截后未保存
                End If
            End If
        End Using
    End Sub

    Private Sub NumericUpDown1_ValueChanged(sender As Object, e As EventArgs) Handles NumericUpDownNeedChars.ValueChanged

    End Sub

    Private Sub ButtonStop_Click(sender As Object, e As EventArgs) Handles ButtonStop.Click
        ButtonStop.Enabled = False
        StopLoop(CStr(Loc.T("Text.db08d97d", "点击结束")))
        '结束线程
        ThreadHub.ForceStopAll()
        '恢复窗体
        PanelOption.Enabled = True
        ButtonTry.Enabled = True
    End Sub
    ' 返回的结构：字段名按你要求 .min / .max
    Public Structure RangeMM
        Public [min] As ULong
        Public [max] As ULong
    End Structure

    ' 解析 "a-b"；失败或 b<a 时返回默认 2..16777216
    Public Function ParseRangeOrDefault(text As String) As RangeMM
        Dim defMin As ULong = 2
        Dim defMax As ULong = 16777216
        Dim result As New RangeMM With {.[min] = defMin, .[max] = defMax}

        If String.IsNullOrWhiteSpace(text) Then Return result

        Dim s As String = text.Trim()
        ' 支持多种横线分隔符：-  –  —  －
        Dim seps As Char() = {"-"c, "–"c, "—"c, "－"c}
        Dim idx As Integer = s.IndexOfAny(seps)

        ' 分隔符位置不合法 -> 默认
        If idx <= 0 OrElse idx >= s.Length - 1 Then Return result

        Dim left As String = s.Substring(0, idx).Trim()
        Dim right As String = s.Substring(idx + 1).Trim()

        Dim a As ULong, b As ULong
        ' 使用不变区域性解析；仅接受整数
        If Not ULong.TryParse(left, NumberStyles.Integer, CultureInfo.InvariantCulture, a) Then Return result
        If Not ULong.TryParse(right, NumberStyles.Integer, CultureInfo.InvariantCulture, b) Then Return result

        ' 右端小于左端 -> 默认
        If b < a Then Return result

        result.[min] = a
        result.[max] = b
        Return result
    End Function
    ''' <summary>
    ''' 引用传递处理两个字符串列表：
    ''' 1) 删除 a 中出现在 b 里的元素
    ''' 2) 将 a 中 b 不包含的元素按出现顺序追加到 b
    ''' 同时对 b 做去重并尽量保持原有顺序
    ''' </summary>
    ''' <param name="a">输入/输出：将被移除与 b 重合项</param>
    ''' <param name="b">输入/输出：追加 a 中 b 不包含的项</param>
    ''' <param name="comparer">
    '''   可选：字符串比较器（默认 Ordinal，区分大小写）。
    '''   不区分大小写可传：StringComparer.OrdinalIgnoreCase
    ''' </param>
    Public Sub ReconcileLists(ByRef a As List(Of String),
                              ByRef b As List(Of String),
                              Optional comparer As IEqualityComparer(Of String) = Nothing)

        If a Is Nothing Then a = New List(Of String)()
        If b Is Nothing Then b = New List(Of String)()
        If comparer Is Nothing Then comparer = StringComparer.Ordinal

        ' 1) 先对 b 排重并保留顺序
        Dim seen As New HashSet(Of String)(comparer)
        Dim bDistinct As New List(Of String)()
        For Each s In b
            If seen.Add(s) Then bDistinct.Add(s)
        Next

        ' 保存“b 的原始集合”（用于从 a 删除交集）
        Dim setBOriginal As New HashSet(Of String)(bDistinct, comparer)

        ' 2) 遍历 a：过滤掉与 b 相交的，同时收集需要追加到 b 的新元素
        Dim aFiltered As New List(Of String)()
        Dim toAppend As New List(Of String)()

        For Each s In a
            If setBOriginal.Contains(s) Then
                ' 在 b 中已存在：从 a 移除（即不加入 aFiltered）
            Else
                ' 保留在 a 中
                aFiltered.Add(s)
                ' 若 b 尚未包含该元素，则标记为待追加，同时加入 seen 防重
                If seen.Add(s) Then
                    toAppend.Add(s)
                End If
            End If
        Next

        ' 3) 回写（引用传递）
        a = aFiltered
        bDistinct.AddRange(toAppend)
        b = bDistinct
    End Sub



    ' 运行状态 / 重入保护 / 循环进度
    Private Shared IsRunning As Boolean = False
    Private Shared InTick As Boolean = False
    Private Shared StepIndex As Integer = 0
    Private Shared StepMax As Integer = 50

    Private Shared resultCount As Integer
    Private Shared reTryCount As Integer

    ' 明确使用 WinForms 计时器
    Private Shared ReadOnly GlobalTimer As New System.Windows.Forms.Timer() With {
    .Interval = 200, .Enabled = False
}

    Private Sub StartLoop(reason As String)
        GlobalTimer.Start()
        IsRunning = True
        LabelInfo.Text = reason
    End Sub
    Private Sub StopLoop(reason As String)
        GlobalTimer.Stop()
        IsRunning = False
        LabelInfo.Text = reason
    End Sub

    '循环打印结果
    Private Sub ShowResult()
        If Not IsRunning Then Return
        If InTick Then Return ' 防止重入
        InTick = True
        Try
            Dim s As ThreadManager.ThreadManagerStats = ThreadHub.GetStats() ' ← 返回 ThreadManager.ThreadManagerStats

            Dim item As FactorData = Nothing
            While GlobalFifo.TryPop(item) And resultCount > 0
                If item.FactorA = 0 Then
                    '分解失败
                    Dim sb As New System.Text.StringBuilder()

                    sb.AppendLine(New String("="c, 48))

                    sb.AppendLine(MarkAt(getStrFromHex(ConvertDecimalToHex(item.N))) & vbCrLf)
                    sb.AppendLine(CStr(Loc.T("Text.2b8cf85c", "因数分解失败:")) & item.FactorMode & CStr(Loc.T("Text.b7177565", ".如果有必要可以尝试使用 RSATool2 或者其他专业工具尝试分解。")) & vbCrLf)
                    sb.AppendLine("<B><color=red>N:" & ConvertDecimalToHex(item.N) & "</color></B>" & vbCrLf)
                    sb.AppendLine(New String("="c, 48))

                    Dim logForRich As String = sb.ToString()
                    LogRich(RichTextLog, logForRich)
                    If UiSafe.GetControlValue(CheckBoxWriteResultToLog) Then
                        Logger.WriteExeLog(System.Text.RegularExpressions.Regex.Replace(logForRich, "<.*?>", ""))
                    End If

                    Continue While
                End If



                Dim Nhex As String, Dhex As String
                ' 1) 标准 RSA：p=61, q=53, e=65537 (十进制)
                Dim isPassed = TryMakeRsaND_WithFmt(item.FactorA, item.FactorB, UiSafe.GetControlValue(TextE), "ddh", Nhex, Dhex)
                If (isPassed) Then
                    '生成D成功
                    If Not UiSafe.GetControlValue(CheckBoxRsaTest) OrElse (TryRsaBidirectionalSelfTest(Nhex, Dhex, UiSafe.GetControlValue(TextE), fmt:="hhh")) Then
                        Dim sb As New System.Text.StringBuilder()

                        '随机数据加密解密验证通过
                        sb.AppendLine(New String("="c, 48))

                        sb.AppendLine(MarkAt(getStrFromHex(Nhex)) & vbCrLf)
                        sb.AppendLine(CStr(Loc.T("Text.94999e82", "因数分解工具:")) & item.FactorMode & "" & vbCrLf)
                        sb.AppendLine("<B><color=green>P:" & ConvertDecimalToHex(item.FactorA) & "</color></B>")
                        sb.AppendLine("<B><color=blue>Q:" & ConvertDecimalToHex(item.FactorB) & "</color></B>" & vbCrLf)
                        sb.AppendLine("N:" & Nhex & vbCrLf)
                        sb.AppendLine("<B>D:" & Dhex & Dhex & "</B>" & vbCrLf)
                        sb.AppendLine("E:" & UiSafe.GetControlValue(TextE))
                        sb.AppendLine(New String("="c, 48))

                        Dim logForRich As String = sb.ToString()
                        LogRich(RichTextLog, logForRich)
                        If UiSafe.GetControlValue(CheckBoxWriteResultToLog) Then
                            Logger.WriteExeLog(System.Text.RegularExpressions.Regex.Replace(logForRich, "<.*?>", ""))
                        End If

                        resultCount = resultCount - 1
                    End If
                End If
                RichTextLog.Refresh()
            End While
            'Dim s As ThreadManager.ThreadManagerStats = ThreadHub.GetStats() ' ← 返回 ThreadManager.ThreadManagerStats
            'LogRich(RichTextLog, $"[Stats] Q={s.QueueCount} Run={s.RunningCount} Done={s.CompletedTotal} Max={s.MaxConcurrent} Paused={s.IsPaused}")


            If resultCount <= 0 Then
                ButtonStop.PerformClick()
                StopLoop(CStr(Loc.T("Text.74778ee0", "结束，已达结果下限。")))
            End If
            If reTryCount <= 0 Then
                'LogRich(RichTextLog, $"[Stats] Q={s.QueueCount} Run={s.RunningCount} Done={s.CompletedTotal} Max={s.MaxConcurrent} Paused={s.IsPaused}")
                If s.QueueCount = 0 And s.RunningCount = 0 Then

                    ButtonStop.PerformClick()
                    StopLoop(CStr(Loc.T("Text.15fa7de4", "结束，超过重试上限。")))
                End If

            End If

        Catch ex As Exception
            StopLoop("Error: " & ex.Message)
        Finally
            InTick = False
        End Try

    End Sub
    Private Sub frmPrimeKit_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        ' 比如：提示保存 / 阻止关闭
        ' If Not IsSaved Then
        '     If MessageBox.Show(CStr(Loc.T("Text.d24762d5", "未保存，确定要退出？")), CStr(Loc.T("Text.b25b7a81", "提示")), MessageBoxButtons.YesNo) = DialogResult.No Then
        '         e.Cancel = True
        '     End If
        ' End If
        SavePanelOptionToIni(PanelOption)  ' section 默认 "PanelOption"，扩展名默认 ".ini"
    End Sub
    Private Sub ComboBoxLanguage_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBoxLanguage.SelectedIndexChanged
        Dim code As String = TryCast(ComboBoxLanguage.SelectedItem, String)
        If String.IsNullOrWhiteSpace(code) Then Return

        Dim filePath As String = Path.Combine(LangFolder, code & ".json")
        If IsValidLangFile(filePath) Or code = "zh-CN" Then
            Try
                If code = "zh-CN" And Not IsValidLangFile(filePath) Then
                    ' 取消语言应用，恢复默认
                    Loc.DisableAndRestore(Me)
                    Return
                Else
                    Loc.LoadJsonFile(filePath)
                    Loc.ApplyTo(Me)
                End If
            Catch ex As Exception
                MessageBox.Show(Me, $"语言文件应用失败(Language file application failed.)：{ex.Message}", "语言切换 switch languages", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End Try
        Else
            MessageBox.Show(Me, "所选语言文件不存在或格式无效。" & vbCrLf & "The selected language file does not exist or has an invalid format.", "语言切换 switch languages", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End If
    End Sub

    ' ========== 扫描与验证 ==========
    Private Sub RefreshLanguageList()
        ComboBoxLanguage.Items.Clear()

        If Not Directory.Exists(LangFolder) Then
            Directory.CreateDirectory(LangFolder)
            Return
        End If

        Dim jsonFiles = Directory.EnumerateFiles(LangFolder, "*.json", SearchOption.TopDirectoryOnly)
        Dim codes As New List(Of String)

        For Each f In jsonFiles
            If IsValidLangFile(f) Then
                Dim code = Path.GetFileNameWithoutExtension(f)
                If code = "_missing" Then Continue For
                codes.Add(code)
            End If
        Next
        If Not codes.Contains("zh-CN") Then
            codes.Add("zh-CN")
        End If

        codes.Sort(StringComparer.OrdinalIgnoreCase)
        ComboBoxLanguage.Items.AddRange(codes.Cast(Of Object).ToArray())
    End Sub

    Private Function IsValidLangFile(path As String) As Boolean
        If Not File.Exists(path) Then Return False
        Try
            Using doc = JsonDocument.Parse(File.ReadAllText(path))
                Dim root = doc.RootElement
                ' 合法格式1：包含 "strings"（对象，可空）与 "controls"（对象，可空）
                If root.TryGetProperty("strings", Nothing) OrElse root.TryGetProperty("controls", Nothing) Then
                    Return True
                End If
                ' 合法格式2：简单 { key: text } 的扁平对象
                If root.ValueKind = JsonValueKind.Object Then
                    ' 粗检：至少有一个属性是字符串值
                    For Each p In root.EnumerateObject()
                        If p.Value.ValueKind = JsonValueKind.String Then Return True
                    Next
                End If
            End Using
        Catch
            Return False
        End Try
        Return False
    End Function

    ' ========== 启动时应用策略 ==========
    Private Sub ApplyLanguageOnLoad()
        ' 优先：若下拉已有选择且有效，则直接应用
        Dim preSel = TryCast(ComboBoxLanguage.SelectedItem, String)
        If Not String.IsNullOrWhiteSpace(preSel) Then
            Dim p = Path.Combine(LangFolder, preSel & ".json")
            If IsValidLangFile(p) Then
                ApplyLanguageFile(p)
                Return
            End If
        End If

        ' 其次：按系统语言规则决定
        Dim ui = CultureInfo.CurrentUICulture
        Dim langCodeFull As String = ui.Name          ' 如 "en-US"、"zh-CN"
        Dim langCodeTwo As String = ui.TwoLetterISOLanguageName ' 如 "en"、"zh"

        ' 非中文系统：优先匹配同名文件 → 再找英文 → 否则不使用语言文件
        If Not langCodeTwo.Equals("zh", StringComparison.OrdinalIgnoreCase) Then
            Dim candidates As New List(Of String)
            candidates.Add(langCodeFull)                  ' en-US
            candidates.Add(langCodeTwo)                   ' en
            candidates.Add("en")                          ' 回退：en

            For Each code In candidates.Distinct(StringComparer.OrdinalIgnoreCase)
                Dim filePath = Path.Combine(LangFolder, code & ".json")
                If IsValidLangFile(filePath) Then
                    SetComboAndApply(code, filePath)
                    Return
                End If
            Next
            ' 没找到有效英文 → 不使用语言文件（保持程序内置默认）
            Return
        Else
            ' 中文系统：优先匹配 zh-CN / zh / zh-Hans 等（按文件是否存在）
            Dim candidates As New List(Of String) From {
            langCodeFull,         ' zh-CN / zh-TW
            langCodeTwo,          ' zh
            "zh-CN", "zh-Hans"    ' 常见命名
        }
            For Each code In candidates.Distinct(StringComparer.OrdinalIgnoreCase)
                Dim filePath = Path.Combine(LangFolder, code & ".json")
                If IsValidLangFile(filePath) Then
                    SetComboAndApply(code, filePath)
                    Return
                End If
            Next
            ' 找不到就不使用语言文件
            Return
        End If
    End Sub

    Private Sub SetComboAndApply(code As String, path As String)
        ' 若下拉没有该项，补充进去
        Dim exists As Boolean = ComboBoxLanguage.Items.Cast(Of Object)().
        Any(Function(x) String.Equals(CStr(x), code, StringComparison.OrdinalIgnoreCase))
        If Not exists Then
            ComboBoxLanguage.Items.Add(code)
        End If

        ComboBoxLanguage.SelectedItem = code
        ApplyLanguageFile(path)
    End Sub

    Private Sub ApplyLanguageFile(path As String)
        Try
            Loc.LoadJsonFile(path)
            Loc.ApplyTo(Me)
        Catch ex As Exception
            MessageBox.Show(Me, $"语言文件应用失败：{ex.Message}", "语言加载", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub
    Private LangFolder As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lang")

    Private Sub frmPrimeKit_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        ' 1) 扫描 lang 文件夹，加载合法的 json 文件名到下拉
        Loc.EnsureCaptured(Me)   ' 先采默认快照（不改变任何文本）

        RefreshLanguageList()


        ' 2) 启动时的应用逻辑
        '   2.1 如果 ComboBoxLanguage 里已经有选中的值，且对应 json 有效 → 直接应用
        '   2.2 否则按系统语言选择（非中文 → 尝试匹配系统语言 → 不到就找英文 → 否则不加载；
        '       中文系统 → 找不到就不加载）
        ApplyLanguageOnLoad()
        LoadPanelOptionFromIni(PanelOption)

        NumericUpDownThreads.Minimum = 1
        NumericUpDownThreads.Maximum = Environment.ProcessorCount * 2
        NumericUpDownThreads.Value = Environment.ProcessorCount

        ThreadHub.Init(UiSafe.GetControlValue(NumericUpDownThreads), AddressOf SetLabelInfo)
        ButtonStop.Enabled = False


        AddHandler GlobalTimer.Tick, AddressOf ShowResult

    End Sub

    Private Sub ButtonAbout_Click(sender As Object, e As EventArgs) Handles ButtonAbout.Click
        AboutDialog.ShowAbout(Me)
    End Sub
End Class