#If Not BREAK_BUILD Then
Imports NUnit.Framework
Imports Xunit
#End If

<TestFixture()>
Public Class AppSettingTests

    Private mAppSetting As AppSetting
    Private idval As String = "test234567"
    Private nameval As String = "test"

    <SetUp()>
    Public Sub SetUp()
        mAppSetting = New AppSetting(idval, nameval)
    End Sub

    <TearDown()>
    Public Sub TearDown()

        'nothing to do here
    End Sub

    <Test()>
    Public Sub id()
        NUnit.Framework.Assert.AreSame(mAppSetting.id, idval, "ID value loaded was " & mAppSetting.id & " and not the expected value of : " & idval)
    End Sub

    <Test()>
    Public Sub name()
#If Not BREAK_TEST Then
        NUnit.Framework.Assert.AreSame(mAppSetting.name, nameval)
#Else
        NUnit.Framework.Assert.AreSame(mAppSetting.name + "?", nameval)
#End If
    End Sub

    <Fact()>
    Public Sub name2()
#If Not BREAK_TEST Then
        Xunit.Assert.Equal(New AppSetting(idval, nameval).name, nameval)
#Else
        Xunit.Assert.Equal(New AppSetting(idval, nameval).name, nameval + "?")
#End If
    End Sub
End Class
