' ===========================================
' RSA Single-File Helper (VB.NET)
' - TryMakeRsaND_WithFmt:  输入 p,q,e(按 fmt 解析) -> 输出 N,D(十六进制大写)
' - TryRsaBidirectionalSelfTest: 输入 n,d,e(按 fmt 解析)
'     随机 49 份数据(1~8192 字节)做 E->D、D->E 双向验证（原始幂模，无填充）
'
' 进制选择:
'   fmt 长度=3，按顺序指定每个输入的进制：'d' = 十进制；'h' = 十六进制(无 0x 前缀)
'   例如:
'     "ddd" = 全部十进制
'     "hdd" = 第1个十六进制，其余十进制
'     "ddh" = 只有第3个是十六进制
'
' 用例:
'   Dim Nhex As String, Dhex As String
'   ' 1) 标准 RSA：p=61, q=53, e=65537 (十进制)
'   Dim ok1 = TryMakeRsaND_WithFmt("61", "53", "65537", "ddd", Nhex, Dhex)
'   ' 2) p/q 十六进制，e 十进制：p=3D(=61), q=35(=53), e=65537
'   Dim ok2 = TryMakeRsaND_WithFmt("3D", "35", "65537", "hhd", Nhex, Dhex)
'   ' 3) 允许 q=1：p 为大素数(十六进制)，e 也十六进制 10001(=65537)
'   Dim ok3 = TryMakeRsaND_WithFmt("FFFFFFFFFFFFFFFFFFFF", "1", "10001", "hdh", Nhex, Dhex)
'
'   ' 4) 双向自测：n 为十六进制，d/e 为十进制
'   Dim passed As Boolean = TryRsaBidirectionalSelfTest(
'       nStr:="C34F9A...ABCD", dStr:="123456789", eStr:="65537", fmt:="hdd")
'
' 说明:
'   - 若 e 与 φ(N) 不互素（无模逆），TryMakeRsaND_WithFmt 返回 False。
'   - 自测使用原始 m^e mod n / m^d mod n（无 padding），仅用于数学一致性检验。
'   - 生产用途请使用带填充（OAEP/PSS）的标准库。
' ===========================================

Option Strict On
Option Explicit On
Imports System.Globalization
Imports System.Numerics
Imports System.Security.Cryptography

