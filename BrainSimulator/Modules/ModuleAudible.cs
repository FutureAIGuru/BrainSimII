//
// Copyright (c) [Name]. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace BrainSimulator.Modules
{
    public class ModuleAudible : ModuleBase
    {
        List<Thing> shortTermMemoryList = new List<Thing>(); //order of words recently received in order to match existing phrases
        public int wordCount = 0;
        public int phraseCount = 0;
        public int textCount = 0;
        bool talking = false;
        int maxPhonemesPerWord = 7;
        int maxShortTermMemory = 20;
        const int maxWords = 300;
        const int maxPhrases = 100;

        //this is a temporary list which evaluates expands possible phrases to select the best
        List<List<Thing>> possiblePhrases = new List<List<Thing>>();

        Thing currentText = null;

        public override void Fire()
        {
            Init();  //be sure to leave this here
            if (FindModuleByName("AudibleUKS") is ModuleUKS2 UKS)
            {
                SpeakPhrase(UKS);
                HandlePhonemes(UKS);

                if (GetNeuronValue("Word") == 1)
                    PruneWords(UKS);
                if (GetNeuronValue("Phrase") == 1)
                    PrunePhrases(UKS);
                if (GetNeuronValue("Stext") == 1)
                    currentText = UKS.AddThing("txt" + textCount++, UKS.Labeled("Text"));
                if (GetNeuronValue("Etext") == 1)
                    currentText = null;
            }
        }


        List<Thing> CopyList(List<Thing> l)
        {
            List<Thing> retVal = new List<Thing>();
            foreach (Thing t in l)
                retVal.Add(t);
            return retVal;
        }

        private void HandlePhonemes(ModuleUKS2 UKS)
        {
            List<Thing> phonemes = UKS.AnyChildrenFired(UKS.Labeled("Phoneme"), 1, 0, true, false);
            List<Thing> phrases = UKS.GetChildren(UKS.Labeled("Phrase"));
            List<Thing> words = UKS.GetChildren(UKS.Labeled("Word"));

            //add incoming phonemes to short-term memory and do more processing 
            //if there is a new phoneme, add it to short-term memory
            //TODO: handle phonemes as they come in rather than waiting for a pause
            Debug.Assert(phonemes.Count < 2);
            if (phonemes.Count == 1)
            {
                Thing phoneme = phonemes[0];
                shortTermMemoryList.Add(phoneme);
            }

            if (phonemes.Count == 0 && shortTermMemoryList.Count > 0) //only process if the list isn't empty
            {
                if (talking)
                {
                    talking = false;
                    shortTermMemoryList.Clear();
                    return;
                }

                //  MakeWordOfShortInput(UKS, shortTermMemoryList, words);

                FindPossibleWords(UKS, shortTermMemoryList);

                List<Thing> bestPhrase = FindBestPhrase(UKS);

                ConvertUnmatchedPhonemesToWords(UKS, bestPhrase);

                ExtendWordsWithSinglePhonemes(UKS, bestPhrase, words);

                Thing phrase = AddPhrase(UKS, bestPhrase);

                //add a phrase to the text
                if (currentText != null && phrase != null)
                    currentText.AddReference(phrase);
                //are we matching a text we already know?

                possiblePhrases.Clear();
                shortTermMemoryList.Clear();
            }
        }

        private Thing AddPhrase(ModuleUKS2 UKS, List<Thing> bestPhrase)
        {
            //see if the phrase already exists and add it if not
            Thing phraseFound = UKS.ReferenceMatch(bestPhrase, UKS.GetChildren(UKS.Labeled("Phrase")));
            if (phraseFound == null)// && bestPhrase.Count > 1)
            {
                phraseFound = UKS.AddThing("ph" + phraseCount++, UKS.Labeled("Phrase"), null, bestPhrase.ToArray());
            }
            foreach (Thing t in bestPhrase) t.useCount++;
            if (phraseFound != null) phraseFound.useCount++;
            return phraseFound;
        }

        //this substitutes a cluster of phonemes in a phrase for a word
        private bool ReplaceWordInPhrase(List<Thing> phrase, Thing word)
        {
            bool retVal = false;
            for (int i = 0; i <= phrase.Count - word.References.Count; i++)
            {
                if (phrase[i] == word) continue;  //the word is already in the phrase
                for (int j = 0; j < word.References.Count & i + j < phrase.Count; j++)
                {
                    if (word.References[j].T != phrase[i + j])
                    {
                        goto notFound;
                    }
                }
                phrase[i] = word;
                phrase.RemoveRange(i + 1, word.References.Count - 1);
                retVal = true;
                notFound:;
            }
            return retVal;
        }

        private void MakeWordOfShortInput(ModuleUKS2 UKS, List<Thing> phrase, List<Thing> words)
        {
            if (phrase.Count < maxPhonemesPerWord)
            {
                if (UKS.ReferenceMatch(phrase, words) == null)
                    UKS.AddThing("w" + wordCount++, UKS.Labeled("Word"), null, phrase.ToArray());
            }
        }

        //this searches the current input stream to see if any known words match
        //then it builds a list of all possible phrases which can be made from known words and leaves unknown phonemes in place
        //this addresses the problem of words which incorporate other words and word splits which may be ambiguous;
        // Ann, Can, Cant    
        private void FindPossibleWords(ModuleUKS2 UKS, List<Thing> phrase)
        {
            possiblePhrases.Clear();
            List<Thing> words = UKS.GetChildren(UKS.Labeled("Word"));
            List<Thing> inputPhrase = CopyList(phrase);
            List<Thing> possibleWords = new List<Thing>();

            //find all possible words then see if combinations can fill the phrase?
            for (int i = 0; i < inputPhrase.Count; i++)
            {
                //the order here is important as it interacts with the next loop 
                for (int j = inputPhrase.Count - i; j > 0; j--)
                {
                    List<Thing> phonemeSequence = inputPhrase.GetRange(i, j);
                    Thing foundWord = UKS.ReferenceMatch(phonemeSequence, words);
                    if (foundWord != null)
                    {
                        if (!possibleWords.Contains(foundWord))
                            possibleWords.Add(foundWord);
                    }
                }
            }

            //replace a sequence of phonemes in the phrase with a word
            //failures are words we found which can't be put in phrase in combiination with others
            //that's why we end up with multiple candidate phrases
            List<Thing> failures = new List<Thing>();
            possibleWords = possibleWords.OrderByDescending(x => x.References.Count).ToList();
            foreach (Thing word in possibleWords)
            {
                if (!ReplaceWordInPhrase(inputPhrase, word))
                    failures.Add(word);
            }
            possiblePhrases.Add(inputPhrase);
            foreach (Thing word in failures)
            {
                inputPhrase = CopyList(phrase);
                ReplaceWordInPhrase(inputPhrase, word);
                foreach (Thing word1 in possibleWords)
                {
                    ReplaceWordInPhrase(inputPhrase, word1);
                }
                possiblePhrases.Add(inputPhrase);
            }
        }


        private void ExtendWordsWithSinglePhonemes(ModuleUKS2 UKS, List<Thing> bestPhrase, List<Thing> words)
        {
            //if the phrase contains any single phonemes, append/prepend them to adjascent words
            for (int i = 0; i < bestPhrase.Count; i++)
            {
                if (bestPhrase[i].Parents[0] == UKS.Labeled("Phoneme"))
                {
                    bool preceeding = false;
                    bool following = false;
                    if (i == 0 || bestPhrase[i - 1].Parents[0] == UKS.Labeled("Word"))
                        preceeding = true;
                    if (i == bestPhrase.Count - 1 || bestPhrase[i + 1].Parents[0] == UKS.Labeled("Word"))
                        following = true;
                    if (preceeding && following)
                    {
                        //create new word merged with preceeding word
                        Thing newWordExtended = null;
                        Thing newWordPrepended = null;
                        if (i != 0)
                        {
                            Thing baseWord = bestPhrase[i - 1];
                            List<Thing> newRefs = new List<Thing>();
                            foreach (Link l in baseWord.References)
                                newRefs.Add(l.T);
                            newRefs.Add(bestPhrase[i]);
                            Thing t = UKS.ReferenceMatch(newRefs, words);
                            if (t == null)
                                newWordExtended = UKS.AddThing("w" + wordCount++, UKS.Labeled("Word"), null, newRefs.ToArray());
                        }
                        //create new word merged with following word
                        if (i != bestPhrase.Count - 1)
                        {
                            Thing baseWord = bestPhrase[i + 1];
                            List<Thing> newRefs = new List<Thing>();
                            newRefs.Add(bestPhrase[i]);
                            foreach (Link l in baseWord.References)
                                newRefs.Add(l.T);
                            Thing t = UKS.ReferenceMatch(newRefs, words);
                            if (t == null)
                                newWordPrepended = UKS.AddThing("w" + wordCount++, UKS.Labeled("Word"), null, newRefs.ToArray());
                        }
                        if (newWordExtended != null)
                        {
                            bestPhrase.RemoveRange(i - 1, 2);
                            bestPhrase.Insert(i - 1, newWordExtended);
                        }
                        else if (newWordPrepended != null)
                        {
                            bestPhrase.RemoveRange(i, 2);
                            bestPhrase.Insert(i, newWordPrepended);
                        }
                    }
                }
            }
        }

        //count only the recognized words in a phrase
        int GetWordCount(ModuleUKS2 UKS, List<Thing> phrase)
        {
            return phrase.Count(x => x.Parents[0] == UKS.Labeled("Word"));
        }

        private void ConvertUnmatchedPhonemesToWords(ModuleUKS2 UKS, List<Thing> bestPhrase)
        {
            //convert remaining phonemes to word(s) add remainder of phrase as word(s)
            int startOfWord = 0;
            int wordLength = 0;
            int bestWordCount = GetWordCount(UKS, bestPhrase);
            while (startOfWord < bestPhrase.Count)
            {
                while (startOfWord + wordLength < bestPhrase.Count && bestPhrase[startOfWord + wordLength].Parents[0] != UKS.Labeled("Word"))
                {
                    wordLength++;
                }
                if ((wordLength > 1 && wordLength < maxPhonemesPerWord) ||
                        (wordLength == 1 && bestPhrase.Count == 1)) //only add a single-phoneme word if it's the only word in the phrase
                {
                    List<Thing> newRefs = bestPhrase.GetRange(startOfWord, wordLength);
                    Thing newWord = UKS.AddThing("w" + wordCount++, UKS.Labeled("Word"), null, newRefs.ToArray());
                    ReplaceWordInPhrase(bestPhrase, newWord);
                    startOfWord++;
                }
                else
                    startOfWord += wordLength + 1;
                wordLength = 0;
            }
        }

        private List<Thing> FindBestPhrase(ModuleUKS2 UKS)
        {
            //find best phrase... shortest or (if equal) with more detected words
            List<Thing> bestPhrase = possiblePhrases[0];
            int bestUseCount = bestPhrase.Sum(x => x.useCount);
            int bestWordCount = bestPhrase.Count(x => x.Parents[0] == UKS.Labeled("Word"));
            int bestPhonemeCount = bestPhrase.Count(x => x.Parents[0] == UKS.Labeled("Phoneme"));
            for (int i = 1; i < possiblePhrases.Count; i++)
            {
                List<Thing> nextPossible = possiblePhrases[i];
                int useCount = nextPossible.Sum(x => x.useCount);
                int wordCount = nextPossible.Count(x => x.Parents[0] == UKS.Labeled("Word"));
                int phonemeCount = nextPossible.Count(x => x.Parents[0] == UKS.Labeled("Phoneme"));

                if (wordCount > bestWordCount)
                {
                    bestPhrase = nextPossible;
                    bestWordCount = wordCount;
                    bestUseCount = useCount;
                }
                else if (wordCount == bestWordCount && useCount > bestUseCount)
                {
                    bestPhrase = nextPossible;
                    bestWordCount = wordCount;
                    bestUseCount = useCount;
                }
            }

            return bestPhrase;
        }

        private void PruneWords(ModuleUKS2 UKS)
        {
            List<Thing> words = UKS.GetChildren(UKS.Labeled("Word"));

            if (false) return;
            if (words.Count < maxWords / 2) return;
            List<Thing> sortedWords = (List<Thing>)words.OrderByDescending(x => x.useCount).ToList();

            for (int j = sortedWords.Count / 2; j < sortedWords.Count; j++)
            {
                Thing word = sortedWords[j];

                //remove words with low usecount or not referenced in any phrases
                // if (word.useCount < 1 || word.ReferencedBy.Count == 0)
                {
                    for (int i = 0; i < word.ReferencedBy.Count; i++)
                    {
                        Thing phrase = word.ReferencedBy[i].T;
                        UnReplaceReferences(word, phrase);
                    }
                    UKS.DeleteThing(word);
                }
            }
        }

        //return the location of searchTarget within sequence of -1 if not found
        private int IndexOfSequence(List<Thing> sequence, List<Thing> searchTarget)
        {
            int retVal = -1;
            for (int i = 0; i <= sequence.Count - searchTarget.Count; i++)
            {
                for (int j = 0; j < searchTarget.Count; j++)
                {
                    if (sequence[i + j] != searchTarget[j]) goto NotFound;
                }
                return i;
                NotFound:;
            }
            return retVal;
        }

        //In seq2, replace seq1 at index
        private void ReplaceReferences(Thing phrase1, Thing phrase2, int index)
        {
            phrase2.References[index].T = phrase1;
            for (int k = 0; k < phrase1.References.Count - 1; k++)
            {
                phrase2.RemoveReferenceAt(index + 1);
            }
        }
        private void UnReplaceReferences(Thing phrase1, Thing phrase2)
        {
            for (int i = 0; i < phrase2.References.Count; i++)
            {
                Link l = phrase2.References[i];
                if (l.T == phrase1)
                {
                    phrase2.RemoveReferenceAt(i);
                    for (int j = 0; j < phrase1.References.Count; j++)
                    {
                        phrase2.InsertReferenceAt(i + j, phrase1.References[j].T);
                    }
                }
            }
        }

        //TODO: make this happen when new phrases are added
        private void PrunePhrases(ModuleUKS2 UKS)
        {


            //find phrases which differ by a single word...can this be a grammar exemplar?
            //find phrases which have phonemes in them and see if there are now words to put in them
            List<Thing> phrases = UKS.GetChildren(UKS.Labeled("Phrase"));
            List<Thing> words = UKS.GetChildren(UKS.Labeled("Word"));

            //if a word is usually followed by another specific word, create a new bigger word out of two smaller ones.
            for (int i = 0; i < words.Count; i++)
            {
                Thing word = words[i];
                List<Link> followingWords = new List<Link>();
                int greatestWeight = -1;
                int greatestWeightIndex = -1;
                for (int j = 0; j < word.ReferencedBy.Count; j++)
                {
                    Thing phrase = word.ReferencedBy[j].T;
                    int k = phrase.References.FindIndex(x => x.T == word);
                    Debug.Assert(k >= 0);
                    if (k < phrase.References.Count - 1)
                    {
                        Thing followingWord = phrase.References[k + 1].T;

                        int index = followingWords.FindIndex(x => x.T == followingWord);
                        if (index != -1)
                        {
                            followingWords[index].weight++;
                            if (followingWords[index].weight > greatestWeight)
                            {
                                greatestWeight = (int)followingWords[index].weight;
                                greatestWeightIndex = index;
                            }
                        }
                        else
                        {
                            followingWords.Add(new Link() { T = followingWord, weight = 0 });
                        }
                    }
                }
                if (greatestWeight > 3)
                {

                }
            }




            List<Thing> phrasesWithPhonemes = phrases.FindAll(x => x.References.Any(l => l.T.Parents[0] == UKS.Labeled("Phoneme")));
            foreach (Thing phrase in phrases)//hrasesWithPhonemes)
            {
                possiblePhrases.Clear();
                //convert phrase back to phonemes
                Thing expandedPhrase = new Thing();
                ExpandToClass(expandedPhrase, phrase, UKS.Labeled("Phoneme"));
                FindPossibleWords(UKS, expandedPhrase.ReferencesAsThings);
                List<Thing> bestPhrase = FindBestPhrase(UKS);
                ConvertUnmatchedPhonemesToWords(UKS, bestPhrase);
                ExtendWordsWithSinglePhonemes(UKS, bestPhrase, words);
                int newUseCount = bestPhrase.Sum(x => x.useCount);
                int oldUseCount = phrase.ReferencesAsThings.Sum(x => x.useCount);
                int newWordCount = GetWordCount(UKS, bestPhrase);
                int oldWordCount = GetWordCount(UKS, phrase.ReferencesAsThings);
                if (newWordCount == bestPhrase.Count || newWordCount < oldWordCount || newUseCount > oldUseCount)
                {
                    //replace the references in the phrase
                    while (phrase.References.Count > 0) phrase.RemoveReference(phrase.References[0].T);
                    foreach (Thing t in bestPhrase) phrase.AddReference(t);
                }
            }
            return;

            //find phrases which incorporate other phrases
            for (int i = 0; i < phrases.Count; i++)
            {
                Thing phrase1 = phrases[i];
                for (int j = i + 1; j < phrases.Count; j++)
                {
                    Thing phrase2 = phrases[j];
                    if (phrase1.References.Count < phrase2.References.Count)
                    {
                        int index = IndexOfSequence(phrase2.ReferencesAsThings, phrase1.ReferencesAsThings);
                        if (index > -1)
                        {
                            ReplaceReferences(phrase1, phrase2, index);
                        }
                    }
                    else if (phrase1.References.Count > phrase2.References.Count)
                    {
                        int index = IndexOfSequence(phrase1.ReferencesAsThings, phrase2.ReferencesAsThings);
                        if (index > -1)
                        {
                            phrase1.References[index].T = phrase2;
                            for (int k = 0; k < phrase2.References.Count - 1; k++)
                            {
                                phrase1.RemoveReferenceAt(index + 1);
                            }
                        }
                    }
                    else if (phrase1.References.Count == phrase2.References.Count)
                    {
                        int index = IndexOfSequence(phrase2.ReferencesAsThings, phrase1.ReferencesAsThings);
                        if (index > -1)
                        {
                            UKS.DeleteThing(phrase2);
                        }
                    }
                }
            }

            //search for common subsphrases and convert them into phrases
            //l is the length of commonality we're searching for
            for (int i = 0; i < phrases.Count; i++)
            {
                Thing phrase1 = phrases[i];
                for (int l = phrase1.References.Count - 1; l > 1; l--)
                {
                    for (int offset = 0; offset < phrase1.References.Count - l + 1; offset++)
                    {
                        List<Thing> subRange = phrase1.ReferencesAsThings.GetRange(offset, l).ToList();
                        Thing newPhrase = null;
                        for (int j = i + 1; j < phrases.Count; j++)
                        {
                            Thing phrase2 = phrases[j];
                            int index = IndexOfSequence(phrase2.ReferencesAsThings, subRange);
                            if (index > -1 && phrase2.References.Count > subRange.Count)
                            {
                                if (newPhrase == null)
                                {
                                    newPhrase = UKS.AddThing("ph" + phraseCount++, UKS.Labeled("Phrase"), null, subRange.ToArray());
                                    phrase1.References[offset].T = newPhrase;
                                    for (int k = 0; k < newPhrase.References.Count - 1; k++)
                                    {
                                        phrase1.RemoveReferenceAt(offset + 1);
                                    }
                                }
                                phrase2.References[index].T = newPhrase;
                                for (int k = 0; k < newPhrase.References.Count - 1; k++)
                                {
                                    phrase2.RemoveReferenceAt(index + 1);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void SpeakThing(string label)
        {
            if (FindModuleByName("AudibleUKS") is ModuleUKS2 UKS)
            {
                if (UKS.Labeled(label) is Thing t)
                    SpeakThing(UKS, t);
            }
        }
        //given a thing, say it
        public void SpeakThing(ModuleUKS2 UKS, Thing t)
        {
            talking = true;
            if (t.Parents[0] == UKS.Labeled("Text"))
            {
                for (int i = 0; i < t.References.Count; i++)
                {
                    Thing t1 = t.References[i].T;
                    Thing tOut = new Thing();
                    ExpandToClass(tOut, t1, UKS.Labeled("Phoneme"));
                    //                    tOut.References = tOut.References.GetRange(2000, 200);
                    UKS.Play(tOut);
                }
            }
            else
            {
                Thing tOut = new Thing();
                ExpandToClass(tOut, t, UKS.Labeled("Phoneme"));
                UKS.Play(tOut);
            }
        }

        private void SpeakPhrase(ModuleUKS2 UKS)
        {
            List<Thing> wordsToSpeak = UKS.AnyChildrenFired(UKS.Labeled("Word"), 1, 0, false, true);
            if (wordsToSpeak.Count == 1)
            {
                UKS.Play(wordsToSpeak[0]);
                talking = true;
            }
            wordsToSpeak = UKS.AnyChildrenFired(UKS.Labeled("Phrase"), 1, 0, false, true);
            if (wordsToSpeak.Count == 1)
            {
                SpeakThing(UKS, wordsToSpeak[0]);
            }
        }

        private List<Thing> ExpandToClass(Thing tOut, Thing t, Thing baseParent)
        {
            foreach (Link l in t.References)
            {
                if (l.T.Parents[0] == baseParent)
                {
                    tOut.References.Add(l);
                }
                else
                {
                    ExpandToClass(tOut, l.T, baseParent);
                }
            }
            return tOut.ReferencesAsThings;
        }


        public override void Initialize()
        {
            wordCount = 0;
            phraseCount = 0;
            textCount = 0;
            currentText = null;
            AddLabel("Word");
            AddLabel("Phrase");
            AddLabel("Stext");
            AddLabel("Etext");

            shortTermMemoryList.Clear();
            if (FindModuleByName("AudibleUKS") is ModuleUKS2 UKS)
            {
                UKS.AddThing("Word", "Audible");
                UKS.AddThing("Phrase", "Audible");
                UKS.AddThing("Text", "Audible");
            }
        }
    }
}
