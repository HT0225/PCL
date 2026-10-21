Imports System.Runtime.InteropServices
Imports System.Security.Principal

Public Class PageOtherTest

#Region "初始化"

    Private IsLoad As Boolean
    Private Shared IsMemoryOptimizeRunning As Boolean = False
    Private IsCaveEnabled As Boolean = True

    Private Sub PageOtherTest_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If IsLoad Then Return
        IsLoad = True
        Try
            TextDownloadFolder.Text = If(Settings.Get(Of String)("CacheDownloadFolder"), "")
            TextDownloadFolder.Validate()
            If Not TextDownloadFolder.IsValidated OrElse String.IsNullOrEmpty(TextDownloadFolder.Text) Then
                TextDownloadFolder.Text = Paths.Base & "PCL\MyDownload\"
            End If
            TextDownloadFolder.Validate()
            TextDownloadName.Validate()
            StartButtonRefresh()
            CaveHand()
        Catch ex As Exception
            Logger.Warn(ex, "初始化百宝箱失败")
        End Try
    End Sub

#End Region

#Region "自定义下载"

    Private Sub SaveCacheDownloadFolder()
        Try
            Settings.Set("CacheDownloadFolder", TextDownloadFolder.Text)
            Dim Rule = TextDownloadName.ValidateRules.FirstOrDefault()
            If TypeOf Rule Is ValidateFileName Then DirectCast(Rule, ValidateFileName).ParentFolder = TextDownloadFolder.Text
            TextDownloadName.Validate()
        Catch ex As Exception
            Logger.Warn(ex, "保存自定义下载设置失败")
        End Try
    End Sub

    Private Sub StartButtonRefresh()
        BtnDownloadStart.IsEnabled = TextDownloadFolder.IsValidated AndAlso TextDownloadUrl.IsValidated AndAlso TextDownloadName.IsValidated
        BtnDownloadOpen.IsEnabled = TextDownloadFolder.IsValidated
    End Sub

    Private Sub TextDownloadUrl_TextChanged(sender As Object, e As TextChangedEventArgs) Handles TextDownloadUrl.TextChanged
        Try
            If String.IsNullOrEmpty(TextDownloadName.Text) AndAlso Not String.IsNullOrEmpty(TextDownloadUrl.Text) Then
                TextDownloadName.Text = PathUtils.GetLastPart(WebUtility.UrlDecode(TextDownloadUrl.Text))
            End If
        Catch ex As Exception
            'URL 尚未输入完整时忽略
        End Try
        StartButtonRefresh()
    End Sub

    Private Sub TextDownloadUrl_KeyDown(sender As Object, e As KeyEventArgs) Handles TextDownloadUrl.KeyDown
        If e.Key = Key.Enter AndAlso BtnDownloadStart.IsEnabled Then BtnDownloadStart_Click(sender, e)
    End Sub

    Private Sub TextDownloadFolder_TextChanged(sender As Object, e As TextChangedEventArgs) Handles TextDownloadFolder.TextChanged
        SaveCacheDownloadFolder()
        StartButtonRefresh()
    End Sub

    Private Sub TextDownloadName_TextChanged(sender As Object, e As TextChangedEventArgs) Handles TextDownloadName.TextChanged
        StartButtonRefresh()
    End Sub

    Private Sub BtnDownloadStart_Click(sender As Object, e As EventArgs) Handles BtnDownloadStart.Click
        StartCustomDownload(TextDownloadUrl.Text, TextDownloadName.Text, TextDownloadFolder.Text)
        TextDownloadUrl.Text = ""
        TextDownloadUrl.Validate()
        TextDownloadUrl.ForceShowAsSuccess()
        TextDownloadName.Text = ""
        TextDownloadName.Validate()
        TextDownloadName.ForceShowAsSuccess()
        StartButtonRefresh()
    End Sub

    Public Shared Sub StartCustomDownload(Url As String, FileName As String, Optional Folder As String = Nothing)
        Try
            If String.IsNullOrWhiteSpace(Folder) Then
                Dim Target As String = Dialogs.SaveFile("选择文件保存位置", FileName)
                If String.IsNullOrEmpty(Target) Then Return
                FileName = PathUtils.GetLastPart(Target)
                Folder = PathUtils.RemoveLastPart(Target)
            End If
            Folder = PathUtils.AddSlashSuffix(Folder.Replace("/", "\"))
            Try
                DirectoryUtils.Create(Folder)
                CheckPermissionWithException(Folder)
            Catch ex As Exception
                Logger.Error(ex, $"访问文件夹失败（{Folder}）", LogBehavior.Toast)
                Return
            End Try
            Dim Uuid As Integer = GetUuid()
            Dim Download As New LoaderDownload("自定义下载文件：" & FileName & " ",
                New NetFile() {New NetFile({Url}, Folder & FileName, Nothing, SimulateBrowserHeaders:=True)})
            Dim Combo As New LoaderCombo(Of Integer)("自定义下载 (" & Uuid & ") ", New LoaderBase() {Download})
            Combo.OnStateChanged = Sub(Loader As LoaderBase) DownloadState(DirectCast(Loader, LoaderCombo(Of Integer)))
            Combo.Start()
            LoaderTaskbarAdd(Combo)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.BtnExtraDownload.Ribble()
        Catch ex As Exception
            Logger.Error(ex, "开始自定义下载失败")
        End Try
    End Sub

    Private Sub BtnDownloadSelect_Click(sender As Object, e As EventArgs) Handles BtnDownloadSelect.Click
        Dim Folder As String = Dialogs.SelectFolder("选择目标文件夹", False).FirstOrDefault()
        If Folder IsNot Nothing Then TextDownloadFolder.Text = Folder
    End Sub

    Private Sub BtnDownloadOpen_Click(sender As Object, e As EventArgs) Handles BtnDownloadOpen.Click
        Try
            DirectoryUtils.Create(TextDownloadFolder.Text)
            StartProcess(TextDownloadFolder.Text)
        Catch ex As Exception
            Logger.Warn(ex, "打开下载文件夹失败")
        End Try
    End Sub

    Private Shared Sub DownloadState(Loader As LoaderCombo(Of Integer))
        Select Case Loader.State
            Case LoadState.Finished
                Hint(Loader.Name & "完成！", HintType.Green)
                Interaction.Beep()
            Case LoadState.Failed
                Logger.Error(Loader.Error, Loader.Name & "失败", LogBehavior.Toast)
                Interaction.Beep()
            Case LoadState.Canceled
                Hint(Loader.Name & "已取消！")
        End Select
    End Sub

#End Region

#Region "今日人品"

    Private Sub BtnJrrp_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles BtnJrrp.MouseLeftButtonUp
        Jrrp()
    End Sub

    Public Shared Sub Jrrp()
        Dim DayOfYear As Integer = DateTime.Now.DayOfYear
        Dim Year As Integer = DateTime.Now.Year
        Dim Day As Integer = DateTime.Now.Day
        Dim Hash1 As Double = CDbl(("asdfgbn" & DayOfYear & "12#3$45" & Year & "IUY").GetStableHashCode())
        Dim Hash2 As Double = CDbl(("QWERTY" & Identify & "0*8&6" & Day & "kjhg").GetStableHashCode())
        Dim Value As Integer = CInt(Math.Round(Math.Abs((Hash1 / 3.0 + Hash2 / 3.0) / 527.0) Mod 1001.0))
        Dim Score As Integer = If(Value < 970, CInt(Math.Round(Value / 969.0 * 99.0)), 100)
        Dim Text As String
        If Score = 100 Then
            Text = "！100！100！！！！！" & If(ThemeUnlock(13, False), vbCrLf & "隐藏主题 欧皇彩 已解锁！", "")
        ElseIf Score >= 98 Then
            Text = "！差点就到 100 了呢……"
        ElseIf Score >= 90 Then
            Text = "！好评如潮！"
        ElseIf Score >= 65 Then
            Text = "！今天运气不错呢！"
        ElseIf Score > 50 Then
            Text = "！还行啦，还行啦。"
        ElseIf Score = 50 Then
            Text = "！五五开……"
        ElseIf Score >= 40 Then
            Text = "！勉强还行吧……？"
        ElseIf Score >= 20 Then
            Text = "！呜……"
        ElseIf Score >= 11 Then
            Text = "？！不会吧……"
        ElseIf Score >= 1 Then
            Text = "……（是百分制哦）"
        Else
            Text = "？！"
            If MyMsgBox("在查看结果前，请先同意以下附加使用条款：" & vbCrLf & vbCrLf &
                        "1. 我知晓并了解 PCL 的今日人品功能完全没有出 Bug。" & vbCrLf &
                        "2. PCL 不对使用本软件所间接造成的一切财产损失（如砸电脑等）等负责。",
                        "今日人品 - 附加使用条款", "同意并查看结果", "再见", "", IsWarn:=True) = 2 Then Return
        End If
        MyMsgBox(If(FrmMain.IsSystemTimeChanged, "在这个时候，你的人品值会是：", "你今天的人品值是：") & Score & Text,
                 If(FrmMain.IsSystemTimeChanged, "今日人品? - ", "今日人品 - ") & DateTime.Now.ToString("yyyy'/'M'/'d"),
                 "我知道了", "", "", Score < 20)
    End Sub

#End Region

#Region "清理游戏垃圾"

    Private Sub BtnClear_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles BtnClear.MouseLeftButtonUp
        RubbishClear()
    End Sub

    Public Shared Sub RubbishClear()
        RunInUi(Sub()
                    If FrmOtherTest IsNot Nothing AndAlso FrmOtherTest.BtnClear IsNot Nothing AndAlso FrmOtherTest.BtnClear.IsLoaded Then
                        FrmOtherTest.BtnClear.IsEnabled = False
                    End If
                End Sub)
        RunInNewThread(
            Sub()
                Try
                    If HasRunningMinecraft OrElse McLaunchLoader.State = LoadState.Loading Then
                        Hint("请先关闭所有运行中的游戏！")
                    ElseIf HasDownloadingTask() Then
                        Hint("请在所有下载任务完成后再来清理吧！")
                    Else
                        If Not McFolderList.Any() Then McFolderListLoader.Start()
                        If PathUtils.IsSame(PathTemp, Path.GetTempPath() & "PCL\") Then
                            If Settings.Get(Of Integer)("HintClearRubbish") <= 2 Then
                                If MyMsgBox("即将清理游戏日志、错误报告、缓存等文件。" & vbCrLf &
                                            "虽然应该没人往这些地方放重要文件，但还是问一下，是否确认继续？" & vbCrLf & vbCrLf &
                                            "在完成清理后，需要重启 PCL。", "清理确认", "确定", "取消") = 2 Then Return
                                Settings.Set("HintClearRubbish", Settings.Get(Of Integer)("HintClearRubbish") + 1)
                            End If
                        ElseIf MyMsgBox("即将清理游戏日志、错误报告、缓存等文件。" & vbCrLf & vbCrLf &
                                        "你已将缓存文件夹手动修改为：" & PathTemp & vbCrLf &
                                        "清理过程中，将删除该文件夹中的所有内容，且无法恢复。请确认其中没有除了 PCL 缓存以外的重要文件！" & vbCrLf & vbCrLf &
                                        "在完成清理后，需要重启 PCL。", "清理确认", "确定", "取消") = 2 Then
                            Return
                        End If
                        Dim FolderList As New List(Of String)
                        If Not McFolderList.Any() Then McFolderListLoader.WaitForExit()
                        For Each Folder In McFolderList
                            FolderList.Add(Folder.Location)
                            FolderList.AddRange(DirectoryUtils.EnumerateDirectories(Folder.Location & "versions", False, "*"))
                        Next
                        Dim ClearFolder As Action(Of String) =
                            Sub(Dir As String)
                                Try
                                    DirectoryUtils.Delete(Dir, False)
                                Catch ex As Exception
                                    Logger.Warn(ex, $"清理文件夹失败（{Dir}）")
                                End Try
                            End Sub
                        For Each Folder In FolderList
                            ClearFolder(Path.Combine(Folder, ".mixin.out"))
                            ClearFolder(Path.Combine(Folder, "crash-reports"))
                            ClearFolder(Path.Combine(Folder, "debug"))
                            ClearFolder(Path.Combine(Folder, "logs"))
                            For Each File In DirectoryUtils.EnumerateFiles(Folder, False, "*")
                                Dim FileName = PathUtils.GetLastPart(File)
                                If FileName.StartsWithF("hs_err_pid", False) OrElse PathUtils.GetExtension(FileName) = "log" OrElse FileName = "WailaErrorOutput.txt" Then
                                    FileUtils.Delete(File, False)
                                End If
                            Next
                            For Each SubDir In DirectoryUtils.EnumerateDirectories(Folder, False, "*")
                                Dim DirName = PathUtils.GetLastPart(SubDir)
                                If DirName = PathUtils.GetLastPart(Folder) & "-natives" OrElse DirName = "natives-windows-x86_64" Then ClearFolder(SubDir)
                            Next
                        Next
                        ClearFolder(PathTemp)
                        ClearFolder(OsDrive & "ProgramData\PCL\")
                        If MyMsgBox("垃圾已经清理好啦！", "清理完成", "关闭 PCL", "重启 PCL", "", IsWarn:=False, HighLight:=True, ForceWait:=True) = 2 Then
                            StartProcess(New ProcessStartInfo(PathExe, "--wait"))
                        End If
                        FormMain.EndProgramForce()
                    End If
                Catch ex As Exception
                    Logger.Error(ex, "清理垃圾失败", LogBehavior.Toast)
                Finally
                    RunInUiWait(Sub()
                                    If FrmOtherTest IsNot Nothing AndAlso FrmOtherTest.BtnClear IsNot Nothing AndAlso FrmOtherTest.BtnClear.IsLoaded Then
                                        FrmOtherTest.BtnClear.IsEnabled = True
                                    End If
                                End Sub)
                End Try
            End Sub, "Rubbish Clear")
    End Sub

#End Region

#Region "内存优化"

    Private Sub BtnMemory_Click(sender As Object, e As EventArgs) Handles BtnMemory.Click
        RunInThread(Sub() MemoryOptimize(True))
    End Sub

    Public Shared Sub MemoryOptimize(ShowHint As Boolean)
        If IsMemoryOptimizeRunning Then
            If ShowHint Then Hint("内存优化尚未结束，请稍等！")
            Return
        End If
        IsMemoryOptimizeRunning = True
        Try
            Dim Added As Long
            If WindowsUtils.HasAdminRole() Then
                Dim Before As Long = CLng(My.Computer.Info.AvailablePhysicalMemory)
                Try
                    MemoryOptimizeInternal(ShowHint)
                Catch ex As Exception
                    Logger.Warn(ex, "内存优化失败", If(ShowHint, LogBehavior.Toast, LogBehavior.None))
                    Return
                Finally
                    IsMemoryOptimizeRunning = False
                End Try
                Added = CLng(My.Computer.Info.AvailablePhysicalMemory) - Before
            Else
                Try
                    Added = CLng(RunAsAdmin("--memory")) * 1024L
                Catch ex As Exception
                    Logger.Warn(ex, "命令行形式内存优化失败")
                    If ShowHint Then Hint("获取管理员权限失败，请尝试右键 PCL，选择 " & vbLQ & "以管理员身份运行" & vbRQ & "！", HintType.Red)
                    Return
                Finally
                    IsMemoryOptimizeRunning = False
                End Try
                If Added < 0 Then Return
            End If
            Dim Remaining As String = StringUtils.FormatByteSize(CLng(My.Computer.Info.AvailablePhysicalMemory))
            If Added > 0 Then
                If ShowHint Then Hint($"内存优化完成，可用内存增加了 {StringUtils.FormatByteSize(CLng(Math.Round(Added * 0.8)))}，目前剩余内存 {Remaining}！", HintType.Green)
            ElseIf ShowHint Then
                Hint($"内存优化完成，已经优化到了最佳状态，目前剩余内存 {Remaining}！")
            End If
        Finally
            IsMemoryOptimizeRunning = False
        End Try
    End Sub

    Public Shared Sub MemoryOptimizeInternal(ShowHint As Boolean)
        If Not WindowsUtils.HasAdminRole() Then
            Throw New Exception("内存优化功能需要管理员权限！" & vbCrLf &
                                "如果需要自动以管理员身份启动 PCL，可以右键 PCL，打开 属性 → 兼容性 → 以管理员身份运行此程序。")
        End If
        Using Identity As WindowsIdentity = WindowsIdentity.GetCurrent(TokenAccessLevels.Query Or TokenAccessLevels.AdjustPrivileges)
            Dim NewState As New PrivilegeToken With {.PrivilegeCount = 1, .Luid = 0, .Attributes = 2}
            Dim SystemName As String = Nothing
            Dim Name As String = "SeProfileSingleProcessPrivilege"
            If Not LookupPrivilegeValue(SystemName, Name, NewState.Luid) Then
                Throw New Exception($"获取内存优化权限失败（错误代码：{Marshal.GetLastWin32Error()}）")
            End If
            Dim PreviousState As IntPtr = IntPtr.Zero
            Dim ReturnLength As Integer = 0
            If Not AdjustTokenPrivileges(Identity.Token, False, NewState, Marshal.SizeOf(NewState), PreviousState, ReturnLength) OrElse Marshal.GetLastWin32Error() <> 0 Then
                Throw New Exception($"获取内存优化权限失败（错误代码：{Marshal.GetLastWin32Error()}）")
            End If
        End Using
        For I = 2 To 4
            Dim Handle = GCHandle.Alloc(I, GCHandleType.Pinned)
            Try
                Dim Result As UInteger = NtSetSystemInformation(80, Handle.AddrOfPinnedObject(), Marshal.SizeOf(I))
                If Result <> 0 Then Throw New Exception($"内存优化操作 {I} 失败（错误代码：{Result}）")
            Finally
                Handle.Free()
            End Try
        Next
    End Sub

    <StructLayout(LayoutKind.Sequential)>
    Private Structure PrivilegeToken
        Public PrivilegeCount As Integer
        Public Luid As Long
        Public Attributes As Integer
    End Structure

    <DllImport("ntdll.dll", SetLastError:=True)>
    Private Shared Function NtSetSystemInformation(SystemInformationClass As Integer, SystemInformation As IntPtr, SystemInformationLength As Integer) As UInteger
    End Function

    <DllImport("advapi32.dll", EntryPoint:="LookupPrivilegeValueA", SetLastError:=True, CharSet:=CharSet.Ansi)>
    Private Shared Function LookupPrivilegeValue(<MarshalAs(UnmanagedType.VBByRefStr)> ByRef lpSystemName As String,
                                                 <MarshalAs(UnmanagedType.VBByRefStr)> ByRef lpName As String,
                                                 ByRef lpLuid As Long) As Boolean
    End Function

    <DllImport("advapi32.dll", SetLastError:=True)>
    Private Shared Function AdjustTokenPrivileges(TokenHandle As IntPtr, DisableAllPrivileges As Boolean,
                                                  ByRef NewState As PrivilegeToken, BufferLength As Integer,
                                                  ByRef PreviousState As IntPtr, ByRef ReturnLength As Integer) As Boolean
    End Function

#End Region

#Region "回声洞"

    Private Sub BtnCave_Click(sender As Object, e As EventArgs) Handles BtnCave.Click
        OpenWebsite("https://jinshuju.net/f/esXHQF")
    End Sub

    Private Sub CaveHand()
        BtnCave.Visibility = If(CurrentRank < DonationRank.Rank23, Visibility.Collapsed, Visibility.Visible)
    End Sub

    Private Sub CardCave_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles CardCave.MouseLeftButtonUp
        If Not IsCaveEnabled Then Return
        Dim OldText As String = LabCave.Text
        LabCave.Text = GetRandomCave()
        IsCaveEnabled = False
        AniStart(New AniData() {
            AaOpacity(LabCave, -1, 5),
            AaOpacity(LabCave, 1, 5, 30),
            AaOpacity(LabCave, -1, 5, 70),
            AaOpacity(LabCave, 0.9, 5, 85),
            AaOpacity(LabCave, -1, 5, 125),
            AaOpacity(LabCave, 0.8, 5, 145),
            AaOpacity(LabCave, -1, 5, 165),
            AaOpacity(LabCave, 0.7, 5, 195),
            AaOpacity(LabCave, -1, 5, 210),
            AaOpacity(LabCave, 0.5, 5, 235),
            AaOpacity(LabCave, -1, 5, 250),
            AaOpacity(LabCave, 1, 1, 400, After:=True),
            AaTextAppear(LabCave, Hide:=False, TimePerText:=True, Time:=60, Delay:=400),
            AaCode(Sub() IsCaveEnabled = True, 0, After:=True)
        }, "Cave")
        LabCave.Text = OldText
    End Sub

    Public Shared Function GetRandomCave() As String
        Return RandomOne(PageOtherCaveData.CaveTexts)
    End Function

    Public Shared Function GetRandomHint() As String
        Try
            Dim HintFile As String = Paths.Base & "PCL\hints.txt"
            If FileUtils.Exists(HintFile) Then
                Dim Lines = FileUtils.ReadAsLines(HintFile, True).Where(Function(s) Not String.IsNullOrWhiteSpace(s)).ToList()
                If Lines.Any() Then Return RandomOne(Lines)
            End If
        Catch ex As Exception
            Logger.Error(ex, "获取自定义 你知道吗 提示失败", LogBehavior.Toast)
        End Try
        Return GetRandomPresetHint()
    End Function

    Public Shared Function GetRandomPresetHint() As String
        Return RandomOne(PageOtherCaveData.PresetHintTexts)
    End Function

#End Region

#Region "千万别点"

    Private Sub BtnClick_Click(sender As Object, e As EventArgs) Handles BtnClick.Click
        ThemeDontClick = 2
        Dim Effects As New List(Of Integer) From {RandomInteger(0, 7)}
        If RandomInteger(0, 1) = 1 Then Effects.Add(RandomInteger(1, 6))
        If DateTime.Now.Month = 4 AndAlso DateTime.Now.Day = 1 Then Effects = New List(Of Integer) From {7}
        If Effects.Contains(1) Then
            If MyMsgBox("当暴露在点击确定后的场景时，有极小部分人群会引发癫痫。这种情形可能是由于某些未查出的癫痫症状引起，即使该人员并没有患癫痫病史也有可能造成此类病症。如果您的家人或任何家庭成员曾有过类似症状，请在点击确定前咨询您的医生或医师。如果您在稍后出现任何症状，包括头晕、目眩、眼部或肌肉抽搐、失去意识、失去方向感、抽搐或出现任何自己无法控制的动作，请立即关闭 PCL 并咨询您的医生或医师。" & vbCrLf &
                        "这是最后的警告，是否继续操作？", "警告", "确定", "取消", "", IsWarn:=True) = 2 Then Return
        Else
            MyMsgBox("PCL 作者不会受理由于点击千万别点造成的任何 Bug。" & vbCrLf & "这是最后的警告，是否继续操作？",
                     "警告", "确定", "确定", "确定", IsWarn:=True)
        End If
        ThemeRefresh()
        For Each EffectId In Effects
            Select Case EffectId
                Case 0
                    ThemeDontClick = 1
                    ThemeRefresh()
                Case 1
                    ThemeDontClick = 2
                    ThemeRefresh()
                Case 2
                    If FrmMain.PanBack.LayoutTransform IsNot Nothing Then FrmMain.PanBack.RenderTransformOrigin = New Point(1.25, 1.25)
                    Select Case RandomInteger(0, 2)
                        Case 0 : FrmMain.PanBack.RenderTransform = New ScaleTransform(1, -1)
                        Case 1 : FrmMain.PanBack.RenderTransform = New ScaleTransform(-1, -1)
                        Case 2 : FrmMain.PanBack.RenderTransform = New ScaleTransform(-1, 1)
                    End Select
                Case 3
                    FrmMain.IsSizeSaveable = False
                    FrmMain.PanBack.LayoutTransform = New ScaleTransform(2.5, 2.5)
                    FrmMain.Height = My.Computer.Screen.WorkingArea.Height - 200
                    FrmMain.Width = My.Computer.Screen.WorkingArea.Width - 200
                    FrmMain.Left = 0
                    FrmMain.Top = 0
                    AniStop("Don't Click Scale")
                Case 4
                    FrmMain.IsSizeSaveable = False
                    FrmMain.RenderTransform = New ScaleTransform()
                    AniStart(AaScaleTransform(FrmMain, -0.85, 5000, 0, New AniEaseOutFluent(AniEasePower.ExtraStrong)), "Don't Click Scale")
                    AniStop("Don't Click Rotate")
                Case 5
                    FrmMain.RenderTransform = New RotateTransform()
                    AniStart(AaRotateTransform(FrmMain, 1000000 * RandomOne(New Integer() {1, -1}), 10000000), "Don't Click Rotate")
                    AniStop("Don't Click Scale")
                Case 6
                    FrmMain.IsSizeSaveable = False
                    If RandomInteger(0, 1) <> 0 Then
                        AniStart(AaX(FrmMain, 10000000 * RandomOne(New Integer() {1, -1}), 30000000), "Don't Click Move X")
                    Else
                        AniStart(AaY(FrmMain, 10000000 * RandomOne(New Integer() {1, -1}), 50000000), "Don't Click Move Y")
                    End If
                    RunInThread(
                        Sub()
                            Do While True
                                RunInUi(
                                    Sub()
                                        Dim ScreenBounds = My.Computer.Screen.Bounds
                                        If FrmMain.Top < -FrmMain.Height Then FrmMain.Top = ScreenBounds.Height + 49
                                        If FrmMain.Top > ScreenBounds.Height + 50 Then FrmMain.Top = -FrmMain.Height + 1
                                        If FrmMain.Left < -FrmMain.Width Then FrmMain.Left = ScreenBounds.Width + 49
                                        If FrmMain.Left > ScreenBounds.Width + 50 Then FrmMain.Left = -FrmMain.Width + 1
                                    End Sub)
                                Thread.Sleep(10)
                            Loop
                        End Sub)
                Case 7
                    OpenWebsite("https://www.bilibili.com/video/BV1GJ411x7h7")
            End Select
        Next
        BtnClick.Visibility = Visibility.Collapsed
    End Sub

#End Region

End Class