Module RsaAllInOne

    ' ================== 对外公开函数 ==================

    ' 按 fmt 解析 p,q,e（d=十进制，h=十六进制），计算 N 与 d（私钥指数）
    ' - 允许 p=1 或 q=1；若两者都为 1 或 e 无逆元则返回 False
    ' - 返回的 Nhex、Dhex 为十六进制(大写，无 0x)
    Public Function TryMakeRsaND_WithFmt(pStr As String,
                                         qStr As String,
                                         eStr As String,
                                         fmt As String,
                                         ByRef Nhex As String,
                                         ByRef Dhex As String) As Boolean
        Nhex = Nothing : Dhex = Nothing

        If String.IsNullOrWhiteSpace(fmt) OrElse fmt.Length <> 3 Then Return False
        Dim fP = Char.ToLowerInvariant(fmt(0))
        Dim fQ = Char.ToLowerInvariant(fmt(1))
        Dim fE = Char.ToLowerInvariant(fmt(2))
        If Not IsValidFmtChar(fP) OrElse Not IsValidFmtChar(fQ) OrElse Not IsValidFmtChar(fE) Then Return False

        Dim p As BigInteger, q As BigInteger, e As BigInteger
        If Not TryParseByFmt(pStr, fP, p) Then Return False
        If Not TryParseByFmt(qStr, fQ, q) Then Return False
        If Not TryParseByFmt(eStr, fE, e) Then Return False

        ' 基本合法性
        If p < 1 OrElse q < 1 Then Return False
        If p = 1 AndAlso q = 1 Then Return False
        If e <= 1 Then Return False

        ' 计算 N 与 φ(N)
        Dim N As BigInteger = p * q
        Dim phi As BigInteger
        If p = 1 Then
            phi = q - 1
        ElseIf q = 1 Then
            phi = p - 1
        Else
            phi = (p - 1) * (q - 1)
        End If
        If phi <= 0 Then Return False

        ' 规范 e 到 [1, φ-1]
        e = ModPositive(e, phi)
        If e = 0 Then Return False

        ' d = e^{-1} mod φ(N)
        Dim d As BigInteger
        If Not TryModInverse(e, phi, d) Then Return False

        ' 输出十六进制（大写）
        Nhex = ToHexUpper(N)
        Dhex = ToHexUpper(d)
        Return True
    End Function

    ' 自测：输入 n、d、e（按 fmt 解析），随机生成 49 份(1~8192 字节)数据，
    ' 做 E->D、D->E 两个方向的幂模转换，全部通过则 True
    ' fmt 的三位依次对应 n、d、e 的进制
    Public Function TryRsaBidirectionalSelfTest(nStr As String,
                                                dStr As String,
                                                eStr As String,
                                                fmt As String) As Boolean
        If String.IsNullOrWhiteSpace(fmt) OrElse fmt.Length <> 3 Then Return False
        Dim fN = Char.ToLowerInvariant(fmt(0))
        Dim fD = Char.ToLowerInvariant(fmt(1))
        Dim fE = Char.ToLowerInvariant(fmt(2))
        If Not IsValidFmtChar(fN) OrElse Not IsValidFmtChar(fD) OrElse Not IsValidFmtChar(fE) Then Return False

        Dim n As BigInteger, d As BigInteger, e As BigInteger
        If Not TryParseByFmt(nStr, fN, n) Then Return False
        If Not TryParseByFmt(dStr, fD, d) Then Return False
        If Not TryParseByFmt(eStr, fE, e) Then Return False

        If n <= 1 OrElse d <= 1 OrElse e <= 1 Then Return False

        Dim k As Integer = ByteLenOf(n)     ' n 的字节长度
        If k < 2 Then Return False          ' n 太小无法稳妥分块

        Using rng As RandomNumberGenerator = RandomNumberGenerator.Create()
            For i As Integer = 0 To 6
                Dim dataLen = RandomInt(rng, 1, 8192)
                Dim plain(dataLen - 1) As Byte
                rng.GetBytes(plain)

                ' --- 路径 A：E -> D ---
                ' 加密端对最后一块自动零填充到 k-1；解回得到多块(k-1)拼接
                Dim cipherA = RsaRawEncryptBlocks(plain, k, e, n)   ' 明文块 k-1 → 密文块 k
                Dim backA = RsaRawDecryptBlocks(cipherA, k, d, n) ' 密文块 k   → 明文块 k-1
                If Not BytesEqual(plain, TruncateTo(backA, dataLen)) Then Return False

                ' --- 路径 B：D -> E（数学对称性 / 类签名→验签）---
                Dim cipherB = RsaRawEncryptBlocks(plain, k, d, n)
                Dim backB = RsaRawDecryptBlocks(cipherB, k, e, n)
                If Not BytesEqual(plain, TruncateTo(backB, dataLen)) Then Return False
            Next
        End Using

        Return True
    End Function

    ' ================== 内部工具函数 ==================

    Private Function IsValidFmtChar(c As Char) As Boolean
        Return c = "d"c OrElse c = "h"c
    End Function

    Private Function TryParseByFmt(s As String, fmt As Char, ByRef v As BigInteger) As Boolean
        If String.IsNullOrWhiteSpace(s) Then Return False
        s = s.Trim()

        If fmt = "d"c Then
            ' 十进制：仅 0-9，允许前导 0
            For Each ch In s
                If ch < "0"c OrElse ch > "9"c Then Return False
            Next
            Return BigInteger.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, v)
        Else
            ' 十六进制：兼容 "0x"/"&H" 前缀，允许任意个前导 0；去掉所有非 hex 前缀后再解析
            ' 1) 去掉常见前缀
            If s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) Then s = s.Substring(2)
            If s.StartsWith("&h", StringComparison.OrdinalIgnoreCase) Then s = s.Substring(2)

            ' 2) 校验并保留仅 hex 字符
            For Each ch In s
                Dim isHexDigit = (ch >= "0"c AndAlso ch <= "9"c) OrElse
                             (ch >= "a"c AndAlso ch <= "f"c) OrElse
                             (ch >= "A"c AndAlso ch <= "F"c)
                If Not isHexDigit Then Return False
            Next

            ' 3) 去掉所有前导 0
            Dim i As Integer = 0
            While i < s.Length AndAlso s(i) = "0"c
                i += 1
            End While
            s = s.Substring(i)

            ' 4) 全 0 的情况
            If s.Length = 0 Then
                v = BigInteger.Zero
                Return True
            End If

            ' 5) 为避免被当作负数：若最高有效 nibble 在 8..F，则前面补一个 '0'
            Dim first As Char = s(0)
            Dim highNibbleIs8ToF As Boolean =
            (first >= "8"c AndAlso first <= "9"c) OrElse
            (first >= "A"c AndAlso first <= "F"c) OrElse
            (first >= "a"c AndAlso first <= "f"c)
            If highNibbleIs8ToF Then
                s = "0" & s
            End If

            ' 6) 解析（AllowHexSpecifier：大端十六进制）
            Return BigInteger.TryParse(s, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, v)
        End If
    End Function


    ' e 在模 m 下规范为正余数
    Private Function ModPositive(a As BigInteger, m As BigInteger) As BigInteger
        Dim r = a Mod m
        If r < 0 Then r += m
        Return r
    End Function

    ' 扩展欧几里得：求 a 在 mod m 下的逆元
    Private Function TryModInverse(a As BigInteger, m As BigInteger, ByRef inv As BigInteger) As Boolean
        Dim t0 As BigInteger = 0, t1 As BigInteger = 1
        Dim r0 As BigInteger = m, r1 As BigInteger = ModPositive(a, m)
        While r1 <> 0
            Dim q As BigInteger = BigInteger.Divide(r0, r1)  ' 修复：BigInteger 没有 "\" 运算符
            Dim tmpR = r0 - q * r1 : r0 = r1 : r1 = tmpR
            Dim tmpT = t0 - q * t1 : t0 = t1 : t1 = tmpT
        End While
        If r0 <> 1 Then inv = 0 : Return False
        inv = ModPositive(t0, m)
        Return True
    End Function

    ' BigInteger 与大端字节数组互转（保持非负）
    Private Function BigIntFromBigEndian(block As Byte()) As BigInteger
        Dim le = CType(block.Clone(), Byte())
        Array.Reverse(le)
        ' 追加 0x00 防止被当作负数（两补码符号位）
        Dim ext(le.Length) As Byte
        Array.Copy(le, 0, ext, 0, le.Length)
        ext(le.Length) = 0
        Return New BigInteger(ext)
    End Function

    Private Function BigEndianFromBigInt(x As BigInteger, size As Integer) As Byte()
        If x.Sign < 0 Then Throw New ArgumentException(CStr(Loc.T("Text.6a2ec8b4", "原始 RSA 数据块不支持负数。")))
        Dim tmp = x.ToByteArray(isUnsigned:=True, isBigEndian:=False) ' 小端无符号
        Array.Reverse(tmp)                                            ' 转大端
        If tmp.Length > size Then Throw New ArgumentException(CStr(Loc.T("Text.027e49f5", "整数对该块而言过大（超出块大小）。")))
        Dim be(size - 1) As Byte
        Dim dstStart = size - tmp.Length
        Array.Copy(tmp, 0, be, dstStart, tmp.Length)
        Return be
    End Function

    Private Function ByteLenOf(n As BigInteger) As Integer
        If n.Sign < 0 Then Throw New ArgumentException(CStr(Loc.T("Text.5696acd8", "n 必须为正数。")))
        If n.IsZero Then Return 1
        Return n.ToByteArray(isUnsigned:=True, isBigEndian:=True).Length
    End Function

    ' 原始 RSA 幂模：明文(每块 k-1) -> 密文(每块 k)，末块零填充
    Private Function RsaRawEncryptBlocks(plain As Byte(),
                                         k As Integer,
                                         exp As BigInteger,
                                         modN As BigInteger) As Byte()
        If plain Is Nothing Then Return Nothing
        Dim inSize As Integer = k - 1
        Dim outSize As Integer = k

        Dim outBuf As New List(Of Byte)(CInt(Math.Ceiling(plain.Length / CDbl(inSize))) * outSize)
        Dim offset As Integer = 0
        While offset < plain.Length
            Dim take As Integer = Math.Min(inSize, plain.Length - offset)
            Dim block(inSize - 1) As Byte          ' 固定 inSize；默认 0 填充
            Array.Copy(plain, offset, block, 0, take)

            Dim m As BigInteger = BigIntFromBigEndian(block)   ' m < 256^(k-1) < n
            Dim c As BigInteger = BigInteger.ModPow(m, exp, modN)
            Dim cBytes = BigEndianFromBigInt(c, outSize)       ' 固定 k 字节
            outBuf.AddRange(cBytes)

            offset += take
        End While
        Return outBuf.ToArray()
    End Function

    ' 原始 RSA 幂模：密文(每块 k) -> 明文(每块 k-1)
    Private Function RsaRawDecryptBlocks(cipher As Byte(),
                                         k As Integer,
                                         exp As BigInteger,
                                         modN As BigInteger) As Byte()
        If cipher Is Nothing Then Return Nothing
        Dim inSize As Integer = k
        Dim outSize As Integer = k - 1

        If cipher.Length Mod inSize <> 0 Then Throw New ArgumentException(CStr(Loc.T("Text.94d59c34", "密文长度不是块大小的整数倍。")))
        Dim outBuf As New List(Of Byte)(cipher.Length \ inSize * outSize)

        Dim offset As Integer = 0
        While offset < cipher.Length
            Dim block(inSize - 1) As Byte
            Array.Copy(cipher, offset, block, 0, inSize)

            Dim c As BigInteger = BigIntFromBigEndian(block)   ' c < n
            Dim m As BigInteger = BigInteger.ModPow(c, exp, modN)
            Dim mBytes = BigEndianFromBigInt(m, outSize)       ' 固定 k-1 字节（保留前导零）
            outBuf.AddRange(mBytes)

            offset += inSize
        End While
        Return outBuf.ToArray()
    End Function

    ' 截断到指定长度
    Private Function TruncateTo(data As Byte(), take As Integer) As Byte()
        If data Is Nothing Then Return Nothing
        If take >= data.Length Then Return CType(data.Clone(), Byte())
        Dim r(take - 1) As Byte
        Array.Copy(data, 0, r, 0, take)
        Return r
    End Function

    ' 常量时间风格字节序列比较
    Private Function BytesEqual(a As Byte(), b As Byte()) As Boolean
        If a Is Nothing OrElse b Is Nothing OrElse a.Length <> b.Length Then Return False
        Dim diff As Integer = 0
        For i As Integer = 0 To a.Length - 1
            diff = diff Or (a(i) Xor b(i))
        Next
        Return diff = 0
    End Function

    ' 生成 [minVal, maxVal] 闭区间的均匀随机整数（无偏）
    Private Function RandomInt(rng As RandomNumberGenerator, minVal As Integer, maxVal As Integer) As Integer
        If minVal > maxVal Then Throw New ArgumentException()
        Dim range As UInteger = CUInt(maxVal - minVal + 1)
        Dim buf(3) As Byte
        Dim limit As UInteger = UInteger.MaxValue - (UInteger.MaxValue Mod range)
        Dim r As UInteger
        Do
            rng.GetBytes(buf)
            r = BitConverter.ToUInt32(buf, 0)
        Loop While r > limit
        Return CInt(minVal + (r Mod range))
    End Function

    ' 十六进制大写输出（无 0x）
    ' 规则：去掉所有前导0；若结果为奇数长度，在最前面补1个0；保证最终为偶数长度
    Private Function ToHexUpper(x As BigInteger) As String
        Dim s As String = x.ToString("X", CultureInfo.InvariantCulture) ' 最短表示（可能为 "0" 或奇数位）
        ' 去除所有前导0
        Dim i As Integer = 0
        While i < s.Length AndAlso s(i) = "0"c
            i += 1
        End While
        s = s.Substring(i)

        ' 全部是0的情况：变成空串，此时按规则输出 "00"
        If s.Length = 0 Then
            Return "00"
        End If

        ' 若长度为奇数，则补1个前导0；长度为偶数不变
        If (s.Length And 1) = 1 Then
            s = "0" & s
        End If
        Return s
    End Function


End Module
