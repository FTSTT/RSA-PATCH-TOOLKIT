Imports System.Text
Imports System.Threading

' =========================================================
' 偏移无关 CRC-32 中性变体生成器（不改变整文件 CRC32）
' aCr/FTSTT 2025年10月25日 夜
' =========================================================
Module CrcNeutralVariantGenerator
    Public Function GenerateZeroDeltaVariantsAB(inputStr As String,
                                            inputStrB As String,
                                            kind As String,
                                            Optional allowRandomFix As Boolean = True,
                                            Optional unicodeMode As Boolean = False,
                                            Optional frozenRanges As String = Nothing,
                                            Optional allowedChars As String = Nothing) As String
        ' 目标：返回“调整后的 B”，使得 CRC32(B') == CRC32(A)。
        ' 规则：仅在冻结范围之外求解；字符/编码/允许集与 GenerateZeroDeltaVariants 相同。
        ' 找到一个解即返回；找不到返回 Nothing。

        Dim K = kind.Trim()
        If Not {"hex", "HEX", "bytes", "dec", "base64"}.Contains(K) Then
            Throw New ArgumentException(CStr(Loc.T("Text.644955d1", "kind 只能为 hex / HEX / bytes / dec / base64")))
        End If

        ' 规范化与校验
        Dim visA As String = inputStr
        Dim visB As String = inputStrB
        Dim toStringFunc As Func(Of Byte(), String) = Nothing

        Select Case K
            Case "hex"
                visA = visA.ToLowerInvariant()
                visB = visB.ToLowerInvariant()
                ValidateHexText(visA) : ValidateHexText(visB)
                toStringFunc = AddressOf BytesToHexLower
            Case "HEX"
                visA = visA.ToUpperInvariant()
                visB = visB.ToUpperInvariant()
                ValidateHexText(visA) : ValidateHexText(visB)
                toStringFunc = AddressOf BytesToHexUpper
            Case "bytes"
                ' bytes 路径把可见串当作 2N 位 hex
                ValidateHexText(visA) : ValidateHexText(visB)
                toStringFunc = AddressOf BytesToHexLower
            Case "dec"
                If visA.Length = 0 OrElse Not visA.All(Function(ch) ch >= "0"c AndAlso ch <= "9"c) Then
                    Throw New ArgumentException(CStr(Loc.T("Text.76283e9e", "A dec 输入仅允许 '0'..'9'，且长度≥1")))
                End If
                If visB.Length = 0 OrElse Not visB.All(Function(ch) ch >= "0"c AndAlso ch <= "9"c) Then
                    Throw New ArgumentException(CStr(Loc.T("Text.f672922b", "B dec 输入仅允许 '0'..'9'，且长度≥1")))
                End If
            Case "base64"
                If visA.Length = 0 OrElse (visA.Length Mod 4) = 1 Then
                    Throw New ArgumentException(CStr(Loc.T("Text.23596565", "A base64 长度非法：长度 mod 4 不能为 1")))
                End If
                If visB.Length = 0 OrElse (visB.Length Mod 4) = 1 Then
                    Throw New ArgumentException(CStr(Loc.T("Text.658b5001", "B base64 长度非法：长度 mod 4 不能为 1")))
                End If
                ValidateBase64Form(visA) : ValidateBase64Form(visB)
        End Select

        If visA.Length <> visB.Length Then
            Throw New ArgumentException(CStr(Loc.T("Text.bf6b7ce9", "A/B 可见长度必须一致")))
        End If

        ' 自定义可见字符集合
        Dim customSet As HashSet(Of Char) = Nothing
        If Not String.IsNullOrEmpty(allowedChars) Then
            customSet = New HashSet(Of Char)(allowedChars.ToCharArray())
        End If

        ' ===== bytes（二进制）路径：直接用 4 字节线性求解器解需要的综合影响 =====
        If K = "bytes" Then
            Dim bytesA = ParseHexToBytes(visA)
            Dim bytesB = ParseHexToBytes(visB)
            If bytesA.Length <> bytesB.Length Then Throw New ArgumentException(CStr(Loc.T("Text.c7c88d15", "A/B 字节长度必须一致")))
            Dim N = bytesA.Length
            If N < 4 Then Return Nothing

            Dim infl = BuildInfluenceTable(N)
            Dim frozenByte = BuildFrozenMask(frozenRanges, N)
            Dim freePos = Enumerable.Range(0, N).Where(Function(i) Not frozenByte(i)).ToList()
            If freePos.Count < 4 Then Return Nothing

            ' 线性域所需的“综合影响” = f(A) xor f(B)
            Dim need As UInteger = Crc32Linear(bytesA) Xor Crc32Linear(bytesB)
            If need = 0UI Then
                ' B 已与 A 同 CRC
                Return toStringFunc(bytesB)
            End If

            Dim rnd As New Random()
            Dim maxTrials As Integer = 8192

            For t1 = 1 To maxTrials
                Dim shuffled = freePos.OrderBy(Function(__i) rnd.Next()).ToList()
                Dim fixPos = shuffled.Take(4).OrderBy(Function(x) x).ToList()
                Dim delta(3) As Byte
                If TrySolveFourByteNeutralization(infl, fixPos, need, delta) Then
                    Dim nb = CType(bytesB.Clone(), Byte())
                    For j = 0 To 3
                        nb(fixPos(j)) = CByte(nb(fixPos(j)) Xor delta(j))
                    Next
                    ' 严格验证最终 CRC
                    If Crc32(nb) = Crc32(bytesA) Then
                        Return toStringFunc(nb)
                    End If
                End If
            Next

            Return Nothing
        End If

        ' ===== 可见字符路径（hex/HEX/dec/base64）=====
        Dim isUnicode = unicodeMode
        Dim Lch = visA.Length
        Dim frozenChar = BuildFrozenMask(frozenRanges, Lch)

        If Not isUnicode Then
            ' ----- ASCII（1 字节/字符），按字符冻结 & 允许集求解 -----
            Dim aB = Encoding.ASCII.GetBytes(visA)
            Dim bB = Encoding.ASCII.GetBytes(visB)
            Dim need As UInteger = Crc32Linear(aB) Xor Crc32Linear(bB)
            If need = 0UI Then Return visB

            Dim infl = BuildInfluenceTable(Lch)
            Dim perPosAlphabet = BuildPerPosAlphabet_ASCII_WithCustom(visB, K, frozenChar, customSet)

            Dim candidate = Enumerable.Range(0, Lch).Where(Function(i) perPosAlphabet(i).Count > 1).ToList()
            If candidate.Count = 0 Then Return Nothing

            'Dim needChars As Integer = If(K = "dec", 12, If(K = "base64", 6, 8))
            Dim needChars As Integer = AppGlobals.g_needChars - 1
            Dim perPosMax As Integer = If(K = "dec", 5, 8)
            Dim rnd As New Random()
            Dim rounds As Integer = 64

            For r = 1 To rounds
                If candidate.Count < needChars Then Exit For
                Dim varPos = PickDistinct(rnd, candidate, needChars).OrderBy(Function(x) x).ToList()

                ' 组装每位置的有限选项集（包含原字符）
                Dim options As New List(Of List(Of Byte))()
                Dim rr As New Random(314159 + r)
                For Each p In varPos
                    Dim allowed = perPosAlphabet(p)
                    Dim setx As New HashSet(Of Byte) From {bB(p)}
                    Dim attempts = 0
                    While setx.Count < Math.Min(perPosMax, allowed.Count) AndAlso attempts < 1024
                        attempts += 1
                        setx.Add(allowed(rr.Next(allowed.Count)))
                    End While
                    options.Add(setx.ToList())
                Next

                ' MITM：让两半的综合影响异或为 need
                Dim half = varPos.Count \ 2
                Dim idxA = Enumerable.Range(0, half).ToArray()
                Dim idxB = Enumerable.Range(half, varPos.Count - half).ToArray()

                Dim mapA As New Dictionary(Of UInteger, List(Of Byte()))()
                EnumerateHalf_ASCII(bB, varPos, options, infl, idxA, 0, New List(Of Byte)(), 0UI, mapA)

                Dim solution As String = Nothing
                EnumerateHalfWithMatch_ASCII(bB, varPos, options, infl, idxB, 0, New List(Of Byte)(), 0UI, mapA,
                Sub(joined As Byte())
                    If solution IsNot Nothing Then Return
                    Dim outArr = CType(bB.Clone(), Byte())
                    For i As Integer = 0 To varPos.Count - 1
                        outArr(varPos(i)) = joined(i)
                    Next
                    Dim outStr = Encoding.ASCII.GetString(outArr)
                    If Crc32(Encoding.ASCII.GetBytes(outStr)) = Crc32(aB) Then
                        solution = outStr
                    End If
                End Sub, OptionalXorOffset:=need)

                If solution IsNot Nothing Then Return solution
            Next

            Return Nothing
        Else
            ' ----- Unicode(UTF-16LE)，按字符冻结 & 允许集求解 -----
            Dim aU = Encoding.Unicode.GetBytes(visA)
            Dim bU = Encoding.Unicode.GetBytes(visB)
            Dim need As UInteger = Crc32Linear(aU) Xor Crc32Linear(bU)
            If need = 0UI Then Return visB

            Dim inflB = BuildInfluenceTable(Lch * 2)
            Dim perPosChars = BuildPerPosAlphabet_Chars_WithCustom(visB, K, frozenChar, customSet)

            Dim candidate = Enumerable.Range(0, Lch).Where(Function(i) perPosChars(i).Count > 1).ToList()
            If candidate.Count = 0 Then Return Nothing

            'Dim needChars As Integer = If(K = "dec", 12, If(K = "base64", 6, 8))
            Dim needChars As Integer = AppGlobals.g_needChars - 1
            Dim perPosMax As Integer = If(K = "dec", 5, 8)
            Dim rnd As New Random()
            Dim rounds As Integer = 64

            For r = 1 To rounds
                If candidate.Count < needChars Then Exit For
                Dim varPos = PickDistinct(rnd, candidate, needChars).OrderBy(Function(x) x).ToList()

                Dim options As New List(Of List(Of Char))()
                Dim rr As New Random(314159 + r)
                For Each p In varPos
                    Dim allowed = perPosChars(p)
                    Dim setx As New HashSet(Of Char) From {visB(p)}
                    Dim attempts = 0
                    While setx.Count < Math.Min(perPosMax, allowed.Count) AndAlso attempts < 1024
                        attempts += 1
                        setx.Add(allowed(rr.Next(allowed.Count)))
                    End While
                    options.Add(setx.ToList())
                Next

                Dim half = varPos.Count \ 2
                Dim idxA = Enumerable.Range(0, half).ToArray()
                Dim idxB = Enumerable.Range(half, varPos.Count - half).ToArray()

                Dim mapA As New Dictionary(Of UInteger, List(Of Char()))()
                EnumerateHalf_Unicode(visB.ToCharArray(), inflB, varPos, options, idxA, 0, New List(Of Char)(), 0UI, mapA)

                Dim solution As String = Nothing
                EnumerateHalfWithMatch_Unicode(visB.ToCharArray(), inflB, varPos, options, idxB, 0, New List(Of Char)(), 0UI, mapA,
                Sub(joined As Char())
                    If solution IsNot Nothing Then Return
                    Dim outChars = visB.ToCharArray()
                    For i As Integer = 0 To varPos.Count - 1
                        outChars(varPos(i)) = joined(i)
                    Next
                    Dim outStr = New String(outChars)
                    If Crc32(Encoding.Unicode.GetBytes(outStr)) = Crc32(aU) Then
                        solution = outStr
                    End If
                End Sub, OptionalXorOffset:=need)

                If solution IsNot Nothing Then Return solution
            Next

            Return Nothing
        End If
    End Function
    Public Function GenerateZeroDeltaVariants(inputStr As String,
                                              kind As String,
                                              maxResults As Integer,
                                              Optional allowRandomFix As Boolean = True,
                                              Optional unicodeMode As Boolean = False,
                                              Optional frozenRanges As String = Nothing,
                                              Optional allowedChars As String = Nothing) As List(Of String)

        If maxResults = 0 Then Return New List(Of String)()

        Dim K = kind.Trim()
        If Not {"hex", "HEX", "bytes", "dec", "base64"}.Contains(K) Then
            Throw New ArgumentException(CStr(Loc.T("Text.644955d1", "kind 只能为 hex / HEX / bytes / dec / base64")))
        End If

        ' 规范化 & 校验可见输入；准备输出函数
        Dim visible As String = inputStr
        Dim toStringFunc As Func(Of Byte(), String) = Nothing

        Select Case K
            Case "hex"
                visible = visible.ToLowerInvariant()
                ValidateHexText(visible)
                toStringFunc = AddressOf BytesToHexLower
            Case "HEX"
                visible = visible.ToUpperInvariant()
                ValidateHexText(visible)
                toStringFunc = AddressOf BytesToHexUpper
            Case "bytes"
                ValidateHexText(visible) ' 2N 位 hex → N 字节
                toStringFunc = AddressOf BytesToHexLower
            Case "dec"
                If visible.Length = 0 OrElse Not visible.All(Function(ch) ch >= "0"c AndAlso ch <= "9"c) Then
                    Throw New ArgumentException(CStr(Loc.T("Text.d2c60775", "dec 输入仅允许 '0'..'9'，且长度≥1")))
                End If
            Case "base64"
                If visible.Length = 0 OrElse (visible.Length Mod 4) = 1 Then
                    Throw New ArgumentException(CStr(Loc.T("Text.23596565", "A base64 长度非法：长度 mod 4 不能为 1")))
                End If
                ValidateBase64Form(visible)
        End Select

        ' 自定义可见字符集合（与内建字符集求交）
        Dim customSet As HashSet(Of Char) = Nothing
        If Not String.IsNullOrEmpty(allowedChars) Then
            customSet = New HashSet(Of Char)(allowedChars.ToCharArray())
        End If

        ' ===== bytes（二进制）路径 =====
        If K = "bytes" Then
            Dim origBytes = ParseHexToBytes(visible)
            Dim N = origBytes.Length
            If N < 5 Then Return New List(Of String)() ' 需要 1 个“微改字节” + 4 个“补偿字节”

            Dim infl = BuildInfluenceTable(N)
            Dim frozenByte = BuildFrozenMask(frozenRanges, N)  ' 按字节冻结
            Dim freePos = Enumerable.Range(0, N).Where(Function(i) Not frozenByte(i)).ToList()
            If freePos.Count < 5 Then Return New List(Of String)()

            Dim cap = If(maxResults < 0, Integer.MaxValue, Math.Max(1, maxResults))
            Dim results As New List(Of Byte())()
            Dim okCount As Integer = 0
            Dim origCrc As UInteger = Crc32(origBytes)
            Dim seen As New HashSet(Of String)()
            Dim rnd As New Random()

            Dim minAcceptable As Integer = If(cap = Integer.MaxValue, 1, (cap + 1) \ 2)
            Dim maxOuterTrials As Integer = Math.Min(4096, If(cap = Integer.MaxValue, 2048, cap * 64))
            Dim hardLimit As Integer = 1000000

            For trial = 1 To maxOuterTrials
                If results.Count >= cap Then Exit For

                ' --- 选择 1 个“微改位” + 4 个“补偿位”（互不相同） ---
                Dim shuffled = freePos.OrderBy(Function(__i) rnd.Next()).ToList()
                Dim editPos As Integer = shuffled(0)
                Dim fixPos As List(Of Integer) = shuffled.Skip(1).Take(4).OrderBy(Function(x) x).ToList()

                ' --- 随机挑一个非零掩码，进行“微改” ---
                Dim maskVal As Integer : Do : maskVal = rnd.Next(1, 256) : Loop While maskVal = 0
                Dim nb = CType(origBytes.Clone(), Byte())
                nb(editPos) = CByte(nb(editPos) Xor maskVal)

                ' --- 目标综合影响（syndrome）：只来自“微改位” ---
                Dim target As UInteger = 0UI
                Dim d0 As Integer = origBytes(editPos) Xor nb(editPos)
                If d0 <> 0 Then target = target Xor infl(editPos, d0)

                ' --- 用 4 个补偿位精确抵消 target ---
                Dim deltaBytes As Byte() = New Byte(3) {}
                If TrySolveFourByteNeutralization(infl, fixPos, target, deltaBytes) Then
                    ' 应用补偿
                    For j = 0 To 3
                        Dim p = fixPos(j)
                        nb(p) = CByte(nb(p) Xor deltaBytes(j))
                    Next
                    ' 若结果不到目标的一半，继续尝试到 1,000,000 次
                    If okCount < minAcceptable Then
                        Dim trial2 As Integer = maxOuterTrials
                        While trial2 < hardLimit AndAlso results.Count < cap
                            trial2 += 1

                            Dim shuffled2 = freePos.OrderBy(Function(__i) rnd.Next()).ToList()
                            Dim editPos2 As Integer = shuffled2(0)
                            Dim fixPos2 As List(Of Integer) = shuffled2.Skip(1).Take(4).OrderBy(Function(x) x).ToList()

                            Dim maskVal2 As Integer : Do : maskVal2 = rnd.Next(1, 256) : Loop While maskVal2 = 0
                            Dim nb2 = CType(origBytes.Clone(), Byte())
                            nb2(editPos2) = CByte(nb2(editPos2) Xor maskVal2)

                            Dim target2 As UInteger = 0UI
                            Dim d02 As Integer = origBytes(editPos2) Xor nb2(editPos2)
                            If d02 <> 0 Then target2 = target2 Xor infl(editPos2, d02)

                            Dim deltaBytes2 As Byte() = New Byte(3) {}
                            If TrySolveFourByteNeutralization(infl, fixPos2, target2, deltaBytes2) Then
                                For j = 0 To 3
                                    Dim p = fixPos2(j)
                                    nb2(p) = CByte(nb2(p) Xor deltaBytes2(j))
                                Next
                                If Not nb2.SequenceEqual(origBytes) Then
                                    Dim outStr2 = toStringFunc(nb2)
                                    If seen.Add(outStr2) Then
                                        If Crc32(nb2) = origCrc Then
                                            results.Add(CType(nb2.Clone(), Byte()))
                                            okCount += 1
                                        End If
                                    End If
                                End If
                            End If
                        End While
                    End If


                    ' 防止解退化为与原始完全一致
                    If Not nb.SequenceEqual(origBytes) Then
                        Dim outStr = toStringFunc(nb)
                        If seen.Add(outStr) Then results.Add(CType(nb.Clone(), Byte()))
                    End If
                Else
                    ' 4x8 → 32 矩阵不满秩，换一组位置再试
                    Continue For
                End If
            Next

            Dim outs = results.Select(Function(b) toStringFunc(b)).ToList()
            If maxResults >= 0 AndAlso outs.Count > maxResults Then outs = outs.Take(maxResults).ToList()
            Return outs
        End If

        ' ===== 可见字符路径（hex/HEX/dec/base64）=====
        Dim isUnicode = unicodeMode
        Dim Lch = visible.Length
        Dim frozenChar = BuildFrozenMask(frozenRanges, Lch) ' 按字符

        If Not isUnicode Then
            ' -------- ASCII 字节层（1字节/字符），冻结按字符；allowedChars 交集 --------
            Dim origAscii = Encoding.ASCII.GetBytes(visible)
            Dim infl = BuildInfluenceTable(Lch)

            Dim perPosAlphabet As List(Of List(Of Byte)) =
                BuildPerPosAlphabet_ASCII_WithCustom(visible, K, frozenChar, customSet)

            Dim cap = If(maxResults < 0, Integer.MaxValue, Math.Max(1, maxResults))
            Dim minAcceptable As Integer = If(cap = Integer.MaxValue, 1, (cap + 1) \ 2)
            Dim results As New List(Of String)()
            Dim seen As New HashSet(Of String)()
            Dim okCount As Integer = 0
            Dim origCrc As UInteger = Crc32(Encoding.ASCII.GetBytes(visible))

            ' 多轮位点随机选择以提高命中率（每轮使用 MITM）
            Dim rounds As Integer = Math.Min(16, If(cap = Integer.MaxValue, 16, Math.Max(4, cap)))
            For r = 1 To rounds
                If results.Count >= cap Then Exit For
                Dim got = GenerateCharset_ASCII(visible, origAscii, infl, perPosAlphabet,
                                                maxToAdd:=cap - results.Count, requireMicroEdit:=False, kind:=K)
                For Each s In got
                    If s <> visible AndAlso seen.Add(s) Then
                        If Crc32(Encoding.ASCII.GetBytes(s)) = origCrc Then
                            results.Add(s)
                            okCount += 1
                        End If
                    End If
                Next
            Next

            If results.Count < cap Then
                Dim extra = GenerateCharset_ASCII(visible, origAscii, infl, perPosAlphabet,
                                                  maxToAdd:=cap - results.Count, requireMicroEdit:=True, kind:=K)
                For Each s In extra
                    If s <> visible AndAlso seen.Add(s) Then
                        If Crc32(Encoding.ASCII.GetBytes(s)) = origCrc Then
                            results.Add(s)
                            okCount += 1
                        End If
                    End If
                Next
            End If
            ' 若未达目标一半，继续尝试到 1,000,000 次
            If okCount < minAcceptable Then
                Dim attempts As Integer = 0
                While attempts < 1000000 AndAlso results.Count < cap
                    attempts += 1
                    Dim gotA = GenerateCharset_ASCII(visible, origAscii, infl, perPosAlphabet, maxToAdd:=Math.Max(1, cap - results.Count), requireMicroEdit:=False, kind:=K)
                    For Each s In gotA
                        If s <> visible AndAlso seen.Add(s) Then
                            If Crc32(Encoding.ASCII.GetBytes(s)) = origCrc Then
                                results.Add(s) : okCount += 1
                            End If
                        End If
                    Next
                    If results.Count >= cap Then Exit While
                    Dim gotB = GenerateCharset_ASCII(visible, origAscii, infl, perPosAlphabet, maxToAdd:=Math.Max(1, cap - results.Count), requireMicroEdit:=True, kind:=K)
                    For Each s In gotB
                        If s <> visible AndAlso seen.Add(s) Then
                            If Crc32(Encoding.ASCII.GetBytes(s)) = origCrc Then
                                results.Add(s) : okCount += 1
                            End If
                        End If
                    Next
                End While
            End If


            If maxResults >= 0 AndAlso results.Count > maxResults Then results = results.Take(maxResults).ToList()
            Return results
        Else
            ' -------- Unicode(UTF-16LE) 字节层，冻结按字符；allowedChars 交集 --------
            Dim inflB = BuildInfluenceTable(Lch * 2)
            Dim perPosChars As List(Of List(Of Char)) =
                BuildPerPosAlphabet_Chars_WithCustom(visible, K, frozenChar, customSet)

            Dim cap = If(maxResults < 0, Integer.MaxValue, Math.Max(1, maxResults))
            Dim minAcceptable As Integer = If(cap = Integer.MaxValue, 1, (cap + 1) \ 2)
            Dim results As New List(Of String)()
            Dim seen As New HashSet(Of String)()
            Dim okCount As Integer = 0
            Dim origCrc As UInteger = Crc32(Encoding.Unicode.GetBytes(visible))

            Dim rounds As Integer = Math.Min(16, If(cap = Integer.MaxValue, 16, Math.Max(4, cap)))
            For r = 1 To rounds
                If results.Count >= cap Then Exit For
                Dim got = GenerateCharset_Unicode(visible, inflB, perPosChars,
                                                  maxToAdd:=cap - results.Count, requireMicroEdit:=False, kind:=K)
                For Each s In got
                    If s <> visible AndAlso seen.Add(s) Then
                        If Crc32(Encoding.Unicode.GetBytes(s)) = origCrc Then
                            results.Add(s)
                            okCount += 1
                        End If
                    End If
                Next
            Next

            If results.Count < cap Then
                Dim extra = GenerateCharset_Unicode(visible, inflB, perPosChars,
                                                    maxToAdd:=cap - results.Count, requireMicroEdit:=True, kind:=K)
                For Each s In extra
                    If s <> visible AndAlso seen.Add(s) Then
                        If Crc32(Encoding.Unicode.GetBytes(s)) = origCrc Then
                            results.Add(s)
                            okCount += 1
                        End If
                    End If
                Next

                ' 若未达目标一半，继续尝试到 1,000,000 次
                If okCount < minAcceptable Then
                    Dim attempts As Integer = 0
                    While attempts < 1000000 AndAlso results.Count < cap
                        attempts += 1
                        Dim gotA = GenerateCharset_Unicode(visible, inflB, perPosChars, maxToAdd:=Math.Max(1, cap - results.Count), requireMicroEdit:=False, kind:=K)
                        For Each s In gotA
                            If s <> visible AndAlso seen.Add(s) Then
                                If Crc32(Encoding.Unicode.GetBytes(s)) = origCrc Then
                                    results.Add(s) : okCount += 1
                                End If
                            End If
                        Next
                        If results.Count >= cap Then Exit While
                        Dim gotB = GenerateCharset_Unicode(visible, inflB, perPosChars, maxToAdd:=Math.Max(1, cap - results.Count), requireMicroEdit:=True, kind:=K)
                        For Each s In gotB
                            If s <> visible AndAlso seen.Add(s) Then
                                If Crc32(Encoding.Unicode.GetBytes(s)) = origCrc Then
                                    results.Add(s) : okCount += 1
                                End If
                            End If
                        Next
                    End While
                End If
            End If

            If maxResults >= 0 AndAlso results.Count > maxResults Then results = results.Take(maxResults).ToList()
            Return results
        End If
    End Function

    ' ==================== 4 字节线性精确补偿器 ====================
    ' 给定 infl(pos, d) 的 32bit 影响表、4 个补偿位置 pos[4] 以及目标 32bit target，
    ' 在 GF(2) 上求解 32x32 线性方程 M * x = target，x 表示 4 个字节（32 个比特）的异或掩码。
    Private Function TrySolveFourByteNeutralization(infl As UInteger(,), fixPos As List(Of Integer),
                                                    target As UInteger,
                                                    ByRef deltaBytes As Byte()) As Boolean
        deltaBytes = New Byte(3) {}

        ' 组装 32x32 矩阵（列 = 各比特，行 = 32 个 CRC 位，LSB 在第 0 行）
        Dim M(31, 31) As Integer ' 0/1
        Dim col As Integer = 0
        For j = 0 To 3
            Dim p = fixPos(j)
            For k = 0 To 7
                Dim d As Integer = 1 << k
                Dim v As UInteger = infl(p, d)
                For r = 0 To 31
                    M(r, col) = CInt((v >> r) And 1UI)
                Next
                col += 1
            Next
        Next

        ' 右端向量
        Dim b(31) As Integer
        For r = 0 To 31
            b(r) = CInt((target >> r) And 1UI)
        Next

        ' 高斯消元 (GF(2)) — 得到一个解（如存在）
        Dim x(31) As Integer
        If Not GaussElimGF2(M, b, x) Then
            Return False
        End If

        ' 打包回 4 个字节
        For j = 0 To 3
            Dim val As Integer = 0
            For k = 0 To 7
                If x(j * 8 + k) <> 0 Then val = val Or (1 << k)
            Next
            deltaBytes(j) = CByte(val And &HFF)
        Next
        Return True
    End Function

    ' GF(2) 高斯消元：M(32x32) * x = b(32)
    ' 返回是否存在解；若存在，x 给出一个解（行简化到上三角后回代）
    Private Function GaussElimGF2(M As Integer(,), b As Integer(), ByRef x As Integer()) As Boolean
        Dim n As Integer = 32
        Dim row As Integer = 0

        Dim colPerm(31) As Integer
        For i = 0 To 31 : colPerm(i) = i : Next

        ' 前向消元（带列交换的部分选主元）
        For col = 0 To n - 1
            ' 在 [row..] 中寻找该列的 1 作为主元
            Dim pivot As Integer = -1
            For r = row To n - 1
                If M(r, col) <> 0 Then
                    pivot = r : Exit For
                End If
            Next
            If pivot = -1 Then
                ' 该列全 0，尝试与右侧某一列交换（列交换等价于调换未知数顺序）
                Dim swapCol As Integer = -1
                For c = col + 1 To n - 1
                    For r = row To n - 1
                        If M(r, c) <> 0 Then swapCol = c : Exit For
                    Next
                    If swapCol <> -1 Then Exit For
                Next
                If swapCol = -1 Then
                    ' 剩余列都是 0，检查剩余行是否对 b 可满足
                    For r = row To n - 1
                        Dim allZero As Boolean = True
                        For c = col To n - 1
                            If M(r, c) <> 0 Then allZero = False : Exit For
                        Next
                        If allZero AndAlso (b(r) <> 0) Then Return False
                    Next
                    Exit For
                Else
                    ' 交换列
                    For r = 0 To n - 1
                        Dim tmp = M(r, col) : M(r, col) = M(r, swapCol) : M(r, swapCol) = tmp
                    Next
                    Dim tmpi = colPerm(col) : colPerm(col) = colPerm(swapCol) : colPerm(swapCol) = tmpi
                End If

                ' 重新在当前列寻找主元
                pivot = -1
                For r = row To n - 1
                    If M(r, col) <> 0 Then pivot = r : Exit For
                Next
                If pivot = -1 Then Continue For
            End If

            ' 交换到当前行
            If pivot <> row Then
                For c = col To n - 1
                    Dim tmp = M(row, c) : M(row, c) = M(pivot, c) : M(pivot, c) = tmp
                Next
                Dim tmpb = b(row) : b(row) = b(pivot) : b(pivot) = tmpb
            End If

            ' 消去其它行的该列
            For r = 0 To n - 1
                If r <> row AndAlso M(r, col) <> 0 Then
                    For c = col To n - 1
                        M(r, c) = M(r, c) Xor M(row, c)
                    Next
                    b(r) = b(r) Xor b(row)
                End If
            Next

            row += 1
            If row = n Then Exit For
        Next

        ' 若有矛盾行则返回 False（已在上面检查）
        ' 回代（由于已消到列简化形式，这里直接读取即可）
        Dim sol(31) As Integer
        For r = 0 To n - 1
            ' 找到该行最左 1 的列
            Dim lead As Integer = -1
            For c = 0 To n - 1
                If M(r, c) <> 0 Then lead = c : Exit For
            Next
            If lead = -1 Then Continue For
            sol(lead) = b(r) And 1
        Next

        ' 还原列交换对未知数顺序的影响
        x = New Integer(31) {}
        For i = 0 To 31
            x(colPerm(i)) = sol(i)
        Next

        Return True
    End Function

    ' ==================== 构造“ASCII 可见层”每位置允许集合（含自定义集合） ====================
    Private Function BuildPerPosAlphabet_ASCII_WithCustom(visible As String,
                                                          kind As String,
                                                          frozenChar As Boolean(),
                                                          customSet As HashSet(Of Char)) As List(Of List(Of Byte))
        Dim res As New List(Of List(Of Byte))(visible.Length)
        Dim setHexLo = "0123456789abcdef".ToHashSet()
        Dim setHexUp = "0123456789ABCDEF".ToHashSet()
        Dim setDec = "0123456789".ToHashSet()
        Dim setB64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/".ToHashSet()

        Dim tailEqFrom As Integer = -1
        If kind = "base64" Then tailEqFrom = visible.LastIndexOf("="c)

        For i = 0 To visible.Length - 1
            Dim baseSet As HashSet(Of Char)
            Select Case kind
                Case "hex" : baseSet = setHexLo
                Case "HEX" : baseSet = setHexUp
                Case "dec" : baseSet = setDec
                Case "base64"
                    If tailEqFrom >= 0 AndAlso i >= tailEqFrom Then
                        res.Add(New List(Of Byte) From {CByte(AscW("="c))})
                        Continue For
                    Else
                        baseSet = setB64
                    End If
                Case Else
                    baseSet = setDec
            End Select

            Dim finalSet As IEnumerable(Of Char) = baseSet
            If customSet IsNot Nothing Then
                finalSet = finalSet.Intersect(customSet)
            End If

            If frozenChar(i) Then
                finalSet = New Char() {visible(i)}
            End If

            Dim listChars = finalSet.ToList()
            If listChars.Count = 0 Then listChars = New List(Of Char) From {visible(i)}
            res.Add(listChars.Select(Function(c) CByte(AscW(c))).ToList())
        Next
        Return res
    End Function

    ' ==================== 构造“Unicode 可见层”每位置允许集合（含自定义集合） ====================
    Private Function BuildPerPosAlphabet_Chars_WithCustom(visible As String,
                                                          kind As String,
                                                          frozenChar As Boolean(),
                                                          customSet As HashSet(Of Char)) As List(Of List(Of Char))
        Dim res As New List(Of List(Of Char))(visible.Length)
        Dim setHexLo = "0123456789abcdef".ToHashSet()
        Dim setHexUp = "0123456789ABCDEF".ToHashSet()
        Dim setDec = "0123456789".ToHashSet()
        Dim setB64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/".ToHashSet()

        Dim tailEqFrom As Integer = -1
        If kind = "base64" Then tailEqFrom = visible.LastIndexOf("="c)

        For i = 0 To visible.Length - 1
            Dim baseSet As HashSet(Of Char)
            Select Case kind
                Case "hex" : baseSet = setHexLo
                Case "HEX" : baseSet = setHexUp
                Case "dec" : baseSet = setDec
                Case "base64"
                    If tailEqFrom >= 0 AndAlso i >= tailEqFrom Then
                        res.Add(New List(Of Char) From {"="c})
                        Continue For
                    Else
                        baseSet = setB64
                    End If
                Case Else
                    baseSet = setDec
            End Select

            Dim finalSet As IEnumerable(Of Char) = baseSet
            If customSet IsNot Nothing Then
                finalSet = finalSet.Intersect(customSet)
            End If

            If frozenChar(i) Then
                finalSet = New Char() {visible(i)}
            End If

            Dim listChars = finalSet.ToList()
            If listChars.Count = 0 Then listChars = New List(Of Char) From {visible(i)}
            res.Add(listChars)
        Next
        Return res
    End Function

    ' ==================== ASCII：受限字符集 + MITM ====================
    Private Function GenerateCharset_ASCII(visible As String,
                                           origAscii As Byte(),
                                           infl As UInteger(,),
                                           perPosAlphabet As List(Of List(Of Byte)),
                                           maxToAdd As Integer,
                                           requireMicroEdit As Boolean,
                                           kind As String) As List(Of String)
        Dim res As New List(Of String)()
        If maxToAdd <= 0 Then Return res
        Dim rnd As New Random()
        Dim N = origAscii.Length

        'Dim needChars As Integer = If(kind = "dec", 12, If(kind = "base64", 6, 8))
        Dim needChars As Integer = AppGlobals.g_needChars - 1
        Dim candidate = Enumerable.Range(0, N).Where(Function(i) perPosAlphabet(i).Count > 1).ToList()
        If candidate.Count < needChars Then Return res
        Dim varPos = PickDistinct(rnd, candidate, needChars).OrderBy(Function(x) x).ToList()

        Dim perPosMax As Integer = If(kind = "dec", 5, 8)
        Dim options As New List(Of List(Of Byte))()
        Dim rr As New Random(314159)
        For Each p In varPos
            Dim allowed = perPosAlphabet(p)
            Dim setx As New HashSet(Of Byte) From {origAscii(p)}
            Dim attempts = 0
            While setx.Count < Math.Min(perPosMax, allowed.Count) AndAlso attempts < 1024
                attempts += 1
                setx.Add(allowed(rr.Next(allowed.Count)))
            End While
            options.Add(setx.ToList())
        Next

        Dim nb = CType(origAscii.Clone(), Byte())
        Dim S0 As UInteger = 0UI
        If requireMicroEdit Then
            Dim others = candidate.Except(varPos).ToList()
            Dim pos = If(others.Count > 0, others(rnd.Next(others.Count)), varPos(rnd.Next(varPos.Count)))
            Dim opts = perPosAlphabet(pos).Where(Function(b) b <> origAscii(pos)).ToList()
            If opts.Count = 0 Then Return res
            nb(pos) = opts(rnd.Next(opts.Count))
            Dim d = origAscii(pos) Xor nb(pos)
            If d <> 0 Then S0 = infl(pos, d)
        End If

        Dim half = varPos.Count \ 2
        Dim idxA = Enumerable.Range(0, half).ToArray()
        Dim idxB = Enumerable.Range(half, varPos.Count - half).ToArray()
        Dim mapA As New Dictionary(Of UInteger, List(Of Byte()))()
        EnumerateHalf_ASCII(origAscii, varPos, options, infl, idxA, 0, New List(Of Byte)(), 0UI, mapA)

        Dim added As Integer = 0
        EnumerateHalfWithMatch_ASCII(origAscii, varPos, options, infl, idxB, 0, New List(Of Byte)(), 0UI, mapA,
            Sub(joined As Byte())
                If added >= maxToAdd Then Return
                Dim outArr = CType(nb.Clone(), Byte())
                For i As Integer = 0 To varPos.Count - 1
                    outArr(varPos(i)) = joined(i)
                Next
                Dim outStr = Encoding.ASCII.GetString(outArr)
                If outStr <> visible Then
                    res.Add(outStr) : added += 1
                End If
            End Sub, OptionalXorOffset:=S0)

        Return res
    End Function

    Private Sub EnumerateHalf_ASCII(orig As Byte(), varPos As List(Of Integer), options As List(Of List(Of Byte)),
                                    infl As UInteger(,), idxs As Integer(), depth As Integer,
                                    chosen As List(Of Byte), acc As UInteger,
                                    mapA As Dictionary(Of UInteger, List(Of Byte())))
        If depth = idxs.Length Then
            If Not mapA.ContainsKey(acc) Then mapA(acc) = New List(Of Byte())()
            mapA(acc).Add(chosen.ToArray())
            Return
        End If
        Dim i = idxs(depth)
        Dim p = varPos(i)
        For Each c In options(i)
            Dim d = orig(p) Xor c
            Dim acc2 = acc
            If d <> 0 Then acc2 = acc2 Xor infl(p, d)
            chosen.Add(c)
            EnumerateHalf_ASCII(orig, varPos, options, infl, idxs, depth + 1, chosen, acc2, mapA)
            chosen.RemoveAt(chosen.Count - 1)
        Next
    End Sub

    Private Sub EnumerateHalfWithMatch_ASCII(orig As Byte(), varPos As List(Of Integer), options As List(Of List(Of Byte)),
                                             infl As UInteger(,), idxs As Integer(), depth As Integer,
                                             chosen As List(Of Byte), acc As UInteger,
                                             mapA As Dictionary(Of UInteger, List(Of Byte())),
                                             onSolution As Action(Of Byte()),
                                             Optional OptionalXorOffset As UInteger = 0UI)
        If depth = idxs.Length Then
            Dim need = acc Xor OptionalXorOffset
            If mapA.ContainsKey(need) Then
                For Each lhs In mapA(need)
                    Dim joined As Byte() = New Byte(varPos.Count - 1) {}
                    Array.Copy(lhs, 0, joined, 0, lhs.Length)
                    Array.Copy(chosen.ToArray(), 0, joined, lhs.Length, chosen.Count)
                    onSolution(joined)
                Next
            End If
            Return
        End If
        Dim i = idxs(depth)
        Dim p = varPos(i)
        For Each c In options(i)
            Dim d = orig(p) Xor c
            Dim acc2 = acc
            If d <> 0 Then acc2 = acc2 Xor infl(p, d)
            chosen.Add(c)
            EnumerateHalfWithMatch_ASCII(orig, varPos, options, infl, idxs, depth + 1, chosen, acc2, mapA, onSolution, OptionalXorOffset)
            chosen.RemoveAt(chosen.Count - 1)
        Next
    End Sub

    ' ==================== Unicode(UTF-16LE)：受限字符集 + MITM ====================
    Private Function GenerateCharset_Unicode(visible As String,
                                             inflB As UInteger(,),
                                             perPosChars As List(Of List(Of Char)),
                                             maxToAdd As Integer,
                                             requireMicroEdit As Boolean,
                                             kind As String) As List(Of String)
        Dim res As New List(Of String)()
        If maxToAdd <= 0 Then Return res

        Dim rnd As New Random()
        Dim Nch = visible.Length
        Dim utf16 = Encoding.Unicode ' UTF-16LE

        'Dim needChars As Integer = If(kind = "dec", 12, If(kind = "base64", 6, 8))
        Dim needChars As Integer = AppGlobals.g_needChars - 1
        Dim candidate = Enumerable.Range(0, Nch).Where(Function(i) perPosChars(i).Count > 1).ToList()
        If candidate.Count < needChars Then Return res

        Dim varPos = PickDistinct(rnd, candidate, needChars).OrderBy(Function(x) x).ToList()

        Dim perPosMax As Integer = If(kind = "dec", 5, 8)
        Dim perPosOptions As New List(Of List(Of Char))()
        Dim rr As New Random(314159)
        For Each p In varPos
            Dim allowed = perPosChars(p)
            Dim setx As New HashSet(Of Char) From {visible(p)}
            Dim attempts = 0
            While setx.Count < Math.Min(perPosMax, allowed.Count) AndAlso attempts < 1024
                attempts += 1
                setx.Add(allowed(rr.Next(allowed.Count)))
            End While
            perPosOptions.Add(setx.ToList())
        Next

        Dim baseChars = visible.ToCharArray()
        Dim S0 As UInteger = 0UI
        If requireMicroEdit Then
            Dim others = candidate.Except(varPos).ToList()
            Dim pos As Integer = If(others.Count > 0, others(rnd.Next(others.Count)), varPos(rnd.Next(varPos.Count)))
            Dim oldc0 As Char = baseChars(pos)
            Dim opts = perPosChars(pos).Where(Function(c) c <> oldc0).ToList()
            If opts.Count = 0 Then Return res
            Dim newc = opts(rnd.Next(opts.Count))

            Dim ob = utf16.GetBytes(New Char() {oldc0})
            Dim nb = utf16.GetBytes(New Char() {newc})
            Dim d0 = ob(0) Xor nb(0)
            Dim d1 = ob(1) Xor nb(1)
            If d0 <> 0 Then S0 = S0 Xor inflB(2 * pos, d0)
            If d1 <> 0 Then S0 = S0 Xor inflB(2 * pos + 1, d1)
            baseChars(pos) = newc
        End If

        Dim half = varPos.Count \ 2
        Dim idxA = Enumerable.Range(0, half).ToArray()
        Dim idxB = Enumerable.Range(half, varPos.Count - half).ToArray()

        Dim mapA As New Dictionary(Of UInteger, List(Of Char()))()
        EnumerateHalf_Unicode(visible.ToCharArray(), inflB, varPos, perPosOptions, idxA, 0, New List(Of Char)(), 0UI, mapA)

        Dim added As Integer = 0
        EnumerateHalfWithMatch_Unicode(visible.ToCharArray(), inflB, varPos, perPosOptions, idxB, 0, New List(Of Char)(), 0UI, mapA,
            Sub(joined As Char())
                If added >= maxToAdd Then Return
                Dim outChars = CType(baseChars.Clone(), Char())
                For i As Integer = 0 To varPos.Count - 1
                    outChars(varPos(i)) = joined(i)
                Next
                Dim outStr = New String(outChars)
                If outStr <> visible Then
                    res.Add(outStr) : added += 1
                End If
            End Sub, OptionalXorOffset:=S0)

        Return res
    End Function

    Private Sub EnumerateHalf_Unicode(visible As Char(), inflB As UInteger(,), varPos As List(Of Integer),
                                      options As List(Of List(Of Char)), idxs As Integer(), depth As Integer,
                                      chosen As List(Of Char), acc As UInteger,
                                      mapA As Dictionary(Of UInteger, List(Of Char())))
        If depth = idxs.Length Then
            If Not mapA.ContainsKey(acc) Then mapA(acc) = New List(Of Char())()
            mapA(acc).Add(chosen.ToArray())
            Return
        End If
        Dim i = idxs(depth)
        Dim p = varPos(i)
        Dim oldc As Char = visible(p)
        For Each c In options(i)
            Dim acc2 = acc
            If c <> oldc Then
                Dim ob = Encoding.Unicode.GetBytes(New Char() {oldc})
                Dim nb = Encoding.Unicode.GetBytes(New Char() {c})
                Dim d0 = ob(0) Xor nb(0)
                Dim d1 = ob(1) Xor nb(1)
                If d0 <> 0 Then acc2 = acc2 Xor inflB(2 * p, d0)
                If d1 <> 0 Then acc2 = acc2 Xor inflB(2 * p + 1, d1)
            End If
            chosen.Add(c)
            EnumerateHalf_Unicode(visible, inflB, varPos, options, idxs, depth + 1, chosen, acc2, mapA)
            chosen.RemoveAt(chosen.Count - 1)
        Next
    End Sub

    Private Sub EnumerateHalfWithMatch_Unicode(visible As Char(), inflB As UInteger(,), varPos As List(Of Integer),
                                               options As List(Of List(Of Char)), idxs As Integer(), depth As Integer,
                                               chosen As List(Of Char), acc As UInteger,
                                               mapA As Dictionary(Of UInteger, List(Of Char())),
                                               onSolution As Action(Of Char()),
                                               Optional OptionalXorOffset As UInteger = 0UI)
        If depth = idxs.Length Then
            Dim need = acc Xor OptionalXorOffset
            If mapA.ContainsKey(need) Then
                For Each lhs In mapA(need)
                    Dim joined As Char() = New Char(varPos.Count - 1) {}
                    Array.Copy(lhs, 0, joined, 0, lhs.Length)
                    Array.Copy(chosen.ToArray(), 0, joined, lhs.Length, chosen.Count)
                    onSolution(joined)
                Next
            End If
            Return
        End If
        Dim i = idxs(depth)
        Dim p = varPos(i)
        Dim oldc As Char = visible(p) ' 显式声明，修复未声明错误
        For Each c In options(i)
            Dim acc2 = acc
            If c <> oldc Then
                Dim ob = Encoding.Unicode.GetBytes(New Char() {oldc})
                Dim nb = Encoding.Unicode.GetBytes(New Char() {c})
                Dim d0 = ob(0) Xor nb(0)
                Dim d1 = ob(1) Xor nb(1)
                If d0 <> 0 Then acc2 = acc2 Xor inflB(2 * p, d0)
                If d1 <> 0 Then acc2 = acc2 Xor inflB(2 * p + 1, d1)
            End If
            chosen.Add(c)
            EnumerateHalfWithMatch_Unicode(visible, inflB, varPos, options, idxs, depth + 1, chosen, acc2, mapA, onSolution, OptionalXorOffset)
            chosen.RemoveAt(chosen.Count - 1)
        Next
    End Sub

    ' ==================== 冻结掩码（1-based 闭区间解析） ====================
    Private Function BuildFrozenMask(rangeSpec As String, length As Integer) As Boolean()
        Dim mask As Boolean() = New Boolean(length - 1) {}
        If String.IsNullOrWhiteSpace(rangeSpec) Then Return mask
        Dim parts = rangeSpec.Replace(","c, " "c).Split({" "c, vbTab}, StringSplitOptions.RemoveEmptyEntries)
        For Each tok In parts
            Dim seg As String = tok.Trim()
            If seg.Length = 0 Then Continue For
            Dim a As Integer, b As Integer
            If seg.Contains("-"c) Then
                Dim ab = seg.Split("-"c)
                If ab.Length <> 2 Then Throw New ArgumentException(CStr(Loc.T("Text.d3cf3dba", "冻结区格式错误：")) & seg)
                a = Integer.Parse(ab(0))
                b = Integer.Parse(ab(1))
            Else
                a = Integer.Parse(seg) : b = a
            End If
            If a <= 0 OrElse b <= 0 OrElse a > b Then Throw New ArgumentException(CStr(Loc.T("Text.374b7ed4", "冻结区非法：")) & seg)
            Dim L = Math.Max(0, a - 1)
            Dim R = Math.Min(length - 1, b - 1)
            For i = L To R : mask(i) = True : Next
        Next
        Return mask
    End Function

    ' ==================== CRC32 & 工具 ====================

    Private Function BuildInfluenceTable(len As Integer) As UInteger(,)
        Dim tbl As UInteger(,) = New UInteger(len - 1, 255) {}
        Dim tmp As Byte() = New Byte(len - 1) {}
        For pos As Integer = 0 To len - 1
            For d As Integer = 0 To 255
                Array.Clear(tmp, 0, tmp.Length)
                tmp(pos) = CByte(d)
                tbl(pos, d) = Crc32Linear(tmp)
            Next
        Next
        Return tbl
    End Function


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


    Private Function Crc32Linear(data As Byte()) As UInteger
        ' 线性 CRC（init=0, xorout=0），仅用于影响表与线性求解
        Dim c As UInteger = 0UI
        For i As Integer = 0 To data.Length - 1
            Dim idx As Integer = CInt((c Xor data(i)) And &HFFUI)
            c = CrcTable(idx) Xor (c >> 8)
        Next
        Return c
    End Function

    Private Function Crc32(data As Byte()) As UInteger
        Dim c As UInteger = &HFFFFFFFFUI
        For i As Integer = 0 To data.Length - 1
            Dim idx As Integer = CInt((c Xor data(i)) And &HFFUI)
            c = CrcTable(idx) Xor (c >> 8)
        Next
        Return c Xor &HFFFFFFFFUI
    End Function

    Private Sub ValidateHexText(hexText As String)
        If (hexText.Length Mod 2) <> 0 Then Throw New ArgumentException(CStr(Loc.T("Text.75a687a8", "hex 文本长度必须为偶数（2N）")))
        For Each ch In hexText
            Dim ok = (ch >= "0"c AndAlso ch <= "9"c) OrElse
                     (ch >= "a"c AndAlso ch <= "f"c) OrElse
                     (ch >= "A"c AndAlso ch <= "F"c)
            If Not ok Then Throw New ArgumentException(CStr(Loc.T("Text.1f8e8c15", "hex 含非法字符：")) & ch)
        Next
    End Sub

    Private Sub ValidateBase64Form(b64 As String)
        Dim baseSet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"
        For i As Integer = 0 To b64.Length - 1
            Dim ch = b64(i)
            If ch = "="c Then
                For j As Integer = i To b64.Length - 1
                    If b64(j) <> "="c Then Throw New ArgumentException(CStr(Loc.T("Text.77a6ab09", "base64 仅允许尾部 '='")))
                Next
                Exit For
            Else
                If baseSet.IndexOf(ch) < 0 Then Throw New ArgumentException(CStr(Loc.T("Text.3c859af0", "base64 含非法字符：")) & ch)
            End If
        Next
    End Sub

    Private Function ParseHexToBytes(hex As String) As Byte()
        Dim n = hex.Length \ 2
        Dim b(n - 1) As Byte
        For i As Integer = 0 To n - 1
            b(i) = Convert.ToByte(hex.Substring(2 * i, 2), 16)
        Next
        Return b
    End Function

    Private Function BytesToHexLower(data As Byte()) As String
        Dim sb As New StringBuilder(data.Length * 2)
        For Each x In data
            sb.Append(x.ToString("x2"))
        Next
        Return sb.ToString()
    End Function

    Private Function BytesToHexUpper(data As Byte()) As String
        Dim sb As New StringBuilder(data.Length * 2)
        For Each x In data
            sb.Append(x.ToString("X2"))
        Next
        Return sb.ToString()
    End Function

    Private Function PickDistinct(rnd As Random, pool As IEnumerable(Of Integer), k As Integer) As List(Of Integer)
        Dim arr = pool.ToList()
        If k > arr.Count Then Throw New ArgumentException(CStr(Loc.T("Text.685ca427", "可选数量不足")))
        Dim res As New List(Of Integer)(k)
        For i As Integer = 1 To k
            Dim idx = rnd.Next(arr.Count)
            res.Add(arr(idx))
            arr.RemoveAt(idx)
        Next
        Return res
    End Function

    <Runtime.CompilerServices.Extension>
    Private Function ToHashSet(s As String) As HashSet(Of Char)
        Return New HashSet(Of Char)(s.ToCharArray())
    End Function
    ' 随机填充：使用 allowedChars，在 frozenRanges 之外随机替换 inputStr 的字符
    ' 额外规则：避开等号（=）
    '   1) 不会改动 inputStr 中现有的 '='
    '   2) 从 allowedChars 中排除 '=' 后再进行随机选择
    Public Function RandomFillUsingAllowedChars(inputStr As String,
                                            frozenRanges As String,
                                            allowedChars As String,
                                            Optional seed As Integer? = Nothing) As String
        If String.IsNullOrEmpty(inputStr) Then Return inputStr
        If String.IsNullOrEmpty(allowedChars) Then
            Throw New ArgumentException(CStr(Loc.T("Text.245362a2", "allowedChars 不能为空")))
        End If

        ' 去重 + 排除 '='
        Dim uniq As New List(Of Char)
        Dim seen As New HashSet(Of Char)
        For Each ch In allowedChars
            If ch <> "="c AndAlso Not seen.Contains(ch) Then
                seen.Add(ch)
                uniq.Add(ch)
            End If
        Next
        If uniq.Count = 0 Then
            Throw New ArgumentException(CStr(Loc.T("Text.fa6fd39e", "allowedChars 除去 '=' 后为空，无法进行随机填充")))
        End If

        Dim chars = inputStr.ToCharArray()
        Dim L = chars.Length

        ' 与 GenerateZeroDeltaVariants 相同的冻结规则（按字符维度）
        Dim freeze As Boolean() = BuildFrozenMask(frozenRanges, L)

        ' 额外冻结：现有等号不改动
        For i As Integer = 0 To L - 1
            If chars(i) = "="c Then
                freeze(i) = True
            End If
        Next

        ' 随机源
        Dim rnd As Random = If(seed.HasValue, New Random(seed.Value), New Random(Guid.NewGuid().GetHashCode()))

        ' 在未冻结的位置随机填充（避开 '='）
        For i As Integer = 0 To L - 1
            If Not freeze(i) Then
                chars(i) = uniq(rnd.Next(0, uniq.Count))
            End If
        Next

        Return New String(chars)
    End Function
    ' 对比 A 和 B，把差异位置输出为“冻结区域格式”（1-based 闭区间，逗号分隔）
    ' 规则：
    '   - 以 A 为基础长度；若 B 过长则先截断到 A.Length
    '   - 若 B 比 A 短，则 A 的尾部视为“差异”
    ' 返回示例："4,7-9,12"
    Public Function DiffToFrozenRanges(a As String, b As String) As String
        If a Is Nothing Then a = ""
        If b Is Nothing Then b = ""
        If b.Length > a.Length Then b = b.Substring(0, a.Length)

        Dim n As Integer = a.Length
        If n = 0 Then Return ""

        Dim sb As New System.Text.StringBuilder()
        Dim i As Integer = 0
        While i < n
            Dim isDiff As Boolean =
                (i >= b.Length) OrElse (a.Chars(i) <> b.Chars(i))

            If isDiff Then
                Dim start1 As Integer = i + 1 ' 1-based
                i += 1
                While i < n AndAlso ((i >= b.Length) OrElse (a.Chars(i) <> b.Chars(i)))
                    i += 1
                End While
                Dim end1 As Integer = i ' 1-based
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
    ' 1) 将字符串中“非 * 的连续片段”转换为冻结区域格式（1-based 闭区间、逗号分隔）
    '    例如： "***ab*cd**" -> "4-5,7-8"
    Public Function NonStarToFrozenRanges(s As String) As String
        If s Is Nothing Then Return ""
        Dim n As Integer = s.Length
        If n = 0 Then Return ""

        Dim sb As New System.Text.StringBuilder()
        Dim i As Integer = 0
        While i < n
            If s.Chars(i) <> "*"c Then
                Dim start1 As Integer = i + 1 ' 1-based
                i += 1
                While i < n AndAlso s.Chars(i) <> "*"c
                    i += 1
                End While
                Dim [end] As Integer = i ' 1-based
                If sb.Length > 0 Then sb.Append(","c)
                If start1 = [end] Then
                    sb.Append(start1.ToString())
                Else
                    sb.Append(start1.ToString()).Append("-"c).Append([end].ToString())
                End If
            Else
                i += 1
            End If
        End While

        Return sb.ToString()
    End Function

    ' 2) 用 b 的“非 *”字符覆盖 a 的对应位置；b 超过 a 的长度则截断到 a.Length
    '    例如： a="abcdef", b="**X*Y"  -> 结果 "abXdeY"
    Public Function OverlayNonStar(a As String, b As String) As String
        If a Is Nothing Then a = ""
        If b Is Nothing Then b = ""
        If b.Length > a.Length Then b = b.Substring(0, a.Length)
        If a.Length = 0 Then Return ""

        Dim sb As New System.Text.StringBuilder(a)
        For i As Integer = 0 To b.Length - 1
            Dim ch As Char = b.Chars(i)
            If ch <> "*"c Then
                sb.Chars(i) = ch
            End If
        Next
        Return sb.ToString()
    End Function

    ' —— 异步：多解 —— '
    Public Function GenerateZeroDeltaVariantsAsync(
        inputStr As String,
        kind As String,
        maxResults As Integer,
        Optional allowRandomFix As Boolean = True,
        Optional unicodeMode As Boolean = False,
        Optional frozenRanges As String = Nothing,
        Optional allowedChars As String = Nothing,
        Optional ct As CancellationToken = Nothing
    ) As Task(Of List(Of String))
        ' 把CPU密集的同步实现丢到线程池，避免卡UI
        Return Task.Run(Function()
                            ct.ThrowIfCancellationRequested()
                            Return GenerateZeroDeltaVariants(inputStr, kind, maxResults, allowRandomFix, unicodeMode, frozenRanges, allowedChars)
                        End Function, ct)
    End Function

    ' —— 异步：AB对齐（返回一个结果或 Nothing） —— '
    Public Function GenerateZeroDeltaVariantsABAsync(
        inputStr As String,
        inputStrB As String,
        kind As String,
        Optional allowRandomFix As Boolean = True,
        Optional unicodeMode As Boolean = False,
        Optional frozenRanges As String = Nothing,
        Optional allowedChars As String = Nothing,
        Optional ct As CancellationToken = Nothing
    ) As Task(Of String)
        Return Task.Run(Function()
                            ct.ThrowIfCancellationRequested()
                            Return GenerateZeroDeltaVariantsAB(inputStr, inputStrB, kind, allowRandomFix, unicodeMode, frozenRanges, allowedChars)
                        End Function, ct)
    End Function
End Module