using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TypeCobol.Transform
{
    /// <summary>
    /// This class implements the encoder from a TypeCobol Source code to Mixed Cobol generated source and a commented original
    /// TypeCobol sourcde code and provides de decoder method for the mixed source to the original TypeCobol source code.
    /// </summary>
    public class Decoder
    {
        private static string PROGNAME = System.AppDomain.CurrentDomain.FriendlyName;
        const String DoNotEdit = "000000* DO NOT EDIT THIS FILE. AUTOMATICALLY GENERATED";
        const string Part2MagicLine = "000000*£TC-PART2££££££££££££££££££££££££££££££££££££££££££££££££££££££££";
        const string Part3MagicLine = "000000*£TC-PART3££££££££££££££££££££££££££££££££££££££££££££££££££££££££";
        const string Part4MagicLine = "000000*£TC-PART4££££££££££££££££££££££££££££££££££££££££££££££££££££££££";
        static readonly String CompilerOptionsRegExp = "(.......)?([Cc][Oo][Nn][Tt][Rr][Oo][Ll]|[Pp][Rr][Oo][Cc][Ee][Ss][Ss]|[Cc][Bb][Ll]) +";
        static System.Text.RegularExpressions.Regex CompilerOptionsRegExpMatcher;
        static readonly String TypeCobolVersionRegExp = "......\\*TypeCobol_Version\\:[Vv]?[0-9]+\\.[0-9]+(\\.[0-9]+)?.*";
        static System.Text.RegularExpressions.Regex TypeCobolVersionRegExpMatcher;
        const int LineLength = 66;
        const int CommentPos = 6;

        /// <summary>
        /// Check if the given line is Compiler Option.
        /// </summary>
        /// <param name="line">The line to check</param>
        /// <returns>True if yes, false otherwise</returns>
        public static bool MaybeOption(string line)
        {
            if (CompilerOptionsRegExpMatcher == null)
                CompilerOptionsRegExpMatcher = new System.Text.RegularExpressions.Regex(CompilerOptionsRegExp);
            return CompilerOptionsRegExpMatcher.IsMatch(line);
        }

        /// <summary>
        /// Check if the gine line is a TypeCobol version line.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public static bool MaybeTypeCobolVersion(string line)
        {
            if (TypeCobolVersionRegExpMatcher == null)
                TypeCobolVersionRegExpMatcher = new System.Text.RegularExpressions.Regex(TypeCobolVersionRegExp);
            return TypeCobolVersionRegExpMatcher.IsMatch(line);
        }

        /// <summary>
        /// Encode will concatenates typecbolLine and cobol85Lines into outputStringBuilder
        /// </summary>
        /// <param name="typeCobolLines">TypeCobol source lines</param>
        /// <param name="cobol85Lines">Generated Cobol lines</param>
        /// <param name="outputStringBuilder">Output string builder that contains mixed TypeCobol/GeneratedCobol</param>
        /// <returns></returns>
        public static bool Encode(string[] typeCobolLines, string[] cobol85Lines, StringBuilder outputStringBuilder)
        {
            try
            {
                var CBLDirectiveLines = new List<string>();
                foreach (var typeCobolLine in typeCobolLines)
                {
                    if (MaybeOption(typeCobolLine))
                    {
                        outputStringBuilder.AppendLine(typeCobolLine); //Write CBL lines at the top of the document
                        CBLDirectiveLines.Add(typeCobolLine);
                    }
                    else break;
                }

                int part2Start = CBLDirectiveLines.Count + 3;
                int part3Start = part2Start + (cobol85Lines != null ? (cobol85Lines.Length != 0 ? cobol85Lines.Length - CBLDirectiveLines.Count : 0) : 0) + 1;
                int part4Start = part3Start + typeCobolLines.Length - CBLDirectiveLines.Count + 1;
                //string firstLine = string.Format("000000*£TC-PART1£PART2-{0:000000}£PART3-{1:000000}£PART4-{2:000000}£££££££££££££££££", 
                //                part2Start, part3Start, part4Start);
                //outputWriter.WriteLine(firstLine);

                outputStringBuilder.AppendLine(string.Format("000000*£TC-PART1£PART2-{0:000000}£PART3-{1:000000}£PART4-{2:000000}£££££££££££££££££",
                                part2Start, part3Start, part4Start));
                outputStringBuilder.AppendLine(DoNotEdit);


                //Part 2 - Cobol 85 generated code
                bool stopMaybeOptions = false;
                outputStringBuilder.AppendLine("000000*£TC-PART2££££££££££££££££££££££££££££££££££££££££££££££££££££££££");
                foreach (var cobol85Line in cobol85Lines)
                {
                    if (!stopMaybeOptions)
                    {
                        if (MaybeOption(cobol85Line))
                            continue; //Ignore this line cause it contains CBL directive
                        else if (!MaybeTypeCobolVersion(cobol85Line))
                            stopMaybeOptions = true;
                    }
                    outputStringBuilder.AppendLine(cobol85Line);
                }

                //Part 3 - TypeCobol without 7th column
                outputStringBuilder.AppendLine(Part3MagicLine);
                System.Text.StringBuilder columns7 = new System.Text.StringBuilder(part4Start - part3Start);
                foreach (var typeCobolLine in typeCobolLines)
                {
                    if (CBLDirectiveLines.Contains(typeCobolLine))
                        continue; //Ignore this line cause it contains CBL directive

                    if (typeCobolLine.Length >= CommentPos)
                    {
                        //TODO Check the length >= 8
                        if (typeCobolLine.Length > 7)
                            outputStringBuilder.AppendLine("000000*" + typeCobolLine.Substring(7));
                        else
                            outputStringBuilder.AppendLine("000000*");
                        if (typeCobolLine.Length > CommentPos)
                            columns7.Append(typeCobolLine[CommentPos]);
                        else
                            columns7.Append(' ');
                    }
                    else
                    {
                        outputStringBuilder.AppendLine("000000*");
                        columns7.Append(' ');
                    }
                }

                //Part 4 - 7th column of the TypeCobol part 3
                outputStringBuilder.AppendLine(Part4MagicLine);
                String s_columns7 = columns7.ToString();
                int c7Length = (LineLength - 1);
                int nSplit = (s_columns7.Length / c7Length) + ((s_columns7.Length % c7Length) == 0 ? 0 : 1);
                for (int i = 0, sPos = 0; i < nSplit; i++, sPos += c7Length)
                {
                    outputStringBuilder.Append("000000*");
                    outputStringBuilder.AppendLine(s_columns7.Substring(sPos, Math.Min(c7Length, s_columns7.Length - sPos)));
                }
            }
            catch (Exception e)
            {//Any exception lead to an error --> This may not be a Generated Cobol file from a TypeCobol File.                
                Console.WriteLine(String.Format("{0} : {1}", PROGNAME, string.Format(Resource.Exception_error, e.Message)));
                return false;
            }

            return true;
        }


        /// <summary>
        /// Encoder method that concatenates the TypeCobol Source code with the generated Cobol source code.
        /// </summary>
        /// <param name="typeCobolFilePath">The path to the original TypeCobol source file</param>
        /// <param name="cobol85FilePath">The path to the generated Cobol 85 source file, if null this means that an empty generated file is requested.</param>
        /// <param name="outputFilePath">The path to the output file which will contains the conactenation.</param>
        /// <returns>true if the conactenation was successful, false otherwise</returns>
	    public static bool concatenateFiles(string typeCobolFilePath, string cobol85FilePath, string outputFilePath)
        {
            try
            {
                Stream outputStream = File.OpenWrite(outputFilePath);
                var outputWriter = new StreamWriter(outputStream);

                string[] typeCobolLines = File.ReadAllLines(typeCobolFilePath);
                string[] cobol85Lines = cobol85FilePath != null ? File.ReadAllLines(cobol85FilePath) : new string[0];
                var outputStringBuilder = new StringBuilder();

                Encode(typeCobolLines, cobol85Lines, outputStringBuilder);

                outputWriter.Write(outputStringBuilder);

                outputWriter.Flush();
                outputStream.Close();
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }


        /// <summary>
        /// he decoder method which extract the original TypeCobol source code froma mixed source code.
        /// </summary>
        /// <param name="concatenatedFilePath">The path to the concatened source file</param>
        /// <param name="typeCobolOutputFilePath">The output file which will contains the original TypeCobol source codde</param>
        /// <returns>True if the decoding is successfull, false otherwise</returns>
	    public static int decode(string concatenatedFilePath, string typeCobolOutputFilePath)
        {
            Stream outputStream = File.OpenWrite(typeCobolOutputFilePath);
            var outputWriter = new StreamWriter(outputStream);
            try
            {
                var tcLines = new List<string>();
                var tcLinesCol7 = new StringBuilder();
                bool isInPart3 = false, isInPart4 = false;
                int part3Length = 0;
                int part3StartFromLine1 = 0;
                int realPart3LineNumber = 0;
                var CBLDirectiveLines = new List<string>();

                bool stopMaybeOptions = false;
                foreach (var line in File.ReadLines(concatenatedFilePath))
                {
                    if (!stopMaybeOptions)
                    {
                        if (MaybeOption(line))
                        {
                            realPart3LineNumber--; //Avoid a false positive line part3 change
                            CBLDirectiveLines.Add(line);
                            continue;
                        }
                        else
                        {
                            stopMaybeOptions = true;
                        }
                    }

                    if (!isInPart3 && !isInPart4)
                        realPart3LineNumber++;

                    if (line.Contains("*£TC-PART1")) //Detect first line
                    {
                        part3StartFromLine1 = Convert.ToInt32(line.Substring(36, 6));
                        continue; //Go on the next line
                    }
                    else if (line.Contains("*£TC-PART3")) //Detect start of Part 3
                    {
                        isInPart3 = true;
                        continue;
                    }
                    else if (line.Contains("*£TC-PART4")) //Detect start of part 4
                    {
                        isInPart3 = false;
                        isInPart4 = true;
                        continue;
                    }

                    if (isInPart3) //If inside part 3 add lines
                    {
                        if (line.Length >= 7)
                            tcLines.Add(line.Substring(7));
                        else
                            tcLines.Add("");

                        part3Length++;
                    }

                    if (isInPart4) // If inside of part 4 append
                    {
                        if (line.Length >= 7)
                        {   //We must remove any character after column 72
                            int len = (line.Length > 72 ? 72 : line.Length) - 7;
                            string transcript = line.Substring(7, len);
                            tcLinesCol7.Append(transcript.PadRight(LineLength - 1));
                        }
                    }
                }

                foreach (var CBLDirectiveLine in CBLDirectiveLines)
                {
                    outputWriter.WriteLine(CBLDirectiveLine);
                }

                //Write
                for (var i = 0; i < part3Length; i++)
                {
                    String line = (tcLinesCol7[i] + tcLines[i]);
                    if (line.Trim().Length != 0)
                        outputWriter.Write("      ");//Write spaces for line number
                    else
                        line = "";
                    if (i != part3Length - 1)
                        outputWriter.WriteLine(line);
                    else
                        outputWriter.Write(line);
                }

                return Math.Abs(realPart3LineNumber - part3StartFromLine1);
            }
            catch (Exception e)
            {//Any exception lead to an error --> This may not be a Generated Cobol file from a TypeCobol File.                
                Console.WriteLine(String.Format("{0} : {1}", PROGNAME, string.Format(Resource.Exception_error, e.Message)));
                return -1; //In case of error
            }
            finally
            {
                outputWriter.Flush();
                outputWriter.Close();
            }
        }
    }
}

