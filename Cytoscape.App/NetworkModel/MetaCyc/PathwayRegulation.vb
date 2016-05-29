﻿Imports Microsoft.VisualBasic.DocumentFormat.Csv.Extensions
Imports Microsoft.VisualBasic
Imports LANS.SystemsBiology.DatabaseServices
Imports LANS.SystemsBiology.AnalysisTools.NBCR.Extensions.MEME_Suite

Namespace NetworkModel

    Public Class PathwayRegulation : Inherits Pathways

        Sub New(MetaCyc As LANS.SystemsBiology.Assembly.MetaCyc.File.FileSystem.DatabaseLoadder)
            Call MyBase.New(MetaCyc)
        End Sub

        Public Sub AnalysisMetaPathwayRegulations(ExportDir As String, RegulationProfiles As MatchedResult())
            Dim Edges As Microsoft.VisualBasic.DataVisualization.Network.FileStream.NetworkEdge() = Nothing, Nodes As Pathways.Pathway() = Nothing
            Call Export(Edges, Nodes)

            Call Console.WriteLine("Generate TF <--> pathway regulations...")
            Dim TFCollection As String() = (From Line In RegulationProfiles Select Line.TF Distinct Order By TF Ascending).ToArray
            Dim LQuery = (From Tf As String In TFCollection
                          Let Regulation = (From Line In RegulationProfiles Where String.Equals(Tf, Line.TF) Select Line).ToArray
                          Let Get_TFRegulatedGenes = Function() As String()
                                                         Dim _ChunkList As List(Of String) = New List(Of String)
                                                         For Each Line In Regulation
                                                             Call _ChunkList.AddRange(Line.OperonGeneIds)
                                                         Next
                                                         Return (From strId As String In _ChunkList Select strId Distinct Order By strId).ToArray
                                                     End Function
                          Let TFRegulatedGenes As String() = Get_TFRegulatedGenes()
                          Select New With {.TF = Tf, .RegulatedPathways = (From item In Nodes Where item.SuperPathway = False AndAlso Regulates(TFRegulatedGenes, item) Select item).ToArray}).ToArray

            Dim Network = New List(Of Microsoft.VisualBasic.DataVisualization.Network.FileStream.NetworkEdge)
            Dim NodeList As List(Of PathwayRegulator) = New List(Of PathwayRegulator)

            Call Console.WriteLine("Start to create network visualization model...")

            For Each Line In LQuery
                Dim Regulations = (From Pathway In Line.RegulatedPathways
                                   Select New Microsoft.VisualBasic.DataVisualization.Network.FileStream.NetworkEdge With {
                                       .FromNode = Line.TF,
                                       .ToNode = Pathway.Identifier,
                                       .InteractionType = "Regulates"}).ToArray

                If Not Regulations.IsNullOrEmpty Then
                    Call Network.AddRange(Regulations)
                    Call NodeList.Add(New PathwayRegulator With {.Identifier = Line.TF})

                    Dim ChunkTemp As List(Of Microsoft.VisualBasic.DataVisualization.Network.FileStream.NetworkEdge) = New List(Of Microsoft.VisualBasic.DataVisualization.Network.FileStream.NetworkEdge)
                    For Each Node In Regulations
                        If Not Exists(ChunkTemp, Node) Then
                            Call ChunkTemp.Add(Node)
                        End If
                    Next
                    Dim RegulationNodes As List(Of Microsoft.VisualBasic.DataVisualization.Network.FileStream.Node) = New List(Of Microsoft.VisualBasic.DataVisualization.Network.FileStream.Node)
                    For Each Item In ChunkTemp
                        If RegulationNodes.GetItem(Item.ToNode) Is Nothing Then
                            Call RegulationNodes.Add(New Microsoft.VisualBasic.DataVisualization.Network.FileStream.Node With {.Identifier = Item.ToNode, .NodeType = "Pathway"})
                        End If
                    Next
                    Dim PathwaysIds = (From item In RegulationNodes Select item.Identifier Distinct).ToArray
                    Dim Interactions = (From item In Edges.AsParallel Where Array.IndexOf(PathwaysIds, item.FromNode) > -1 AndAlso Array.IndexOf(PathwaysIds, item.ToNode) > -1 Select item).ToArray

                    Call ChunkTemp.AddRange(Interactions)
                    Call RegulationNodes.Add(New Microsoft.VisualBasic.DataVisualization.Network.FileStream.Node With {.NodeType = "Regulator", .Identifier = Line.TF})

                    Call RegulationNodes.SaveTo(String.Format("{0}/{1}/Nodes.csv", ExportDir, Line.TF), False)
                    Call ChunkTemp.SaveTo(String.Format("{0}/{1}/PathwayRegulations.csv", ExportDir, Line.TF), True)
                End If
            Next

            Call Network.AddRange(Edges)
            Dim ChunkList As List(Of Microsoft.VisualBasic.DataVisualization.Network.FileStream.NetworkEdge) = New List(Of Microsoft.VisualBasic.DataVisualization.Network.FileStream.NetworkEdge)

            Call Console.WriteLine("Remove the duplicated data!")

            For Each Node In Network
                If Not Exists(ChunkList, Node) Then
                    Call ChunkList.Add(Node)
                End If
            Next

            Network = ChunkList

            Call Console.WriteLine("All of the job done, start to saving data!")

            Call Network.SaveTo(String.Format("{0}/Edges.csv", ExportDir), False)
            Call Nodes.SaveTo(String.Format("{0}/Pathways.csv", ExportDir), False)
            Call NodeList.SaveTo(String.Format("{0}/Regulators.csv", ExportDir), False)
        End Sub

        Private Shared Function Regulates(RegulatedGenes As String(), Pathway As Pathways.Pathway) As Boolean
            For Each RegulatedGene As String In RegulatedGenes
                If Array.IndexOf(Pathway.GeneObjects, RegulatedGene) Then
                    Return True
                End If
            Next

            Return False
        End Function

        Private Shared Function Exists(Network As Generic.IEnumerable(Of Microsoft.VisualBasic.DataVisualization.Network.FileStream.NetworkEdge),
                                       Node As Microsoft.VisualBasic.DataVisualization.Network.FileStream.NetworkEdge) As Boolean
            Dim LQuery = (From NodeItem In Network.AsParallel Where NodeItem.IsEqual(Node) Select 1).ToArray
            Return Not LQuery.IsNullOrEmpty
        End Function

        Public Class PathwayRegulator : Inherits Microsoft.VisualBasic.DataVisualization.Network.FileStream.Node

        End Class
    End Class
End Namespace