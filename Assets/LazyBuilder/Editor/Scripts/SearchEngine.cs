using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace LazyBuilder
{

    public class SearchEngine
    {
        public static List<Item> FuzzySearch(List<Item> source, string key)
        {
            //return source.Where(x => FuzzyAllMatch(x, key) > 1).OrderByDescending(x => FuzzyIdMatch(x, key))
            //    .ThenByDescending(x => FuzzyTagMatch(x, key)).ToList();

            // Must be word match - then - all char match - then - ID char match

            return source.
                Where(x => CharMatch_Combined(x, key) > 1).
                //OrderByDescending(x => WordMatch(x.Id, key)).
                OrderByDescending(x => WordMatch_Combined(x, key)).
                ThenByDescending(x => WordMatch(x.Id, key)).
                //ThenByDescending(x => WordMatch_Combined(x, key)).
                //ThenByDescending(x => CharMatch_Combined(x, key)).
                //ThenByDescending(x => CharMatch_Id(x, key)).
                ToList();

        }



        public static int WordMatch_Combined(Item source, string key)
        {
            int idMatches = WordMatch(source.Id, key);
            return idMatches + WordMatch_Tag(source, key);
        }

        public static int WordMatch_Tag(Item source, string key)
        {
            int matches = 0;
            List<string> usedWords = new List<string>();
            foreach (var tag in source.Tags)
                matches += WordMatch(tag, key, ref usedWords);
            return matches;
        }

        public static int WordMatch(string source, string key)
        {
            List<string> discard = new List<string>();
            return WordMatch(source, key, ref discard);
        }

        public static int WordMatch(string source, string key, ref List<string> usedWord)
        {
            //Source ex: BigPineTree
            //Key ex: Forest Tree

            if (usedWord == null)
                usedWord = new List<string>();

            int wordMatches = 0;

            source = source.SeparateCase();
            string[] sourceSplitted = source.Split(' ');
            string[] keySplitted = key.Split(' ');

            foreach (var sourceSplit in sourceSplitted)
            {
                if (sourceSplit == "") continue;

                foreach (var keySplit in keySplitted)
                {
                    if (sourceSplit.ToUpper() == keySplit.ToUpper() && !usedWord.Contains(sourceSplit.ToUpper()))
                    {
                        usedWord.Add(keySplit.ToUpper());
                        wordMatches++;
                    }
                }
            }
            return wordMatches;
        }



        public static int CharMatch_Combined(Item source, string key)
        {
            int idMatches = CharMatch(source.Id, key);
            return idMatches + CharMatch_Tag(source, key);
        }

        public static int CharMatch_Id(Item source, string key)
        {
            var idMatch = CharMatch(source.Id, key);
            return idMatch;
        }
        public static int CharMatch_Tag(Item source, string key)
        {
            int matches = 0;
            foreach (var tag in source.Tags)
                matches += CharMatch(tag, key);
            return matches;
        }



        public static int CharMatch(string source, string key)
        {
            int maxMatches = 0, sequenceMatches = 0, i = 0;

            var charactersSource = source.ToCharArray();
            var charactersKey = key.ToCharArray();

            while (i < charactersSource.Length)
            {
                if (sequenceMatches < charactersKey.Length &&
                    sequenceMatches + i < charactersSource.Length
                    && charactersKey[sequenceMatches].Upper() == charactersSource[i + sequenceMatches].Upper())
                    sequenceMatches++;
                else
                {
                    maxMatches = sequenceMatches > maxMatches ? sequenceMatches : maxMatches;
                    sequenceMatches = 0;
                    i++;
                }
            }

            maxMatches = sequenceMatches > maxMatches ? sequenceMatches : maxMatches;

            ////Ignore single character matchs (in case of a multi char word)
            //if (ignoreSingleMatch && key.Length > 1 && maxMatches < 2)
            //    maxMatches = 0;
            return maxMatches;
        }
    }
}
