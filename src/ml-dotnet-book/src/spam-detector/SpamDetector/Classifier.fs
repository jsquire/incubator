﻿namespace MachineLearningBook.SpamDetector
    module Classifier =

        open MachineLearningBook.SpamDetector.Tokenizer

        /// Data that has been grouped and measured by token 
        type TokenGrouping = {
            ProportionOfData : float;
            TokenFrequencies : Map<Token,float>
        }

        /// Constructs assoiated wtih a naive Bayes classification
        module NaiveBayes =

            let private calculateProportion count total = 
                (float count) / (float total)

            let private laplace count total =
                (float (count + 1)) / (float (total + 1))

            let private countTokensIn (group : seq<TokenizedData>) (token : Token) =
                group
                |> Seq.filter (Set.contains token)
                |> Seq.length

            let private tokenScore (group : TokenGrouping) (token : Token) =
                match group.TokenFrequencies.TryFind(token) with
                | Some frequency -> log frequency 
                | None           -> 0.0

            
            /// Analyzes a set of tokenized documents to gain an understandng of the proportions to which tokens appear in them
            let analyzeDataSet (tokenizedDataSet : seq<TokenizedData>) (totalDataElementCount : int) (classificationTokens : Set<Token>) =
                let dataElementWithTokensCount = tokenizedDataSet |> Seq.length
                                
                let calculateScore token = 
                    laplace (countTokensIn tokenizedDataSet token) dataElementWithTokensCount

                let scoredTokens =
                    classificationTokens
                    |> Set.map (fun token -> token, (calculateScore token))
                    |> Map.ofSeq                    

                // TokenGrouping
                { 
                    ProportionOfData = (calculateProportion dataElementWithTokensCount totalDataElementCount) ; 
                    TokenFrequencies = scoredTokens 
                }


            /// Classifies a set of data by considering it against a grouping of measured tokens
            let classify<'TData, 'TGroupBy> (groups    : seq<('TGroupBy * TokenGrouping)>) 
                                            (tokenizer : Tokenizer<'TData>) 
                                            (data      : 'TData) =
                let tokenized = tokenizer data
                
                let calculateScore (data : TokenizedData) (group : TokenGrouping) =
                    log (group.ProportionOfData + (data |> Seq.sumBy (tokenScore group)))

                match groups with
                | empty when Seq.isEmpty empty -> 
                    None

                | groups ->                        
                    Some (
                        groups
                        |> Seq.maxBy (fun (_, group) -> calculateScore tokenized group)
                        |> fst
                    )

            
