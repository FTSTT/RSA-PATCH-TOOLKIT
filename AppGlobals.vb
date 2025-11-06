' AppGlobals.vb
Public NotInheritable Class AppGlobals
    ' A 框原始字符串（只读显示）
    Public Shared g_InputStr As String = ""

    ' 允许输入的字符集（为空则不限制）；"*" 始终允许
    Public Shared g_AllowedChars As String = ""

    ' B 框保存的掩码
    Public Shared g_SavedMask As String = ""

    ' “保存并随机填充种子”的结果字符串（窗体打开时置为 Nothing）
    Public Shared g_InputStr_rnd As String = Nothing

    Public Shared g_frozenRanges As String = Nothing

    Public Shared g_needChars As Integer = 7

End Class
