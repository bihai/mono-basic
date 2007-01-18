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

#If DEBUG Then
#Const DEBUGMETHODRESOLUTION = 1
#Const DEBUGMETHODADD = 1
#Const EXTENDEDDEBUG = 0
#End If

''' <summary>
''' A module of useful global functions.
''' </summary>
''' <remarks></remarks>
Public Class Helper
    Private m_Compiler As Compiler
    Private Shared m_SharedCompilers As New Generic.List(Of Compiler)

    Public Shared LOGMETHODRESOLUTION As Boolean = False

    Public Const ALLMEMBERS As BindingFlags = BindingFlags.FlattenHierarchy Or BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance Or BindingFlags.Static

    Public Const ALLNOBASEMEMBERS As BindingFlags = BindingFlags.DeclaredOnly Or BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance Or BindingFlags.Static

#If DEBUG Then
    Shared Sub RunRT()
        Dim path As String = "rt\bin\rt.exe"
        Dim parent As String = VB.CurDir
        Do Until parent.Length <= 3
            Dim filename As String = IO.Path.Combine(parent, path)
            If IO.File.Exists(filename) Then
                Diagnostics.Process.Start(filename)
                Return
            End If

            parent = IO.Path.GetDirectoryName(parent)
        Loop
        System.Windows.Forms.MessageBox.Show("rt.exe not found.")
    End Sub
#End If

    Shared Function IsOnMS() As Boolean
        Return Not IsOnMono()
    End Function

    Shared Function IsOnMono() As Boolean
        Dim t As Type = GetType(Integer)

        If t.GetType().ToString = "System.MonoType" Then
            Return True
        Else
            Return False
        End If
    End Function

    Shared Function VerifyValueClassification(ByRef Expression As Expression, ByVal Info As ResolveInfo) As Boolean
        Dim result As Boolean = True
        If Expression.Classification.IsValueClassification Then
            result = True
        ElseIf Expression.Classification.CanBeValueClassification Then
            Expression = Expression.ReclassifyToValueExpression
            result = Expression.ResolveExpression(Info) AndAlso result
        Else
            Helper.AddError()
            result = False
        End If
        Return result
    End Function

    Shared Function IsReflectionType(ByVal Type As Type) As Boolean
        Dim typesTypename As String = Type.GetType.Name
        Dim result As Boolean

        result = typesTypename = "TypeBuilder" OrElse typesTypename = "TypeBuilderInstantiation" OrElse typesTypename = "SymbolType"

#If DEBUG Then
        Helper.Assert(result = (Type.GetType.Namespace = "System.Reflection.Emit"), Type.GetType.FullName)
#End If

        Return result
    End Function

    Shared Function IsReflectionMember(ByVal Member As MemberInfo) As Boolean
        Dim result As Boolean
        If TypeOf Member Is MethodDescriptor Then Return False
        If TypeOf Member Is FieldDescriptor Then Return False
        If TypeOf Member Is ConstructorDescriptor Then Return False
        If TypeOf Member Is EventDescriptor Then Return False
        If TypeOf Member Is TypeDescriptor Then Return False
        If TypeOf Member Is PropertyDescriptor Then Return False

        If Member.DeclaringType IsNot Nothing Then
            result = IsReflectionType(Member.DeclaringType)
        ElseIf Member.MemberType = MemberTypes.TypeInfo OrElse Member.MemberType = MemberTypes.NestedType Then
            result = IsReflectionType(DirectCast(Member, Type))
        Else
            Helper.NotImplemented()
        End If
        Return result
    End Function

    Shared Function IsReflectionMember(ByVal Members() As MemberInfo) As Boolean
        If Members Is Nothing Then Return True
        If Members.Length = 0 Then Return True

        For Each m As MemberInfo In Members
            If IsReflectionMember(m) = False Then Return False
        Next
        Return True
    End Function

    Shared Function IsEmittableMember(ByVal Member As MemberInfo) As Boolean
        Dim result As Boolean

        If Member Is Nothing Then Return True
        result = Member.GetType.Namespace.StartsWith("System")

        Return result
    End Function

    Shared Function IsEmittableMember(ByVal Members() As MemberInfo) As Boolean
        If Members Is Nothing Then Return True
        If Members.Length = 0 Then Return True

        For Each m As MemberInfo In Members
            If IsEmittableMember(m) = False Then Return False
        Next
        Return True
    End Function

    Shared Function GetBaseMembers(ByVal Compiler As Compiler, ByVal Type As Type) As MemberInfo() ' Generic.List(Of MemberInfo)
        Dim result As New Generic.List(Of MemberInfo)
#If EXTENDEDDEBUG Then
        Compiler.Report.WriteLine("Getting base members for type " & Type.FullName)
