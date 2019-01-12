Imports System.Net
Imports System.Net.Sockets


Public Class Form1
    Dim socketz As New Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP)
    Dim bytedata(4096) As Byte
    Dim myip As IPAddress
    Dim started As Boolean = True
    Dim sizediff As Size
    Dim formloaded As Boolean = False
    Dim FilterIPAddress As New IPAddress(0)
    Dim FilterIP As Boolean
    Dim mycomputerconnections() As Net.NetworkInformation.NetworkInterface

    'DGV Update stuff
    Dim stringz As String = ""
    Dim Typez As String = ""
    Dim ipfrom As IPAddress
    Dim ipto As IPAddress
    Dim destinationport As UInteger
    Dim sourceport As UInteger

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        sizediff.Height = Me.Height - DGV.Height
        sizediff.Width = Me.Width - DGV.Width
        formloaded = True

        mycomputerconnections = Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces

        For i = 0 To mycomputerconnections.Length - 1
            ComboBox1.Items.Add(mycomputerconnections(i).Name)
        Next
    End Sub
    Private Sub OnReceive(ByVal asyncresult As IAsyncResult)

        If started = True Then
            'Get Length of packet (including header)
            Dim readlength As UInteger = BitConverter.ToUInt16(Byteswap(bytedata, 2), 0)
            sourceport = BitConverter.ToUInt16(Byteswap(bytedata, 22), 0)
            destinationport = BitConverter.ToUInt16(Byteswap(bytedata, 24), 0)

            'Get Protocol Type
            If bytedata(9) = 6 Then
                Typez = "TCP"
            ElseIf bytedata(9) = 17 Then
                Typez = "UDP"
            Else
                Typez = "???"
            End If

            'Get IP from and to
            ipfrom = New IPAddress(BitConverter.ToUInt32(bytedata, 12))
            ipto = New IPAddress(BitConverter.ToUInt32(bytedata, 16))

            'If this is a packet to/from me and not from myself then...
            If (ipfrom.Equals(myip) = True Or ipto.Equals(myip) = True) And ipto.Equals(ipfrom) = False Then
                If FilterIP = False Or (FilterIP = True And (FilterIPAddress.Equals(ipfrom) Or FilterIPAddress.Equals(ipto))) Then

                    'Fix data
                    stringz = ""
                    For i = 26 To readlength - 1
                        If Char.IsLetterOrDigit(Chr(bytedata(i))) = True Then
                            stringz = stringz & Chr(bytedata(i))
                        Else
                            stringz = stringz & "."
                        End If
                    Next

                    'Put data to DataGridView, since it's on a different thread now, invoke it
                    DGV.Invoke(New MethodInvoker(AddressOf DGVUpdate))

                End If
            End If

        End If

        'Restart the Receiving
        socketz.BeginReceive(bytedata, 0, bytedata.Length, SocketFlags.None, New AsyncCallback(AddressOf OnReceive), Nothing)
    End Sub

    Private Sub DGVUpdate()

        'Remove rows if there are too many
        If DGV.Rows.Count > 50 Then
            DGV.Rows.RemoveAt(0)
        End If
        Dim row As String() = New String() {ipfrom.ToString & ":" & sourceport, ipto.ToString & ":" & destinationport, Typez, stringz}



        DGV.Rows.Add(row)
        'DGV.Rows(DGV.Rows.Count - 1).Cells(0).Value = ipfrom.ToString & ":" & sourceport 'From Column, size at 125
        'DGV.Rows(DGV.Rows.Count - 1).Cells(1).Value = ipto.ToString & ":" & destinationport 'To Column, size at 125
        'DGV.Rows(DGV.Rows.Count - 1).Cells(2).Value = Typez 'Type Column, size at 50
        'DGV.Rows(DGV.Rows.Count - 1).Cells(3).Value = stringz 'Data column, size mode set to fill

    End Sub

    Private Function Byteswap(ByVal bytez() As Byte, ByVal index As UInteger)
        Dim result(1) As Byte
        result(0) = bytez(index + 1)
        result(1) = bytez(index)
        Return result
    End Function

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If started = True Then
            Button1.Text = "Start"
            started = False
        Else
            Button1.Text = "Stop"
            started = True
        End If
    End Sub

    Private Sub Form1_Resize(sender As Object, e As EventArgs) Handles MyBase.Resize
        If formloaded = True Then
            DGV.Size = Me.Size - sizediff
        End If
    End Sub

    Private Sub TextBox1_TextChanged(sender As Object, e As EventArgs) Handles TextBox1.TextChanged
        Try
            If TextBox1.Text <> "" And TextBox1.Text IsNot Nothing Then
                FilterIPAddress = IPAddress.Parse(TextBox1.Text)
                FilterIP = True
                TextBox1.BackColor = Color.LimeGreen
            Else
                FilterIP = False
                TextBox1.BackColor = Color.White
            End If
        Catch ex As Exception
            FilterIP = False
            TextBox1.BackColor = Color.White
        End Try
    End Sub

    Private Sub ComboBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBox1.SelectedIndexChanged
        For i = 0 To mycomputerconnections(ComboBox1.SelectedIndex).GetIPProperties.UnicastAddresses.Count - 1

            If mycomputerconnections(ComboBox1.SelectedIndex).GetIPProperties.UnicastAddresses(i).Address.AddressFamily = Net.Sockets.AddressFamily.InterNetwork Then

                myip = mycomputerconnections(ComboBox1.SelectedIndex).GetIPProperties.UnicastAddresses(i).Address

                BindSocket()

            End If

        Next
    End Sub
    Private Sub BindSocket()

        Try
            socketz.Bind(New IPEndPoint(myip, 0))
            socketz.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, True)
            Dim bytrue() As Byte = {1, 0, 0, 0}
            Dim byout() As Byte = {1, 0, 0, 0}
            socketz.IOControl(IOControlCode.ReceiveAll, bytrue, byout)
            socketz.Blocking = False
            ReDim bytedata(socketz.ReceiveBufferSize)
            socketz.BeginReceive(bytedata, 0, bytedata.Length, SocketFlags.None, New AsyncCallback(AddressOf OnReceive), Nothing)
            ComboBox1.Enabled = False
        Catch ex As Exception
            ComboBox1.BackColor = Color.Red
        End Try

    End Sub

End Class
