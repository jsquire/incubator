﻿namespace MachineLearningBook.SpamDetector.UnitTests
    module ClassifierTests =

        open Microsoft.FSharp.Reflection
        open MachineLearningBook.SpamDetector
        open DataSet
        open Tokenizer
        open Classifier
        open Xunit
        open FsUnit.Xunit  

        //
        // Naive Bayes Analyze tests
        //
        type ``The Naive Bays analysis should produce the expected results`` () =
            
            static member DataSetProportionMemberData 
                with get() = 
                    let singleSet = [| Set.empty.Add("blah") |] |> Seq.ofArray
                    let doubleSet = [| Set.empty.Add("blah"); Set.empty.Add("blah") |] |> Seq.ofArray
                    
                    seq<obj[]> {
                        yield [| singleSet; 1;  1.0        |]
                        yield [| doubleSet; 2;  1.0        |]
                        yield [| singleSet; 2;  0.5        |]
                        yield [| doubleSet; 4;  0.5        |]
                        yield [| singleSet; 3; (1.0 / 3.0) |]
                        yield [| doubleSet; 3; (2.0 / 3.0) |]
                    }


            [<Fact>]
            member verify.``If there were no total elements in the data set, then it should have no proportion`` () =               
                let tokenizedDataSet     = [| Set.empty.Add("blah") |] |> Seq.ofArray
                let totalDataSetElements = 0
                let tokens               = [| "test"; "data" |] |> Set.ofArray
                let result               = NaiveBayes.analyzeDataSet tokenizedDataSet totalDataSetElements tokens

                result.ProportionOfData |> should equal 0.0
                result.TokenFrequencies |> should haveCount tokens.Count

            
            [<Fact>]
            member verify.``An empty data set should have no proportion`` () =               
                let tokenizedDataSet     = Set.empty
                let totalDataSetElements = 100
                let tokens               = [| "test"; "data" |] |> Set.ofArray
                let result               = NaiveBayes.analyzeDataSet tokenizedDataSet totalDataSetElements tokens

                result.ProportionOfData |> should equal 0.0
                result.TokenFrequencies |> should haveCount tokens.Count


            [<Theory>]
            [<MemberData("DataSetProportionMemberData")>]
            member verify.``The proportion represents the set elements against all elements`` (dataSet            : seq<TokenizedData>) 
                                                                                              (totalDataElements  : int) 
                                                                                              (expectedProportion : float) =               
                
                let tokens = [| "test"; "data" |] |> Set.ofArray
                let result = NaiveBayes.analyzeDataSet dataSet totalDataElements tokens

                result.ProportionOfData |> should be (equalWithin 0.05 expectedProportion) 
                result.TokenFrequencies |> should haveCount tokens.Count


            [<Fact>]
            member verify.``An empty data set returns minimum weighted tokens`` () =               
                let tokenizedDataSet     = Set.empty
                let totalDataSetElements = 100
                let tokens               = [| "test"; "data" |] |> Set.ofArray
                let result               = NaiveBayes.analyzeDataSet tokenizedDataSet totalDataSetElements tokens

                result.TokenFrequencies |> should haveCount tokens.Count
                
                // Token frequencies are smoothed, which will eliminate the chance of a 0% frequency.  Instead, they will
                // apppear in proportion to the number of tokens in the classification set.  
                //
                // Calculate the minumum frequency for the single token that appears in no elements.

                let minFrequency = (1.0 / (float (tokens |> Set.count)))

                result.TokenFrequencies 
                |> Seq.filter (fun pair -> pair.Value > minFrequency)
                |> should be Empty

         
            [<Fact>]
            member verify.``Tokens that appear in no data set should have minimum frequency`` () =               
                let noSetToken           = "never"
                let tokenizedDataSet     = [| Set.empty.Add("blah") |] |> Seq.ofArray
                let totalDataSetElements = tokenizedDataSet |> Seq.length
                let tokens               = Set.empty.Add(noSetToken);
                let result               = NaiveBayes.analyzeDataSet tokenizedDataSet totalDataSetElements tokens

                result.TokenFrequencies |> should haveCount tokens.Count
                result.TokenFrequencies.[noSetToken] |> should be (lessThanOrEqualTo 0.5)
            

            [<Fact>]
            member verify.``Token frequencies for a token in all elements and a token in no elements should reflect the minimum and maximum frequencies, respectively `` () =               
                let expectedToken        = "test"    
                let noSetToken           = "never"
                let tokenizedDataSet     = [| Set.empty.Add(expectedToken) |] |> Seq.ofArray                
                let totalDataSetElements = 10
                let tokens               = [| expectedToken; noSetToken |] |> Set.ofArray
                let result               = NaiveBayes.analyzeDataSet tokenizedDataSet totalDataSetElements tokens

                result.TokenFrequencies |> should haveCount tokens.Count

                // Token frequencies are smoothed, which will eliminate the chance of a 0% frequency.  Instead, they will
                // apppear in proportion to the number of tokens in the classification set.  
                //
                // Calculate the minumum frequency for the single token that appears in no elements.

                let minFrequency = (1.0 / (float (tokens |> Set.count)))

                result.TokenFrequencies 
                |> Seq.filter (fun pair -> pair.Value > minFrequency)
                |> Seq.length 
                |> should equal 1                

                // The expected token appears in all elements and should have a frequency of 100%

                result.TokenFrequencies 
                |> Seq.filter (fun pair -> pair.Value = 1.0)
                |> Seq.length
                |> should equal 1


            [<Fact>]
            member verify.``Token frequencies should reflect the number of data elements they appear in`` () =               
                let everySetToken        = "test"
                let oneSetToken          = "other"
                let noSetToken           = "never"
                let baseTokenSet         = Set.empty.Add(everySetToken)
                let tokenizedDataSet     = [| baseTokenSet; baseTokenSet.Add(oneSetToken) |] |> Seq.ofArray                
                let totalDataSetElements = tokenizedDataSet |> Seq.length
                let tokens               = [| everySetToken; oneSetToken; noSetToken |] |> Set.ofArray
                let result               = NaiveBayes.analyzeDataSet tokenizedDataSet totalDataSetElements tokens
                
                result.TokenFrequencies |> should haveCount tokens.Count

                // Token frequencies are smoothed, which will eliminate the chance of a 0% frequency.  Instead, they will
                // apppear in proportion to the number of tokens in the classification set.  
                //
                // Calculate the minumum frequency for the single token that appears in no elements.

                let minFrequency = (1.0 / (float (tokens |> Set.count)))

                // Each token in the set should have at least the minimum frequency.
                
                result.TokenFrequencies 
                |> Seq.filter (fun pair -> pair.Value >= minFrequency)
                |> Seq.length 
                |> should equal 3

                // Because two tokens appear in at least one element, they should have at least 50% frequency.

                result.TokenFrequencies 
                |> Seq.filter (fun pair -> pair.Value > 0.50)
                |> Seq.length 
                |> should equal 2               

                // One token appears in all elements and should have a frequency of 100%

                result.TokenFrequencies 
                |> Seq.filter (fun pair -> pair.Value = 1.0)
                |> Seq.length
                |> should equal 1

        //
        // Naive Bayes Transform tests
        //
        type ``The Naive Bays data transformation should produce the expected results`` () =
            
            static member DataTransformationLengthMemberData
                with get() =                                         
                    seq<obj[]> {
                        yield [| [ (DocType.Ham, "this is a test")];                                                                                          1 |]
                        yield [| [ (DocType.Ham, "this is a test"); (DocType.Spam, "Another one") ];                                                          2 |]
                        yield [| [ (DocType.Ham, "this is a test"); (DocType.Spam, "Another one"); (DocType.Spam, "A thired") ];                              2 |]
                        yield [| [ (DocType.Ham, "this is a test"); (DocType.Spam, "Another one"); (DocType.Spam, "A thired"); (DocType.Spam, "A fourth" ) ]; 2 |]
                        yield [| list<DocType * string>.Empty;                                                                                                0 |]
                    }


            static member DataTransformationResultLabelsMemberData
                with get() =                                         
                    seq<obj[]> {
                        yield [| [ (DocType.Ham, "this is a test")];                                                                                          [ DocType.Ham ]               |]
                        yield [| [ (DocType.Ham, "this is a test"); (DocType.Spam, "Another one") ];                                                          [ DocType.Ham; DocType.Spam ] |]
                        yield [| [ (DocType.Ham, "this is a test"); (DocType.Spam, "Another one"); (DocType.Spam, "A thired") ];                              [ DocType.Ham; DocType.Spam ] |]
                        yield [| [ (DocType.Ham, "this is a test"); (DocType.Spam, "Another one"); (DocType.Spam, "A thired"); (DocType.Spam, "A fourth" ) ]; [ DocType.Ham; DocType.Spam ] |]
                        yield [| [ (DocType.Ham, "this is a test"); (DocType.Ham, "Another one"); (DocType.Ham, "A thired"); (DocType.Ham, "A fourth" ) ];    [ DocType.Ham]                |]
                        yield [| List<DocType * string>.Empty;                                                                                                List<DocType>.Empty           |]
                    }

            static member DataTransformationResultTokensMemberData
                with get() =                                         
                    seq<obj[]> {
                        yield [| 
                            [ (DocType.Ham, "this is a test") ]; 
                            [ "test"; "one" ];
                            Map.empty.Add(DocType.Ham, [ ("test", 1.0); ("one", 0.5) ]) 
                        |]
                        
                        yield [| 
                            [ (DocType.Ham, "this is a test"); (DocType.Spam, "Another test one") ]; 
                            [ "test"; "one" ];
                            Map.empty.Add(DocType.Ham, [ ("test", 1.0); ("one", 0.5) ]).Add(DocType.Spam, [ ("test", 1.0); ("one", 1.0)  ]) 
                        |]

                        yield [| 
                            [ (DocType.Ham, "this is a test"); (DocType.Spam, "Another test one"); (DocType.Spam, "Last test") ]; 
                            [ "test"; "one" ];
                            Map.empty.Add(DocType.Ham, [ ("test", 1.0); ("one", 0.5) ]).Add(DocType.Spam, [ ("test", 1.0); ("one", 0.5)  ]) 
                        |]

                        yield [| 
                            [ (DocType.Ham, "this is a test"); (DocType.Spam, "Another test one"); (DocType.Spam, "Last test"); (DocType.Spam, "one last") ]; 
                            [ "test"; "one" ];
                            Map.empty.Add(DocType.Ham, [ ("test", 1.0); ("one", 0.5) ]).Add(DocType.Spam, [ ("test", 0.66); ("one", 0.66) ]) 
                        |]

                        yield [| 
                            [ (DocType.Ham, "this is a test"); (DocType.Spam, "Another test one"); (DocType.Spam, "Last test"); (DocType.Spam, "nothing here") ]; 
                            [ "test"; "one" ];
                            Map.empty.Add(DocType.Ham, [ ("test", 1.0); ("one", 0.5) ]).Add(DocType.Spam, [ ("test", 0.66); ("one", 0.33) ]) 
                        |]
                        
                    }


            [<Fact>]
            member verify.``An empty set of data produces an empty result`` () =
                let empty  = List.empty<(DocType * string)>
                let tokens = Set.empty.Add("One").Add("Two") 
                let result = NaiveBayes.transformData empty Tokenizer.wordBreakTokenizer tokens

                result |> should be Empty


            [<Fact>]
            member verify.``An empty set of classification tokens should produce no token results`` () =
                let data   = [ (DocType.Ham, "this is a test"); (DocType.Spam, "Another one") ]
                let tokens = Set.empty 
                let result = NaiveBayes.transformData data Tokenizer.wordBreakTokenizer tokens
                                
                let tokenGroups = 
                    result 
                    |> Seq.map snd
                    |> Seq.filter (fun group -> (not (Map.isEmpty group.TokenFrequencies)))

                tokenGroups |> should be Empty

            [<Theory>]
            [<MemberData("DataTransformationLengthMemberData")>]
            member verify.``A data set produces the expected result length`` (inputSet       : List<DocType * string>) 
                                                                             (expectedLength : int)   =                
                let tokens = Set.empty.Add("One").Add("Two") 
                let result = NaiveBayes.transformData inputSet Tokenizer.wordBreakTokenizer tokens
                
                (result |> Seq.length) |> should equal expectedLength



            [<Theory>]
            [<MemberData("DataTransformationResultLabelsMemberData")>]
            member verify.``A data set produces the expected result labels`` (inputSet       : List<DocType * string>) 
                                                                             (expectedLabels : List<DocType>) =                
                let tokens = Set.empty.Add("One").Add("Two") 
                let result = NaiveBayes.transformData inputSet Tokenizer.wordBreakTokenizer tokens
                
                let resultLabels =
                    result 
                    |> Seq.map (fun item -> (fst item))
                    |> List.ofSeq

                
                resultLabels |> should matchList expectedLabels

            
            [<Fact>]
            member verify.``A data set with single label and no tokens produces the expected result tokens`` () =                
                let docType  = DocType.Ham                
                let tokens   = (Set.empty.Add("One").Add("Two") |> Set.map (fun item -> item.ToLowerInvariant ()))
                let inputSet = [ (docType, "This has no token")]; 
                let result   = NaiveBayes.transformData inputSet Tokenizer.wordBreakTokenizer tokens

                let groupings = 
                    result                         
                    |> Seq.map snd
                    |> Array.ofSeq

                groupings |> should haveLength 1

                let grouping = groupings.[0]

                grouping.ProportionOfData |> should equal 1.0
                grouping.TokenFrequencies |> should haveCount 2
                
                grouping.TokenFrequencies 
                |> Map.toList                
                |> List.map fst
                |> should matchList (tokens |> Set.toList)

                grouping.TokenFrequencies
                |> Map.toList
                |> List.fold (fun acc item -> acc + (snd item)) 0.0
                |> should (equalWithin 0.25) 1.0
                

            [<Fact>]
            member verify.``A data set with single label and token produces the expected result tokens`` () =                
                let docType  = DocType.Ham
                let token    = "One".ToLowerInvariant ()
                let tokens   = Set.empty.Add(token) 
                let inputSet = [ (docType, (sprintf "This has %s" token)) ]; 
                let result   = NaiveBayes.transformData inputSet Tokenizer.wordBreakTokenizer tokens

                let groupings = 
                    result                         
                    |> Seq.map snd
                    |> Array.ofSeq

                groupings |> should haveLength 1

                let grouping = groupings.[0]

                grouping.ProportionOfData |> should equal 1.0
                grouping.TokenFrequencies |> should haveCount 1
                grouping.TokenFrequencies.ContainsKey(token) |> should be True

                grouping.TokenFrequencies
                |> Map.toList
                |> List.fold (fun acc item -> acc + (snd item)) 0.0
                |> should (equalWithin 0.25) 1.0

            
            [<Fact>]
            member verify.``A data set with two labels and tokens produces the expected result tokens`` () =                                
                let token      = "One".ToLowerInvariant ()
                let otherToken = "Two".ToLowerInvariant ()
                let tokens     = Set.empty.Add(token).Add(otherToken)
                let inputSet   = [ (DocType.Ham, (sprintf "This has %s" token)); (DocType.Spam, (sprintf "This has %s and %s" token otherToken)) ]
                let result     = NaiveBayes.transformData inputSet Tokenizer.wordBreakTokenizer tokens

                let groupings = 
                    result                         
                    |> Map.ofSeq

                groupings |> should haveCount 2

                // Examine the Ham 

                groupings.ContainsKey(DocType.Ham) |> should be True

                groupings.[DocType.Ham].ProportionOfData |> should equal 0.5
                groupings.[DocType.Ham].TokenFrequencies |> should haveCount 2
                groupings.[DocType.Ham].TokenFrequencies.ContainsKey token |> should be True
                groupings.[DocType.Ham].TokenFrequencies.ContainsKey otherToken |> should be True

                groupings.[DocType.Ham].TokenFrequencies.[token] |> should (equalWithin 0.1) 1.0
                groupings.[DocType.Ham].TokenFrequencies.[otherToken] |> should (equalWithin 0.1) 0.5

                // Examine the Spam 

                groupings.ContainsKey(DocType.Spam) |> should be True

                groupings.[DocType.Spam].ProportionOfData |> should equal 0.5
                groupings.[DocType.Spam].TokenFrequencies |> should haveCount 2
                groupings.[DocType.Spam].TokenFrequencies.ContainsKey(token) |> should be True
                groupings.[DocType.Spam].TokenFrequencies.ContainsKey(otherToken) |> should be True

                groupings.[DocType.Spam].TokenFrequencies.[token] |> should (equalWithin 0.1) 1.0
                groupings.[DocType.Spam].TokenFrequencies.[otherToken] |> should (equalWithin 0.1) 1.0

            
            [<Theory>]
            [<MemberData("DataTransformationResultTokensMemberData")>]
            member verify.``Data set transformation produces the expected result tokens``(inputSet : List<DocType * string>)  
                                                                                         (tokens   : List<Token>)
                                                                                         (expected : Map<DocType, List<Token * float>>) =                  
                let docTypeInput = 
                    inputSet
                    |> List.map fst
                    |> Set.ofList 


                let result = 
                    NaiveBayes.transformData inputSet Tokenizer.wordBreakTokenizer (tokens |> Set.ofList)
                    |> Map.ofSeq


                // The result set should have grouped the input by its DocType; verify that there are as many
                // result items as there were unique DocType members in the input.

                result |> should haveCount docTypeInput.Count

                // Each DocType from the input set should be represented in the result set.

                docTypeInput
                |> Set.filter (fun item -> not (result.ContainsKey item))
                |> should be Empty

                // The result set should contain the set of tokens in each DocType mapping, with the expected
                // proportions (within a fuzzy match due to laplace smoothing)
                
                for docType in docTypeInput do                    
                    let resultItem          = result.[docType]
                    let expectedFrequencies = (expected.[docType] |> Map.ofList)
                    let inputCountOfDocType = (inputSet |> List.filter(fun item -> (fst item) = docType) |> List.length)
                    let expectedProportion  = ((float inputCountOfDocType) / (float inputSet.Length))

                    resultItem.ProportionOfData |> should (equalWithin 0.1) expectedProportion

                    tokens
                    |> List.filter (fun token -> not (resultItem.TokenFrequencies.ContainsKey token))
                    |> should be Empty

                    for token in tokens do
                        resultItem.TokenFrequencies.[token] |> should (equalWithin 0.19) expectedFrequencies.[token]

        
        //
        // Naive Bayes Classification tests
        //
        type ``The Naive Bays classification should produce the expected results`` () =

            [<Fact>]
            member verify.``An empty group returns no result`` () =               
                let groups    = Seq.empty<_ * TokenGrouping>        
                let data      = "something"
                let tokenizer = (fun target -> Set.empty<Tokenizer.Token>.Add data)
                let result    = NaiveBayes.classify groups tokenizer data

                result.IsNone |> should be True


            [<Fact>]
            member verify.``An single group returns the only element`` () =
                let group = {
                    ProportionOfData = 0.0;
                    TokenFrequencies = Map.empty<Tokenizer.Token,float>
                }

                let expected  = DataSet.DocType.Ham
                let groups    = [| (expected, group) |]                
                let data      = "something"
                let tokenizer = (fun target -> Set.empty<Tokenizer.Token>.Add data)
                let result    = NaiveBayes.classify groups tokenizer data

                result.IsSome |> should be True
                result.Value  |> should equal expected


            [<Fact>]
            member verify.``The item with the highest frequency group is returned`` () =
                let data = "something"

                let lowGroup = {
                    ProportionOfData = 50.0;
                    TokenFrequencies = [| (data, 20.0) |] |> Map.ofSeq
                }


                let highGroup = {
                    ProportionOfData = 50.0;
                    TokenFrequencies = [| (data, 80.0) |] |> Map.ofSeq
                }


                let expected  = 65
                let groups    = [| (99, lowGroup); (expected, highGroup) |]                                
                let tokenizer = (fun target -> Set.empty<Tokenizer.Token>.Add data)
                let result    = NaiveBayes.classify groups tokenizer data

                result.IsSome |> should be True
                result.Value  |> should equal expected


            [<Fact>]
            member verify.``Unknown tokens do not influence the classification`` () =
                let data    = "something"
                let unknown = "unknown"

                let lowGroup = {
                    ProportionOfData = 50.0;
                    TokenFrequencies = [| (data, 20.0); (unknown, 50.0) |] |> Map.ofSeq
                }


                let highGroup = {
                    ProportionOfData = 50.0;
                    TokenFrequencies = [| (data, 80.0); (unknown, 90.0) |] |> Map.ofSeq
                }


                let expected  = DataSet.DocType.Ham
                let groups    = [| (DataSet.DocType.Spam, lowGroup); (expected, highGroup) |]                                
                let tokenizer = (fun target -> Set.empty<Tokenizer.Token>.Add data)
                let result    = NaiveBayes.classify groups tokenizer data

                result.IsSome |> should be True
                result.Value  |> should equal expected


        //
        // Naive Bayes Training tests
        //
        type ``Training of the Naive Bays Classifier should produce the expected results`` () =            

            [<Fact>]
            member verify.``Training with an empty group returns a valid classifier`` () =               
                let groups            = Seq.empty<_ * TokenGrouping>        
                let data              = "something"
                let tokens            = Set.empty<Tokenizer.Token>.Add data
                let tokenizer         = (fun target -> tokens)
                let classifier        = NaiveBayes.train groups tokenizer tokens                
                let classifierType    = classifier.GetType();                
                                
                FSharpType.IsFunction (classifierType) |> should be True


            [<Fact>]
            member verify.``Training with a single group returns a classifier that identifies tokens in the group`` () =               
                let docType           = DocType.Spam
                let data              = "something"
                let groups            = seq { yield (docType, data) }                
                let tokens            = Set.empty<Tokenizer.Token>.Add data
                let tokenizer         = (fun target -> tokens)
                let classifier        = NaiveBayes.train groups tokenizer tokens                
                let result            = classifier data

                result.IsSome |> should be True
                result.Value  |> should equal docType


            [<Fact>]
            member verify.``Training with a single group returns a classifier that can predict tokens not in the group`` () =               
                let docType           = DocType.Ham
                let data              = "something"
                let groups            = seq { yield (docType, data) }                
                let tokens            = Set.empty<Tokenizer.Token>.Add data
                let tokenizer         = (fun target -> tokens)
                let classifier        = NaiveBayes.train groups tokenizer tokens                
                let result            = classifier "unknown"

                result.IsSome |> should be True


            [<Fact>]
            member verify.``Training with multiple groups returns a classifier that identifies tokens in the group`` () =                               
                let docType = DocType.Spam
                let data    = "something"
                
                let groups = seq { 
                    yield (docType,     (sprintf "This item has %s in it" data))
                    yield (docType,     (sprintf "So does this one.  %s is here" data))
                    yield (DocType.Ham, (sprintf "%s is here too" data))
                    yield (DocType.Ham, ("It doesn't show up here, so the other type wins"))
                }                   

                let tokens            = Set.empty<Tokenizer.Token>.Add data
                let tokenizer         = (fun target -> tokens)
                let classifier        = NaiveBayes.train groups tokenizer tokens                
                let result            = classifier data

                result.IsSome |> should be True
                result.Value  |> should equal docType


            [<Fact>]
            member verify.``Training with multiple groups group returns a classifier that can predict tokens not in the group`` () =               
                let docType = DocType.Spam
                let data    = "something"
                
                let groups = seq { 
                    yield (docType,     (sprintf "This item has %s in it" data))
                    yield (docType,     (sprintf "So does this one.  %s is here" data))
                    yield (DocType.Ham, (sprintf "%s is here too" data))
                    yield (DocType.Ham, ("It doesn't show up here, so the other type wins"))
                } 
                
                let tokens            = Set.empty<Tokenizer.Token>.Add data
                let tokenizer         = (fun target -> tokens)
                let classifier        = NaiveBayes.train groups tokenizer tokens                
                let result            = classifier "unknown"

                result.IsSome |> should be True