#End If
        If Type.IsInterface Then
            Dim ifaces() As Type
            ifaces = Type.GetInterfaces()
            For Each iface As Type In ifaces
                result.AddRange(iface.GetMembers(Helper.ALLMEMBERS))
            Next
            'Remove duplicates (might happen since interfaces can have multiple bases)
            Dim tmp As New Generic.List(Of MemberInfo)
            For Each item As MemberInfo In result
                If tmp.Contains(item) = False Then tmp.Add(item)
            Next
            result = tmp
        ElseIf Type.BaseType IsNot Nothing Then
            result.AddRange(Type.BaseType.GetMembers(Helper.ALLMEMBERS))
        End If

        Return result.ToArray
    End Function

    ''' <summary>
    ''' Returns an array of the types of the method info. DEBUG METHOD!!
    ''' </summary>
    ''' <param name="method"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    <Diagnostics.Conditional("DEBUG")> Shared Function GetParameterTypes(ByVal Compiler As Compiler, ByVal method As MethodInfo) As Type()
        Dim result As Type()
        Dim builder As MethodBuilder = TryCast(method, MethodBuilder)
        If builder IsNot Nothing Then
            Dim reflectableInfo As MethodInfo = TryCast(Compiler.TypeManager.GetRegisteredMember(builder), MethodInfo)

            If reflectableInfo IsNot Nothing Then
                result = GetParameterTypes(reflectableInfo.GetParameters)
            Else
                Helper.Assert(False)
                result = CType(GetType(MethodBuilder).GetField("m_parameterTypes", BindingFlags.Instance Or BindingFlags.NonPublic Or BindingFlags.Public).GetValue(builder), Type())
            End If
        Else
            result = GetParameterTypes(method.GetParameters())
        End If
        Return result
    End Function

    ''' <summary>
    ''' Returns an array of the types of the method info. DEBUG METHOD!!
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    <Diagnostics.Conditional("DEBUG")> Shared Function GetParameterTypes(ByVal Compiler As Compiler, ByVal ctor As ConstructorInfo) As Type()
        Dim result As Type()
        Dim builder As ConstructorBuilder = TryCast(ctor, ConstructorBuilder)
        If builder IsNot Nothing Then
            Dim tmp As ConstructorInfo = TryCast(Compiler.TypeManager.GetRegisteredMember(builder), ConstructorInfo)
            Dim method As MethodBuilder
            If tmp IsNot Nothing Then
                result = GetParameterTypes(tmp.GetParameters)
            Else
                Helper.Assert(False)
                method = CType(GetType(ConstructorBuilder).GetField("m_methodBuilder", BindingFlags.Instance Or BindingFlags.NonPublic Or BindingFlags.Public).GetValue(builder), MethodBuilder)
                result = GetParameterTypes(Compiler, method)
            End If
        Else
            result = GetParameterTypes(ctor.GetParameters())
        End If
        Return result
    End Function

    Shared Function GetGenericParameterConstraints(ByVal Type As Type) As Type()
        If Type.IsGenericParameter = False Then Throw New InternalException("")
        If TypeOf Type Is GenericTypeParameterBuilder Then
            Helper.NotImplemented() : Throw New InternalException("")
        Else
            Return Type.GetGenericParameterConstraints
        End If
    End Function

    Shared Function IsAssembly(ByVal member As MemberInfo) As Boolean
        Dim mi As MethodInfo = TryCast(member, MethodInfo)
        If mi IsNot Nothing Then
            Return mi.IsAssembly
        Else
            Dim ci As ConstructorInfo = TryCast(member, ConstructorInfo)
            If ci IsNot Nothing Then
                Return ci.IsAssembly
            Else
                Dim fi As FieldInfo = TryCast(member, FieldInfo)
                If fi IsNot Nothing Then
                    Return fi.IsAssembly
                Else
                    Helper.NotImplemented()
                End If
            End If
        End If
    End Function

    Shared Function GetNames(ByVal List As IEnumerable) As String()
        Dim result As New Generic.List(Of String)
        For Each item As INameable In List
            result.Add(item.Name)
        Next
        Return result.ToArray
    End Function

    Shared Function GetTypeCode(ByVal Type As TypeDescriptor) As TypeCode
        If Helper.IsEnum(Type) Then
            Return GetTypeCode(Type.GetElementType)
        Else
            Return TypeCode.Object
        End If
    End Function

    ''' <summary>
    ''' Compares two vb-names (case-insensitive)
    ''' </summary>
    ''' <param name="Value1"></param>
    ''' <param name="Value2"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function NameCompare(ByVal Value1 As String, ByVal Value2 As String) As Boolean
        Helper.Assert(Value1 IsNot Nothing)
        Helper.Assert(Value2 IsNot Nothing)
        Return String.Compare(Value1, Value2, StringComparison.OrdinalIgnoreCase) = 0
    End Function

    ''' <summary>
    ''' Compares two names.
    ''' </summary>
    ''' <param name="Value1"></param>
    ''' <param name="Value2"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function NameCompare(ByVal Value1 As String, ByVal Value2 As String, ByVal Ordinal As Boolean) As Boolean
        If Ordinal Then
            Return NameCompareOrdinal(Value1, Value2)
        Else
            Return NameCompare(Value1, Value2)
        End If
    End Function

    ''' <summary>
    ''' Compares two names.
    ''' </summary>
    ''' <param name="Value1"></param>
    ''' <param name="Value2"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function NameCompareOrdinal(ByVal Value1 As String, ByVal Value2 As String) As Boolean
        Helper.Assert(Value1 IsNot Nothing)
        Helper.Assert(Value2 IsNot Nothing)
        Return String.CompareOrdinal(Value1, Value2) = 0
    End Function

    ''' <summary>
    ''' Compares two vb-names (case-insensitive)
    ''' </summary>
    ''' <param name="Value1"></param>
    ''' <param name="Value2"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function CompareName(ByVal Value1 As String, ByVal Value2 As String) As Boolean
        Return NameCompare(Value1, Value2)
    End Function

    ''' <summary>
    ''' Compares two strings.
    ''' </summary>
    ''' <param name="Value1"></param>
    ''' <param name="Value2"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function CompareName(ByVal Value1 As String, ByVal Value2 As String, ByVal Ordinal As Boolean) As Boolean
        Return NameCompare(Value1, Value2, Ordinal)
    End Function

    ''' <summary>
    ''' Compares two strings.
    ''' </summary>
    ''' <param name="Value1"></param>
    ''' <param name="Value2"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function CompareNameOrdinal(ByVal Value1 As String, ByVal Value2 As String) As Boolean
        Return NameCompare(Value1, Value2)
    End Function

    Shared Function GetTypeCode(ByVal Type As Type) As TypeCode
        Dim tD As TypeDescriptor = TryCast(Type, TypeDescriptor)
        If tD Is Nothing Then
            Return System.Type.GetTypeCode(Type)
        Else
            Return GetTypeCode(tD)
        End If
    End Function

    Shared Function IsTypeDeclaration(ByVal first As Object) As Boolean
        Return TypeOf first Is TypeDescriptor OrElse TypeOf first Is IType OrElse TypeOf first Is Type
    End Function

    Shared Function IsFieldDeclaration(ByVal first As Object) As Boolean
        Return TypeOf first Is VariableDeclaration OrElse TypeOf first Is FieldInfo
    End Function

    Shared Function IsInterface(ByVal Compiler As Compiler, ByVal Type As Type) As Boolean
        Dim tmpTP As Type

        If Type.GetType.Name = "SymbolType" Then Return Type.IsInterface

        tmpTP = Compiler.TypeManager.GetRegisteredType(Type)

        If TypeOf Type Is GenericTypeParameterBuilder Then
            Return False
        ElseIf TypeOf tmpTP Is TypeParameterDescriptor Then
            Return False
        ElseIf tmpTP.IsByRef Then
            Return False
        ElseIf TypeOf Type Is Type Then
            Return Compiler.TypeManager.GetRegisteredType(Type).IsInterface
        Else
            Helper.NotImplemented()
        End If
    End Function

    Shared Function IsEnum(ByVal Type As Type) As Boolean
        If TypeOf Type Is TypeBuilder Then
            Return Type.IsEnum
        End If
        Dim FullName As String = Type.GetType.FullName
        If FullName = "System.Type" Then
            Return Type.IsEnum
        ElseIf FullName.Contains("TypeBuilderInstantiation") Then
            Return False
        ElseIf FullName.Contains("RuntimeType") Then
            Return Type.IsEnum
        ElseIf FullName.Contains("SymbolType") Then
            Return False
        ElseIf Type.GetType.Namespace = "System.Reflection.Emit" Then
            Return False
        ElseIf TypeOf Type Is TypeParameterDescriptor Then
            Return False
        Else
            Return Type.IsEnum
            'Helper.NotImplementedYet("IsEnum of type '" & Type.GetType.FullName & "'")
        End If
    End Function

    Shared Function IsEnumFieldDeclaration(ByVal first As Object) As Boolean
        If TypeOf first Is EnumMemberDeclaration Then Return True
        Dim fld As FieldInfo = TryCast(first, FieldInfo)
        Return fld IsNot Nothing AndAlso fld.DeclaringType.IsEnum
    End Function

    Shared Function IsEventDeclaration(ByVal first As Object) As Boolean
        Return TypeOf first Is EventInfo
    End Function

    Shared Function IsPropertyDeclaration(ByVal first As Object) As Boolean
        Return TypeOf first Is RegularPropertyDeclaration OrElse TypeOf first Is PropertyInfo OrElse TypeOf first Is PropertyDeclaration
    End Function

    Shared Function IsMethodDeclaration(ByVal first As Object) As Boolean
        Return TypeOf first Is SubDeclaration OrElse TypeOf first Is FunctionDeclaration OrElse TypeOf first Is IMethod OrElse TypeOf first Is MethodInfo
    End Function

    ''' <summary>
    ''' Returns all the members in the types with the specified name.
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function GetMembersOfTypes(ByVal Compiler As Compiler, ByVal Types As TypeDictionary, ByVal Name As String) As Generic.List(Of MemberInfo)
        Dim result As New Generic.List(Of MemberInfo)
        For Each type As Type In Types.TypesAsArray
            'result.AddRange(Helper.FilterByName(type.GetMembers, Name))
            result.AddRange(Compiler.TypeManager.GetCache(type).LookupMembersFlattened(Name))
        Next
        Return result
    End Function

    ''' <summary>
    ''' Returns all the members in the types with the specified name.
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function GetMembersOfTypes(ByVal Compiler As Compiler, ByVal Types As TypeList, ByVal Name As String) As Generic.List(Of MemberInfo)
        Dim result As New Generic.List(Of MemberInfo)
        For Each type As Type In Types
            'result.AddRange(Helper.FilterByName(type.GetMembers, Name))
            result.AddRange(Compiler.TypeManager.GetCache(type).LookupMembersFlattened(Name))
        Next
        Return result
    End Function

    Shared Function GetInstanceConstructors(ByVal type As Type) As Generic.List(Of MemberInfo)
        Dim result As New Generic.List(Of MemberInfo)

        result.AddRange(type.GetConstructors(BindingFlags.Instance Or BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.DeclaredOnly))

        Return result
    End Function

    ''' <summary>
    ''' Removes private members if they are from an external assembly.
    ''' </summary>
    ''' <param name="Members"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function FilterExternalInaccessible(ByVal Compiler As Compiler, ByVal Members As Generic.List(Of MemberInfo)) As Generic.List(Of MemberInfo)
        Dim result As New Generic.List(Of MemberInfo)

        For Each member As MemberInfo In Members
            If (IsPrivate(member) OrElse IsFriend(member)) AndAlso Compiler.Assembly.IsDefinedHere(member.DeclaringType) = False Then
                Continue For
            End If
            result.Add(member)
        Next

        Return result
    End Function

    Shared Function IsProtectedOrAssem(ByVal Member As MemberInfo) As Boolean
        Helper.Assert(Member IsNot Nothing)
        Select Case Member.MemberType
            Case MemberTypes.Constructor
                Dim ctor As ConstructorInfo = DirectCast(Member, ConstructorInfo)
                Return ctor.IsFamilyOrAssembly
            Case MemberTypes.Event
                Dim eventM As EventInfo = DirectCast(Member, EventInfo)
                Return CBool(Helper.GetEventAccess(eventM) = MethodAttributes.FamORAssem)
            Case MemberTypes.Field
                Dim field As FieldInfo = DirectCast(Member, FieldInfo)
                Return field.IsFamilyOrAssembly
            Case MemberTypes.NestedType
                Dim tp As Type = DirectCast(Member, Type)
                Return tp.IsNestedFamORAssem
            Case MemberTypes.Method
                Dim method As MethodInfo = DirectCast(Member, MethodInfo)
                Return method.IsFamilyOrAssembly OrElse method.IsFamily
            Case MemberTypes.Property
                Dim propM As PropertyInfo = DirectCast(Member, PropertyInfo)
                Return Helper.GetPropertyAccess(propM) = MethodAttributes.FamORAssem
            Case MemberTypes.TypeInfo
                Dim tp As Type = DirectCast(Member, Type)
                Return tp.IsNotPublic OrElse tp.IsNested AndAlso tp.IsNestedFamORAssem
            Case Else
                Throw New InternalException("")
        End Select
    End Function

    Shared Function IsFriend(ByVal Member As MemberInfo) As Boolean
        Helper.Assert(Member IsNot Nothing)
        Select Case Member.MemberType
            Case MemberTypes.Constructor
                Return DirectCast(Member, ConstructorInfo).IsAssembly
            Case MemberTypes.Event
                Dim eventM As EventInfo = DirectCast(Member, EventInfo)
                Return CBool(Helper.GetEventAccess(eventM) = MethodAttributes.Assembly)
            Case MemberTypes.Field
                Return DirectCast(Member, FieldInfo).IsAssembly
            Case MemberTypes.NestedType
                Return DirectCast(Member, Type).IsNestedAssembly
            Case MemberTypes.Method
                Return DirectCast(Member, MethodInfo).IsAssembly
            Case MemberTypes.Property
                Dim propM As PropertyInfo = DirectCast(Member, PropertyInfo)
                Return Helper.GetPropertyAccess(propM) = MethodAttributes.Assembly
            Case MemberTypes.TypeInfo
                Dim tp As Type = DirectCast(Member, Type)
                Return tp.IsNotPublic OrElse tp.IsNested AndAlso tp.IsNestedAssembly
            Case Else
                Throw New InternalException("")
        End Select
    End Function

    Shared Function IsPrivate(ByVal Member As MemberInfo) As Boolean
        Select Case Member.MemberType
            Case MemberTypes.Constructor
                Return DirectCast(Member, ConstructorInfo).IsPrivate
            Case MemberTypes.Event
                Dim eventM As EventInfo = DirectCast(Member, EventInfo)
                Return CBool(Helper.GetEventAccess(eventM) = MethodAttributes.Private)
            Case MemberTypes.Field
                Return DirectCast(Member, FieldInfo).IsPrivate
            Case MemberTypes.NestedType
                Return DirectCast(Member, Type).IsNestedPrivate
            Case MemberTypes.Method
                Return DirectCast(Member, MethodInfo).IsPrivate
            Case MemberTypes.Property
                Dim pInfo As PropertyInfo = DirectCast(Member, PropertyInfo)
                Return Helper.GetPropertyAccess(pInfo) = MethodAttributes.Private
            Case MemberTypes.TypeInfo
                Dim tp As Type = DirectCast(Member, Type)
                Return tp.IsNested AndAlso tp.IsNestedPrivate
            Case Else
                Throw New InternalException("")
        End Select
    End Function

    Shared Function IsPublic(ByVal Member As MemberInfo) As Boolean
        Select Case Member.MemberType
            Case MemberTypes.Constructor
                Return DirectCast(Member, ConstructorInfo).IsPublic
            Case MemberTypes.Event
                Dim eventM As EventInfo = DirectCast(Member, EventInfo)
                Return CBool(Helper.GetEventAccess(eventM) = MethodAttributes.Public)
            Case MemberTypes.Field
                Return DirectCast(Member, FieldInfo).IsPublic
            Case MemberTypes.Method
                Return DirectCast(Member, MethodInfo).IsPublic
            Case MemberTypes.Property
                Dim pInfo As PropertyInfo = DirectCast(Member, PropertyInfo)
                Return Helper.GetPropertyAccess(pInfo) = MethodAttributes.Public
            Case MemberTypes.TypeInfo, MemberTypes.NestedType
                Dim tp As Type = DirectCast(Member, Type)
                Return tp.IsPublic OrElse (tp.IsNested AndAlso tp.IsNestedPublic)
            Case Else
                Throw New InternalException("")
        End Select
    End Function

    Shared Function FilterByTypeArguments(ByVal Members As Generic.List(Of MemberInfo), ByVal TypeArguments As TypeArgumentList) As Generic.List(Of MemberInfo)
        Dim result As New Generic.List(Of MemberInfo)
        Dim argCount As Integer

        If TypeArguments IsNot Nothing Then argCount = TypeArguments.Count

        For Each member As MemberInfo In Members
            Dim minfo As MethodInfo = TryCast(member, MethodInfo)
            If minfo IsNot Nothing Then
                If minfo.GetGenericArguments.Length = argCount Then
                    If argCount > 0 Then
                        member = TypeArguments.Parent.Compiler.TypeManager.MakeGenericMethod(TypeArguments.Parent, minfo, minfo.GetGenericArguments, TypeArguments.AsTypeArray)
                        result.Add(member)
                    Else
                        result.Add(member)
                    End If
                Else
                    'Helper.StopIfDebugging()
                End If
            Else
                result.Add(member)
            End If
        Next

        Return result
    End Function

    <Obsolete()> Shared Function FilterByName(ByVal members() As MemberInfo, ByVal Name As String) As Generic.List(Of MemberInfo)
        Dim result As New Generic.List(Of MemberInfo)
        Helper.AssertNotNothing(members)
        For Each member As MemberInfo In members
            If NameResolution.CompareName(member.Name, Name) Then result.Add(member)
        Next
        Return result
    End Function

    Shared Function FilterByName(ByVal members() As PropertyInfo, ByVal Name As String) As Generic.List(Of PropertyInfo)
        Dim result As New Generic.List(Of PropertyInfo)
        Helper.AssertNotNothing(members)
        For Each member As PropertyInfo In members
            If NameResolution.CompareName(member.Name, Name) Then result.Add(member)
        Next
        Return result
    End Function

    Shared Function FilterByName2(ByVal Members As Generic.List(Of MemberInfo), ByVal Name As String) As Generic.List(Of MemberInfo)
        Dim result As New Generic.List(Of MemberInfo)
        For Each member As MemberInfo In Members
            If NameResolution.CompareName(member.Name, Name) Then result.Add(member)
        Next
        Return result
    End Function

    Shared Function FilterByName(ByVal collection As ICollection, ByVal Name As String) As ArrayList
        Dim result As New ArrayList
        Dim tmpname As String = ""
        For Each obj As Object In collection
            If TypeOf obj Is INameable Then
                tmpname = DirectCast(obj, INameable).Name
            ElseIf TypeOf obj Is MemberInfo Then
                tmpname = DirectCast(obj, MemberInfo).Name
            Else
                Helper.NotImplemented()
            End If
            If NameResolution.CompareName(Name, tmpname) Then result.Add(obj)
        Next

        Return result
    End Function

    Shared Function FilterByName(ByVal collection As Generic.List(Of Type), ByVal Name As String) As Generic.List(Of Type)
        Dim result As New Generic.List(Of Type)
        Dim tmpname As String = ""
        For Each obj As Type In collection
            If NameResolution.CompareName(Name, obj.Name) Then result.Add(obj)
        Next

        Return result
    End Function

    Shared Sub FilterByName(ByVal collection As Generic.List(Of Type), ByVal Name As String, ByVal result As Generic.List(Of MemberInfo))
        For Each obj As Type In collection
            If NameResolution.NameCompare(Name, obj.Name) Then result.Add(obj)
        Next
    End Sub

    Shared Function FilterByName(ByVal Types As TypeList, ByVal Name As String) As TypeList
        Dim result As New TypeList
        For Each obj As Type In Types
            If NameResolution.CompareName(Name, obj.Name) Then result.Add(obj)
        Next
        Return result
    End Function

    Shared Function FilterByName(ByVal Types As TypeDictionary, ByVal Name As String) As Type
        If Types.ContainsKey(Name) Then
            Return Types(Name)
        Else
            Return Nothing
        End If
    End Function

    ''' <summary>
    ''' Returns a list of type descriptors that only are modules.
    ''' </summary>
    ''' <param name="Types"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function FilterToModules(ByVal Compiler As Compiler, ByVal Types As Generic.List(Of Type)) As Generic.List(Of Type)
        Dim result As New Generic.List(Of Type)
        For Each t As Type In Types
            If IsModule(Compiler, t) Then result.Add(t)
        Next
        Return result
    End Function

    Shared Function FilterTo(Of DesiredType As Class, CollectionType As Class)(ByVal Types As Generic.List(Of CollectionType)) As Generic.List(Of CollectionType)
        Dim result As New Generic.List(Of CollectionType)
        For Each t As CollectionType In Types
            If TypeOf t Is DesiredType Then result.Add(t)
        Next
        Return result
    End Function


    ''' <summary>
    ''' Finds a non-private, non-shared constructor with no parameters in the array.
    ''' If nothing found, returns nothing.
    ''' </summary>
    ''' <param name="Constructors"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Function GetDefaultConstructor(ByVal Constructors() As ConstructorInfo) As ConstructorInfo
        For i As Integer = 0 To Constructors.GetUpperBound(0)
            If HasParameters(Constructors(i)) = False OrElse HasOnlyOptionalParameters(Constructors(i)) Then
                If Constructors(i).IsStatic = False AndAlso Constructors(i).IsPrivate = False Then
                    Return Constructors(i)
                End If
            End If
        Next
        Return Nothing
    End Function

    Function HasOnlyOptionalParameters(ByVal Constructor As ConstructorInfo) As Boolean
        Helper.Assert(HasParameters(Constructor))
        Return Constructor.GetParameters(0).IsOptional
    End Function

    ''' <summary>
    ''' Returns true if this constructor has any parameter, default or normal parameter.
    ''' </summary>
    ''' <param name="Constructor"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function HasParameters(ByVal Constructor As ConstructorInfo) As Boolean
        Return Constructor.GetParameters().Length > 0
    End Function

    ''' <summary>
    ''' Finds a public, non-shared constructor with no parameters of the type.
    ''' If nothing found, returns nothing.
    ''' </summary>
    ''' <param name="tp"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Function GetDefaultConstructor(ByVal tp As Type) As ConstructorInfo
        Return GetDefaultConstructor(GetConstructors(tp))
    End Function

    Function GetDefaultGenericConstructor(ByVal tn As ConstructedTypeName) As ConstructorInfo
        Dim result As ConstructorInfo
        Dim candidates() As ConstructorInfo

        Dim openconstructor As ConstructorInfo
        If tn.ResolvedType.GetType.Name = "TypeBuilderInstantiation" Then
            candidates = tn.OpenResolvedType.GetConstructors(BindingFlags.DeclaredOnly Or BindingFlags.Instance Or BindingFlags.Public)
            openconstructor = GetDefaultConstructor(candidates)
            result = TypeBuilder.GetConstructor(tn.ClosedResolvedType, openconstructor)
        Else
            candidates = tn.ClosedResolvedType.GetConstructors(BindingFlags.DeclaredOnly Or BindingFlags.Instance Or BindingFlags.Public)
            result = GetDefaultConstructor(candidates)
            ' result = New GenericConstructorDescriptor(tn, tn.ClosedResolvedType, result)
        End If

        Return result
    End Function

    ''' <summary>
    ''' Returns all the constructors of the type descriptor. (instance + static + public + nonpublic)
    ''' </summary>
    ''' <param name="tp"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Function GetConstructors(ByVal tp As Type) As ConstructorInfo()
        Helper.Assert(tp IsNot Nothing)
        Helper.Assert(TypeOf tp Is TypeBuilder = False AndAlso tp.GetType.Name <> "TypeBuilderInstantiation")
        Return tp.GetConstructors(BindingFlags.Instance Or BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Static Or BindingFlags.DeclaredOnly)
    End Function

    Shared Function GetParameterTypes(ByVal Parameters As ParameterInfo()) As Type()
        Dim result() As Type
        Helper.Assert(Parameters IsNot Nothing)
        ReDim result(Parameters.Length - 1)
        For i As Integer = 0 To Parameters.GetUpperBound(0)
            result(i) = Parameters(i).ParameterType
        Next
        Return result
    End Function

    ''' <summary>
    ''' Checks if the specified type is a VB Module.
    ''' </summary>
    ''' <param name="type"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function IsModule(ByVal Compiler As Compiler, ByVal type As Type) As Boolean
        Dim result As Boolean
        If TypeOf type Is TypeDescriptor Then
            Return IsModule(Compiler, DirectCast(type, TypeDescriptor))
        Else
            result = type.IsClass AndAlso Compiler.TypeCache.StandardModuleAttribute IsNot Nothing AndAlso type.IsDefined(Compiler.TypeCache.StandardModuleAttribute, False)

            'Compiler.Report.WriteLine("IsModule: type=" & type.FullName & ", result=" & result.ToString)
            'If type.Name = "Constants" Then Stop
            Return result
        End If
    End Function

    ''' <summary>
    ''' Checks if the specified type is a VB Module.
    ''' </summary>
    ''' <param name="type"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function IsModule(ByVal Compiler As Compiler, ByVal type As TypeDescriptor) As Boolean
        If type.Declaration IsNot Nothing Then
            Return type.Declaration.IsModule
        Else
            Return IsModule(Compiler, type.TypeInReflection)
        End If
    End Function

    Shared Function FilterByName(ByVal lst As Generic.List(Of TypeDescriptor), ByVal Name As String) As Generic.List(Of TypeDescriptor)
        Dim result As New Generic.List(Of TypeDescriptor)
        For Each t As TypeDescriptor In lst
            If NameResolution.CompareName(t.Name, Name) Then result.Add(t)
        Next
        Return result
    End Function

    '''' <summary>
    '''' Returns all members from the specified type.
    '''' Included: 
    '''' - all scopes for the compiling code, public and protected for external assemblies.
    '''' - instance and shared members.
    '''' - inherited members.
    '''' </summary>
    '''' <param name="Type"></param>
    '''' <returns></returns>
    '''' <remarks></remarks>
    '<Obsolete()> Shared Function GetMembers(ByVal Compiler As Compiler, ByVal Type As Type) As MemberInfo()
    '    Static cache As New Generic.Dictionary(Of Type, Generic.List(Of MemberInfo))
    '    Dim result As Generic.List(Of MemberInfo)

    '    If TypeOf Type Is TypeDescriptor = False AndAlso cache.ContainsKey(Type) Then
    '        result = cache(Type)
    '    Else
    '        Dim reflectableType As Type
    '        reflectableType = Compiler.TypeManager.GetRegisteredType(Type)

    '        Dim memberCache As MemberCache
    '        If Compiler.TypeManager.MemberCache.ContainsKey(reflectableType) = False Then
    '            memberCache = New MemberCache(Compiler, reflectableType)
    '        Else
    '            memberCache = Compiler.TypeManager.MemberCache(reflectableType)
    '        End If

    '        Dim result2 As Generic.List(Of MemberInfo)
    '        result2 = memberCache.FlattenedCache.GetAllMembers

    '        'result = New Generic.List(Of MemberInfo)
    '        'result.AddRange(reflectableType.GetMembers(Helper.ALLNOBASEMEMBERS))

    '        ''RemoveShadowed(Compiler, result)

    '        'If reflectableType.BaseType IsNot Nothing Then
    '        '    AddMembers(Compiler, Type, result, GetMembers(Compiler, reflectableType.BaseType))
    '        'ElseIf reflectableType.IsGenericParameter = False AndAlso reflectableType.IsInterface Then
    '        '    Dim ifaces() As Type
    '        '    ifaces = reflectableType.GetInterfaces()
    '        '    For Each iface As Type In ifaces
    '        '        Helper.AddMembers(Compiler, reflectableType, result, iface.GetMembers(Helper.ALLMEMBERS))
    '        '    Next
    '        '    Helper.AddMembers(Compiler, reflectableType, result, Compiler.TypeCache.Object.GetMembers(Helper.ALLMEMBERS))
    '        'End If
    '        'result = Helper.FilterExternalInaccessible(Compiler, result)

    '        'Helper.Assert(result.Count <= result2.Count)

    '        result = result2

    '        If TypeOf Type Is TypeDescriptor = False Then cache.Add(Type, result)
    '    End If

    '    Return result.ToArray
    'End Function

    '''' <summary>
    '''' Gets all the members in the specified type with the specified name.
    '''' Returns Nothing if nothing is found.
    '''' </summary>
    '''' <param name="Type"></param>
    '''' <param name="Name"></param>
    '''' <returns></returns>
    '''' <remarks></remarks>
    '<Obsolete()> Shared Function GetMembers(ByVal Compiler As Compiler, ByVal Type As Type, ByVal Name As String) As MemberInfo()
    '    Dim result As New Generic.List(Of MemberInfo)

    '    result.AddRange(GetMembers(Compiler, Type))
    '    result = Helper.FilterByName2(result, Name)

    '    Return result.ToArray
    'End Function

    ''' <summary>
    ''' Creates an integer array of the arguments.
    ''' </summary>
    ''' <param name="Info"></param>
    ''' <param name="Arguments"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function EmitIntegerArray(ByVal Info As EmitInfo, ByVal Arguments As ArgumentList) As Boolean
        Dim result As Boolean = True

        Dim arrayType As Type = Info.Compiler.TypeCache.Integer_Array
        Dim elementType As Type = arrayType.GetElementType
        Dim tmpVar As LocalBuilder = Info.ILGen.DeclareLocal(arrayType)
        Dim elementInfo As EmitInfo = Info.Clone(True, False, elementType)

        'Create the array.
        ArrayCreationExpression.EmitArrayCreation(Info, arrayType, New Generic.List(Of Integer)(New Integer() {Arguments.Count}))

        'Save it into a temporary variable.
        Emitter.EmitStoreVariable(Info, tmpVar)

        'Store every element into its index in the array.
        For i As Integer = 0 To Arguments.Count - 1
            'Load the array variable.
            Emitter.EmitLoadVariable(Info, tmpVar)
            Emitter.EmitLoadI4Value(Info, i)
            'Load all the indices.
            result = Arguments(i).GenerateCode(elementInfo) AndAlso result
            'Store the element in the arry.
            Emitter.EmitStoreElement(elementInfo, elementType, arrayType)
            'Increment the indices.
        Next

        'Load the final array onto the stack.
        Emitter.EmitLoadVariable(Info, tmpVar)

        Return result
    End Function

    Shared Function EmitStoreArrayElement(ByVal Info As EmitInfo, ByVal ArrayVariable As Expression, ByVal Arguments As ArgumentList) As Boolean
        Dim result As Boolean = True
        Dim ArrayType As Type = ArrayVariable.ExpressionType
        Dim ElementType As Type = ArrayType.GetElementType
        Dim isNonPrimitiveValueType As Boolean = ElementType.IsPrimitive = False AndAlso ElementType.IsValueType
        Dim isArraySetValue As Boolean = ArrayType.GetArrayRank > 1
        Dim newValue As Expression = Info.RHSExpression

        Helper.Assert(newValue IsNot Nothing)
        Helper.Assert(newValue.Classification.IsValueClassification)

        result = ArrayVariable.GenerateCode(Info.Clone(True, False, ArrayType)) AndAlso result

        If isArraySetValue Then
            result = newValue.GenerateCode(Info.Clone(True, False, ElementType)) AndAlso result
            If ElementType.IsValueType Then
                Emitter.EmitBox(Info)
            End If
            result = EmitIntegerArray(Info, Arguments) AndAlso result
            Emitter.EmitCallOrCallVirt(Info, Info.Compiler.TypeCache.Array_SetValue)
        Else
            Dim methodtypes As New Generic.List(Of Type)
            Dim elementInfo As EmitInfo = Info.Clone(True, False, Info.Compiler.TypeCache.Integer)
            For i As Integer = 0 To Arguments.Count - 1
                result = Arguments(i).GenerateCode(elementInfo) AndAlso result
                Emitter.EmitConversion(Info.Compiler.TypeCache.Integer, Info)
                methodtypes.Add(Info.Compiler.TypeCache.Integer)
            Next

            Dim rInfo As EmitInfo = Info.Clone(True, False, ElementType)
            methodtypes.Add(ElementType)

            If isNonPrimitiveValueType Then
                Emitter.EmitLoadElementAddress(Info, ElementType, ArrayType)
                result = Info.RHSExpression.Classification.GenerateCode(rInfo) AndAlso result
                Emitter.EmitStoreObject(Info, ElementType)
            Else
                result = Info.RHSExpression.Classification.GenerateCode(rInfo) AndAlso result
                Emitter.EmitStoreElement(Info, ElementType, ArrayType)
            End If
        End If
        Return result
    End Function

    Shared Function EmitLoadArrayElement(ByVal Info As EmitInfo, ByVal ArrayVariable As Expression, ByVal Arguments As ArgumentList) As Boolean
        Dim result As Boolean = True
        Dim ArrayType As Type = ArrayVariable.ExpressionType
        Dim ElementType As Type = ArrayType.GetElementType
        Dim isNonPrimitiveValueType As Boolean = ElementType.IsPrimitive = False AndAlso ElementType.IsValueType
        Dim isArrayGetValue As Boolean = ArrayType.GetArrayRank > 1

        result = ArrayVariable.GenerateCode(Info) AndAlso result

        If isArrayGetValue Then
            result = EmitIntegerArray(Info, Arguments) AndAlso result
            Emitter.EmitCallOrCallVirt(Info, Info.Compiler.TypeCache.Array_GetValue)
            If ElementType.IsValueType Then
                Emitter.EmitUnbox(Info, ElementType)
            Else
                Emitter.EmitCastClass(Info, Info.Compiler.TypeCache.Object, ElementType)
            End If
        Else
            Dim elementInfo As EmitInfo = Info.Clone(True, False, Info.Compiler.TypeCache.Integer)
            Dim methodtypes(Arguments.Count - 1) As Type
            For i As Integer = 0 To Arguments.Count - 1
                result = Arguments(i).GenerateCode(elementInfo) AndAlso result
                Emitter.EmitConversion(Info.Compiler.TypeCache.Integer, Info)
                methodtypes(i) = Info.Compiler.TypeCache.Integer
            Next

            If isNonPrimitiveValueType Then
                Emitter.EmitLoadElementAddress(Info, ElementType, ArrayType)
                Emitter.EmitLoadObject(Info, ElementType)
            Else
                Emitter.EmitLoadElement(Info, ArrayType)
            End If
        End If
        Return result
    End Function

    Shared Function EmitArrayCreation(ByVal Info As EmitInfo) As Boolean

    End Function

    ''' <summary>
    ''' Emits the instanceexpression (if any), the arguments (if any), the optional arguments (if any) and then calls the method (virt or not).
    ''' </summary>
    ''' <param name="Info"></param>
    ''' <param name="InstanceExpression"></param>
    ''' <param name="Arguments"></param>
    ''' <param name="Method"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function EmitArgumentsAndCallOrCallVirt(ByVal Info As EmitInfo, ByVal InstanceExpression As Expression, ByVal Arguments As ArgumentList, ByVal Method As MethodBase) As Boolean
        Dim result As Boolean = True
        Dim needsConstrained As Boolean
        Dim constrainedLocal As LocalBuilder = Nothing

        needsConstrained = InstanceExpression IsNot Nothing AndAlso InstanceExpression.ExpressionType.IsGenericParameter

        If InstanceExpression IsNot Nothing Then
            Dim ieDesiredType As Type
            Dim ieInfo As EmitInfo

            If needsConstrained Then
                ieDesiredType = InstanceExpression.ExpressionType
            Else
                ieDesiredType = Method.DeclaringType
                If ieDesiredType.IsValueType Then
                    ieDesiredType = Info.Compiler.TypeManager.MakeByRefType(CType(Info.Method, ParsedObject), ieDesiredType)
                End If
            End If

            ieInfo = Info.Clone(True, False, ieDesiredType)

            result = InstanceExpression.GenerateCode(ieInfo) AndAlso result

            If needsConstrained Then
                constrainedLocal = Emitter.DeclareLocal(Info, InstanceExpression.ExpressionType)
                Emitter.EmitStoreVariable(Info, constrainedLocal)
                Emitter.EmitLoadVariableLocation(Info, constrainedLocal)
            End If
        End If

        If Arguments IsNot Nothing Then
            Dim methodParameters() As ParameterInfo
            methodParameters = Helper.GetParameters(Info.Compiler, Method)
            result = Arguments.GenerateCode(Info, methodParameters) AndAlso result
        End If

        If needsConstrained Then
            Emitter.EmitConstrainedCallVirt(Info, Method, InstanceExpression.ExpressionType)
        ElseIf InstanceExpression IsNot Nothing AndAlso (TypeOf InstanceExpression Is MyClassExpression OrElse TypeOf InstanceExpression Is MyBaseExpression) Then
            Emitter.EmitCall(Info, Method)
        Else
            Emitter.EmitCallOrCallVirt(Info, Method)
        End If

        If constrainedLocal IsNot Nothing Then
            Emitter.FreeLocal(constrainedLocal)
        End If

        Return result
    End Function

    Shared Function GetInvokeMethod(ByVal Compiler As Compiler, ByVal DelegateType As Type) As MethodInfo
        Helper.Assert(IsDelegate(Compiler, DelegateType))
        Return DelegateType.GetMethod(DelegateDeclaration.STR_Invoke, BindingFlags.DeclaredOnly Or BindingFlags.Public Or BindingFlags.Instance)
    End Function

    Shared Function IsDelegate(ByVal Compiler As Compiler, ByVal Type As Type) As Boolean
        Return Helper.IsSubclassOf(Compiler.TypeCache.MulticastDelegate, Type)
    End Function

    ''' <summary>
    ''' Returns true if the type has a default property
    ''' </summary>
    ''' <param name="Type"></param>
    ''' <param name="DefaultProperties"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function HasDefaultProperty(ByVal Compiler As Compiler, ByVal Type As Type, ByRef DefaultProperties As Generic.List(Of PropertyInfo)) As Boolean
        Dim attrib As Reflection.DefaultMemberAttribute
        Dim tD As TypeDescriptor

        tD = TryCast(Type, TypeDescriptor)

        If tD Is Nothing Then
            Dim members As New Generic.List(Of MemberInfo)
            Dim properties As New Generic.List(Of PropertyInfo)

            Helper.Assert(Type IsNot Nothing)
            attrib = CType(System.Attribute.GetCustomAttribute(Type, GetType(DefaultMemberAttribute), True), DefaultMemberAttribute)

            If attrib Is Nothing Then Return False

            'members = Helper.FilterByName(Type.GetMembers(), attrib.MemberName)
            members = Compiler.TypeManager.GetCache(Type).LookupMembersFlattened(attrib.MemberName)

            For Each member As MemberInfo In members
                If member.MemberType = MemberTypes.Property Then
                    properties.Add(DirectCast(member, PropertyInfo))
                Else
                    Throw New InternalException("")
                End If
            Next
            DefaultProperties = properties
            Return True
        Else
            Dim members As New Generic.List(Of MemberInfo)
            Dim properties As New Generic.List(Of PropertyInfo)
            'members.AddRange(tD.GetMembers())
            members.AddRange(Compiler.TypeManager.GetCache(tD).FlattenedCache.GetAllMembers)
            For Each member As MemberInfo In members
                Dim propD As PropertyDescriptor = TryCast(member, PropertyDescriptor)
                Dim prop As PropertyInfo = TryCast(member, PropertyInfo)
                If propD IsNot Nothing Then
                    If propD.IsDefault Then properties.Add(propD)
                ElseIf prop IsNot Nothing Then
                    Helper.NotImplemented() 'I don't know if this is a possibility with generic types.
                End If
            Next
            If properties.Count = 0 Then
                If tD.BaseType IsNot Nothing Then
                    Return Helper.HasDefaultProperty(Compiler, tD.BaseType, DefaultProperties)
                Else
                    Return False
                End If
            Else
                DefaultProperties = properties
                Return True
            End If
        End If
    End Function

    Shared Function GetDefaultMemberAttribute(ByVal Type As Type) As DefaultMemberAttribute
        Dim attrib As Reflection.DefaultMemberAttribute
        Dim tD As TypeDescriptor

        tD = TryCast(Type, TypeDescriptor)

        If tD Is Nothing Then
            attrib = CType(System.Attribute.GetCustomAttribute(Type, GetType(DefaultMemberAttribute), True), DefaultMemberAttribute)
        Else
            Dim types() As Object
            types = tD.GetCustomAttributes(True)
            attrib = Nothing
        End If
        Return attrib
    End Function

    Shared Function IsShadows(ByVal Member As MemberInfo) As Boolean
        Dim result As Boolean = True
        Select Case Member.MemberType
            Case MemberTypes.Method, MemberTypes.Constructor
                Return DirectCast(Member, MethodBase).IsHideBySig = False
            Case MemberTypes.Property
                Return CBool(Helper.GetPropertyAttributes(DirectCast(Member, PropertyInfo)) And MethodAttributes.HideBySig) = False
            Case MemberTypes.Field
                Return True
            Case MemberTypes.TypeInfo
                Return True
            Case MemberTypes.NestedType
                Return True
            Case MemberTypes.Event
                Return DirectCast(Member, EventInfo).GetAddMethod.IsHideBySig = False
            Case Else
                Helper.NotImplemented()
                Throw New InternalException("")
        End Select
    End Function

    Shared Function IsShared(ByVal Member As MemberInfo) As Boolean
        Dim result As Boolean = True
        Select Case Member.MemberType
            Case MemberTypes.Method, MemberTypes.Constructor
                Return DirectCast(Member, MethodBase).IsStatic
            Case MemberTypes.Property
                Dim pInfo As PropertyInfo = DirectCast(Member, PropertyInfo)
                Return CBool(Helper.GetPropertyAttributes(pInfo) And MethodAttributes.Static)
            Case MemberTypes.Field
                Dim fInfo As FieldInfo = DirectCast(Member, FieldInfo)
                Return fInfo.IsStatic
            Case MemberTypes.TypeInfo
                Return False
            Case MemberTypes.NestedType
                Return False
            Case MemberTypes.Event
                Return DirectCast(Member, EventInfo).GetAddMethod.IsStatic
            Case Else
                Throw New InternalException("")
        End Select
    End Function

    Shared Function GetTypes(ByVal Params As ParameterInfo()) As Type()
        Dim result() As Type = Nothing

        If Params Is Nothing Then Return result
        ReDim result(Params.GetUpperBound(0))
        For i As Integer = 0 To Params.GetUpperBound(0)
            result(i) = Params(i).ParameterType
        Next
        Return result
    End Function

    Shared Function GetTypes(ByVal Arguments As Generic.List(Of Argument)) As Type()
        Dim result() As Type = Type.EmptyTypes

        If Arguments Is Nothing Then Return result
        ReDim result(Arguments.Count - 1)
        For i As Integer = 0 To Arguments.Count - 1
            Helper.Assert(Arguments(i) IsNot Nothing)
            If Arguments(i) IsNot Nothing AndAlso Arguments(i).Expression IsNot Nothing Then
                result(i) = Arguments(i).Expression.ExpressionType
            End If
        Next
        Return result
    End Function

    Shared Function GetTypes(ByVal Params As ParameterInfo()()) As Type()()
        Dim result()() As Type

        Helper.Assert(Params IsNot Nothing)

        ReDim result(Params.GetUpperBound(0))
        For i As Integer = 0 To Params.GetUpperBound(0)
            result(i) = Helper.GetTypes(Params(i))
        Next

        Return result
    End Function

    Shared Sub ApplyTypeArguments(ByVal Members As Generic.List(Of MemberInfo), ByVal TypeArguments As TypeArgumentList)
        If TypeArguments Is Nothing OrElse TypeArguments.Count = 0 Then Return

        For i As Integer = Members.Count - 1 To 0 Step -1
            Members(i) = ApplyTypeArguments(Members(i), TypeArguments)
            If Members(i) Is Nothing Then Members.RemoveAt(i)
        Next

    End Sub

    Shared Function ApplyTypeArguments(ByVal Member As MemberInfo, ByVal TypeArguments As TypeArgumentList) As MemberInfo
        Dim result As MemberInfo
        Dim minfo As MethodInfo

        minfo = TryCast(Member, MethodInfo)
        If minfo IsNot Nothing Then
            Dim args() As Type
            args = minfo.GetGenericArguments()

            If args.Length = TypeArguments.Count Then
                result = TypeArguments.Compiler.TypeManager.MakeGenericMethod(TypeArguments.Parent, minfo, args, TypeArguments.AsTypeArray)
            Else
                result = Nothing
            End If
        Else
            result = Nothing
            Helper.NotImplemented()
        End If

        Return result
    End Function

    Shared Function ApplyTypeArguments(ByVal Parent As ParsedObject, ByVal OpenType As Type, ByVal TypeParameters As Type(), ByVal TypeArguments() As Type) As Type
        Dim result As Type = Nothing

        If OpenType Is Nothing Then Return Nothing

        Helper.Assert(TypeParameters IsNot Nothing AndAlso TypeArguments IsNot Nothing)
        Helper.Assert(TypeParameters.Length = TypeArguments.Length)

        If OpenType.IsGenericParameter Then
            For i As Integer = 0 To TypeParameters.Length - 1
                If NameResolution.CompareName(TypeParameters(i).Name, OpenType.Name) Then
                    result = TypeArguments(i)
                    Exit For
                End If
            Next
            Helper.Assert(result IsNot Nothing)
        ElseIf OpenType.IsGenericType Then
            Dim typeParams() As Type
            Dim typeArgs As New Generic.List(Of Type)

            typeParams = OpenType.GetGenericArguments()

            For i As Integer = 0 To typeParams.Length - 1
                For j As Integer = 0 To TypeParameters.Length - 1
                    If NameResolution.CompareName(typeParams(i).Name, TypeParameters(j).Name) Then
                        typeArgs.Add(TypeArguments(j))
                        Exit For
                    End If
                Next
                If typeArgs.Count - 1 < i Then typeArgs.Add(typeParams(i))
            Next

            Helper.Assert(typeArgs.Count = typeParams.Length AndAlso typeArgs.Count > 0)

            result = Parent.Compiler.TypeManager.MakeGenericType(Parent, OpenType, typeArgs.ToArray)
        ElseIf OpenType.IsGenericTypeDefinition Then
            Helper.NotImplemented()
        ElseIf OpenType.ContainsGenericParameters Then
            If OpenType.IsArray Then
                Dim elementType As Type
                elementType = OpenType.GetElementType
                elementType = ApplyTypeArguments(Parent, elementType, TypeParameters, TypeArguments)
                result = New ArrayTypeDescriptor(Parent, elementType, OpenType.GetArrayRank)
            ElseIf OpenType.IsByRef Then
                Dim elementType As Type
                elementType = OpenType.GetElementType
                elementType = ApplyTypeArguments(Parent, elementType, TypeParameters, TypeArguments)
                result = New ByRefTypeDescriptor(Parent, elementType)
            Else
                Helper.NotImplemented()
            End If
        Else
            result = OpenType
        End If

        Helper.Assert(result IsNot Nothing)

        Return result
    End Function

    Shared Function ApplyTypeArguments(ByVal Parent As ParsedObject, ByVal OpenParameter As ParameterInfo, ByVal TypeParameters As Type(), ByVal TypeArguments() As Type) As ParameterInfo
        Dim result As ParameterInfo

        Helper.Assert(TypeParameters IsNot Nothing AndAlso TypeArguments IsNot Nothing)
        Helper.Assert(TypeParameters.Length = TypeArguments.Length)

        Dim paramType As Type
        paramType = ApplyTypeArguments(Parent, OpenParameter.ParameterType, TypeParameters, TypeArguments)

        If paramType Is OpenParameter.ParameterType Then
            result = OpenParameter
        Else
            result = Parent.Compiler.TypeManager.MakeGenericParameter(Parent, OpenParameter, paramType)
        End If

        Helper.Assert(result IsNot Nothing)

        Return result
    End Function

    Shared Function ApplyTypeArguments(ByVal Parent As ParsedObject, ByVal OpenParameters As ParameterInfo(), ByVal TypeParameters As Type(), ByVal TypeArguments() As Type) As ParameterInfo()
        Dim result(OpenParameters.Length - 1) As ParameterInfo

        For i As Integer = 0 To result.Length - 1
            result(i) = ApplyTypeArguments(Parent, OpenParameters(i), TypeParameters, TypeArguments)
        Next

        Return result
    End Function

    Shared Function GetConversionOperators(ByVal Compiler As Compiler, ByVal Names As Generic.Queue(Of String), ByVal Type As Type, ByVal ReturnType As Type) As Generic.List(Of MethodInfo)
        Dim ops As Generic.List(Of MethodInfo)

        ops = GetOperators(Compiler, Names, Type)

        For i As Integer = ops.Count - 1 To 0 Step -1
            If CompareType(ops(i).ReturnType, ReturnType) = False Then ops.RemoveAt(i)
        Next

        Return ops
    End Function


    Shared Function GetWideningConversionOperators(ByVal Compiler As Compiler, ByVal Type As Type, ByVal ReturnType As Type) As Generic.List(Of MethodInfo)
        Return GetConversionOperators(Compiler, New Generic.Queue(Of String)(New String() {"op_Implicit"}), Type, ReturnType)
    End Function

    Shared Function GetNarrowingConversionOperators(ByVal Compiler As Compiler, ByVal Type As Type, ByVal ReturnType As Type) As Generic.List(Of MethodInfo)
        Return GetConversionOperators(Compiler, New Generic.Queue(Of String)(New String() {"op_Explicit"}), Type, ReturnType)
    End Function

    Shared Function GetOperators(ByVal Compiler As Compiler, ByVal Names As Generic.Queue(Of String), ByVal Type As Type) As Generic.List(Of MethodInfo)
        Dim result As New Generic.List(Of MethodInfo)
        Dim testName As String

        'Dim members() As MemberInfo
        Dim members As Generic.List(Of MemberInfo)
        'members = Type.GetMembers(BindingFlags.Static Or BindingFlags.Public Or BindingFlags.NonPublic)
        members = Compiler.TypeManager.GetCache(Type).FlattenedCache.GetAllMembers

        Do Until Names.Count = 0
            testName = Names.Dequeue

            For Each member As MemberInfo In members
                If member.MemberType = MemberTypes.Method Then
                    Dim method As MethodInfo = DirectCast(member, MethodInfo)
                    If method.IsSpecialName AndAlso NameResolution.NameCompare(method.Name, testName) AndAlso method.IsStatic Then
                        result.Add(method)
                    End If
                End If
            Next
            If result.Count > 0 Then Exit Do
        Loop

        Return result
    End Function

    Shared Function GetUnaryOperators(ByVal Compiler As Compiler, ByVal Op As UnaryOperators, ByVal Type As Type) As Generic.List(Of MethodInfo)
        Dim opName As String
        Dim opNameAlternatives As New Generic.Queue(Of String)

        opName = Enums.GetStringAttribute(Op).Value
        opNameAlternatives.Enqueue(opName)

        Select Case Op
            Case UnaryOperators.Not
                opNameAlternatives.Enqueue("op_LogicalNot")
        End Select

        Return GetOperators(Compiler, opNameAlternatives, Type)
    End Function

    Shared Function GetBinaryOperators(ByVal Compiler As Compiler, ByVal Op As BinaryOperators, ByVal Type As Type) As Generic.List(Of MethodInfo)
        Dim opName As String
        Dim opNameAlternatives As New Generic.Queue(Of String)

        opName = Enums.GetStringAttribute(Op).Value
        opNameAlternatives.Enqueue(opName)

        Select Case Op
            Case BinaryOperators.And
                opNameAlternatives.Enqueue("op_LogicalAnd")
            Case BinaryOperators.Or
                opNameAlternatives.Enqueue("op_LogicalOr")
            Case BinaryOperators.ShiftLeft
                'See: http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconOperatorOverloadingUsageGuidelines.asp
                opNameAlternatives.Enqueue("op_SignedRightShift")
            Case BinaryOperators.ShiftRight
                opNameAlternatives.Enqueue("op_UnsignedRightShift")
        End Select

        Return GetOperators(Compiler, opNameAlternatives, Type)
    End Function

    ''' <summary>
    ''' Finds the parent namespace of the specified namespace.
    ''' "NS1.NS2" => "NS1"
    ''' "NS1" => ""
    ''' "" => Nothing
    ''' Nothing =>InternalException()
    ''' </summary>
    ''' <param name="Namespace"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function GetNamespaceParent(ByVal [Namespace] As String) As String
        If [Namespace] Is Nothing Then
            Throw New InternalException("")
        ElseIf [Namespace] = "" Then
            Return Nothing
        Else
            Dim dotIdx As Integer
            dotIdx = [Namespace].LastIndexOf("."c)
            If dotIdx > 0 Then
                Return [Namespace].Substring(0, dotIdx)
            ElseIf dotIdx = 0 Then
                Throw New InternalException("A namespace starting with a dot??")
            Else
                Return ""
            End If
        End If
    End Function

    Shared Function IsAccessibleExternal(ByVal Compiler As Compiler, ByVal Member As MemberInfo) As Boolean
        If Member.DeclaringType IsNot Nothing Then
            If Member.DeclaringType.Assembly IsNot Nothing Then
                If Member.DeclaringType.Assembly Is Compiler.AssemblyBuilder Then Return True
            End If
        End If

        If IsPublic(Member) Then Return True
        If IsProtectedOrAssem(Member) Then Return True
        If IsPrivate(Member) Then Return False
        If IsFriend(Member) Then Return False

        Return False
    End Function

    ''' <summary>
    ''' Checks if the called type is accessible from the caller type.
    ''' </summary>
    ''' <param name="CalledType"></param>
    ''' <param name="CallerType"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function IsAccessible(ByVal CalledType As Type, ByVal CallerType As Type) As Boolean
        If Not CalledType.Assembly Is CallerType.Assembly Then
            'The types are not in the same assembly, they can only be accessible if the
            'called type is public and all its declaring types are public.
            Dim declType As Type = CalledType
            Do Until declType Is Nothing
                If declType.IsPublic = False AndAlso declType.IsNestedPublic = False Then Return False
                declType = declType.DeclaringType
            Loop
            Return True
        End If

        'If it is the same type they are obviously accessible.
        If CompareType(CalledType, CallerType) Then Return True

        'Now both types are in the same assembly.

        'If the called type is not a nested type it is accessible.
        If CalledType.DeclaringType Is Nothing Then Return True
        'If the called type is a private nested type it is inaccessible
        If CalledType.IsNestedPrivate Then Return Helper.CompareType(CalledType.DeclaringType, CallerType)

        'Add all the surrounding types of the caller type to a list.
        Dim callerHierarchy As New Generic.List(Of Type)
        Dim tmp As Type = CallerType.DeclaringType
        Do Until tmp Is Nothing
            callerHierarchy.Add(tmp)
            tmp = tmp.DeclaringType
        Loop

        Dim tmpCaller As Type = CalledType.DeclaringType
        Do Until tmpCaller Is Nothing
            If callerHierarchy.Contains(tmpCaller) Then
                'We've reached a common surrounding type.
                'No matter what accessibility level this type has 
                'it is accessible.
                Return True
            End If
            If tmpCaller.IsNestedPrivate Then
                'There is a private type here...
                Return False
            End If
            tmpCaller = tmpCaller.DeclaringType
        Loop

        'There is no common surrounding type, and the access level of all 
        'surrounding types of the called types are non-private, so the type
        'is accessible.
        Return True
    End Function

    Shared Function IsAccessible(ByVal Compiler As Compiler, ByVal CalledMethodAccessability As MethodAttributes, ByVal CalledType As Type) As Boolean

        Helper.Assert(Compiler IsNot Nothing)
        Helper.Assert(CalledType IsNot Nothing)

        'Checks it the accessed method / type is accessible from the current compiling code
        '(for attributes that is not contained within a type)

        Dim testNested As Type = CalledType
        Dim compiledType As Boolean = Compiler.Assembly.IsDefinedHere(CalledType)
        Dim mostDeclaredType As Type = Nothing

        Do Until testNested Is Nothing
            mostDeclaredType = testNested
            'If it is a nested private type, it is not accessible.
            If testNested.IsNestedPrivate Then Return False
            'If it is not a nested public type in an external assembly, it is not accessible.
            If compiledType = False AndAlso testNested.IsNestedPublic = False AndAlso testNested.IsNested Then Return False
            testNested = testNested.DeclaringType
        Loop

        'If the most external type is not public then it is not accessible.
        If compiledType = False AndAlso mostDeclaredType.IsPublic = False Then Return False

        'The type is at least accessible now, check the method.

        Dim ac As MethodAttributes = (CalledMethodAccessability And MethodAttributes.MemberAccessMask)
        Dim isPrivate As Boolean = ac = MethodAttributes.Private
        Dim isFriend As Boolean = ac = MethodAttributes.Assembly OrElse ac = MethodAttributes.FamORAssem
        Dim isProtected As Boolean = ac = MethodAttributes.Family OrElse ac = MethodAttributes.FamORAssem
        Dim isPublic As Boolean = ac = MethodAttributes.Public

        'If the member is private, the member is not accessible
        '(to be accessible the types must be equal or the caller type must
        'be a nested type of the called type, cases already covered).
        If isPrivate Then Return False

        If isFriend AndAlso isProtected Then
            'Friend and Protected
            'If it is an external type it is not accessible.
            Return compiledType
        ElseIf isFriend Then
            'Friend, but not Protected
            'If it is an external type it is not accessible.
            Return compiledType
        ElseIf isProtected Then
            'Protected, but not Friend
            'It is not accessible.
            Return False
        ElseIf isPublic Then
            Return True
        End If

        Helper.NotImplemented("No accessibility??")

        Return False
    End Function


    Shared Function IsAccessible(ByVal CalledMethodAccessability As MethodAttributes, ByVal CalledType As Type, ByVal CallerType As Type) As Boolean
        'If both types are equal everything is accessible.
        If CompareType(CalledType, CallerType) Then Return True

        'If the callertype is a nested class of the called type, then everything is accessible as well.
        If IsNested(CalledType, CallerType) Then Return True

        'If the called type is not accessible from the caller, the member cannot be accessible either.
        If IsAccessible(CalledType, CallerType) = False Then Return False

        Dim ac As MethodAttributes = (CalledMethodAccessability And MethodAttributes.MemberAccessMask)
        Dim isPrivate As Boolean = ac = MethodAttributes.Private
        Dim isFriend As Boolean = ac = MethodAttributes.Assembly OrElse ac = MethodAttributes.FamORAssem
        Dim isProtected As Boolean = ac = MethodAttributes.Family OrElse ac = MethodAttributes.FamORAssem
        Dim isPublic As Boolean = ac = MethodAttributes.Public

        'Public members are always accessible!
        If isPublic Then Return True

        'If the member is private, the member is not accessible
        '(to be accessible the types must be equal or the caller type must
        'be a nested type of the called type, cases already covered).
        If isPrivate Then Return False

        If isFriend AndAlso isProtected Then
            'Friend and Protected
            'Both types must be in the same assembly or CallerType must inherit from CalledType.
            Return (CalledType.Assembly Is CallerType.Assembly) OrElse (CallerType.IsSubclassOf(CalledType))
        ElseIf isFriend Then
            'Friend, but not Protected
            'Both types must be in the same assembly
            Return CalledType.Assembly Is CallerType.Assembly
        ElseIf isProtected Then
            'Protected, but not Friend
            'CallerType must inherit from CalledType.
            Return CallerType.IsSubclassOf(CalledType)
        End If

        Helper.NotImplemented("No accessibility??")

        'private 	    = 1	= 0001
        'famandassembly = 2 = 0010
        'Assembly       = 3 = 0011
        'family         = 4 = 0100
        'famorassembly  = 5 = 0101
        'public 	    = 6	= 0110

    End Function

    ''' <summary>
    ''' Returns true if CallerType is a nested class of CalledType.
    ''' Returns false if both types are equal.
    ''' </summary>
    ''' <param name="CalledType"></param>
    ''' <param name="CallerType"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function IsNested(ByVal CalledType As Type, ByVal CallerType As Type) As Boolean
        Dim tmp As Type = CallerType.DeclaringType
        Do Until tmp Is Nothing
            If CompareType(CalledType, tmp) Then Return True
            tmp = tmp.DeclaringType
        Loop
        Return False
    End Function

    Shared Function IsAccessible(ByVal FieldAccessability As FieldAttributes, ByVal CalledType As Type, ByVal CallerType As Type) As Boolean
        'The fieldattributes for accessibility are the same as methodattributes.
        Return IsAccessible(CType(FieldAccessability, MethodAttributes), CalledType, CallerType)
    End Function

    Shared Function CreateGenericTypename(ByVal Typename As String, ByVal TypeArgumentCount As Integer) As String
        If TypeArgumentCount = 0 Then
            Return Typename
        Else
            Return Typename & "`" & TypeArgumentCount.ToString
        End If
    End Function

    Shared Function CreateArray(Of T)(ByVal Value As T, ByVal Length As Integer) As T()
        Dim result(Length - 1) As T
        For i As Integer = 0 To Length - 1
            result(i) = Value
        Next
        Return result
    End Function

    Shared Function GetDelegateArguments(ByVal Compiler As Compiler, ByVal delegateType As Type) As ParameterInfo()
        Dim method As MethodInfo = GetInvokeMethod(Compiler, delegateType)
        Return method.GetParameters
    End Function

    ''' <summary>
    ''' Finds the member with the exact same signature.
    ''' </summary>
    ''' <param name="grp"></param>
    ''' <param name="params"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function ResolveGroupExact(ByVal grp As Generic.List(Of MemberInfo), ByVal params() As Type) As MemberInfo
        Dim result As MemberInfo = Nothing

        For i As Integer = 0 To grp.Count - 1
            Dim member As MemberInfo = grp(i)
            Select Case member.MemberType
                Case MemberTypes.Method
                    Dim method As MethodInfo = DirectCast(member, MethodInfo)
                    Dim paramtypes As Type() = Helper.GetParameterTypes(method.GetParameters)
                    If Helper.CompareTypes(paramtypes, params) Then
                        Helper.Assert(result Is Nothing)
                        result = method
                        Exit For
                    End If
                Case MemberTypes.Property
                    Helper.NotImplemented()
                Case MemberTypes.Event
                    Helper.NotImplemented()
                Case Else
                    Throw New InternalException("")
            End Select
        Next

        Return result
    End Function
    ''' <summary>
    ''' Finds the member with the exact same signature.
    ''' </summary>
    ''' <param name="grp"></param>
    ''' <param name="params"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function ResolveGroupExact(ByVal grp As Generic.List(Of MethodBase), ByVal params() As Type) As MethodBase
        Dim result As MethodBase = Nothing

        For i As Integer = 0 To grp.Count - 1
            Dim member As MethodBase = grp(i)
            Dim paramtypes As Type() = Helper.GetParameterTypes(member.GetParameters)
            If Helper.CompareTypes(paramtypes, params) Then
                Helper.Assert(result Is Nothing)
                result = member
                Exit For
            End If
        Next

        Return result
    End Function

    Shared Function MakeArrayType(ByVal OriginalType As Type, ByVal Ranks As Integer) As Type
        Dim result As Type = OriginalType
        If Ranks = 1 Then
            result = result.MakeArrayType()
        ElseIf Ranks > 1 Then
            result = result.MakeArrayType(Ranks)
        Else
            Throw New InternalException("")
        End If
        Return result
    End Function

    <Diagnostics.Conditional("DEBUG")> Sub DumpDefine(ByVal Compiler As Compiler, ByVal Method As MethodBuilder)
        Dim rettype As Type = Method.ReturnType
        Dim strrettype As String
        If rettype IsNot Nothing Then
            If rettype.FullName Is Nothing Then
                strrettype = rettype.Name
            Else
                strrettype = rettype.FullName
            End If
        Else
            strrettype = ""
        End If
        Dim tp As Type = Method.DeclaringType
        Dim paramstypes As Type() = Helper.GetParameterTypes(Compiler, Method)
        Dim attribs As MethodAttributes = Method.Attributes
        Dim impl As MethodImplAttributes = Method.GetMethodImplementationFlags
        Dim name As String = tp.FullName & ":" & Method.Name

