Imports System.Numerics
Imports System.Security.Cryptography
Imports System.Text

Public Module RSATEST

    Public Enum OutputFormat
        Hex
        Dec
        Base64
    End Enum

    '==================== 对外主入口 ====================

    ''' <summary>
    ''' 生成 n、d 并执行自测（默认 100 个不同随机明文，各 1 次加/解密）。
    ''' 无论成功/失败，返回按 fmt 格式化的 P,Q,E,N,D 以及 Passed。
    ''' </summary>
    Public Function GenerateAndSelfTest(p As BigInteger,
                                        q As BigInteger,
                                        e As BigInteger,
                                        fmt As OutputFormat,
                                        Optional iterations As Integer = 100,
                                        Optional useOaepSha256 As Boolean = True) _
                                        As (P As String, Q As String, E As String, N As String, D As String, Passed As Boolean)

        ' 计算 n、d
        Dim n As BigInteger, d As BigInteger
        Dim nd = GenerateNdRaw(p, q, e)
        n = nd.N
        d = nd.D

        ' 自测（失败也照常返回）
        Dim passed As Boolean
        Try
            passed = VerifyRsaEncDec(p, q, e, d, iterations, useOaepSha256)
        Catch ex As Exception
            Console.WriteLine(CStr(Loc.T("Text.452a044f", "RSA 自测异常: ")) & ex.Message)
            passed = False
        End Try

        ' 返回格式化后的 p,q,e,n,d 与结果
        Return (FormatBigInt(p, fmt),
                FormatBigInt(q, fmt),
                FormatBigInt(e, fmt),
                FormatBigInt(n, fmt),
                FormatBigInt(d, fmt),
                passed)
    End Function

    '==================== 生成 n、d ====================

    ''' <summary>
    ''' 生成 n、d（未格式化）。φ(n) = (p-1)(q-1)。
    ''' </summary>
    Public Function GenerateNdRaw(p As BigInteger,
                                  q As BigInteger,
                                  e As BigInteger) As (N As BigInteger, D As BigInteger)

        If p <= 1 OrElse q <= 1 Then Throw New ArgumentException(CStr(Loc.T("Text.ddda6f20", "p、q 必须 > 1")))
        If e <= 1 Then Throw New ArgumentException(CStr(Loc.T("Text.79874bd1", "e 必须 > 1")))

        Dim n As BigInteger = p * q
        Dim phi As BigInteger = BigInteger.Multiply(p - 1, q - 1) ' φ(n)

        If BigInteger.GreatestCommonDivisor(e, phi) <> 1 Then
            Throw New ArgumentException(CStr(Loc.T("Text.83141085", "e 与 φ(n) 不互素，无法求逆。")))
        End If

        Dim d As BigInteger = ModInverse(e, phi)
        Return (n, d)
    End Function

    '==================== 自测：软实现兜底 ====================

    ''' <summary>
    ''' 100 个不同随机明文，各 1 次加/解密。
    ''' n 位数 < 512 时走软实现（BigInteger 无填充）；否则用 .NET RSA（OAEP-SHA256 或 PKCS#1 v1.5）。
    ''' </summary>
    Public Function VerifyRsaEncDec(p As BigInteger,
                                    q As BigInteger,
                                    e As BigInteger,
                                    d As BigInteger,
                                    Optional iterations As Integer = 100,
                                    Optional useOaepSha256 As Boolean = True) As Boolean

        Dim n As BigInteger = p * q
        Dim nBits As Integer = BitLength(n)

        ' ===== 情况 A：n 太小（<512 位）→ 软实现（无填充）=====
        If nBits < 512 Then
            Dim rnd(31) As Byte
            For i = 1 To iterations
                ' 生成 m ∈ [1, n-1]
                Dim m As BigInteger
                Do
                    RandomNumberGenerator.Fill(rnd)
                    ' 强制正数：拼接 0x00
                    m = New BigInteger(rnd.Concat(New Byte() {0}).ToArray())
                    m = BigInteger.Remainder(m, n)
                Loop While m <= 0

                Dim c As BigInteger = BigInteger.ModPow(m, e, n)
                Dim m2 As BigInteger = BigInteger.ModPow(c, d, n)
                If m <> m2 Then
                    'Console.WriteLine($CStr(Loc.T("Text.dffd6185", "[软实现] 第 {i} 次不匹配。")))
                    Return False
                End If
            Next
            Return True
        End If

        ' ===== 情况 B：n ≥ 512 位 → 使用 .NET RSA =====
        Dim rsaParams As New RSAParameters With {
            .Modulus = TrimLeadingZeros(ToBigEndianUnsignedBytes(n)),
            .Exponent = TrimLeadingZeros(ToBigEndianUnsignedBytes(e)),
            .D = TrimLeadingZeros(ToBigEndianUnsignedBytes(d)),
            .P = TrimLeadingZeros(ToBigEndianUnsignedBytes(p)),
            .Q = TrimLeadingZeros(ToBigEndianUnsignedBytes(q)),
            .DP = TrimLeadingZeros(ToBigEndianUnsignedBytes(BigInteger.Remainder(d, p - 1))),
            .DQ = TrimLeadingZeros(ToBigEndianUnsignedBytes(BigInteger.Remainder(d, q - 1))),
            .InverseQ = TrimLeadingZeros(ToBigEndianUnsignedBytes(ModInverse(q, p))) ' q^{-1} mod p
        }

        Using rsa As RSA = RSA.Create()
            Try
                rsa.ImportParameters(rsaParams)
            Catch ex As Exception
                Throw New CryptographicException(CStr(Loc.T("Text.d4b8f642", "导入 RSAParameters 失败（请确认 p、q 足够大且参数字节序/长度正确）。原始错误：")) & ex.Message, ex)
            End Try

            Dim keyBytes As Integer = rsa.KeySize \ 8
            Dim maxPlain As Integer =
                If(useOaepSha256, keyBytes - 2 * 32 - 2,  ' OAEP-SHA256：k - 2*hLen - 2
                               keyBytes - 11)             ' PKCS#1 v1.5：k - 11
            If maxPlain <= 0 Then Throw New InvalidOperationException(CStr(Loc.T("Text.67d5e639", "密钥过小，无法进行带填充的加密。")))

            Dim upper As Integer = Math.Min(2048, maxPlain)
            Dim padding = If(useOaepSha256, RSAEncryptionPadding.OaepSHA256, RSAEncryptionPadding.Pkcs1)

            ' 预先生成 iterations 个不同随机明文
            Dim plains As New List(Of Byte())(iterations)
            For i = 1 To iterations
                Dim len As Integer = RandomInt(1, upper)
                Dim pbuf(len - 1) As Byte
                RandomNumberGenerator.Fill(pbuf)
                plains.Add(pbuf)
            Next

            ' 每个只加/解密 1 次
            For i = 1 To iterations
                Dim plain = plains(i - 1)
                Dim cipher = rsa.Encrypt(plain, padding)
                Dim recovered = rsa.Decrypt(cipher, padding)
                If Not BytesEqual(plain, recovered) Then
                    'Console.WriteLine($CStr(Loc.T("Text.542551de", "第 {i} 次不匹配：明文与解密结果不同。")))
                    Return False
                End If
            Next
        End Using

        Return True
    End Function

    '==================== 工具函数 ====================

    ''' <summary>返回 BigInteger 的位数（BitLength）。</summary>
    Private Function BitLength(x As BigInteger) As Integer
        If x.Sign = 0 Then Return 0
        Dim le As Byte() = x.ToByteArray() ' 小端、带符号
        ' 去符号扩展
        If le.Length >= 2 AndAlso le(le.Length - 1) = 0 AndAlso (le(le.Length - 2) And &H80) = 0 Then
            Array.Resize(le, le.Length - 1)
        End If
        Dim msb As Byte = le(le.Length - 1)
        Dim leading As Integer = 8
        While leading > 0 AndAlso (msb And (1 << (leading - 1))) = 0
            leading -= 1
        End While
        Return (le.Length - 1) * 8 + leading
    End Function

    Private Function RandomInt(minInclusive As Integer, maxInclusive As Integer) As Integer
        If minInclusive > maxInclusive Then Throw New ArgumentException(CStr(Loc.T("Text.13365301", "随机区间非法")))
        Dim diff As UInteger = CUInt(maxInclusive - minInclusive)
        Dim buf(3) As Byte
        RandomNumberGenerator.Fill(buf)
        Dim r As UInteger = BitConverter.ToUInt32(buf, 0)
        Return CInt(minInclusive + (r Mod (diff + 1UI)))
    End Function

    Private Function BytesEqual(a As Byte(), b As Byte()) As Boolean
        If a Is Nothing OrElse b Is Nothing OrElse a.Length <> b.Length Then Return False
        Dim diff As Integer = 0
        For i = 0 To a.Length - 1
            diff = diff Or (a(i) Xor b(i))
        Next
        Return diff = 0
    End Function

    ''' <summary>扩展欧几里得求 a^{-1} mod m。</summary>
    Private Function ModInverse(a As BigInteger, m As BigInteger) As BigInteger
        Dim t As BigInteger = 0, newT As BigInteger = 1
        Dim r As BigInteger = m, newR As BigInteger = BigInteger.Remainder(a, m)
        While newR <> 0
            Dim q As BigInteger = BigInteger.Divide(r, newR)
            Dim tmpT As BigInteger = t - q * newT : t = newT : newT = tmpT
            Dim tmpR As BigInteger = r - q * newR : r = newR : newR = tmpR
        End While
        If r <> 1 Then Throw New ArithmeticException(CStr(Loc.T("Text.2e958c99", "不存在模逆。")))
        If t < 0 Then t += m
        Return t
    End Function

    ''' <summary>最小公倍数（备用：若改用 λ(n)=lcm(p-1,q-1)）。</summary>
    Private Function Lcm(a As BigInteger, b As BigInteger) As BigInteger
        Dim g As BigInteger = BigInteger.GreatestCommonDivisor(a, b)
        Return BigInteger.Multiply(BigInteger.Divide(a, g), b)
    End Function

    Private Function FormatBigInt(x As BigInteger, fmt As OutputFormat) As String
        Select Case fmt
            Case OutputFormat.Dec
                Return x.ToString()
            Case OutputFormat.Hex
                Return ToHex(x)
            Case OutputFormat.Base64
                Return ToBase64(x)
            Case Else
                Return x.ToString()
        End Select
    End Function

    Private Function ToHex(x As BigInteger) As String
        Dim bytesBE = ToBigEndianUnsignedBytes(x)
        Dim sb As New StringBuilder(bytesBE.Length * 2)
        For Each b In bytesBE
            sb.Append(b.ToString("X2"))
        Next
        If sb.Length = 0 Then Return "00"
        Return sb.ToString()
    End Function

    Private Function ToBase64(x As BigInteger) As String
        Dim bytesBE = ToBigEndianUnsignedBytes(x)
        Return Convert.ToBase64String(bytesBE)
    End Function

    ''' <summary>转无符号大端字节序。</summary>
    Private Function ToBigEndianUnsignedBytes(x As BigInteger) As Byte()
        If x = 0 Then Return New Byte() {0}
        Dim le As Byte() = x.ToByteArray() ' 小端、二补码
        ' 去符号扩展（最高符号字节 0x00）
        If le.Length >= 2 AndAlso le(le.Length - 1) = 0 AndAlso (le(le.Length - 2) And &H80) = 0 Then
            Array.Resize(le, le.Length - 1)
        End If
        Array.Reverse(le) ' 转大端
        Return le
    End Function

    Private Function TrimLeadingZeros(be As Byte()) As Byte()
        If be Is Nothing OrElse be.Length = 0 Then Return New Byte() {0}
        Dim idx As Integer = 0
        While idx < be.Length - 1 AndAlso be(idx) = 0
            idx += 1
        End While
        If idx = 0 Then Return be
        Dim res(be.Length - idx - 1) As Byte
        Buffer.BlockCopy(be, idx, res, 0, res.Length)
        Return res
    End Function

End Module
