using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileAnalysisTools
{
    class Common
    {
        private const int MaxStates = 6 * 50 + 100;
        private const int MaxChars = 26;

        private int[] Out = new int[MaxStates];
        private int[] FF = new int[MaxStates];
        private int[,] GF = new int[MaxStates, MaxChars];

        public string FetchValues(string stringWithEqualSign)
        {
            try
            {
                string[] value = stringWithEqualSign.Split('=');
                string returnValue = value[1];
                return returnValue;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public string DisplayTextForExtension(string textWithSpecialChar)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (char c in textWithSpecialChar)
                {
                    if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                        sb.Append(c);
                }
                return sb.ToString().ToUpper();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public string GetPastRetentionStatus(string lastModifiedDate, int retentionYear)
        {
            int modifiedYear = Convert.ToInt32(lastModifiedDate.Split(' ')[0].Split('/')[2]);
            int currentYear = Convert.ToInt32(DateTime.Now.Year);
            if ((currentYear - retentionYear) >= modifiedYear)
                return "YES";
            else
                return "NO";
            //DateTime lasModified = DateTime.ParseExact(lastModifiedDate, "d/M/yyyy hh:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture); //  d/M/yyyy h:mm:ss tt 10/3/2014 2:28:45 AM
            //if (DateTime.Now.Year - retentionYear >= lasModified.Year)
            //    return "YES";
            //else
            //    return "NO";
        }

        private int BuildMatchingMachine(string[] words, char lowestChar = 'a', char highestChar = 'z')
        {
            Out = Enumerable.Repeat(0, Out.Length).ToArray();
            FF = Enumerable.Repeat(-1, FF.Length).ToArray();

            for (int i = 0; i < MaxStates; ++i)
            {
                for (int j = 0; j < MaxChars; ++j)
                {
                    GF[i, j] = -1;
                }
            }

            int states = 1;

            for (int i = 0; i < words.Length; ++i)
            {
                string keyword = words[i];
                int currentState = 0;

                for (int j = 0; j < keyword.Length; ++j)
                {
                    int c = keyword[j] - lowestChar;

                    if (GF[currentState, c] == -1)
                    {
                        GF[currentState, c] = states++;
                    }

                    currentState = GF[currentState, c];
                }

                Out[currentState] |= (1 << i);
            }

            for (int c = 0; c < MaxChars; ++c)
            {
                if (GF[0, c] == -1)
                {
                    GF[0, c] = 0;
                }
            }

            List<int> q = new List<int>();
            for (int c = 0; c <= highestChar - lowestChar; ++c)
            {
                if (GF[0, c] != -1 && GF[0, c] != 0)
                {
                    FF[GF[0, c]] = 0;
                    q.Add(GF[0, c]);
                }
            }

            while (Convert.ToBoolean(q.Count))
            {
                int state = q[0];
                q.RemoveAt(0);

                for (int c = 0; c <= highestChar - lowestChar; ++c)
                {
                    if (GF[state, c] != -1)
                    {
                        int failure = FF[state];

                        while (GF[failure, c] == -1)
                        {
                            failure = FF[failure];
                        }

                        failure = GF[failure, c];
                        FF[GF[state, c]] = failure;
                        Out[GF[state, c]] |= Out[failure];
                        q.Add(GF[state, c]);
                    }
                }
            }

            return states;
        }

        private int FindNextState(int currentState, char nextInput, char lowestChar = 'a')
        {
            int answer = currentState;
            int c = nextInput - lowestChar;

            while (GF[answer, c] == -1)
            {
                answer = FF[answer];
            }

            return GF[answer, c];
        }

        public List<int> FindAllStates(string text, string[] keywords, char lowestChar = 'a', char highestChar = 'z')
        {
            BuildMatchingMachine(keywords, lowestChar, highestChar);

            int currentState = 0;
            List<int> retVal = new List<int>();

            for (int i = 0; i < text.Length; ++i)
            {
                currentState = FindNextState(currentState, text[i], lowestChar);

                if (Out[currentState] == 0)
                    continue;

                for (int j = 0; j < keywords.Length; ++j)
                {
                    if (Convert.ToBoolean(Out[currentState] & (1 << j)))
                    {
                        retVal.Insert(0, i - keywords[j].Length + 1);
                    }
                }
            }

            return retVal;
        }
    }
}