#If EXTENDEDDEBUG Then
        Try
            System.Console.ForegroundColor = ConsoleColor.Red
        Catch ex As Exception

        End Try

        Compiler.Report.WriteLine(vbnc.Report.ReportLevels.Debug, String.Format("Defined method '{0}'. Attributes: '{1}'. ImplAttributes: '{2}'. Parameters: '{3}', ReturnType: '{4}'", name, attribs.ToString.Replace("PrivateScope, ", ""), impl.ToString, TypesToString(paramstypes), strrettype))

        Try
            System.Console.ResetColor()
        Catch ex As Exception

        End Try
#End If
    End Sub

    <Diagnostics.Conditional("DEBUG")> Sub DumpDefine(ByVal Compiler As Compiler, ByVal Method As ConstructorBuilder)
        Dim rettype As Type = Nothing
        Dim strrettype As String
        If rettype IsNot Nothing Then
            strrettype = rettype.FullName
        Else
            strrettype = ""
        End If
        Dim tp As Type = Method.DeclaringType
        Dim paramstypes As Type() = Helper.GetParameterTypes(compiler, Method)
        Dim attribs As MethodAttributes = Method.Attributes
        Dim impl As MethodImplAttributes = Method.GetMethodImplementationFlags
        Dim name As String = tp.FullName & ":" & Method.Name

