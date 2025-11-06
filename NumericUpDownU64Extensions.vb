' NumericUpDownU64Extensions.vb
Option Strict On
Option Explicit On
Imports System.Runtime.CompilerServices

Public Module NumericUpDownU64Extensions

    ''' <summary>把 NumericUpDown 配置为支持 ULong，并可指定最小/最大值与步进。</summary>
    <Extension>
    Public Sub ConfigureAsUInt64(nud As NumericUpDown,
                                 Optional minULong As ULong = 0UL,
                                 Optional maxULong As ULong = ULong.MaxValue,
                                 Optional increment As ULong = 1UL,
                                 Optional useThousandsSeparator As Boolean = True)
        nud.DecimalPlaces = 0
        nud.Minimum = Convert.ToDecimal(minULong)
        nud.Maximum = Convert.ToDecimal(maxULong)
        nud.Increment = Convert.ToDecimal(increment)
        nud.ThousandsSeparator = useThousandsSeparator
        nud.TextAlign = HorizontalAlignment.Right
    End Sub

    <Extension>
    Public Function GetUInt64(nud As NumericUpDown) As ULong
        Dim d As Decimal = Decimal.Truncate(nud.Value)
        If d <= 0D Then Return 0UL
        Dim maxU As Decimal = Convert.ToDecimal(ULong.MaxValue)
        If d >= maxU Then Return ULong.MaxValue
        Return Convert.ToUInt64(d)
    End Function

    <Extension>
    Public Sub SetUInt64(nud As NumericUpDown, value As ULong)
        Dim dec As Decimal = Convert.ToDecimal(value)
        If dec < nud.Minimum Then dec = nud.Minimum
        If dec > nud.Maximum Then dec = nud.Maximum
        nud.Value = dec
    End Sub

End Module
