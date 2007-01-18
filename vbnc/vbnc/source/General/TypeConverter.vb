' 
' Visual Basic.Net Compiler
' Copyright (C) 2004 - 2007 Rolf Bjarne Kvinge, RKvinge@novell.com
' 
' This library is free software; you can redistribute it and/or
' modify it under the terms of the GNU Lesser General Public
' License as published by the Free Software Foundation; either
' version 2.1 of the License, or (at your option) any later version.
' 
' This library is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY; without even the implied warranty of
' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
' Lesser General Public License for more details.
' 
' You should have received a copy of the GNU Lesser General Public
' License along with this library; if not, write to the Free Software
' Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
' 


Public Class TypeConverter

    '0 = A Empty          A
    '1 = B Object         B
    '2 = C DBNull         C
    '3 = D Boolean        D
    '4 = E Char           E
    '5 = F SByte          F
    '6 = G Byte           G
    '7 = H Int16(Short)   H
    '8 = I UInt16(UShort) I
    '9 = J Int32          J
    '10= K UInt32         K 
    '11= L Int64(Long)    L
    '12= M UInt64(ULong)  M
    '13= N Single         N
    '14= O Double         O
    '15= P Decimal        P
    '16= Q DateTime       Q
    '17= - 17             -
    '18= S String         S

    Public Shared LikeResultType As String = "" & _
            "XXXXXXXXXXXXXXXXX-X" & _
            "XBXBBBBBBBBBBBBBB-B" & _
            "XXXXXXXXXXXXXXXXX-X" & _
            "XBXDDDDDDDDDDDDDD-D" & _
            "XBXDDDDDDDDDDDDDD-D" & _
            "XBXDDDDDDDDDDDDDD-D" & _
            "XBXDDDDDDDDDDDDDD-D" & _
            "XBXDDDDDDDDDDDDDD-D" & _
            "XBXDDDDDDDDDDDDDD-D" & _
            "XBXDDDDDDDDDDDDDD-D" & _
            "XBXDDDDDDDDDDDDDD-D" & _
            "XBXDDDDDDDDDDDDDD-D" & _
            "XBXDDDDDDDDDDDDDD-D" & _
            "XBXDDDDDDDDDDDDDD-D" & _
            "XBXDDDDDDDDDDDDDD-D" & _
            "XBXDDDDDDDDDDDDDD-D" & _
            "XBXDDDDDDDDDDDDDD-D" & _
            "-------------------" & _
            "XBXDDDDDDDDDDDDDD-D"

    Public Shared LikeOperandType As String = "" & _
                "XXXXXXXXXXXXXXXXX-X" & _
                "XBXBBBBBBBBBBBBBB-B" & _
                "XXXXXXXXXXXXXXXXX-X" & _
                "XBXSSSSSSSSSSSSSS-S" & _
                "XBXSSSSSSSSSSSSSS-S" & _
                "XBXSSSSSSSSSSSSSS-S" & _
                "XBXSSSSSSSSSSSSSS-S" & _
                "XBXSSSSSSSSSSSSSS-S" & _
                "XBXSSSSSSSSSSSSSS-S" & _
                "XBXSSSSSSSSSSSSSS-S" & _
                "XBXSSSSSSSSSSSSSS-S" & _
                "XBXSSSSSSSSSSSSSS-S" & _
                "XBXSSSSSSSSSSSSSS-S" & _
                "XBXSSSSSSSSSSSSSS-S" & _
                "XBXSSSSSSSSSSSSSS-S" & _
                "XBXSSSSSSSSSSSSSS-S" & _
                "XBXSSSSSSSSSSSSSS-S" & _
                "-------------------" & _
                "XBXSSSSSSSSSSSSSS-S"

    Public Shared ConcatResultType As String = LikeOperandType

    Public Shared ConcatOperandType As String = LikeOperandType

    Public Shared ModResultType As String = "" & _
            "XXXXXXXXXXXXXXXXX-X" & _
            "XBXBXBBBBBBBBBBBX-B" & _
            "XXXXXXXXXXXXXXXXX-X" & _
            "XBXFXFHHJJLLPNOPX-O" & _
            "XXXXXXXXXXXXXXXXX-X" & _
            "XBXFXFHHJJLLPNOPX-O" & _
            "XBXHXHGHIJKLMNOPX-O" & _
            "XBXHXHHHJJLLPNOPX-O" & _
            "XBXJXJIJIJKLMNOPX-O" & _
            "XBXJXJJJJJLLPNOPX-O" & _
            "XBXLXLKLKLKLMNOPX-O" & _
            "XBXLXLLLLLLLPNOPX-O" & _
            "XBXPXPMPMPMPMNOPX-O" & _
            "XBXNXNNNNNNNNNOPX-O" & _
            "XBXOXOOOOOOOOOOOX-O" & _
            "XBXPXPPPPPPPPPOPX-O" & _
            "XXXXXXXXXXXXXXXXX-X" & _
            "-------------------" & _
            "XBXOXOOOOOOOOOOOX-O"

    Public Shared IntDivResultTypes As String = "" & _
            "XXXXXXXXXXXXXXXXX-X" & _
            "XBXBXBBBBBBBBBBBB-B" & _
            "XXXXXXXXXXXXXXXXX-X" & _
            "XBXFXHHJJJLLLLLLX-L" & _
            "XXXXXXXXXXXXXXXXX-X" & _
            "XBXHXGHJJJLLLLLLX-L" & _
            "XBXHXHHIIJKLMLLLX-L" & _
            "XBXJXJIIJJLLLLLLX-L" & _
            "XBXJXJIJIJKLMLLLX-L" & _
            "XBXJXJJJJJLLLLLLX-L" & _
            "XBXLXLKLKLKLMLLLX-L" & _
            "XBXLXLLLLLLLLLLLX-L" & _
            "XBXLXLMLMLMLMLLLX-L" & _
            "XBXLXLLLLLLLLLLLX-L" & _
            "XBXLXLLLLLLLLLLLX-L" & _
            "XBXLXLLLLLLLLLLLX-L" & _
            "XBXXXXXXXXXXXXXXX-L" & _
            "-------------------" & _
            "XBXLXLLLLLLLLLLLL-L"

    Public Shared RealDivResultTypes As String = "" & _
            "XXXXXXXXXXXXXXXXX-X" & _
            "XBXBXBBBBBBBBBBBX-B" & _
            "XXXXXXXXXXXXXXXXX-X" & _
            "XBXOXOOOOOOOONXOX-O" & _
            "XXXXXXXXXXXXXXXXX-X" & _
            "XBXOXOOOOOOOONOOX-O" & _
            "XBXOXOOOOOOOONOOX-O" & _
            "XBXOXOOOOOOOONOOX-O" & _
            "XBXOXOOOOOOOONOOX-O" & _
            "XBXOXOOOOOOOONOOX-O" & _
            "XBXOXOOOOOOOONOOX-O" & _
            "XBXOXOOOOOOOONOOX-O" & _
            "XBXOXOOOOOOOONOOX-O" & _
            "XBXNXNNNNNNNNNOOX-O" & _
            "XBXOXOOOOOOOOOOOX-O" & _
            "XBXOXOOOOOOOOOOOX-O" & _
            "XXXXXXXXXXXXXXXXX-X" & _
            "-------------------" & _
            "XBXOXOOOOOOOOOOOX-O"

    Public Shared AddResultType As String = "" & _
            "XXXXXXXXXXXXXXXXX-X" & _
            "XBXBBBBBBBBBBBBBB-B" & _
            "XXXXXXXXXXXXXXXXX-X" & _
            "XBXFXFHHJJLLPNOPX-O" & _
            "XBXXSXXXXXXXXXXXX-S" & _
            "XBXFXFHHJJLLPNOPX-O" & _
            "XBXHXHGHIJKLMNOPX-O" & _
            "XBXHXHHHJJLLPNOPX-O" & _
            "XBXJXJIJIJKLMNOPX-O" & _
            "XBXJXJJJJJLLPNOPX-O" & _
            "XBXLXLKLKLKLMNOPX-O" & _
            "XBXLXLLLLLLLPNOPX-O" & _
            "XBXPXPMPMPMPMNOPX-O" & _
            "XBXNXNNNNNNNNNOPX-O" & _
            "XBXOXOOOOOOOOOOOX-O" & _
            "XBXPXPPPPPPPPPOPX-O" & _
            "XBXXXXXXXXXXXXXXX-S" & _
            "-------------------" & _
            "XBXOSOOOOOOOOOOOS-S"

    Public Shared SubResultType As String = "" & _
            "XXXXXXXXXXXXXXXXX-X" & _
            "XBXBXBBBBBBBBBBBB-B" & _
            "XXXXXXXXXXXXXXXXX-X" & _
            "XBXFXFHHJJLLPNOPX-O" & _
            "XXXXSXXXXXXXXXXXX-X" & _
            "XBXFXFHHJJLLPNOPX-O" & _
            "XBXHXHGHIJKLMNOPX-O" & _
            "XBXHXHHHJJLLPNOPX-O" & _
            "XBXJXJIJIJKLMNOPX-O" & _
            "XBXJXJJJJJLLPNOPX-O" & _
            "XBXLXLKLKLKLMNOPX-O" & _
            "XBXLXLLLLLLLPNOPX-O" & _
            "XBXPXPMPMPMPMNOPX-O" & _
            "XBXNXNNNNNNNNNOPX-O" & _
            "XBXOXOOOOOOOOOOOX-O" & _
            "XBXPXPPPPPPPPPOPX-O" & _
            "XBXXXXXXXXXXXXXXX-X" & _
            "-------------------" & _
            "XBXOXOOOOOOOOOOOX-O"

    Public Shared MultResultType As String = SubResultType

    Public Shared ShortcircuitResultType As String = "" & _
            "XXXXXXXXXXXXXXXXX-X" & _
            "XBXBXBBBBBBBBBBBX-B" & _
            "XXXXXXXXXXXXXXXXX-X" & _
            "XBXDXDDDDDDDDDDDX-D" & _
            "XXXXXXXXXXXXXXXXX-X" & _
            "XBXDXDDDDDDDDDDDX-D" & _
            "XBXDXDDDDDDDDDDDX-D" & _
            "XBXDXDDDDDDDDDDDX-D" & _
            "XBXDXDDDDDDDDDDDX-D" & _
            "XBXDXDDDDDDDDDDDX-D" & _
            "XBXDXDDDDDDDDDDDX-D" & _
            "XBXDXDDDDDDDDDDDX-D" & _
            "XBXDXDDDDDDDDDDDX-D" & _
            "XBXDXDDDDDDDDDDDX-D" & _
            "XBXDXDDDDDDDDDDDX-D" & _
            "XBXDXDDDDDDDDDDDX-D" & _
            "XXXXXXXXXXXXXXXXX-X" & _
            "-------------------" & _
            "XBXDXDDDDDDDDDDDX-D"

    Public Shared LogicalOperatorResultType As String = "" & _
            "XXXXXXXXXXXXXXXXX-X" & _
            "XBXBXBBBBBBBBBBBX-B" & _
            "XXXXXXXXXXXXXXXXX-X" & _
            "XBXDXFHHJJLLLLLLX-D" & _
            "XXXXXXXXXXXXXXXXX-X" & _
            "XBXFXFHHJJLLLLLLX-L" & _
            "XBXHXHGHIJKLMLLLX-L" & _
            "XBXHXHHHJJLLLLLLX-L" & _
            "XBXJXJIJIJKLMLLLX-L" & _
            "XBXJXJJJJJLLLLLLX-L" & _
            "XBXLXLKLKLKLMLLLX-L" & _
            "XBXLXLLLLLLLLLLLX-L" & _
            "XBXLXLMLMLMLMLLLX-L" & _
            "XBXLXLLLLLLLLLLLX-L" & _
            "XBXLXLLLLLLLLLLLX-L" & _
            "XBXLXLLLLLLLLLLLX-L" & _
            "XXXXXXXXXXXXXXXXX-X" & _
            "-------------------" & _
            "XBXDXLLLLLLLLLLLX-L"

    Public Shared BinaryOperandTypes As String = _
            "XXXXXXXXXXXXXXXXX-X" & _
            "XBXBBBBBBBBBBBBBB-B" & _
            "XXXXXXXXXXXXXXXXX-X" & _
            "XBXDXFHHJJPLPNOPX-D" & _
            "XBXXEXXXXXXXXXXXX-S" & _
            "XBXFXFHHJJPLPNOPX-O" & _
            "XBXHXHGHIJKLMNOPX-O" & _
            "XBXHXHHHJJPLPNOPX-O" & _
            "XBXIXJIJIJKLMNOPX-O" & _
            "XBXJXJJJJJPLPNOPX-O" & _
            "XBXPXPKPKPKLMNOPX-O" & _
            "XBXLXLLLLLLLPNOPX-O" & _
            "XBXPXPMPMPMPMNOPX-O" & _
            "XBXNXNNNNNNNNNOPX-O" & _
            "XBXOXOOOOOOOOOOPX-O" & _
            "XBXPXPPPPPPPPPPPX-O" & _
            "XBXXXXXXXXXXXXXXQ-Q" & _
            "-------------------" & _
            "XBXDSOOOOOOOOOOOQ-S"


    Public Shared ExponentResultTypes As String = _
            "XXXXXXXXXXXXXXXXX-X" & _
            "XBXBXBBBBBBBBBBBX-B" & _
            "XXXXXXXXXXXXXXXXX-X" & _
            "XBXOXOOOOOOOOOOOX-O" & _
            "XXXXXXXXXXXXXXXXX-X" & _
            "XBXOXOOOOOOOOOOOX-O" & _
            "XBXOXOOOOOOOOOOOX-O" & _
            "XBXOXOOOOOOOOOOOX-O" & _
            "XBXOXOOOOOOOOOOOX-O" & _
            "XBXOXOOOOOOOOOOOX-O" & _
            "XBXOXOOOOOOOOOOOX-O" & _
            "XBXOXOOOOOOOOOOOX-O" & _
            "XBXOXOOOOOOOOOOOX-O" & _
            "XBXOXOOOOOOOOOOOX-O" & _
            "XBXOXOOOOOOOOOOOX-O" & _
            "XBXOXOOOOOOOOOOOX-O" & _
            "XXXXXXXXXXXXXXXXX-X" & _
            "-------------------" & _
            "XBXOXOOOOOOOOOOOX-O"

    Public Shared NotOperatorResultType As String = "XBXDXFGHIJKLMLLLX-L"

    Public Shared UnaryPlusResultType As String = "XBXFXFGHIJKLMNOPX-O"

    Public Shared UnaryMinusResultType As String = "XBXFXFHHJJLLPNOPX-O"

    Public Shared ShiftResultType2 As String = "XBXFXFGHIJKLMLLLX-L"

    Public Shared ShiftResultType As String = _
 "XXXXXXXXXXXXXXXXX-X" & _
 "XBXFXFGHIJKLMLLLX-L" & _
 "XXXXXXXXXXXXXXXXX-X" & _
 "XBXFXFGHIJKLMLLLX-L" & _
 "XXXXXXXXXXXXXXXXX-X" & _
 "XBXFXFGHIJKLMLLLX-L" & _
 "XBXFXFGHIJKLMLLLX-L" & _
 "XBXFXFGHIJKLMLLLX-L" & _
 "XBXFXFGHIJKLMLLLX-L" & _
 "XBXFXFGHIJKLMLLLX-L" & _
 "XBXFXFGHIJKLMLLLX-L" & _
 "XBXFXFGHIJKLMLLLX-L" & _
 "XBXFXFGHIJKLMLLLX-L" & _
 "XBXFXFGHIJKLMLLLX-L" & _
 "XBXFXFGHIJKLMLLLX-L" & _
 "XBXFXFGHIJKLMLLLX-L" & _
 "XXXXXXXXXXXXXXXXX-X" & _
 "-------------------" & _
 "XBXFXFGHIJKLMLLLX-L"


    Shared Function GetExpOperandType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        Return GetExpResultType(op1, op2)
    End Function

    Shared Function GetEqualsOperandType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        Return GetResultType(op1, op2, BinaryOperandTypes)
    End Function

    Shared Function GetLTOperandType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        Return GetResultType(op1, op2, BinaryOperandTypes)
    End Function

    Shared Function GetGTOperandType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        Return GetResultType(op1, op2, BinaryOperandTypes)
    End Function

    Shared Function GetLEOperandType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        Return GetResultType(op1, op2, BinaryOperandTypes)
    End Function

    Shared Function GetGEOperandType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        Return GetResultType(op1, op2, BinaryOperandTypes)
    End Function

    Shared Function GetNotEqualsOperandType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        Return GetResultType(op1, op2, BinaryOperandTypes)
    End Function

    Shared Function GetShiftResultType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        Return GetResultType(op1, op2, ShiftResultType)
    End Function

    Shared Function GetModResultType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        Return GetResultType(op1, op2, ModResultType)
    End Function

    Shared Function GetLikeResultType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        Return GetResultType(op1, op2, LikeResultType)
    End Function

    Shared Function GetLikeOperandType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        Return GetResultType(op1, op2, LikeOperandType)
    End Function

    Shared Function GetConcatResultType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        Return GetResultType(op1, op2, ConcatResultType)
    End Function

    Shared Function GetConcatOperandType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        Return GetResultType(op1, op2, ConcatOperandType)
    End Function

    Shared Function GetRealDivResultType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        Return GetResultType(op1, op2, RealDivResultTypes)
    End Function

    Shared Function GetIntDivResultType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        Return GetResultType(op1, op2, IntDivResultTypes)
    End Function

    Shared Function GetExpResultType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        Return GetResultType(op1, op2, ExponentResultTypes)
    End Function

    Shared Function GetEqualsResultType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        If GetEqualsOperandType(op1, op2) = TypeCode.Empty Then Return TypeCode.Empty
        Return TypeCode.Boolean
    End Function

    Shared Function GetLTResultType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        If GetLTOperandType(op1, op2) = TypeCode.Empty Then Return TypeCode.Empty
        Return TypeCode.Boolean
    End Function

    Shared Function GetGTResultType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        If GetGTOperandType(op1, op2) = TypeCode.Empty Then Return TypeCode.Empty
        Return TypeCode.Boolean
    End Function

    Shared Function GetLEResultType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        If GetLEOperandType(op1, op2) = TypeCode.Empty Then Return TypeCode.Empty
        Return TypeCode.Boolean
    End Function

    Shared Function GetGEResultType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        If GetGEOperandType(op1, op2) = TypeCode.Empty Then Return TypeCode.Empty
        Return TypeCode.Boolean
    End Function

    Shared Function GetNotEqualsResultType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        If GetNotEqualsOperandType(op1, op2) = TypeCode.Empty Then Return TypeCode.Empty
        Return TypeCode.Boolean
    End Function

    Shared Function GetUnaryMinusResultType(ByVal op1 As TypeCode) As TypeCode
        Return GetResultType(op1, UnaryMinusResultType)
    End Function

    Shared Function GetUnaryPlusResultType(ByVal op1 As TypeCode) As TypeCode
        Return GetResultType(op1, UnaryPlusResultType)
    End Function

    Shared Function GetAndResultType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        Return GetResultType(op1, op2, LogicalOperatorResultType)
    End Function

    Shared Function GetOrResultType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        Return GetResultType(op1, op2, LogicalOperatorResultType)
    End Function

    Shared Function GetXorResultType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        Return GetResultType(op1, op2, LogicalOperatorResultType)
    End Function

    Shared Function GetUnaryNotResultType(ByVal op1 As TypeCode) As TypeCode
        Return GetResultType(op1, NotOperatorResultType)
    End Function

    Shared Function GetOrElseResultType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        Return GetResultType(op1, op2, ShortcircuitResultType)
    End Function

    Shared Function GetAndAlsoResultType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        Return GetResultType(op1, op2, ShortcircuitResultType)
    End Function

    Shared Function GetMultResultType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        Return GetResultType(op1, op2, MultResultType)
    End Function

    Shared Function GetBinaryAddResultType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        Return GetResultType(op1, op2, AddResultType)
    End Function

    Shared Function GetBinarySubResultType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        Return GetResultType(op1, op2, SubResultType)
    End Function

    Shared Function GetIsIsNotOperandType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        Return TypeCode.Object
    End Function

    Shared Function GetIsIsNotResultType(ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        Return TypeCode.Boolean
    End Function

    Shared Function GetUnaryResultType(ByVal op As KS, ByVal op1 As TypeCode) As TypeCode
        Select Case op
            Case KS.Add
                Return GetUnaryPlusResultType(op1)
            Case KS.Minus
                Return GetUnaryMinusResultType(op1)
            Case KS.Not
                Return GetUnaryNotResultType(op1)
            Case Else
                Helper.NotImplemented()
        End Select
    End Function

    Shared Function GetBinaryResultType(ByVal op As KS, ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        Select Case op
            Case KS.And
                Return GetAndResultType(op1, op2)
            Case KS.AndAlso
                Return GetAndAlsoResultType(op1, op2)
            Case KS.Or
                Return GetOrResultType(op1, op2)
            Case KS.OrElse
                Return GetOrElseResultType(op1, op2)
            Case KS.Xor
                Return GetXorResultType(op1, op2)
            Case KS.Add
                Return GetBinaryAddResultType(op1, op2)
            Case KS.Minus
                Return GetBinarySubResultType(op1, op2)
            Case KS.Mult
                Return GetMultResultType(op1, op2)
            Case KS.RealDivision
                Return GetRealDivResultType(op1, op2)
            Case KS.IntDivision
                Return GetIntDivResultType(op1, op2)
            Case KS.Power
                Return GetExpOperandType(op1, op2)
            Case KS.Concat
                Return GetConcatResultType(op1, op2)
            Case KS.GE
                Return GetGEResultType(op1, op2)
            Case KS.GT
                Return GetGTResultType(op1, op2)
            Case KS.LE
                Return GetLEResultType(op1, op2)
            Case KS.LT
                Return GetLTResultType(op1, op2)
            Case KS.Equals
                Return GetEqualsResultType(op1, op2)
            Case KS.NotEqual
                Return GetNotEqualsResultType(op1, op2)
            Case KS.ShiftLeft, KS.ShiftRight
                Return GetShiftResultType(op1, op2)
            Case KS.Mod
                Return GetModResultType(op1, op2)
            Case KS.Like
                Return GetLikeResultType(op1, op2)
            Case KS.Is, KS.IsNot
                Return GetIsIsNotResultType(op1, op2)
            Case Else
                Helper.NotImplemented()
        End Select
    End Function

    Shared Function GetUnaryOperandType(ByVal op As KS, ByVal operand As TypeCode) As TypeCode
        Select Case op
            Case KS.Add
                Return GetUnaryPlusResultType(operand)
            Case KS.Minus
                Return GetUnaryMinusResultType(operand)
            Case KS.Not
                Return GetUnaryNotResultType(operand)
            Case Else
                Throw New InternalException("")
        End Select
    End Function

    Shared Function GetBinaryOperandType(ByVal op As KS, ByVal op1 As TypeCode, ByVal op2 As TypeCode) As TypeCode
        Select Case op
            Case KS.And
                Return GetAndResultType(op1, op2)
            Case KS.AndAlso
                Return GetAndAlsoResultType(op1, op2)
            Case KS.Or
                Return GetOrResultType(op1, op2)
            Case KS.OrElse
                Return GetOrElseResultType(op1, op2)
            Case KS.Xor
                Return GetXorResultType(op1, op2)
            Case KS.Add
                Return GetBinaryAddResultType(op1, op2)
            Case KS.Minus
                Return GetBinarySubResultType(op1, op2)
            Case KS.Mult
                Return GetMultResultType(op1, op2)
            Case KS.RealDivision
                Return GetRealDivResultType(op1, op2)
            Case KS.IntDivision
                Return GetIntDivResultType(op1, op2)
            Case KS.Power
                Return GetExpOperandType(op1, op2)
            Case KS.Concat
                Return GetConcatOperandType(op1, op2)
            Case KS.GE
                Return GetGEOperandType(op1, op2)
            Case KS.GT
                Return GetGTOperandType(op1, op2)
            Case KS.LE
                Return GetLEOperandType(op1, op2)
            Case KS.LT
                Return GetLTOperandType(op1, op2)
            Case KS.Equals
                Return GetEqualsOperandType(op1, op2)
            Case KS.NotEqual
                Return GetNotEqualsOperandType(op1, op2)
            Case KS.ShiftLeft, KS.ShiftRight
                Return GetShiftResultType(op1, op2)
            Case KS.Mod
                Return GetModResultType(op1, op2)
            Case KS.Like
                Return GetLikeOperandType(op1, op2)
            Case KS.Is, KS.IsNot
                Return GetIsIsNotOperandType(op1, op2)
            Case Else
                Helper.NotImplemented()
        End Select
    End Function

    Private Shared Function GetResultType(ByVal op1 As TypeCode, ByVal array As String) As TypeCode
        Dim chr As Char
        chr = array.Chars(op1)
        If chr = "X"c Then
            Return Nothing
        Else
            Return CType(Microsoft.VisualBasic.Asc(chr) - Microsoft.VisualBasic.Asc("A"), TypeCode)
        End If
    End Function

    Private Shared Function GetResultType(ByVal op1 As TypeCode, ByVal op2 As TypeCode, ByVal array As String) As TypeCode
        Dim chr As Char
        chr = array.Chars(op1 + op2 * 19)
        If chr = "X"c Then
            Return Nothing
        Else
            Return CType(Microsoft.VisualBasic.Asc(chr) - Microsoft.VisualBasic.Asc("A"), TypeCode)
        End If
    End Function

    ''' <summary>
    ''' Converts the source to the destination type. Compiletime conversions are the only ones that succeeds.
    ''' Returns nothing if no conversion possible.
    ''' </summary>
    ''' <param name="Source"></param>
    ''' <param name="Destination"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function ConvertTo(ByVal Source As Object, ByVal Destination As Type) As Object
        Dim result As Object

        Helper.Assert(Source IsNot Nothing)
        Helper.Assert(Destination IsNot Nothing)

        Dim dtc As TypeCode = Helper.GetTypeCode(Destination)
        Dim stc As TypeCode = Helper.GetTypeCode(Source.GetType)

        'Console.WriteLine("ConvertTo: from " & stc.ToString() & " to " & dtc.ToString)

        If dtc = stc Then Return Source

        Select Case dtc
            Case TypeCode.Boolean
                result = ConvertToBoolean(Source, stc)
            Case TypeCode.Byte
                result = ConvertToByte(Source, stc)
            Case TypeCode.Char
                result = ConvertToChar(Source, stc)
            Case TypeCode.DateTime
                result = ConvertToDateTime(Source, stc)
            Case TypeCode.DBNull
                result = ConvertToDBNull(Source, stc)
            Case TypeCode.Decimal
                result = ConvertToDecimal(Source, stc)
            Case TypeCode.Double
                result = ConvertToDouble(Source, stc)
            Case TypeCode.Empty
                result = ConvertToEmpty(Source, stc)
            Case TypeCode.Int16
                result = ConvertToInt16(Source, stc)
            Case TypeCode.Int32
                result = ConvertToInt32(Source, stc)
            Case TypeCode.Int64
                result = ConvertToInt64(Source, stc)
            Case TypeCode.Object
                result = ConvertToObject(Source, stc)
            Case TypeCode.SByte
                result = ConvertToSByte(Source, stc)
            Case TypeCode.Single
                result = ConvertToSingle(Source, stc)
            Case TypeCode.String
                result = ConvertToString(Source, stc)
            Case TypeCode.UInt16
                result = ConvertToUInt16(Source, stc)
            Case TypeCode.UInt32
                result = ConvertToUInt32(Source, stc)
            Case TypeCode.UInt64
                result = ConvertToUInt64(Source, stc)
            Case Else
                Throw New InternalException("")
        End Select

        Return result
    End Function

    Public Shared Function ConvertToBoolean(ByVal Source As Object, ByVal SourceTypeCode As TypeCode) As Boolean
        Select Case SourceTypeCode
            Case TypeCode.Boolean
                Throw New NotImplementedException
            Case TypeCode.Byte
                Throw New NotImplementedException
            Case TypeCode.Char
                Throw New NotImplementedException
            Case TypeCode.DateTime
                Throw New NotImplementedException
            Case TypeCode.DBNull
                Throw New NotImplementedException
            Case TypeCode.Decimal
                Throw New NotImplementedException
            Case TypeCode.Double
                Throw New NotImplementedException
            Case TypeCode.Empty
                Throw New NotImplementedException
            Case TypeCode.Int16
                Throw New NotImplementedException
            Case TypeCode.Int32
                Throw New NotImplementedException
            Case TypeCode.Int64
                Throw New NotImplementedException
            Case TypeCode.Object
                Throw New NotImplementedException
            Case TypeCode.SByte
                Throw New NotImplementedException
            Case TypeCode.Single
                Throw New NotImplementedException
            Case TypeCode.String
                Throw New NotImplementedException
            Case TypeCode.UInt16
                Throw New NotImplementedException
            Case TypeCode.UInt32
                Throw New NotImplementedException
            Case TypeCode.UInt64
                Throw New NotImplementedException
            Case Else
                Throw New InternalException("")
        End Select
    End Function

    Public Shared Function ConvertToByte(ByVal Source As Object, ByVal SourceTypeCode As TypeCode) As Byte
        Select Case SourceTypeCode
            Case TypeCode.Boolean
                Throw New NotImplementedException
            Case TypeCode.Byte
                Throw New NotImplementedException
            Case TypeCode.Char
                Throw New NotImplementedException
            Case TypeCode.DateTime
                Throw New NotImplementedException
            Case TypeCode.DBNull
                Throw New NotImplementedException
            Case TypeCode.Decimal
                Throw New NotImplementedException
            Case TypeCode.Double
                Throw New NotImplementedException
            Case TypeCode.Empty
                Throw New NotImplementedException
            Case TypeCode.Int16
                Throw New NotImplementedException
            Case TypeCode.Int32
                Throw New NotImplementedException
            Case TypeCode.Int64
                Throw New NotImplementedException
            Case TypeCode.Object
                Throw New NotImplementedException
            Case TypeCode.SByte
                Throw New NotImplementedException
            Case TypeCode.Single
                Throw New NotImplementedException
            Case TypeCode.String
                Throw New NotImplementedException
            Case TypeCode.UInt16
                Throw New NotImplementedException
            Case TypeCode.UInt32
                Throw New NotImplementedException
            Case TypeCode.UInt64
                Throw New NotImplementedException
            Case Else
                Throw New InternalException("")
        End Select
    End Function

    Public Shared Function ConvertToChar(ByVal Source As Object, ByVal SourceTypeCode As TypeCode) As Char
        Select Case SourceTypeCode
            Case TypeCode.Boolean
                Throw New NotImplementedException
            Case TypeCode.Byte
                Throw New NotImplementedException
            Case TypeCode.Char
                Throw New NotImplementedException
            Case TypeCode.DateTime
                Throw New NotImplementedException
            Case TypeCode.DBNull
                Throw New NotImplementedException
            Case TypeCode.Decimal
                Throw New NotImplementedException
            Case TypeCode.Double
                Throw New NotImplementedException
            Case TypeCode.Empty
                Throw New NotImplementedException
            Case TypeCode.Int16
                Throw New NotImplementedException
            Case TypeCode.Int32
                Throw New NotImplementedException
            Case TypeCode.Int64
                Throw New NotImplementedException
            Case TypeCode.Object
                Throw New NotImplementedException
            Case TypeCode.SByte
                Throw New NotImplementedException
            Case TypeCode.Single
                Throw New NotImplementedException
            Case TypeCode.String
                Throw New NotImplementedException
            Case TypeCode.UInt16
                Throw New NotImplementedException
            Case TypeCode.UInt32
                Throw New NotImplementedException
            Case TypeCode.UInt64
                Throw New NotImplementedException
            Case Else
                Throw New InternalException("")
        End Select
    End Function

    Public Shared Function ConvertToDateTime(ByVal Source As Object, ByVal SourceTypeCode As TypeCode) As DateTime
        Select Case SourceTypeCode
            Case TypeCode.Boolean
                Throw New NotImplementedException
            Case TypeCode.Byte
                Throw New NotImplementedException
            Case TypeCode.Char
                Throw New NotImplementedException
            Case TypeCode.DateTime
                Throw New NotImplementedException
            Case TypeCode.DBNull
                Throw New NotImplementedException
            Case TypeCode.Decimal
                Throw New NotImplementedException
            Case TypeCode.Double
                Throw New NotImplementedException
            Case TypeCode.Empty
                Throw New NotImplementedException
            Case TypeCode.Int16
                Throw New NotImplementedException
            Case TypeCode.Int32
                Throw New NotImplementedException
            Case TypeCode.Int64
                Throw New NotImplementedException
            Case TypeCode.Object
                Throw New NotImplementedException
            Case TypeCode.SByte
                Throw New NotImplementedException
            Case TypeCode.Single
                Throw New NotImplementedException
            Case TypeCode.String
                Throw New NotImplementedException
            Case TypeCode.UInt16
                Throw New NotImplementedException
            Case TypeCode.UInt32
                Throw New NotImplementedException
            Case TypeCode.UInt64
                Throw New NotImplementedException
            Case Else
                Throw New InternalException("")
        End Select
    End Function

    Public Shared Function ConvertToDBNull(ByVal Source As Object, ByVal SourceTypeCode As TypeCode) As DBNull
        Select Case SourceTypeCode
            Case TypeCode.Boolean
                Throw New NotImplementedException
            Case TypeCode.Byte
                Throw New NotImplementedException
            Case TypeCode.Char
                Throw New NotImplementedException
            Case TypeCode.DateTime
                Throw New NotImplementedException
            Case TypeCode.DBNull
                Throw New NotImplementedException
            Case TypeCode.Decimal
                Throw New NotImplementedException
            Case TypeCode.Double
                Throw New NotImplementedException
            Case TypeCode.Empty
                Throw New NotImplementedException
            Case TypeCode.Int16
                Throw New NotImplementedException
            Case TypeCode.Int32
                Throw New NotImplementedException
            Case TypeCode.Int64
                Throw New NotImplementedException
            Case TypeCode.Object
                Throw New NotImplementedException
            Case TypeCode.SByte
                Throw New NotImplementedException
            Case TypeCode.Single
                Throw New NotImplementedException
            Case TypeCode.String
                Throw New NotImplementedException
            Case TypeCode.UInt16
                Throw New NotImplementedException
            Case TypeCode.UInt32
                Throw New NotImplementedException
            Case TypeCode.UInt64
                Throw New NotImplementedException
            Case Else
                Throw New InternalException("")
        End Select
    End Function

    Public Shared Function ConvertToDecimal(ByVal Source As Object, ByVal SourceTypeCode As TypeCode) As Object
        Dim result As Decimal
        Select Case SourceTypeCode
            Case TypeCode.Boolean
                Throw New NotImplementedException
            Case TypeCode.Char
                Throw New NotImplementedException
            Case TypeCode.DateTime
                Throw New NotImplementedException
            Case TypeCode.DBNull
                Throw New NotImplementedException
            Case TypeCode.Decimal
                Throw New NotImplementedException
            Case TypeCode.Double
                Throw New NotImplementedException
            Case TypeCode.Empty
                Throw New NotImplementedException
            Case TypeCode.SByte, TypeCode.Int16, TypeCode.Int32, TypeCode.Int64
                result = CLng(Source)
            Case TypeCode.Object
                Throw New NotImplementedException
            Case TypeCode.Single
                Throw New NotImplementedException
            Case TypeCode.String
                Throw New NotImplementedException
            Case TypeCode.Byte, TypeCode.UInt16, TypeCode.UInt32, TypeCode.UInt64
                result = CULng(Source)
            Case Else
                Throw New InternalException("")
        End Select
        Return result
    End Function

    Public Shared Function ConvertToDouble(ByVal Source As Object, ByVal SourceTypeCode As TypeCode) As Double
        Select Case SourceTypeCode
            Case TypeCode.Boolean
                Throw New NotImplementedException
            Case TypeCode.Byte
                Throw New NotImplementedException
            Case TypeCode.Char
                Throw New NotImplementedException
            Case TypeCode.DateTime
                Throw New NotImplementedException
            Case TypeCode.DBNull
                Throw New NotImplementedException
            Case TypeCode.Decimal
                Throw New NotImplementedException
            Case TypeCode.Double
                Throw New NotImplementedException
            Case TypeCode.Empty
                Throw New NotImplementedException
            Case TypeCode.Int16
                Throw New NotImplementedException
            Case TypeCode.Int32
                Return CDbl(Source)
            Case TypeCode.Int64
                Throw New NotImplementedException
            Case TypeCode.Object
                Throw New NotImplementedException
            Case TypeCode.SByte
                Throw New NotImplementedException
            Case TypeCode.Single
                Throw New NotImplementedException
            Case TypeCode.String
                Throw New NotImplementedException
            Case TypeCode.UInt16
                Throw New NotImplementedException
            Case TypeCode.UInt32
                Throw New NotImplementedException
            Case TypeCode.UInt64
                Throw New NotImplementedException
            Case Else
                Throw New InternalException("")
        End Select
    End Function

    Public Shared Function ConvertToEmpty(ByVal Source As Object, ByVal SourceTypeCode As TypeCode) As Object
        Select Case SourceTypeCode
            Case TypeCode.Boolean
                Throw New NotImplementedException
            Case TypeCode.Byte
                Throw New NotImplementedException
            Case TypeCode.Char
                Throw New NotImplementedException
            Case TypeCode.DateTime
                Throw New NotImplementedException
            Case TypeCode.DBNull
                Throw New NotImplementedException
            Case TypeCode.Decimal
                Throw New NotImplementedException
            Case TypeCode.Double
                Throw New NotImplementedException
            Case TypeCode.Empty
                Throw New NotImplementedException
            Case TypeCode.Int16
                Throw New NotImplementedException
            Case TypeCode.Int32
                Throw New NotImplementedException
            Case TypeCode.Int64
                Throw New NotImplementedException
            Case TypeCode.Object
                Throw New NotImplementedException
            Case TypeCode.SByte
                Throw New NotImplementedException
            Case TypeCode.Single
                Throw New NotImplementedException
            Case TypeCode.String
                Throw New NotImplementedException
            Case TypeCode.UInt16
                Throw New NotImplementedException
            Case TypeCode.UInt32
                Throw New NotImplementedException
            Case TypeCode.UInt64
                Throw New NotImplementedException
            Case Else
                Throw New InternalException("")
        End Select
    End Function

    Public Shared Function ConvertToInt16(ByVal Source As Object, ByVal SourceTypeCode As TypeCode) As Int16
        Select Case SourceTypeCode
            Case TypeCode.Boolean
                Throw New NotImplementedException
            Case TypeCode.Byte
                Throw New NotImplementedException
            Case TypeCode.Char
                Throw New NotImplementedException
            Case TypeCode.DateTime
                Throw New NotImplementedException
            Case TypeCode.DBNull
                Throw New NotImplementedException
            Case TypeCode.Decimal
                Throw New NotImplementedException
            Case TypeCode.Double
                Throw New NotImplementedException
            Case TypeCode.Empty
                Throw New NotImplementedException
            Case TypeCode.Int16
                Throw New NotImplementedException
            Case TypeCode.Int32
                Throw New NotImplementedException
            Case TypeCode.Int64
                Throw New NotImplementedException
            Case TypeCode.Object
                Throw New NotImplementedException
            Case TypeCode.SByte
                Throw New NotImplementedException
            Case TypeCode.Single
                Throw New NotImplementedException
            Case TypeCode.String
                Throw New NotImplementedException
            Case TypeCode.UInt16
                Throw New NotImplementedException
            Case TypeCode.UInt32
                Throw New NotImplementedException
            Case TypeCode.UInt64
                Throw New NotImplementedException
            Case Else
                Throw New InternalException("")
        End Select
    End Function

    Public Shared Function ConvertToInt32(ByVal Source As Object, ByVal SourceTypeCode As TypeCode) As Int32
        Select Case SourceTypeCode
            Case TypeCode.Boolean
                Throw New NotImplementedException
            Case TypeCode.Byte
                Throw New NotImplementedException
            Case TypeCode.Char
                Throw New NotImplementedException
            Case TypeCode.DateTime
                Throw New NotImplementedException
            Case TypeCode.DBNull
                Throw New NotImplementedException
            Case TypeCode.Decimal
                Throw New NotImplementedException
            Case TypeCode.Double
                Throw New NotImplementedException
            Case TypeCode.Empty
                Throw New NotImplementedException
            Case TypeCode.Int16
                Throw New NotImplementedException
            Case TypeCode.Int32
                Throw New NotImplementedException
            Case TypeCode.Int64
                Throw New NotImplementedException
            Case TypeCode.Object
                Throw New NotImplementedException
            Case TypeCode.SByte
                Throw New NotImplementedException
            Case TypeCode.Single
                Throw New NotImplementedException
            Case TypeCode.String
                Throw New NotImplementedException
            Case TypeCode.UInt16
                Throw New NotImplementedException
            Case TypeCode.UInt32
                Throw New NotImplementedException
            Case TypeCode.UInt64
                Throw New NotImplementedException
            Case Else
                Throw New InternalException("")
        End Select
    End Function

    Public Shared Function ConvertToInt64(ByVal Source As Object, ByVal SourceTypeCode As TypeCode) As Int64
        Select Case SourceTypeCode
            Case TypeCode.Boolean
                Throw New NotImplementedException
            Case TypeCode.Byte
                Return CLng(Source)
            Case TypeCode.Char
                Throw New NotImplementedException
            Case TypeCode.DateTime
                Throw New NotImplementedException
            Case TypeCode.DBNull
                Throw New NotImplementedException
            Case TypeCode.Decimal
                Throw New NotImplementedException
            Case TypeCode.Double
                Throw New NotImplementedException
            Case TypeCode.Empty
                Throw New NotImplementedException
            Case TypeCode.Int16
                Return CLng(Source)
            Case TypeCode.Int32
                Return CLng(Source)
            Case TypeCode.Int64
                Return CLng(Source)
            Case TypeCode.Object
                Throw New NotImplementedException
            Case TypeCode.SByte
                Return CLng(Source)
            Case TypeCode.Single
                Throw New NotImplementedException
            Case TypeCode.String
                Throw New NotImplementedException
            Case TypeCode.UInt16
                Return CLng(Source)
            Case TypeCode.UInt32
                Return CLng(Source)
            Case TypeCode.UInt64
                Throw New NotImplementedException
            Case Else
                Throw New InternalException("")
        End Select
    End Function

    Public Shared Function ConvertToObject(ByVal Source As Object, ByVal SourceTypeCode As TypeCode) As Object
        Select Case SourceTypeCode
            Case TypeCode.Boolean
                Throw New NotImplementedException
            Case TypeCode.Byte
                Throw New NotImplementedException
            Case TypeCode.Char
                Throw New NotImplementedException
            Case TypeCode.DateTime
                Throw New NotImplementedException
            Case TypeCode.DBNull
                Return CObj(Source) ' Nothing 'Throw New NotImplementedException
            Case TypeCode.Decimal
                Throw New NotImplementedException
            Case TypeCode.Double
                Throw New NotImplementedException
            Case TypeCode.Empty
                Throw New NotImplementedException
            Case TypeCode.Int16
                Throw New NotImplementedException
            Case TypeCode.Int32
                Return CObj(Source) '                Throw New NotImplementedException
            Case TypeCode.Int64
                Throw New NotImplementedException
            Case TypeCode.Object
                Throw New NotImplementedException
            Case TypeCode.SByte
                Throw New NotImplementedException
            Case TypeCode.Single
                Throw New NotImplementedException
            Case TypeCode.String
                Throw New NotImplementedException
            Case TypeCode.UInt16
                Throw New NotImplementedException
            Case TypeCode.UInt32
                Throw New NotImplementedException
            Case TypeCode.UInt64
                Throw New NotImplementedException
            Case Else
                Throw New InternalException("")
        End Select
    End Function

    Public Shared Function ConvertToSByte(ByVal Source As Object, ByVal SourceTypeCode As TypeCode) As SByte
        Select Case SourceTypeCode
            Case TypeCode.Boolean
                Throw New NotImplementedException
            Case TypeCode.Byte
                Throw New NotImplementedException
            Case TypeCode.Char
                Throw New NotImplementedException
            Case TypeCode.DateTime
                Throw New NotImplementedException
            Case TypeCode.DBNull
                Throw New NotImplementedException
            Case TypeCode.Decimal
                Throw New NotImplementedException
            Case TypeCode.Double
                Throw New NotImplementedException
            Case TypeCode.Empty
                Throw New NotImplementedException
            Case TypeCode.Int16
                Throw New NotImplementedException
            Case TypeCode.Int32
                Throw New NotImplementedException
            Case TypeCode.Int64
                Throw New NotImplementedException
            Case TypeCode.Object
                Throw New NotImplementedException
            Case TypeCode.SByte
                Throw New NotImplementedException
            Case TypeCode.Single
                Throw New NotImplementedException
            Case TypeCode.String
                Throw New NotImplementedException
            Case TypeCode.UInt16
                Throw New NotImplementedException
            Case TypeCode.UInt32
                Throw New NotImplementedException
            Case TypeCode.UInt64
                Throw New NotImplementedException
            Case Else
                Throw New InternalException("")
        End Select
    End Function

    Public Shared Function ConvertToSingle(ByVal Source As Object, ByVal SourceTypeCode As TypeCode) As Single
        Select Case SourceTypeCode
            Case TypeCode.Boolean
                Throw New NotImplementedException
            Case TypeCode.Byte
                Throw New NotImplementedException
            Case TypeCode.Char
                Throw New NotImplementedException
            Case TypeCode.DateTime
                Throw New NotImplementedException
            Case TypeCode.DBNull
                Throw New NotImplementedException
            Case TypeCode.Decimal
                Throw New NotImplementedException
            Case TypeCode.Double
                Throw New NotImplementedException
            Case TypeCode.Empty
                Throw New NotImplementedException
            Case TypeCode.Int16
                Throw New NotImplementedException
            Case TypeCode.Int32
                Throw New NotImplementedException
            Case TypeCode.Int64
                Throw New NotImplementedException
            Case TypeCode.Object
                Throw New NotImplementedException
            Case TypeCode.SByte
                Throw New NotImplementedException
            Case TypeCode.Single
                Throw New NotImplementedException
            Case TypeCode.String
                Throw New NotImplementedException
            Case TypeCode.UInt16
                Throw New NotImplementedException
            Case TypeCode.UInt32
                Throw New NotImplementedException
            Case TypeCode.UInt64
                Throw New NotImplementedException
            Case Else
                Throw New InternalException("")
        End Select
    End Function

    Public Shared Function ConvertToString(ByVal Source As Object, ByVal SourceTypeCode As TypeCode) As String
        Select Case SourceTypeCode
            Case TypeCode.Boolean
                Throw New NotImplementedException
            Case TypeCode.Byte
                Throw New NotImplementedException
            Case TypeCode.Char
                Return CStr(Source)
            Case TypeCode.DateTime
                Throw New NotImplementedException
            Case TypeCode.DBNull
                Return Nothing 'Throw New NotImplementedException
            Case TypeCode.Decimal
                Throw New NotImplementedException
            Case TypeCode.Double
                Throw New NotImplementedException
            Case TypeCode.Empty
                Throw New NotImplementedException
            Case TypeCode.Int16
                Throw New NotImplementedException
            Case TypeCode.Int32
                Throw New NotImplementedException
            Case TypeCode.Int64
                Throw New NotImplementedException
            Case TypeCode.Object
                Throw New NotImplementedException
            Case TypeCode.SByte
                Throw New NotImplementedException
            Case TypeCode.Single
                Throw New NotImplementedException
            Case TypeCode.String
                Throw New NotImplementedException
            Case TypeCode.UInt16
                Throw New NotImplementedException
            Case TypeCode.UInt32
                Throw New NotImplementedException
            Case TypeCode.UInt64
                Throw New NotImplementedException
            Case Else
                Throw New InternalException("")
        End Select
    End Function

    Public Shared Function ConvertToUInt16(ByVal Source As Object, ByVal SourceTypeCode As TypeCode) As UInt16
        Select Case SourceTypeCode
            Case TypeCode.Boolean
                Throw New NotImplementedException
            Case TypeCode.Byte
                Throw New NotImplementedException
            Case TypeCode.Char
                Throw New NotImplementedException
            Case TypeCode.DateTime
                Throw New NotImplementedException
            Case TypeCode.DBNull
                Throw New NotImplementedException
            Case TypeCode.Decimal
                Throw New NotImplementedException
            Case TypeCode.Double
                Throw New NotImplementedException
            Case TypeCode.Empty
                Throw New NotImplementedException
            Case TypeCode.Int16
                Throw New NotImplementedException
            Case TypeCode.Int32
                Throw New NotImplementedException
            Case TypeCode.Int64
                Throw New NotImplementedException
            Case TypeCode.Object
                Throw New NotImplementedException
            Case TypeCode.SByte
                Throw New NotImplementedException
            Case TypeCode.Single
                Throw New NotImplementedException
            Case TypeCode.String
                Throw New NotImplementedException
            Case TypeCode.UInt16
                Throw New NotImplementedException
            Case TypeCode.UInt32
                Throw New NotImplementedException
            Case TypeCode.UInt64
                Throw New NotImplementedException
            Case Else
                Throw New InternalException("")
        End Select
    End Function

    Public Shared Function ConvertToUInt32(ByVal Source As Object, ByVal SourceTypeCode As TypeCode) As UInt32
        Select Case SourceTypeCode
            Case TypeCode.Boolean
                Throw New NotImplementedException
            Case TypeCode.Byte
                Throw New NotImplementedException
            Case TypeCode.Char
                Throw New NotImplementedException
            Case TypeCode.DateTime
                Throw New NotImplementedException
            Case TypeCode.DBNull
                Throw New NotImplementedException
            Case TypeCode.Decimal
                Throw New NotImplementedException
            Case TypeCode.Double
                Throw New NotImplementedException
            Case TypeCode.Empty
                Throw New NotImplementedException
            Case TypeCode.Int16
                Throw New NotImplementedException
            Case TypeCode.Int32
                Throw New NotImplementedException
            Case TypeCode.Int64
                Throw New NotImplementedException
            Case TypeCode.Object
                Throw New NotImplementedException
            Case TypeCode.SByte
                Throw New NotImplementedException
            Case TypeCode.Single
                Throw New NotImplementedException
            Case TypeCode.String
                Throw New NotImplementedException
            Case TypeCode.UInt16
                Throw New NotImplementedException
            Case TypeCode.UInt32
                Throw New NotImplementedException
            Case TypeCode.UInt64
                Throw New NotImplementedException
            Case Else
                Throw New InternalException("")
        End Select
    End Function

    Public Shared Function ConvertToUInt64(ByVal Source As Object, ByVal SourceTypeCode As TypeCode) As Object
        Dim result As ULong
        Select Case SourceTypeCode
            Case TypeCode.Boolean
                Throw New NotImplementedException
            Case TypeCode.Byte
                result = CByte(Source)
            Case TypeCode.Char
                Throw New NotImplementedException
            Case TypeCode.DateTime
                Throw New NotImplementedException
            Case TypeCode.DBNull
                Throw New NotImplementedException
            Case TypeCode.Decimal
                Throw New NotImplementedException
            Case TypeCode.Double
                Return Nothing
            Case TypeCode.Empty
                Throw New NotImplementedException
            Case TypeCode.SByte, TypeCode.Int16, TypeCode.Int32, TypeCode.Int64
                Dim tmp As Long = CLng(Source)
                If tmp >= 0 Then
                    result = CULng(tmp)
                Else
                    Return Nothing
                End If
            Case TypeCode.Object
                Throw New NotImplementedException
            Case TypeCode.Single
                Return Nothing
            Case TypeCode.String
                Throw New NotImplementedException
            Case TypeCode.UInt16, TypeCode.UInt32, TypeCode.UInt64
                Return CULng(Source)
            Case Else
                Throw New InternalException("")
        End Select
        Return result
    End Function

End Class