#If EXTENDEDDEBUG Then
        Compiler.Report.WriteLine(vbnc.Report.ReportLevels.Debug, String.Format("Defined ctor '{0}'. Attributes: '{1}'. ImplAttributes: '{2}'. Parameters: '{3}', ReturnType: '{4}'", name, attribs.ToString.Replace("PrivateScope, ", ""), impl.ToString, TypesToString(paramstypes), strrettype))
#End If

    End Sub

    'Shared Function TypesToString(ByVal types() As Type) As String
    '    Dim result As New System.Text.StringBuilder

    '    For i As Integer = 0 To types.GetUpperBound(0)
    '        If i > 0 Then result.Append(", ")
    '        result.Append(types(i).FullName)
    '    Next

    '    Return result.ToString
    'End Function

    Shared Function IsTypeConvertibleToAny(ByVal TypesToSearch As Type(), ByVal TypeToFind As Type) As Boolean
        For Each t As Type In TypesToSearch
            If Helper.CompareType(t, TypeToFind) OrElse t.IsSubclassOf(TypeToFind) Then Return True
        Next
        Return False
    End Function

    Shared Function IsNothing(Of T)(ByVal Value As T) As Boolean
        Return Value Is Nothing
    End Function

    <Diagnostics.Conditional("EXTENDEDDEBUG")> Sub AddCheck(ByVal Message As String)
#If EXTENDEDDEBUG Then
        Compiler.Report.WriteLine(vbnc.Report.ReportLevels.Debug, "Skipped check: " & Message)
#End If
    End Sub

    Shared Function DefineCollection(ByVal Collection As IEnumerable) As Boolean
        Dim result As Boolean = True
        For Each obj As IBaseObject In Collection
            result = obj.Define AndAlso result
        Next
        Return result
    End Function

    Shared Function DefineMembersCollection(ByVal Collection As Generic.IEnumerable(Of IDefinableMember)) As Boolean
        Dim result As Boolean = True
        For Each obj As IDefinableMember In Collection
            result = obj.DefineMember AndAlso result
        Next
        Return result
    End Function

    Shared Function ResolveCodeCollection(ByVal Collection As IEnumerable, ByVal Info As ResolveInfo) As Boolean
        Dim result As Boolean = True
        If Info Is Nothing Then Info = ResolveInfo.Default(Info.Compiler)
        For Each obj As BaseObject In Collection
            result = obj.ResolveCode(Info) AndAlso result
            Helper.Assert(result = (obj.Compiler.Report.Errors = 0))
        Next
        Return result
    End Function

    Shared Function ResolveTypeReferencesCollection(ByVal Collection As IEnumerable) As Boolean
        Dim result As Boolean = True
        For Each obj As ParsedObject In Collection
            result = obj.ResolveTypeReferences AndAlso result
            vbnc.Helper.Assert(result = (obj.Compiler.Report.Errors = 0))
        Next
        Return result
    End Function

    Shared Function ResolveTypeReferences(ByVal ParamArray Collection As ParsedObject()) As Boolean
        Dim result As Boolean = True
        For Each obj As ParsedObject In Collection
            If obj IsNot Nothing Then result = obj.ResolveTypeReferences AndAlso result
        Next
        Return result
    End Function

    Shared Function ResolveStatementCollection(ByVal Collection As IEnumerable, ByVal Info As ResolveInfo) As Boolean
        Dim result As Boolean = True
        For Each obj As Statement In Collection
            result = obj.ResolveStatement(Info) AndAlso result
        Next
        Return result
    End Function

    Shared Function GenerateCodeCollection(ByVal Collection As IEnumerable, ByVal Info As EmitInfo) As Boolean
        Dim result As Boolean = True
        For Each obj As IBaseObject In Collection
            result = obj.GenerateCode(Info) AndAlso result
        Next
        Return result
    End Function

    Shared Function GenerateCodeCollection(ByVal Collection As IList, ByVal Info As EmitInfo, ByVal Types As Type()) As Boolean
        Dim result As Boolean = True
        Helper.Assert(Collection.Count = Types.Length)
        For i As Integer = 0 To Collection.Count - 1
            result = DirectCast(Collection(i), IBaseObject).GenerateCode(Info.Clone(Info.IsRHS, Info.IsExplicitConversion, Types(i))) AndAlso result
        Next
        Return result
    End Function

    Shared Function CloneExpressionArray(ByVal Expressions() As Expression, ByVal NewParent As ParsedObject) As Expression()
        Dim result(Expressions.GetUpperBound(0)) As Expression
        For i As Integer = 0 To result.GetUpperBound(0)
            If Expressions(i) IsNot Nothing Then
                result(i) = Expressions(i).Clone(NewParent)
            End If
        Next
        Return result
    End Function

    ReadOnly Property Compiler() As Compiler
        Get
            Return m_Compiler
        End Get
    End Property

    ''' <summary>
    ''' If there is only one shared compiler, that one is returned, otherwise nothing is returned.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared ReadOnly Property SharedCompiler() As Compiler
        Get
            If m_SharedCompilers.Count = 1 Then
                Return m_SharedCompilers(0)
            Else
                Return Nothing
            End If
        End Get
    End Property

    Sub New(ByVal Compiler As Compiler)
        m_Compiler = Compiler
        If m_SharedCompilers.Contains(Compiler) = False Then
            m_SharedCompilers.Add(Compiler)
        End If
    End Sub

    <Diagnostics.DebuggerHidden()> Shared Sub Assert(ByVal Condition As Boolean, ByVal Message As String)
        If Condition = False Then
            Diagnostics.Debug.WriteLine(Message)
            If SharedCompiler IsNot Nothing Then SharedCompiler.Report.WriteLine(Report.ReportLevels.Debug, Message)
        End If
        Assert(Condition)
    End Sub

    <Diagnostics.DebuggerHidden()> Shared Sub Assert(ByVal Condition As Boolean)
        If Condition = False Then Helper.Stop()
    End Sub

    <Diagnostics.DebuggerHidden()> Shared Sub AssertNotNothing(ByVal Value As Object)
        If Value Is Nothing Then Helper.Stop()
        If TypeOf Value Is IEnumerable Then AssertNotNothing(DirectCast(Value, IEnumerable))
    End Sub

    <Diagnostics.DebuggerHidden()> Shared Sub AssertNotNothing(ByVal Value As IEnumerable)
        If Value Is Nothing Then
            Helper.Stop()
        Else
            For Each obj As Object In Value
                If obj Is Nothing Then Helper.Stop()
            Next
        End If
    End Sub

    Shared Sub AssertType(Of T)(ByVal Collection As IEnumerable)
        For Each v As Object In Collection
            Assert(TypeOf v Is T)
        Next
    End Sub

    <Diagnostics.DebuggerHidden()> Shared Sub AddError(Optional ByVal Message As String = "(No message provided)")
        Dim msg As String
        msg = "An error message should have been shown: '" & Message & "'"
        If IsDebugging() Then
            Console.WriteLine(msg)
            Diagnostics.Debug.WriteLine(msg)
            Stop
        Else
            Throw New NotImplementedException(msg)
        End If
    End Sub

    <Diagnostics.DebuggerHidden()> Shared Sub AddWarning(Optional ByVal Message As String = "(No message provided)")
        Dim msg As String
        msg = "A warning message should have been shown: '" & Message & "'"
        Diagnostics.Debug.WriteLine(msg)
        Console.WriteLine(msg)
        If IsDebugging() Then
            Stop
        Else
            'Throw New NotImplementedException(msg)
        End If
    End Sub

    Shared Function IsBootstrapping() As Boolean
        Return Reflection.Assembly.GetExecutingAssembly.Location.Contains("SelfCompile.exe")
    End Function

    Shared Function IsDebugging() As Boolean
        'Return False
        If Diagnostics.Debugger.IsAttached = False Then Return False
        If Reflection.Assembly.GetEntryAssembly Is Nothing Then Return False
        If Reflection.Assembly.GetEntryAssembly.FullName.Contains("rt") Then Return False
        If AppDomain.CurrentDomain.FriendlyName.Contains("rt") Then Return False
        Return True
    End Function

    <Diagnostics.Conditional("EXTENDEDDEBUG"), Diagnostics.DebuggerHidden()> Shared Sub NotImplementedYet(ByVal message As String)
