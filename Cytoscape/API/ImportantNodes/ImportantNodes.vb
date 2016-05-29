﻿Imports LANS.SystemsBiology.AnalysisTools.DataVisualization.Interaction.Cytoscape.DocumentFormat.Tables
Imports LANS.SystemsBiology.InteractionModel
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.DocumentFormat.Csv
Imports Microsoft.VisualBasic.DocumentFormat.Csv.StorageProvider.Reflection
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports Microsoft.VisualBasic
Imports Microsoft.VisualBasic.Linq
Imports System.Runtime.CompilerServices

Namespace API.ImportantNodes

    '{Hu, 2010 #173}

    <[PackageNamespace]("Cytoscape.ImportantNodes")>
    Public Module ImportantNodes

        <ExportAPI("Read.Csv.Nodes.Cytoscape")>
        Public Function LoadNodeTable(path As String) As Node()
            Return path.LoadCsv(Of Node)(False).ToArray
        End Function

        <ExportAPI("Read.Csv.RegulationEdges")>
        Public Function ReadRegulations(path As String) As RegulatorRegulation()
            Dim inBuf As IEnumerable(Of Regulations) = path.LoadCsv(Of Regulations)(False)
            Dim allORFs As String() = (From item In inBuf Select item.ORF Distinct).ToArray
            Return (From ORF As String In allORFs.AsParallel
                    Let Regulators As String() = (From x As Regulations In inBuf
                                                  Where String.Equals(x.ORF, ORF)
                                                  Select x.Regulator
                                                  Distinct).ToArray
                    Let rel As RegulatorRegulation = New RegulatorRegulation With {
                        .LocusId = ORF,
                        .Regulators = Regulators
                    }
                    Select rel).ToArray
        End Function

        ''' <summary>
        '''这个仅仅是理论上面的计算结果，仅供参考
        ''' </summary>
        ''' <param name="ImportantNodes"></param>
        ''' <param name="Regulations"></param>
        ''' <param name="rankCutoff"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        <ExportAPI("Regulator.Significants",
                   Info:="rank_cutoff = 0 stands for using the default value; -1 stands for using all of the nodes without any cutoff value screening, else 0 - 1 for the selected percentage.")>
        <Extension>
        Public Function SignificantRegulator(ImportantNodes As IEnumerable(Of KeyValuePair(Of Integer, Node())),
                                             Regulations As IEnumerable(Of IRegulatorRegulation),
                                             <Parameter("Rank.Cutoff",
                                                        "0 stands for using the default value; -1 stands for using all of the nodes without any cutoff value screening, else 0 - 1 for the selected percentage.")>
                                             Optional rankCutoff As Double = -1) As RankRegulations()
            If rankCutoff = 0.0R Then
                rankCutoff = 0.1 * ImportantNodes.Count
            ElseIf rankCutoff < 0 Then
                rankCutoff = ImportantNodes.Count
            Else
                rankCutoff = ImportantNodes.Count * rankCutoff
            End If

            Regulations = (From rel As IRegulatorRegulation
                           In Regulations.AsParallel
                           Where Not (String.IsNullOrEmpty(rel.LocusId) OrElse rel.Regulators.IsNullOrEmpty)
                           Select rel).ToArray    ' Trim Data
            ImportantNodes = (From node In ImportantNodes
                              Where node.Key <= rankCutoff
                              Select node).ToArray
            rankCutoff += 1

            Dim ImportantRankNodes = (From node In ImportantNodes.AsParallel
                                      Select New NodeRank With {
                                          .Rank = node.Key,
                                          .Nodes = (From n As Node In node.Value Select n.SharedName).ToArray}).ToArray
            Dim RegulatorRanks = (From ranks As NodeRank In ImportantRankNodes.AsParallel
                                  Select New RankRegulations With {
                                      .RankScore = rankCutoff - ranks.Rank,
                                      .Regulators = (From rel As IRegulatorRegulation
                                                     In Regulations
                                                     Where Array.IndexOf(ranks.Nodes, rel.LocusId) > -1
                                                     Select rel.Regulators).MatrixAsIterator.Distinct.ToArray,
                                      .GeneCluster = ranks.Nodes}).ToArray
            Return RegulatorRanks
        End Function

        <ExportAPI("Write.Csv.Nodes.Important")>
        Public Function SaveResult(data As IEnumerable(Of KeyValuePair(Of Integer, Node())), saveCsv As String) As Boolean
            Dim Csv As DocumentStream.File = New DocumentStream.File From {New String() {"Rank", "ImportantNodes"}}
            Csv += (From item In data.AsParallel Let Rank = item.Key
                    Let Nodes As String = String.Join("; ", (From n In item.Value Select n.SharedName).ToArray)
                    Let Row As DocumentStream.RowObject = {CStr(Rank), Nodes}.ToCsvRow
                    Select Row
                    Order By Val(Row.First) Ascending).ToArray
            Return Csv.Save(saveCsv, False)
        End Function

        <ExportAPI("read.csv.rank_nodes")>
        Public Function ReadRankedNodes(path As String) As KeyValuePair(Of Integer, Node())()
            Dim CsvData = path.LoadCsv(Of NodeRank)(False)
            Return (From x As NodeRank
                    In CsvData.AsParallel
                    Let nodes = (From name As String In x.Nodes Select New Node With {.SharedName = name}).ToArray
                    Select New KeyValuePair(Of Integer, Node())(x.Rank, nodes)).ToArray
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="S"></param>
        ''' <param name="Fast">
        ''' if fast parameter is set to true, then a parallel edition of the algorithm 
        ''' will implemented for accelerates the network calculation, and this is much 
        ''' helpful for a large scale network.
        ''' </param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        <ExportAPI("evaluate.importance",
                   Info:="If fast parameter is set to true, then a parallel edition of the algorithm will implemented for accelerates the network calculation.")>
        Public Function EquivalenceClass(S As Node(), Optional Fast As Boolean = False) As KeyValuePair(Of Integer, Node())()
            If Fast Then Return __equivalenceFast(S.ToList, S)

            Dim NDS As List(Of Node) = S.ToList
            Dim Extra As List(Of Node) = New List(Of Node)
            Dim Rank As Integer = 0
            Dim SortResult As List(Of KeyValuePair(Of Integer, Node())) = New List(Of KeyValuePair(Of Integer, Node()))

            Do While Not NDS.IsNullOrEmpty
                For Each a As Node In NDS.ToArray

                    For Each b As Node In S
                        If NDS.IndexOf(b) > -1 AndAlso a < b Then

                            Call NDS.Remove(a)
                            Call Extra.Add(a)
                        End If
                    Next
                Next

                Rank += 1
                Call SortResult.Add(New KeyValuePair(Of Integer, Node())(Rank, NDS.ToArray))
                Call Console.WriteLine("Rank:= {0};  ImportantNodes:= {1}", SortResult.Last.Key, String.Join("; ", (From item In SortResult.Last.Value Select item.SharedName).ToArray))
                NDS = Extra.Distinct.ToList
                Call Extra.Clear()
            Loop

            Return SortResult.ToArray
        End Function

        Private Function __equivalenceFast(NDS As List(Of Node), S As Node()) As KeyValuePair(Of Integer, Node())()
            Dim Rank As Integer = 0
            Dim SortResult As List(Of KeyValuePair(Of Integer, Node())) = New List(Of KeyValuePair(Of Integer, Node()))

            Do While Not NDS.IsNullOrEmpty
                Dim LQuery = (From b As Node
                              In S.AsParallel
                              Where NDS.IndexOf(b) > -1
                              Let ia = (From a As Node In NDS Where a < b Select a).ToArray
                              Select ia).MatrixToVector
                NDS = (From node As Node
                       In NDS.AsParallel
                       Where Array.IndexOf(LQuery, node) = -1
                       Select node
                       Distinct).ToList
                Rank += 1
                If NDS.IsNullOrEmpty Then
                    Exit Do
                End If
                Call SortResult.Add(New KeyValuePair(Of Integer, Node())(Rank, NDS.ToArray))
                Call Console.WriteLine("Rank:= {0};  ImportantNodes:= {1}", SortResult.Last.Key, String.Join("; ", (From item In SortResult.Last.Value Select item.SharedName).ToArray))
                NDS = LQuery.Distinct.ToList
            Loop

            Return SortResult.ToArray
        End Function
    End Module
End Namespace