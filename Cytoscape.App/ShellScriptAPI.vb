﻿Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports LANS.SystemsBiology.DatabaseServices
Imports LANS.SystemsBiology.AnalysisTools.NBCR.Extensions.MEME_Suite
Imports System.Drawing
Imports LANS.SystemsBiology.AnalysisTools.DataVisualization.Interaction.Cytoscape.DocumentFormat.CytoscapeGraphView
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports LANS.SystemsBiology.Assembly.MetaCyc.File.FileSystem
Imports LANS.SystemsBiology.Assembly.KEGG.DBGET.ReferenceMap
Imports Microsoft.VisualBasic.Linq

<[PackageNamespace]("Cytoscape",
                    Cites:="Shannon, P., et al. (2003). ""Cytoscape: a software environment For integrated models Of biomolecular interaction networks."" Genome Res 13(11): 2498-2504.
<p>Cytoscape is an open source software project for integrating biomolecular interaction networks with high-throughput expression data and other molecular states into a unified 
                    conceptual framework. Although applicable to any system of molecular components and interactions, Cytoscape is most powerful when used in conjunction 
                    with large databases of protein-protein, protein-DNA, and genetic interactions that are increasingly available for humans and model organisms. 
                    Cytoscape's software Core provides basic functionality to layout and query the network; to visually integrate the network with expression profiles, phenotypes, 
                    and other molecular states; and to link the network to databases of functional annotations. The Core is extensible through a straightforward plug-in architecture, 
                    allowing rapid development of additional computational analyses and features. Several case studies of Cytoscape plug-ins are surveyed, including a search for 
                    interaction pathways correlating with changes in gene expression, a study of protein complexes involved in cellular recovery to DNA damage, inference of a 
                    combined physical/functional interaction network for Halobacterium, and an interface to detailed stochastic/kinetic gene regulatory models.

", Url:="http://www.cytoscape.org/", Publisher:="Shannon, P.<br />
Markiel, A.<br />
Ozier, O.<br />
Baliga, N. S.<br />
Wang, J. T.<br />
Ramage, D.<br />
Amin, N.<br />
Schwikowski, B.<br />
Ideker, T.", Description:="")>
<Cite(Title:="Cytoscape: a software environment for integrated models of biomolecular interaction networks",
     Journal:="Genome Res", Pages:="2498-504", Issue:="11", Volume:=13,
      Authors:="Shannon, P.
Markiel, A.
Ozier, O.
Baliga, N. S.
Wang, J. T.
Ramage, D.
Amin, N.
Schwikowski, B.
Ideker, T.", Year:=2003, DOI:="10.1101/gr.1239303",
      Abstract:="Cytoscape is an open source software project for integrating biomolecular interaction networks with high-throughput expression data and other molecular states into a unified conceptual framework. 
Although applicable to any system of molecular components and interactions, Cytoscape is most powerful when used in conjunction with large databases of protein-protein, protein-DNA, and genetic interactions that are increasingly available for humans and model organisms. 
Cytoscape's software Core provides basic functionality to layout and query the network; to visually integrate the network with expression profiles, phenotypes, and other molecular states; and to link the network to databases of functional annotations. 
The Core is extensible through a straightforward plug-in architecture, allowing rapid development of additional computational analyses and features. 
Several case studies of Cytoscape plug-ins are surveyed, including a search for interaction pathways correlating with changes in gene expression, a study of protein complexes involved in cellular recovery to DNA damage, 
inference of a combined physical/functional interaction network for Halobacterium, and an interface to detailed stochastic/kinetic gene regulatory models.",
      AuthorAddress:="Institute for Systems Biology, Seattle, Washington 98103, USA.",
      Keywords:="Algorithms
Archaeal Proteins/chemistry/metabolism
Bacteriophage lambda/physiology
Computational Biology/*methods
Halobacterium/chemistry/cytology/physiology
Internet
*Models, Biological
*Neural Networks (Computer)
Phenotype
Software/*trends
*Software Design
Stochastic Processes", PubMed:=14597658, ISSN:="1088-9051 (Print);
1088-9051 (Linking)")>
Public Module ShellScriptAPI

    <ExportAPI("Export.Metacyc.Pathways")>
    Public Function ExportMetaCycPathways(MetaCyc As DatabaseLoadder, Export As String) As Integer
        Return New Cytoscape.NetworkModel.Pathways(MetaCyc).Export(Export)
    End Function

    <ExportAPI("export.pathway_regulations")>
    Public Sub ExportPathwayRegulations(Regulations As MatchedResult(), metacyc As DatabaseLoadder, Export As String)
        Dim analysis As New Cytoscape.NetworkModel.PathwayRegulation(metacyc)
        Call analysis.AnalysisMetaPathwayRegulations(Export, Regulations)
    End Sub

    <ExportAPI("Read.Xml.CytoscapeGraph")>
    Public Function LoadNetworkGraphicDocument(Xml As String) As Graph
        Return Graph.Load(Xml)
    End Function

    <ExportAPI("Cytoscape.Graph.Drawing",
               Info:="size is a string expression in format likes <width>,<height>, if this parameter is empty then the system will using the cytoscape network file default size.")>
    Public Function DrawingMap(graph As Graph, Optional size As String = "") As Image
        Dim _size = GraphDrawing.getSize(size)
        Return GraphDrawing.InvokeDrawing(graph, _size)
    End Function

    <ExportAPI("Remove.Duplicates")>
    Public Function RemoveDuplicated(graph As Graph) As Graph
        Return graph.DeleteDuplication
    End Function

    <ExportAPI("Cytoscape.Export.KEGG.ReferenceMap")>
    Public Function CreateMapNetworkData(RefMap As ReferenceMapData) As Graph
        Dim Reaction = (From refRxn As ReferenceReaction In RefMap.Reactions
                        Let Orthology As String() = (From obj In RefMap.GetGeneOrthology(refRxn) Select obj.Key.Description).ToArray
                        Select (From xId As String In refRxn.ECNum
                                Select ID = String.Format("[{0}] {1}", xId, refRxn.Entry),
                                    DataModel = refRxn.ReactionModel,
                                    refRxnX = refRxn,
                                    EcNum = xId)).MatrixAsIterator

        Dim Graph As Graph = Graph.CreateObject(RefMap.Name.Replace("<br>", ""), "KEGG reference map data", RefMap.Description.Replace("<br>", ""))
        Graph.ID = RefMap.EntryId
        Graph.LabelTitle = RefMap.Name
        Graph.Nodes = (From rxn In Reaction
                       Let InternalAttr = New DocumentElements.Attribute() {
                           New DocumentElements.Attribute With {
                                .Name = "KEGG_ENTRY",
                                .Type = DocumentElements.Attribute.ATTR_VALUE_TYPE_STRING,
                                .Value = rxn.refRxnX.Entry
                           },
                           New DocumentElements.Attribute With {
                                .Name = "Equation",
                                .Type = DocumentElements.Attribute.ATTR_VALUE_TYPE_STRING,
                                .Value = rxn.refRxnX.Equation
                           },
                           New DocumentElements.Attribute With {
                                .Name = "EC",
                                .Type = DocumentElements.Attribute.ATTR_VALUE_TYPE_STRING,
                                .Value = rxn.EcNum
                           },
                           New DocumentElements.Attribute With {
                                .Name = "def",
                                .Type = DocumentElements.Attribute.ATTR_VALUE_TYPE_STRING,
                                .Value = rxn.refRxnX.Definition
                           },
                           New DocumentElements.Attribute With {
                                .Name = "comments",
                                .Type = DocumentElements.Attribute.ATTR_VALUE_TYPE_STRING,
                                .Value = rxn.refRxnX.Comments
                           }
                       }
                       Select New DocumentElements.Node With {
                           .LabelTitle = rxn.ID,
                           .Attributes = InternalAttr
                           }).ToArray.AddHandle

        Graph.Edges = (From rxn In Reaction
                       Let Edges = (From target In Reaction
                                    Let Compound As String() = (From source In rxn.DataModel.Products
                                                                Where target.DataModel.GetCoEfficient(source.Identifier) < 0
                                                                Select source.Identifier).ToArray
                                    Where Not Compound.IsNullOrEmpty
                                    Select New DocumentElements.Edge With {
                                        .source = Graph.GetNode(rxn.ID).IDPointer,
                                        .target = Graph.GetNode(target.ID).IDPointer,
                                        .Label = Compound.First}).ToArray
                       Select Edges).ToArray.MatrixToVector.AddHandle '从rxn的右边到target的左边形成一条边
        Return Graph
    End Function

    <ExportAPI("Cytoscape.Drawing.KEGG.refMap",
               Info:="size is a string expression in format likes <width>,<height>, if this parameter is empty then the system will using the cytoscape network file default size.")>
    Public Function DrawingMap(graph As Graph, refMap As ReferenceMapData, Mapping As String(), Optional size As String = "") As Image
        Return GraphDrawing.InvokeDrawing(graph, refMap, Mapping, size)
    End Function
End Module