#If EXTENDEDDEBUG Then
        Console.WriteLine("Not implemented yet: " & message)
#End If
    End Sub

    <Diagnostics.DebuggerHidden()> Shared Sub ErrorRecoveryNotImplemented()
        Console.WriteLine("Error recovery not implemented yet.")
    End Sub

    <Diagnostics.DebuggerHidden()> Shared Sub NotImplemented(Optional ByVal message As String = "")
        If IsDebugging() Then
            Diagnostics.Debug.WriteLine(message)
            Stop
        Else
            Throw New NotImplementedException(message)
        End If
    End Sub

    <Diagnostics.DebuggerHidden()> Shared Sub StopIfDebugging(Optional ByVal Condition As Boolean = True)
        If Condition AndAlso IsDebugging() Then
            Stop
        End If
    End Sub

    <Diagnostics.DebuggerHidden()> Shared Sub [Stop](Optional ByVal Message As String = "")
        If IsDebugging() Then
            Stop
        Else
            Throw New InternalException(Message)
        End If
    End Sub

    ''' <summary>
    ''' This function takes a string as an argument and split it on the space character,
    ''' with the " as acceptable character.
    ''' </summary>
    Shared Function ParseLine(ByVal strLine As String) As String()
        Dim strs As New ArrayList
        Dim bInQuote As Boolean
        Dim iStart As Integer
        Dim strAdd As String = ""

        For i As Integer = 0 To strLine.Length - 1
            If strLine.Chars(i) = """" Then
                If strLine.Length - 1 >= i + 1 AndAlso strLine.Chars(i + 1) = """" Then
                    strAdd &= """"
                Else
                    bInQuote = Not bInQuote
                End If
            ElseIf bInQuote = False AndAlso strLine.Chars(i) = " " Then
                If strAdd.Trim() <> "" Then strs.Add(strAdd)
                strAdd = ""
                iStart = i + 1
            Else
                strAdd &= strLine.Chars(i)
            End If
        Next
        If strAdd <> "" Then strs.Add(strAdd)

        'Add the strings to the return value
        Dim stt(strs.Count - 1) As String
        For i As Integer = 0 To strs.Count - 1
            stt(i) = DirectCast(strs(i), String)
        Next

        Return stt
    End Function

    ''' <summary>
    ''' Get the type attribute from the scope
    ''' </summary>
    ''' <param name="Modifiers"></param>
    ''' <param name="isNested"></param>
    ''' <returns></returns>
    ''' <remarks>
    ''' Scope: 
    ''' Private = private
    ''' Protected = family
    ''' Protected Friend = famorassem
    ''' Friend = assembly
    ''' Public = public
    ''' </remarks>
    Shared Function getTypeAttributeScopeFromScope(ByVal Modifiers As Modifiers, ByVal isNested As Boolean) As System.Reflection.TypeAttributes
        If Not isNested Then
            If Modifiers IsNot Nothing Then
                If Modifiers.Is(KS.Public) Then
                    Return Reflection.TypeAttributes.Public
                Else
                    Return Reflection.TypeAttributes.NotPublic
                End If
            Else
                Return TypeAttributes.NotPublic
            End If
        Else
            If Modifiers IsNot Nothing Then
                If Modifiers.Is(KS.Public) Then
                    Return Reflection.TypeAttributes.NestedPublic
                ElseIf Modifiers.Is(KS.Friend) Then
                    If Modifiers.Is(KS.Protected) Then
                        Return Reflection.TypeAttributes.NestedFamORAssem
                        '0Return Reflection.TypeAttributes.NotPublic
                        'Return Reflection.TypeAttributes.VisibilityMask
                    Else
                        Return Reflection.TypeAttributes.NestedAssembly
                        'Return Reflection.TypeAttributes.NotPublic
                    End If
                ElseIf Modifiers.Is(KS.Protected) Then
                    Return Reflection.TypeAttributes.NestedFamily
                    'Return Reflection.TypeAttributes.NotPublic
                ElseIf Modifiers.Is(KS.Private) Then
                    Return Reflection.TypeAttributes.NestedPrivate
                Else
                    'Compiler.Report.WriteLine(vbnc.Report.ReportLevels.Debug, "Default scope set to public...")
                    Return Reflection.TypeAttributes.NestedPublic
                End If
            Else
                Return Reflection.TypeAttributes.NestedPublic
            End If
        End If
    End Function

    'TODO: This function is horribly inefficient. Change to use shift operators.
    Shared Function BinToInt(ByVal str As String) As ULong
        Dim len As Integer = str.Length
        For i As Integer = len To 1 Step -1
            Select Case str.Chars(i - 1)
                Case "1"c
                    BinToInt += CULng(2 ^ (len - i))
                Case "0"c
                    'ok
                Case Else
                    Throw New ArgumentOutOfRangeException("str", str, "Invalid binary number: cannot contain character " & str.Chars(i - 1))
            End Select
        Next
    End Function

    Shared Function DecToDbl(ByVal str As String) As Double
        Return Double.Parse(str, USCulture)
    End Function

    Shared ReadOnly Property USCulture() As Globalization.CultureInfo
        Get
            Return New Globalization.CultureInfo("en-US")
        End Get
    End Property

    Shared Function DecToInt(ByVal str As String) As Decimal
        Return Decimal.Parse(str)
    End Function

    'TODO: This function can also be severely optimized.
    Shared Function HexToInt(ByVal str As String) As ULong
        Dim i, n As Integer
        Dim l As Integer = str.Length
        For i = l To 1 Step -1
            Select Case str.Chars(i - 1)
                Case "0"c
                    n = 0
                Case "1"c
                    n = 1
                Case "2"c
                    n = 2
                Case "3"c
                    n = 3
                Case "4"c
                    n = 4
                Case "5"c
                    n = 5
                Case "6"c
                    n = 6
                Case "7"c
                    n = 7
                Case "8"c
                    n = 8
                Case "9"c
                    n = 9
                Case "a"c, "A"c
                    n = 10
                Case "b"c, "B"c
                    n = 11
                Case "c"c, "C"c
                    n = 12
                Case "d"c, "D"c
                    n = 13
                Case "e"c, "E"c
                    n = 14
                Case "f"c, "F"c
                    n = 15
                Case Else
                    Throw New ArgumentOutOfRangeException("str", str, "Invalid hex number: cannot contain character " & str.Chars(i - 1))
            End Select

            HexToInt += CULng(n * (16 ^ (l - i)))
        Next
    End Function

    Shared Function IntToHex(ByVal Int As ULong) As String
        Return Microsoft.VisualBasic.Hex(Int)
    End Function

    Shared Function IntToBin(ByVal Int As ULong) As String
        If Int = 0 Then Return "0"
        IntToBin = ""
        Do Until Int = 0
            If CBool(Int And 1UL) Then
                IntToBin = "1" & IntToBin
            Else
                IntToBin = "0" & IntToBin
            End If
            Int >>= 1
        Loop
    End Function

    Shared Function IntToOct(ByVal Int As ULong) As String
        Return Microsoft.VisualBasic.Oct(Int)
    End Function

    'TODO: This function can also be severely optimized.
    Shared Function OctToInt(ByVal str As String) As ULong
        Dim i, n As Integer
        Dim l As Integer = str.Length
        For i = l To 1 Step -1
            Select Case str.Chars(i - 1)
                Case "0"c
                    n = 0
                Case "1"c
                    n = 1
                Case "2"c
                    n = 2
                Case "3"c
                    n = 3
                Case "4"c
                    n = 4
                Case "5"c
                    n = 5
                Case "6"c
                    n = 6
                Case "7"c
                    n = 7
                Case Else
                    Throw New ArgumentOutOfRangeException("str", str, "Invalid octal number: cannot contain character " & str.Chars(i - 1))
            End Select
            OctToInt += CULng(n * (8 ^ (l - i)))
        Next
    End Function

    '#If DEBUG Then
    '    ''' <summary>
    '    ''' Invokes Dump of all objects in the collection, but not the object ifself
    '    ''' </summary>
    '    ''' <param name="obj"></param>
    '    ''' <remarks></remarks>
    '    Shared Sub DumpCollection(ByVal obj As IList, ByVal Dumper As IndentedTextWriter, Optional ByVal Delimiter As String = "")
    '        Dim tmpDelimiter As String = ""
    '        For Each o As BaseObject In obj
    '            Dumper.Write(tmpDelimiter)
    '            o.Dump(Dumper)
    '            tmpDelimiter = Delimiter
    '        Next
    '    End Sub

    '    ''' <summary>
    '    ''' Invokes Dump of all objects in the collection, but not the object ifself
    '    ''' </summary>
    '    ''' <param name="obj"></param>
    '    ''' <remarks></remarks>
    '    Shared Sub DumpCollection(ByVal obj As IList, ByVal Dumper As IndentedTextWriter, ByVal Prefix As String, ByVal Postfix As String)
    '        For Each o As BaseObject In obj
    '            Dumper.Write(Prefix)
    '            o.Dump(Dumper)
    '            Dumper.Write(Postfix)
    '        Next
    '    End Sub

    '    ''' <summary>
    '    ''' Invokes Dump of all objects in the collection, but not the object ifself
    '    ''' </summary>
    '    ''' <param name="obj"></param>
    '    ''' <remarks></remarks>
    '    Shared Sub DumpCollection(ByVal obj As IEnumerable, ByVal Dumper As IndentedTextWriter, Optional ByVal Delimiter As String = "")
    '        Dim tmpDelimiter As String = ""
    '        For Each o As BaseObject In obj
    '            Dumper.Write(tmpDelimiter)
    '            o.Dump(Dumper)
    '            tmpDelimiter = Delimiter
    '        Next
    '    End Sub
    '#End If

    ''' <summary>
    ''' Returns a sequence number, incremented in 1 on every call
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function GetSequenceNumber() As Integer
        Static number As Integer
        number += 1
        Return number
    End Function

    ''' <summary>
    ''' Converts the value into how it would look in a source file. 
    ''' I.E: if it is a date, surround with #, if it is a string, surround with "
    ''' </summary>
    ''' <param name="Value"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function ValueToCodeConstant(ByVal Value As Object) As String
        If TypeOf Value Is String Then
            Return """" & Value.ToString.Replace("""", """""") & """"
        ElseIf TypeOf Value Is Char Then
            Return """" & Value.ToString.Replace("""", """""") & """c"
        ElseIf TypeOf Value Is Date Then
            Return "#" & Value.ToString & "#"
        ElseIf Value Is Nothing Then
            Return KS.Nothing.ToString
        Else
            Return Value.ToString
        End If
    End Function

    ''' <summary>
    ''' If the argument is a typedescriptor, looks up the 
    ''' </summary>
    ''' <param name="Type"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function GetTypeOrTypeBuilder(ByVal Type As Type) As Type
        If Type Is Nothing Then Return Nothing
        Dim tmp As TypeDescriptor = TryCast(Type, TypeDescriptor)
        If tmp Is Nothing Then
            Return Type
        Else
            Return tmp.TypeInReflection
        End If
    End Function

    ''' <summary>
    ''' If the argument is a typedescriptor, looks up the 
    ''' </summary>
    ''' <param name="Ctor"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function GetCtorOrCtorBuilder(ByVal Ctor As ConstructorInfo) As ConstructorInfo
        Dim tmp As ConstructorDescriptor = TryCast(Ctor, ConstructorDescriptor)
        If tmp Is Nothing Then
            Return Ctor
        Else
            Helper.Assert(tmp.ConstructorInReflection IsNot Nothing)
            Return tmp.ConstructorInReflection
        End If
    End Function

    Shared Function GetMethodOrMethodBuilder(ByVal Method As MethodInfo) As MethodInfo
        Dim tmp As MethodDescriptor = TryCast(Method, MethodDescriptor)
        If tmp Is Nothing Then
            Return Method
        Else
            Return tmp.MethodInReflection
        End If
    End Function

    Shared Function GetPropertyOrPropertyBuilder(ByVal [Property] As PropertyInfo) As PropertyInfo
        Dim tmp As PropertyDescriptor = TryCast([Property], PropertyDescriptor)
        If tmp Is Nothing Then
            Return [Property]
        Else
            Return tmp.PropertyInReflection
        End If
    End Function

    Shared Function GetFieldOrFieldBuilder(ByVal Field As FieldInfo) As FieldInfo
        Dim tmp As FieldDescriptor = TryCast(Field, FieldDescriptor)
        If tmp Is Nothing Then
            Return Field
        Else
            Return tmp.FieldInReflection
        End If
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="Type"></param>
    ''' <remarks></remarks>
    Shared Sub SetTypeOrTypeBuilder(ByVal Type As Type())
        If Type Is Nothing Then Return
        For i As Integer = 0 To Type.Length - 1
            Helper.Assert(Type(i) IsNot Nothing)
            Type(i) = GetTypeOrTypeBuilder(Type(i))
        Next
    End Sub

    Shared Function GetTypeOrTypeBuilders(ByVal Type As Type(), Optional ByVal OnlySuccessful As Boolean = False) As Type()
        Dim result() As Type
        If Type Is Nothing Then Return Nothing

        ReDim result(Type.GetUpperBound(0))
        For i As Integer = 0 To Type.GetUpperBound(0)
            Dim tmp As Type
            tmp = GetTypeOrTypeBuilder(Type(i))
            If tmp Is Nothing AndAlso OnlySuccessful Then
                result(i) = Type(i)
            Else
                result(i) = tmp
            End If
        Next
        Return result
    End Function

    Shared Function IsAssignable(ByVal Compiler As Compiler, ByVal FromType As Type, ByVal ToType As Type) As Boolean
        'If TypeOf FromType Is TypeDescriptor Then FromType = FromType.UnderlyingSystemType
        'If TypeOf ToType Is TypeDescriptor Then ToType = ToType.UnderlyingSystemType
#If EXTENDEDDEBUG Then
        Compiler.Report.WriteLine("IsAssignable (FromType := " & FromType.FullName & ", ToType := " & ToType.FullName)
#End If
        If FromType Is ToType Then
            Return True
        ElseIf Helper.CompareType(FromType, Compiler.TypeCache.Nothing) Then
            Return True
        ElseIf FromType.FullName Is Nothing AndAlso ToType.FullName Is Nothing AndAlso FromType.IsArray = True AndAlso ToType.IsArray = True AndAlso FromType.Name.Equals(ToType.Name, StringComparison.Ordinal) Then
            Return True
        ElseIf CompareType(ToType, Compiler.TypeCache.Object) Then
            Return True
        ElseIf TypeOf ToType Is GenericTypeParameterBuilder AndAlso TypeOf FromType Is Type Then
            Return ToType.Name = FromType.Name
        ElseIf ToType.GetType.Name = "TypeBuilderInstantiation" Then
            If Helper.CompareType(Helper.GetTypeOrTypeBuilder(FromType), ToType) Then
                Return True
            Else
                Helper.NotImplementedYet("")
            End If
            Return True
        ElseIf TypeOf ToType Is TypeDescriptor = False AndAlso TypeOf FromType Is TypeDescriptor = False AndAlso ToType.IsAssignableFrom(FromType) Then
            Return True
        ElseIf IsInterface(Compiler, ToType) Then
            Dim ifaces() As Type = Compiler.TypeManager.GetRegisteredType(FromType).GetInterfaces()
            For Each iface As Type In ifaces
                If Helper.IsAssignable(Compiler, iface, ToType) Then Return True
                If Helper.CompareType(iface, ToType) Then Return True
                If Helper.IsSubclassOf(ToType, iface) Then Return True
            Next
            If IsInterface(Compiler, FromType) AndAlso FromType.IsGenericType AndAlso ToType.IsGenericType Then
                Dim baseFromI, baseToI As Type
                baseFromI = FromType.GetGenericTypeDefinition
                baseToI = ToType.GetGenericTypeDefinition
                If Helper.CompareType(baseFromI, baseToI) Then
                    Dim fromArgs, toArgs As Type()
                    fromArgs = FromType.GetGenericArguments
                    toArgs = ToType.GetGenericArguments
                    If fromArgs.Length = toArgs.Length Then
                        For i As Integer = 0 To toArgs.Length - 1
                            If Helper.IsAssignable(Compiler, fromArgs(i), toArgs(i)) = False Then Return False
                        Next
                        Return True
                    End If
                End If
            End If
            Return False
        ElseIf Helper.IsEnum(FromType) AndAlso Compiler.TypeResolution.IsImplicitlyConvertible(Compiler, GetEnumType(Compiler, FromType), ToType) Then
            Return True
        ElseIf ToType.FullName IsNot Nothing AndAlso FromType.FullName IsNot Nothing AndAlso ToType.FullName.Equals(FromType.FullName, StringComparison.Ordinal) Then
            Return True
        ElseIf Helper.CompareType(Compiler.TypeCache.UInteger, ToType) AndAlso Helper.CompareType(Compiler.TypeCache.UShort, FromType) Then
            Return True
        ElseIf Helper.CompareType(FromType, Compiler.TypeCache.Object) Then
            Return False
        ElseIf Helper.IsSubclassOf(ToType, FromType) Then
            Return True
        ElseIf Helper.IsSubclassOf(FromType, ToType) Then
            Return False
        ElseIf FromType.IsArray AndAlso ToType.IsArray Then
            Return Helper.IsAssignable(Compiler, FromType.GetElementType, ToType.GetElementType)
        Else
            Helper.NotImplementedYet("Don't know if it possible to convert from " & FromType.Name & " to " & ToType.Name)
            Return False
        End If
    End Function

    Shared Function IsSubclassOf(ByVal BaseClass As Type, ByVal DerivedClass As Type) As Boolean
        Dim base As Type = DerivedClass.BaseType
        Do While base IsNot Nothing
            If Helper.CompareType(base, BaseClass) Then Return True
            base = base.BaseType
        Loop
        Return False
    End Function

    Shared Function DoesTypeImplementInterface(ByVal Compiler As Compiler, ByVal Type As Type, ByVal [Interface] As Type) As Boolean
        Dim ifaces() As Type
        ifaces = Compiler.TypeManager.GetRegisteredType(Type).GetInterfaces
        For Each iface As Type In ifaces
            If Helper.IsAssignable(Compiler, iface, [Interface]) Then Return True
        Next
        Return False
        Return Array.IndexOf(Type.GetInterfaces, [Interface]) >= 0
    End Function

    Shared Function GetEnumType(ByVal Compiler As Compiler, ByVal EnumType As Type) As Type
        Helper.Assert(EnumType.IsEnum)
        EnumType = Compiler.TypeManager.GetRegisteredType(EnumType)
        If TypeOf EnumType Is TypeDescriptor Then
            Return DirectCast(DirectCast(EnumType, TypeDescriptor).Declaration, EnumDeclaration).EnumConstantType
        Else
            Dim fInfo As FieldInfo
            fInfo = EnumType.GetField(EnumDeclaration.EnumTypeMemberName, BindingFlags.Static Or BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance)

            Helper.Assert(fInfo IsNot Nothing)

            Return fInfo.FieldType
        End If
    End Function

    ''' <summary>
    ''' Creates a CType expression containing the specified FromExpression if necessary.
    ''' </summary>
    ''' <param name="Parent"></param>
    ''' <param name="FromExpression"></param>
    ''' <param name="DestinationType"></param>
    ''' <param name="result"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function CreateTypeConversion(ByVal Parent As ParsedObject, ByVal FromExpression As Expression, ByVal DestinationType As Type, ByRef result As Boolean) As Expression
        Dim fromExpr As Expression

        Helper.Assert(FromExpression IsNot Nothing)

        fromExpr = FromExpression

        Dim fromType As Type
        fromType = FromExpression.ExpressionType

