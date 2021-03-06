﻿Imports System.Net
Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading

Public Class MainForm

    Private ReadOnly finalBuffer(1024000) As Byte

    Private Sub getHtmlButton_Click(sender As System.Object, e As System.EventArgs) Handles getHtmlButton.Click
        ThreadPool.QueueUserWorkItem(New WaitCallback(AddressOf StartRequest))
        getHtmlButton.Enabled = False
    End Sub

    Private Sub StartRequest()
        Dim webRequest As HttpWebRequest = DirectCast( _
            Net.WebRequest.Create("http://google.ca"),  _
            HttpWebRequest)
        webRequest.BeginGetResponse(New AsyncCallback(
            AddressOf GetResponseCallback), webRequest)
    End Sub

    Private Sub GetResponseCallback(ByVal asyncResult As IAsyncResult)
        Dim webRequest As HttpWebRequest = DirectCast(asyncResult.AsyncState, HttpWebRequest)
        Dim response As WebResponse = webRequest.EndGetResponse(asyncResult)
        Dim stream As Stream = response.GetResponseStream()
        If (Not stream Is Nothing) Then
            StreamHelper.BeginReadStreamToEnd(stream, finalBuffer, 0, CInt(finalBuffer.Length), New AsyncCallback(AddressOf ReadToEndCallback), stream)
        End If
    End Sub

    Private Sub ReadToEndCallback(ByVal asyncResult As IAsyncResult)
        Dim bytesRead As Integer = StreamHelper.EndReadStreamToEnd(asyncResult)
        Trace.WriteLine(String.Format("Read {0} bytes", bytesRead))
        Dim html As String = Encoding.ASCII.GetString(finalBuffer, 0, bytesRead)
        SetData(html)
        EnableButton()
    End Sub

    Delegate Sub EnableButtonDelegate()
    Delegate Sub SetDataDelegate(ByVal html As String)

    Private Sub SetData(ByVal html As String)
        If String.IsNullOrEmpty(html) Then Return

        If InvokeRequired Then
            Dim objArray(0) As Object
            objArray(0) = html
            BeginInvoke(New SetDataDelegate(AddressOf SetData), objArray)
            Return
        End If

        listBox.Items.Clear()
        For Each x As String In GetScriptBodies(html)
            listBox.Items.Add(x)
        Next
    End Sub

    Private Sub EnableButton()
        If (Not InvokeRequired) Then
            getHtmlButton.Enabled = True
        Else
            BeginInvoke(New EnableButtonDelegate(AddressOf EnableButton))
        End If
    End Sub

    Private Shared Function GetScriptBodies(ByVal html As String) As IEnumerable(Of String)
        Const pattern As String = "<script.*?>\s*(?'scriptBody'.+?)\s*</script>"
        Dim matches As MatchCollection = Regex.Matches(html, pattern)
        Dim result As List(Of String) = New List(Of String)(matches.Count)
        For Each match As Match In matches
            Dim scriptBody As String = match.Groups("scriptBody").Value
            result.Add(scriptBody)
        Next
        Return result
    End Function
End Class
