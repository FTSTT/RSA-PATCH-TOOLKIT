
Imports System.Numerics
Imports System.Runtime.InteropServices
Imports System.Text

Friend Module EgwNative
    Private Const Dll As String = "egw.dll"

    <DllImport(Dll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi)>
    Private Function egw_version(<Out> outStr As StringBuilder, outLen As Integer) As Integer
    End Function

    <DllImport(Dll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi)>
    Private Function egw_trim(n_dec As String, <Out> outStr As StringBuilder, outLen As Integer) As Integer
    End Function

    <DllImport(Dll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi)>
    Private Function egw_is_probable_prime(n_dec As String, reps As Integer) As Integer
    End Function

    <DllImport(Dll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi)>
    Private Function egw_next_prime(n_dec As String, <Out> outStr As StringBuilder, outLen As Integer) As Integer
    End Function

    <DllImport(Dll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi)>
    Private Function egw_gcd(a_dec As String, b_dec As String, <Out> outStr As StringBuilder, outLen As Integer) As Integer
    End Function

    <DllImport(Dll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi)>
    Private Function egw_powm(a_dec As String, e_dec As String, m_dec As String, <Out> outStr As StringBuilder, outLen As Integer) As Integer
    End Function

    <DllImport(Dll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi)>
    Private Function egw_add(a_dec As String, b_dec As String, <Out> outStr As StringBuilder, outLen As Integer) As Integer
    End Function

    <DllImport(Dll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi)>
    Private Function egw_sub(a_dec As String, b_dec As String, <Out> outStr As StringBuilder, outLen As Integer) As Integer
    End Function

    <DllImport(Dll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi)>
    Private Function egw_mul(a_dec As String, b_dec As String, <Out> outStr As StringBuilder, outLen As Integer) As Integer
    End Function

    <DllImport(Dll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi)>
    Private Function egw_tdiv_qr(a_dec As String, b_dec As String, <Out> qStr As StringBuilder, qLen As Integer, <Out> rStr As StringBuilder, rLen As Integer) As Integer
    End Function

    <StructLayout(LayoutKind.Sequential)>
    Public Structure EgwEcmParams
        Public curves As ULong
        Public B1 As ULong
        Public B2min As ULong
        Public B2max As ULong
        Public method As UInteger ' 0 ECM, 1 PM1, 2 PP1
        Public reserved As UInteger
    End Structure

    <DllImport(Dll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi)>
    Private Function egw_find_factor(n_dec As String, ByRef p As EgwEcmParams, <Out> outStr As StringBuilder, outLen As Integer) As Integer
    End Function

    <DllImport(Dll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi)>
    Private Function egw_find_factor_ecm(n_dec As String, curves As ULong, B1 As ULong, B2min As ULong, B2max As ULong, <Out> outStr As StringBuilder, outLen As Integer) As Integer
    End Function

    <DllImport(Dll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi)>
    Private Function egw_find_factor_pm1(n_dec As String, curves As ULong, B1 As ULong, <Out> outStr As StringBuilder, outLen As Integer) As Integer
    End Function

    <DllImport(Dll, CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Ansi)>
    Private Function egw_find_factor_pp1(n_dec As String, curves As ULong, B1 As ULong, <Out> outStr As StringBuilder, outLen As Integer) As Integer
    End Function

    ' ---------- 便捷封装 ----------
    ' ---- 辅助：严格成功（rc=0）版，用于 add/sub/mul/powm/next_prime 等肯定有输出的函数 ----
    Private Function TakeOk(callit As Func(Of StringBuilder, Integer, Integer)) As String
        Dim cap = 4096
        For i = 1 To 4
            Dim sb As New StringBuilder(cap)
            Dim rc = callit(sb, sb.Capacity)
            If rc = 0 Then Return sb.ToString()   ' EGW_OK
            If rc = -2 Then                       ' EGW_BUFSMALL
                cap *= 2
            Else
                Throw New Exception("native rc=" & rc)  ' 其它负数=错误；1 不该出现在这些函数里
            End If
        Next
        Throw New Exception("buffer too small")
    End Function

    ' ---- 辅助：允许 NOTFOUND（rc=1）版，用于 ECM/PM1/PP1 ----
    Private Function TakeAllowNotFound(callit As Func(Of StringBuilder, Integer, Integer)) As String
        Dim cap = 4096
        For i = 1 To 4
            Dim sb As New StringBuilder(cap)
            Dim rc = callit(sb, sb.Capacity)
            Select Case rc
                Case 0  ' EGW_OK
                    Return sb.ToString()
                Case 1  ' EGW_NOTFOUND
                    Return ""  ' 没找到因子：返回空串表示“未命中”
                Case -2 ' EGW_BUFSMALL
                    cap *= 2
                Case Else
                    Throw New Exception("native rc=" & rc)
            End Select
        Next
        Throw New Exception("buffer too small")
    End Function
    Private Function IsDec(s As String) As Boolean
        If String.IsNullOrWhiteSpace(s) Then Return False
        s = s.Trim()
        ' 允许可选的正负号
        For i = 0 To s.Length - 1
            Dim c = s(i)
            If i = 0 AndAlso (c = "+"c OrElse c = "-"c) Then Continue For
            If c < "0"c OrElse c > "9"c Then Return False
        Next
        Return True
    End Function
    Public Function Version() As String
        Return TakeOk(Function(sb, cap) egw_version(sb, cap))
    End Function

    Public Function TrimDec(s As String) As String
        Return TakeOk(Function(sb, cap) egw_trim(s, sb, cap))
    End Function

    Public Function IsProbablePrime(n As String, Optional reps As Integer = 25) As Boolean
        Dim rc = egw_is_probable_prime(n, reps)
        If rc < 0 Then Throw New Exception("native rc=" & rc)
        Return rc = 1
    End Function

    Public Function NextPrime(n As String) As String
        Return TakeOk(Function(sb, cap) egw_next_prime(n, sb, cap))
    End Function

    Public Function Add(a As String, b As String) As String
        Return TakeOk(Function(sb, cap) egw_add(a, b, sb, cap))
    End Function

    Public Function SubDec(a As String, b As String) As String
        Return TakeOk(Function(sb, cap) egw_sub(a, b, sb, cap))
    End Function

    Public Function Mul(a As String, b As String) As String
        Return TakeOk(Function(sb, cap) egw_mul(a, b, sb, cap))
    End Function

    Public Function TDivQR(a As String, b As String) As (q As String, r As String)
        Dim q As New StringBuilder(4096)
        Dim r As New StringBuilder(4096)
        Dim rc = egw_tdiv_qr(a, b, q, q.Capacity, r, r.Capacity)
        If rc <> 0 Then Throw New Exception("native rc=" & rc)
        Return (q.ToString(), r.ToString())
    End Function

    Public Function ECM_FindFactor(n As String, curves As ULong, B1 As ULong, Optional B2min As ULong = 0, Optional B2max As ULong = 0) As String
        Return TakeAllowNotFound(Function(sb, cap) egw_find_factor_ecm(n, curves, B1, B2min, B2max, sb, cap))
    End Function

    Public Function PM1_FindFactor(n As String, curves As ULong, B1 As ULong) As String
        Return TakeAllowNotFound(Function(sb, cap) egw_find_factor_pm1(n, curves, B1, sb, cap))
    End Function

    Public Function PP1_FindFactor(n As String, curves As ULong, B1 As ULong) As String
        Return TakeAllowNotFound(Function(sb, cap) egw_find_factor_pp1(n, curves, B1, sb, cap))
    End Function

    ' 埃拉托斯特尼筛法生成素数，限制范围 [start, end]，支持 Long 类型
    Public Function GeneratePrimes(start As Long, [end] As Long) As List(Of Long)
        ' 初始化素数列表
        Dim sieve As Boolean() = New Boolean([end] + 1) {}
        Dim primes As New List(Of Long)()

        ' 设置所有数为真，表示是素数
        For i As Long = 2 To [end]
            sieve(i) = True
        Next

        ' 筛选过程
        For i As Long = 2 To Math.Sqrt([end])
            If sieve(i) Then
                For j As Long = i * i To [end] Step i
                    sieve(j) = False
                Next
            End If
        Next

        ' 收集素数
        For i As Long = start To [end]
            If sieve(i) Then
                primes.Add(i)
            End If
        Next

        Return primes
    End Function


    ' --- 公共接口：只用素数做除数，支持 Long 范围 ---
    Public Function TrialDivisionStr(nStr As String, start As Long, [end] As Long) As String
        Dim n As BigInteger = BigInteger.Parse(nStr)
        If n <= 1 Then Return Nothing
        If start < 2 Then start = 2

        ' 将 end 截到 sqrt(n)，但若 sqrt(n) > Long.MaxValue 则保留用户 end
        Dim capEnd As Long = [end]
        Dim s As BigInteger = SqrtFloor(n)
        If s <= Long.MaxValue Then
            Dim sL As Long = CLng(s)
            If capEnd > sL Then capEnd = sL
        End If
        If start > capEnd Then Return Nothing

        ' 用分段筛生成 [start, capEnd] 的素数
        For Each p In SegmentedPrimes(start, capEnd)
            If (n Mod p) = 0 Then Return p.ToString()
        Next
        Return Nothing
    End Function

    ' --- 分段筛：生成 [L,R] 的全部素数（L,R 为 Long） ---
    Private Function SegmentedPrimes(L As Long, R As Long) As IEnumerable(Of Long)
        Dim result As New List(Of Long)
        If R < 2 OrElse L > R Then Return result
        If L < 2 Then L = 2

        ' 先筛出基础素数：到 sqrt(R)
        Dim rSqrt As Long
        Dim rs As BigInteger = SqrtFloor(New BigInteger(R))
        rSqrt = If(rs > Long.MaxValue, CLng(Math.Sqrt(CDbl(R))), CLng(rs))
        Dim basePrimes = SimpleSieveUpTo(rSqrt)

        ' 分段（避免一次性分配超大数组）
        Const BLOCK As Integer = 1_000_000   ' 可按机器内存调
        Dim segStart As Long = L
        While segStart <= R
            Dim segEnd As Long = Math.Min(segStart + BLOCK - 1, R)
            Dim size As Integer = CInt(segEnd - segStart + 1)
            Dim isPrime() As Boolean = Enumerable.Repeat(True, size).ToArray()

            For Each p In basePrimes
                Dim pp As Long = CLng(p)
                Dim startMul As Long = Math.Max(pp * pp, ((segStart + pp - 1) \ pp) * pp)
                If startMul < pp * pp Then startMul = pp * pp
                If startMul < segStart Then startMul = segStart
                For m As Long = startMul To segEnd Step pp
                    isPrime(CInt(m - segStart)) = False
                Next
            Next

            For i As Integer = 0 To size - 1
                If isPrime(i) Then result.Add(segStart + i)
            Next

            If segEnd = Long.MaxValue Then Exit While
            segStart = segEnd + 1
        End While

        Return result
    End Function

    ' --- 朴素筛：生成 [2..limit] 的素数，limit 一般为 sqrt(R)（<= ~3e9，实用中常 <= 1e7）---
    Private Function SimpleSieveUpTo(limit As Long) As List(Of Integer)
        Dim res As New List(Of Integer)
        If limit < 2 Then Return res
        ' 为了内存安全，这里对极端超大 limit 可以加保护（例如上限 50_000_000）
        If limit > 50_000_000 Then
            limit = 50_000_000
        End If

        Dim sieve(limit) As Boolean
        For i As Long = 2 To limit
            sieve(i) = True
        Next
        Dim r As Long = CLng(Math.Sqrt(limit))
        For i As Long = 2 To r
            If sieve(i) Then
                Dim stepStart As Long = i * i
                For j As Long = stepStart To limit Step i
                    sieve(j) = False
                Next
            End If
        Next
        For i As Long = 2 To limit
            If sieve(i) Then res.Add(CInt(i))
        Next
        Return res
    End Function

    ' --- BigInteger 整数平方根（向下取整）---
    Private Function SqrtFloor(n As BigInteger) As BigInteger
        If n.Sign <= 0 Then Return BigInteger.Zero
        ' 初始近似：2^(floor(log2(n)/2)+1)
        Dim bits As Integer = CInt(BigInteger.Log(n, 2))
        Dim x As BigInteger = BigInteger.One << ((bits \ 2) + 1)
        While True
            Dim y As BigInteger = (x + n / x) >> 1
            If y >= x Then Return x
            x = y
        End While
    End Function


    ' Brent（Pollard Rho–Brent），返回一个非平凡因子（十进制字符串）或 Nothing
    Public Function BrentFactorization(nStr As String,
                                Optional maxIter As Integer = 100000,
                                Optional seed As Long = 0,
                                Optional m As Integer = 100) As String
        Dim n As BigInteger = BigInteger.Parse(nStr)
        If n Mod 2 = 0 Then Return "2"
        If n Mod 3 = 0 Then Return "3"
        If n = 1 Then Return Nothing

        Dim y As BigInteger
        Dim c As BigInteger
        If seed = 0 Then
            ' 简易可重复伪随机（避免依赖 Random 实例跨线程）
            y = 2 : c = 1
        Else
            y = (seed Mod (n - 1)) : If y = 0 Then y = 1
            c = ((seed * seed + 1) Mod (n - 1)) : If c = 0 Then c = 1
        End If

        Dim r As Integer = 1
        Dim iters As Integer = 0

        Do
            Dim x As BigInteger = y
            For i = 1 To r
                y = (y * y + c) Mod n   ' f(y) mod n —— 用余数
            Next

            Dim k As Integer = 0
            While k < r
                Dim q As BigInteger = 1
                Dim limit As Integer = Math.Min(m, r - k)

                For i = 1 To limit
                    y = (y * y + c) Mod n
                    Dim diff As BigInteger = x - y
                    If diff.Sign < 0 Then diff = BigInteger.Negate(diff)
                    q = (q * (diff Mod n)) Mod n
                    iters += 1
                    If iters >= maxIter Then Return Nothing
                Next

                Dim g As BigInteger = BigInteger.GreatestCommonDivisor(q, n)
                If g > 1 AndAlso g < n Then
                    Return g.ToString()
                End If
                If g = n Then
                    ' 退化：逐步 GCD
                    Do
                        y = (y * y + c) Mod n
                        Dim diff As BigInteger = x - y
                        If diff.Sign < 0 Then diff = BigInteger.Negate(diff)
                        g = BigInteger.GreatestCommonDivisor(diff, n)
                        iters += 1
                        If g > 1 AndAlso g < n Then
                            Return g.ToString()
                        End If
                        If iters >= maxIter Then Return Nothing
                    Loop
                End If

                k += limit
            End While

            r *= 2
        Loop
    End Function






    Private B1Table As New Dictionary(Of Integer, Tuple(Of String, Integer)) From {
        {20, Tuple.Create("11000", 74)},
        {25, Tuple.Create("50000", 221)},
        {30, Tuple.Create("250000", 453)},
        {35, Tuple.Create("1000000", 984)},
        {40, Tuple.Create("3000000", 2541)},
        {45, Tuple.Create("11000000", 4949)},
        {50, Tuple.Create("43000000", 8266)},
        {55, Tuple.Create("110000000", 20158)},
        {60, Tuple.Create("260000000", 47173)},
        {65, Tuple.Create("850000000", 77666)}
    }

    ' 获取推荐的 B1 列表，并根据参数指定的总曲线数量进行合理分配
    Public Function GetRecommendedB1List(digits As String, totalCurves As Integer) As List(Of Tuple(Of String, Integer))
        ' 解析大数位数
        Dim numDigits As Integer = digits.Length

        ' 初始化结果列表
        Dim result As New List(Of Tuple(Of String, Integer))()

        ' 定义剩余曲线数量
        Dim remainingCurves As Integer = totalCurves
        Dim totalRecommendedCurves As Integer = 0

        ' 计算推荐表中所有曲线数量的总和（用于按比例分配）
        For Each key As Integer In B1Table.Keys
            totalRecommendedCurves += B1Table(key).Item2
        Next

        ' 计算B1的权重，权重越小的B1，分配的曲线数量越多
        Dim weightFactor As New Dictionary(Of Integer, Double)

        ' 分配的权重：较小的 B1 获取更多的曲线（权重为1/平方根B1）
        For i As Integer = 20 To numDigits Step 5
            If B1Table.ContainsKey(i) Then
                ' 假设小的 B1 值会有较大的权重（权重为1/平方根B1）
                weightFactor(i) = 1 / Math.Sqrt(i)
            End If
        Next

        ' 根据权重分配曲线
        For i As Integer = 20 To numDigits Step 5
            ' If the current `i` is greater than the largest key, use the largest B1 (65)
            If B1Table.ContainsKey(i) Then
                ' 获取当前B1的曲线数量
                Dim recommendedCurves As Integer = B1Table(i).Item2

                ' 根据权重计算该 B1 需要分配的曲线数量
                Dim allocatedCurves As Integer = CInt(Math.Round(weightFactor(i) / weightFactor.Values.Sum() * totalCurves))

                ' 确保曲线数量至少为1
                If allocatedCurves < 1 Then
                    allocatedCurves = 1
                End If

                ' 防止剩余曲线数为负
                If allocatedCurves > remainingCurves Then
                    allocatedCurves = remainingCurves
                End If

                ' 添加当前 B1 和对应的曲线数量
                result.Add(New Tuple(Of String, Integer)(B1Table(i).Item1, allocatedCurves))

                ' 更新剩余曲线数
                remainingCurves -= allocatedCurves

                ' 如果没有剩余曲线，退出循环
                If remainingCurves <= 0 Then
                    Exit For
                End If
            Else
                ' If no matching key found, use the largest key (65)
                If remainingCurves > 0 Then
                    result.Add(New Tuple(Of String, Integer)(B1Table(65).Item1, remainingCurves))
                End If
            End If
        Next

        ' 如果仍有剩余曲线，则分配给最大 B1（65）
        If remainingCurves > 0 Then
            result.Add(New Tuple(Of String, Integer)(B1Table(65).Item1, remainingCurves))
        End If

        Return result
    End Function




End Module