#If EXTENDEDDEBUG Then
        Parent.Compiler.Report.WriteLine("Creating type conversion, from " & fromType.FullName & " to " & DestinationType.FullName)
        If DestinationType.IsByRef Then
            Parent.Compiler.Report.WriteLine(">DestinationType.ElementType = " & DestinationType.GetElementType.FullName)
            Parent.Compiler.Report.WriteLine(">IsAssignable to DestinationType.ElementType = " & IsAssignable(Parent.Compiler, fromType, DestinationType.GetElementType))
        End If
#End If

        If Helper.CompareType(fromType, DestinationType) Then
            'do nothing
        ElseIf fromExpr.ExpressionType.IsByRef AndAlso IsAssignable(Parent.Compiler, fromType.GetElementType, DestinationType) Then
            'do nothing
        ElseIf DestinationType.IsByRef AndAlso IsAssignable(Parent.Compiler, fromExpr.ExpressionType, DestinationType.GetElementType) Then
#If EXTENDEDDEBUG Then
            Parent.Compiler.Report.WriteLine(">3")
#End If
            If fromExpr.ExpressionType.IsByRef = False AndAlso Helper.CompareType(fromExpr.ExpressionType, DestinationType.GetElementType) = False Then
                fromExpr = New CTypeExpression(Parent, fromExpr, DestinationType.GetElementType)
                result = fromExpr.ResolveExpression(ResolveInfo.Default(Parent.Compiler)) AndAlso result
            End If
            'do nothing
        ElseIf CompareType(fromExpr.ExpressionType, Parent.Compiler.TypeCache.Nothing) Then
            'do nothing
        ElseIf CompareType(DestinationType, Parent.Compiler.TypeCache.Enum) AndAlso fromExpr.ExpressionType.IsEnum Then
            fromExpr = New BoxExpression(Parent, fromExpr, fromExpr.ExpressionType)
        ElseIf CompareType(fromExpr.ExpressionType, DestinationType) = False Then
            Dim CTypeExp As CTypeExpression

            If fromExpr.ExpressionType.IsByRef Then
                fromExpr = New DeRefExpression(fromExpr, fromExpr)
            End If

            CTypeExp = New CTypeExpression(Parent, fromExpr, DestinationType)
            result = CTypeExp.ResolveExpression(ResolveInfo.Default(Parent.Compiler)) AndAlso result
            fromExpr = CTypeExp
        End If

#If EXTENDEDDEBUG Then
        If fromType IsNot FromExpression Then
            Parent.Compiler.Report.WriteLine(Report.ReportLevels.Debug, "Created type conversion from '" & FromExpression.ExpressionType.Name & "' to '" & DestinationType.Name & "'")
        End If
#End If

        Return fromExpr
    End Function

    ''' <summary>
    ''' Returns true if all types in both arrays are the exact same types.
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function CompareTypes(ByVal Types1() As Type, ByVal Types2() As Type) As Boolean
        If Types1 Is Nothing AndAlso Types2 Is Nothing Then
            Return True
        ElseIf Types1 Is Nothing Xor Types2 Is Nothing Then
            Return False
        Else
            If Types1.Length <> Types2.Length Then Return False
            For i As Integer = 0 To Types1.Length - 1
                If Helper.CompareType(Types1(i), Types2(i)) = False Then Return False
            Next
            Return True
        End If
    End Function

    Shared Function CompareType(ByVal t1 As Type, ByVal t2 As Type) As Boolean
        If t1 Is Nothing AndAlso t2 Is Nothing Then Return True

        Dim td1, td2 As TypeDescriptor
        td1 = TryCast(t1, TypeDescriptor)
        td2 = TryCast(t2, TypeDescriptor)

        If td1 IsNot Nothing AndAlso td2 IsNot Nothing Then
            'They are both type descriptors.
            Return td1.Equals(td2)
        ElseIf td1 Is Nothing AndAlso td2 Is Nothing Then
            'None of them are type descriptors.
            If t1 Is Nothing Then Return False
            Return t1.Equals(t2)
        Else
            If td1 Is Nothing Then
                Dim tmp As Type = t1
                td1 = td2
                t2 = t1
            End If
            'Only td1 is a type descriptor
            If td1.Declaration IsNot Nothing Then
                If td1.Declaration.TypeBuilder IsNot Nothing Then
                    Return td1.Declaration.TypeBuilder.Equals(t2)
                Else
                    Return False 'If t2 is a Type, but td1 doesn't have a TypeBuilder yet, both types cannot be equal.
                End If
            ElseIf TypeOf td1 Is TypeParameterDescriptor Then
                'td2 is not a typeparameterdescriptor
                Return False
            ElseIf td1.IsArray <> t2.IsArray Then
                Return False
            ElseIf td1.IsByRef AndAlso t2.IsByRef Then
                Return Helper.CompareType(td1.GetElementType, t2.GetElementType)
            ElseIf TypeOf td1 Is GenericTypeDescriptor Then
                Dim tdg1 As GenericTypeDescriptor = DirectCast(td1, GenericTypeDescriptor)
                If t2.IsGenericParameter = False AndAlso t2.IsGenericType = False AndAlso t2.IsGenericTypeDefinition = False AndAlso t2.ContainsGenericParameters = False Then
                    Return False
                ElseIf Helper.CompareType(tdg1.BaseType, t2.BaseType) = False Then
                    Return False
                ElseIf Helper.CompareType(tdg1.GetGenericTypeDefinition, t2.GetGenericTypeDefinition) = False Then
                    Return False
                Else
                    Dim args1, args2 As Type()
                    args1 = tdg1.GetGenericArguments
                    args2 = t2.GetGenericArguments
                    If args1.Length <> args2.Length Then Return False
                    For i As Integer = 0 To args1.Length - 1
                        If Helper.CompareType(args1(i), args2(i)) = False Then Return False
                    Next
                    Return True
                End If
            Else
                'td1 is a type descriptor, but it does not have a type declaration?
                Return False 'Helper.NotImplemented()
            End If
            Helper.NotImplemented()
        End If
    End Function

    ''' <summary>
    ''' Creates a vb-like representation of the parameters
    ''' </summary>
    ''' <param name="Params"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Overloads Shared Function ToString(ByVal Compiler As Compiler, ByVal Params As ParameterInfo()) As String
        Dim result As String = ""
        Dim sep As String = ""

        For Each t As ParameterInfo In Params
            Dim tmp As String
            If t.ParameterType.IsByRef Then
                tmp = "ByRef " & t.ParameterType.GetElementType.ToString
            Else
                tmp = t.ParameterType.ToString
            End If
            If t.IsOptional Then
                tmp = "Optional " & tmp
            End If
            If Helper.IsParamArrayParameter(Compiler, t) Then
                tmp = "ParamArray " & tmp
            End If
            result = result & sep & tmp
            sep = ", "
        Next

        Return "(" & result & ")"

    End Function

    Shared Function IsParamArrayParameter(ByVal Compiler As Compiler, ByVal Parameter As ParameterInfo) As Boolean
        Dim pD As ParameterDescriptor = TryCast(Parameter, ParameterDescriptor)
        If pD IsNot Nothing Then
            Return pD.IsParamArray
        Else
            Return Parameter.IsDefined(Compiler.TypeCache.ParamArrayAttribute, False)
        End If
    End Function

    Overloads Shared Function ToString(ByVal Types As Type()) As String
        Dim result As String = ""
        Dim sep As String = ""

        For Each t As Type In Types
            Helper.Assert(t IsNot Nothing)
            result &= sep & t.ToString
            sep = ", "
        Next

        Return "{" & result & "}"
    End Function

    Overloads Shared Function ToString(ByVal Compiler As Compiler, ByVal Member As MemberInfo) As String
        Dim result As String
        Select Case Member.MemberType
            Case MemberTypes.Method, MemberTypes.Constructor
                Dim minfo As MethodBase = DirectCast(Member, MethodBase)
                result = minfo.Name & "(" & Helper.ToString(Compiler, Helper.GetParameters(Compiler, minfo)) & ")"
            Case MemberTypes.Property
                Dim pinfo As PropertyInfo = DirectCast(Member, PropertyInfo)
                result = pinfo.Name & "(" & Helper.ToString(Compiler, Helper.GetParameters(Compiler, pinfo)) & ")"
            Case Else
                Helper.NotImplemented()
                result = ""
        End Select
        Return result
    End Function

    <Diagnostics.Conditional("DEBUGMETHODRESOLUTION")> Shared Sub LogResolutionMessage(ByVal Compiler As Compiler, ByVal msg As String)
        If LOGMETHODRESOLUTION Then
            Compiler.Report.WriteLine(vbnc.Report.ReportLevels.Debug, msg)
        End If
    End Sub

    <Diagnostics.Conditional("DEBUGMETHODADD")> Shared Sub LogAddMessage(ByVal Compiler As Compiler, ByVal msg As String, Optional ByVal condition As Boolean = True)
        If True AndAlso condition Then
            Compiler.Report.WriteLine(vbnc.Report.ReportLevels.Debug, msg)
        End If
    End Sub

    ''' <summary>
    ''' Creates the expression that is to be emitted for an optional parameter.
    ''' </summary>
    ''' <param name="Parent"></param>
    ''' <param name="Parameter"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function GetOptionalValueExpression(ByVal Parent As ParsedObject, ByVal Parameter As ParameterInfo) As Expression
        Dim result As Expression
        If Helper.CompareType(Parameter.ParameterType, Parent.Compiler.TypeCache.Object) Then
            'If an Object parameter does not specify a default value, then the expression 
            'System.Reflection.Missing.Value is used. 
            result = New LoadFieldExpression(Parent, Parent.Compiler.TypeCache.System_Reflection_Missing_Value)
        ElseIf Helper.CompareType(Parameter.ParameterType, Parent.Compiler.TypeCache.Integer) AndAlso Parameter.IsDefined(Parent.Compiler.TypeCache.MS_VB_CS_OptionCompareAttribute, False) Then
            'If an optional Integer parameter 
            'has the Microsoft.VisualBasic.CompilerServices.OptionCompareAttribute attribute, 
            'then the literal 1 is supplied for text comparisons and the literal 0 otherwise
            Dim cExp As ConstantExpression
            If Parent.Location.File.IsOptionCompareText Then
                cExp = New ConstantExpression(Parent, 1I, Parent.Compiler.TypeCache.Integer)
            Else
                cExp = New ConstantExpression(Parent, 0I, Parent.Compiler.TypeCache.Integer)
            End If
            result = cExp
        Else
            'If optional parameters remain, the default value 
            'specified in the optional parameter declaration is matched to the parameter. 
            Dim cExp As ConstantExpression
            cExp = New ConstantExpression(Parent, Parameter.DefaultValue, Parameter.ParameterType)
            result = cExp
        End If
        Return result
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="Parent"></param>
    ''' <param name="Member"></param>
    ''' <param name="InputParameters"></param>
    ''' <param name="Arguments"></param>
    ''' <param name="TypeArguments"></param>
    ''' <param name="outExactArguments"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Shared Function IsApplicable(ByVal Parent As ParsedObject, ByVal Member As MemberInfo, ByVal InputParameters As ParameterInfo(), ByVal Arguments As ArgumentList, ByVal TypeArguments As TypeArgumentList, ByRef outExactArguments() As Generic.List(Of Argument)) As Boolean
        Dim matchedParameters As New Generic.List(Of ParameterInfo)
        Dim exactArguments() As Generic.List(Of Argument) 'This contains the arguments in the exact order to emit
        Dim method As MethodBase = TryCast(Member, MethodBase)
        Dim prop As PropertyInfo = TryCast(Member, PropertyInfo)

        Dim isLastParamArray As Boolean
        Dim paramArrayParameter As ParameterInfo = Nothing
        Dim paramArrayExpression As ArrayCreationExpression = Nothing
        Dim inputParametersCount As Integer = InputParameters.Length

        ReDim exactArguments(1)
        'First element contains arguments to emit invocation of 
        'exact paramarray type (as if it was a normal parameter)
        'SomeMethod(New PType(){p1, p2, p3})
        exactArguments(0) = New Generic.List(Of Argument)(Helper.CreateArray(Of Argument)(Nothing, inputParametersCount))
        'Second element contains arguments to emit paramarray invocation
        'SomeMethod(p1, p2, p3)
        exactArguments(1) = New Generic.List(Of Argument)(exactArguments(0))

        If inputParametersCount > 0 Then
            isLastParamArray = Helper.IsParamArrayParameter(Parent.Compiler, InputParameters(inputParametersCount - 1))
            If isLastParamArray Then
                paramArrayParameter = InputParameters(inputParametersCount - 1)
                Dim paramArrayArg As New PositionalArgument(Parent)

                Helper.Assert(paramArrayExpression Is Nothing)
                paramArrayExpression = New ArrayCreationExpression(paramArrayArg)
                paramArrayExpression.Init(paramArrayParameter.ParameterType, New Expression() {})

                paramArrayArg.Init(paramArrayParameter.Position, paramArrayExpression)
                exactArguments(1)(inputParametersCount - 1) = paramArrayArg
            End If
        End If

        '(if there are more arguments than parameters and the last parameter is not a 
        'paramarray parameter the method should not be applicable)
        If Arguments.Count > InputParameters.Length Then
            If InputParameters.Length < 1 Then Return False
            If isLastParamArray = False Then Return False
        End If

        'Dim numberOfParamArrayElementParameters As Integer
        Dim firstNamedArgument As Integer = Arguments.Count + 1
        For i As Integer = 0 To Arguments.Count - 1
            'First, match each positional argument in order to the list of method parameters. 
            'If there are more positional arguments than parameters and the last parameter 
            'is not a paramarray, the method is not applicable. Otherwise, the paramarray parameter 
            'is expanded with parameters of the paramarray element type to match the number
            'of positional arguments. If a positional argument is omitted, the method is not applicable.
            If Arguments(i).IsNamedArgument Then
                firstNamedArgument = i
                Exit For '(No more positional arguments)
            End If

            If inputParametersCount - 1 < i Then
                '(more positional arguments than parameters)
                If isLastParamArray = False Then Return False '(last parameter is not a paramarray)

                'Add the additional expressions to the param array creation expression.
                Helper.Assert(paramArrayExpression.ArrayElementInitalizer.Initializers.Count = 1)
                For j As Integer = i To Arguments.Count - 1
                    'A paramarray element has to be specified.
                    If Arguments(j).Expression Is Nothing Then Return False
                    paramArrayExpression.ArrayElementInitalizer.AddInitializer(Arguments(j).Expression)
                Next
                Exit For
            Else
                matchedParameters.Add(InputParameters(i))

                'Get the default value of the parameter if the specified argument has no expression.
                Dim arg As Argument = Nothing
                If Arguments(i).Expression Is Nothing Then
                    If InputParameters(i).IsOptional = False Then
                        Helper.Assert(False)
                    Else
                        Dim exp As Expression
                        Dim pArg As New PositionalArgument(Parent)
                        exp = GetOptionalValueExpression(pArg, InputParameters(i))
                        pArg.Init(InputParameters(i).Position, exp)
                        arg = pArg
                    End If
                Else
                    arg = Arguments(i)
                End If

                exactArguments(0)(i) = arg
                If isLastParamArray AndAlso inputParametersCount - 1 = i Then
                    Helper.Assert(paramArrayExpression.ArrayElementInitalizer.Initializers.Count = 0)
                    paramArrayExpression.ArrayElementInitalizer.AddInitializer(arg.Expression)
                Else
                    exactArguments(1)(i) = arg
                End If
            End If
            '??? If a positional argument is omitted, the method is not applicable.
        Next


        For i As Integer = firstNamedArgument To Arguments.Count - 1
            Helper.Assert(Arguments(i).IsNamedArgument)

            'Next, match each named argument to a parameter with the given name. 
            'If one of the named arguments fails to match, matches a paramarray parameter, 
            'or matches an argument already matched with another positional or named argument,
            'the method is not applicable.

            Dim namedArgument As NamedArgument = DirectCast(Arguments(i), NamedArgument)

            Dim matched As Boolean = False
            For j As Integer = 0 To inputParametersCount - 1
                'Next, match each named argument to a parameter with the given name. 
                If NameResolution.CompareName(InputParameters(i).Name, namedArgument.Name) Then
                    If matchedParameters.Contains(InputParameters(i)) Then
                        'If one of the named arguments (...) matches an argument already matched with 
                        'another positional or named argument, the method is not applicable
                        Return False
                    ElseIf Helper.IsParamArrayParameter(Parent.Compiler, InputParameters(i)) Then
                        'If one of the named arguments (...) matches a paramarray parameter, 
                        '(...) the method is not applicable.
                        Return False
                    Else
                        matchedParameters.Add(InputParameters(i))
                        exactArguments(0)(j) = Arguments(i)
                        exactArguments(1)(j) = Arguments(i)
                        matched = True
                        Exit For
                    End If
                End If
            Next
            'If one of the named arguments fails to match (...) the method is not applicable
            If matched = False Then Return False
        Next

        'Next, if parameters that have not been matched are not optional, 
        'the method is not applicable. If optional parameters remain, the default value 
        'specified in the optional parameter declaration is matched to the parameter. 
        'If an Object parameter does not specify a default value, then the expression 
        'System.Reflection.Missing.Value is used. If an optional Integer parameter 
        'has the Microsoft.VisualBasic.CompilerServices.OptionCompareAttribute attribute, 
        'then the literal 1 is supplied for text comparisons and the literal 0 otherwise.

        For i As Integer = 0 To inputParametersCount - 1
            If matchedParameters.Contains(InputParameters(i)) = False Then
                'if parameters that have not been matched are not optional,the method is not applicable
                If InputParameters(i).IsOptional = False Then Return False

                Dim exp As Expression
                Dim arg As New PositionalArgument(Parent)
                exp = GetOptionalValueExpression(arg, InputParameters(i))
                arg.Init(InputParameters(i).Position, exp)
                exactArguments(0)(i) = arg
                If IsParamArrayParameter(Parent.Compiler, InputParameters(i)) = False Then
                    'he arraycreation has already been created and added to the exactArguments(1).
                    exactArguments(1)(i) = arg
                End If
            End If
        Next

        'Finally, if type arguments have been specified, they are matched against
        'the type parameter list. If the two lists do not have the same number of elements, 
        'the method is not applicable, unless the type argument list is empty. If the 
        'type argument list is empty, type inferencing is used to try and infer 
        'the type argument list. If type inferencing fails, the method is not applicable.
        'Otherwise, the type arguments are filled in the place of the 
        'type parameters in the signature.
        Dim genericTypeArgumentCount As Integer
        Dim genericTypeArguments As Type()
        If method IsNot Nothing AndAlso method.IsGenericMethod Then
            genericTypeArguments = method.GetGenericArguments()
            genericTypeArgumentCount = genericTypeArguments.Length
        ElseIf prop IsNot Nothing Then
            'property cannot be generic.
        End If

        If genericTypeArgumentCount > 0 AndAlso (TypeArguments Is Nothing OrElse TypeArguments.List.Count = 0) Then
            'If the Then type argument list is empty, type inferencing is used to try and infer 
            'the type argument list.
            Helper.NotImplementedYet("Type argument inference")
        ElseIf TypeArguments IsNot Nothing AndAlso TypeArguments.List.Count > 0 Then
            'If the two lists do not have the same number of elements, the method is not applicable
            If TypeArguments.List.Count <> genericTypeArgumentCount Then Return False

            Helper.NotImplemented("Type argument matching")
        End If

        outExactArguments = exactArguments

        Helper.AssertNotNothing(exactArguments(0))
        Helper.AssertNotNothing(exactArguments(1))
        Helper.Assert(exactArguments(0).Count = exactArguments(1).Count)

        Return True 'Method is applicable!!
    End Function

    Shared Function ArgumentsToExpressions(ByVal Arguments As Generic.List(Of Argument)) As Expression()
        Dim result(Arguments.Count - 1) As Expression

        For i As Integer = 0 To Arguments.Count - 1
            result(i) = Arguments(i).Expression
        Next

        Return result
    End Function

    Shared Function IsFirstMoreApplicable(ByVal Compiler As Compiler, ByVal Arguments As Generic.List(Of Argument), ByVal MTypes As Type(), ByVal NTypes() As Type) As Boolean
        Dim result As Boolean = True
        'A member M is considered more applicable than N if their signatures are different and, 
        'for each pair of parameters Mj and Nj that matches an argument Aj, 
        'one of the following conditions is true:
        '	Mj and Nj have identical types, or
        '	There exists a widening conversion from the type of Mj to the type Nj, or
        '	Aj is the literal 0, Mj is a numeric type and Nj is an enumerated type, or
        '	Mj is Byte and Nj is SByte, or
        '	Mj is Short and Nj is UShort, or
        '	Mj is Integer and Nj is UInteger, or 
        '	Mj is Long and Nj is ULong.

        'A member M is considered more applicable than N if their signatures are different 
        If Helper.CompareTypes(MTypes, NTypes) Then
            'Signatures are not different so none is more applicable
            Return False
        End If

        For i As Integer = 0 To Arguments.Count - 1
            Dim is1stMoreApplicable As Boolean
            Dim isEqual, isWidening, isLiteral0, isByte, isShort, isInteger, isLong As Boolean

            If MTypes.Length - 1 < i OrElse NTypes.Length - 1 < i Then Exit For

            '	Mj and Nj have identical types, or
            isEqual = Helper.CompareType(MTypes(i), NTypes(i))

            '	There exists a widening conversion from the type of Mj to the type Nj, or
            isWidening = Compiler.TypeResolution.IsImplicitlyConvertible(Compiler, MTypes(i), NTypes(i))

            '	Aj is the literal 0, Mj is a numeric type and Nj is an enumerated type, or
            isLiteral0 = IsLiteral0Expression(Compiler, Arguments(i).Expression) AndAlso Compiler.TypeResolution.IsNumericType(MTypes(i)) AndAlso Helper.IsEnum(NTypes(i))

            '	Mj is Byte and Nj is SByte, or
            isByte = Helper.CompareType(MTypes(i), Compiler.TypeCache.Byte) AndAlso Helper.CompareType(NTypes(i), Compiler.TypeCache.SByte)

            '	Mj is Short and Nj is UShort, or
            isShort = Helper.CompareType(MTypes(i), Compiler.TypeCache.Short) AndAlso Helper.CompareType(NTypes(i), Compiler.TypeCache.UShort)

            '	Mj is Integer and Nj is UInteger, or 
            isInteger = Helper.CompareType(MTypes(i), Compiler.TypeCache.Integer) AndAlso Helper.CompareType(NTypes(i), Compiler.TypeCache.UInteger)

            '	Mj is Long and Nj is ULong.
            isLong = Helper.CompareType(MTypes(i), Compiler.TypeCache.Long) AndAlso Helper.CompareType(NTypes(i), Compiler.TypeCache.ULong)

            is1stMoreApplicable = isEqual OrElse isWidening OrElse isLiteral0 OrElse isByte OrElse isShort OrElse isInteger OrElse isLong
            result = is1stMoreApplicable AndAlso result
        Next

        Return result
    End Function


    Shared Function IsLiteral0Expression(ByVal Compiler As Compiler, ByVal exp As Expression) As Boolean
        If exp Is Nothing Then Return False
        Dim litExp As LiteralExpression = TryCast(exp, LiteralExpression)
        If litExp Is Nothing Then Return False
        If litExp.ConstantValue Is Nothing Then Return False
        If Compiler.TypeResolution.IsIntegralType(litExp.ConstantValue.GetType) = False Then Return False
        If CDbl(litExp.ConstantValue) = 0.0 Then Return True
        Return False
    End Function

    Shared Function IsFirstLessGeneric() As Boolean
        'A member M is determined to be less generic than a member N using the following steps:
        '-	If M has fewer method type parameters than N, then M is less generic than N.
        '-	Otherwise, if for each pair of matching parameters Mj and Nj, Mj and Nj are equally generic with respect to type parameters on the method, or Mj is less generic with respect to type parameters on the method, and at least one Mj is less generic than Nj, then M is less generic than N.
        '-	Otherwise, if for each pair of matching parameters Mj and Nj, Mj and Nj are equally generic with respect to type parameters on the type, or Mj is less generic with respect to type parameters on the type, and at least one Mj is less generic than Nj, then M is less generic than N.
        Helper.NotImplemented()
    End Function

    Shared Function IsAccessible(ByVal Compiler As Compiler, ByVal Caller As TypeDeclaration, ByVal Method As MethodBase) As Boolean
        If Caller Is Nothing Then
            Return Helper.IsAccessible(Compiler, Method.Attributes, Method.DeclaringType)
        Else
            Return Helper.IsAccessible(Method.Attributes, Method.DeclaringType, Caller.TypeDescriptor)
        End If
    End Function

    Shared Function IsAccessible(ByVal Compiler As Compiler, ByVal Caller As TypeDeclaration, ByVal [Property] As PropertyInfo) As Boolean
        If Caller Is Nothing Then
            Return Helper.IsAccessible(Compiler, GetPropertyAccess([Property]), [Property].DeclaringType)
        Else
            Return Helper.IsAccessible(GetPropertyAccess([Property]), [Property].DeclaringType, Caller.TypeDescriptor)
        End If
    End Function

    Shared Function GetMethodAttributes(ByVal Member As MemberInfo) As MethodAttributes
        Select Case Member.MemberType
            Case MemberTypes.Method
                Return DirectCast(Member, MethodInfo).Attributes
            Case MemberTypes.Property
                Return GetPropertyAttributes(DirectCast(Member, PropertyInfo))
            Case Else
                Throw New InternalException("")
        End Select
    End Function

    Shared Function GetPropertyAttributes(ByVal [Property] As PropertyInfo) As MethodAttributes
        Dim result As MethodAttributes
        Dim getA, setA As MethodAttributes
        Dim getM, setM As MethodInfo

        getM = [Property].GetGetMethod(True)
        setM = [Property].GetSetMethod(True)

        Helper.Assert(getM IsNot Nothing OrElse setM IsNot Nothing)

        If getM IsNot Nothing Then
            getA = getM.Attributes
        End If

        If setM IsNot Nothing Then
            setA = setM.Attributes
        End If

        result = setA Or getA

        Dim visibility As MethodAttributes
        visibility = result And MethodAttributes.MemberAccessMask
        If visibility = MethodAttributes.MemberAccessMask Then
            visibility = MethodAttributes.Public
            result = (result And (Not MethodAttributes.MemberAccessMask)) Or visibility
        End If

        Return result
    End Function

    Shared Function GetEventAttributes(ByVal [Event] As EventInfo) As MethodAttributes
        Dim result As MethodAttributes
        Dim getA, setA, raiseA As MethodAttributes
        Dim getM, setM, raiseM As MethodInfo

        getM = [Event].GetAddMethod(True)
        setM = [Event].GetRemoveMethod(True)
        raiseM = [Event].GetRaiseMethod(True)

        Helper.Assert(getM IsNot Nothing OrElse setM IsNot Nothing OrElse raiseM IsNot Nothing)

        If getM IsNot Nothing Then
            getA = getM.Attributes
        End If

        If setM IsNot Nothing Then
            setA = setM.Attributes
        End If

        If raiseM IsNot Nothing Then
            raiseA = raiseM.Attributes
        End If

        result = setA Or getA Or raiseA

        Return result
    End Function

    Shared Function GetPropertyAccess(ByVal [Property] As PropertyInfo) As MethodAttributes
        Dim result As MethodAttributes

        result = GetPropertyAttributes([Property])
        result = result And MethodAttributes.MemberAccessMask

        Return result
    End Function

    Shared Function GetEventAccess(ByVal [Event] As EventInfo) As MethodAttributes
        Dim result As MethodAttributes

        result = GetEventAttributes([Event])
        result = result And MethodAttributes.MemberAccessMask

        Return result
    End Function

    Shared Function IsAccessible(ByVal Compiler As Compiler, ByVal Caller As TypeDeclaration, ByVal Member As MemberInfo) As Boolean
        Select Case Member.MemberType
            Case MemberTypes.Constructor, MemberTypes.Method
                Return IsAccessible(Compiler, Caller, DirectCast(Member, MethodBase))
            Case MemberTypes.Property
                Return IsAccessible(Compiler, Caller, DirectCast(Member, PropertyInfo))
            Case Else
                Throw New InternalException("")
        End Select
    End Function

    Overloads Shared Function GetParameters(ByVal Compiler As Compiler, ByVal Member As MemberInfo) As ParameterInfo()
        Select Case Member.MemberType
            Case MemberTypes.Method
                Return GetParameters(Compiler, DirectCast(Member, MethodInfo))
            Case MemberTypes.Property
                Return DirectCast(Member, PropertyInfo).GetIndexParameters
            Case MemberTypes.Constructor
                Return DirectCast(Member, ConstructorInfo).GetParameters
            Case Else
                Throw New InternalException("")
        End Select
    End Function

    Overloads Shared Function GetParameters(ByVal Compiler As Compiler, ByVal Members As Generic.IList(Of MemberInfo)) As ParameterInfo()()
        Dim result As ParameterInfo()()
        ReDim result(Members.Count - 1)
        For i As Integer = 0 To result.Length - 1
            result(i) = GetParameters(Compiler, Members(i))
        Next
        Return result
    End Function

    ''' <summary>
    ''' Gets the parameters of the specified constructor 
    ''' </summary>
    ''' <param name="constructor"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Overloads Shared Function GetParameters(ByVal Compiler As Compiler, ByVal constructor As ConstructorInfo) As ParameterInfo()
        If Helper.IsReflectionMember(constructor) Then
            Dim ctor As MemberInfo
            ctor = Compiler.TypeManager.GetRegisteredMember(constructor)
            Helper.Assert(Helper.IsReflectionMember(ctor) = False)
            Return DirectCast(ctor, ConstructorInfo).GetParameters
        Else
            Return constructor.GetParameters
        End If
    End Function

    Overloads Shared Function GetParameters(ByVal members As MemberInfo()) As ParameterInfo()
        Helper.NotImplemented() : Return Nothing
    End Function

    ''' <summary>
    ''' Gets the parameters of the specified method
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Overloads Shared Function GetParameters(ByVal Compiler As Compiler, ByVal method As MethodInfo) As ParameterInfo()
        If method.GetType.Name = "MethodBuilderInstantiation" Then
            Return method.GetGenericMethodDefinition.GetParameters
        ElseIf method.GetType.Name = "SymbolMethod" Then
            Return New ParameterInfo() {} 'Helper.NotImplemented()
        Else
            Return method.GetParameters
        End If
        If Compiler.theAss.IsDefinedHere(method.DeclaringType) Then
            Helper.NotImplemented() : Return Nothing
            'Return DirectCast(Compiler.theAss.FindBuildingType(method.DeclaringType), ContainerType).FindMethod(method).GetParameters()
        Else
            Return method.GetParameters
        End If
    End Function

    ''' <summary>
    ''' Gets the parameters of the specified method
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Overloads Shared Function GetParameters(ByVal Compiler As Compiler, ByVal method As MethodBase) As ParameterInfo()
        If TypeOf method Is MethodInfo Then
            Return GetParameters(Compiler, DirectCast(method, MethodInfo))
        ElseIf TypeOf method Is ConstructorInfo Then
            Return GetParameters(Compiler, DirectCast(method, ConstructorInfo))
        Else
            Helper.Stop()
            Throw New NotImplementedException
        End If
    End Function

    ''' <summary>
    ''' Returns -1 if it is narrowing, 0 if argument is convertible, 1 if paramargument is convertible
    ''' </summary>
    ''' <param name="Compiler"></param>
    ''' <param name="Argument"></param>
    ''' <param name="ParamArgument"></param>
    ''' <param name="Parameter"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Shared Function IsConvertible(ByVal Compiler As Compiler, ByVal Argument As Argument, ByVal ParamArgument As Argument, ByVal Parameter As ParameterInfo) As Integer
        Dim inputType As Type
        Dim argType, paramArgType As Type

        inputType = Parameter.ParameterType
        argType = Argument.Expression.ExpressionType

        Dim a, b As Boolean
        a = Compiler.TypeResolution.IsImplicitlyConvertible(Compiler, argType, inputType)

        paramArgType = Nothing
        If Argument IsNot ParamArgument Then
            Dim ace As ArrayCreationExpression
            Dim elementType As Type = inputType.GetElementType
            ace = TryCast(ParamArgument.Expression, ArrayCreationExpression)
            Helper.Assert(ace IsNot Nothing)
            b = True
            For Each init As VariableInitializer In ace.ArrayElementInitalizer.Initializers
                paramArgType = init.AsRegularInitializer.ExpressionType
                If Compiler.TypeResolution.IsImplicitlyConvertible(Compiler, paramArgType, elementType) = False Then
                    b = False
                    Exit For
                End If
            Next
        End If

        If a And b Then
            'If a single argument expression matches a paramarray parameter
            ' and the type of the argument expression is convertible to 
            'both the type of the paramarray parameter and the paramarray element type, 
            'the method is applicable in both its expanded and unexpanded forms, 
            'with two exceptions. If the conversion from the type of the argument expression
            ' to the paramarray type is narrowing, then the method is 
            'only applicable in its expanded form. If the argument expression is 
            'the null literal Nothing, then the method is only applicable in its unexpanded form. For example:
            If TypeOf Argument.Expression Is NothingConstantExpression Then
                Return 0
            ElseIf Argument.Expression.IsConstant AndAlso Argument.Expression.ConstantValue Is Nothing Then
                Return 0
            ElseIf Helper.CompareType(argType, paramArgType) Then
                Return 0
            Else
                Helper.NotImplemented()
                Return -1
            End If
        ElseIf a Then
            Return 0
        ElseIf b Then
            Return 1
        Else
            Return -1
        End If
    End Function

    Private Shared Function GetExpandedTypes(ByVal Compiler As Compiler, ByVal Parameters() As ParameterInfo, ByVal ArgumentCount As Integer) As Type()
        Dim result(ArgumentCount - 1) As Type
        Dim lastParameter As Integer = Parameters.Length - 1

        Dim elementType As Type = Nothing
        For i As Integer = 0 To ArgumentCount - 1
            If i > lastParameter Then
                result(i) = elementType
            ElseIf i = lastParameter Then
                If Helper.IsParamArrayParameter(Compiler, Parameters(lastParameter)) Then
                    elementType = Parameters(lastParameter).ParameterType.GetElementType
                    result(i) = elementType
                Else
                    result(i) = Parameters(i).ParameterType
                End If
            Else
                result(i) = Parameters(i).ParameterType
            End If
        Next

        Helper.AssertNotNothing(result)

        Return result
    End Function

    Shared Function ResolveGroup(ByVal Parent As ParsedObject, ByVal InputGroup As Generic.List(Of MemberInfo), ByVal ResolvedGroup As Generic.List(Of MemberInfo), ByVal Arguments As ArgumentList, ByVal TypeArguments As TypeArgumentList, ByRef OutputArguments As Generic.List(Of Argument)) As Boolean
        Dim result As Boolean = True
        Dim Compiler As Compiler = Parent.Compiler
        Dim Caller As TypeDeclaration = Parent.FindTypeParent

        Helper.Assert(InputGroup.Count > 0)
        Helper.Assert(ResolvedGroup.Count = 0)

#If DEBUG Then
        ResolvedGroup.Clear()
#End If
        'All the candidates are added to the resolved group and then they are removed
        ResolvedGroup.AddRange(InputGroup)

        Dim methodsLeft As Integer = ResolvedGroup.Count
        Dim methodName As String = InputGroup(0).Name
        Dim inputParameters()() As ParameterInfo = Helper.GetParameters(Compiler, InputGroup)
        Dim inputTypes()() As Type = Helper.GetTypes(inputParameters)
        'Dim matchedArguments(ResolvedGroup.Count - 1) As Generic.List(Of Argument)
        Dim exactArguments(ResolvedGroup.Count - 1)() As Generic.List(Of Argument)
        Dim exactArguments2(ResolvedGroup.Count - 1) As Generic.List(Of Argument)
        'Dim matchedArgumentsTypes(ResolvedGroup.Count - 1)() As Type
        'Dim expandedArgumentTypes(ResolvedGroup.Count - 1) As Generic.List(Of Type)
        Dim codedArgumentsString As String = "(" & Arguments.AsString & ")"
        Dim completeMethodName As String = methodName & codedArgumentsString

#If DEBUGMETHODRESOLUTION Then
        Dim msg2 As String
        msg2 = "Resolving: " & methodName
        If Parent.HasLocation Then msg2 &= " (" & Parent.Location.ToString & ")"
        LogResolutionMessage(Compiler, msg2)
#End If

        'Remove methods that aren't accesible 
        For i As Integer = ResolvedGroup.Count - 1 To 0 Step -1
            If IsAccessible(Compiler, Caller, ResolvedGroup(i)) = False Then
                LogResolutionMessage(Compiler, String.Format("NOT ACCESSIBLE: Method call to '{0}{1}' with arguments '{2}'", methodName, Helper.ToString(inputTypes(i)), codedArgumentsString))
                ResolvedGroup(i) = Nothing : methodsLeft -= 1
            Else
                LogResolutionMessage(Compiler, String.Format("ACCESSIBLE    : Method call to '{0}{1}' with arguments '{2}'", methodName, Helper.ToString(inputTypes(i)), codedArgumentsString))
            End If
        Next
        LogResolutionMessage(Compiler, String.Format("Found {0} accessible candidates.", methodsLeft.ToString))

        'Remove methods that aren't applicable
        For i As Integer = ResolvedGroup.Count - 1 To 0 Step -1
            If ResolvedGroup(i) Is Nothing Then Continue For
            'matchedArguments(i) = New Generic.List(Of Argument)
            'expandedArgumentTypes(i) = New Generic.List(Of Type)
            Dim exactArgs() As Generic.List(Of Argument) = Nothing
            If IsApplicable(Parent, ResolvedGroup(i), inputParameters(i), Arguments, TypeArguments, exactArgs) = False Then
                LogResolutionMessage(Compiler, String.Format("NOT APPLICABLE: Method call to '{0}{1}' with arguments '{2}'", methodName, Helper.ToString(inputTypes(i)), codedArgumentsString))
                ResolvedGroup(i) = Nothing : methodsLeft -= 1
            Else
                LogResolutionMessage(Compiler, String.Format("APPLICABLE    : Method call to '{0}{1}'  with arguments '{2}'", InputGroup(i).DeclaringType.FullName & ":" & methodName, Helper.ToString(inputTypes(i)), codedArgumentsString))
                exactArguments(i) = exactArgs
            End If
        Next
        LogResolutionMessage(Compiler, String.Format("Found {0} applicable candidates.", methodsLeft.ToString))

        If methodsLeft <= 1 Then
            GoTo Done
        End If

        'Eliminate all members from the set that require narrowing coercions to be applicable 
        'to the argument list, except for the case where the argument expression type is Object. 
        'If the set is empty, a compile-time error results. 
        'If only one member remains in the set, that is the applicable member.
        For i As Integer = 0 To ResolvedGroup.Count - 1
            If ResolvedGroup(i) Is Nothing Then Continue For

            For j As Integer = 0 To inputParameters(i).Length - 1
                Dim arg, paramArg As Argument
                Dim param As ParameterInfo

                param = inputParameters(i)(j)
                arg = exactArguments(i)(0)(j)
                paramArg = exactArguments(i)(1)(j)

                If Helper.CompareType(arg.Expression.ExpressionType, Compiler.TypeCache.Object) Then Exit For

                Dim IsConvertible As Integer
                IsConvertible = Helper.IsConvertible(Compiler, arg, paramArg, param)

                If IsConvertible = -1 Then
                    LogResolutionMessage(Compiler, String.Format("NOT CONVERTIBLE: Method call to '{0}{1}' with arguments '{2}'", methodName, Helper.ToString(inputTypes(i)), codedArgumentsString))
                    ResolvedGroup(i) = Nothing : methodsLeft -= 1
                    Exit For
                Else
                    exactArguments2(i) = exactArguments(i)(IsConvertible)
                End If
            Next
        Next
        LogResolutionMessage(Compiler, String.Format("Found {0} non-narrowing candidates (1).", methodsLeft.ToString))

        If methodsLeft <= 1 Then GoTo Done

        'Eliminate all remaining members from the set that require narrowing coercions 
        'to be applicable to the argument list. 
        For i As Integer = 0 To ResolvedGroup.Count - 1
            If ResolvedGroup(i) Is Nothing Then Continue For

            For j As Integer = 0 To inputParameters(i).Length - 1
                Dim arg, paramArg As Argument
                Dim param As ParameterInfo

                param = inputParameters(i)(j)
                arg = exactArguments(i)(0)(j)
                paramArg = exactArguments(i)(1)(j)

                Dim IsConvertible As Integer
                IsConvertible = Helper.IsConvertible(Compiler, arg, paramArg, param)

                If IsConvertible = -1 Then
                    LogResolutionMessage(Compiler, String.Format("NOT CONVERTIBLE: Method call to '{0}{1}' with arguments '{2}'", methodName, Helper.ToString(inputTypes(i)), codedArgumentsString))
                    ResolvedGroup(i) = Nothing : methodsLeft -= 1
                    Exit For
                Else
                    exactArguments2(i) = exactArguments(i)(IsConvertible)
                End If
            Next

            If inputParameters(i).Length = 0 Then
                exactArguments2(i) = exactArguments(i)(0)
            End If
        Next
        LogResolutionMessage(Compiler, String.Format("Found {0} non-narrowing candidates (2).", methodsLeft.ToString))

        If methodsLeft = 0 Then
            'If the set is empty, 
            'the type containing the method group is not an interface, and strict semantics are not being 
            'used, the invocation target expression is reclassified as a late-bound method access. 
            'If strict semantics are being used or the method group is contained in an interface 
            'and the set is empty, a compile-time error results.
            Helper.NotImplementedYet("Reclassify to late-bound method access.")
        End If

        If methodsLeft <= 1 Then GoTo Done

        'Find most applicable methods.
        Dim expandedArgumentTypes(ResolvedGroup.Count - 1)() As Type

        For i As Integer = 0 To ResolvedGroup.Count - 1
            If ResolvedGroup(i) Is Nothing Then Continue For
            Helper.Assert(exactArguments2(i) IsNot Nothing)

            For j As Integer = i + 1 To ResolvedGroup.Count - 1
                If ResolvedGroup(j) Is Nothing Then Continue For
                Helper.Assert(exactArguments2(j) IsNot Nothing)

                Dim a, b As Boolean

                If expandedArgumentTypes(i) Is Nothing Then
                    expandedArgumentTypes(i) = GetExpandedTypes(Compiler, inputParameters(i), Arguments.Count)
                End If
                If expandedArgumentTypes(j) Is Nothing Then
                    expandedArgumentTypes(j) = GetExpandedTypes(Compiler, inputParameters(j), Arguments.Count)
                End If

                a = IsFirstMoreApplicable(Compiler, exactArguments2(i), expandedArgumentTypes(i), expandedArgumentTypes(j))
                b = IsFirstMoreApplicable(Compiler, exactArguments2(i), expandedArgumentTypes(j), expandedArgumentTypes(i))

                If a = False AndAlso b = False Then
                    'It is possible for M and N to have the same signature if one or both contains an expanded 
                    'paramarray parameter. In that case, the member with the fewest number of arguments matching
                    'expanded paramarray parameters is considered more applicable. 
                    Dim iParamArgs, jParamArgs As Integer
                    Dim tmp As ParameterInfo()

                    tmp = inputParameters(i)
                    If tmp.Length > 0 AndAlso Helper.IsParamArrayParameter(Compiler, tmp(tmp.Length - 1)) Then
                        Dim exp As ArrayCreationExpression
                        exp = TryCast(exactArguments(i)(1)(tmp.Length - 1).Expression, ArrayCreationExpression)
                        Helper.Assert(exp IsNot Nothing)
                        iParamArgs = exp.ArrayElementInitalizer.Initializers.Count + 1
                    End If
                    tmp = inputParameters(j)
                    If tmp.Length > 0 AndAlso Helper.IsParamArrayParameter(Compiler, tmp(tmp.Length - 1)) Then
                        Dim exp As ArrayCreationExpression
                        exp = TryCast(exactArguments(j)(1)(tmp.Length - 1).Expression, ArrayCreationExpression)
                        Helper.Assert(exp IsNot Nothing)
                        jParamArgs = exp.ArrayElementInitalizer.Initializers.Count + 1
                    End If
                    If jParamArgs > iParamArgs Then
                        a = True
                    ElseIf iParamArgs > jParamArgs Then
                        b = True
                    End If
                    Helper.Assert(iParamArgs <> jParamArgs OrElse (iParamArgs = 0 AndAlso jParamArgs = 0), InputGroup(0).Name)
                End If

                If a Xor b Then
                    If a = False Then
                        LogResolutionMessage(Compiler, String.Format("NOT MOST APPLICABLE: Method call to '{0}{1}' with arguments '{2}'", methodName, Helper.ToString(inputTypes(i)), codedArgumentsString))
                        ResolvedGroup(i) = Nothing : methodsLeft -= 1
                        Exit For
                    Else
                        LogResolutionMessage(Compiler, String.Format("NOT MOST APPLICABLE: Method call to '{0}{1}' with arguments '{2}'", methodName, Helper.ToString(inputTypes(j)), codedArgumentsString))
                        ResolvedGroup(j) = Nothing : methodsLeft -= 1
                    End If
                Else
                    LogResolutionMessage(Compiler, String.Format("EQUALLY APPLICABLE: Method call to '{0}{1}' with arguments '{2}' and with arguments '{3}'", methodName, codedArgumentsString, Helper.ToString(inputTypes(i)), Helper.ToString(inputTypes(j))))
                End If
            Next
        Next

        If methodsLeft > 1 Then
            'Remove methods from base classes with the same signature (if they are virtual or shadows).
            Helper.NotImplementedYet("Remove methods from base classes with the same signature (if they are virtual or shadows). SHOULD NOT BE NECESSARY")
            For i As Integer = 0 To ResolvedGroup.Count - 2
                For j As Integer = i + 1 To ResolvedGroup.Count - 1
                    Dim m1, m2 As MethodInfo
                    m1 = TryCast(ResolvedGroup(i), MethodInfo)
                    m2 = TryCast(ResolvedGroup(j), MethodInfo)
                    If Not (m1 IsNot Nothing AndAlso m2 IsNot Nothing) Then Continue For
                    If Not ((m1.IsVirtual AndAlso m2.IsVirtual) OrElse (m1.IsHideBySig OrElse m2.IsHideBySig)) Then Continue For
                    If Helper.CompareType(m1.DeclaringType, m2.DeclaringType) Then Continue For
                    If Helper.CompareTypes(inputTypes(i), inputTypes(j)) = False Then Continue For

                    If Helper.IsAssignable(Compiler, m1.DeclaringType, m2.DeclaringType) Then
                        If m1.Attributes = MethodAttributes.NewSlot Then Continue For
                        ResolvedGroup(j) = Nothing : methodsLeft -= 1
                    ElseIf Helper.IsAssignable(Compiler, m2.DeclaringType, m1.DeclaringType) Then
                        If m2.Attributes = MethodAttributes.NewSlot Then Continue For
                        ResolvedGroup(i) = Nothing : methodsLeft -= 1
                    Else
                        Helper.NotImplemented()
                    End If
                Next
            Next
        End If

        If methodsLeft <= 1 Then GoTo Done

Done:
        For i As Integer = ResolvedGroup.Count - 1 To 0 Step -1
            If ResolvedGroup(i) Is Nothing Then ResolvedGroup.RemoveAt(i)
        Next
        Helper.Assert(methodsLeft = ResolvedGroup.Count)

        result = ResolvedGroup.Count = 1

        If result Then
            For i As Integer = 0 To InputGroup.Count - 1
                If InputGroup(i) Is ResolvedGroup(0) Then
                    If exactArguments2(i) Is Nothing Then
                        Dim params() As ParameterInfo
                        params = inputParameters(i)
                        Dim useParamArray As Boolean

                        If params.Length > 0 Then
                            Dim param As ParameterInfo = params(params.Length - 1)
                            If Helper.IsParamArrayParameter(Compiler, param) Then
                                If Arguments.Count <> params.Length Then
                                    useParamArray = True
                                ElseIf TypeOf Arguments.Arguments(params.Length - 1).Expression Is NothingConstantExpression = False Then
                                    Dim convertible As Integer
                                    Dim lastIndex As Integer = params.Length - 1
                                    convertible = Helper.IsConvertible(Compiler, exactArguments(i)(0)(lastIndex), exactArguments(i)(1)(lastIndex), param)
                                    If convertible = 1 Then
                                        useParamArray = True
                                    End If
                                End If
                            End If
                        End If

                        If useParamArray Then
                            OutputArguments = exactArguments(i)(1)
                        Else
                            OutputArguments = exactArguments(i)(0)
                        End If
                    Else
                        OutputArguments = exactArguments2(i)
                    End If

                    If OutputArguments.Count > 0 Then
                        Dim ace As ArrayCreationExpression
                        ace = TryCast(OutputArguments.Item(OutputArguments.Count - 1).Expression, ArrayCreationExpression)
                        If ace IsNot Nothing AndAlso ace.IsResolved = False AndAlso IsParamArrayParameter(Compiler, inputParameters(i)(inputParameters(i).Length - 1)) Then
                            If ace.ResolveExpression(ResolveInfo.Default(Compiler)) = False Then
                                Helper.ErrorRecoveryNotImplemented()
                            End If
                        End If
                    End If

                    Exit For
                End If
            Next
        End If


#If DEBUGMETHODRESOLUTION Then
        Dim msg As String
        If ResolvedGroup.Count = 1 Then
            msg = String.Format("Method call to '{0}' resolved: ", completeMethodName)
            msg = msg & " to "
            msg = msg & Helper.ToString(Compiler, GetParameters(Parent.Compiler, ResolvedGroup(0))) & VB.vbNewLine
        Else
            msg = String.Format("Method call to '{0}' NOT resolved ({1} methods were found).", completeMethodName, methodsLeft.ToString) & VB.vbNewLine
        End If
        LogResolutionMessage(Compiler, msg)
#End If
        Return result
    End Function

    ''' <summary>
    ''' Adds all the members to the derived class members, unless they are shadowed or overridden
    ''' </summary>
    ''' <param name="DerivedClassMembers"></param>
    ''' <param name="BaseClassMembers"></param>
    ''' <remarks></remarks>
    Shared Sub AddMembers(ByVal Compiler As Compiler, ByVal Type As Type, ByVal DerivedClassMembers As Generic.List(Of MemberInfo), ByVal BaseClassMembers As MemberInfo())
        Dim shadowed As New Generic.List(Of String)
        Dim overridden As New Generic.List(Of String)

        If BaseClassMembers.Length = 0 Then Return

        Helper.Assert(Type IsNot Nothing)
        Dim logging As Boolean

        If Type.BaseType IsNot Nothing Then
            logging = False 'Type.BaseType.Name = "Form"
        End If

        LogAddMessage(Compiler, "", logging)

        If Type.BaseType IsNot Nothing Then
            LogAddMessage(Compiler, String.Format("Adding members to type '{0}' from its base type '{1}'", Type.Name, Type.BaseType.Name), logging)
        Else
            LogAddMessage(Compiler, String.Format("Adding members to type '{0}' from its unknown base type", Type.Name), logging)
        End If

        For Each member As MemberInfo In DerivedClassMembers
            Select Case member.MemberType
                Case MemberTypes.Constructor
                    'Constructors are not added.
                Case MemberTypes.Event
                    'Events can only be shadows
                    shadowed.Add(member.Name)
                Case MemberTypes.Field
                    shadowed.Add(member.Name)
                Case MemberTypes.Method
                    Dim mInfo As MethodInfo = DirectCast(member, MethodInfo)
                    If mInfo.IsHideBySig Then
                        overridden.AddRange(GetOverloadableSignatures(Compiler, mInfo))
                    Else
                        shadowed.Add(mInfo.Name)
                    End If
                Case MemberTypes.NestedType
                    shadowed.Add(member.Name)
                Case MemberTypes.Property
                    Dim pInfo As PropertyInfo = DirectCast(member, PropertyInfo)
                    If CBool(Helper.GetPropertyAttributes(pInfo) And MethodAttributes.HideBySig) Then
                        overridden.AddRange(GetOverloadableSignatures(Compiler, pInfo))
                    Else
                        shadowed.Add(pInfo.Name)
                    End If
                Case MemberTypes.TypeInfo
                    shadowed.Add(member.Name)
                Case Else
                    Throw New InternalException("")
            End Select
        Next

        For i As Integer = 0 To shadowed.Count - 1
            LogAddMessage(Compiler, "Shadows:    " & shadowed(i), logging)
            shadowed(i) = shadowed(i).ToLowerInvariant
        Next
        For i As Integer = 0 To overridden.Count - 1
            LogAddMessage(Compiler, "Overridden: " & overridden(i), logging)
            overridden(i) = overridden(i).ToLowerInvariant
        Next

        For Each member As MemberInfo In BaseClassMembers
            Dim name As String = member.Name.ToLowerInvariant

            If shadowed.Contains(name) Then
                LogAddMessage(Compiler, "Discarded (shadowed): " & name, logging)
                Continue For
            End If


            Select Case member.MemberType
                Case MemberTypes.Constructor
                    LogAddMessage(Compiler, "Discarded (constructor): " & name, logging)
                    Continue For 'Constructors are not added
                Case MemberTypes.Method, MemberTypes.Property
                    Dim signatures As String()
                    Dim found As Boolean

                    If IsAccessibleExternal(Compiler, member) = False Then
                        LogAddMessage(Compiler, "Discarted (not accessible): " & name, logging)
                        Continue For
                    End If

                    found = False
                    signatures = GetOverloadableSignatures(Compiler, member)
                    For Each signature As String In signatures
                        name = signature.ToLowerInvariant
                        If overridden.Contains(name) Then
                            found = True
                            Exit For
                        End If
                    Next
                    If found = True Then
                        LogAddMessage(Compiler, "Discarded (overridden, " & member.MemberType.ToString() & "): " & name, logging)
                        Continue For
                    End If
                Case MemberTypes.Event, MemberTypes.Field, MemberTypes.NestedType, MemberTypes.TypeInfo
                    If IsAccessibleExternal(Compiler, member) = False Then
                        LogAddMessage(Compiler, "Discarted (not accessible): " & name, logging)
                        Continue For
                    End If
                Case Else
                    Throw New InternalException("")
            End Select

            'Not shadowed nor overriden
            LogAddMessage(Compiler, "Added (" & member.MemberType.ToString & "): " & name, logging)
            DerivedClassMembers.Add(member)
        Next

        LogAddMessage(Compiler, "", logging)
    End Sub

    Shared Function IsHideBySig(ByVal Member As MemberInfo) As Boolean
        Select Case Member.MemberType
            Case MemberTypes.Constructor
                Return False
            Case MemberTypes.Event, MemberTypes.Field, MemberTypes.NestedType, MemberTypes.TypeInfo
                Return False
            Case MemberTypes.Property
                Dim pInfo As PropertyInfo = DirectCast(Member, PropertyInfo)
                Return CBool(GetPropertyAttributes(pInfo) And MethodAttributes.HideBySig)
            Case MemberTypes.Method
                Dim mInfo As MethodInfo = DirectCast(Member, MethodInfo)
                Return mInfo.IsHideBySig
            Case Else
                Throw New InternalException("")
        End Select
    End Function

    Shared Sub RemoveShadowed(ByVal Compiler As Compiler, ByVal Members As Generic.List(Of MemberInfo))
        Dim hash As New Generic.Dictionary(Of String, MemberInfo)
        Dim shadowableNames As New Generic.List(Of String)
        For i As Integer = Members.Count - 1 To 0 Step -1
            Dim member As MemberInfo = Members(i)
            Dim hashedMember As MemberInfo = hash(member.Name)
            Dim hashedMemberType, memberType As Type

            If hashedMember Is Nothing Then
                hash.Add(member.Name, member)
                Select Case member.MemberType
                    Case MemberTypes.Property, MemberTypes.Method
                        If IsHideBySig(member) Then
                            For Each sig As String In Helper.GetOverloadableSignatures(Compiler, member)
                                hash.Add(sig, member)
                            Next
                        End If
                End Select
                Continue For
            End If

            hashedMemberType = hashedMember.DeclaringType
            memberType = member.DeclaringType

            If Helper.CompareType(hashedMemberType, memberType) Then Continue For
            If hashedMemberType.IsInterface Xor memberType.IsInterface Then Continue For

            Dim mostDerivedWins As Boolean
            Dim isHashedMostDerived As Boolean
            Dim isMemberMostDerived As Boolean

            Dim removeIndex As Integer = -1
            If Helper.IsSubclassOf(hashedMemberType, memberType) Then
                isHashedMostDerived = True
                For j As Integer = Members.Count - 1 To 0 Step -1
                    If Members(j) Is hashedMember Then
                        removeIndex = j
                        Exit For
                    End If
                Next
            ElseIf Helper.IsSubclassOf(memberType, hashedMemberType) Then
                isMemberMostDerived = True
                removeIndex = i
            End If

            If member.MemberType <> hashedMember.MemberType Then
                mostDerivedWins = True
            Else
                Select Case member.MemberType
                    Case MemberTypes.Constructor
                        Continue For
                    Case MemberTypes.Event, MemberTypes.Field, MemberTypes.NestedType, MemberTypes.TypeInfo
                        mostDerivedWins = True
                    Case MemberTypes.Property, MemberTypes.Method
                        If isMemberMostDerived Then
                            If IsHideBySig(member) Then

                            Else
                                mostDerivedWins = True
                            End If
                        ElseIf isHashedMostDerived Then
                            If IsHideBySig(hashedMember) Then

                            Else
                                mostDerivedWins = True
                            End If
                        Else
                            Helper.NotImplemented()
                        End If
                    Case Else
                        Throw New InternalException("")
                End Select
            End If


            If mostDerivedWins Then
                'the most derived wins
                If removeIndex >= 0 Then
                    Members.RemoveAt(removeIndex)
                    Continue For
                End If

                Helper.NotImplementedYet("Is this shadowed?")
            End If
        Next
    End Sub

    Shared Function GetOverloadableSignatures(ByVal Compiler As Compiler, ByVal Member As MemberInfo) As String()
        Dim result As New Generic.List(Of String)
        Dim params() As ParameterInfo
        Dim types() As Type
        Dim sep As String = ""

        params = Helper.GetParameters(Compiler, Member)
        types = Helper.GetTypes(params)

        Dim signature As String = ""
        For i As Integer = 0 To types.Length - 1
            If types(i).IsByRef Then types(i) = types(i).GetElementType
            If params(i).IsOptional Then
                result.Add(Member.Name & "(" & signature & ")")
            End If
            signature &= sep & types(i).Namespace & "." & types(i).Name
            sep = ", "
        Next

        result.Add(Member.Name & "(" & signature & ")")

        Return result.ToArray
    End Function

End Class